using MikanXR;
using UnityEngine;

namespace MikanXRPlugin
{
    /// The ID of a VR Device
    using MikanSpatialAnchorID = System.Int32;

    public class MikanAnchor : MikanBehavior
    {
        public MikanScene OwnerScene { get; private set; }

        private MikanSpatialAnchorID _anchorId = -1;
        public MikanSpatialAnchorID AnchorID => _anchorId;

        private MikanSpaceTransform _mikanSpaceTransform = new MikanSpaceTransform();
        public MikanSpaceTransform MikanSpaceTransform => _mikanSpaceTransform;

        [SerializeField]
        private string _anchorName = "";
        public string AnchorName
        {
            get
            {
                return _anchorName;
            }
            set
            {
                _anchorName = value;
            }
        }

        public static MikanAnchor SpawnAnchor(MikanScene ownerScene, MikanSpatialAnchorInfo anchorInfo)
        {
            var anchorPrefab= ownerScene.OwnerManager.OwnerPlugin.anchorPrefab;

            GameObject anchorGO;
            if (anchorPrefab != null)
            {
                anchorGO = Instantiate(anchorPrefab);
            }
            else
            {
				anchorGO = new GameObject();
			}
			anchorGO.name = anchorInfo.anchor_name;
            anchorGO.transform.SetParent(ownerScene.transform, false);

			var anchor = anchorGO.GetComponent<MikanAnchor>();
            if (anchor == null)
            {
				anchor = anchorGO.AddComponent<MikanAnchor>();
			}

            return anchor;
		}

        public void ApplyAnchorInfo(MikanSpatialAnchorInfo anchorInfo)
        {
            _anchorId = anchorInfo.anchor_id;
            _anchorName = anchorInfo.anchor_name;

            // Update our transform in Mikan Space
            _mikanSpaceTransform.ApplyMikanTransform(anchorInfo.world_transform);
        }

        public void UpdateSceneTransform()
        {
            if (OwnerScene != null)
            {
                // Get the anchor transform in Mikan Space
                Matrix4x4 MikanSpaceTransform = _mikanSpaceTransform.MikanSpaceTRSMat;

                // Get the conversion from the scene to go from Mikan to Scene space
                Matrix4x4 MikanToSceneXform = OwnerScene.MikanToSceneTransform;

                // Compute the scene space transform
                Matrix4x4 SceneSpaceTransform = MikanToSceneXform * MikanSpaceTransform;

                // Update the relative transform of the anchor
                transform.localPosition = MikanMath.ExtractTranslationFromMatrix(SceneSpaceTransform);
                transform.localRotation = MikanMath.ExtractRotationFromMatrix(SceneSpaceTransform);
                transform.localScale = MikanMath.ExtractScaleFromMatrix(SceneSpaceTransform);
            }
        }
    }
}
