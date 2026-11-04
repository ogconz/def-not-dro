using System;
using System.Collections.Generic;
using UnityEngine;

public static class UICustomUpdater
{
	public static int updatedFrame = -1;

	private static List<Action> m_actions = new List<Action>(16);

	public static void RegisterAction(Action action)
	{
		m_actions.Add(action);
	}

	public static void UnregisterAction(Action action)
	{
		m_actions.Remove(action);
	}

	public static void Clear()
	{
		m_actions.Clear();
	}

	public static void Update()
	{
		updatedFrame = Time.frameCount;
		try
		{
			for (int num = m_actions.Count - 1; num >= 0; num--)
			{
				m_actions[num].Call();
			}
		}
		catch
		{
		}
		finally
		{
			m_actions.Clear();
		}
	}
}
