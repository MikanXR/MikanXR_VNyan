using System;
using Klak.Spout;
using MikanXR;
using MikanCore;
using UnityEngine;
using System.Runtime.CompilerServices;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Reflection;

namespace MikanXRPlugin
{
	public class MikanCamera : MikanBehavior
	{
		public static Shader rgbDepthPackShader = null;

		private Material _depthPackMaterial = null;

		private Camera _xrCamera = null;

		private RenderTexture _colorRenderTexture = null;
		public RenderTexture ColorRenderTexture => _colorRenderTexture;

		private RenderTexture _packDepthRenderTexture = null;
		private RenderTexture PackDepthRenderTexture => _packDepthRenderTexture;

		private System.Type _spoutSenderType = null;
		private MethodInfo _spoutSenderUpdateMethodInfo = null;
		private MethodInfo _spoutSenderDisposeMethodInfo = null;
		private System.Object _colorSenderObject = null;
		private System.Object _depthSenderObject = null;

		private LineRenderer _frustumRenderer = null;
		private float _hfovRadians = 90f * Mathf.Deg2Rad;
		private float _vfovRadians = 90f * Mathf.Deg2Rad;
		private float _zNear = 0.1f;
		private float _zFar = 100f;

		private ulong _lastRenderedFrame = 0;
		private MikanClientGraphicsApi _mikanClientGraphicsApi = MikanClientGraphicsApi.UNKNOWN;

		public void Start()
		{
			Log(MikanLogLevel.Info, "MikanCamera Start called");

			FetchSpoutAPI();
			RebuildFrustumGeometry();

			if (_xrCamera == null)
			{
				BindXRCamera();
			}
		}

		public void OnDestroy()
		{
			Log(MikanLogLevel.Info, "MikanCamera OnDestroy called");
			DisposeRenderTarget();
		}

		private void RebuildFrustumGeometry()
		{
			if (_frustumRenderer == null)
			{
				_frustumRenderer = gameObject.GetComponent<LineRenderer>();
			}

			if (_frustumRenderer != null)
			{
				_frustumRenderer.useWorldSpace = false;
				_frustumRenderer.startWidth = 0.05f;
				_frustumRenderer.endWidth = 0.05f;
				_frustumRenderer.startColor = Color.yellow;
				_frustumRenderer.endColor = Color.yellow;
				_frustumRenderer.material = new Material(Shader.Find("Unlit/Color"));
				_frustumRenderer.material.color = Color.yellow;
				_frustumRenderer.gameObject.layer = LayerMask.NameToLayer("UI");

				float HRatio = Mathf.Tan(_hfovRadians / 2f);
				float VRatio = Mathf.Tan(_vfovRadians / 2f);

				Vector3 cameraRight = Vector3.right;
				Vector3 cameraUp = Vector3.up;
				Vector3 cameraForward = Vector3.forward;
				Vector3 cameraOrigin = Vector3.zero;

				Vector3 nearX = cameraRight * _zNear * HRatio;
				Vector3 farX = cameraRight * _zFar * HRatio;

				Vector3 nearY = cameraUp * _zNear * VRatio;
				Vector3 farY = cameraUp * _zFar * VRatio;

				Vector3 nearZ = cameraForward * _zNear;
				Vector3 farZ = cameraForward * _zFar;

				Vector3 nearCenter = cameraOrigin + nearZ;
				Vector3 near0 = cameraOrigin + nearX + nearY + nearZ;
				Vector3 near1 = cameraOrigin - nearX + nearY + nearZ;
				Vector3 near2 = cameraOrigin - nearX - nearY + nearZ;
				Vector3 near3 = cameraOrigin + nearX - nearY + nearZ;

				Vector3 far0 = cameraOrigin + farX + farY + farZ;
				Vector3 far1 = cameraOrigin - farX + farY + farZ;
				Vector3 far2 = cameraOrigin - farX - farY + farZ;
				Vector3 far3 = cameraOrigin + farX - farY + farZ;

				_frustumRenderer.positionCount = 26;

				// Far Cone
				_frustumRenderer.SetPosition(0, cameraOrigin);
				_frustumRenderer.SetPosition(1, far0);
				_frustumRenderer.SetPosition(2, far1);

				_frustumRenderer.SetPosition(3, cameraOrigin);
				_frustumRenderer.SetPosition(4, far1);
				_frustumRenderer.SetPosition(5, far2);

				_frustumRenderer.SetPosition(6, cameraOrigin);
				_frustumRenderer.SetPosition(7, far2);
				_frustumRenderer.SetPosition(8, far3);

				_frustumRenderer.SetPosition(9, cameraOrigin);
				_frustumRenderer.SetPosition(10, far3);
				_frustumRenderer.SetPosition(11, far0);

				// Near Cone
				_frustumRenderer.SetPosition(12, cameraOrigin);
				_frustumRenderer.SetPosition(13, near0);
				_frustumRenderer.SetPosition(14, near1);

				_frustumRenderer.SetPosition(15, cameraOrigin);
				_frustumRenderer.SetPosition(16, near1);
				_frustumRenderer.SetPosition(17, near2);

				_frustumRenderer.SetPosition(18, cameraOrigin);
				_frustumRenderer.SetPosition(19, near2);
				_frustumRenderer.SetPosition(20, near3);

				_frustumRenderer.SetPosition(21, cameraOrigin);
				_frustumRenderer.SetPosition(22, near3);
				_frustumRenderer.SetPosition(23, near0);

				// Forward Direction
				_frustumRenderer.SetPosition(24, cameraOrigin);
				_frustumRenderer.SetPosition(25, nearCenter);
			}
		}

		public void BindXRCamera()
		{
			Log(MikanLogLevel.Info, "MikanCamera BindXRCamera called");

			_xrCamera = gameObject.GetComponent<Camera>();
			if (_xrCamera != null)
			{
				Log(MikanLogLevel.Info, "  Found XRCamera");

				// Don't render anything on the UI layer
				_xrCamera.cullingMask = _xrCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));
			}
			else
			{
				Log(MikanLogLevel.Warning, "  Failed to find sibling XRCamera");
			}
		}

		private void FetchSpoutAPI()
		{
			Assembly sysData = Assembly.Load("Klak.Spout.Runtime");
			_spoutSenderType = sysData.GetType("Klak.Spout.Sender");

			_spoutSenderUpdateMethodInfo = _spoutSenderType.GetMethod("Update");
			_spoutSenderDisposeMethodInfo = _spoutSenderType.GetMethod("Dispose");
		}

		private System.Object CreateSender(string senderName, Texture renderTexture)
		{
			return Activator.CreateInstance(
				   _spoutSenderType,
				   BindingFlags.Instance
				   | BindingFlags.Public
				   | BindingFlags.NonPublic,
				   null,
				   new System.Object[] { senderName, renderTexture }, // or your actual constructor arguments
				   null
				);
		}

		private void ReleaseSender(ref System.Object senderObject)
		{
			if (senderObject != null)
			{
				if (_spoutSenderDisposeMethodInfo != null)
				{
					_spoutSenderDisposeMethodInfo.Invoke(senderObject, null);
				}

				senderObject = null;
			}
		}

		public async void HandleCameraIntrinsicsChanged()
		{
			Log(MikanLogLevel.Info, "MikanCamera HandleCameraIntrinsicsChanged called");
			if (ClientAPI.GetIsConnected())
			{
				var response = await ClientAPI.VideoSourceAPI.GetVideoSourceIntrinsics();
				if (response.resultCode == MikanResult.Success)
				{
					var videoSourceIntrinsics = response as MikanVideoSourceIntrinsics;
					MikanMonoIntrinsics monoIntrinsics = videoSourceIntrinsics.mono;
					float videoSourcePixelWidth = (float)monoIntrinsics.pixel_width;
					float videoSourcePixelHeight = (float)monoIntrinsics.pixel_height;

					if (_xrCamera != null)
					{
						_xrCamera.fieldOfView = (float)monoIntrinsics.vfov;
						_xrCamera.aspect = videoSourcePixelWidth / videoSourcePixelHeight;
						_xrCamera.nearClipPlane = (float)monoIntrinsics.znear;
						_xrCamera.farClipPlane = (float)monoIntrinsics.zfar;
						_xrCamera.depthTextureMode = DepthTextureMode.Depth;
						_xrCamera.clearFlags = CameraClearFlags.SolidColor;
						_xrCamera.backgroundColor = Color.clear;

						// Disable the camera so that we can drive manual rendering
						_xrCamera.enabled = false;

						// Update frustum geometry
						_hfovRadians = (float)monoIntrinsics.hfov * Mathf.Deg2Rad;
						_vfovRadians = (float)monoIntrinsics.vfov * Mathf.Deg2Rad;
						_zNear = (float)monoIntrinsics.znear;
						_zFar = (float)monoIntrinsics.zfar;
						RebuildFrustumGeometry();

						Log(
							MikanLogLevel.Info,
							$"MikanClient: Updated camera params: fov:{_xrCamera.fieldOfView}, aspect:{_xrCamera.aspect}, near:{_xrCamera.nearClipPlane}, far:{_xrCamera.farClipPlane}");
					}
					else
					{
						Log(MikanLogLevel.Warning, "  No valid XRCamera found to apply intrinsics to.");
					}
				}
				else
				{
					Log(MikanLogLevel.Error, "  Failed to fetch camera intrinsics!");
				}
			}
			else
			{
				Log(MikanLogLevel.Info, "  Ignoring HandleCameraIntrinsicsChanged - Mikan Disconnected.");
			}
		}

		private RenderTextureDescriptor CreateUnityRenderTargetDescriptor(
			MikanRenderTargetDescriptor RTDesc,
			bool isColor)
		{
			RenderTextureDescriptor rtDesc = new RenderTextureDescriptor()
			{
				width = (int)RTDesc.width,
				height = (int)RTDesc.height,
				colorFormat = RenderTextureFormat.ARGB32,
				depthBufferBits = isColor ? 32 : 0,
				msaaSamples = 1,
				dimension = TextureDimension.Tex2D,
				volumeDepth = 1,
				enableRandomWrite = false,
				memoryless = RenderTextureMemoryless.None,
				useMipMap = false,
				autoGenerateMips = false,
				bindMS = false,
				useDynamicScale = false,
				shadowSamplingMode = ShadowSamplingMode.None,
				vrUsage = VRTextureUsage.None,
				graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm
			};

			return rtDesc;
		}

		public bool RecreateRenderTarget(MikanRenderTargetDescriptor RTDesc)
		{
			bool bSuccess = true;
			int width = (int)RTDesc.width;
			int height = (int)RTDesc.height;

			Log(MikanLogLevel.Info, "MikanCamera RecreateRenderTarget called");

			if (width <= 0 || height <= 0)
			{
				Log(MikanLogLevel.Error, "  MikanCamera: Unable to create render texture. Texture dimension must be higher than zero.");
				return false;
			}

			_mikanClientGraphicsApi = RTDesc.graphicsAPI;

			_colorRenderTexture = new RenderTexture(CreateUnityRenderTargetDescriptor(RTDesc, true));
			_packDepthRenderTexture = new RenderTexture(CreateUnityRenderTargetDescriptor(RTDesc, false));

			if (!_colorRenderTexture.Create())
			{
				Log(MikanLogLevel.Error, "  MikanCamera: Unable to create render texture.");
				return false;
			}

			if (!_packDepthRenderTexture.Create())
			{
				Log(MikanLogLevel.Error, "  MikanCamera: Unable to create render texture.");
				return false;
			}

			if (_xrCamera != null)
			{
				_xrCamera.targetTexture = _colorRenderTexture;
			}
			else
			{
				Log(MikanLogLevel.Warning, "  No valid XRCamera found to assign render target to.");
			}

			string clientId = MikanManager.Instance.MikanClient.ClientInfo.clientId;


			if (_colorRenderTexture != null)
			{
				ReleaseSender(ref _colorSenderObject);
				_colorSenderObject = CreateSender(clientId + "_color", _colorRenderTexture);
			}

			if (_packDepthRenderTexture != null)
			{
				ReleaseSender(ref _depthSenderObject);
				_depthSenderObject = CreateSender(clientId + "_depth", _packDepthRenderTexture);
			}

			if (_depthPackMaterial == null && rgbDepthPackShader != null)
			{
				_depthPackMaterial = new Material(rgbDepthPackShader);
				_depthPackMaterial.hideFlags = HideFlags.HideAndDontSave;
			}

			Log(MikanLogLevel.Info, $"  MikanCamera: Created {width}x{height} render target texture");

			return bSuccess;
		}

		public void DisposeRenderTarget()
		{
			if (_xrCamera != null)
			{
				_xrCamera.targetTexture = null;
			}

			ReleaseSender(ref _colorSenderObject);
			ReleaseSender(ref _depthSenderObject);

			if (_colorRenderTexture != null && _colorRenderTexture.IsCreated())
			{
				_colorRenderTexture.Release();
			}
			_colorRenderTexture = null;

			if (_packDepthRenderTexture != null && _packDepthRenderTexture.IsCreated())
			{
				_packDepthRenderTexture.Release();
			}
			_packDepthRenderTexture = null;
		}

		public void CaptureFrame(ulong frameIndex)
		{
			_lastRenderedFrame = frameIndex;

			if (_xrCamera != null)
			{
				_xrCamera.Render();

				if (_colorSenderObject != null)
				{
					_spoutSenderUpdateMethodInfo.Invoke(_colorSenderObject, null);
				}

				if (_depthPackMaterial != null)
				{
					Graphics.Blit(_colorRenderTexture, _packDepthRenderTexture, _depthPackMaterial);

					if (_depthSenderObject != null)
					{
						_spoutSenderUpdateMethodInfo.Invoke(_depthSenderObject, null);
					}
				}
			}

			if (_mikanClientGraphicsApi == MikanClientGraphicsApi.Direct3D11 ||
				_mikanClientGraphicsApi == MikanClientGraphicsApi.OpenGL)
			{
				// Fast interprocess shared texture transfer
				MikanClientFrameRendered frame = new MikanClientFrameRendered
				{
					frame_index = frameIndex,
					zNear = _xrCamera.nearClipPlane,
					zFar = _xrCamera.farClipPlane
				};
				ClientAPI.RenderTargetAPI.PublishRenderTargetTextures(frame);
			}
		}
	}
}
