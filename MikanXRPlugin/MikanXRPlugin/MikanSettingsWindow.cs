using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MikanXRPlugin
{
    class MikanSettingsWindow : MonoBehaviour, IDragHandler, IPointerDownHandler
    {
		private MikanXRPlugin _ownerPlugin;
		private MikanSettings _settings;

        public RectTransform dragRect;
		private Text _statusText;
		private Text _logText;

		public void Setup(MikanXRPlugin ownerPlugin)
		{
			_ownerPlugin = ownerPlugin;
			_settings = ownerPlugin.Settings;

			SetupUI();

			SetStatusMessage("Disconnected");

			BindMikanClientEvents(ownerPlugin.Client);
		}

		private void SetupUI()
		{
			gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);

			// Scene Position
			InputField SceneXPosInput =
				gameObject.transform.Find("Panel/SceneControls/ScenePosition/SceneXPosField").GetComponent<InputField>();
			InputField SceneYPosInput =
				gameObject.transform.Find("Panel/SceneControls/ScenePosition/SceneYPosField").GetComponent<InputField>();
			InputField SceneZPosInput =
				gameObject.transform.Find("Panel/SceneControls/ScenePosition/SceneZPosField").GetComponent<InputField>();
			SceneXPosInput?.onValueChanged.AddListener((v) =>
			{
				if (float.TryParse(v, out float newValue))
				{
					var newPos = _settings.ScenePosition;
					newPos.x = newValue;
					_settings.ScenePosition = newPos;
				}
			});
			SceneYPosInput?.onValueChanged.AddListener((v) =>
			{
				if (float.TryParse(v, out float newValue))
				{
					var newPos = _settings.ScenePosition;
					newPos.y = newValue;
					_settings.ScenePosition = newPos;
				}
			});
			SceneZPosInput?.onValueChanged.AddListener((v) =>
			{
				if (float.TryParse(v, out float newValue))
				{
					var newPos = _settings.ScenePosition;
					newPos.z = newValue;
					_settings.ScenePosition = newPos;
				}
			});
			SceneXPosInput?.SetTextWithoutNotify(_settings.ScenePosition.x.ToString());
			SceneYPosInput?.SetTextWithoutNotify(_settings.ScenePosition.y.ToString());
			SceneZPosInput?.SetTextWithoutNotify(_settings.ScenePosition.z.ToString());

			// Scene Orientation
			InputField SceneXAngleInput =
				gameObject.transform.Find("Panel/SceneControls/SceneRotation/SceneXAngleField").GetComponent<InputField>();
			InputField SceneYAngleInput =
				gameObject.transform.Find("Panel/SceneControls/SceneRotation/SceneYAngleField").GetComponent<InputField>();
			InputField SceneZAngleInput =
				gameObject.transform.Find("Panel/SceneControls/SceneRotation/SceneZAngleField").GetComponent<InputField>();
			SceneXAngleInput?.onValueChanged.AddListener((v) =>
			{
				if (float.TryParse(v, out float newValue))
				{
					var newAngle = _settings.SceneEulerAngles;
					newAngle.x = newValue;
					_settings.SceneEulerAngles = newAngle;
				}
			});
			SceneYAngleInput?.onValueChanged.AddListener((v) =>
			{
				if (float.TryParse(v, out float newValue))
				{
					var newAngle = _settings.SceneEulerAngles;
					newAngle.y = newValue;
					_settings.SceneEulerAngles = newAngle;
				}
			});
			SceneZAngleInput?.onValueChanged.AddListener((v) =>
			{
				if (float.TryParse(v, out float newValue))
				{
					var newAngle = _settings.SceneEulerAngles;
					newAngle.z = newValue;
					_settings.SceneEulerAngles = newAngle;
				}
			});
			SceneXAngleInput?.SetTextWithoutNotify(_settings.SceneEulerAngles.x.ToString());
			SceneYAngleInput?.SetTextWithoutNotify(_settings.SceneEulerAngles.y.ToString());
			SceneZAngleInput?.SetTextWithoutNotify(_settings.SceneEulerAngles.z.ToString());

			// Scene Scale
			InputField SceneScaleInput =
				gameObject.transform.Find("Panel/SceneControls/SceneScale/SceneScaleField").GetComponent<InputField>();
			SceneScaleInput?.onValueChanged.AddListener((v) =>
			{
				if (float.TryParse(v, out float newScale))
				{
					_settings.SceneScale = newScale;
				}
			});
			SceneScaleInput?.SetTextWithoutNotify(_settings.SceneScale.ToString());

			// Connection Status
			_statusText = gameObject.transform.Find("Panel/StatusControls/ConnectionStatus/Status Indicator").GetComponent<Text>();
			_logText = gameObject.transform.Find("Panel/StatusControls/Scroll View/Viewport/Content").GetComponent<Text>();

			gameObject.transform.Find("Panel/TitleBar/CloseButton").GetComponent<Button>().onClick.AddListener(
				() =>
				{
					HideWindow();
				});
		}

		private void SetStatusMessage(string message)
		{
			if (_statusText != null)
			{
				_statusText.text = message;
			}
		}

		public bool IsWindowVisible()
		{
			return gameObject.activeSelf;
		}

		public void ShowWindow()
		{
			if (!IsWindowVisible())
			{
				gameObject.SetActive(true);
				gameObject.transform.SetAsLastSibling();
			}
		}

		public void HideWindow()
		{
			gameObject.SetActive(false);
		}

		public void ToggleWindow()
		{
			if (IsWindowVisible())
			{
				HideWindow();
			}
			else
			{
				ShowWindow();
			}
		}

		public void AddLogMessage(string message)
		{
			if (_logText != null)
			{
				_logText.text += message + "\n";
			}
		}

		private void BindMikanClientEvents(MikanClient client)
		{
			client.OnConnected.AddListener(() =>
			{
				_statusText.text = "Connected";
			});

			client.OnDisconnected.AddListener(() =>
			{
				_statusText.text = "Disconnected";
			});
		}

		public void OnDrag(PointerEventData eventData)
        {
            if (dragRect != null)
            {
				dragRect.anchoredPosition += eventData.delta;
			}
		}

        public void OnPointerDown(PointerEventData eventData)
        {
            if (dragRect != null)
            {
				dragRect.SetAsLastSibling();
			}
		}
	}
}
