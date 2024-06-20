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

		private MikanSettingsWindow _settingsWindow;

		private bool _isMikanInitialized = false;

		private MikanSettings _settings = null;
		public MikanSettings Settings => _settings;

		private MikanManager _mikanManager = null;
		public MikanManager Manager => _mikanManager;

		private MikanClient _mikanClient = null;
		public MikanClient Client => _mikanClient;

		private MikanLogger _logger = null;
		public MikanLogger Logger => _logger;

		public void Awake()
		{
			// Initialize the settings
			_settings = new MikanSettings();
			_settings.LoadSettings();

			// Setup MikanManager, Client and Logger
			SetupMikan();

			// Create the Settings UI
			SetupVNyanUI();
		}

		public void OnDestroy()
		{
			TearDownMikan();
		}

		public void OnApplicationQuit()
		{
			TearDownMikan();

			if (_settings != null)
			{
				_settings.SaveSettings();
			}
		}

		private void SetupMikan()
		{
			if (!_isMikanInitialized)
			{
				// Create the MikanManager
				_mikanManager = gameObject.AddComponent<MikanManager>();
				_mikanManager.Setup(this);

				// Cache the MikanClient that the MikanManager created
				_mikanClient = _mikanManager.MikanClient;

				// Register the logger callback
				_logger = _mikanClient.ClientAPI.CoreAPI.Logger;
				_logger.AddLogCallback(Log);

				_isMikanInitialized = true;
			}
		}

		private void TearDownMikan()
		{
			if (_isMikanInitialized)
			{
				if (_mikanManager != null)
				{
					_mikanManager.TearDown();
				}

				if (_logger != null)
				{
					_logger.RemoveLogCallback(Log);
					_logger = null;
				}

				_mikanClient = null;
				_isMikanInitialized = false;
			}
		}

		public void SetupVNyanUI()
		{
			GameObject settingsWindowGO;

			if (VNyanInterface.VNyanInterface.VNyanUI != null)
			{
				// Register button to plugins window
				_logger.Log(MikanLogLevel.Info, "Registering plugin button");
				VNyanInterface.VNyanInterface.VNyanUI.registerPluginButton("MikanXR", this);

				// Create a window that will show when the button in plugins window is clicked
				_logger.Log(MikanLogLevel.Info, "Instantiating Window Prefab");
				settingsWindowGO = (GameObject)VNyanInterface.VNyanInterface.VNyanUI.instantiateUIPrefab(windowPrefab);
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
				settingsWindowGO = GameObject.Instantiate(windowPrefab);
				settingsWindowGO.transform.SetParent(canvasObject.transform);
			}

			if (settingsWindowGO != null)
			{
				_settingsWindow = settingsWindowGO.GetComponent<MikanSettingsWindow>();
				_settingsWindow.Setup(this);

				// Hide the window by default when spawned in vnyan
				if (VNyanInterface.VNyanInterface.VNyanUI != null)
				{
					_settingsWindow.HideWindow();
				}
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
			// Also route logs to the settings window
			if (_settingsWindow != null)
			{
				_settingsWindow.AddLogMessage(message);
			}

			Debug.Log(message);
		}

		// -- VNyanInterface.IButtonClickedHandler
		public void pluginButtonClicked()
		{
			if (_settingsWindow != null)
			{
				_settingsWindow.ToggleWindow();
			}
		}
	}
}
