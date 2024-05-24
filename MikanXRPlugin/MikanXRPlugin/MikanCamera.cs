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

        private ulong _lastRenderedFrame = 0;
        private MikanClientGraphicsApi _mikanClientGraphicsApi = MikanClientGraphicsApi.UNKNOWN;

		public void Start()
        {
			Log(MikanLogLevel.Info, "MikanCamera Start called");

            FetchSpoutAPI();

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

        public void BindXRCamera()
        {
            Log(MikanLogLevel.Info, "MikanCamera BindXRCamera called");

            _xrCamera = gameObject.GetComponent<Camera>();
            if (_xrCamera != null)
            {
                Log(MikanLogLevel.Info, "  Found XRCamera");
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
                _colorSenderObject= CreateSender(clientId + "_color", _colorRenderTexture);
            }

			if (_packDepthRenderTexture != null)
			{
				ReleaseSender(ref _depthSenderObject);
                _depthSenderObject= CreateSender(clientId + "_depth", _packDepthRenderTexture);
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
