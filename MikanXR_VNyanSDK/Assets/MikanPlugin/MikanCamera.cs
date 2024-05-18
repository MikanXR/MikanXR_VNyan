using System;
using Klak.Spout;
using MikanXR;
using MikanCore;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace MikanXRPlugin
{
    public class MikanCamera : MikanBehavior
    {
		public static SpoutResources spoutShaderResources = null;
		public static Shader rgbDepthPackShader = null;

        private Camera _xrCamera = null;

        private RenderTexture _colorRenderTexture = null;
        public RenderTexture ColorRenderTexture => _colorRenderTexture;

        private RenderTexture _packDepthRenderTexture = null;
        private RenderTexture PackDepthRenderTexture => _packDepthRenderTexture;

        private SpoutSender _colorSender= null;
        private SpoutSender _depthSender= null;

        private ulong _lastRenderedFrame = 0;
        private MikanClientGraphicsApi _mikanClientGraphicsApi = MikanClientGraphicsApi.UNKNOWN;

		private Material _material;
		private Material material
		{
			get
			{
				if (_material == null && rgbDepthPackShader != null)
				{
					_material = new Material(rgbDepthPackShader);
					_material.hideFlags = HideFlags.HideAndDontSave;
				}
				return _material;
			}
		}

		public void Start()
        {
            Log(MikanLogLevel.Info, "MikanCamera Start called");

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

        public void BindSpoutSenders()
        {
            Log(MikanLogLevel.Info, "MikanCamera BindSpoutSenders called");

			SpoutSender[] senderComponents = gameObject.GetComponents<SpoutSender>();
            if (senderComponents.Length == 2)
            {
				string clientId = MikanManager.Instance.MikanClient.ClientInfo.clientId;

				_colorSender = senderComponents[0];
				_colorSender.SetResources(spoutShaderResources);
				_colorSender.spoutName = clientId + "_color";
				_colorSender.keepAlpha = true;
				_colorSender.captureMethod = CaptureMethod.Texture;
                Log(MikanLogLevel.Info, $"  Setup spout sender {_colorSender.spoutName}");

				_depthSender = senderComponents[1];
				_depthSender.SetResources(spoutShaderResources);
				_depthSender.spoutName = clientId + "_depth";
				_depthSender.keepAlpha = true;
				_depthSender.captureMethod = CaptureMethod.Texture;
                Log(MikanLogLevel.Info, $"  Setup spout sender {_depthSender.spoutName}");
			}
            else
            {
				Log(MikanLogLevel.Error, "  MikanCamera: Failed to find SpoutSender components");
				_colorSender= null;
                _depthSender= null;
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

            _colorRenderTexture = new RenderTexture(width, height, 32, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                anisoLevel = 0
            };

			_packDepthRenderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
			{
				antiAliasing = 1,
				wrapMode = TextureWrapMode.Clamp,
				useMipMap = false,
				anisoLevel = 0
			};

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

            if (_colorSender != null)
            {
                _colorSender.sourceTexture = _colorRenderTexture;
            }

			if (_packDepthRenderTexture != null)
			{
				_depthSender.sourceTexture = _packDepthRenderTexture;
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

				if (material != null)
				{
					Graphics.Blit(_colorRenderTexture, _packDepthRenderTexture, material);
				}
			}

            if (_mikanClientGraphicsApi == MikanClientGraphicsApi.Direct3D11 ||
                _mikanClientGraphicsApi == MikanClientGraphicsApi.OpenGL)
            {
                IntPtr textureNativePtr = _colorRenderTexture.GetNativeTexturePtr();
                IntPtr depthTextureNativePtr = _packDepthRenderTexture.GetNativeTexturePtr();

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
