using System.Globalization;
using VNyanInterface;

namespace MikanXR.VNyan
{
	// The free camera's settings window, opened from the plugin window's "Camera settings" button
	public class MikanVNyanFreeCameraWindow
	{
		private const int k_width = 340;
		private const int k_height = 340;
		private const int k_margin = 16;
		private const int k_rowHeight = 30;
		private const int k_labelWidth = 170;
		private const int k_fieldWidth = 60;

		private readonly MikanVNyanFreeCamera _freeCamera;
		private readonly IVNyanUIWindow _window;
		private readonly IVNyanUIContentPanel _panel;

		private IVNyanUIToggle _gamepad;
		private IVNyanUIToggle _invertY;
		private IVNyanUIInputField _moveSpeed;
		private IVNyanUIInputField _fastMultiplier;
		private IVNyanUIInputField _lookSensitivity;
		private IVNyanUIInputField _gamepadLookSpeed;
		private IVNyanUIInputField _acceleration;

		private MikanVNyanFreeCameraSettings Settings => _freeCamera.Settings;
		private static IUIInterface UI => VNyanInterface.VNyanInterface.VNyanUI;

		public MikanVNyanFreeCameraWindow(MikanVNyanFreeCamera freeCamera)
		{
			_freeCamera = freeCamera;

			_window = UI.createDraggableWindow(k_width, k_height, true, false);
			_window.setVisible(false);
			_panel = _window.getContentPanel();

			Build();
		}

		public void Show()
		{
			// The speed also changes from the scroll wheel and the shoulder buttons
			Refresh();
			_window.setVisible(true);
			_window.bringToFront();
		}

		private void Build()
		{
			int y = -k_margin;

			UI.createLabel(_panel, "MikanXR free camera", k_margin, y, k_width - 2 * k_margin, k_rowHeight);
			y -= k_rowHeight;
			UI.createLabel(_panel, "Hold RMB: mouse look, WASD, Q/E, Shift", k_margin, y, k_width - 2 * k_margin, k_rowHeight);
			y -= k_rowHeight + 6;

			_gamepad = UI.createToggle(_panel, "Gamepad controls", k_margin, y, k_width - 2 * k_margin, k_rowHeight, Settings.gamepadEnabled);
			_gamepad.onToggleValueChanged += () =>
			{
				Settings.gamepadEnabled = _gamepad.getIsOn();
				_freeCamera.SaveSettings();
			};
			y -= k_rowHeight;

			_invertY = UI.createToggle(_panel, "Invert look Y", k_margin, y, k_width - 2 * k_margin, k_rowHeight, Settings.invertY);
			_invertY.onToggleValueChanged += () =>
			{
				Settings.invertY = _invertY.getIsOn();
				_freeCamera.SaveSettings();
			};
			y -= k_rowHeight + 6;

			_moveSpeed = BuildFloatRow("Move speed (units/s)", ref y, () => Settings.moveSpeed, value => Settings.moveSpeed = value);
			_fastMultiplier = BuildFloatRow("Shift multiplier", ref y, () => Settings.fastMultiplier, value => Settings.fastMultiplier = value);
			_lookSensitivity = BuildFloatRow("Mouse look (deg/px)", ref y, () => Settings.lookSensitivity, value => Settings.lookSensitivity = value);
			_gamepadLookSpeed = BuildFloatRow("Stick look (deg/s)", ref y, () => Settings.gamepadLookSpeed, value => Settings.gamepadLookSpeed = value);
			_acceleration = BuildFloatRow("Acceleration", ref y, () => Settings.acceleration, value => Settings.acceleration = value);
			y -= 6;

			IVNyanUIButton reset = UI.createButton(_panel, "Reset view", k_margin, y, 140, k_rowHeight);
			reset.onButtonClicked += _freeCamera.ResetView;
		}

		private IVNyanUIInputField BuildFloatRow(string label, ref int y, System.Func<float> get, System.Action<float> set)
		{
			UI.createLabel(_panel, label, k_margin, y, k_labelWidth, k_rowHeight);
			IVNyanUIInputField field = UI.createInputField(_panel, k_margin + k_labelWidth, y, k_fieldWidth, k_rowHeight, Format(get()), Format(get()));
			field.onInputFieldEndEdit += () =>
			{
				// Zero or negative values would freeze or reverse the camera
				if (float.TryParse(field.getText(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value) && value > 0f)
				{
					set(value);
					_freeCamera.SaveSettings();
				}

				field.setText(Format(get()));
			};
			y -= k_rowHeight;

			return field;
		}

		private void Refresh()
		{
			_gamepad.setIsOn(Settings.gamepadEnabled);
			_invertY.setIsOn(Settings.invertY);
			_moveSpeed.setText(Format(Settings.moveSpeed));
			_fastMultiplier.setText(Format(Settings.fastMultiplier));
			_lookSensitivity.setText(Format(Settings.lookSensitivity));
			_gamepadLookSpeed.setText(Format(Settings.gamepadLookSpeed));
			_acceleration.setText(Format(Settings.acceleration));
		}

		private static string Format(float value)
		{
			return value.ToString("0.###", CultureInfo.InvariantCulture);
		}
	}
}
