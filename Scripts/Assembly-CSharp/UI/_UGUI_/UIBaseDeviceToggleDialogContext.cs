using System;
using System.Collections.Generic;
using Binding;
using Binding.Base;
using GameInput;
using Rewired;
using UnityEngine;

namespace UI._UGUI_
{
	[RaycastBlocker(true)]
	[GameHotkeyBlocker(true)]
	public class UIBaseDeviceToggleDialogContext : UIBaseDialogContext
	{
		[SerializeField]
		private DeviceToggleTypeView m_toggleTypeView;

		public readonly Property<string> optionalTitle = new Property<string>();

		public readonly Property<string> deviceTypeTitle = new Property<string>();

		public Property<bool> availableNextType = new BoolProperty(false);

		public Property<bool> availablePrevType = new BoolProperty(false);

		public Property<bool> selectTypeSwitcher = new BoolProperty(false);

		public int index;

		public List<GameInput.DeviceType> allowedDeviceTypeList = new List<GameInput.DeviceType>();

		private readonly GameInput.DeviceType[] m_devicePriorityTypes = new GameInput.DeviceType[3]
		{
			GameInput.DeviceType.Wheel,
			GameInput.DeviceType.Gamepad,
			GameInput.DeviceType.Keyboard
		};

		private readonly List<string> m_availableTypeListToShow = new List<string>(Enum.GetValues(typeof(GameInput.DeviceType)).Length);

		private void Awake()
		{
			DeviceToggleTypeView toggleTypeView = m_toggleTypeView;
			toggleTypeView.OnClickButtonNext = (System.Action)Delegate.Combine(toggleTypeView.OnClickButtonNext, new System.Action(OnClickButtonNext));
			DeviceToggleTypeView toggleTypeView2 = m_toggleTypeView;
			toggleTypeView2.OnClickButtonPrev = (System.Action)Delegate.Combine(toggleTypeView2.OnClickButtonPrev, new System.Action(OnClickButtonPrev));
		}

		private void OnEnable()
		{
			selectTypeSwitcher.value = false;
		}

		public override void NavigateLeft()
		{
			if (selectTypeSwitcher.value)
			{
				OnClickButtonPrev();
			}
			else
			{
				base.NavigateLeft();
			}
		}

		public override void NavigateRight()
		{
			if (selectTypeSwitcher.value)
			{
				OnClickButtonNext();
			}
			else
			{
				base.NavigateRight();
			}
		}

		[GameHotkey(ActionType.NavUp, false, false)]
		[GameHotkey(ActionType.NavDPadUp, false, false)]
		public void NavigateUp()
		{
			selectTypeSwitcher.value = true;
			ResetSelection();
		}

		[GameHotkey(ActionType.NavDown, false, false)]
		[GameHotkey(ActionType.NavDPadDown, false, false)]
		public void NavigateDown()
		{
			selectTypeSwitcher.value = false;
			UpdateSelection();
		}

		public void InitAllowedDevicesType()
		{
			m_availableTypeListToShow.Clear();
			foreach (GameInput.DeviceType allowedDeviceType in allowedDeviceTypeList)
			{
				m_availableTypeListToShow.Add(LocalizeDeviceType(allowedDeviceType));
			}
			DefineDefaultIndexDevice();
			UpdateSwitcher();
		}

		private void DefineDefaultIndexDevice()
		{
			Dictionary<Controller, string> activeControllers = GameInput.InputManager.instance.activeControllers;
			GameInput.DeviceType[] devicePriorityTypes = m_devicePriorityTypes;
			foreach (GameInput.DeviceType deviceType in devicePriorityTypes)
			{
				foreach (KeyValuePair<Controller, string> item in activeControllers)
				{
					ControllerMetaInfo metaInfo = item.Key.GetMetaInfo();
					if (metaInfo != null && metaInfo.deviceType == deviceType)
					{
						index = (allowedDeviceTypeList.Contains(deviceType) ? allowedDeviceTypeList.IndexOf(deviceType) : 0);
						break;
					}
				}
			}
		}

		private void OnClickButtonNext()
		{
			if (TrySelectDeviceType(index + 1))
			{
				UpdateSwitcher();
			}
		}

		private void OnClickButtonPrev()
		{
			if (TrySelectDeviceType(index - 1))
			{
				UpdateSwitcher();
			}
		}

		private void UpdateSwitcher()
		{
			availableNextType.value = index + 1 <= m_availableTypeListToShow.Count - 1;
			availablePrevType.value = index - 1 >= 0;
			deviceTypeTitle.value = m_availableTypeListToShow[index];
		}

		private string LocalizeDeviceType(GameInput.DeviceType type)
		{
			switch (type)
			{
			case GameInput.DeviceType.Keyboard:
			case GameInput.DeviceType.Mouse:
				return LocalizationKeys.settings_input_keyboard.Localized();
			case GameInput.DeviceType.Gamepad:
				return LocalizationKeys.settings_input_gamepad.Localized();
			case GameInput.DeviceType.Wheel:
			case GameInput.DeviceType.Pedals:
				return LocalizationKeys.bind_wheel.Localized();
			default:
				return LocalizationKeys.Unknown.Localized();
			}
		}

		private bool TrySelectDeviceType(int deviceIndex)
		{
			if (deviceIndex < 0 || deviceIndex >= m_availableTypeListToShow.Count)
			{
				return false;
			}
			index = deviceIndex;
			return true;
		}

		protected override void OnDestroy()
		{
			DeviceToggleTypeView toggleTypeView = m_toggleTypeView;
			toggleTypeView.OnClickButtonNext = (System.Action)Delegate.Remove(toggleTypeView.OnClickButtonNext, new System.Action(OnClickButtonNext));
			DeviceToggleTypeView toggleTypeView2 = m_toggleTypeView;
			toggleTypeView2.OnClickButtonPrev = (System.Action)Delegate.Remove(toggleTypeView2.OnClickButtonPrev, new System.Action(OnClickButtonPrev));
			base.OnDestroy();
		}
	}
}
