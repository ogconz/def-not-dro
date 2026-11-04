using System;
using System.Runtime.InteropServices;
using System.Text;
using AOT;
using DirectInputX;
using GameInput;
using Rewired;

public class DirectInputFeedback : IForceFeedbackSystem
{
	private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

	private bool m_isDisabled;

	private bool m_stopEffectsRequested;

	private RawForceCalculator m_rawForceCalculator;

	private static bool m_forceAngleLimitiSimulation = false;

	private string m_deviceHardwareID;

	private static object m_ref = new object();

	private static IntPtr m_wndHandle = IntPtr.Zero;

	public bool isInitiated { get; private set; }

	public DirectInputFeedback()
	{
		m_rawForceCalculator = new RawForceCalculator(SetRawForce);
	}

	private void SetRawForce(float force)
	{
		InputAPI.SetConstantForce(0f - force);
	}

	public void Initialize()
	{
		m_isDisabled = true;
	}

	public bool IsThisDeviceController(Controller controller)
	{
		if (!isInitiated || controller.type != ControllerType.Joystick)
		{
			return false;
		}
		ControllerMetaInfo controllerMetaInfo = controller.TryGetMetaInfo();
		if (controllerMetaInfo != null && controllerMetaInfo.deviceType == DeviceType.Wheel)
		{
			return true;
		}
		return false;
	}

	private void UpdateWheelRange()
	{
		if (isInitiated)
		{
			InputAPI.SetAngleRange((int)GameInput.InputManager.settings.Params.wheelParams.steeringWheelAngle);
		}
	}

	public void SetConstantForce(float k)
	{
		if (!m_isDisabled)
		{
			m_rawForceCalculator.SetConstantForce(k);
		}
	}

	public void PlayImpactEffect(float k)
	{
		if (!m_isDisabled)
		{
			m_rawForceCalculator.PlayImpactEffect(k);
		}
	}

	public void PlayGrassSurfaceEffect(float k)
	{
		m_rawForceCalculator.PlayGrassSurfaceEffect(k);
	}

	public void PlayGravelSurfaceEffect(float k)
	{
		m_rawForceCalculator.PlayGravelSurfaceEffect(k);
	}

	public void StopEffects(bool instant = true)
	{
		m_stopEffectsRequested = true;
		if (instant)
		{
			DoStopEffects();
		}
	}

	private void DoStopEffects()
	{
		if (m_stopEffectsRequested)
		{
			m_stopEffectsRequested = false;
			if (isInitiated)
			{
				m_rawForceCalculator.StopEffects();
				UpdateWheelRange();
			}
		}
	}

	public bool IsFFBWheelExist()
	{
		return true;
	}

	public void SetTargetDeviceHardwareID(string hardwareID)
	{
		m_deviceHardwareID = hardwareID;
	}

	public void Enable()
	{
		if (isInitiated && !m_isDisabled)
		{
			return;
		}
		m_isDisabled = false;
		if (isInitiated)
		{
			return;
		}
		if (m_wndHandle == IntPtr.Zero)
		{
			EnumWindows(EnumWindowsCallBack, IntPtr.Zero);
		}
		m_forceAngleLimitiSimulation = GameConsole.Int["dinput"] == 11;
		try
		{
			if (m_wndHandle != IntPtr.Zero)
			{
				StringBuilder stringBuilder = new StringBuilder(200);
				stringBuilder.Append(m_deviceHardwareID);
				if (InputAPI.Init(bWindowMode: true, m_wndHandle, m_forceAngleLimitiSimulation, stringBuilder))
				{
					InputSettings.WheelParams wheelParams = GameInput.InputManager.settings.Params.wheelParams;
					wheelParams.SteeringWheelAngleChanged = (System.Action)Delegate.Combine(wheelParams.SteeringWheelAngleChanged, new System.Action(UpdateWheelRange));
					isInitiated = true;
					GameLog.Print(AlertLevel.Notify, "Input", $"DirectInputFeedback> Activated (angle range support: {InputAPI.IsAngleRangeSupported()})");
					StopEffects(instant: false);
				}
				else
				{
					isInitiated = false;
					GameLog.Print(AlertLevel.Notify, "Input", "DirectInputFeedback> Have no ffb devices");
				}
			}
			else
			{
				isInitiated = false;
				GameLog.Print(AlertLevel.Notify, "Input", "DirectInputFeedback> Cant lock window");
			}
		}
		catch (Exception ex)
		{
			isInitiated = false;
			GameLog.Print(AlertLevel.Notify, "Input", "DirectInputFeedback> Something going wrong: " + ex.Message);
		}
	}

	public void Disable()
	{
		if (!m_isDisabled)
		{
			InputSettings.WheelParams wheelParams = GameInput.InputManager.settings.Params.wheelParams;
			wheelParams.SteeringWheelAngleChanged = (System.Action)Delegate.Remove(wheelParams.SteeringWheelAngleChanged, new System.Action(UpdateWheelRange));
			if (isInitiated)
			{
				InputAPI.Release();
				isInitiated = false;
				StopEffects();
				GameLog.Print(AlertLevel.Notify, "Input", "DirectInputFeedback> Released");
			}
		}
	}

	public void Update()
	{
		if (isInitiated)
		{
			InputAPI.Update();
			m_rawForceCalculator.Update(m_isDisabled);
			DoStopEffects();
		}
	}

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern int GetClassName(IntPtr hWnd, StringBuilder lpWindowText, int nMaxCount);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern int GetWindowThreadProcessId(HandleRef handle, out int processId);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr extraData);

	[MonoPInvokeCallback(typeof(EnumWindowsProc))]
	private static bool EnumWindowsCallBack(IntPtr hWnd, IntPtr lParam)
	{
		GetWindowThreadProcessId(new HandleRef(m_ref, hWnd), out var processId);
		int processId2 = FocusState.ProcessId;
		if (processId == processId2)
		{
			StringBuilder stringBuilder = new StringBuilder(100);
			GetClassName(hWnd, stringBuilder, stringBuilder.Capacity);
			if (stringBuilder.ToString().ToLower().Contains("unitywndclass"))
			{
				m_wndHandle = hWnd;
				return false;
			}
		}
		return true;
	}

	[DllImport("user32.dll")]
	private static extern IntPtr GetActiveWindow();

	public bool IsWheelAngleRangeSupported()
	{
		return InputAPI.IsAngleRangeSupported();
	}
}
