using System;
using System.Collections.Generic;
using UnityEngine;
using MikanXR;
using MikanCore;

namespace MikanXRPlugin
{
	using MikanSpatialAnchorID = Int32;
	using MikanStencilID = Int32;

	public class MikanScene : MonoBehaviour
	{
		public MikanManager OwnerManager { get; private set; }
		private MikanClient _client = null;
		private MikanLogger _logger = null;

		private MikanCamera _sceneCamera = null;
		public MikanCamera SceneCamera => _sceneCamera;

		private float _cameraPositionScale = 1f;

		// The table of scene anchors currently active in the scene
		private Dictionary<MikanSpatialAnchorID, MikanAnchor> _sceneAnchors
			= new Dictionary<MikanSpatialAnchorID, MikanAnchor>();

		public Color BackgroundColorKey = new Color(0.0f, 0.0f, 0.0f, 0.0f);

		public static MikanScene SpawnScene(MikanManager ownerManager)
		{
			var mikanSceneObject = new GameObject(
				"MikanSceneObject",
				new System.Type[] {
					typeof(MikanScene),
					typeof(MikanScriptEventHandler)
				});

			MikanScene sceneComponent = mikanSceneObject.GetComponent<MikanScene>();
			sceneComponent.Setup(ownerManager);

			return sceneComponent;
		}

		public void DespawnScene(MikanScene mikanScene)
		{
			Teardown();
			Destroy(mikanScene.gameObject);
		}

		private void Setup(MikanManager ownerManager)
		{
			// Cache references to common mikan objects
			OwnerManager = ownerManager;
			_client = ownerManager.MikanClient;
			_logger = _client.ClientAPI.CoreAPI.Logger;

			// Bind client events
			BindClientEvents(_client);

			// Bind settings events
			BindSettingsEvents(ownerManager.OwnerPlugin.Settings);

			// Spawn the Mikan camera
			SpawnMikanCamera();

			// If we are already connected to Mikan,
			// tell the scene to fetch anchors, setup scene transform, etc
			// Otherwise wait for MikanManager to tell the scene about the connection
			if (_client.ClientAPI.GetIsConnected())
			{
				HandleMikanConnected();
			}
		}

		private void Teardown()
		{
			// Unbind settings events
			UnbindSettingsEvents(OwnerManager.OwnerPlugin.Settings);

			// Unbind client events
			UnbindClientEvents(_client);

			// Despawn the Mikan camera
			DespawnMikanCamera();

			// Forget cached references
			_logger = null;
			_client = null;
			OwnerManager = null;
		}

		private void BindClientEvents(MikanClient client)
		{
			client.OnConnected.AddListener(HandleMikanConnected);
			client.OnDisconnected.AddListener(HandleMikanDisconnected);
			client.OnRenderBufferCreated.AddListener(HandleRenderBufferCreated);
			client.OnRenderBufferDisposed.AddListener(HandleRenderBufferDisposed);
			client.OnAnchorListChanged.AddListener(HandleAnchorListChanged);
			client.OnAnchorPoseChanged.AddListener(HandleAnchorPoseChanged);
			client.OnNewFrameReceived.AddListener(HandleNewVideoSourceFrame);
		}

		private void UnbindClientEvents(MikanClient client)
		{
			client.OnConnected.RemoveListener(HandleMikanConnected);
			client.OnDisconnected.RemoveListener(HandleMikanDisconnected);
			client.OnRenderBufferCreated.RemoveListener(HandleRenderBufferCreated);
			client.OnRenderBufferDisposed.RemoveListener(HandleRenderBufferDisposed);
			client.OnAnchorListChanged.RemoveListener(HandleAnchorListChanged);
			client.OnAnchorPoseChanged.RemoveListener(HandleAnchorPoseChanged);
			client.OnNewFrameReceived.RemoveListener(HandleNewVideoSourceFrame);
		}

		private void BindSettingsEvents(MikanSettings settings)
		{
			settings.OnCameraPositionScaleChanged.AddListener(HandleCameraPositionScaleChanged);
		}

		private void UnbindSettingsEvents(MikanSettings settings)
		{
			settings.OnCameraPositionScaleChanged.RemoveListener(HandleCameraPositionScaleChanged);
		}

		void SpawnMikanCamera()
		{
			if (_sceneCamera == null)
			{
				_logger.Log(MikanLogLevel.Info, "MikanManager: Spawning Mikan camera");
				_sceneCamera= MikanCamera.SpawnCamera(this);
			}
			else
			{
				_logger.Log(MikanLogLevel.Info, "MikanManager: Ignoring camera spawn request. Already spawned.");
			}
		}

		void DespawnMikanCamera()
		{
			if (_sceneCamera != null)
			{
				_logger.Log(MikanLogLevel.Info, "MikanManager: Despawn Mikan camera");
				MikanCamera.DespawnCamera(_sceneCamera);
				_sceneCamera = null;
			}
			else
			{
				_logger.Log(MikanLogLevel.Info, "MikanManager: Ignoring camera de-spawn request. Already despawned.");
			}
		}

		public MikanAnchor GetMikanAnchorById(MikanSpatialAnchorID anchorId)
		{
			if (_sceneAnchors.TryGetValue(anchorId, out MikanAnchor anchor))
			{
				return anchor;
			}

			return null;
		}

		public MikanAnchor GetMikanAnchorByName(string anchorName)
		{
			foreach (var kvp in _sceneAnchors)
			{
				MikanAnchor anchor = kvp.Value;

				if (anchor.AnchorName == anchorName)
				{
					return anchor;
				}
			}

			return null;
		}

		protected void HandleMikanConnected()
		{
			HandleCameraIntrinsicsChanged();
			HandleAnchorListChanged();
		}

		protected void HandleMikanDisconnected()
		{
			HandleRenderBufferDisposed();
		}

		protected void HandleRenderBufferCreated()
		{
			if (_client.IsRenderTargetDescriptorValid)
			{
				if (_sceneCamera != null)
				{
					_sceneCamera.RecreateRenderTarget(_client.RenderTargetDescriptor);
				}
				else
				{
					_logger.Log(MikanLogLevel.Warning, "MikanScene: Missing expected scene camera.");
				}
			}
			else
			{
				_logger.Log(MikanLogLevel.Error, "MikanScene: Invalid render target descriptor!");
			}
		}

		protected void HandleRenderBufferDisposed()
		{
			if (_sceneCamera != null)
			{
				_sceneCamera.DisposeRenderTarget();
			}
		}

		public async void HandleAnchorListChanged()
		{
			MikanResponse listResponse = await _client.ClientAPI.SpatialAnchorAPI.GetSpatialAnchorList();
			if (listResponse.resultCode == MikanResult.Success)
			{
				MikanSpatialAnchorList spatialAnchorList = listResponse as MikanSpatialAnchorList;

				// Destroy any anchors that are no longer in the scene
				foreach (var kvp in _sceneAnchors)
				{
					MikanSpatialAnchorID anchorID = kvp.Key;
					MikanAnchor anchor = kvp.Value;

					if (!spatialAnchorList.spatial_anchor_id_list.Contains(anchorID))
					{
						Destroy(anchor.gameObject);
					}
				}

				foreach (var anchorID in spatialAnchorList.spatial_anchor_id_list)
				{
					MikanResponse anchorResponse = await _client.ClientAPI.SpatialAnchorAPI.getSpatialAnchorInfo(anchorID);
					if (anchorResponse.resultCode == MikanResult.Success)
					{
						MikanSpatialAnchorInfo anchorInfo = anchorResponse as MikanSpatialAnchorInfo;

						if (_sceneAnchors.TryGetValue(anchorID, out MikanAnchor anchor))
						{
							anchor.ApplyAnchorInfo(anchorInfo);
						}
						else
						{
							_sceneAnchors.Add(anchorID, MikanAnchor.SpawnAnchor(this, anchorInfo));
						}
					}
				}
			}
		}

		public void HandleAnchorPoseChanged(MikanAnchorPoseUpdateEvent AnchorPoseEvent)
		{
			MikanAnchor anchor = GetMikanAnchorById(AnchorPoseEvent.anchor_id);
			if (anchor != null)
			{
				anchor.HandleAnchorPoseChanged(AnchorPoseEvent);
			}
		}

		public void HandleCameraIntrinsicsChanged()
		{
			if (_sceneCamera != null)
			{
				_sceneCamera.HandleCameraIntrinsicsChanged();
			}
		}

		public void HandleCameraPositionScaleChanged(float newScale)
		{
			_cameraPositionScale = newScale;
		}

		public void HandleNewVideoSourceFrame(MikanVideoSourceNewFrameEvent newFrameEvent)
		{
			if (_sceneCamera != null)
			{
				// Compute the camera transform in Mikan Space (but in Unity coordinate system)
				Vector3 sceneCameraPosition = MikanMath.MikanVector3fToVector3(newFrameEvent.cameraPosition);
				Vector3 sceneCameraForward = MikanMath.MikanVector3fToVector3(newFrameEvent.cameraForward);
				Vector3 sceneCameraUp = MikanMath.MikanVector3fToVector3(newFrameEvent.cameraUp);
				Quaternion sceneCameraRotation = Quaternion.LookRotation(sceneCameraForward, sceneCameraUp);
				Matrix4x4 sceneCameraTransform = Matrix4x4.TRS(sceneCameraPosition, sceneCameraRotation, Vector3.one);

				// Update the camera's transform relative to the parent scene
				_sceneCamera.transform.localPosition =
					MikanMath.ExtractTranslationFromMatrix(sceneCameraTransform) * _cameraPositionScale;
				_sceneCamera.transform.localRotation =
					MikanMath.ExtractRotationFromMatrix(sceneCameraTransform);

				// Render out a new frame
				_sceneCamera.CaptureFrame(newFrameEvent.frame);
			}
		}
	}
}