using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class SetOfValuesPrinter : StringPrinter
{
	private List<string> m_values = new List<string>();

	private List<string> m_names = new List<string>();

	private string m_title = "";

	public SetOfValuesPrinter(string title)
	{
		m_title = title + ": ";
	}

	public SetOfValuesPrinter AddVarValue(string value)
	{
		m_values.Add(value);
		return this;
	}

	public SetOfValuesPrinter AddVarName(string value)
	{
		m_names.Add(value);
		return this;
	}

	public string Print()
	{
		if (m_names.Count != m_values.Count)
		{
			return "Error: names and values counts are different!";
		}
		int num = 0;
		foreach (string value in m_values)
		{
			num = Mathf.Max(num, value.Length);
		}
		int num2 = 0;
		foreach (string name in m_names)
		{
			num2 = Mathf.Max(num2, name.Length);
		}
		int num3 = num + num2 + 2;
		int num4 = lineWidth / num3;
		int totalWidth = (lineWidth - num4 * num3) / num4 + 1;
		StringBuilder stringBuilder = new StringBuilder(m_title + Environment.NewLine + " ");
		int num5 = 0;
		for (int i = 0; i < m_values.Count; i++)
		{
			stringBuilder.Append(m_values[i].PadLeft(num, ' ') + ":" + m_names[i].PadRight(num2));
			num5++;
			if (num5 == num4)
			{
				num5 = 0;
				if (i != m_values.Count - 1)
				{
					stringBuilder.Append(Environment.NewLine + " ");
				}
			}
			else
			{
				stringBuilder.Append("".PadLeft(totalWidth, ' '));
			}
		}
		return stringBuilder.ToString();
	}
}
