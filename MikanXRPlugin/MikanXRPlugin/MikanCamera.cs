using System;
using MikanXR;
using UnityEngine;

namespace MikanXRPlugin
{
    public class MikanCamera : MikanBehavior
    {
        private Camera _xrCamera = null;
        private RenderTexture _renderTexture = null;
        private ulong _lastRenderedFrame = 0;
        private MikanClientGraphicsApi _mikanClientGraphicsApi = MikanClientGraphicsApi.UNKNOWN;

        void Start()
        {
            Log(MikanLogLevel.Info, "MikanCamera Start called");

            if (_xrCamera == null)
            {
                BindXRCamera();
            }
        }

        void OnDestroy()
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

        public async void HandleCameraIntrinsicsChanged()
        {
            var client = MikanManager.Instance.ClientAPI;

            Log(MikanLogLevel.Info, "MikanCamera HandleCameraIntrinsicsChanged called");
            if (client.GetIsConnected())
            {
                var response = await client.VideoSourceAPI.GetVideoSourceIntrinsics();
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

            int depthBufferPrecision = 32;
            _renderTexture = new RenderTexture(width, height, depthBufferPrecision, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                anisoLevel = 0
            };

            if (!_renderTexture.Create())
            {
                Log(MikanLogLevel.Error, "  MikanCamera: Unable to create render texture.");
                return false;
            }

            if (_xrCamera != null)
            {
                _xrCamera.targetTexture = _renderTexture;
            }
            else
            {
                Log(MikanLogLevel.Warning, "  No valid XRCamera found to assign render target to.");
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

            if (_renderTexture != null && _renderTexture.IsCreated())
            {
                _renderTexture.Release();
            }
            _renderTexture = null;
        }

        public void CaptureFrame(ulong frameIndex)
        {
            _lastRenderedFrame = frameIndex;

            if (_xrCamera != null)
            {
                _xrCamera.Render();
            }

            if (_mikanClientGraphicsApi == MikanClientGraphicsApi.Direct3D11 ||
                _mikanClientGraphicsApi == MikanClientGraphicsApi.OpenGL)
            {
                IntPtr textureNativePtr = _renderTexture.GetNativeTexturePtr();
                IntPtr depthTextureNativePtr = _renderTexture.GetNativeDepthBufferPtr();

                // Fast interprocess shared texture transfer
                MikanClientFrameRendered frame = new MikanClientFrameRendered
                {
                    frame_index = frameIndex,
                    zNear = _xrCamera.nearClipPlane,
                    zFar = _xrCamera.farClipPlane
                };
                MikanManager.Instance.ClientAPI.PublishRenderTargetTextures(
                    textureNativePtr, depthTextureNativePtr, ref frame);
            }
        }
    }
}
