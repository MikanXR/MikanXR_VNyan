using MikanCore;
using System.Threading.Tasks;

namespace MikanXR
{
	public class MikanRenderTargetAPI
	{
		private MikanRequestManager _requestManager;

		public MikanRenderTargetAPI(MikanRequestManager requestManager)
		{
			_requestManager = requestManager;
		}

		private static readonly string k_allocateRenderTargetTexture = "allocateRenderTargetTextures";
		public Task<MikanResponse> AllocateRenderTargetTextures(MikanRenderTargetDescriptor descriptor)
		{
			return _requestManager.SendRequestWithPayload(k_allocateRenderTargetTexture, descriptor);
		}

		private static readonly string k_frameRendered = "frameRendered";
		public MikanResult PublishRenderTargetTextures(MikanClientFrameRendered frameInfo)
		{
			return _requestManager.SendRequestWithPayloadNoResponse(k_frameRendered, frameInfo);
		}

		private static readonly string k_freeRenderTargetTextures = "freeRenderTargetTextures";
		public Task<MikanResponse> FreeRenderTargetTextures()
		{
			return _requestManager.SendRequest(k_freeRenderTargetTextures);
		}
	}
}
