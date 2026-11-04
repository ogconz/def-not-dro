using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ValidItem : IValidComponentProcess
{
	public string type;

	public int min;

	public int max;

	public int current;

	public IValidComponentProcess validComponentProcess;

	public List<Component> components;

	private bool m_isSuccess;

	public bool isSuccess => m_isSuccess;

	public string processMessage => ToString();

	public ValidItem(string type, int min, int max, IValidComponentProcess validComponentProcess = null, int current = 0, List<Component> components = null)
	{
		this.type = type;
		this.min = min;
		this.max = max;
		this.current = current;
		this.validComponentProcess = validComponentProcess;
		this.components = components ?? new List<Component>();
		m_isSuccess = true;
	}

	public string ToStat()
	{
		return $"{type} : {current} count ({min} min - {max} max)";
	}

	public override string ToString()
	{
		string text = string.Empty;
		IValidComponentProcess validComponentProcess = this.validComponentProcess;
		if (validComponentProcess != null && !validComponentProcess.isSuccess)
		{
			text = text + this.validComponentProcess.processMessage + "\n";
		}
		if (current < min)
		{
			text += $"There are less than {min} {type}\n";
		}
		if (current > max)
		{
			text += $"There are more than {max} {type}\n";
		}
		return text;
	}

	public void ValidProcess()
	{
		foreach (Component component in components)
		{
			ValidProcess(component);
		}
		m_isSuccess = m_isSuccess && current >= min && current <= max;
	}

	public void ValidProcess(Component comp)
	{
		validComponentProcess?.ValidProcess(comp);
		m_isSuccess = m_isSuccess && (validComponentProcess?.isSuccess ?? true);
	}

	public void Reset()
	{
		validComponentProcess?.Reset();
		components.Clear();
		m_isSuccess = true;
	}
}
