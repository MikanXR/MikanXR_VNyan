using System.Threading.Tasks;

namespace MikanXR
{
	public class MikanStencilAPI
	{
		private MikanRequestManager _requestManager;

		public MikanStencilAPI(MikanRequestManager requestManager)
		{
			_requestManager= requestManager;
			_requestManager.AddTextResponseFactory<MikanStencilList>();
			_requestManager.AddTextResponseFactory<MikanStencilQuad>();
			_requestManager.AddTextResponseFactory<MikanStencilBox>();
			_requestManager.AddTextResponseFactory<MikanStencilModel>();
			_requestManager.AddBinaryResponseFactory<
				MikanStencilModeRenderGeometryFactory, 
				MikanStencilModelRenderGeometry>();
		}

		private static readonly string k_getStencilList = "getStencilList";
		public Task<MikanResponse> getStencilList() // returns MikanStencilList
		{
			return _requestManager.SendRequest(k_getStencilList);
		}
		
		private static readonly string k_getQuadStencil = "getQuadStencil";
		public Task<MikanResponse> getQuadStencil(int stencilId) // returns MikanStencilQuad
		{
			return _requestManager.SendRequestWithPayload<int>(k_getQuadStencil, stencilId);
		}
		
		private static readonly string k_getBoxStencil = "getBoxStencil";
		public Task<MikanResponse> getBoxStencil(int stencilId) // returns MikanStencilBox
		{
			return _requestManager.SendRequestWithPayload<int>(k_getBoxStencil, stencilId);
		}
		
		private static readonly string k_getModelStencil = "getModelStencil";
		public Task<MikanResponse> getModelStencil(int stencilId) // returns MikanStencilModel
		{
			return _requestManager.SendRequestWithPayload<int>(k_getModelStencil, stencilId);
		}

		private static readonly string k_getModelStencilMesh = "getModelStencilMesh";
		public Task<MikanResponse> getModelStencilMesh(int stencilId) // returns MikanStencilModelMesh
		{
			return _requestManager.SendRequestWithPayload<int>(k_getModelStencilMesh, stencilId);
		}
	}
}