using System;
using GameInput;
using Rewired;
using UnityEngine;

public class LogitechFeedback : IForceFeedbackSystem
{
	private const bool EnableSdkShutdown = false;

	private const string LogitechVendorName = "logitech";

	private const string GhubInterfix = "ghub";

	private const string UsbInterfix = "usb";

	private bool m_isEnabled = true;

	private float m_grassEffect;

	private float m_groundEffect;

	private bool m_stopEffectsRequested;

	private int m_deviceIndex;

	public bool isInitiated { get; private set; }

	public void Initialize()
	{
		m_isEnabled = false;
	}

	public bool IsThisDeviceController(Controller controller)
	{
		if (!isInitiated || controller.type != ControllerType.Joystick)
		{
			return false;
		}
		ControllerMetaInfo controllerMetaInfo = controller.TryGetMetaInfo();
		if (controllerMetaInfo != null && controllerMetaInfo.deviceData != -1)
		{
			m_deviceIndex = controllerMetaInfo.deviceData;
			return true;
		}
		int num = -1;
		for (int i = 0; i < 4; i++)
		{
			if (!LogitechGSDK.LogiIsConnected(i) || !LogitechGSDK.LogiHasForceFeedback(i))
			{
				continue;
			}
			string deviceName = LogitechGSDK.LogiSteeringGetFriendlyProductName(i);
			if (CompareControllerName(controller, deviceName))
			{
				m_deviceIndex = i;
				if (controllerMetaInfo != null)
				{
					controllerMetaInfo.deviceData = m_deviceIndex;
				}
				return true;
			}
			if (num == -1 && (ControllerNameHaveFFB(controller) || GameConsole.Bool["ffb_force"]))
			{
				num = i;
			}
		}
		if (num != -1)
		{
			m_deviceIndex = num;
			if (controllerMetaInfo != null)
			{
				controllerMetaInfo.deviceData = m_deviceIndex;
			}
			return true;
		}
		return false;
	}

	private static bool ControllerNameHaveFFB(Controller controller)
	{
		if (HaveFFB(controller.hardwareName))
		{
			return true;
		}
		return false;
	}

	private static bool HaveFFB(string name)
	{
		name = name.ToLowerInvariant();
		if (name.Contains("ffb"))
		{
			return true;
		}
		if (name.Contains("force"))
		{
			return true;
		}
		if (name.Contains("feed"))
		{
			return true;
		}
		return false;
	}

	private static bool CheckNameToDeviceName(string name, string prepairedDeviceName)
	{
		name = PrepareNameToCompare(name);
		if (prepairedDeviceName.StartsWith(name) || name.StartsWith(prepairedDeviceName))
		{
			return true;
		}
		return false;
	}

	private static string PrepareNameToCompare(string s)
	{
		return s.ToLower().Replace(" ", string.Empty).Replace("ghub", string.Empty)
			.Replace("logitech", string.Empty)
			.Replace("usb", string.Empty);
	}

	private static bool CompareControllerName(Controller controller, string deviceName)
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

	private void UpdateWheelRange()
	{
		if (!isInitiated)
		{
			return;
		}
		for (int i = 0; i < 4; i++)
		{
			if (LogitechGSDK.LogiIsConnected(i) && LogitechGSDK.LogiHasForceFeedback(i))
			{
				InputSettings.Parameters parameters = GameInput.InputManager.settings.Params;
				LogitechGSDK.LogiSetOperatingRange(i, (int)parameters.wheelParams.steeringWheelAngle);
				if (!Application.isEditor)
				{
					LogitechGSDK.LogiControllerPropertiesData properties = default(LogitechGSDK.LogiControllerPropertiesData);
					LogitechGSDK.LogiGetCurrentControllerProperties(i, ref properties);
					properties.wheelRange = (int)parameters.wheelParams.steeringWheelAngle;
					LogitechGSDK.LogiSetPreferredControllerProperties(properties);
				}
			}
		}
	}

	public void SetConstantForce(float k)
	{
		if (m_isEnabled)
		{
			k = GameInput.InputManager.settings.Params.wheelParams.forceFeedback * k;
			int deviceIndex = m_deviceIndex;
			if (LogitechGSDK.LogiIsConnected(deviceIndex) && LogitechGSDK.LogiHasForceFeedback(deviceIndex))
			{
				LogitechGSDK.LogiPlayConstantForce(deviceIndex, Mathf.RoundToInt(100f * k));
			}
		}
	}

	public void PlayImpactEffect(float k)
	{
		if (m_isEnabled)
		{
			k = GameInput.InputManager.settings.Params.wheelParams.forceFeedback * k;
			int deviceIndex = m_deviceIndex;
			if (LogitechGSDK.LogiIsConnected(deviceIndex) && LogitechGSDK.LogiHasForceFeedback(deviceIndex))
			{
				LogitechGSDK.LogiPlayFrontalCollisionForce(deviceIndex, Mathf.RoundToInt(100f * k));
			}
		}
	}

	public void PlayGrassSurfaceEffect(float k)
	{
		k = GameInput.InputManager.settings.Params.wheelParams.forceFeedback * k;
		m_grassEffect = Mathf.Max(m_grassEffect, k);
	}

	public void PlayGravelSurfaceEffect(float k)
	{
		k = GameInput.InputManager.settings.Params.wheelParams.forceFeedback * k;
		m_groundEffect = Mathf.Max(m_groundEffect, k);
	}

	public void StopEffects(bool instant = true)
	{
		m_stopEffectsRequested = true;
		if (instant)
		{
			DoStopEffectsIfNeed();
		}
	}

	private void DoStopEffectsIfNeed()
	{
		if (!m_stopEffectsRequested)
		{
			return;
		}
		m_stopEffectsRequested = false;
		if (!isInitiated)
		{
			return;
		}
		for (int i = 0; i < 4; i++)
		{
			if (LogitechGSDK.LogiIsConnected(i) && LogitechGSDK.LogiHasForceFeedback(i))
			{
				LogitechGSDK.LogiStopDamperForce(i);
				LogitechGSDK.LogiStopSpringForce(i);
				LogitechGSDK.LogiStopCarAirborne(i);
				LogitechGSDK.LogiPlayConstantForce(i, 0);
				LogitechGSDK.LogiStopBumpyRoadEffect(i);
				LogitechGSDK.LogiStopDirtRoadEffect(i);
				LogitechGSDK.LogiStopSlipperyRoadEffect(i);
				LogitechGSDK.LogiStopSoftstopForce(i);
				LogitechGSDK.LogiStopSurfaceEffect(i);
			}
		}
	}

	public bool IsFFBWheelExist()
	{
		if (LogitechGSDK.LogiUpdate())
		{
			for (int i = 0; i < 4; i++)
			{
				bool flag = LogitechGSDK.LogiIsConnected(i);
				bool flag2 = LogitechGSDK.LogiHasForceFeedback(i);
				bool flag3 = LogitechGSDK.LogiIsDeviceConnected(i, 0);
				if (flag && flag2 && flag3)
				{
					return true;
				}
			}
		}
		else
		{
			GameLog.Print(AlertLevel.Error, "Input", "LogitechFeedback> IsFeedbackDeviceExist failed!");
		}
		return false;
	}

	public void Enable()
	{
		if (isInitiated && m_isEnabled)
		{
			return;
		}
		m_isEnabled = true;
		if (isInitiated)
		{
			return;
		}
		try
		{
			if (LogitechGSDK.LogiSteeringInitialize(ignoreXInputControllers: false))
			{
				InputSettings.WheelParams wheelParams = GameInput.InputManager.settings.Params.wheelParams;
				wheelParams.SteeringWheelAngleChanged = (System.Action)Delegate.Combine(wheelParams.SteeringWheelAngleChanged, new System.Action(UpdateWheelRange));
				StopEffects(instant: false);
				UpdateWheelRange();
				isInitiated = true;
				GameLog.Print(AlertLevel.Notify, "Input", "LogitechFeedback> Initialization success!");
			}
			else
			{
				isInitiated = false;
				GameLog.Print(AlertLevel.Notify, "Input", "LogitechFeedback> Initialization failed!");
			}
		}
		catch
		{
			isInitiated = false;
			GameLog.Print(AlertLevel.Error, "Input", "LogitechFeedback> Failed to attach!");
		}
	}

	public void Disable()
	{
		if (m_isEnabled && isInitiated)
		{
			m_isEnabled = false;
			InputSettings.WheelParams wheelParams = GameInput.InputManager.settings.Params.wheelParams;
			wheelParams.SteeringWheelAngleChanged = (System.Action)Delegate.Remove(wheelParams.SteeringWheelAngleChanged, new System.Action(UpdateWheelRange));
			StopEffects();
			GameLog.Print(AlertLevel.Notify, "Input", "LogitechFeedback> Finalization!");
		}
	}

	public void Update()
	{
		if (!isInitiated)
		{
			return;
		}
		if (LogitechGSDK.LogiUpdate())
		{
			DoStopEffectsIfNeed();
			int deviceIndex = m_deviceIndex;
			if (LogitechGSDK.LogiIsConnected(deviceIndex) && LogitechGSDK.LogiHasForceFeedback(deviceIndex))
			{
				if (m_grassEffect > 0f)
				{
					LogitechGSDK.LogiPlayDirtRoadEffect(deviceIndex, Mathf.RoundToInt(100f * m_grassEffect));
				}
				else
				{
					LogitechGSDK.LogiStopDirtRoadEffect(deviceIndex);
				}
				if (m_groundEffect > 0f)
				{
					LogitechGSDK.LogiPlayBumpyRoadEffect(deviceIndex, Mathf.RoundToInt(100f * m_groundEffect));
				}
				else
				{
					LogitechGSDK.LogiStopBumpyRoadEffect(deviceIndex);
				}
			}
		}
		m_grassEffect = Mathf.Max(0f, m_grassEffect - Time.deltaTime);
		m_groundEffect = Mathf.Max(0f, m_groundEffect - Time.deltaTime);
	}
}
