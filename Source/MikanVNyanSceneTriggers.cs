using MikanXR.Unity;
using UnityEngine;
using VNyanInterface;

namespace MikanXR.VNyan
{
	// Tells VNyan's node graphs when the editor's current scene changes, so a graph can react
	// per scene: load an avatar position slot, swap props, run a transition.
	//
	// Each change fires two triggers: MikanSceneChanged (value1 the scene id, text1 the scene
	// name) for a graph that branches on the name, and MikanScene_<scene name> so a scene can
	// have a trigger node of its own wired straight to what it does. The MikanSceneId and
	// MikanSceneName parameters hold the current scene for a graph that reads it outside the
	// event. The first announcement comes once the client connects and learns the scene.
	//
	// The scene id arrives before the scene's components do (the data store lists each system
	// separately), so an announcement waits for the scene's values for a bounded time.
	public class MikanVNyanSceneTriggers : MonoBehaviour
	{
		public const string SceneChangedTrigger = "MikanSceneChanged";
		public const string SceneTriggerPrefix = "MikanScene_";
		public const string SceneIdParameter = "MikanSceneId";
		public const string SceneNameParameter = "MikanSceneName";

		private const int k_invalidId = (int)MikanConstants.InvalidMikanID;
		// How long an announcement waits for the scene's values before going out without a name
		private const float k_pendingTimeoutSeconds = 5f;

		private MikanClient _client;
		private bool _announcePending;
		private float _announceDeadline;

		public void Bind(MikanClient client)
		{
			_client = client;
			_client.Disconnected += HandleDisconnected;
			_client.DataStore.CurrentSceneChanged += HandleCurrentSceneChanged;
		}

		private void OnDestroy()
		{
			if (_client == null)
			{
				return;
			}

			_client.Disconnected -= HandleDisconnected;
			if (_client.DataStore != null)
			{
				_client.DataStore.CurrentSceneChanged -= HandleCurrentSceneChanged;
			}
		}

		private void HandleDisconnected()
		{
			_announcePending = false;
		}

		private void HandleCurrentSceneChanged(int sceneId)
		{
			_announcePending = true;
			_announceDeadline = Time.unscaledTime + k_pendingTimeoutSeconds;
		}

		private void Update()
		{
			if (!_announcePending || _client == null || !_client.IsClientInitialized)
			{
				return;
			}

			int sceneId = _client.DataStore.CurrentSceneId;
			string sceneName = "";

			if (sceneId != k_invalidId)
			{
				MikanSceneComponentValues sceneValues =
					_client.DataStore.GetValues<MikanSceneComponentValues>(MikanSystemNames.SceneSystem, sceneId);
				if (sceneValues == null && Time.unscaledTime < _announceDeadline)
				{
					return;
				}

				sceneName = sceneValues?.component_name ?? "";
			}

			_announcePending = false;

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
