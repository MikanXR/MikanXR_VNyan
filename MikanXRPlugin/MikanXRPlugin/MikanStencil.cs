using MikanXR;
using UnityEngine;

namespace MikanXRPlugin
{
	/// The ID of a Stencil in Mikan
	using MikanStencilID = System.Int32;

	public class MikanStencil : MonoBehaviour
	{
		public MikanScene OwnerScene
		{
			get; private set;
		}

		private MikanStencilID _stencilId = -1;
		public MikanStencilID StencilID => _stencilId;

		private string _stencilName = "";
		public string StencilName => _stencilName;

		private MikanSpaceTransform _mikanSpaceTransform = new MikanSpaceTransform();
		public MikanSpaceTransform MikanSpaceTransform => _mikanSpaceTransform;

		public static MikanStencil SpawnQuadStencil(MikanScene ownerScene, MikanStencilQuad quadStencilInfo)
		{
			return SpawnStencil(
					ownerScene,
					quadStencilInfo.stencil_id,
					quadStencilInfo.stencil_name,
					quadStencilInfo.relative_transform);
		}

		public static MikanStencil SpawnBoxStencil(MikanScene ownerScene, MikanStencilBox boxStencilInfo)
		{
			return SpawnStencil(
					ownerScene,
					boxStencilInfo.stencil_id,
					boxStencilInfo.stencil_name,
					boxStencilInfo.relative_transform);
		}

		public static MikanStencil SpawnModelStencil(MikanScene ownerScene, MikanStencilModel modelStencilInfo)
		{
			return SpawnStencil(
					ownerScene,
					modelStencilInfo.stencil_id,
					modelStencilInfo.stencil_name,
					modelStencilInfo.relative_transform);
		}

		private static MikanStencil SpawnStencil(
			MikanScene ownerScene, 
			MikanStencilID stencilId,
			string stencilName,
			MikanTransform relativeTransform)
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
			stencilGO.name = stencilName;
			stencilGO.transform.SetParent(ownerScene.transform, false);

			var stencil = stencilGO.GetComponent<MikanStencil>();
			if (stencil == null)
			{
				stencil = stencilGO.AddComponent<MikanStencil>();
			}

			stencil.Setup(ownerScene, stencilId, stencilName, relativeTransform);

			return stencil;
		}

		public static void DespawnAnchor(MikanStencil stencil)
		{
			if (stencil != null)
			{
				stencil.Teardown();
				Destroy(stencil.gameObject);
			}
		}

		private void Setup(
			MikanScene ownerScene,
			MikanStencilID stencilId,
			string stencilName,
			MikanTransform relativeTransform)
		{
			OwnerScene = ownerScene;

			_stencilId = stencilId;
			_stencilName = stencilName;

			// Update our transform in Mikan Space
			_mikanSpaceTransform.ApplyMikanTransform(relativeTransform);

			// Update the Unity transform
			UpdateSceneTransform();
		}

		private void Teardown()
		{
		}


		public void ApplyQuadStencilInfo(MikanStencilQuad mikanStencilQuad)
		{
			// Update the stencil transform from the event
			_mikanSpaceTransform.ApplyMikanTransform(mikanStencilQuad.relative_transform);

			UpdateSceneTransform();
		}

		public void ApplyBoxStencilInfo(MikanStencilBox mikanStencilBox)
		{
			// Update the stencil transform from the event
			_mikanSpaceTransform.ApplyMikanTransform(mikanStencilBox.relative_transform);

			UpdateSceneTransform();
		}

		public void ApplyModelStencilInfo(MikanStencilModel mikanStencilModel)
		{
			// Update the stencil transform from the event
			_mikanSpaceTransform.ApplyMikanTransform(mikanStencilModel.relative_transform);

			UpdateSceneTransform();
		}

		public void HandleStencilPoseChanged(MikanStencilPoseUpdateEvent stencilPoseEvent)
		{
			// Update the stencil transform from the event
			MikanSpaceTransform.ApplyMikanTransform(stencilPoseEvent.transform);

			UpdateSceneTransform();
		}

		private void UpdateSceneTransform()
		{
			// Get the anchor transform in Mikan Space
			Matrix4x4 SceneSpaceTransform = _mikanSpaceTransform.MikanSpaceTRSMat;

			// Update the relative transform of the anchor
			transform.localPosition = MikanMath.ExtractTranslationFromMatrix(SceneSpaceTransform);
			transform.localRotation = MikanMath.ExtractRotationFromMatrix(SceneSpaceTransform);
			transform.localScale = MikanMath.ExtractScaleFromMatrix(SceneSpaceTransform);
		}
	}
}
