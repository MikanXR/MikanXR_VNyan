using MikanXR.Unity;
using UnityEngine;
using VNyanInterface;

namespace MikanXR.VNyan
{
	// Tells VNyan's node graphs when the editor's current scene changes, so a graph can react
	// per scene: load an avatar position slot, swap props, run a transition.
	//
	// Each announcement from the package's MikanSceneAnnouncer fires two triggers:
	// MikanSceneChanged (value1 the scene id, text1 the scene name) for a graph that branches on
	// the name, and MikanScene_<scene name> so a scene can have a trigger node of its own wired
	// straight to what it does. The MikanSceneId and MikanSceneName parameters hold the current
	// scene for a graph that reads it outside the event.
	public class MikanVNyanSceneTriggers : MonoBehaviour
	{
		public const string SceneChangedTrigger = "MikanSceneChanged";
		public const string SceneTriggerPrefix = "MikanScene_";
		public const string SceneIdParameter = "MikanSceneId";
		public const string SceneNameParameter = "MikanSceneName";

		private MikanSceneAnnouncer _announcer;

		public void Bind(MikanClient client)
		{
			_announcer = gameObject.AddComponent<MikanSceneAnnouncer>();
			_announcer.Bind(client);
			_announcer.SceneAnnounced += HandleSceneAnnounced;
		}

		private void OnDestroy()
		{
			if (_announcer != null)
			{
				_announcer.SceneAnnounced -= HandleSceneAnnounced;
			}
		}

		private void HandleSceneAnnounced(int sceneId, string sceneName)
		{
			IParameterInterface parameters = VNyanInterface.VNyanInterface.VNyanParameter;
			parameters.setVNyanParameterFloat(SceneIdParameter, sceneId);
			parameters.setVNyanParameterString(SceneNameParameter, sceneName);

			ITriggerInterface triggers = VNyanInterface.VNyanInterface.VNyanTrigger;
			triggers.callTrigger(SceneChangedTrigger, sceneId, 0, 0, sceneName, "", "");
			if (!string.IsNullOrEmpty(sceneName))
			{
				triggers.callTrigger(SceneTriggerPrefix + sceneName, sceneId, 0, 0, sceneName, "", "");
			}

			Debug.Log($"[MikanXR] scene changed to '{sceneName}' ({sceneId})");
		}
	}
}
