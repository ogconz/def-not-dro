using System;
using System.Collections.Generic;
using Rewired;
using SimpleJSON;

namespace GameInput
{
	public class InputSettings
	{
		public class Parameters
		{
			public ESteeringSpeed steeringSpeed = ESteeringSpeed.FAST;

			public float steeringLinearity = 1f;

			public float accLinearity = 1f;

			public float brakeLinearity = 1f;

			public float clutchLinearity = 1f;

			public float cursorAcceleration = 1f;

			public float handbrakeLinearity = 1f;

			public float buttonForce = 1f;

			public bool holdIgnition;

			public bool automaticGearboxEnabled = true;

			private bool m_automaticClutchEnabled = true;

			[JsonIgnore]
			public Action<bool> autoClutchChanged;

			public GamepadParams gamepadParams = new GamepadParams();

			public WheelParams wheelParams = new WheelParams();

			public float forceFeedback = 0.8f;

			public bool vibrationEnabled = true;

			public List<ControlsSetup.Setup> defaultUnlockedPhysicsParams;

			public List<ControlsSetup.Setup> customUnlockedPhysicsParams;

			public WheelPresetType currentWheelPresetType;

			[JsonIgnore]
			public List<StaticWheelPreset> staticWheelPresets = new List<StaticWheelPreset>
			{
				new StaticWheelPreset(WheelPresetType.Beginner),
				new StaticWheelPreset(WheelPresetType.Pro)
			};

			public bool isLogitechLastSteeringDevice;

			public DeviceType? steeringDeviceType;

			private float m_steeringDeadZone;

			public bool automaticClutchEnabled
			{
				get
				{
					return m_automaticClutchEnabled;
				}
				set
				{
					if (m_automaticClutchEnabled != value)
					{
						m_automaticClutchEnabled = value;
						autoClutchChanged.SafeCall(m_automaticClutchEnabled);
					}
				}
			}

			public bool autoClutch
			{
				get
				{
					if (!automaticGearboxEnabled)
					{
						return automaticClutchEnabled;
					}
					return true;
				}
			}

			public float steeringDeadZone
			{
				get
				{
					if (m_steeringDeadZone != 0f)
					{
						return m_steeringDeadZone;
					}
					return InputManager.settings.Params.wheelParams.steeringDeadzone;
				}
				set
				{
					InputManager.settings.Params.wheelParams.steeringDeadzone = value;
					m_steeringDeadZone = value;
				}
			}

			public ESteeringWheelAngle steeringWheelAngle
			{
				get
				{
					return wheelParams.steeringWheelAngle;
				}
				set
				{
					wheelParams.steeringWheelAngle = value;
				}
			}
		}

		public class GamepadParams
		{
			private bool? m_vibrationEnabled;

			public bool vibrationEnabled
			{
				get
				{
					if (!m_vibrationEnabled.HasValue)
					{
						return InputManager.settings.Params.vibrationEnabled;
					}
					return m_vibrationEnabled.Value;
				}
				set
				{
					m_vibrationEnabled = value;
				}
			}
		}

		public class WheelParams
		{
			[JsonIgnore]
			public System.Action SteeringWheelAngleChanged;

			public bool isThrust248AccelParamOverrided;

			public bool isThrustAccelOverrided;

			public bool isLogitechAccelOverrided;

			private ESteeringWheelAngle m_steeringWheelAngle = ESteeringWheelAngle.R900;

			private float? m_steeringDeadzone;

			private float? m_forceFeedback;

			[JsonIgnore]
			public bool feedbackEnabled => forceFeedback > 0.01f;

			public ESteeringWheelAngle steeringWheelAngle
			{
				get
				{
					return m_steeringWheelAngle;
				}
				set
				{
					m_steeringWheelAngle = value;
					SteeringWheelAngleChanged.SafeCall();
				}
			}

			public float steeringDeadzone
			{
				get
				{
					return m_steeringDeadzone.GetValueOrDefault();
				}
				set
				{
					m_steeringDeadzone = value;
				}
			}

			public float forceFeedback
			{
				get
				{
					if (!m_forceFeedback.HasValue)
					{
						return InputManager.settings.Params.forceFeedback;
					}
					return m_forceFeedback.Value;
				}
				set
				{
					m_forceFeedback = value;
				}
			}
		}

		public class DeviceProfile
		{
			public int version = 1;

			public DeviceType deviceType = DeviceType.Unknown;

			public bool disableFFB;

			public List<BindButtonElement> buttons = new List<BindButtonElement>();

			public List<BindAxisElement> axises = new List<BindAxisElement>();

			private static IReadOnlyList<string> m_mozaWheelBasesPidVid = new List<string> { "00346e", "02346e", "05346e", "06346e", "07346e", "04346e" };

			public void Bind(Controller rewiredController)
			{
				ControllerMetaInfo metaInfo = rewiredController.GetMetaInfo();
				CheckVersion(metaInfo);
				RemoveOldSystemBindings();
				ReConstructSystemBindings(metaInfo);
				foreach (BindButtonElement button in buttons)
				{
					button.deviceInfo = metaInfo;
					button.inputButton = GetInputButton(rewiredController, button);
				}
				if (rewiredController is ControllerWithAxes controller)
				{
					foreach (BindAxisElement axise in axises)
					{
						axise.deviceInfo = metaInfo;
						axise.inputAxis = GetInputAxis(controller, axise);
						if (axise is BindAxisGamepadElement bindAxisGamepadElement)
						{
							bindAxisGamepadElement.inputAxisY = GetInputAxis(controller, bindAxisGamepadElement);
						}
					}
				}
				else
				{
					axises.Clear();
				}
				buttons.RemoveAll((BindButtonElement b) => b.inputButton == null || b.bindedAction == ActionType.ErrorAction);
				axises.RemoveAll((BindAxisElement a) => a.inputAxis == null || a.bindedAction == ActionType.ErrorAction);
			}

			private int GetButtonElementHash(BindButtonElement element)
			{
				return (int)element.bindedAction + ((int)element.bindType << 16);
			}

			private void RemoveDublicates()
			{
				HashSet<string> hashSet = new HashSet<string>();
				List<BindButtonElement> list = new List<BindButtonElement>();
				for (int num = buttons.Count - 1; num >= 0; num--)
				{
					BindButtonElement bindButtonElement = buttons[num];
					if (!bindButtonElement.IsSystemAction())
					{
						if (hashSet.Contains(bindButtonElement.elementId))
						{
							list.Add(bindButtonElement);
						}
						else
						{
							hashSet.Add(bindButtonElement.elementId);
						}
					}
				}
				foreach (BindButtonElement item in list)
				{
					buttons.Remove(item);
				}
				hashSet.Clear();
				List<BindAxisElement> list2 = new List<BindAxisElement>();
				for (int num2 = axises.Count - 1; num2 >= 0; num2--)
				{
					BindAxisElement bindAxisElement = axises[num2];
					if (!bindAxisElement.IsSystemAction())
					{
						if (hashSet.Contains(bindAxisElement.elementName))
						{
							list2.Add(bindAxisElement);
						}
						else
						{
							hashSet.Add(bindAxisElement.elementName);
						}
					}
				}
				foreach (BindAxisElement item2 in list2)
				{
					axises.Remove(item2);
				}
			}

			private void CheckVersion(ControllerMetaInfo controllerMeta)
			{
				if (!HasNonSystemBinds(controllerMeta) || version != 12)
				{
					ConstructDefault(controllerMeta, version);
					RemoveDublicates();
					version = 12;
				}
			}

			public void ConstructDefault(ControllerMetaInfo controllerMeta)
			{
				version = 12;
				ConstructDefault(controllerMeta, 0);
			}

			private bool HasNonSystemBinds(ControllerMetaInfo controllerMeta)
			{
				Controller rewiredController = controllerMeta.rewiredController;
				if (rewiredController != null && rewiredController.elementCount == 0)
				{
					return false;
				}
				int count = 0;
				CheckBindedAction<BindButtonElement>(buttons, ref count);
				CheckBindedAction<BindAxisElement>(axises, ref count);
				return count != 0;
				static void CheckBindedAction<T>(List<T> binds, ref int reference) where T : BindElement
				{
					foreach (T bind in binds)
					{
						ActionType bindedAction = bind.bindedAction;
						if (!Action.IsSystemAction(bindedAction) && bindedAction != ActionType.ErrorAction)
						{
							reference++;
						}
					}
				}
			}

			public void ConstructDefault(ControllerMetaInfo controllerMeta, int version)
			{
				deviceType = controllerMeta.deviceType;
				switch (controllerMeta.deviceType)
				{
				case DeviceType.Keyboard:
					ConstructDefaultKeyboard(controllerMeta.rewiredController as Keyboard, version);
					return;
				case DeviceType.Mouse:
					ConstructDefaultMouse(controllerMeta.rewiredController as Mouse, version);
					return;
				case DeviceType.Pedals:
					ConstructDefaultPedals(controllerMeta.rewiredController as ControllerWithAxes, version);
					return;
				}
				TryMapShifter(controllerMeta.rewiredController, version);
				switch (controllerMeta.deviceType)
				{
				case DeviceType.ButtonPanel:
					ConstructDefaultNoAxis(controllerMeta.rewiredController, version);
					break;
				case DeviceType.Wheel:
					if (controllerMeta.isPS4PadCompatible)
					{
						ConstructDefaultWheelWithPad(controllerMeta.rewiredController as ControllerWithAxes, version);
					}
					else
					{
						ConstructDefaultWheel(controllerMeta.rewiredController as ControllerWithAxes, version);
					}
					break;
				default:
					ConstructDefaultPad(controllerMeta.rewiredController as ControllerWithAxes, version, isWheel: false);
					break;
				}
			}

			private void ConstructDefaultWheelWithPad(ControllerWithAxes controllerWithAxes, int version)
			{
				ConstructDefaultWheel(controllerWithAxes, version);
				ConstructDefaultPad(controllerWithAxes, version, isWheel: true);
			}

			private static bool IsHaveAxis(ControllerWithAxes controller, string axisNameSubstring)
			{
				if (controller != null)
				{
					foreach (ControllerElementIdentifier axisElementIdentifier in controller.AxisElementIdentifiers)
					{
						if (axisElementIdentifier.name.ToLower().Contains(axisNameSubstring))
						{
							return true;
						}
					}
				}
				return false;
			}

			private static bool TryGetSavedDeviceTypeByController(Controller controller, out DeviceType savedDeviceType)
			{
				savedDeviceType = DeviceType.Unknown;
				InputManager instance = InputManager.instance;
				if (instance == null)
				{
					return false;
				}
				if (!instance.activeControllers.TryGetValue(controller, out var value))
				{
					return false;
				}
				if (!InputManager.settings.devices.TryGetValue(value, out var value2))
				{
					return false;
				}
				if (value2.deviceType == DeviceType.Unknown)
				{
					return false;
				}
				savedDeviceType = value2.deviceType;
				return true;
			}

			public static DeviceType DetermineDeviceType(Controller controller, string deviceName, bool ignoreSaves = false)
			{
				if (!ignoreSaves && TryGetSavedDeviceTypeByController(controller, out var savedDeviceType))
				{
					return savedDeviceType;
				}
				if (controller is Keyboard)
				{
					return DeviceType.Keyboard;
				}
				if (controller is Mouse)
				{
					return DeviceType.Mouse;
				}
				if (!(controller is ControllerWithAxes controller2))
				{
					return DeviceType.ButtonPanel;
				}
				bool flag = IsHaveAxis(controller2, "wheel");
				if (!flag && IsHaveAxis(controller2, "accelerator") && IsHaveAxis(controller2, "brake"))
				{
					return DeviceType.Pedals;
				}
				string text = deviceName.ToLower();
				bool flag2 = text.Contains("wheel") || text.Contains("racing") || text.Contains("driving");
				flag2 |= IsMozaWheelBaseController(controller);
				if (!flag2)
				{
					flag2 = flag;
				}
				if (!flag2)
				{
					return DeviceType.Gamepad;
				}
				return DeviceType.Wheel;
			}

			private static bool IsMozaWheelBaseController(Controller controller)
			{
				if (controller == null)
				{
					return false;
				}
				string text = controller.hardwareIdentifier.ToLower();
				foreach (string item in m_mozaWheelBasesPidVid)
				{
					if (text.Contains(item))
					{
						return true;
					}
				}
				return false;
			}

			private void RemoveOldSystemBindings()
			{
				for (int num = buttons.Count - 1; num > -1; num--)
				{
					if (buttons[num].IsSystemAction())
					{
						buttons.RemoveAt(num);
					}
				}
				for (int num2 = axises.Count - 1; num2 > -1; num2--)
				{
					if (axises[num2].IsSystemAction())
					{
						axises.RemoveAt(num2);
					}
				}
			}

			private void RemoveButtonElement(ref string[] arr)
			{
				_ = arr;
			}

			private void RemoveAxisElement(ref string[] arr)
			{
				_ = arr;
			}

			private void ReConstructSystemBindings(ControllerMetaInfo controllerMeta)
			{
				switch (controllerMeta.deviceType)
				{
				case DeviceType.Keyboard:
					buttons.Add(new BindButtonElement("E", ActionType.NavNextPage));
					buttons.Add(new BindButtonElement("Q", ActionType.NavPrevPage));
					buttons.Add(new BindButtonElement("W", ActionType.NavUp));
					buttons.Add(new BindButtonElement("Up Arrow", ActionType.NavUp));
					buttons.Add(new BindButtonElement("S", ActionType.NavDown));
					buttons.Add(new BindButtonElement("Down Arrow", ActionType.NavDown));
					buttons.Add(new BindButtonElement("D", ActionType.NavRight));
					buttons.Add(new BindButtonElement("Right Arrow", ActionType.NavRight));
					buttons.Add(new BindButtonElement("A", ActionType.NavLeft));
					buttons.Add(new BindButtonElement("Left Arrow", ActionType.NavLeft));
					buttons.Add(new BindButtonElement("X", ActionType.NavAdditional0BtnX));
					buttons.Add(new BindButtonElement("Y", ActionType.NavAdditional1BtnY));
					buttons.Add(new BindButtonElement("1", ActionType.NavAdditionalPrevBtnL2));
					buttons.Add(new BindButtonElement("3", ActionType.NavAdditionalNextBtnR2));
					buttons.Add(new BindButtonElement("ESC", ActionType.NavCancelBtnB, BindButtonType.Positive, skipShiftCtrlButtons: true));
					buttons.Add(new BindButtonElement("Space", ActionType.NavSelectBtnA, BindButtonType.Positive, skipShiftCtrlButtons: true));
					buttons.Add(new BindButtonElement("Return", ActionType.NavSelectBtnA, BindButtonType.Positive, skipShiftCtrlButtons: true));
					buttons.Add(new BindButtonElement("Keypad Enter", ActionType.NavSelectBtnA, BindButtonType.Positive, skipShiftCtrlButtons: true));
					buttons.Add(new BindButtonElement("ESC", ActionType.NavBack, BindButtonType.Positive, skipShiftCtrlButtons: true));
					buttons.Add(new BindButtonElement("Tab", ActionType.NavStart, BindButtonType.Positive, skipShiftCtrlButtons: true));
					buttons.Add(new BindButtonElement("Keypad8", ActionType.NavDPadUp));
					buttons.Add(new BindButtonElement("Keypad2", ActionType.NavDPadDown));
					buttons.Add(new BindButtonElement("Keypad6", ActionType.NavDPadRight));
					buttons.Add(new BindButtonElement("Keypad4", ActionType.NavDPadLeft));
					buttons.Add(new BindButtonElement("G", ActionType.NavBtnExpand));
					buttons.Add(new BindButtonElement("T", ActionType.NavBtnExpandRight));
					buttons.Add(new BindButtonElement("Z", ActionType.NavDPadUp));
					buttons.Add(new BindButtonElement("C", ActionType.NavDPadDown));
					buttons.Add(new BindButtonElement("V", ActionType.NavDPadLeft));
					buttons.Add(new BindButtonElement("B", ActionType.NavDPadRight));
					buttons.Add(new BindButtonElement("R", ActionType.NavReport));
					buttons.Add(new BindButtonElement("W", ActionType.FlyMovement));
					buttons.Add(new BindButtonElement("S", ActionType.FlyMovement, BindButtonType.Negative));
					buttons.Add(new BindButtonElement("A", ActionType.FlyStrafe, BindButtonType.Negative));
					buttons.Add(new BindButtonElement("D", ActionType.FlyStrafe));
					buttons.Add(new BindButtonElement("Up Arrow", ActionType.FlyMovement));
					buttons.Add(new BindButtonElement("Down Arrow", ActionType.FlyMovement, BindButtonType.Negative));
					buttons.Add(new BindButtonElement("Right Arrow", ActionType.FlyStrafe));
					buttons.Add(new BindButtonElement("Left Arrow", ActionType.FlyStrafe, BindButtonType.Negative));
					buttons.Add(new BindButtonElement("Space", ActionType.FlyHeightUp));
					buttons.Add(new BindButtonElement("Left Control", ActionType.FlyHeightDown));
					buttons.Add(new BindButtonElement("Left Shift", ActionType.FlyTurboMode));
					buttons.Add(new BindButtonElement("P", ActionType.FlyHideUI));
					buttons.Add(new BindButtonElement("Tab", ActionType.FlyStatistics));
					buttons.Add(new BindButtonElement("G", ActionType.FlyReset));
					buttons.Add(new BindButtonElement("Tab", ActionType.HideSidePanel));
					buttons.Add(new BindButtonElement("Left Shift", ActionType.ZoomIn));
					buttons.Add(new BindButtonElement("Left Control", ActionType.ZoomOut));
					buttons.Add(new BindButtonElement("T", ActionType.OpenTextChatReserve));
					break;
				case DeviceType.ButtonPanel:
				case DeviceType.Gamepad:
				case DeviceType.Pedals:
					axises.Add(new BindAxisElement("Left Trigger", 0f, 1f, invert: false, ActionType.NavAdditionalPrevBtnL2));
					axises.Add(new BindAxisElement("Right Trigger", 0f, 1f, invert: false, ActionType.NavAdditionalNextBtnR2));
					buttons.Add(new BindButtonElement("Right Shoulder", ActionType.NavNextPage));
					buttons.Add(new BindButtonElement("Left Shoulder", ActionType.NavPrevPage));
					buttons.Add(new BindButtonElement("B", ActionType.NavCancelBtnB));
					buttons.Add(new BindButtonElement("A", ActionType.NavSelectBtnA));
					buttons.Add(new BindButtonElement("X", ActionType.NavAdditional0BtnX));
					buttons.Add(new BindButtonElement("Y", ActionType.NavAdditional1BtnY));
					buttons.Add(new BindButtonElement("Right Stick Button", ActionType.FlyTurboMode));
					buttons.Add(new BindButtonElement("Square", ActionType.FlyChangeFOV));
					buttons.Add(new BindButtonElement("Triangle", ActionType.FlyReset));
					buttons.Add(new BindButtonElement("Left Stick Button", ActionType.FlyHideUI));
					buttons.Add(new BindButtonElement("Left Stick Button", ActionType.NavReport));
					buttons.Add(new BindButtonElement("Cross", ActionType.FlyStatistics));
					buttons.Add(new BindButtonElement("Cross Button", ActionType.FlyStatistics));
					buttons.Add(new BindButtonElement("L3", ActionType.FlyHideUI));
					buttons.Add(new BindButtonElement("L3", ActionType.NavReport));
					buttons.Add(new BindButtonElement("R3", ActionType.FlyTurboMode));
					buttons.Add(new BindButtonElement("Left Stick Button", ActionType.NavBtnExpand));
					buttons.Add(new BindButtonElement("Right Stick Button", ActionType.NavBtnExpandRight));
					buttons.Add(new BindButtonElement("L3", ActionType.NavBtnExpand));
					buttons.Add(new BindButtonElement("R3", ActionType.NavBtnExpandRight));
					buttons.Add(new BindButtonElement("Menu", ActionType.NavBack));
					buttons.Add(new BindButtonElement("Start", ActionType.NavBack));
					buttons.Add(new BindButtonElement("Start", ActionType.NavStart));
					buttons.Add(new BindButtonElement("Menu", ActionType.NavStart));
					axises.Add(new BindAxisElement("Left Stick X", -1f, 1f, invert: false, ActionType.FlyStrafe));
					axises.Add(new BindAxisElement("Left Stick Y", -1f, 1f, invert: false, ActionType.FlyMovement));
					axises.Add(new BindAxisElement("Right Stick X", -1f, 1f, invert: false, ActionType.FlyRotationHorizontal));
					axises.Add(new BindAxisElement("Right Stick Y", -1f, 1f, invert: false, ActionType.FlyRotationVertical));
					axises.Add(new BindAxisElement("R2 Button", 0f, 1f, invert: false, ActionType.FlyHeightUp));
					axises.Add(new BindAxisElement("L2 Button", 0f, 1f, invert: false, ActionType.FlyHeightDown));
					axises.Add(new BindAxisElement("Right Trigger", 0f, 1f, invert: false, ActionType.FlyHeightUp));
					axises.Add(new BindAxisElement("Left Trigger", 0f, 1f, invert: false, ActionType.FlyHeightDown));
					buttons.Add(new BindButtonElement("Right Stick Button", ActionType.FlyTurboMode));
					buttons.Add(new BindButtonElement("X", ActionType.FlyChangeFOV));
					buttons.Add(new BindButtonElement("Y", ActionType.FlyReset));
					buttons.Add(new BindButtonElement("A", ActionType.FlyStatistics));
					buttons.Add(new BindButtonElement("Touchpad Button", ActionType.HideSidePanel));
					buttons.Add(new BindButtonElement("View Button", ActionType.HideSidePanel));
					buttons.Add(new BindButtonElement("View", ActionType.HideSidePanel));
					axises.Add(new BindAxisElement("R2 Button", 0f, 1f, invert: false, ActionType.ZoomIn));
					axises.Add(new BindAxisElement("L2 Button", 0f, 1f, invert: false, ActionType.ZoomOut));
					axises.Add(new BindAxisElement("Right Trigger", 0f, 1f, invert: false, ActionType.ZoomIn));
					axises.Add(new BindAxisElement("Left Trigger", 0f, 1f, invert: false, ActionType.ZoomOut));
					buttons.Add(new BindButtonElement("Y", ActionType.OpenTextChatReserve));
					buttons.Add(new BindButtonElement("Triangle", ActionType.OpenTextChatReserve));
					AddNavigations();
					AddPS4PadElements();
					break;
				case DeviceType.Mouse:
					buttons.Add(new BindButtonElement("Left Mouse Button", ActionType.NavMouseSelect));
					break;
				case DeviceType.Wheel:
					buttons.Add(new BindButtonElement("Left Paddle", ActionType.NavPrevPage));
					buttons.Add(new BindButtonElement("Right Paddle", ActionType.NavNextPage));
					buttons.Add(new BindButtonElement("Option", ActionType.NavBack));
					buttons.Add(new BindButtonElement("Option", ActionType.NavStart));
					buttons.Add(new BindButtonElement("Menu", ActionType.NavBack));
					buttons.Add(new BindButtonElement("Back", ActionType.NavBack));
					buttons.Add(new BindButtonElement("Start", ActionType.NavBack));
					buttons.Add(new BindButtonElement("Start", ActionType.NavStart));
					AddNavigations();
					if (controllerMeta.isXboxCompatible)
					{
						AddThrustMasterXboxElement(InputManager.instance, controllerMeta.rewiredController);
						AddXboxElements();
						buttons.Add(new BindButtonElement("Right Trigger", ActionType.FlyHeightUp));
						buttons.Add(new BindButtonElement("Left Trigger", ActionType.FlyHeightDown));
						buttons.Add(new BindButtonElement("View", ActionType.FlyHideUI));
						buttons.Add(new BindButtonElement("X", ActionType.FlyChangeFOV));
						buttons.Add(new BindButtonElement("Y", ActionType.FlyReset));
						buttons.Add(new BindButtonElement("A", ActionType.FlyStatistics));
						buttons.Add(new BindButtonElement("Y", ActionType.OpenTextChatReserve));
					}
					if (controllerMeta.isPS4PadCompatible)
					{
						AddThrustMasterAndLogitechPS4Element(InputManager.instance, controllerMeta.rewiredController);
						AddPS4PadElements();
						buttons.Add(new BindButtonElement("R2 Button", ActionType.FlyHeightUp));
						buttons.Add(new BindButtonElement("L2 Button", ActionType.FlyHeightDown));
						buttons.Add(new BindButtonElement("L3", ActionType.FlyHideUI));
						buttons.Add(new BindButtonElement("Square", ActionType.FlyChangeFOV));
						buttons.Add(new BindButtonElement("Triangle", ActionType.FlyReset));
						buttons.Add(new BindButtonElement("Cross", ActionType.FlyStatistics));
						buttons.Add(new BindButtonElement("Triangle", ActionType.OpenTextChatReserve));
					}
					break;
				}
			}

			private bool IsNullInputManagerOrController(InputManager inputManager, Controller controller)
			{
				if (!(inputManager == null))
				{
					return controller == null;
				}
				return true;
			}

			private void AddThrustMasterAndLogitechPS4Element(InputManager inputManager, Controller controller)
			{
				if (!IsNullInputManagerOrController(inputManager, controller) && (inputManager.IsThrustmasterLastSteeringDeviceOrController(controller) || inputManager.IsControllerLogitechWheel(controller)))
				{
					buttons.Add(new BindButtonElement("R3", ActionType.NavReport));
					buttons.Add(new BindButtonElement("L3", ActionType.NavBtnExpandRight));
				}
			}

			private void AddThrustMasterXboxElement(InputManager inputManager, Controller controller)
			{
				if (!IsNullInputManagerOrController(inputManager, controller) && inputManager.IsThrustmasterLastSteeringDeviceOrController(controller))
				{
					buttons.Add(new BindButtonElement("RB2", ActionType.NavReport));
					buttons.Add(new BindButtonElement("LB2", ActionType.NavBtnExpandRight));
				}
			}

			private void AddNavigations()
			{
				buttons.Add(new BindButtonElement("D-Pad Up", ActionType.NavDPadUp));
				buttons.Add(new BindButtonElement("D-Pad Down", ActionType.NavDPadDown));
				buttons.Add(new BindButtonElement("D-Pad Right", ActionType.NavDPadRight));
				buttons.Add(new BindButtonElement("D-Pad Left", ActionType.NavDPadLeft));
				axises.Add(new BindAxisElement("Left Stick Y", 0f, 1f, invert: false, ActionType.NavUp));
				axises.Add(new BindAxisElement("Left Stick Y", 0f, -1f, invert: false, ActionType.NavDown));
				axises.Add(new BindAxisElement("Left Stick X", 0f, 1f, invert: false, ActionType.NavRight));
				axises.Add(new BindAxisElement("Left Stick X", 0f, -1f, invert: false, ActionType.NavLeft));
				axises.Add(new BindAxisElement("Right Stick Y", 0f, 1f, invert: false, ActionType.NavAddUp));
				axises.Add(new BindAxisElement("Right Stick Y", 0f, -1f, invert: false, ActionType.NavAddDown));
				axises.Add(new BindAxisElement("Right Stick X", 0f, 1f, invert: false, ActionType.NavAddRight));
				axises.Add(new BindAxisElement("Right Stick X", 0f, -1f, invert: false, ActionType.NavAddLeft));
			}

			private void AddPS4PadElements()
			{
				axises.Add(new BindAxisElement("L2", 0f, 1f, invert: false, ActionType.NavAdditionalPrevBtnL2));
				axises.Add(new BindAxisElement("R2", 0f, 1f, invert: false, ActionType.NavAdditionalNextBtnR2));
				buttons.Add(new BindButtonElement("Circle", GetPS4EnterButtonModeAction(ActionType.NavCancelBtnB)));
				buttons.Add(new BindButtonElement("Cross", GetPS4EnterButtonModeAction(ActionType.NavSelectBtnA)));
				buttons.Add(new BindButtonElement("Square", ActionType.NavAdditional0BtnX));
				buttons.Add(new BindButtonElement("Triangle", ActionType.NavAdditional1BtnY));
				buttons.Add(new BindButtonElement("Circle Button", GetPS4EnterButtonModeAction(ActionType.NavCancelBtnB)));
				buttons.Add(new BindButtonElement("Cross Button", GetPS4EnterButtonModeAction(ActionType.NavSelectBtnA)));
				buttons.Add(new BindButtonElement("L1 Button", ActionType.NavPrevPage));
				buttons.Add(new BindButtonElement("R1 Button", ActionType.NavNextPage));
				buttons.Add(new BindButtonElement("L2 Button", ActionType.NavAdditionalPrevBtnL2));
				buttons.Add(new BindButtonElement("R2 Button", ActionType.NavAdditionalNextBtnR2));
				buttons.Add(new BindButtonElement("Options Button", ActionType.NavBack));
				buttons.Add(new BindButtonElement("Options", ActionType.NavBack));
				buttons.Add(new BindButtonElement("Options Button", ActionType.NavStart));
				buttons.Add(new BindButtonElement("Options", ActionType.NavStart));
				buttons.Add(new BindButtonElement("Left Stick Button", ActionType.NavReport));
				buttons.Add(new BindButtonElement("Left Stick Button", ActionType.NavBtnExpand));
				buttons.Add(new BindButtonElement("Right Stick Button", ActionType.NavBtnExpandRight));
				buttons.Add(new BindButtonElement("L3", ActionType.NavBtnExpand));
				buttons.Add(new BindButtonElement("R3", ActionType.NavBtnExpandRight));
			}

			private void AddXboxElements()
			{
				buttons.Add(new BindButtonElement("B", ActionType.NavCancelBtnB));
				buttons.Add(new BindButtonElement("A", ActionType.NavSelectBtnA));
				buttons.Add(new BindButtonElement("X", ActionType.NavAdditional0BtnX));
				buttons.Add(new BindButtonElement("Y", ActionType.NavAdditional1BtnY));
				buttons.Add(new BindButtonElement("View", ActionType.NavBack));
				buttons.Add(new BindButtonElement("View", ActionType.HideUI));
				buttons.Add(new BindButtonElement("Menu", ActionType.NavStart));
				buttons.Add(new BindButtonElement("Left Stick Button", ActionType.NavBtnExpand));
				buttons.Add(new BindButtonElement("Left Stick Button", ActionType.NavReport));
				buttons.Add(new BindButtonElement("Right Stick Button", ActionType.NavBtnExpandRight));
				axises.Add(new BindAxisElement("Left Trigger", 0f, 1f, invert: false, ActionType.NavAdditionalPrevBtnL2));
				axises.Add(new BindAxisElement("Right Trigger", 0f, 1f, invert: false, ActionType.NavAdditionalNextBtnR2));
				buttons.Add(new BindButtonElement("Left Shoulder", ActionType.NavAdditionalPrevBtnL2));
				buttons.Add(new BindButtonElement("Right Shoulder", ActionType.NavAdditionalNextBtnR2));
			}

			private ActionType GetPS4EnterButtonModeAction(ActionType action)
			{
				return action;
			}

			private void ConstructDefaultKeyboard(Keyboard controller, int version)
			{
				if (version < 1)
				{
					buttons.Add(new BindButtonElement("Left Arrow", ActionType.Turn, BindButtonType.Negative));
					buttons.Add(new BindButtonElement("Right Arrow", ActionType.Turn));
					buttons.Add(new BindButtonElement("Up Arrow", ActionType.Accelerate));
					buttons.Add(new BindButtonElement("Down Arrow", ActionType.Brake));
					buttons.Add(new BindButtonElement("Z", ActionType.Clutch));
					buttons.Add(new BindButtonElement("Space", ActionType.HandBrake));
					buttons.Add(new BindButtonElement("Left Shift", ActionType.GearUp));
					buttons.Add(new BindButtonElement("Left Control", ActionType.GearDown));
					buttons.Add(new BindButtonElement("C", ActionType.NextCamera));
					buttons.Add(new BindButtonElement("V", ActionType.PrevCamera));
					buttons.Add(new BindButtonElement("B", ActionType.RearViewCamera));
					buttons.Add(new BindButtonElement("R", ActionType.RespawnCar));
					buttons.Add(new BindButtonElement("Insert", ActionType.RestartRace));
					buttons.Add(new BindButtonElement("Keypad 7", ActionType.Gear1));
					buttons.Add(new BindButtonElement("Keypad 1", ActionType.Gear2));
					buttons.Add(new BindButtonElement("Keypad 8", ActionType.Gear3));
					buttons.Add(new BindButtonElement("Keypad 2", ActionType.Gear4));
					buttons.Add(new BindButtonElement("Keypad 9", ActionType.Gear5));
					buttons.Add(new BindButtonElement("Keypad 3", ActionType.Gear6));
					buttons.Add(new BindButtonElement("Keypad 4", ActionType.Gear7));
					buttons.Add(new BindButtonElement("Keypad .", ActionType.GearR));
					buttons.Add(new BindButtonElement("F1", ActionType.VRCamReset));
					buttons.Add(new BindButtonElement("F8", ActionType.ShowInputBar));
				}
				if (version < 2)
				{
					buttons.Add(new BindButtonElement("F11", ActionType.HideUI));
					buttons.Add(new BindButtonElement("F9", ActionType.HideMouse));
				}
				if (version < 3)
				{
					buttons.Add(new BindButtonElement("F5", ActionType.VoteMap));
				}
				if (version < 5)
				{
					buttons.Add(new BindButtonElement("F", ActionType.FreeCamMode));
				}
				if (version < 6)
				{
					buttons.Add(new BindButtonElement("H", ActionType.Flares));
				}
				if (version < 8)
				{
					buttons.RemoveAll((BindButtonElement b) => b.IsBindElementName("Z"));
					buttons.Add(new BindButtonElement("Left Alt", ActionType.Clutch));
					buttons.Add(new BindButtonElement("Tab", ActionType.RaceStats));
					buttons.Add(new BindButtonElement("X", ActionType.RaceJoin));
					buttons.Add(new BindButtonElement("Z", ActionType.RaceCreate));
				}
				if (version < 11)
				{
					buttons.RemoveAll((BindButtonElement b) => b.IsBindElementName("X") || b.IsBindElementName("Z") || b.IsBindElementName("F5"));
					buttons.Add(new BindButtonElement("X", ActionType.ShowRadialMenu));
					buttons.Add(new BindButtonElement("1", ActionType.CreateWaypoint));
					buttons.Add(new BindButtonElement("2", ActionType.TeleportToWaypoint));
					buttons.Add(new BindButtonElement("3", ActionType.VoteMap));
					buttons.Add(new BindButtonElement("4", ActionType.RaceJoin));
					buttons.Add(new BindButtonElement("5", ActionType.RaceCreate));
					buttons.Add(new BindButtonElement("6", ActionType.GoToDynostand));
				}
				if (version < 12)
				{
					buttons.Add(new BindButtonElement("T", ActionType.OpenTextChat));
				}
			}

			private void ConstructDefaultMouse(Mouse controller, int version)
			{
				if (version < 5)
				{
					buttons.Add(new BindButtonElement("Right Mouse Button", ActionType.FreeCamView));
				}
			}

			private void ConstructDefaultNoAxis(Controller controller, int version)
			{
			}

			private void ConstructDefaultPad(ControllerWithAxes controller, int version, bool isWheel)
			{
				if (version < 1)
				{
					if (!isWheel)
					{
						axises.Add(new BindAxisElement("Left Stick X", -1f, 1f, invert: false, ActionType.Turn));
						axises.Add(new BindAxisElement("Right Trigger", 0f, 1f, invert: false, ActionType.Accelerate));
						axises.Add(new BindAxisElement("Left Trigger", 0f, 1f, invert: false, ActionType.Brake));
					}
					buttons.Add(new BindButtonElement("A", ActionType.HandBrake));
					buttons.Add(new BindButtonElement("Y", ActionType.NextCamera));
					buttons.Add(new BindButtonElement("B", ActionType.RespawnCar));
					buttons.Add(new BindButtonElement("X", ActionType.Clutch));
					buttons.Add(new BindButtonElement("Right Shoulder", ActionType.GearUp));
					buttons.Add(new BindButtonElement("Left Shoulder", ActionType.GearDown));
					buttons.Add(new BindButtonElement("R2 Button", ActionType.Accelerate));
					buttons.Add(new BindButtonElement("L2 Button", ActionType.Brake));
					buttons.Add(new BindButtonElement("R1 Button", ActionType.GearUp));
					buttons.Add(new BindButtonElement("L1 Button", ActionType.GearDown));
					buttons.Add(new BindButtonElement("Left Stick Button", ActionType.NavReport));
				}
				if (version < 4)
				{
					buttons.Add(new BindButtonElement("D-Pad Up", ActionType.DPadUp));
					buttons.Add(new BindButtonElement("D-Pad Right", ActionType.DPadRight));
					buttons.Add(new BindButtonElement("D-Pad Down", ActionType.DPadDown));
					buttons.Add(new BindButtonElement("D-Pad Left", ActionType.DPadLeft));
					buttons.Add(new BindButtonElement("Back", ActionType.InterfaceSelect));
					buttons.Add(new BindButtonElement("View", ActionType.HideUI));
				}
				if (version < 7)
				{
					buttons.RemoveAll((BindButtonElement b) => b.IsBindElementName("R2 Button"));
					buttons.RemoveAll((BindButtonElement b) => b.IsBindElementName("L2 Button"));
					axises.Add(new BindAxisElement("R2 Button", 0f, 1f, invert: false, ActionType.Accelerate));
					axises.Add(new BindAxisElement("L2 Button", 0f, 1f, invert: false, ActionType.Brake));
					buttons.Add(new BindButtonElement("Cross", ActionType.HandBrake));
					buttons.Add(new BindButtonElement("Triangle", ActionType.NextCamera));
					buttons.Add(new BindButtonElement("Circle", ActionType.RespawnCar));
					buttons.Add(new BindButtonElement("Square", ActionType.Clutch));
				}
				if (version < 8)
				{
					buttons.Add(new BindButtonElement("D-Pad Right", ActionType.VoteMap));
					buttons.Add(new BindButtonElement("D-Pad Left", ActionType.RaceStats));
					buttons.Add(new BindButtonElement("D-Pad Down", ActionType.RaceJoin));
					buttons.Add(new BindButtonElement("D-Pad Up", ActionType.RaceCreate));
				}
				if (version < 11)
				{
					buttons.RemoveAll((BindButtonElement b) => b.IsBindElementName("D-Pad Right") || b.IsBindElementName("D-Pad Down") || b.IsBindElementName("D-Pad Up"));
					buttons.Add(new BindButtonElement("D-Pad Right", ActionType.ShowRadialMenu));
				}
				if (version < 12)
				{
					buttons.Add(new BindButtonElement("D-Pad Up", ActionType.OpenTextChat));
				}
			}

			private void ConstructDefaultPedals(ControllerWithAxes controller, int version)
			{
				if (version < 1)
				{
					axises.Add(new BindAxisElement("Accelerator", 0f, 1f, invert: false, ActionType.Accelerate));
					axises.Add(new BindAxisElement("Brake", 0f, 1f, invert: false, ActionType.Brake));
					axises.Add(new BindAxisElement("Clutch", 0f, 1f, invert: false, ActionType.Clutch));
					axises.Add(new BindAxisElement("HandBrake", 0f, 1f, invert: false, ActionType.HandBrake));
				}
			}

			private void ConstructDefaultWheel(ControllerWithAxes controller, int version)
			{
				if (version < 1)
				{
					axises.Add(new BindAxisElement("Wheel", -1f, 1f, invert: false, ActionType.Turn));
					axises.Add(new BindAxisElement("Accelerator", 0f, 1f, invert: false, ActionType.Accelerate));
					axises.Add(new BindAxisElement("Brake", 0f, 1f, invert: false, ActionType.Brake));
					axises.Add(new BindAxisElement("Clutch", 0f, 1f, invert: false, ActionType.Clutch));
					axises.Add(new BindAxisElement("HandBrake", 0f, 1f, invert: false, ActionType.HandBrake));
					buttons.Add(new BindButtonElement("A", ActionType.HandBrake));
					buttons.Add(new BindButtonElement("Y", ActionType.NextCamera));
					buttons.Add(new BindButtonElement("B", ActionType.RespawnCar));
					buttons.Add(new BindButtonElement("X", ActionType.Clutch));
					buttons.Add(new BindButtonElement("D-Pad Right", ActionType.VoteMap));
					buttons.Add(new BindButtonElement("D-Pad Left", ActionType.RaceStats));
					buttons.Add(new BindButtonElement("D-Pad Down", ActionType.RaceJoin));
					buttons.Add(new BindButtonElement("D-Pad Up", ActionType.RaceCreate));
					buttons.Add(new BindButtonElement("Right Paddle", ActionType.GearUp));
					buttons.Add(new BindButtonElement("Left Paddle", ActionType.GearDown));
					buttons.Add(new BindButtonElement("RSB", ActionType.NextCamera));
					buttons.Add(new BindButtonElement("LSB", ActionType.PrevCamera));
				}
			}

			private void TryMapShifter(Controller controller, int version)
			{
				if (version < 1)
				{
					buttons.Add(new BindButtonElement("Shifter 1", ActionType.Gear1));
					buttons.Add(new BindButtonElement("Shifter 2", ActionType.Gear2));
					buttons.Add(new BindButtonElement("Shifter 3", ActionType.Gear3));
					buttons.Add(new BindButtonElement("Shifter 4", ActionType.Gear4));
					buttons.Add(new BindButtonElement("Shifter 5", ActionType.Gear5));
					buttons.Add(new BindButtonElement("Shifter 6", ActionType.Gear6));
					buttons.Add(new BindButtonElement("Shifter 7", ActionType.Gear7));
					buttons.Add(new BindButtonElement("Shifter 8", ActionType.GearR));
				}
			}
		}

		private const int VERSION = 12;

		[JsonIgnore]
		public Parameters Params = new Parameters();

		public Dictionary<string, DeviceProfile> devices = new Dictionary<string, DeviceProfile>();

		[JsonSerialize]
		private string paramsProxy
		{
			get
			{
				try
				{
					return JSON.Serialize(Params);
				}
				catch
				{
					return "";
				}
			}
			set
			{
				try
				{
					JSON.Parse(value, Params);
				}
				catch
				{
					GameLog.Print(AlertLevel.Error, "System", "Failure with parse InputSettings.Params! Parsing aborted");
				}
			}
		}

		public static Controller.Button GetInputButton(Controller controller, BindElement bind)
		{
			if (bind.elementActionResource == null)
			{
				bind.UpdateElementActionResource();
			}
			for (int i = 0; i < controller.buttonCount; i++)
			{
				if (bind.IsBindElementName(controller.ButtonElementIdentifiers[i].name))
				{
					return controller.Buttons[i];
				}
			}
			return null;
		}

		public static Controller.Axis GetInputAxis(ControllerWithAxes controller, BindElement bind)
		{
			if (bind.elementActionResource == null)
			{
				bind.UpdateElementActionResource();
			}
			for (int i = 0; i < controller.axisCount; i++)
			{
				if (bind.IsBindElementName(controller.AxisElementIdentifiers[i].name))
				{
					return controller.Axes[i];
				}
			}
			return null;
		}

		public static Controller.Axis GetInputAxis(ControllerWithAxes controller, BindAxisGamepadElement bind)
		{
			if (bind.elementActionResource == null)
			{
				bind.UpdateElementActionResource();
			}
			for (int i = 0; i < controller.axisCount; i++)
			{
				if (bind.elementNameY.Equals(controller.AxisElementIdentifiers[i].name, StringComparison.InvariantCultureIgnoreCase))
				{
					return controller.Axes[i];
				}
			}
			return null;
		}
	}
}
