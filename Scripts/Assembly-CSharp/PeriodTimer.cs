using System;
using UnityEngine;

public class PeriodTimer
{
	private readonly float m_period;

	private float m_currentTime;

	private readonly Action m_callBack;

	public PeriodTimer(float period, Action callBack)
	{
		m_period = period;
		m_callBack = callBack;
		Reset();
	}

	public void Reset(float newPeriod)
	{
		m_currentTime = newPeriod;
	}

	public void Reset()
	{
		m_currentTime = m_period;
	}

	public void Update()
	{
		Update(Time.deltaTime);
	}

	public void Update(float deltaTime)
	{
		m_currentTime -= deltaTime;
		if (m_currentTime < 0f)
		{
			m_callBack.Call();
			Reset();
		}
	}
}
