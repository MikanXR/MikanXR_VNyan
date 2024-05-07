using MikanXR;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MikanXRPlugin
{
    public class MikanXRPlugin : MikanBehavior, VNyanInterface.IButtonClickedHandler
    {
		public GameObject windowPrefab;

        private GameObject window;

		public GameObject MikanSceneObject { get; private set; }
		public GameObject MikanCameraObject { get; private set; }

		// Settings
		private string sceneOriginAnchorName = "";
        private float someValue2 = 5.0f;

        public void Awake()
        {
            // Register button to plugins window
            VNyanInterface.VNyanInterface.VNyanUI.registerPluginButton("MikanXR", this);

            // Create a window that will show when the button in plugins window is clicked
            window = (GameObject)VNyanInterface.VNyanInterface.VNyanUI.instantiateUIPrefab(windowPrefab);

            // Load settings
            loadPluginSettings();

            // Hide the window by default
            if (window != null)
            {
                window.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);  
                window.SetActive(false);

                // Set ui component callbacks and loaded values
                window.GetComponentInChildren<Slider>()?.onValueChanged.AddListener((v) => { someValue2 = v; });
                window.GetComponentInChildren<Slider>()?.SetValueWithoutNotify(someValue2);

                window.GetComponentInChildren<InputField>()?.onValueChanged.AddListener((v) => { sceneOriginAnchorName = v; });
                window.GetComponentInChildren<InputField>()?.SetTextWithoutNotify(sceneOriginAnchorName);
            }

            // Create the MikanManager
            {
				GameObject gameObject = new GameObject("MikanXRManager");
				var mikanManager= gameObject.AddComponent<MikanManager>();
                var loggerPlugin= gameObject.AddComponent<MikanLogger_Unity>();

                mikanManager.SetLogHandler(loggerPlugin);
			}

            // Listen to scene changes
			SceneManager.activeSceneChanged += OnActiveSceneChanged;
		}

        public void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;

            if (MikanManager.Instance != null)
            {
				GameObject.Destroy(MikanManager.Instance.gameObject);
			}
		}

        private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            Log(MikanLogLevel.Info, 
                $"MikanXRBeatSaberController: Active scene changed: {oldScene.name} -> {newScene.name}");

			DespawnMikanScene();
			//UnbindSceneComponents();
            //BindSceneComponents();
            SpawnMikanScene(newScene);
		}

        private void DespawnMikanScene()
        {
			DespawnMikanCamera();

			if (MikanSceneObject != null)
			{
				Destroy(MikanSceneObject);
				MikanSceneObject = null;
			}
		}

        private void SpawnMikanScene(Scene scene)
        {
			if (MikanSceneObject == null)
			{
				Log(MikanLogLevel.Info, "MikanXRBeatSaberController: Spawning Mikan XR Scene");

				MikanSceneObject = new GameObject(
					"MikanSceneObject",
					new System.Type[] {
						typeof(MikanScene),
						typeof(MikanSceneEventHandler) });

				MikanScene sceneComponent = MikanSceneObject.GetComponent<MikanScene>();
				sceneComponent.SceneOriginAnchorName = sceneOriginAnchorName;

				//Log(MikanLogLevel.Info, $"MikanXRBeatSaberController: Attaching to room origin");

				//MikanSceneObject.transform.position = roomCenter;
				//MikanSceneObject.transform.rotation = roomRotation;

				// Create and attach the MikanCamera to the Mikan Scene
				SpawnMikanCamera(MikanSceneObject);

				// Tell the scene to look for the new camera we spawned
				sceneComponent.BindSceneCamera();

				// If we are already connected to Mikan,
				// tell the scene to fetch anchors, setup scene transform, etc
				// Otherwise wait for MikanManager to tell the scene about the connection
				if (MikanManager.Instance.ClientAPI.GetIsConnected())
				{
					sceneComponent.HandleMikanConnected();
				}
			}
			else
			{
				Log(MikanLogLevel.Info, 
					"MikanXRBeatSaberController: Ignoring Spawn Mikan XR Scene request. Already spawned");
			}
		}

		public void SpawnMikanCamera(GameObject mikanSceneObject)
		{
			if (MikanCameraObject == null)
			{
				Log(MikanLogLevel.Info, $"MikanXRBeatSaberController: Spawning Mikan camera");

				MikanCameraObject = new GameObject(
					"MikanCameraObject",
					new System.Type[] {
						typeof(Camera),
						typeof(MikanCamera)});

				Camera cameraComponent = MikanCameraObject.GetComponent<Camera>();
				cameraComponent.stereoTargetEye = StereoTargetEyeMask.None;
				cameraComponent.backgroundColor = new Color(0, 0, 0, 0);
				cameraComponent.clearFlags = CameraClearFlags.SolidColor;
				cameraComponent.forceIntoRenderTexture = true;
				cameraComponent.transform.parent = mikanSceneObject.transform;

				MikanCamera captureComponent = MikanCameraObject.GetComponent<MikanCamera>();

				// Tell the MikanCamera to look for a sibling Camera component
				captureComponent.BindXRCamera();

				// If we are connected to Mikan, 
				// copy the camera intrinsics (fov, pixel dimensions, etc) to the Unity Camera
				// If not, wait for Mikan to connect and we'll copy the settings then
				captureComponent.HandleCameraIntrinsicsChanged();

				// If we have a valid render target descriptor (defined video source),
				// create a valid render target from the descriptor
				// If not, wait for Mikan to connect and we'll create it then
				if (MikanManager.Instance.IsRenderTargetDescriptorValid)
				{
					captureComponent.RecreateRenderTarget(MikanManager.Instance.RenderTargetDescriptor);
				}
				else
				{
					Log(MikanLogLevel.Warning, 
						"MikanXRBeatSaberController: No valid render target descriptor. Not connected to Mikan yet?");
				}
			}
			else
			{
				Log(MikanLogLevel.Info, "MikanXRBeatSaberController: Ignoring camera spawn request. Already spawned.");
			}
		}

		public void DespawnMikanCamera()
		{
			if (MikanCameraObject != null)
			{
				Log(MikanLogLevel.Info, "MikanXRBeatSaberController: Despawn Mikan camera");
				Destroy(MikanCameraObject);
				MikanCameraObject = null;
			}
			else
			{
				Log(MikanLogLevel.Info, "MikanXRBeatSaberController: Ignoring camera de-spawn request. Already despawned.");
			}
		}

		/// <summary>
		/// Load plugin settings
		/// </summary>
		private void loadPluginSettings()
        {
            // Get settings in dictionary
            Dictionary<string, string> settings = VNyanInterface.VNyanInterface.VNyanSettings.loadSettings("MikanXRPlugin.cfg");
            if (settings != null)
            {
                // Read string value
                settings.TryGetValue("sceneOriginAnchorName", out sceneOriginAnchorName);

                // Convert second value to decimal
                if (settings.TryGetValue("SomeValue2", out string s))
                {
                    float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out someValue2);
                }

            }
        }

        /// <summary>
        /// Called when VNyan is shutting down
        /// </summary>
        private void OnApplicationQuit()
        {
            // Save settings
            savePluginSettings();
        }

        private void savePluginSettings()
        {
            Dictionary<string, string> settings = new Dictionary<string, string>();
            settings["sceneOriginAnchorName"] = sceneOriginAnchorName;
            settings["SomeValue2"] = someValue2.ToString(CultureInfo.InvariantCulture); // Make sure to use InvariantCulture to avoid decimal delimeter errors

            VNyanInterface.VNyanInterface.VNyanSettings.saveSettings("MikanXRPlugin.cfg", settings);
        }

        public void pluginButtonClicked()
        {
            // Flip the visibility of the window when plugin window button is clicked
            if (window != null)
            {
                window.SetActive(!window.activeSelf);
                if(window.activeSelf)
                    window.transform.SetAsLastSibling();
            }
                
        }
    }
}
