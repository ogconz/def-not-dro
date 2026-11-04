using System;
using System.Collections;
using System.Collections.Generic;
using GameInput;
using Rewired;
using UnityEngine;

public class PCFFBManager : IFFBManager
{
	private const string LOGITECH_VID = "046d";

	private const string THRUSTMASTER_VID = "044f";

	private IForceFeedbackSystem m_feedBack;

	private Controller currController;

	private int m_last_dinput_value = -1;

	public IForceFeedbackSystem feedback
	{
		get
		{
			return m_feedBack;
		}
		private set
		{
			m_feedBack = value;
		}
	}

	public void Initialize()
	{
	}

	public void Release()
	{
		feedback?.Disable();
		feedback = null;
	}

	private void TryInitFFBWheelInterface(bool autoDetectIfNeedDI, Controller сontrollerToDetectInterface = null)
	{
		if (feedback != null)
		{
			return;
		}
		Type[] array = ((!DetermineDInputMode(autoDetectIfNeedDI, сontrollerToDetectInterface)) ? new Type[2]
		{
			typeof(LogitechFeedback),
			typeof(DirectInputFeedback)
		} : new Type[2]
		{
			typeof(DirectInputFeedback),
			typeof(LogitechFeedback)
		});
		foreach (Type type in array)
		{
			feedback = Activator.CreateInstance(type) as IForceFeedbackSystem;
			if (TestInterface(feedback))
			{
				GameLog.Print(AlertLevel.Notify, "System", "WheelForceFeedbackSupport> " + type.Name + " mode <color=green>selected</color>");
				return;
			}
			GameLog.Print(AlertLevel.Notify, "System", "WheelForceFeedbackSupport> " + type.Name + " mode <color=red>discarded</color>");
			TimeController.StartNow(DisableFeedbackWithDelay(feedback));
			feedback = null;
		}
		InitNullFeedback();
	}

	private void InitNullFeedback()
	{
		feedback = new NullFeedback();
		feedback.Initialize();
		feedback.Enable();
		GameLog.Print(AlertLevel.Notify, "System", "WheelForceFeedbackSupport> <color=yellow>Null mode</color>");
	}

	private static bool DetermineDInputMode(bool autoDetectIfNeedDI, Controller prefferedController)
	{
		bool result = GameConsole.Bool["dinput"];
		if (!autoDetectIfNeedDI)
		{
			return result;
		}
		if (prefferedController != null && TryAquireDIParamByController(prefferedController, out var dinput) && dinput.HasValue)
		{
			return dinput.Value;
		}
		IList<Controller> controllers = ReInput.controllers.Controllers;
		for (int i = 0; i < controllers.Count; i++)
		{
			if (TryAquireDIParamByController(controllers[i], out var dinput2) && dinput2.HasValue)
			{
				return dinput2.Value;
			}
		}
		return result;
	}

	private static bool TryAquireDIParamByController(Controller controller, out bool? dinput)
	{
		dinput = null;
		if (controller == null || controller.type != ControllerType.Joystick)
		{
			return false;
		}
		GetControllerVendorIdentifiers(controller, out var isLogitech, out var isThrustmaster);
		if (isLogitech)
		{
			GameLog.Print(AlertLevel.Notify, "System", "WheelForceFeedbackSupport> <color=white>autodetected Logitech</color>");
			dinput = false;
			return true;
		}
		if (isThrustmaster)
		{
			GameLog.Print(AlertLevel.Notify, "System", "WheelForceFeedbackSupport> <color=white>autodetected Thrustmaster</color>");
			dinput = true;
			return true;
		}
		return false;
	}

	private static void GetControllerVendorIdentifiers(Controller controller, out bool isLogitech, out bool isThrustmaster)
	{
		isLogitech = controller?.hardwareIdentifier.ToLower().Contains("046d") ?? false;
		isThrustmaster = controller?.hardwareIdentifier.ToLower().Contains("044f") ?? false;
	}

	private IEnumerator DisableFeedbackWithDelay(IForceFeedbackSystem fb)
	{
		yield return new WaitForSeconds(0.3f);
		fb.Disable();
	}

	public void OnControllerListChanged(Controller addedController, Controller removedController)
	{
		if (currController == null && addedController == null && removedController == null)
		{
			InitNullFeedback();
			return;
		}
		if (currController != null && (currController == removedController || (addedController != null && addedController != currController)))
		{
			Release();
			InitNullFeedback();
			currController = null;
		}
		if (currController == null && addedController != null)
		{
			ControllerMetaInfo metaInfo = addedController.GetMetaInfo();
			if (metaInfo != null && !metaInfo.disableFFB && metaInfo.deviceType == GameInput.DeviceType.Wheel)
			{
				currController = addedController;
				Release();
				TryInitFFBWheelInterface(autoDetectIfNeedDI: true, addedController);
			}
		}
	}

	private bool TestInterface(IForceFeedbackSystem feedbackSystem)
	{
		if (feedbackSystem is DirectInputFeedback directInputFeedback)
		{
			directInputFeedback.SetTargetDeviceHardwareID(currController?.hardwareIdentifier);
		}
		feedback.Initialize();
		feedback.Enable();
		if (feedback.isInitiated)
		{
			return feedback.IsFFBWheelExist();
		}
		return false;
	}

	public void OnUpdateDInputSettings()
	{
		bool flag = GameConsole.Bool["dinput"];
		int num = GameConsole.Int["dinput"];
		if (feedback != null)
		{
			bool num2 = feedback is LogitechFeedback;
			bool flag2 = feedback is DirectInputFeedback;
			if (num2)
			{
				GameInput.InputManager.instance.ResetMetaDeviceData();
			}
			bool num3 = (flag != flag2) | (m_last_dinput_value != num);
			m_last_dinput_value = num;
			if (num3)
			{
				Release();
				TryInitFFBWheelInterface(autoDetectIfNeedDI: false);
			}
		}
	}

	public void OnFailedCurrentllySupported(GameInput.DeviceType lastSteeringDeviceType, ControllerMetaInfo oldSteeringDevice, ControllerMetaInfo lastSteeringDevice)
	{
		if (lastSteeringDeviceType == GameInput.DeviceType.Wheel && oldSteeringDevice != lastSteeringDevice)
		{
			GameConsole.SetVar("dinput", (feedback is LogitechFeedback) ? "1" : "0");
			GameConsole.UserCommand("dinput");
		}
	}

	public void OnEnable()
	{
		GameEvents.OnGameEngineStop += new GameEventHandler<GameEvents.EventInfo>(OnGameEngineStop);
		if (feedback != null)
		{
			feedback.Enable();
			feedback.StopEffects(instant: true);
		}
	}

	public void OnDisable()
	{
		GameEvents.OnGameEngineStop -= new GameEventHandler<GameEvents.EventInfo>(OnGameEngineStop);
	}

	private void OnGameEngineStop(GameEvents.EventInfo e)
	{
		Release();
	}

	public void OnAppFocusChanged(bool focusState)
	{
		if (focusState)
		{
			TryInitFFBWheelInterface(autoDetectIfNeedDI: true);
			feedback.Enable();
		}
		else if (feedback != null)
		{
			feedback.Disable();
		}
	}

	public void Update()
	{
		feedback.Update();
	}
}
