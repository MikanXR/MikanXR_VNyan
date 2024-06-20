using MikanXR;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace MikanXRPlugin
{
	public class MikanSettings
	{
		private static readonly string k_pluginSettingsFileName = "MikanXRPlugin.cfg";

		public UnityEvent<float> OnCameraPositionScaleChanged = new UnityEvent<float>();
		private float _cameraPositionScale = 1.0f;
		public float CameraPositionScale
		{
			get
			{
				return _cameraPositionScale;
			}
			set
			{
				if (value != _cameraPositionScale)
				{
					_cameraPositionScale = Math.Max(value, 0.001f);
					OnCameraPositionScaleChanged.Invoke(_cameraPositionScale);
				}
			}
		}

		public void LoadSettings()
		{
			if (VNyanInterface.VNyanInterface.VNyanSettings != null)
			{
				Dictionary<string, string> settings =
					VNyanInterface.VNyanInterface.VNyanSettings.loadSettings(k_pluginSettingsFileName);

				if (settings != null)
				{
					_cameraPositionScale= DeserializeFloat(settings, "cameraPositionScale", 1f);
				}
			}
		}

		public void SaveSettings()
		{
			if (VNyanInterface.VNyanInterface.VNyanSettings != null)
			{
				Dictionary<string, string> settings = new Dictionary<string, string>();

				SerializeFloat(settings, "cameraPositionScale", _cameraPositionScale);

				VNyanInterface.VNyanInterface.VNyanSettings.saveSettings(k_pluginSettingsFileName, settings);
			}
		}

		private float DeserializeFloat(Dictionary<string, string> settings, string key, float defaultValue)
		{
			if (settings.TryGetValue(key, out string value))
			{
				if (float.TryParse(value, out float result))
				{
					return result;
				}
			}

			return defaultValue;
		}

		private void SerializeFloat(Dictionary<string, string> settings, string key, float value)
		{
			settings[key] = value.ToString();
		}
	}
}
