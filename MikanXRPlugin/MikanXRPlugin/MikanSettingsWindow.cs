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

			InputField CameraPositionScaleInput =
				gameObject.transform.Find("Panel/SceneControls/CameraPositionScaleField").GetComponent<InputField>();
			CameraPositionScaleInput?.onValueChanged.AddListener((v) =>
			{
				if (float.TryParse(v, out float newScale))
				{
					_settings.CameraPositionScale = newScale;
				}
			});
			CameraPositionScaleInput?.SetTextWithoutNotify(_settings.CameraPositionScale.ToString());

			_statusText = gameObject.transform.Find("Panel/StatusControls/Status Indicator").GetComponent<Text>();
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
