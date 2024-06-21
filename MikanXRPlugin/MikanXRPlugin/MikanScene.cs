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

		// The table of anchors currently active in the scene
		private Dictionary<MikanSpatialAnchorID, MikanAnchor> _sceneAnchors
			= new Dictionary<MikanSpatialAnchorID, MikanAnchor>();

		// Tables of each kind of stencil currently active in the scene
		private Dictionary<MikanStencilID, MikanQuadStencil> _sceneQuadStencils
			= new Dictionary<MikanStencilID, MikanQuadStencil>();
		private Dictionary<MikanStencilID, MikanBoxStencil> _sceneBoxStencils
			= new Dictionary<MikanStencilID, MikanBoxStencil>();
		private Dictionary<MikanStencilID, MikanModelStencil> _sceneModelStencils
			= new Dictionary<MikanStencilID, MikanModelStencil>();

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
			client.OnQuadStencilListChanged.AddListener(HandleQuadStencilListChanged);
			client.OnBoxStencilListChanged.AddListener(HandleBoxStencilListChanged);
			client.OnModelStencilListChanged.AddListener(HandleModelStencilListChanged);
			client.OnStencilPoseChanged.AddListener(HandleStencilPoseChanged);
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
			client.OnQuadStencilListChanged.RemoveListener(HandleQuadStencilListChanged);
			client.OnBoxStencilListChanged.RemoveListener(HandleBoxStencilListChanged);
			client.OnModelStencilListChanged.RemoveListener(HandleModelStencilListChanged);
			client.OnStencilPoseChanged.RemoveListener(HandleStencilPoseChanged);
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
			HandleQuadStencilListChanged();
			HandleBoxStencilListChanged();
			HandleModelStencilListChanged();
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

		public async void HandleQuadStencilListChanged()
		{
			MikanResponse listResponse = await _client.ClientAPI.StencilAPI.getQuadStencilList();
			if (listResponse.resultCode == MikanResult.Success)
			{
				var stencilList = listResponse as MikanStencilList;

				// Destroy any Quad Stencils that are no longer in the scene
				foreach (var kvp in _sceneQuadStencils)
				{
					MikanStencilID StencilID = kvp.Key;
					MikanQuadStencil Stencil = kvp.Value;

					if (!stencilList.stencil_id_list.Contains(StencilID))
					{
						Destroy(Stencil.gameObject);
					}
				}

				// Update any existing Quad Stencils or spawn new ones
				foreach (var StencilID in stencilList.stencil_id_list)
				{
					MikanResponse StencilResponse = await _client.ClientAPI.StencilAPI.getQuadStencil(StencilID);
					if (StencilResponse.resultCode == MikanResult.Success)
					{
						MikanStencilQuadInfo modelStencilInfo = StencilResponse as MikanStencilQuadInfo;

						if (_sceneQuadStencils.TryGetValue(StencilID, out MikanQuadStencil stencil))
						{
							stencil.ApplyStencilInfo(modelStencilInfo);
						}
						else
						{
							_sceneQuadStencils.Add(
								StencilID, 
								MikanQuadStencil.SpawnStencil(this, modelStencilInfo));
						}
					}
				}
			}
		}

		public async void HandleBoxStencilListChanged()
		{
			MikanResponse listResponse = await _client.ClientAPI.StencilAPI.getBoxStencilList();
			if (listResponse.resultCode == MikanResult.Success)
			{
				var stencilList = listResponse as MikanStencilList;

				// Destroy any Box Stencils that are no longer in the scene
				foreach (var kvp in _sceneBoxStencils)
				{
					MikanStencilID StencilID = kvp.Key;
					MikanStencil Stencil = kvp.Value;

					if (!stencilList.stencil_id_list.Contains(StencilID))
					{
						Destroy(Stencil.gameObject);
					}
				}

				// Update any existing Box Stencils or spawn new ones
				foreach (var StencilID in stencilList.stencil_id_list)
				{
					MikanResponse StencilResponse = await _client.ClientAPI.StencilAPI.getBoxStencil(StencilID);
					if (StencilResponse.resultCode == MikanResult.Success)
					{
						var StencilInfo = StencilResponse as MikanStencilBoxInfo;

						if (_sceneBoxStencils.TryGetValue(StencilID, out MikanBoxStencil stencil))
						{
							stencil.ApplyBoxStencilInfo(StencilInfo);
						}
						else
						{
							_sceneBoxStencils.Add(StencilID, MikanBoxStencil.SpawnStencil(this, StencilInfo));
						}
					}
				}
			}
		}

		public async void HandleModelStencilListChanged()
		{
			MikanResponse listResponse = await _client.ClientAPI.StencilAPI.getModelStencilList();
			if (listResponse.resultCode == MikanResult.Success)
			{
				var stencilList = listResponse as MikanStencilList;

				// Destroy any Model Stencils that are no longer in the scene
				foreach (var kvp in _sceneModelStencils)
				{
					MikanStencilID StencilID = kvp.Key;
					MikanStencil Stencil = kvp.Value;

					if (!stencilList.stencil_id_list.Contains(StencilID))
					{
						Destroy(Stencil.gameObject);
					}
				}

				// Update any existing Model Stencils or spawn new ones
				foreach (var StencilID in stencilList.stencil_id_list)
				{
					// Update or create core stencil object
					MikanResponse StencilResponse = await _client.ClientAPI.StencilAPI.getModelStencil(StencilID);
					if (StencilResponse.resultCode == MikanResult.Success)
					{
						var StencilInfo = StencilResponse as MikanStencilModelInfo;

						if (_sceneModelStencils.TryGetValue(StencilID, out MikanModelStencil modelStencil))
						{
							// Update existing stencil info
							modelStencil.ApplyModelStencilInfo(StencilInfo);
						}
						else
						{
							// Spawn in a new stencil
							modelStencil= MikanModelStencil.SpawnStencil(this, StencilInfo);

							if (modelStencil != null)
							{
								_sceneModelStencils.Add(StencilID, modelStencil);

								// Fetch the meshes for the stencil
								MikanResponse MeshResponse = 
									await _client.ClientAPI.StencilAPI.getModelStencilRenderGeometry(StencilID);
								if (MeshResponse.resultCode == MikanResult.Success)
								{
									var modelGeo = MeshResponse as MikanStencilModelRenderGeometry;

									modelStencil.ApplyModelRenderGeometry(modelGeo);
								}
							}
						}
					}
				}
			}
		}

		public MikanStencil GetMikanStencilById(MikanStencilID stencilId)
		{
			if (_sceneModelStencils.TryGetValue(stencilId, out MikanModelStencil modelStencil))
			{
				return modelStencil;
			}

			if (_sceneBoxStencils.TryGetValue(stencilId, out MikanBoxStencil boxStencil))
			{
				return boxStencil;
			}

			if (_sceneQuadStencils.TryGetValue(stencilId, out MikanQuadStencil quadStencil))
			{
				return quadStencil;
			}

			return null;
		}

		public void HandleStencilPoseChanged(MikanStencilPoseUpdateEvent StencilPoseEvent)
		{
			MikanStencil Stencil = GetMikanStencilById(StencilPoseEvent.stencil_id);
			if (Stencil != null)
			{
				Stencil.HandleStencilPoseChanged(StencilPoseEvent);
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