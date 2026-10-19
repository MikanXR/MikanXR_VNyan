using System;
using System.IO;
using MikanXR.Unity;
using UnityEngine;

namespace MikanXR.VNyan
{
	// The plugin's root: creates the MikanClient object in VNyan's scene, points it at VNyan's
	// settings store and at the shader bundle beside the plugin, and opens the plugin window.
	public class MikanVNyanPlugin
	{
		public const string ClientName = "VNyan";
		// VNyan avatars are human sized and the editor's scenes are tabletop sized, so ten Unity
		// units per Mikan meter is the starting point; the window adjusts it and the setting persists
		public const float DefaultWorldScale = 10f;
		public const string DataFolderName = "MikanXR";
		public const string ShaderBundleName = "mikanxr_shaders.bundle";

		private static MikanVNyanPlugin s_instance;

		private GameObject _root;
		private MikanClient _client;
		private MikanVNyanFreeCamera _freeCamera;
		private MikanVNyanSceneTriggers _sceneTriggers;
		private MikanVNyanWindow _window;

		public static MikanVNyanPlugin Instance => s_instance;
		public MikanClient Client => _client;
		public GameObject Root => _root;
		public MikanVNyanFreeCamera FreeCamera => _freeCamera;

		public static void Start()
		{
			if (s_instance != null)
			{
				return;
			}

			s_instance = new MikanVNyanPlugin();
			s_instance.Initialize();
		}

		// The folder beside the plugin assembly that carries its data: the shader bundle today
		public static string DataFolder
		{
			get
			{
				string assemblyLocation = typeof(MikanVNyanPlugin).Assembly.Location;
				string assemblyFolder = string.IsNullOrEmpty(assemblyLocation)
					? Path.Combine(Application.dataPath, "..", "Items", "Assemblies")
					: Path.GetDirectoryName(assemblyLocation);

				return Path.Combine(assemblyFolder, DataFolderName);
			}
		}

		private void Initialize()
		{
			LoadShaderBundle();

			// The client reads its settings in Awake, so the object is built inactive, given its
			// provider, and activated once it is complete
			_root = new GameObject("MikanXR");
			_root.SetActive(false);
			UnityEngine.Object.DontDestroyOnLoad(_root);

			_client = _root.AddComponent<MikanClient>();
			_client.SettingsProvider = new MikanVNyanSettingsProvider();
			_client.SetClientName(ClientName);
			_client.SetWorldScale(DefaultWorldScale);

			_root.SetActive(true);

			_freeCamera = _root.AddComponent<MikanVNyanFreeCamera>();
			_sceneTriggers = _root.AddComponent<MikanVNyanSceneTriggers>();
			_sceneTriggers.Bind(_client);
			_window = new MikanVNyanWindow(this);
			_root.AddComponent<MikanVNyanTicker>().Window = _window;

			Debug.Log("[MikanXR] VNyan plugin started");
		}

		private void LoadShaderBundle()
		{
			string bundlePath = Path.Combine(DataFolder, ShaderBundleName);
			if (!File.Exists(bundlePath))
			{
				Debug.LogWarning($"[MikanXR] shader bundle not found at {bundlePath}; stencils and depth will not render");
				return;
			}

			try
			{
				AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
				if (bundle == null)
				{
					Debug.LogError($"[MikanXR] shader bundle failed to load from {bundlePath}");
					return;
				}

				MikanShaderLibrary.SetBundle(bundle);
				Debug.Log($"[MikanXR] shader bundle loaded from {bundlePath}");
			}
			catch (Exception e)
			{
				Debug.LogError($"[MikanXR] shader bundle failed to load: {e.Message}");
			}
		}
	}

	// Drives the window's live readouts from the scene's frame loop
	public class MikanVNyanTicker : MonoBehaviour
	{
		private const float k_interval = 0.5f;

		public MikanVNyanWindow Window;
		private float _nextTick;

		private void Update()
		{
			if (Window != null && Time.unscaledTime >= _nextTick)
			{
				_nextTick = Time.unscaledTime + k_interval;
				Window.Tick();
			}
		}
	}
}
