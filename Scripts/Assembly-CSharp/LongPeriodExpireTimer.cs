using System;
using System.Collections;
using System.Globalization;
using UnityEngine;

public class LongPeriodExpireTimer : IDisposable
{
	private double m_expireTime;

	private double m_timerInterval;

	private IEnumerator m_coroutineToRestart;

	public Action timerIntervalExpireEvent;

	public double expireTime
	{
		get
		{
			return m_expireTime;
		}
		set
		{
			m_expireTime = value;
			if (!expired)
			{
				RestartTimer(m_expireTime - TimerUtils.CurrentTime);
			}
		}
	}

	public bool expired => TimerUtils.CurrentTime >= expireTime;

	public void SetExpireTimeByInterval(TimeSpan value)
	{
		expireTime = TimerUtils.CurrentTime + value.TotalSeconds;
	}

	private IEnumerator Timer()
	{
		yield return new WaitForSeconds((float)m_timerInterval);
		timerIntervalExpireEvent.SafeCall();
	}

	private void Stop()
	{
		if (m_coroutineToRestart != null)
		{
			TimeController.RemoveCoroutine(m_coroutineToRestart);
			m_coroutineToRestart = null;
		}
	}

	private void Start()
	{
		m_coroutineToRestart = TimeController.RunCoroutine(Timer());
	}

	private void RestartTimer(double interval)
	{
		m_timerInterval = interval;
		Stop();
		Start();
	}

	public void Dispose()
	{
		Stop();
	}

	public override string ToString()
	{
		return TimerUtils.SecondsToDateTime(expireTime).ToString(CultureInfo.InvariantCulture);
	}
}
