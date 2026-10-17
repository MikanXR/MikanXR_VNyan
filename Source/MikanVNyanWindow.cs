using System.Collections.Generic;
using System.Globalization;
using MikanXR.Unity;
using UnityEngine;
using VNyanInterface;

namespace MikanXR.VNyan
{
	// The plugin's window in VNyan's UI: the connection, what to render, where the Mikan root and
	// each stage sit in the VNyan scene. Built from VNyan's own widgets, so it needs no prefab.
	public class MikanVNyanWindow : IButtonClickedHandler
	{
		private const int k_width = 340;
		private const int k_height = 730;
		private const int k_margin = 16;
		private const int k_rowHeight = 28;
		private const int k_labelWidth = 110;
		private const int k_fieldWidth = 60;

		private readonly MikanVNyanPlugin _plugin;
		private readonly IVNyanUIWindow _window;
		private readonly IVNyanUIContentPanel _panel;

		private IVNyanUILabel _statusLabel;
		private IVNyanUILabel _camerasLabel;
		private IVNyanUIDropdown _stageDropdown;
		private readonly List<MikanStageObject> _stages = new List<MikanStageObject>();
		private MikanStageObject _selectedStage;
		private IVNyanUILabel _stagePinLabel;

		private readonly IVNyanUIInputField[] _rootPosition = new IVNyanUIInputField[3];
		private readonly IVNyanUIInputField[] _rootRotation = new IVNyanUIInputField[3];
		private IVNyanUIInputField _rootScale;
		private readonly IVNyanUIInputField[] _stagePosition = new IVNyanUIInputField[3];
		private readonly IVNyanUIInputField[] _stageRotation = new IVNyanUIInputField[3];
		private IVNyanUIInputField _stageScale;

		private MikanClient Client => _plugin.Client;
		private static IUIInterface UI => VNyanInterface.VNyanInterface.VNyanUI;

		public MikanVNyanWindow(MikanVNyanPlugin plugin)
		{
			_plugin = plugin;

			UI.registerPluginButton("MikanXR", this);

			_window = UI.createDraggableWindow(k_width, k_height, true, false);
			_window.setVisible(false);
			_panel = _window.getContentPanel();

			Build();
		}

		public void pluginButtonClicked()
		{
			_window.setVisible(true);
			_window.bringToFront();
			RefreshStages();
			RefreshRootFields();
		}

		// Y grows downward here, so rows are laid out with a negative y that decreases
		private void Build()
		{
			int y = -k_margin;
			MikanSettings settings = Client.Settings;

			UI.createLabel(_panel, "MikanXR", k_margin, y, 200, k_rowHeight);
			y -= k_rowHeight;
			_statusLabel = UI.createLabel(_panel, "Disconnected", k_margin, y, k_width - 2 * k_margin, k_rowHeight);
			y -= k_rowHeight;
			_camerasLabel = UI.createLabel(_panel, "", k_margin, y, k_width - 2 * k_margin, k_rowHeight);
			y -= k_rowHeight + 6;

			UI.createLabel(_panel, "Editor host", k_margin, y, k_labelWidth, k_rowHeight);
			IVNyanUIInputField host = UI.createInputField(_panel, k_margin + k_labelWidth, y, 180, k_rowHeight, settings.host, "ws://127.0.0.1");
			host.onInputFieldEndEdit += () =>
			{
				settings.host = host.getText();
				Client.SaveSettings();
				Reconnect();
			};
			y -= k_rowHeight;

			UI.createLabel(_panel, "Editor port", k_margin, y, k_labelWidth, k_rowHeight);
			IVNyanUIInputField port = UI.createInputField(_panel, k_margin + k_labelWidth, y, 180, k_rowHeight, settings.port, "8080");
			port.onInputFieldEndEdit += () =>
			{
				settings.port = port.getText();
				Client.SaveSettings();
				Reconnect();
			};
			y -= k_rowHeight + 6;

			IVNyanUIToggle stencils = UI.createToggle(_panel, "Render stencils (knockout and shadows)", k_margin, y, k_width - 2 * k_margin, k_rowHeight, Client.RenderStencils);
			stencils.onToggleValueChanged += () =>
			{
				Client.SetRenderStencils(stencils.getIsOn());
				Client.SaveSettings();
			};
			y -= k_rowHeight;

			IVNyanUIToggle frustums = UI.createToggle(_panel, "Show camera frustums", k_margin, y, k_width - 2 * k_margin, k_rowHeight, settings.showCameraFrustums);
			frustums.onToggleValueChanged += () =>
			{
				Client.SetShowCameraFrustums(frustums.getIsOn());
				Client.SaveSettings();
			};
			y -= k_rowHeight;

			UI.createLabel(_panel, "World scale", k_margin, y, k_labelWidth, k_rowHeight);
			IVNyanUIInputField worldScale = UI.createInputField(_panel, k_margin + k_labelWidth, y, k_fieldWidth, k_rowHeight, Format(Client.WorldScale), "10");
			UI.createLabel(_panel, "units per meter", k_margin + k_labelWidth + k_fieldWidth + 8, y, 120, k_rowHeight);
			worldScale.onInputFieldEndEdit += () =>
			{
				Client.SetWorldScale(Parse(worldScale.getText(), Client.WorldScale));
				worldScale.setText(Format(Client.WorldScale));
				Client.SaveSettings();
			};
			y -= k_rowHeight;

			UI.createLabel(_panel, "Shadow strength", k_margin, y, k_labelWidth, k_rowHeight);
			IVNyanUISlider strength = UI.createSlider(_panel, k_margin + k_labelWidth, y, 180, k_rowHeight, 0f, 1f, Client.ShadowStrength, false);
			strength.onSliderValueChanged += () =>
			{
				Client.SetShadowStrength(strength.getValue());
				Client.SaveSettings();
			};
			y -= k_rowHeight + 6;

			UI.createLabel(_panel, "Mikan root in the VNyan scene", k_margin, y, k_width - 2 * k_margin, k_rowHeight);
			y -= k_rowHeight;
			y = BuildVectorRow("Position", y, _rootPosition, ApplyRootFields);
			y = BuildVectorRow("Rotation", y, _rootRotation, ApplyRootFields);
			UI.createLabel(_panel, "Scale", k_margin, y, k_labelWidth, k_rowHeight);
			_rootScale = UI.createInputField(_panel, k_margin + k_labelWidth, y, k_fieldWidth, k_rowHeight, "1", "1");
			_rootScale.onInputFieldEndEdit += ApplyRootFields;
			y -= k_rowHeight + 6;

			UI.createLabel(_panel, "Stage placement", k_margin, y, k_labelWidth, k_rowHeight);
			_stageDropdown = UI.createDropdown(_panel, k_margin + k_labelWidth, y, 180, k_rowHeight, new List<string>() { "(none)" }, 0);
			_stageDropdown.onDropdownValueChanged += () => SelectStage(_stageDropdown.getSelectedIndex());
			y -= k_rowHeight;
			_stagePinLabel = UI.createLabel(_panel, "", k_margin, y, k_width - 2 * k_margin, k_rowHeight);
			y -= k_rowHeight;
			y = BuildVectorRow("Position", y, _stagePosition, ApplyStageFields);
			y = BuildVectorRow("Rotation", y, _stageRotation, ApplyStageFields);
			UI.createLabel(_panel, "Scale", k_margin, y, k_labelWidth, k_rowHeight);
			_stageScale = UI.createInputField(_panel, k_margin + k_labelWidth, y, k_fieldWidth, k_rowHeight, "1", "1");
			_stageScale.onInputFieldEndEdit += ApplyStageFields;
			y -= k_rowHeight + 4;

			IVNyanUIButton pin = UI.createButton(_panel, "Pin stage here", k_margin, y, 140, k_rowHeight);
			pin.onButtonClicked += () =>
			{
				if (_selectedStage != null)
				{
					_selectedStage.PinHere();
					RefreshStageFields();
				}
			};
			IVNyanUIButton unpin = UI.createButton(_panel, "Follow editor", k_margin + 150, y, 140, k_rowHeight);
			unpin.onButtonClicked += () =>
			{
				if (_selectedStage != null)
				{
					_selectedStage.Unpin();
					RefreshStageFields();
				}
			};
			y -= k_rowHeight + 6;

			IVNyanUIButton refresh = UI.createButton(_panel, "Refresh stages", k_margin, y, 140, k_rowHeight);
			refresh.onButtonClicked += RefreshStages;
			IVNyanUIButton reconnect = UI.createButton(_panel, "Reconnect", k_margin + 150, y, 140, k_rowHeight);
			reconnect.onButtonClicked += Reconnect;
		}

		private int BuildVectorRow(string label, int y, IVNyanUIInputField[] fields, System.Action onEdit)
		{
			UI.createLabel(_panel, label, k_margin, y, k_labelWidth, k_rowHeight);
			for (int axis = 0; axis < 3; ++axis)
			{
				int x = k_margin + k_labelWidth + axis * (k_fieldWidth + 4);
				fields[axis] = UI.createInputField(_panel, x, y, k_fieldWidth, k_rowHeight, "0", "0");
				fields[axis].onInputFieldEndEdit += onEdit;
			}

			return y - k_rowHeight;
		}

		private void Reconnect()
		{
			Client.Disconnect();
			Client.Connect();
		}

		public void Tick()
		{
			if (_statusLabel == null)
			{
				return;
			}

			string status = Client.IsClientInitialized
				? $"Connected to {Client.Settings.host} as {Client.API.GetClientUniqueID()}"
				: (Client.IsConnected ? "Connecting..." : "Disconnected (retrying)");
			_statusLabel.setText(status);

			int cameras = 0;
			int rendering = 0;
			foreach (MikanCameraObject camera in Client.SceneMirror.FindAll<MikanCameraObject>())
			{
				++cameras;
				if (camera.IsAllocated)
				{
					++rendering;
				}
			}
			_camerasLabel.setText($"{Client.SceneMirror.Objects.Count} objects, {cameras} cameras, {rendering} publishing");
		}

		private void RefreshRootFields()
		{
			Transform root = _plugin.Root.transform;
			SetVector(_rootPosition, root.localPosition);
			SetVector(_rootRotation, root.localEulerAngles);
			_rootScale.setText(Format(root.localScale.x));
		}

		private void ApplyRootFields()
		{
			Transform root = _plugin.Root.transform;
			root.localPosition = ReadVector(_rootPosition, root.localPosition);
			root.localEulerAngles = ReadVector(_rootRotation, root.localEulerAngles);
			float scale = Parse(_rootScale.getText(), root.localScale.x);
			root.localScale = Vector3.one * scale;

			Client.Settings.applyRootTransform = true;
			Client.SaveSettings();
		}

		private void RefreshStages()
		{
			_stages.Clear();
			var names = new List<string>();
			foreach (MikanStageObject stage in Client.SceneMirror.FindAll<MikanStageObject>())
			{
				_stages.Add(stage);
				names.Add(string.IsNullOrEmpty(stage.StageName) ? $"Stage {stage.ComponentId}" : stage.StageName);
			}

			if (names.Count == 0)
			{
				names.Add("(none)");
			}

			_stageDropdown.setOptions(names);
			SelectStage(0);
		}

		private void SelectStage(int index)
		{
			_selectedStage = index >= 0 && index < _stages.Count ? _stages[index] : null;
			RefreshStageFields();
		}

		private void RefreshStageFields()
		{
			if (_selectedStage == null)
			{
				_stagePinLabel.setText("No stage mirrored yet");
				return;
			}

			Transform stage = _selectedStage.transform;
			SetVector(_stagePosition, stage.localPosition);
			SetVector(_stageRotation, stage.localEulerAngles);
			_stageScale.setText(Format(stage.localScale.x));
			_stagePinLabel.setText(_selectedStage.IsPinned ? "Pinned here (ignores the editor's transform)" : "Following the editor's transform");
		}

		private void ApplyStageFields()
		{
			if (_selectedStage == null)
			{
				return;
			}

			Transform stage = _selectedStage.transform;
			stage.localPosition = ReadVector(_stagePosition, stage.localPosition);
			stage.localEulerAngles = ReadVector(_stageRotation, stage.localEulerAngles);
			stage.localScale = Vector3.one * Parse(_stageScale.getText(), stage.localScale.x);

			// An edited stage is a placed stage
			_selectedStage.PinHere();
			RefreshStageFields();
		}

		private static void SetVector(IVNyanUIInputField[] fields, Vector3 value)
		{
			fields[0].setText(Format(value.x));
			fields[1].setText(Format(value.y));
			fields[2].setText(Format(value.z));
		}

		private static Vector3 ReadVector(IVNyanUIInputField[] fields, Vector3 fallback)
		{
			return new Vector3(
				Parse(fields[0].getText(), fallback.x),
				Parse(fields[1].getText(), fallback.y),
				Parse(fields[2].getText(), fallback.z));
		}

		private static string Format(float value)
		{
			return value.ToString("0.###", CultureInfo.InvariantCulture);
		}

		private static float Parse(string text, float fallback)
		{
			return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : fallback;
		}
	}
}
