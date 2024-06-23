using System;
using System.Collections.Generic;
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
			get { return _cameraPositionScale; }
			set
			{
				if (value != _cameraPositionScale)
				{
					_cameraPositionScale = Math.Max(value, 0.001f);
					OnCameraPositionScaleChanged.Invoke(_cameraPositionScale);
				}
			}
		}

		public UnityEvent<Vector3> OnScenePositionChanged = new UnityEvent<Vector3>();
		private Vector3 _scenePosition = Vector3.zero;
		public Vector3 ScenePosition
		{
			get { return _scenePosition; }
			set { 
				_scenePosition = value; 
				OnScenePositionChanged.Invoke(_scenePosition);
			}
		}

		public UnityEvent<Vector3> OnSceneOrientationChanged = new UnityEvent<Vector3>();
		private Vector3 _sceneEulerAngles = Vector3.zero;
		public Vector3 SceneEulerAngles
		{
			get { return _sceneEulerAngles; }
			set { 
				_sceneEulerAngles = value; 
				OnSceneOrientationChanged.Invoke(_sceneEulerAngles);
			}
		}

		public UnityEvent<float> OnSceneScaleChanged = new UnityEvent<float>();
		private float _sceneScale = 1.0f;
		public float SceneScale
		{
			get { return _sceneScale; }
			set { 
				_sceneScale = value; 
				OnSceneScaleChanged.Invoke(_sceneScale);
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
					_scenePosition = DeserializeVector3(settings, "scenePosition", Vector3.zero);
					_sceneEulerAngles = DeserializeVector3(settings, "sceneEulerAngles", Vector3.zero);
					_sceneScale = DeserializeFloat(settings, "sceneScale", 1f);
				}
			}
		}

		public void SaveSettings()
		{
			if (VNyanInterface.VNyanInterface.VNyanSettings != null)
			{
				Dictionary<string, string> settings = new Dictionary<string, string>();

				SerializeFloat(settings, "cameraPositionScale", _cameraPositionScale);
				SerializeVector3(settings, "scenePosition", _scenePosition);
				SerializeVector3(settings, "sceneEulerAngles", _sceneEulerAngles);
				SerializeFloat(settings, "sceneScale", _sceneScale);

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

		private Vector3 DeserializeVector3(Dictionary<string, string> settings, string key, Vector3 defaultValue)
		{
			if (settings.TryGetValue(key, out string value))
			{
				string[] stringValues = value.Split(',');

				if (stringValues.Length == 3)
				{
					if (float.TryParse(stringValues[0], out float x) &&
						float.TryParse(stringValues[1], out float y) &&
						float.TryParse(stringValues[2], out float z))
					{
						return new Vector3(x, y, z);
					}
				}
			}

			return defaultValue;
		}

		private void SerializeVector3(Dictionary<string, string> settings, string key, Vector3 value)
		{
			settings[key] = $"{value.x},{value.y},{value.z}";
		}
	}
}
