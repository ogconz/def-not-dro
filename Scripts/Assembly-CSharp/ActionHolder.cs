using System;
using UnityEngine;

public class ActionHolder
{
	private readonly float m_timeToExecute;

	private readonly Action m_action;

	public ActionHolder(float timeToExecute, Action action)
	{
		m_timeToExecute = timeToExecute;
		m_action = action;
	}

	public bool IsReady()
	{
		return m_timeToExecute <= Time.realtimeSinceStartup;
	}

	public virtual void Call()
	{
		if (m_action == null)
		{
			return;
		}
		try
		{
			m_action();
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "TimeController", "Exception in delayed callback: " + ex.ToString());
		}
	}
}
