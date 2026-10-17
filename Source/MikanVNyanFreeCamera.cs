using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using VNyanInterface;

namespace MikanXR.VNyan
{
	// The free camera's settings. VNyan only, so they live in their own file rather than in the
	// shared package's MikanSettings.
	[Serializable]
	public class MikanVNyanFreeCameraSettings
	{
		public const string SettingsFileName = "MikanXRFreeCamera.cfg";
		private const string k_jsonKey = "settings_json";

		public bool enabled = false;
		// Off by default: VNyan's gamepad nodes can be reading the same pad for the avatar
		public bool gamepadEnabled = false;
		public bool invertY = false;
		// Units per second
		public float moveSpeed = 2f;
		public float fastMultiplier = 4f;
		// Degrees per pixel of mouse motion
		public float lookSensitivity = 0.15f;
		// Degrees per second at full stick
		public float gamepadLookSpeed = 120f;
		// How quickly the velocity eases toward the input, per second
		public float acceleration = 10f;

		public static MikanVNyanFreeCameraSettings Load()
		{
			var settings = new MikanVNyanFreeCameraSettings();
			Dictionary<string, string> stored = VNyanInterface.VNyanInterface.VNyanSettings.loadSettings(SettingsFileName);
			if (stored != null && stored.TryGetValue(k_jsonKey, out string json) && !string.IsNullOrEmpty(json))
			{
				JsonUtility.FromJsonOverwrite(json, settings);
			}

			return settings;
		}

		public void Save()
		{
			var stored = new Dictionary<string, string>()
			{
				{ k_jsonKey, JsonUtility.ToJson(this) }
			};

			VNyanInterface.VNyanInterface.VNyanSettings.saveSettings(SettingsFileName, stored);
		}
	}

	// Flies VNyan's view camera with the mouse and keyboard (while the right button is held) or a
	// gamepad. VNyan's OrbitCamera drives the same transform every frame, so it is disabled while
	// the free camera is engaged and re-enabled when it lets go. VNyan still places the camera
	// directly (its saved pose at startup, camera presets), and the free camera continues from
	// any pose it did not write itself.
	public class MikanVNyanFreeCamera : MonoBehaviour, ITriggerHandler
	{
		public const string TriggerOn = "MikanFreeCameraOn";
		public const string TriggerOff = "MikanFreeCameraOff";
		public const string TriggerToggle = "MikanFreeCameraToggle";

		private const string k_cameraPath = "/CameraContainer/Main Camera";
		private const string k_orbitCameraType = "OrbitCamera";
		private const float k_pitchLimit = 89f;
		private const float k_speedStep = 1.25f;
		private const float k_minSpeed = 0.05f;
		private const float k_maxSpeed = 100f;

		private MikanVNyanFreeCameraSettings _settings;
		private Transform _camera;
		private Behaviour _orbitCamera;
		private bool _engaged;

		private float _yaw;
		private float _pitch;
		private Vector3 _velocity;
		private Vector3 _writtenPosition;
		private Quaternion _writtenRotation;
		private Vector3 _resetPosition;
		private float _resetYaw;
		private float _resetPitch;

		private bool _mouseLooking;
		private CursorLockMode _savedLockState;
		private bool _savedCursorVisible;

		public MikanVNyanFreeCameraSettings Settings => _settings;
		public bool IsEnabled => _settings.enabled;
		public bool IsEngaged => _engaged;

		// Raised when the enabled state changes from outside the window (a trigger)
		public event Action EnabledChanged;

		private void Awake()
		{
			_settings = MikanVNyanFreeCameraSettings.Load();
		}

		private void Start()
		{
			VNyanInterface.VNyanInterface.VNyanTrigger.registerTriggerListener(this);
		}

		private void OnDestroy()
		{
			Disengage();
		}

		public void SetEnabled(bool enabled)
		{
			if (_settings.enabled == enabled)
			{
				return;
			}

			_settings.enabled = enabled;
			_settings.Save();

			if (!enabled)
			{
				Disengage();
			}

			// Engaging happens in LateUpdate, which also covers the camera not existing yet
			EnabledChanged?.Invoke();
		}

		public void SaveSettings()
		{
			_settings.Save();
		}

		// Returns to the pose the camera had when the free camera engaged
		public void ResetView()
		{
			if (!_engaged)
			{
				return;
			}

			_camera.position = _resetPosition;
			_yaw = _resetYaw;
			_pitch = _resetPitch;
			_velocity = Vector3.zero;
			ApplyRotation();
			RecordWrittenPose();
		}

		public void triggerCalled(string name, int int1, int int2, int int3, string text1, string text2, string text3)
		{
			switch (name)
			{
				case TriggerOn:
					SetEnabled(true);
					break;
				case TriggerOff:
					SetEnabled(false);
					break;
				case TriggerToggle:
					SetEnabled(!_settings.enabled);
					break;
			}
		}

		private void LateUpdate()
		{
			if (!_settings.enabled)
			{
				return;
			}

			// The camera can be missing at startup or replaced by VNyan, so engaging retries each frame
			if (!_engaged || _camera == null)
			{
				_engaged = false;
				if (!TryEngage())
				{
					return;
				}
			}
			else if (_camera.position != _writtenPosition || Quaternion.Angle(_camera.rotation, _writtenRotation) > 0.01f)
			{
				AdoptPose();
			}

			float deltaTime = Time.unscaledDeltaTime;
			Vector3 moveInput = Vector3.zero;
			float verticalInput = 0f;
			float speedScale = 1f;

			UpdateMouseKeyboard(ref moveInput, ref verticalInput, ref speedScale);
			if (_settings.gamepadEnabled)
			{
				UpdateGamepad(deltaTime, ref moveInput, ref verticalInput);
			}

			ApplyRotation();

			// Planar input moves along the view, vertical input along world up
			Vector3 move = _camera.rotation * Vector3.ClampMagnitude(moveInput, 1f) + Vector3.up * Mathf.Clamp(verticalInput, -1f, 1f);
			Vector3 targetVelocity = move * _settings.moveSpeed * speedScale;
			_velocity = Vector3.Lerp(_velocity, targetVelocity, 1f - Mathf.Exp(-_settings.acceleration * deltaTime));
			_camera.position += _velocity * deltaTime;
			RecordWrittenPose();
		}

		private bool TryEngage()
		{
			GameObject cameraObject = GameObject.Find(k_cameraPath);
			if (cameraObject == null && Camera.main != null)
			{
				cameraObject = Camera.main.gameObject;
			}

			if (cameraObject == null)
			{
				return false;
			}

			_camera = cameraObject.transform;
			_orbitCamera = cameraObject.GetComponent(k_orbitCameraType) as Behaviour;
			if (_orbitCamera != null)
			{
				_orbitCamera.enabled = false;
			}

			// Start from the current pose so the view does not jump
			AdoptPose();
			_engaged = true;

			Debug.Log("[MikanXR] free camera engaged");
			return true;
		}

		// Continues from the camera's current pose, which also becomes the reset pose. VNyan's
		// orbit carries no roll.
		private void AdoptPose()
		{
			Vector3 euler = _camera.eulerAngles;
			_pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, euler.x), -k_pitchLimit, k_pitchLimit);
			_yaw = euler.y;
			_velocity = Vector3.zero;
			_resetPosition = _camera.position;
			_resetYaw = _yaw;
			_resetPitch = _pitch;
		}

		private void RecordWrittenPose()
		{
			_writtenPosition = _camera.position;
			_writtenRotation = _camera.rotation;
		}

		private void Disengage()
		{
			EndMouseLook();

			if (_orbitCamera != null)
			{
				_orbitCamera.enabled = true;
			}

			if (_engaged)
			{
				Debug.Log("[MikanXR] free camera released");
			}

			_orbitCamera = null;
			_camera = null;
			_engaged = false;
		}

		private void UpdateMouseKeyboard(ref Vector3 moveInput, ref float verticalInput, ref float speedScale)
		{
			Mouse mouse = Mouse.current;
			Keyboard keyboard = Keyboard.current;
			if (mouse == null)
			{
				return;
			}

			// A press that lands on VNyan's UI belongs to the UI
			if (mouse.rightButton.wasPressedThisFrame && !IsPointerOverUI())
			{
				BeginMouseLook();
			}
			else if (_mouseLooking && !mouse.rightButton.isPressed)
			{
				EndMouseLook();
			}

			if (!_mouseLooking)
			{
				return;
			}

			Vector2 delta = mouse.delta.ReadValue() * _settings.lookSensitivity;
			_yaw += delta.x;
			_pitch += _settings.invertY ? delta.y : -delta.y;

			float scroll = mouse.scroll.ReadValue().y;
			if (scroll != 0f)
			{
				StepSpeed(scroll > 0f ? 1 : -1);
			}

			if (keyboard == null)
			{
				return;
			}

			if (keyboard.wKey.isPressed) moveInput.z += 1f;
			if (keyboard.sKey.isPressed) moveInput.z -= 1f;
			if (keyboard.dKey.isPressed) moveInput.x += 1f;
			if (keyboard.aKey.isPressed) moveInput.x -= 1f;
			if (keyboard.eKey.isPressed) verticalInput += 1f;
			if (keyboard.qKey.isPressed) verticalInput -= 1f;
			if (keyboard.shiftKey.isPressed) speedScale = _settings.fastMultiplier;
		}

		private void UpdateGamepad(float deltaTime, ref Vector3 moveInput, ref float verticalInput)
		{
			Gamepad gamepad = Gamepad.current;
			if (gamepad == null)
			{
				return;
			}

			// The Input System's stick processors already apply a dead zone
			Vector2 move = gamepad.leftStick.ReadValue();
			moveInput.x += move.x;
			moveInput.z += move.y;
			verticalInput += gamepad.rightTrigger.ReadValue() - gamepad.leftTrigger.ReadValue();

			Vector2 look = gamepad.rightStick.ReadValue() * _settings.gamepadLookSpeed * deltaTime;
			_yaw += look.x;
			_pitch += _settings.invertY ? look.y : -look.y;

			if (gamepad.rightShoulder.wasPressedThisFrame)
			{
				StepSpeed(1);
			}
			else if (gamepad.leftShoulder.wasPressedThisFrame)
			{
				StepSpeed(-1);
			}

			if (gamepad.rightStickButton.wasPressedThisFrame)
			{
				ResetView();
			}
		}

		private void StepSpeed(int direction)
		{
			float speed = direction > 0 ? _settings.moveSpeed * k_speedStep : _settings.moveSpeed / k_speedStep;
			_settings.moveSpeed = Mathf.Clamp(speed, k_minSpeed, k_maxSpeed);
			_settings.Save();
		}

		private void ApplyRotation()
		{
			_pitch = Mathf.Clamp(_pitch, -k_pitchLimit, k_pitchLimit);
			_yaw = Mathf.Repeat(_yaw, 360f);
			_camera.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
		}

		private void BeginMouseLook()
		{
			if (_mouseLooking)
			{
				return;
			}

			// The cursor locks so it cannot wander off the window mid look
			_savedLockState = Cursor.lockState;
			_savedCursorVisible = Cursor.visible;
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
			_mouseLooking = true;
		}

		private void EndMouseLook()
		{
			if (!_mouseLooking)
			{
				return;
			}

			Cursor.lockState = _savedLockState;
			Cursor.visible = _savedCursorVisible;
			_mouseLooking = false;
		}

		private static bool IsPointerOverUI()
		{
			EventSystem eventSystem = EventSystem.current;
			return eventSystem != null && eventSystem.IsPointerOverGameObject();
		}
	}
}
