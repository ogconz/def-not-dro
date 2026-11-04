using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ArrayExtension;
using Binding.Components;
using DG.Tweening;
using DI;
using GameOverlay;
using Rewired;
using Rewired.ControllerExtensions;
using Rewired.Platforms.XboxOne;
using SimpleJSON;
using UnityEngine;
using UnityEngine.UI;

namespace GameInput
{
	public class InputManager : MonoBehaviour
	{
		public class ControllerElementDescriptor
		{
			public string name;

			public Controller.Element element;
		}

		private WheelForceFeedbackSupport m_wheelFeedback;

		public bool dbgLogActions;

		public GameObject dbgInpugInfo;

		private GameObject m_dbgInpugInfoInstance;

		private BindElement[] m_activeBinds = new BindElement[0];

		private BindButtonElement[] m_activeBindButtons = new BindButtonElement[0];

		private BindAxisElement[] m_activeBindAxises = new BindAxisElement[0];

		private Action[] m_actions = new Action[0];

		private ActionType[] m_duplicateActions;

		private Joystick m_currentVibroController;

		private const float VIBRO_IMPACT_IMPULSE_FACTOR = 2f;

		private const float VIBRO_IMPACT_IMPULSE_FADE = 3f;

		private float m_impactVibroImpulse;

		private float m_currentVibroValueL;

		private float m_currentVibroValueR;

		private List<Joystick> m_vibroControllers = new List<Joystick>();

		private ControllerMetaInfo m_curSteeringDevice;

		private ControllerMetaInfo m_curFXDevice;

		private DeviceType m_CurSteeringDeviceType;

		private bool m_isFXEnabled = true;

		public static InputSettings settings = new InputSettings();

		public readonly Dictionary<Controller, string> activeControllers = new Dictionary<Controller, string>();

		public Action<Controller> onDeviceAdd;

		public Action<Controller> onDeviceRemove;

		public DeviceType lastActionDevice = DeviceType.Unknown;

		public bool lastActionByButton;

		public Action<Controller> onChangeActiveDevice;

		private static MonoBehaviour m_inputLockedObject = null;

		private static int m_lastChangeFocusFrame;

		private int m_pollingCount;

		private const string Logitec = "logitec";

		private const string Thrust248 = "t248";

		private List<ControllerElementDescriptor> m_controllerElements = new List<ControllerElementDescriptor>();

		private static GraphicsQuality m_graphicsQuality = null;

		private float m_cursorMoveTime;

		private bool m_zeroVibroSetted;

		private float m_wheelLostTime;

		private const float WHEEL_LOST_COOLDOWN = 2f;

		[Header("Input FX Params")]
		public float onClickActionTimeOut = 0.1f;

		private static int m_blockInputFrame = 0;

		public float fxEfffectTime = 0.2f;

		public float fxEndFade = 0.1f;

		public float fxEndScale = 1.5f;

		private WaitForSecondsRealtime m_clickTimeout = new WaitForSecondsRealtime(0f);

		public static InputManager instance { get; private set; }

		public static bool steamOverlayActive { private get; set; }

		public bool isLastSteeringIsAxis { get; private set; }

		public ControllerMetaInfo CurSteeringDevice
		{
			get
			{
				return m_curSteeringDevice;
			}
			private set
			{
				m_curSteeringDevice = value;
				m_CurSteeringDeviceType = ((m_curSteeringDevice != null) ? m_curSteeringDevice.deviceType : DeviceType.Unknown);
			}
		}

		public ControllerMetaInfo CurFXDevice
		{
			get
			{
				return m_curFXDevice;
			}
			set
			{
				m_curFXDevice = value;
			}
		}

		public DeviceType CurSteeringDeviceType => m_CurSteeringDeviceType;

		public int controllersListGeneration { get; private set; }

		public bool ignoreDuplicateActions { get; set; }

		public Controller lastController { get; private set; }

		private static bool m_isChangedFocus => Time.frameCount - m_lastChangeFocusFrame < 2;

		public bool pollingMode
		{
			get
			{
				return m_pollingCount > 0;
			}
			set
			{
				m_pollingCount = (value ? 3 : 0);
			}
		}

		private void OnEnable()
		{
			TickManager.RegisterFixedUpdate(UpdateActions, 0, "input");
		}

		private void OnDisable()
		{
			TickManager.UnregisterFixedUpdate(UpdateActions);
		}

		public float GetActionValue(ActionType type)
		{
			lastActionDevice = m_actions[(uint)type].GetLastDeviceType();
			lastActionByButton = m_actions[(uint)type].lastActiveBind is BindButtonElement;
			return m_actions[(uint)type].value;
		}

		public static int GetUserId(Controller controller)
		{
			return 0;
		}

		public Action GetActionInfo(ActionType typeAction)
		{
			return m_actions[(uint)typeAction];
		}

		public static void TryShowBindStarterIgnitionMsg(bool needShow, float displayTime = 10f, Controller controller = null, Action<ActionType, bool> callback = null)
		{
			instance.ShowBindStarterIgnitionMsgIfNeed(needShow, displayTime, controller, callback);
		}

		private void ShowBindStarterIgnitionMsgIfNeed(bool canShowMsg, float displayTime, Controller controller = null, Action<ActionType, bool> callback = null)
		{
			bool flag = IsActionBindedForAnyPluggedDevice(ActionType.Ignition);
			bool flag2 = IsActionBindedForAnyPluggedDevice(ActionType.Starter);
			bool flag3 = !flag || !flag2;
			flag3 &= !settings.Params.automaticClutchEnabled || settings.Params.holdIgnition;
			if (flag3 && canShowMsg)
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.need_bind_ignition_starter.Localized(), displayTime);
			}
			callback.SafeCall(flag ? ActionType.Starter : ActionType.Ignition, flag3);
		}

		private bool IsActionBindedForAnyPluggedDevice(ActionType actionType)
		{
			Action actionInfo = GetActionInfo(actionType);
			if (actionInfo == null || actionInfo.activeBinds == null)
			{
				return true;
			}
			foreach (Controller controller in activeControllers.Keys)
			{
				if (actionInfo.activeBinds.Find((BindElement bindElem) => bindElem.deviceInfo.hardwareIdentifier.Equals(controller.hardwareIdentifier)) != null)
				{
					return true;
				}
			}
			return false;
		}

		public BindElement GetActionInfoByDevice(ActionType typeAction, Controller controller, BindElement prevBind = null)
		{
			bool flag = false;
			BindElement bindElement = null;
			BindElement bindElement2 = null;
			DeviceType deviceType = controller.GetMetaInfo().deviceType;
			Action actionInfo = GetActionInfo(typeAction);
			if (actionInfo == null)
			{
				return null;
			}
			if (controller != null)
			{
				foreach (BindElement activeBind in actionInfo.activeBinds)
				{
					if (activeBind.deviceInfo.hardwareIdentifier.Equals(controller.hardwareIdentifier))
					{
						return activeBind;
					}
					if (activeBind == prevBind)
					{
						flag = true;
					}
					if (activeBind.deviceInfo.rewiredController.type == controller.type)
					{
						bindElement = activeBind;
					}
					if (activeBind.deviceInfo.deviceType == deviceType)
					{
						bindElement2 = activeBind;
					}
				}
			}
			if (actionInfo.activeBinds.Count > 0)
			{
				if (flag)
				{
					return prevBind;
				}
				if (bindElement2 != null)
				{
					return bindElement2;
				}
				if (bindElement != null)
				{
					return bindElement;
				}
				return actionInfo.activeBinds[0];
			}
			return null;
		}

		public bool IsLastActiveJoystickAxis2D(int cameraOrbitStick, out Vector2 axis)
		{
			Controller controller = instance.lastController;
			if (controller != null && controller.type == ControllerType.Joystick && cameraOrbitStick > -1 && IsNativePlatformController(controller) && controller is Joystick joystick && joystick.GetMetaInfo().deviceType != DeviceType.Pedals)
			{
				axis = joystick.GetAxis2D(cameraOrbitStick);
				return true;
			}
			axis = Vector2.zero;
			return false;
		}

		public void ResetMetaDeviceData()
		{
			foreach (Controller key in activeControllers.Keys)
			{
				key.GetMetaInfo().deviceData = -1;
			}
			CurSteeringDevice = null;
			CurFXDevice = null;
		}

		public string GetActionBinds(ActionType typeAction)
		{
			Action obj = m_actions[(uint)typeAction];
			string text = string.Empty;
			List<BindElement> activeBinds = obj.activeBinds;
			int count = obj.activeBinds.Count;
			if (count > 0)
			{
				text += activeBinds[0].elementId;
				for (int i = 1; i < count - 1; i++)
				{
					BindElement bindElement = activeBinds[i];
					if (!text.Contains(bindElement.elementId))
					{
						text = text + ", " + bindElement.elementId;
					}
				}
			}
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
			return "NONE";
		}

		public static bool IsSupportFFB(Controller controller)
		{
			if ((bool)instance && (bool)instance.m_wheelFeedback && controller != null)
			{
				return instance.m_wheelFeedback.IsCurrentrlySupportFFB(controller);
			}
			return false;
		}

		public BindElement GetActionActiveBind(ActionType type)
		{
			return m_actions[(uint)type].lastActiveBind;
		}

		public BindElement GetActionBindOfLastInput(ActionType type, bool allowFallbackToKeyboard = false)
		{
			Action obj = m_actions[(uint)type];
			BindElement bindElement = null;
			foreach (BindElement activeBind in obj.activeBinds)
			{
				if (lastController.type == activeBind.deviceInfo.rewiredType)
				{
					return activeBind;
				}
				if (bindElement == null && activeBind.deviceInfo.rewiredType == ControllerType.Keyboard)
				{
					bindElement = activeBind;
				}
			}
			if (!allowFallbackToKeyboard)
			{
				return null;
			}
			return bindElement;
		}

		public DeviceType GetActionDeviceType(ActionType type)
		{
			return m_actions[(uint)type].GetLastDeviceType();
		}

		public bool IsActionHold(ActionType type)
		{
			int num = (int)type;
			lastActionDevice = m_actions[num].GetLastDeviceType();
			lastActionByButton = m_actions[num].lastActiveBind is BindButtonElement;
			bool flag = m_actions[num].lastActiveBind != null && m_actions[num].lastActiveBind.IsHold();
			if (!flag && !ignoreDuplicateActions)
			{
				type = m_duplicateActions[num];
				if (type != ActionType.ErrorAction)
				{
					return IsActionHold(type);
				}
			}
			return flag;
		}

		public bool IsActionPressed(ActionType type)
		{
			int num = (int)type;
			lastActionDevice = m_actions[num].GetLastDeviceType();
			lastActionByButton = m_actions[num].lastActiveBind is BindButtonElement;
			bool flag = m_actions[num].lastActiveBind != null && m_actions[num].lastActiveBind.IsPressed();
			if (!flag && !ignoreDuplicateActions)
			{
				type = m_duplicateActions[num];
				if (type != ActionType.ErrorAction)
				{
					return IsActionPressed(type);
				}
			}
			return flag;
		}

		public bool IsActionReleased(ActionType type)
		{
			int num = (int)type;
			lastActionDevice = m_actions[num].GetLastDeviceType();
			lastActionByButton = m_actions[num].lastActiveBind is BindButtonElement;
			bool flag = m_actions[num].lastActiveBind != null && m_actions[num].lastActiveBind.IsReleased();
			if (!flag && !ignoreDuplicateActions)
			{
				type = m_duplicateActions[num];
				if (type != ActionType.ErrorAction)
				{
					return IsActionReleased(type);
				}
			}
			return flag;
		}

		public void SetDeviceDisableFFB(ControllerMetaInfo device, bool disableFFB)
		{
			if (device.disableFFB != disableFFB)
			{
				if (m_wheelFeedback != null)
				{
					StopAllInputFX();
					m_wheelFeedback.FeedbackActive = false;
				}
				device.disableFFB = disableFFB;
				if (device == CurFXDevice)
				{
					CurFXDevice = null;
				}
			}
		}

		public Action[] GetActions()
		{
			return m_actions;
		}

		public BindElement GetBind(Controller controller, ControllerElementType elementType, string elementIdentifier, bool isNegative = false)
		{
			if (controller == null)
			{
				return null;
			}
			if (string.IsNullOrEmpty(elementIdentifier))
			{
				return null;
			}
			string key = activeControllers[controller];
			InputSettings.DeviceProfile deviceProfile = settings.devices[key];
			BindElement bindElement = null;
			if (elementType == ControllerElementType.Button)
			{
				return deviceProfile.buttons.Find((BindButtonElement e) => !e.IsSystemAction() && e.IsBindElementName(elementIdentifier));
			}
			return deviceProfile.axises.Find((BindAxisElement e) => !e.IsSystemAction() && e.IsBindElementName(elementIdentifier));
		}

		public void RemoveElement(Controller controller, ControllerElementType elementType, ActionType actionType, string elementIdentifier)
		{
			string key = activeControllers[controller];
			InputSettings.DeviceProfile deviceProfile = settings.devices[key];
			if (elementType == ControllerElementType.Button)
			{
				deviceProfile.buttons.RemoveAll((BindButtonElement e) => e.IsBindElementName(elementIdentifier) && !e.IsSystemAction() && e.bindedAction == actionType);
			}
			else
			{
				deviceProfile.axises.RemoveAll((BindAxisElement e) => e.IsBindElementName(elementIdentifier) && !e.IsSystemAction() && e.bindedAction == actionType);
			}
			UpdateBinds();
		}

		public bool IsLogitechWheelLastSteeingDevice()
		{
			if (CurSteeringDeviceType == DeviceType.Wheel && CurSteeringDevice != null)
			{
				if (!CurSteeringDevice.name.ToLower().Contains("logitec"))
				{
					return CurSteeringDevice.hardwareIdentifier.ToLower().Contains("logitec");
				}
				return true;
			}
			return false;
		}

		public bool IsControllerLogitechWheel(Controller controller)
		{
			if (controller == null)
			{
				return false;
			}
			ControllerMetaInfo metaInfo = controller.GetMetaInfo();
			if (metaInfo == null)
			{
				return false;
			}
			if (!metaInfo.name.ToLower().Contains("logitec"))
			{
				return metaInfo.hardwareIdentifier.ToLower().Contains("logitec");
			}
			return true;
		}

		public bool IsThrusmasterT248LastSteeringDeviceOrController(Controller controller = null)
		{
			if (controller != null)
			{
				if (!controller.name.ToLower().Contains("t248"))
				{
					return controller.hardwareIdentifier.ToLower().Contains("t248");
				}
				return true;
			}
			if (CurSteeringDeviceType == DeviceType.Wheel && CurSteeringDevice != null)
			{
				if (!CurSteeringDevice.name.ToLower().Contains("t248"))
				{
					return CurSteeringDevice.hardwareIdentifier.ToLower().Contains("t248");
				}
				return true;
			}
			return false;
		}

		public bool IsThrustmasterLastSteeringDeviceOrController(Controller controller = null)
		{
			if (controller != null)
			{
				ControllerMetaInfo metaInfo = controller.GetMetaInfo();
				if (metaInfo != null)
				{
					if (!metaInfo.name.ToLower().Contains("thrustmaster"))
					{
						return metaInfo.hardwareIdentifier.ToLower().Contains("thrustmaster");
					}
					return true;
				}
			}
			if (CurSteeringDeviceType == DeviceType.Wheel && CurSteeringDevice != null)
			{
				if (!CurSteeringDevice.name.ToLower().Contains("thrustmaster"))
				{
					return CurSteeringDevice.hardwareIdentifier.ToLower().Contains("thrustmaster");
				}
				return true;
			}
			return false;
		}

		public SteeringMode GetLastSteeringMode()
		{
			if (CurSteeringDeviceType == DeviceType.Wheel)
			{
				if (!IsLogitechWheelLastSteeingDevice())
				{
					return SteeringMode.Wheel;
				}
				return SteeringMode.LogitechWheel;
			}
			if (isLastSteeringIsAxis)
			{
				return SteeringMode.Axis;
			}
			return SteeringMode.Buttons;
		}

		public bool IsNativePlatformController(Controller controller)
		{
			return Overlay.instance.IsNativePlatformController(controller);
		}

		public void ClearAction(Controller controller, Action action)
		{
			string key = activeControllers[controller];
			InputSettings.DeviceProfile deviceProfile = settings.devices[key];
			deviceProfile.buttons.RemoveAll((BindButtonElement e) => e.bindedAction == action.actionType);
			deviceProfile.axises.RemoveAll((BindAxisElement e) => e.bindedAction == action.actionType);
			UpdateBinds();
		}

		public BindElement SetBind(Controller controller, ControllerElementType elementType, Action action, string elementIdentifier, bool isNegative = false)
		{
			if (Action.IsSystemAction(action.actionType))
			{
				return null;
			}
			if (controller == null)
			{
				foreach (InputSettings.DeviceProfile value in settings.devices.Values)
				{
					value.axises.RemoveAll((BindAxisElement e) => e.bindedAction == action.actionType);
				}
				UpdateBinds();
				return null;
			}
			if (string.IsNullOrEmpty(elementIdentifier))
			{
				BindButtonType bType = ((!isNegative) ? BindButtonType.Positive : BindButtonType.Negative);
				InputSettings.DeviceProfile deviceProfile = settings.devices[activeControllers[controller]];
				if (elementType == ControllerElementType.Button)
				{
					deviceProfile.buttons.RemoveAll((BindButtonElement e) => e.bindedAction == action.actionType && e.bindType == bType && CompareControlElementName(e.elementName, elementIdentifier));
				}
				else
				{
					deviceProfile.axises.RemoveAll((BindAxisElement e) => e.bindedAction == action.actionType);
				}
				UpdateBinds();
				return null;
			}
			BindButtonType bindType = ((!isNegative) ? BindButtonType.Positive : BindButtonType.Negative);
			string key = activeControllers[controller];
			InputSettings.DeviceProfile deviceProfile2 = settings.devices[key];
			InputSettings.DeviceProfile deviceProfile3 = null;
			if (controller.type == ControllerType.Keyboard || controller.type == ControllerType.Mouse)
			{
				deviceProfile3 = TryGetOppositeMouseOrKeyboardProfileByUsingAndBind(controller.type, action);
			}
			bool needSkipControllerCondition = deviceProfile3 != null;
			BindElement bindElement = null;
			if (elementType == ControllerElementType.Button)
			{
				bindElement = deviceProfile2.buttons.Find((BindButtonElement e) => e.bindedAction == action.actionType && e.IsBindElementName(elementIdentifier) && e.bindType == bindType);
				deviceProfile2.buttons.RemoveAll((BindButtonElement e) => e.IsBindElementName(elementIdentifier) && !e.IsSystemAction());
				Predicate<BindButtonElement> predicate = (BindButtonElement e) => (e.deviceInfo.rewiredController == controller || needSkipControllerCondition) && e.bindedAction == action.actionType && e.bindType == bindType;
				Predicate<BindAxisElement> predicate2 = (BindAxisElement e) => (e.deviceInfo.rewiredController == controller || needSkipControllerCondition) && e.bindedAction == action.actionType;
				InputSettings.DeviceProfile deviceProfile4 = deviceProfile3 ?? deviceProfile2;
				List<BindButtonElement> buttons = deviceProfile4.buttons;
				for (int num = buttons.Count - 1; num >= 0; num--)
				{
					if (predicate(buttons[num]))
					{
						buttons.RemoveAt(num);
					}
				}
				List<BindAxisElement> axises = deviceProfile4.axises;
				for (int num2 = axises.Count - 1; num2 >= 0; num2--)
				{
					if (predicate2(axises[num2]))
					{
						axises.RemoveAt(num2);
					}
				}
				if (bindElement == null)
				{
					if (controller.type == ControllerType.Mouse && action.actionType == ActionType.HideUI && elementIdentifier.ToLower() == "left mouse button")
					{
						GUICommonGodVoice.ShowText("UI Hide action cant be assigned to the LBM");
						return null;
					}
					bindElement = new BindButtonElement(elementIdentifier, action.actionType, bindType);
					deviceProfile2.buttons.Add(bindElement as BindButtonElement);
				}
				else
				{
					bindElement = null;
				}
			}
			else
			{
				bindElement = deviceProfile2.axises.Find((BindAxisElement e) => e.bindedAction == action.actionType && e.IsBindElementName(elementIdentifier));
				deviceProfile2.axises.RemoveAll((BindAxisElement e) => e.IsBindElementName(elementIdentifier) && !e.IsSystemAction() && IsNeedRemoveGearChangeElemBindBySelectedAction(elementType, action.actionType, e.bindedAction));
				if (action.actionType == ActionType.Turn)
				{
					deviceProfile2.buttons.RemoveAll((BindButtonElement e) => !e.IsSystemAction() && e.bindedAction == ActionType.Turn);
					deviceProfile3?.buttons.RemoveAll((BindButtonElement e) => !e.IsSystemAction() && e.bindedAction == ActionType.Turn);
				}
				Predicate<BindButtonElement> predicate3 = (BindButtonElement e) => (e.deviceInfo.rewiredController == controller || needSkipControllerCondition) && e.bindedAction == action.actionType && e.bindType == bindType;
				Predicate<BindAxisElement> predicate4 = (BindAxisElement e) => (e.deviceInfo.rewiredController == controller || needSkipControllerCondition) && e.bindedAction == action.actionType;
				InputSettings.DeviceProfile deviceProfile5 = deviceProfile3 ?? deviceProfile2;
				List<BindButtonElement> buttons2 = deviceProfile5.buttons;
				for (int num3 = buttons2.Count - 1; num3 >= 0; num3--)
				{
					if (predicate3(buttons2[num3]))
					{
						buttons2.RemoveAt(num3);
					}
				}
				List<BindAxisElement> axises2 = deviceProfile5.axises;
				for (int num4 = axises2.Count - 1; num4 >= 0; num4--)
				{
					if (predicate4(axises2[num4]))
					{
						axises2.RemoveAt(num4);
					}
				}
				if (bindElement == null)
				{
					ActionType actionType = action.actionType;
					float minValue = ((action.valueType == ActionValueType.Full) ? (-1f) : 0f);
					float maxValue = 1f;
					string turnActionOrtogonalAxisName = GetTurnActionOrtogonalAxisName(controller.GetMetaInfo().deviceType, elementIdentifier, actionType);
					if (!string.IsNullOrEmpty(turnActionOrtogonalAxisName))
					{
						bindElement = new BindAxisGamepadElement(elementIdentifier, turnActionOrtogonalAxisName, minValue, maxValue, invert: false, actionType);
						deviceProfile2.axises.Add(bindElement as BindAxisGamepadElement);
					}
					else
					{
						if (controller.type == ControllerType.Mouse)
						{
							TryOverrideMouseAxisElementBorderValues(elementIdentifier, actionType, ref minValue, ref maxValue);
						}
						bindElement = new BindAxisElement(elementIdentifier, minValue, maxValue, invert: false, actionType);
						deviceProfile2.axises.Add(bindElement as BindAxisElement);
					}
				}
				else
				{
					bindElement = null;
				}
			}
			UpdateBinds();
			return bindElement;
		}

		private void TryOverrideMouseAxisElementBorderValues(string elementIdentifier, ActionType actionType, ref float minValue, ref float maxValue)
		{
			bool flag;
			if ((flag = elementIdentifier.ToLower().Contains("mouse horizontal")) || elementIdentifier.ToLower().Contains("mouse vertical"))
			{
				maxValue = (flag ? Screen.width : Screen.height);
				maxValue /= 3000f;
				minValue = 0f - maxValue;
			}
			else if (elementIdentifier.ToLower().Contains("mouse wheel"))
			{
				switch (actionType)
				{
				case ActionType.GearUp:
					minValue = 0f;
					maxValue = 1f;
					break;
				case ActionType.GearDown:
					minValue = -1f;
					maxValue = 0f;
					break;
				}
			}
		}

		private InputSettings.DeviceProfile TryGetOppositeMouseOrKeyboardProfileByUsingAndBind(ControllerType controllerType, Action action)
		{
			bool isMouseController = controllerType == ControllerType.Mouse;
			bool isKeyboardController = controllerType == ControllerType.Keyboard;
			if (action.activeBinds.Exists((BindElement bindElem) => (bindElem.deviceInfo.deviceType == DeviceType.Mouse && isMouseController) || (bindElem.deviceInfo.deviceType == DeviceType.Keyboard && isKeyboardController)))
			{
				return null;
			}
			foreach (KeyValuePair<string, InputSettings.DeviceProfile> device in settings.devices)
			{
				InputSettings.DeviceProfile value = device.Value;
				if (value != null)
				{
					DeviceType deviceType = value.deviceType;
					if ((deviceType == DeviceType.Keyboard && isMouseController) || (deviceType == DeviceType.Mouse && isKeyboardController))
					{
						return value;
					}
				}
			}
			return null;
		}

		private bool IsNeedRemoveGearChangeElemBindBySelectedAction(ControllerElementType elementType, ActionType selectedAction, ActionType elemAction)
		{
			if (elementType != ControllerElementType.Axis)
			{
				return false;
			}
			if (Action.IsGearChangeAction(selectedAction))
			{
				return !Action.IsGearChangeAction(elemAction);
			}
			return true;
		}

		private string GetTurnActionOrtogonalAxisName(DeviceType deviceType, string axis, ActionType actionType)
		{
			return string.Empty;
		}

		public void UpdateBinds()
		{
			m_activeBinds = new BindElement[0];
			DeviceType deviceType = DeviceType.Unknown;
			foreach (Controller key in activeControllers.Keys)
			{
				if (!IsNativePlatformController(key))
				{
					continue;
				}
				BindController(key);
				if (!settings.Params.steeringDeviceType.HasValue)
				{
					ControllerMetaInfo metaInfo = key.GetMetaInfo();
					if (metaInfo != null && metaInfo.deviceType == DeviceType.Wheel)
					{
						settings.Params.steeringDeviceType = DeviceType.Wheel;
					}
					else if (metaInfo != null && metaInfo.deviceType == DeviceType.Gamepad)
					{
						deviceType = DeviceType.Gamepad;
					}
				}
			}
			if (!settings.Params.steeringDeviceType.HasValue)
			{
				settings.Params.steeringDeviceType = ((deviceType != DeviceType.Unknown) ? deviceType : DeviceType.Keyboard);
			}
			RecalculateButtonsAndAxises();
		}

		public List<ControllerElementDescriptor> GetControllerElementsType(Controller controller, ControllerElementType elementType)
		{
			m_controllerElements.Clear();
			int num = 0;
			switch (elementType)
			{
			case ControllerElementType.Button:
				foreach (Controller.Button button in controller.Buttons)
				{
					m_controllerElements.Add(new ControllerElementDescriptor
					{
						element = button,
						name = controller.ButtonElementIdentifiers[num].name
					});
					num++;
				}
				break;
			case ControllerElementType.Axis:
				if (!(controller is ControllerWithAxes controllerWithAxes))
				{
					return null;
				}
				foreach (Controller.Axis axis in controllerWithAxes.Axes)
				{
					m_controllerElements.Add(new ControllerElementDescriptor
					{
						element = axis,
						name = controllerWithAxes.AxisElementIdentifiers[num].name
					});
					num++;
				}
				break;
			}
			return m_controllerElements;
		}

		public void PlayVibro(float leftMotorValue, float rightMotorValue)
		{
			m_currentVibroValueL = leftMotorValue;
			m_currentVibroValueR = rightMotorValue;
		}

		public void PlayImpactVibro(float value)
		{
			m_impactVibroImpulse = value * 2f;
		}

		public void StopAllInputFX()
		{
			m_isFXEnabled = false;
			StopAllVibro();
			m_wheelFeedback.StopFFBEffects();
			m_wheelFeedback.SetEnabledFFB(flag: false);
		}

		public void EnableAllInputFX()
		{
			m_isFXEnabled = true;
			m_wheelFeedback.SetEnabledFFB(flag: true);
			CurSteeringDevice = null;
			CurFXDevice = null;
		}

		public void StopAllVibro()
		{
			m_impactVibroImpulse = 0f;
			m_currentVibroValueL = (m_currentVibroValueR = 0f);
			foreach (Joystick vibroController in m_vibroControllers)
			{
				vibroController.StopVibration();
			}
		}

		public void SetCursorVisibility(bool showCursor, bool lockState = false)
		{
			Cursor.visible = showCursor;
			Cursor.lockState = (lockState ? CursorLockMode.Locked : CursorLockMode.None);
		}

		private void Awake()
		{
			controllersListGeneration = -1;
			instance = this;
			FocusState.onChanged = (Action<bool>)Delegate.Combine(FocusState.onChanged, new Action<bool>(OnFocusChange));
			InitActionDublicates();
			m_actions = Action.GetActionDefines();
			FilterPlatformActions();
			ReInput.ControllerConnectedEvent += AddController;
			ReInput.ControllerPreDisconnectEvent += PreDisconnectController;
			UpdateControllers(null, null);
			Load();
			UpdateBinds();
			GameEvents.OnGameEngineStop += (GameEventHandler<GameEvents.EventInfo>)delegate
			{
				FocusState.onChanged = (Action<bool>)Delegate.Remove(FocusState.onChanged, new Action<bool>(OnFocusChange));
				ReInput.ControllerConnectedEvent -= AddController;
				ReInput.ControllerPreDisconnectEvent -= PreDisconnectController;
				Save();
			};
			lastController = ReInput.controllers.GetLastActiveController();
			ReInput.controllers.AddLastActiveControllerChangedDelegate(OnDeviceChanged);
			m_wheelFeedback = DependencyInjector.Resolve<WheelForceFeedbackSupport>();
			m_wheelFeedback.Init(this);
			m_wheelFeedback.OnControllerListChanged(null, null);
		}

		private void FilterPlatformActions()
		{
		}

		private void InitActionDublicates()
		{
			m_duplicateActions = new ActionType[Enum.GetNames(typeof(ActionType)).Length];
			m_duplicateActions[36] = ActionType.NavDPadUp;
			m_duplicateActions[37] = ActionType.NavDPadDown;
			m_duplicateActions[38] = ActionType.NavDPadLeft;
			m_duplicateActions[39] = ActionType.NavDPadRight;
		}

		[Conditional("UNITY_EDITOR")]
		private void CheckActionsCorrectness()
		{
			for (int i = 0; i < m_actions.Length; i++)
			{
				Action action = m_actions[i];
				if ((int)action.actionType != i && action.actionType != ActionType.ErrorAction)
				{
					UnityEngine.Debug.LogErrorFormat("Action {0} has invalid index in m_action (at position of {1})", action.actionType, (ActionType)i);
				}
			}
		}

		private string GetCfgFilePath()
		{
			return Path.Combine(Application.persistentDataPath, GetCfgStorageKey());
		}

		private string GetCfgStorageKey()
		{
			return "input_" + Overlay.GetPlayerHashString() + ".cfg";
		}

		private void Load()
		{
			string text = GetCfgFilePath();
			if (!File.Exists(text))
			{
				text = Path.Combine(Application.persistentDataPath, "input.cfg");
			}
			LoadConfig(text);
		}

		private void LoadConfig(string fname)
		{
			LoadConfigFromFileSystem(GetCfgFilePath());
		}

		private void LoadConfigFromFileSystem(string fname)
		{
			if (!File.Exists(fname))
			{
				return;
			}
			try
			{
				using StreamReader streamReader = new StreamReader(fname);
				JSON.Parse(streamReader.ReadToEnd(), settings, skipErrors: true);
			}
			catch
			{
				GameLog.Print(AlertLevel.Error, "InputManager", "Loading input config is failed - loading default data");
				settings = new InputSettings();
			}
			finally
			{
				if (settings.devices == null)
				{
					settings.devices = new Dictionary<string, InputSettings.DeviceProfile>();
				}
			}
		}

		private void PatchGamepadStickBindings()
		{
			foreach (KeyValuePair<string, InputSettings.DeviceProfile> device in settings.devices)
			{
				List<BindAxisElement> axises = device.Value.axises;
				for (int i = 0; i < axises.Count; i++)
				{
					BindAxisElement bindAxisElement = axises[i];
					if (bindAxisElement != null)
					{
						string elementName = bindAxisElement.elementName;
						ActionType bindedAction = bindAxisElement.bindedAction;
						string turnActionOrtogonalAxisName = GetTurnActionOrtogonalAxisName(device.Value.deviceType, elementName, bindedAction);
						if (!string.IsNullOrEmpty(turnActionOrtogonalAxisName))
						{
							axises[i] = new BindAxisGamepadElement(elementName, turnActionOrtogonalAxisName, bindAxisElement.minValue, bindAxisElement.maxValue, bindAxisElement.invert, bindedAction);
						}
					}
				}
			}
		}

		public void Save()
		{
			foreach (KeyValuePair<Controller, string> activeController in activeControllers)
			{
				if (settings.devices.TryGetValue(activeController.Value, out var value))
				{
					ControllerMetaInfo metaInfo = activeController.Key.GetMetaInfo();
					if (metaInfo != null)
					{
						value.disableFFB = metaInfo.disableFFB;
						value.deviceType = metaInfo.deviceType;
					}
				}
			}
			SerializeInputSettings();
		}

		private void SerializeInputSettings()
		{
			string cfgFilePath = GetCfgFilePath();
			using StreamWriter streamWriter = new StreamWriter(cfgFilePath);
			string value = JSON.Serialize(settings);
			streamWriter.WriteLine(value);
			GameLog.Print(AlertLevel.Notify, "Input", "Bindings saved to " + cfgFilePath);
		}

		private void OnFocusChange(bool focus)
		{
			m_lastChangeFocusFrame = Time.frameCount;
		}

		public static void ResetLockInput(MonoBehaviour obj)
		{
			if (m_inputLockedObject == obj)
			{
				m_inputLockedObject = null;
			}
		}

		public static void LockInput(MonoBehaviour obj)
		{
			m_inputLockedObject = obj;
		}

		public static MonoBehaviour GetLockedInputObject()
		{
			return m_inputLockedObject;
		}

		public static bool IsInputLocked()
		{
			if (FocusState.IsRealFixedActive && !steamOverlayActive && !IsBlockedByClickEffect())
			{
				if ((bool)m_inputLockedObject)
				{
					return m_inputLockedObject.isActiveAndEnabled;
				}
				return false;
			}
			return true;
		}

		public static bool IsInputLocked(MonoBehaviour obj)
		{
			if (obj == m_inputLockedObject && m_inputLockedObject != null)
			{
				return m_inputLockedObject.isActiveAndEnabled;
			}
			return false;
		}

		public static bool IsPS4PadCompatible(Controller controller)
		{
			if (controller.GetExtension<IDualShock4Extension>() != null)
			{
				return true;
			}
			if (controller.ElementIdentifiers.HasAny((ControllerElementIdentifier el) => el.name.ToLower().Contains("cross")) && controller.ElementIdentifiers.HasAny((ControllerElementIdentifier el) => el.name.ToLower().Contains("triangle")))
			{
				return true;
			}
			return false;
		}

		public static bool IsXboxCompatible(Controller controller)
		{
			if (controller.GetExtension<XboxOneGamepadExtension>() != null)
			{
				return true;
			}
			if (controller.ElementIdentifiers.HasAny((ControllerElementIdentifier el) => el.name.ToLower() == "a") && controller.ElementIdentifiers.HasAny((ControllerElementIdentifier el) => el.name.ToLower() == "b"))
			{
				return true;
			}
			return false;
		}

		public static bool CheckIsNeedDisableVSync()
		{
			if (m_graphicsQuality == null)
			{
				m_graphicsQuality = DependencyInjector.Resolve<GraphicsQuality>();
			}
			if (!IsWheelConnected() || m_graphicsQuality.vSync == 0)
			{
				return false;
			}
			m_graphicsQuality.settings.vSyncPrevVal = m_graphicsQuality.vSync;
			m_graphicsQuality.vSync = 0;
			GUICommonGodVoice.ShowText(LocalizationKeys.vsync_wheel_disable.Localized(), 5f);
			return true;
		}

		public static bool IsWheelConnected()
		{
			if (instance == null)
			{
				return false;
			}
			foreach (KeyValuePair<Controller, string> activeController in instance.activeControllers)
			{
				Controller key = activeController.Key;
				if (key != null && key.type != ControllerType.Keyboard && key.type != ControllerType.Mouse)
				{
					ControllerMetaInfo controllerMetaInfo = key.TryGetMetaInfo();
					if (controllerMetaInfo != null && controllerMetaInfo.deviceType == DeviceType.Wheel)
					{
						return true;
					}
				}
			}
			return false;
		}

		private void Update()
		{
			TryUnblockClickEffect();
			if (lastController == null)
			{
				lastController = ReInput.controllers.GetLastActiveController();
				if (lastController != null)
				{
					OnDeviceChanged(lastController);
				}
			}
			if (Time.timeScale == 0f)
			{
				UpdateActions();
			}
			UpdateLastSteeringDevice();
			UpdateVibroEffects();
		}

		private bool IsMouseActive()
		{
			if (ReInput.controllers.Mouse.GetAnyButton())
			{
				return true;
			}
			float sqrMagnitude = ReInput.controllers.Mouse.screenPositionDelta.sqrMagnitude;
			float num = 8.99f;
			if (sqrMagnitude > num)
			{
				return true;
			}
			return false;
		}

		private void UpdateVibroEffects()
		{
			if (m_currentVibroController == null)
			{
				return;
			}
			if (settings.Params.gamepadParams.vibrationEnabled && (m_currentVibroValueL > 0f || m_currentVibroValueR > 0f || m_impactVibroImpulse > 0f) && CurSteeringDevice != null && CurSteeringDevice.rewiredController.identifier.deviceInstanceGuid.Equals(m_currentVibroController.identifier.deviceInstanceGuid) && !CurSteeringDevice.disableFFB)
			{
				m_zeroVibroSetted = false;
				float num = 0.8f;
				float leftMotorLevel = Mathf.Clamp01(m_currentVibroValueL * num + m_impactVibroImpulse * (num + 0.2f));
				float rightMotorLevel = Mathf.Clamp01(m_currentVibroValueR * num + m_impactVibroImpulse * (num + 0.2f));
				ControllerSetVibration(m_currentVibroController, leftMotorLevel, rightMotorLevel);
				m_impactVibroImpulse -= Time.deltaTime * 3f;
				if (m_impactVibroImpulse < 0.05f)
				{
					m_impactVibroImpulse = 0f;
				}
			}
			else if (!m_zeroVibroSetted)
			{
				m_zeroVibroSetted = true;
				ControllerStopVibration(m_currentVibroController);
			}
		}

		private void ControllerStopVibration(Joystick joystick)
		{
			joystick.StopVibration();
		}

		private void ControllerSetVibration(Joystick joystick, float leftMotorLevel, float rightMotorLevel)
		{
			joystick.SetVibration(leftMotorLevel, rightMotorLevel);
		}

		private void OnDeviceChanged(Controller controller)
		{
			if (controller != null)
			{
				lastController = controller;
				GameLog.Print(AlertLevel.Notify, "Input", $"OnChangedDevice: {controller}:'{controller?.Name()}' id:{controller?.id}");
				CheckIsNeedDisableVSync();
				onChangeActiveDevice.SafeCall(controller);
			}
		}

		private void UpdateActions()
		{
			if (m_isChangedFocus)
			{
				return;
			}
			for (int i = 0; i < m_actions.Length; i++)
			{
				m_actions[i].Clear();
			}
			for (int j = 0; j < m_activeBinds.Length; j++)
			{
				m_activeBinds[j].Update();
			}
			for (int k = 0; k < m_activeBindButtons.Length; k++)
			{
				BindButtonElement bindButtonElement = m_activeBindButtons[k];
				Action action = m_actions[(uint)bindButtonElement.bindedAction];
				if (Action.IsProperlyBindSteeringDeviceForActionType(bindButtonElement, action.actionType))
				{
					bindButtonElement.Apply(action);
				}
			}
			for (int l = 0; l < m_actions.Length; l++)
			{
				m_actions[l].ApplyAxis();
			}
			m_pollingCount--;
			if (m_pollingCount < 0)
			{
				m_pollingCount = 0;
			}
		}

		private BindElement GetLastActiveBindByAction(ActionType actionType)
		{
			if ((int)actionType >= 0 && (int)actionType < m_actions.Length)
			{
				return m_actions[(uint)actionType].lastActiveBind;
			}
			return null;
		}

		private BindElement GetLastActiveBindForSteerChange()
		{
			BindElement lastActiveBindByAction = GetLastActiveBindByAction(ActionType.Turn);
			BindElement bindElement = GetLastActiveBindByAction(ActionType.Accelerate);
			if (bindElement != null && bindElement.deviceInfo?.deviceType == DeviceType.Pedals)
			{
				bindElement = null;
			}
			float num = lastActiveBindByAction?.lastTimeValueChanged ?? 0f;
			float num2 = bindElement?.lastTimeValueChanged ?? 0f;
			if (!(num >= num2))
			{
				return bindElement;
			}
			return lastActiveBindByAction;
		}

		public DeviceType GetCurrentSteeringDeviceType()
		{
			if (m_curFXDevice != null)
			{
				return m_curFXDevice.deviceType;
			}
			return m_CurSteeringDeviceType;
		}

		private void UpdateLastSteeringDevice()
		{
			ControllerMetaInfo curSteeringDevice = CurSteeringDevice;
			BindElement lastActiveBindForSteerChange = GetLastActiveBindForSteerChange();
			ControllerMetaInfo controllerMetaInfo = lastActiveBindForSteerChange?.deviceInfo;
			if ((controllerMetaInfo?.deviceType ?? DeviceType.Unknown) == DeviceType.Wheel && CurSteeringDeviceType != DeviceType.Wheel && Time.realtimeSinceStartup - m_wheelLostTime < 2f)
			{
				m_wheelFeedback.StopFFBEffects();
			}
			else if (curSteeringDevice != controllerMetaInfo && controllerMetaInfo != null)
			{
				CurSteeringDevice = controllerMetaInfo;
				isLastSteeringIsAxis = !(lastActiveBindForSteerChange is BindButtonElement);
				if (CurSteeringDevice != CurFXDevice && CurSteeringDevice?.hardwareIdentifier != CurFXDevice?.hardwareIdentifier)
				{
					TrySetNewFXDevice(curSteeringDevice);
				}
			}
		}

		private void TrySetNewFXDevice(ControllerMetaInfo oldSteeringDevice)
		{
			DeviceType deviceType = ((CurFXDevice != null) ? CurFXDevice.deviceType : DeviceType.Unknown);
			if (CurSteeringDeviceType != DeviceType.Wheel && deviceType == DeviceType.Wheel)
			{
				m_wheelLostTime = Time.unscaledTime;
			}
			if ((CurSteeringDevice != null && CurSteeringDevice.disableFFB) || !m_isFXEnabled)
			{
				CurFXDevice = null;
				m_wheelFeedback.FeedbackActive = false;
			}
			else
			{
				if (TrySetNewFXDeviceAsVibroJoystick() || CurSteeringDevice.deviceType != DeviceType.Wheel)
				{
					return;
				}
				bool feedbackEnabled = settings.Params.wheelParams.feedbackEnabled;
				TrySetWheelFeedbackActiveForCurrDevice(feedbackEnabled, delegate(bool success)
				{
					if (success)
					{
						CurFXDevice = CurSteeringDevice;
					}
					else
					{
						m_wheelFeedback.ffbManager.OnFailedCurrentllySupported(CurSteeringDeviceType, oldSteeringDevice, CurSteeringDevice);
						CurFXDevice = null;
					}
				});
			}
		}

		private bool TrySetNewFXDeviceAsVibroJoystick()
		{
			Joystick currentVibroController = m_currentVibroController;
			m_currentVibroController = (CurSteeringDevice.isVibroSupport ? (CurSteeringDevice.rewiredController as Joystick) : null);
			if (currentVibroController != m_currentVibroController)
			{
				StopAllVibro();
				if (m_currentVibroController != null)
				{
					CurFXDevice = CurSteeringDevice;
					m_wheelFeedback.FeedbackActive = false;
					return true;
				}
			}
			return false;
		}

		private void TrySetWheelFeedbackActiveForCurrDevice(bool needActivate, Action<bool> onFeedbackActivate)
		{
			if (!needActivate)
			{
				m_wheelFeedback.FeedbackActive = false;
				return;
			}
			Controller addedController = CurSteeringDevice?.rewiredController;
			m_wheelFeedback.TryInitWheelInterfaceByController(addedController, CurFXDevice?.rewiredController);
			m_wheelFeedback.FeedbackActive = m_wheelFeedback.IsCurrentrlySupportFFB(CurSteeringDevice.rewiredController);
			onFeedbackActivate.SafeCall(m_wheelFeedback.FeedbackActive);
		}

		public void DeviceReset(Controller controller)
		{
			string key = activeControllers[controller];
			settings.devices.Remove(key);
			UpdateBinds();
		}

		private InputSettings.DeviceProfile AddDefaultProfile(Controller controller, string name)
		{
			InputSettings.DeviceProfile deviceProfile = new InputSettings.DeviceProfile();
			settings.devices.Add(name, deviceProfile);
			deviceProfile.ConstructDefault(controller.GetMetaInfo());
			return deviceProfile;
		}

		private void BindController(Controller controller)
		{
			string key = activeControllers[controller];
			InputSettings.DeviceProfile value = null;
			if (!settings.devices.TryGetValue(key, out value))
			{
				value = AddDefaultProfile(controller, key);
			}
			try
			{
				value.Bind(controller);
			}
			catch
			{
				settings.devices.Remove(key);
				value = AddDefaultProfile(controller, key);
				value.Bind(controller);
			}
			controller.GetMetaInfo().disableFFB = value.disableFFB;
			if (value.buttons.Count > 0)
			{
				int length = m_activeBinds.GetLength();
				int num = 0;
				m_activeBinds = m_activeBinds.Resize(length + value.buttons.Count);
				foreach (BindButtonElement button in value.buttons)
				{
					m_activeBinds[length + num] = button;
					num++;
				}
			}
			if (value.axises.Count <= 0)
			{
				return;
			}
			int length2 = m_activeBinds.GetLength();
			int num2 = 0;
			m_activeBinds = m_activeBinds.Resize(length2 + value.axises.Count);
			foreach (BindAxisElement axise in value.axises)
			{
				m_activeBinds[length2 + num2] = axise;
				num2++;
			}
		}

		private void AddController(ControllerStatusChangedEventArgs e)
		{
			Controller controller = ReInput.controllers.GetController(e.controllerType, e.controllerId);
			OnAddedController(controller, controller.name);
		}

		private void PreDisconnectController(ControllerStatusChangedEventArgs e)
		{
			Controller controller = ReInput.controllers.GetController(e.controllerType, e.controllerId);
			m_activeBinds = m_activeBinds.Filter((BindElement b) => b.deviceInfo.rewiredController != controller);
			OnControllerRelease(controller);
		}

		private void OnAddedController(Controller controller, string controllerName)
		{
			UpdateControllers(controller, null);
			UpdateBinds();
			onDeviceAdd.Call(controller);
			lastController = controller;
			OnDeviceChanged(lastController);
		}

		public void OnCustomControllerCreated(CustomController controller, string controllerName)
		{
			OnAddedController(controller, controllerName);
		}

		public void OnControllerRelease(Controller controller)
		{
			ControllerMetaInfoManager.UnregisterDevice(controller);
			UpdateControllers(null, controller);
			UpdateBinds();
			onDeviceRemove.Call(controller);
			lastController = null;
		}

		private void UpdateControllers(Controller addedController, Controller removedController)
		{
			m_vibroControllers.Clear();
			int num = controllersListGeneration + 1;
			controllersListGeneration = num;
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			int value = 0;
			activeControllers.Clear();
			foreach (Controller controller in ReInput.controllers.Controllers)
			{
				if (NeedIngoreController(controller))
				{
					controller.enabled = false;
				}
				else if (controller != removedController)
				{
					string hardwareName = controller.hardwareName;
					if (dictionary.TryGetValue(hardwareName, out value))
					{
						value = (dictionary[hardwareName] = value + 1);
					}
					else
					{
						value = 1;
						dictionary.Add(hardwareName, 1);
					}
					activeControllers.Add(controller, hardwareName + "#" + value);
					if (controller is Joystick { supportsVibration: not false } joystick)
					{
						m_vibroControllers.Add(joystick);
					}
				}
			}
			if (addedController != null)
			{
				ControllerMetaInfo metaInfo = addedController.GetMetaInfo();
				if (metaInfo != null && metaInfo.deviceType == DeviceType.Wheel)
				{
					foreach (Controller key in activeControllers.Keys)
					{
						ControllerMetaInfo metaInfo2 = key.GetMetaInfo();
						if (key != addedController && metaInfo2.deviceType == DeviceType.Wheel)
						{
							if (metaInfo2 == CurFXDevice)
							{
								m_wheelFeedback.StopFFBEffects();
							}
							metaInfo2.disableFFB = true;
							metaInfo2.deviceType = DeviceType.Gamepad;
						}
					}
				}
			}
			if (removedController != null && removedController == CurFXDevice?.rewiredController)
			{
				CurFXDevice = null;
				m_wheelFeedback.StopFFBEffects();
				m_wheelFeedback.OnControllerListChanged(null, removedController);
			}
			ReInput.controllers.Keyboard.enabled = true;
		}

		private bool NeedIngoreController(Controller ctrl)
		{
			if (ctrl.hardwareTypeGuid == Guid.Empty)
			{
				return ctrl.hardwareIdentifier.Contains("00050eb7");
			}
			return false;
		}

		private void RecalculateButtonsAndAxises()
		{
			Action[] actions = m_actions;
			foreach (Action obj in actions)
			{
				obj.activeBinds.Clear();
				obj.lastActiveBind = null;
			}
			BindElement[] activeBinds = m_activeBinds;
			foreach (BindElement bindElement in activeBinds)
			{
				m_actions[(uint)bindElement.bindedAction].activeBinds.Add(bindElement);
			}
			m_activeBindButtons = m_activeBinds.Convert((BindElement b) => b is BindButtonElement, (BindElement b) => b as BindButtonElement);
			m_activeBindAxises = m_activeBinds.Convert((BindElement a) => a is BindAxisElement, (BindElement a) => a as BindAxisElement);
		}

		public static bool CompareControlElementName(string elementName, string originalName)
		{
			if (string.Equals(elementName, originalName, StringComparison.InvariantCultureIgnoreCase))
			{
				return true;
			}
			return false;
		}

		private static string PrepareNameToCompare(string s)
		{
			return s.ToLower().Replace(" ", string.Empty);
		}

		private static bool CheckNameToDeviceName(string paramName, string prepairedDeviceName)
		{
			paramName = PrepareNameToCompare(paramName);
			if (prepairedDeviceName.StartsWith(paramName))
			{
				return true;
			}
			if (paramName.StartsWith(prepairedDeviceName))
			{
				return true;
			}
			return false;
		}

		public static bool CompareControllerName(Controller controller, string deviceName)
		{
			deviceName = PrepareNameToCompare(deviceName);
			if (CheckNameToDeviceName(controller.hardwareName, deviceName))
			{
				return true;
			}
			if (CheckNameToDeviceName(controller.name, deviceName))
			{
				return true;
			}
			return false;
		}

		private IEnumerator PlayClickEffectCoroutine(Image image, System.Action action, bool skipPauseToAction)
		{
			m_blockInputFrame = Time.frameCount;
			PlayClickSound();
			if (image != null)
			{
				float duration = fxEfffectTime;
				image.DOKill(complete: true);
				Color oldColor = image.color;
				Color color = oldColor;
				color.a = 1f;
				image.color = color;
				image.DOFade(fxEndFade, duration).OnComplete(delegate
				{
					if (!(image == null))
					{
						image.color = oldColor;
					}
				}).SetUpdate(isIndependentUpdate: true);
				RectTransform rt = image.rectTransform;
				rt.DOKill(complete: true);
				Vector3 oldScale = rt.localScale;
				rt.DOScale(fxEndScale, duration).OnComplete(delegate
				{
					if (!(rt == null))
					{
						rt.localScale = oldScale;
					}
				}).SetUpdate(isIndependentUpdate: true);
			}
			if (!skipPauseToAction)
			{
				m_clickTimeout.waitTime = onClickActionTimeOut;
				yield return m_clickTimeout;
			}
			else
			{
				yield return null;
			}
			m_blockInputFrame = 0;
			try
			{
				action.SafeCall();
			}
			catch (Exception)
			{
			}
		}

		public static void PlayClickSound()
		{
			SoundController.Play2DUISound(SoundSample.DefaultBtn, 0f);
		}

		private static void TryUnblockClickEffect()
		{
			if (m_blockInputFrame != 0 && Time.frameCount - m_blockInputFrame > 100)
			{
				UnityEngine.Debug.LogWarning("Unblocking input effect");
				m_blockInputFrame = 0;
			}
		}

		private static bool IsBlockedByClickEffect()
		{
			return m_blockInputFrame != 0;
		}

		public void PlayClickEffect(Image image, System.Action action, bool skipPauseToAction, bool skipUIHiddenTest = false)
		{
			if ((skipUIHiddenTest || !KeepAliveManager.isUIHidden) && !IsBlockedByClickEffect() && !IsInputLocked())
			{
				StartCoroutine(PlayClickEffectCoroutine(image, action, skipPauseToAction || image == null));
			}
		}

		public void PlayActionElementBindingPressedEffect(ActionElementBinding actionBinding, System.Action action, bool skipPauseToAction = false)
		{
			if (actionBinding.actionType == ActionType.NavNextPage || actionBinding.actionType == ActionType.NavPrevPage)
			{
				skipPauseToAction = true;
			}
			PlayClickEffect(actionBinding.clickEffectImage, action, skipPauseToAction);
		}

		public void PlayNavigateEffect(ElementNavigate navigate, ActionType actionType, System.Action action)
		{
			PlayClickEffect(null, action, skipPauseToAction: true);
		}

		public void PlayHotkeyPressedEffect(ActionType actionType, System.Action action)
		{
			PlayClickEffect(null, action, skipPauseToAction: true, actionType == ActionType.NavBack);
		}
	}
}
