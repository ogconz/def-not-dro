using System;
using System.Runtime.InteropServices;

public class WinLibKernel
{
	public IntPtr libPtr;

	private bool m_isFree = true;

	public bool Load(string fileName)
	{
		if (!m_isFree)
		{
			return true;
		}
		libPtr = LoadLibrary(fileName);
		if (libPtr == IntPtr.Zero)
		{
			throw new Exception("LibKernel : Failed to load library" + libPtr);
		}
		m_isFree = false;
		return true;
	}

	public bool Free()
	{
		if (m_isFree)
		{
			return false;
		}
		if (libPtr != IntPtr.Zero)
		{
			while (FreeLibrary(libPtr) != 0)
			{
			}
			libPtr = IntPtr.Zero;
			return true;
		}
		m_isFree = true;
		return false;
	}

	public IntPtr GetAddress(IntPtr hModule, string lpProcName)
	{
		return GetProcAddress(hModule, lpProcName);
	}

	[DllImport("Kernel32.dll")]
	private static extern IntPtr LoadLibrary(string lpLibFileName);

	[DllImport("Kernel32.dll")]
	private static extern int FreeLibrary(IntPtr hLibModule);

	[DllImport("Kernel32.dll")]
	private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
}
