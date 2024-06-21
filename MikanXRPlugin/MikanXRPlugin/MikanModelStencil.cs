using MikanXR;
using UnityEngine;

namespace MikanXRPlugin
{
	public class MikanModelStencil : MikanStencil
	{
		public static MikanModelStencil SpawnStencil(
			MikanScene ownerScene, 
			MikanStencilModelInfo modelStencilInfo)
		{
			var stencilPrefab = ownerScene.OwnerManager.OwnerPlugin.stencilPrefab;

			GameObject stencilGO;
			if (stencilPrefab != null)
			{
				stencilGO = Instantiate(stencilPrefab);
			}
			else
			{
				stencilGO = new GameObject();
			}
			stencilGO.name = modelStencilInfo.stencil_name;
			stencilGO.transform.SetParent(ownerScene.transform, false);

			var stencil = stencilGO.GetComponent<MikanModelStencil>();
			if (stencil == null)
			{
				stencil = stencilGO.AddComponent<MikanModelStencil>();
			}

			stencil.Setup(
				ownerScene, 
				modelStencilInfo.stencil_id, 
				modelStencilInfo.stencil_name, 
				modelStencilInfo.relative_transform);

			return stencil;
		}

		public void ApplyModelStencilInfo(MikanStencilModelInfo mikanStencilModel)
		{
			// Update the stencil transform from the event
			_mikanSpaceTransform.ApplyMikanTransform(mikanStencilModel.relative_transform);

			UpdateSceneTransform();
		}
	}
}
