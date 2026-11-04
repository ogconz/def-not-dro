using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public static class FocusState
{
	private static bool m_lastUnityFocusState;

	public static Action<bool> onChanged;

	private static bool m_focusDesync;

	private static int? m_processId;

	public static bool IsActive => m_lastUnityFocusState;

	public static bool IsRealFixedActive
	{
		get
		{
			if (m_focusDesync)
			{
				return CalcRealFocusState();
			}
			return IsActive;
		}
	}

	public static int ProcessId
	{
		get
		{
			if (!m_processId.HasValue)
			{
				using Process process = Process.GetCurrentProcess();
				m_processId = process.Id;
			}
			return m_processId.Value;
		}
	}

	public static void ForceActive()
	{
		m_lastUnityFocusState = true;
	}

	public static void OnApplicationFocusCallback(bool unityFocus)
	{
		bool flag = CalcRealFocusState();
		m_focusDesync = unityFocus != flag;
		m_lastUnityFocusState = unityFocus;
		onChanged.SafeCall(unityFocus);
	}

	private static bool CalcRealFocusState()
	{
		return ApplicationIsActivated();
	}

	public static bool ApplicationIsActivated()
	{
		IntPtr foregroundWindow = GetForegroundWindow();
		if (foregroundWindow == IntPtr.Zero)
		{
			return false;
		}
		int processId = ProcessId;
		GetWindowThreadProcessId(foregroundWindow, out var processId2);
		return processId2 == processId;
	}

	[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
	private static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);
}
