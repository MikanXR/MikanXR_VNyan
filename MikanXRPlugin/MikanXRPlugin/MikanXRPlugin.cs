using MikanXR;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MikanXRPlugin
{
    public class MikanXRPlugin : MonoBehaviour, VNyanInterface.IButtonClickedHandler, IMikanLogger
    {
		public Shader rgbPackShader;

		public GameObject windowPrefab;
        private GameObject window;
		private Text statusText;
		private Text logText;

		public MikanClient Client => MikanManager.Instance.MikanClient;
		public MikanAPI ClientAPI => Client.ClientAPI;

		// Settings
		private string _sceneOriginAnchorName = "";

        public void Awake()
        {
			// Set the depth pack shader for the MikanCamera
			MikanCamera.rgbDepthPackShader = rgbPackShader;

            // Create the MikanManager
			MikanManager.ExternalLogger = this;
			gameObject.AddComponent<MikanManager>();

			SetupWindow();
			LoadPluginSettings();
		}

		public void OnDestroy()
        {
            if (MikanManager.Instance != null)
            {
				MikanManager.Instance.MikanClient.DisposeClient();
			}
		}

		private void SetupWindow()
		{
			if (VNyanInterface.VNyanInterface.VNyanUI != null)
			{
				// Register button to plugins window
				Log(MikanLogLevel.Info, "Registering plugin button");
				VNyanInterface.VNyanInterface.VNyanUI.registerPluginButton("MikanXR", this);

				// Create a window that will show when the button in plugins window is clicked
				Log(MikanLogLevel.Info, "Instantiating Window Prefab");
				window = (GameObject)VNyanInterface.VNyanInterface.VNyanUI.instantiateUIPrefab(windowPrefab);
			}
			else
			{
				Log(MikanLogLevel.Info, "Create Test UI");
				var testUIObject = new GameObject("Test UI");
				new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

				Log(MikanLogLevel.Info, "Instantiating Test Canvas");
				var canvasObject = new GameObject("Test Canvas");
				
				Canvas myCanvas = canvasObject.AddComponent<Canvas>();
				myCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

				canvasObject.AddComponent<CanvasScaler>();
				canvasObject.AddComponent<GraphicRaycaster>();
				canvasObject.transform.SetParent(testUIObject.transform);

				Log(MikanLogLevel.Info, "(Test Mode) Instantiating Window Prefab");
				window = GameObject.Instantiate(windowPrefab);
				window.transform.SetParent(canvasObject.transform);
			}

			if (window != null)
			{
				BindWindowComponents();

				// Hide the window by default when spawned in vnyan
				if (VNyanInterface.VNyanInterface.VNyanUI != null)
				{
					window.SetActive(false);
				}
			}
		}

		private void BindWindowComponents()
		{
			window.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);

			InputField SceneOriginAnchorNameInput =
				window.transform.Find("Panel/SceneControls/SceneOriginField/Text").GetComponent<InputField>();
			SceneOriginAnchorNameInput?.onValueChanged.AddListener((v) =>
			{
				_sceneOriginAnchorName = v;
			});
			SceneOriginAnchorNameInput?.SetTextWithoutNotify(_sceneOriginAnchorName);

			statusText = window.transform.Find("Panel/StatusControls/Status Indicator").GetComponent<Text>();
			logText = window.transform.Find("Panel/StatusControls/Scroll View/Viewport/Content").GetComponent<Text>();
			window.transform.Find("Panel/TitleBar/CloseButton").GetComponent<Button>().onClick.AddListener(
				() =>
				{
					ClosePluginWindow();
				});

			SetStatusMessage("Disconnected");

			Client.OnConnected.AddListener(() =>
			{
				statusText.text = "Connected";
			});

			Client.OnDisconnected.AddListener(() =>
			{
				statusText.text = "Disconnected";
			});
		}

		private void LoadPluginSettings()
        {
			if (VNyanInterface.VNyanInterface.VNyanSettings != null)
			{
				Dictionary<string, string> settings = 
					VNyanInterface.VNyanInterface.VNyanSettings.loadSettings("MikanXRPlugin.cfg");

				if (settings != null)
				{
					settings.TryGetValue("sceneOriginAnchorName", out _sceneOriginAnchorName);
				}
			}
        }

        public void OnApplicationQuit()
        {
            SavePluginSettings();
        }

        private void SavePluginSettings()
        {
			if (VNyanInterface.VNyanInterface.VNyanSettings != null)
			{
				Dictionary<string, string> settings = new Dictionary<string, string>();
				settings["sceneOriginAnchorName"] = _sceneOriginAnchorName;

				VNyanInterface.VNyanInterface.VNyanSettings.saveSettings("MikanXRPlugin.cfg", settings);
			}
		}

		private void ClosePluginWindow()
		{
			if (window != null)
			{
				window.SetActive(false);
			}
		}

		private void SetStatusMessage(string message)
		{
			if (statusText != null)
			{
				statusText.text = message;
			}
		}

		private void AddLogMessage(string message)
		{
			if (logText != null)
			{
				logText.text += message + "\n";
			}

			Debug.Log(message);
		}

		// -- VNyanInterface.IButtonClickedHandler
		public void pluginButtonClicked()
		{
			// Flip the visibility of the window when plugin window button is clicked
			if (window != null)
			{
				window.SetActive(!window.activeSelf);
				if (window.activeSelf)
					window.transform.SetAsLastSibling();
			}
		}

		// -- IMikanLogger
		public void Log(MikanLogLevel mikanLogLevel, string log_message)
		{
			switch (mikanLogLevel)
			{
			case MikanLogLevel.Trace:
				AddLogMessage($"Trace | {log_message}");
				break;
			case MikanLogLevel.Debug:
				AddLogMessage($"DEBUG | {log_message}");
				break;
			case MikanLogLevel.Info:
				AddLogMessage($"INFO | {log_message}");
				break;
			case MikanLogLevel.Warning:
				AddLogMessage($"WARNING | {log_message}");
				break;
			case MikanLogLevel.Error:
				AddLogMessage($"ERROR | {log_message}");
				break;
			case MikanLogLevel.Fatal:
				AddLogMessage($"FATAL | {log_message}");
				break;
			}
		}
	}
}
