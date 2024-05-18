using MikanCore;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MikanXRPlugin
{
	public class MikanManager : MonoBehaviour
	{
		public static MikanManager Instance => _instance;
		private static MikanManager _instance = null;

		private MikanClient _mikanClient = null;
		public MikanClient MikanClient => _mikanClient;

		private MikanLogger _logger = null;

		private GameObject _mikanSceneObject = null;
		private MikanScene _mikanScene = null;
		public MikanScene MikanScene => _mikanScene;

		public void Awake()
		{
			// For this particular MonoBehaviour, we only want one instance to exist at any time, so store a reference to it in a static property
			//   and destroy any that are created while one already exists.
			if (_instance != null)
			{
				DestroyImmediate(this);
				return;
			}
			_instance = this;
			DontDestroyOnLoad(this); // Don't destroy this object on scene changes

			// Create a mikan client sibling component to manage client API
			_mikanClient = this.gameObject.AddComponent<MikanClient>();

			// Cache a reference to the logger
			_logger= _mikanClient.ClientAPI.CoreAPI.Logger;

			// Listen for when the scene changes
			SceneManager.activeSceneChanged += OnSceneChanged;

			// If there is an active scene, spawn the Mikan Scene
			if (SceneManager.GetActiveScene().isLoaded)
			{
				SpawnMikanScene();
			}
		}

		public void OnEnable()
		{
			_logger.Log(MikanLogLevel.Info, $"MikanManager: OnEnable Called");
			_mikanClient.InitClient();
		}

		public void OnDisable()
		{
			_logger.Log(MikanLogLevel.Info, $"MikanManager: OnDisable Called");
			_mikanClient.DisposeClient();
		}

		public void OnApplicationQuit()
		{
			_logger.Log(MikanLogLevel.Info, $"MikanManager: OnApplicationQuit Called");
			_mikanClient.DisposeClient();
		}

		private void OnSceneChanged(Scene oldScene, Scene newScene)
		{
			_logger.Log(MikanLogLevel.Info,
				$"MikanManager: Active scene changed: {oldScene.name} -> {newScene.name}");

			DespawnMikanScene();
			SpawnMikanScene();
		}

		void DespawnMikanScene()
		{
			_logger.Log(MikanLogLevel.Info, "MikanManager: Unloading MikanXR Scene");

			if (_mikanSceneObject != null)
			{
				UnbindMikanScene(_mikanScene);
				Destroy(_mikanSceneObject);
				_mikanSceneObject = null;
			}
		}

		void SpawnMikanScene()
		{
			if (_mikanSceneObject == null)
			{
				_logger.Log(MikanLogLevel.Warning, "MikanManager: Spawning Mikan XR Scene");
				_mikanSceneObject = new GameObject(
					"MikanSceneObject",
					new System.Type[] {
						typeof(MikanScene)
					});

				MikanScene sceneComponent = _mikanSceneObject.GetComponent<MikanScene>();

				// Bind the scene to the MikanManager
				BindMikanScene(sceneComponent);
			}
			else
			{
				_logger.Log(MikanLogLevel.Info, "MikanXRBeatSaberController: Ignoring Spawn Mikan XR Scene request. Already spawned");
			}
		}

		public void BindMikanScene(MikanScene InScene)
		{
			_mikanScene = InScene;
			_logger.Log(MikanLogLevel.Info, $"MikanManager: Binding Mikan Scene");
		}

		public void UnbindMikanScene(MikanScene InScene)
		{
			if (_mikanScene == InScene)
			{
				_logger.Log(MikanLogLevel.Info, $"MikanManager: Unbinding Mikan Scene");
				_mikanScene = InScene;
			}
			else
			{
				_logger.Log(MikanLogLevel.Warning, $"MikanManager: Trying to unbind incorrect scene.");
			}
		}
	}
}