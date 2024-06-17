using MikanCore;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MikanXRPlugin
{
    public class MikanXRPlugin : MonoBehaviour, VNyanInterface.IButtonClickedHandler
    {
		public Shader rgbPackShader;

		public GameObject windowPrefab;
		public GameObject anchorPrefab;
		public GameObject stencilModelPrefab;

        private GameObject _window;
		private Text _statusText;
		private Text _logText;

		private MikanManager _mikanManager= null;
		private MikanClient _mikanClient= null;
		private MikanLogger _logger= null;

		// Settings
		private string _sceneOriginAnchorName = "";

        public void Awake()
        {
			// Set the depth pack shader for the MikanCamera
			MikanCamera.rgbDepthPackShader = rgbPackShader;

            // Create the MikanManager
			_mikanManager= gameObject.AddComponent<MikanManager>();
			_mikanManager.OwnerPlugin= this;
			_mikanClient= _mikanManager.MikanClient;

			// Register the logger callback
			_logger= _mikanClient.ClientAPI.CoreAPI.Logger;
			_logger.AddLogCallback(Log);

			SetupWindow();
			LoadPluginSettings();
		}

		public void OnDestroy()
        {
            if (_mikanManager != null)
            {
				MikanManager.Instance.MikanClient.DisposeClient();
			}
		}

		private void SetupWindow()
		{
			if (VNyanInterface.VNyanInterface.VNyanUI != null)
			{
				// Register button to plugins window
				_logger.Log(MikanLogLevel.Info, "Registering plugin button");
				VNyanInterface.VNyanInterface.VNyanUI.registerPluginButton("MikanXR", this);

				// Create a window that will show when the button in plugins window is clicked
				_logger.Log(MikanLogLevel.Info, "Instantiating Window Prefab");
				_window = (GameObject)VNyanInterface.VNyanInterface.VNyanUI.instantiateUIPrefab(windowPrefab);
			}
			else
			{
				_logger.Log(MikanLogLevel.Info, "Create Test UI");
				var testUIObject = new GameObject("Test UI");
				new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

				_logger.Log(MikanLogLevel.Info, "Instantiating Test Canvas");
				var canvasObject = new GameObject("Test Canvas");
				
				Canvas myCanvas = canvasObject.AddComponent<Canvas>();
				myCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

				canvasObject.AddComponent<CanvasScaler>();
				canvasObject.AddComponent<GraphicRaycaster>();
				canvasObject.transform.SetParent(testUIObject.transform);

				_logger.Log(MikanLogLevel.Info, "(Test Mode) Instantiating Window Prefab");
				_window = GameObject.Instantiate(windowPrefab);
				_window.transform.SetParent(canvasObject.transform);
			}

			if (_window != null)
			{
				BindWindowComponents();

				// Hide the window by default when spawned in vnyan
				if (VNyanInterface.VNyanInterface.VNyanUI != null)
				{
					_window.SetActive(false);
				}
			}
		}

		private void BindWindowComponents()
		{
			_window.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);

			InputField SceneOriginAnchorNameInput =
				_window.transform.Find("Panel/SceneControls/SceneOriginField/Text").GetComponent<InputField>();
			SceneOriginAnchorNameInput?.onValueChanged.AddListener((v) =>
			{
				_sceneOriginAnchorName = v;
			});
			SceneOriginAnchorNameInput?.SetTextWithoutNotify(_sceneOriginAnchorName);

			_statusText = _window.transform.Find("Panel/StatusControls/Status Indicator").GetComponent<Text>();
			_logText = _window.transform.Find("Panel/StatusControls/Scroll View/Viewport/Content").GetComponent<Text>();
			_window.transform.Find("Panel/TitleBar/CloseButton").GetComponent<Button>().onClick.AddListener(
				() =>
				{
					ClosePluginWindow();
				});

			SetStatusMessage("Disconnected");

			_mikanClient.OnConnected.AddListener(() =>
			{
				_statusText.text = "Connected";
			});

			_mikanClient.OnDisconnected.AddListener(() =>
			{
				_statusText.text = "Disconnected";
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
			if (_window != null)
			{
				_window.SetActive(false);
			}
		}

		private void SetStatusMessage(string message)
		{
			if (_statusText != null)
			{
				_statusText.text = message;
			}
		}

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

		private void AddLogMessage(string message)
		{
			if (_logText != null)
			{
				_logText.text += message + "\n";
			}

			Debug.Log(message);
		}

		// -- VNyanInterface.IButtonClickedHandler
		public void pluginButtonClicked()
		{
			// Flip the visibility of the window when plugin window button is clicked
			if (_window != null)
			{
				_window.SetActive(!_window.activeSelf);
				if (_window.activeSelf)
					_window.transform.SetAsLastSibling();
			}
		}		
	}
}
