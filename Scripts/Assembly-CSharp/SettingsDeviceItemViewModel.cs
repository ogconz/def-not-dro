using System;
using System.Collections.Generic;
using GameInput;
using Rewired;
using Rewired.ControllerExtensions;
using Rewired.Interfaces;

public class SettingsDeviceItemViewModel : BaseListItem
{
	public Action<SettingsDeviceItemViewModel> onChangedDeviceType;

	public Action<SettingsDeviceItemViewModel> OnFFBSwitchEvent;

	private UIInputSettingsContext m_context;

	private IControllerVibrator m_controllerInterface;

	private static readonly List<DeviceType> m_allowedDeviceTypeList = new List<DeviceType>
	{
		DeviceType.ButtonPanel,
		DeviceType.Gamepad,
		DeviceType.Pedals,
		DeviceType.Wheel
	};

	private List<string> m_availableTypeListToShow = new List<string>();

	private int m_index;

	public Controller device { get; private set; }

	public ControllerMetaInfo deviceMeta { get; private set; }

	public string deviceName { get; private set; }

	public string deviceType { get; set; }

	public bool deviceActived { get; set; }

	public bool fxActive { get; set; }

	public bool fxDisabled { get; set; }

	public bool availableNextType { get; private set; }

	public bool availablePrevType { get; private set; }

	public int index
	{
		get
		{
			return m_index;
		}
		private set
		{
			if (value >= 0 && value < m_availableTypeListToShow.Count)
			{
				m_index = value;
			}
		}
	}

	public SettingsDeviceItemViewModel(Controller device, UIInputSettingsContext context)
	{
		m_context = context;
		this.device = device;
		deviceMeta = device.GetMetaInfo();
		TryGetControllerExtension();
		UpdateItemInfo();
	}

	public void UpdateItemInfo()
	{
		UpdateDeviceType();
		UpdateDeviceName();
		UpdateDeviceState();
	}

	public void UpdateDeviceState()
	{
		deviceActived = ReInput.controllers.GetLastActiveController() == device;
		DeviceType? steeringDeviceType = GameInput.InputManager.settings.Params.steeringDeviceType;
		fxActive = steeringDeviceType == deviceMeta.deviceType || (steeringDeviceType == DeviceType.Wheel && (deviceMeta.deviceType == DeviceType.Pedals || deviceMeta.deviceType == DeviceType.ButtonPanel));
		fxDisabled = !fxActive;
	}

	private void TryGetControllerExtension()
	{
		m_controllerInterface = device.GetExtension<DualSenseExtension>();
		if (m_controllerInterface == null)
		{
			m_controllerInterface = device.GetExtension<IDualShock4Extension>();
		}
	}

	public void UpdateDeviceType()
	{
		m_availableTypeListToShow.Clear();
		if (device.type == ControllerType.Keyboard)
		{
			this.deviceType = device.type.ToString() + "/" + ControllerType.Mouse;
			m_availableTypeListToShow.Add(this.deviceType);
			index = 0;
		}
		else if (m_controllerInterface is IDualShock4Extension)
		{
			this.deviceType = "Controller";
			m_availableTypeListToShow.Add(this.deviceType);
			index = 0;
		}
		else
		{
			DeviceType deviceType = deviceMeta.deviceType;
			this.deviceType = deviceType.ToString();
			for (int i = 0; i < m_allowedDeviceTypeList.Count; i++)
			{
				DeviceType deviceType2 = m_allowedDeviceTypeList[i];
				m_availableTypeListToShow.Add(deviceType2.ToString());
				if (deviceType2 == deviceType)
				{
					index = i;
				}
			}
		}
		UpdateSwitcher();
	}

	public void UpdateDeviceName()
	{
		IControllerVibrator controllerInterface = m_controllerInterface;
		if (!(controllerInterface is DualSenseExtension))
		{
			if (controllerInterface is IDualShock4Extension)
			{
				deviceName = LocalizationKeys.dualshock_gamepad_name.Localized();
			}
			else
			{
				deviceName = deviceMeta.name;
			}
		}
		else
		{
			deviceName = LocalizationKeys.ps5_dualsense_gamepad_name.Localized();
		}
	}

	public override void OnClick()
	{
		base.OnClick();
	}

	public void NextDeviceTypeValue()
	{
		if (TrySelectDeviceType(index + 1))
		{
			onChangedDeviceType.SafeCall(this);
			UpdateSwitcher();
		}
	}

	public void PrevDeviceTypeValue()
	{
		if (TrySelectDeviceType(index - 1))
		{
			onChangedDeviceType.SafeCall(this);
			UpdateSwitcher();
		}
	}

	private bool TrySelectDeviceType(int index)
	{
		if (index < 0 || index >= m_availableTypeListToShow.Count)
		{
			return false;
		}
		string value = m_availableTypeListToShow[index];
		if (!Enum.TryParse<DeviceType>(value, ignoreCase: true, out var result))
		{
			return false;
		}
		this.index = index;
		deviceType = value;
		deviceMeta.deviceType = result;
		return true;
	}

	private void UpdateSwitcher()
	{
		availableNextType = true;
		availablePrevType = true;
		if (index - 1 < 0)
		{
			availablePrevType = false;
		}
		if (index + 1 > m_availableTypeListToShow.Count - 1)
		{
			availableNextType = false;
		}
	}

	public void OnMissClick()
	{
		m_context.ShowBinds(deviceMeta.rewiredController);
	}

	public void OnFFBSwitch()
	{
		OnFFBSwitchEvent.SafeCall(this);
	}
}
