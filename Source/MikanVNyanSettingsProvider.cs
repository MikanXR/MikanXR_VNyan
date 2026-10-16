using System.Collections.Generic;
using MikanXR.Unity;
using UnityEngine;
using VNyanInterface;

namespace MikanXR.VNyan
{
	// The client's settings in VNyan's own store, which keeps a flat string dictionary per
	// plugin under the user's profile. The whole settings object rides in one JSON value.
	public class MikanVNyanSettingsProvider : IMikanSettingsProvider
	{
		public const string SettingsFileName = "MikanXRPlugin.cfg";
		private const string k_jsonKey = "settings_json";

		public bool Load(MikanSettings settings)
		{
			Dictionary<string, string> stored = VNyanInterface.VNyanInterface.VNyanSettings.loadSettings(SettingsFileName);
			if (stored == null || !stored.TryGetValue(k_jsonKey, out string json) || string.IsNullOrEmpty(json))
			{
				return false;
			}

			JsonUtility.FromJsonOverwrite(json, settings);
			return true;
		}

		public void Save(MikanSettings settings)
		{
			var stored = new Dictionary<string, string>()
			{
				{ k_jsonKey, JsonUtility.ToJson(settings) }
			};

			VNyanInterface.VNyanInterface.VNyanSettings.saveSettings(SettingsFileName, stored);
		}
	}
}
