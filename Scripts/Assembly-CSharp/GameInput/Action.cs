using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameInput
{
	public class Action
	{
		private const float TIMEOUT_GODVOICE_STEERING_DEVICE = 2f;

		public ActionType actionType;

		public ActionValueType valueType;

		public List<BindElement> activeBinds = new List<BindElement>();

		public BindElement lastActiveBind;

		public Func<float, float> signalCurveAffector;

		private static bool[] m_systemActions;

		private static bool[] m_steeringDeviceDependentActions;

		private static float m_lastTimeShownGodvoiceToSetSteeringDevice;

		private float m_value;

		private bool m_pressed;

		private bool m_released;

		public float value
		{
			get
			{
				return m_value;
			}
			set
			{
				switch (valueType)
				{
				case ActionValueType.Full:
					m_value = Mathf.Clamp(value, -1f, 1f);
					break;
				case ActionValueType.Half:
					m_value = Mathf.Clamp(value, 0f, 1f);
					break;
				case ActionValueType.Button:
					m_value = ((value < 0.01f) ? 0f : 1f);
					break;
				}
			}
		}

		public bool pressed
		{
			get
			{
				return m_pressed;
			}
			set
			{
				m_pressed = value;
				if (value)
				{
					m_released = false;
				}
			}
		}

		public bool released
		{
			get
			{
				return m_released;
			}
			set
			{
				m_released = value;
				if (value)
				{
					m_pressed = false;
				}
			}
		}

		public static bool IsSystemAction(ActionType actionType)
		{
			return m_systemActions[(uint)actionType];
		}

		public static bool IsSteeringDeviceDependentActionType(ActionType actionType)
		{
			return m_steeringDeviceDependentActions[(uint)actionType];
		}

		static Action()
		{
			CacheSystemActions();
		}

		private static void CacheSystemActions()
		{
			Array values = Enum.GetValues(typeof(ActionType));
			m_systemActions = new bool[values.Length];
			m_steeringDeviceDependentActions = new bool[values.Length];
			int num = 0;
			foreach (ActionType item in values)
			{
				m_systemActions[num] = GetAttributeOfType<SystemActionAttribute>(item) != null;
				m_steeringDeviceDependentActions[num] = GetAttributeOfType<SteeringDeviceDependent>(item) != null;
				num++;
			}
		}

		public static T GetAttributeOfType<T>(Enum enumVal) where T : Attribute
		{
			object[] customAttributes = enumVal.GetType().GetMember(enumVal.ToString())[0].GetCustomAttributes(typeof(T), inherit: false);
			if (customAttributes.Length == 0)
			{
				return null;
			}
			return (T)customAttributes[0];
		}

		public void Clear()
		{
			m_released = false;
			m_pressed = false;
			m_value = 0f;
		}

		public DeviceType GetLastDeviceType()
		{
			if (lastActiveBind != null)
			{
				return lastActiveBind.deviceInfo.deviceType;
			}
			return DeviceType.Unknown;
		}

		public void ApplyAxis()
		{
			bool flag = false;
			int count = activeBinds.Count;
			for (int i = 0; i < count; i++)
			{
				BindElement bindElement = activeBinds[i];
				if (IsProperlyBindSteeringDeviceForActionType(bindElement, actionType))
				{
					if (bindElement.IsActive())
					{
						flag = true;
						lastActiveBind = bindElement;
						break;
					}
					if (bindElement == lastActiveBind)
					{
						flag = true;
					}
				}
			}
			if (flag && lastActiveBind is BindAxisElement)
			{
				lastActiveBind.Apply(this);
			}
		}

		public static bool IsProperlyBindSteeringDeviceForActionType(BindElement bindElement, ActionType actionType)
		{
			if (!IsSteeringDeviceDependentActionType(actionType))
			{
				return true;
			}
			if (States.current is BaseGameState baseGameState && baseGameState.contextOwner.IsActive<UIInputBindsSettingsContext>())
			{
				return true;
			}
			DeviceType? deviceType = bindElement?.deviceInfo?.deviceType;
			DeviceType? steeringDeviceType = InputManager.settings.Params.steeringDeviceType;
			bool flag = false;
			flag = ((steeringDeviceType != DeviceType.Wheel) ? (deviceType == DeviceType.Keyboard || deviceType == DeviceType.Gamepad || deviceType == DeviceType.Mouse) : (deviceType == DeviceType.Wheel || deviceType == DeviceType.ButtonPanel || (deviceType == DeviceType.Pedals && actionType == ActionType.Accelerate)));
			if (!flag)
			{
				TryShowGodVoiceToSetProperlySteeringDevice(bindElement.IsActive());
			}
			return flag;
		}

		private static void TryShowGodVoiceToSetProperlySteeringDevice(bool isBindActive)
		{
			if (Time.timeScale == 0f)
			{
				m_lastTimeShownGodvoiceToSetSteeringDevice = Time.unscaledTime + Time.unscaledDeltaTime * 2f - 2f;
			}
			if (isBindActive && States.current is BaseRaceState { isFreeCamActive: false } && Time.unscaledTime - m_lastTimeShownGodvoiceToSetSteeringDevice > 2f)
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.need_set_properly_steering_device_in_input_settings.Localized(), 2f);
				m_lastTimeShownGodvoiceToSetSteeringDevice = Time.unscaledTime;
			}
		}

		public static Action[] GetActionDefines()
		{
			Func<InputSettings.Parameters> p = () => InputManager.settings.Params;
			return new Action[90]
			{
				new Action
				{
					actionType = ActionType.ErrorAction,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.Turn,
					valueType = ActionValueType.Full,
					signalCurveAffector = (float x) => GetUnlinearDeadzoneSignal(x, p().steeringLinearity, p().steeringDeadZone)
				},
				new Action
				{
					actionType = ActionType.Accelerate,
					valueType = ActionValueType.Half,
					signalCurveAffector = (float x) => GetUnlinearSignal(x, p().accLinearity)
				},
				new Action
				{
					actionType = ActionType.Brake,
					valueType = ActionValueType.Half,
					signalCurveAffector = (float x) => GetUnlinearSignal(x, p().brakeLinearity)
				},
				new Action
				{
					actionType = ActionType.Clutch,
					valueType = ActionValueType.Half,
					signalCurveAffector = (float x) => GetUnlinearSignal(x, p().clutchLinearity)
				},
				new Action
				{
					actionType = ActionType.HandBrake,
					valueType = ActionValueType.Half,
					signalCurveAffector = (float x) => GetUnlinearSignal(x, p().handbrakeLinearity)
				},
				new Action
				{
					actionType = ActionType.Flares,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.NextCamera,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.PrevCamera,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.RearViewCamera,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.RestartRace,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.RespawnCar,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.GearUp,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.GearDown,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.Gear1,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.Gear2,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.Gear3,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.Gear4,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.Gear5,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.Gear6,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.GearR,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.VoteMap,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.HideUI,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.HideMouse,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.HideDriver,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.HideWheel,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.DPadUp,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.DPadRight,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.DPadDown,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.DPadLeft,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.InterfaceSelect,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.FreeCamMode,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.FreeCamView,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.MenuLayerSeparator,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.NavNextPage,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.NavPrevPage,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.NavUp,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.NavDown,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.NavLeft,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.NavRight,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.NavCancelBtnB,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.NavSelectBtnA,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.NavMouseSelect,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.NavBack,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.NavStart,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.NavAdditional0BtnX,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.NavAdditional1BtnY,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.NavBtnExpand,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.NavDPadUp,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.NavDPadDown,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.NavDPadRight,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.NavDPadLeft,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.NavAdditionalPrevBtnL2,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.NavAdditionalNextBtnR2,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.NavAddUp,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.NavAddDown,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.NavAddRight,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.NavAddLeft,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.RaceStats,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.RaceJoin,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.RaceCreate,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.NavBtnExpandRight,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.VoiceChat,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.FlyMovement,
					valueType = ActionValueType.Full
				},
				new Action
				{
					actionType = ActionType.FlyStrafe,
					valueType = ActionValueType.Full
				},
				new Action
				{
					actionType = ActionType.FlyRotationVertical,
					valueType = ActionValueType.Full
				},
				new Action
				{
					actionType = ActionType.FlyRotationHorizontal,
					valueType = ActionValueType.Full
				},
				new Action
				{
					actionType = ActionType.FlyHeightUp,
					valueType = ActionValueType.Half
				},
				new Action
				{
					actionType = ActionType.FlyHeightDown,
					valueType = ActionValueType.Half
				},
				new Action
				{
					actionType = ActionType.FlyTurboMode,
					valueType = ActionValueType.Half
				},
				new Action
				{
					actionType = ActionType.FlyHideUI,
					valueType = ActionValueType.Half
				},
				new Action
				{
					actionType = ActionType.FlyStatistics,
					valueType = ActionValueType.Half
				},
				new Action
				{
					actionType = ActionType.FlyChangeFOV,
					valueType = ActionValueType.Half
				},
				new Action
				{
					actionType = ActionType.FlyReset,
					valueType = ActionValueType.Half
				},
				new Action
				{
					actionType = ActionType.NavReport,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.Headlights,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.Ignition,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.Starter,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.VRCamReset,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.Gear7,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.ShowInputBar,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.HideSidePanel,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.ZoomIn,
					valueType = ActionValueType.Half
				},
				new Action
				{
					actionType = ActionType.ZoomOut,
					valueType = ActionValueType.Half
				},
				new Action
				{
					actionType = ActionType.ShowRadialMenu,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.GoToDynostand,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.TeleportToWaypoint,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.CreateWaypoint,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.OpenTextChat,
					valueType = ActionValueType.Button
				},
				new Action
				{
					actionType = ActionType.OpenTextChatReserve,
					valueType = ActionValueType.Button
				}
			};
		}

		public static float GetUnlinearSignal(float value, float linearity)
		{
			return value * linearity + value * Mathf.Abs(value) * (1f - linearity);
		}

		public static float GetUnlinearDeadzoneSignal(float value, float linearity, float deadZone)
		{
			float num;
			float num2;
			if (value < 0f)
			{
				num = 0f - value;
				num2 = -1f;
			}
			else
			{
				num = value;
				num2 = 1f;
			}
			if (deadZone > 0.99f)
			{
				deadZone = 0.99f;
			}
			value = ((num < deadZone) ? deadZone : num);
			value = num2 * (value - deadZone) / (1f - deadZone);
			return value * linearity + value * num * (1f - linearity);
		}

		public static bool IsGearChangeAction(ActionType actionType)
		{
			if (actionType != ActionType.GearUp)
			{
				return actionType == ActionType.GearDown;
			}
			return true;
		}
	}
}
