using System;
using UnityEngine;

public static class TimeTracker
{
	private static long m_timeInFrame;

	private static int m_lastCheckFrame = -1;

	public static long launchTime { get; private set; }

	public static long timeInFrame
	{
		get
		{
			if (m_lastCheckFrame != Time.frameCount)
			{
				m_lastCheckFrame = Time.frameCount;
				m_timeInFrame = GetUptimeInSec();
			}
			return m_timeInFrame;
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void AutoInitialize()
	{
		launchTime = GetUptimeInSec();
	}

	public static long GetUptimeInSec()
	{
		return DateTime.UtcNow.Ticks / 10000000;
	}
}
