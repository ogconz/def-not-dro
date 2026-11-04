using System;
using System.Collections.Generic;
using Binding;
using UnityEngine;

public class GridListProperty : ListProperty
{
	public enum GridNavitation
	{
		Up = 0,
		Down = 1,
		Left = 2,
		Right = 3
	}

	private readonly bool m_useHorizontalClamp;

	private readonly int m_columnCount;

	private readonly Dictionary<GridNavitation, int> m_navigationIndexes = new Dictionary<GridNavitation, int>();

	public GridListProperty(int columnCount, bool useHorizontalClamp = true)
	{
		m_columnCount = columnCount;
		m_useHorizontalClamp = useHorizontalClamp;
		FillNavitaionIndexes();
	}

	private void FillNavitaionIndexes()
	{
		m_navigationIndexes[GridNavitation.Up] = -m_columnCount;
		m_navigationIndexes[GridNavitation.Down] = m_columnCount;
		m_navigationIndexes[GridNavitation.Left] = -1;
		m_navigationIndexes[GridNavitation.Right] = 1;
	}

	public void Selection(GridNavitation navigationDirection)
	{
		int selectedIndex = GetSelectedIndex();
		int targetIndex = selectedIndex + m_navigationIndexes[navigationDirection];
		int num = targetIndex / m_columnCount;
		double num2 = Math.Ceiling((double)base.count / (double)m_columnCount) - 1.0;
		if (navigationDirection == GridNavitation.Down && (double)num <= num2)
		{
			targetIndex = Mathf.Clamp(targetIndex, 0, base.count - 1);
		}
		if (CheckIndexRange(targetIndex))
		{
			if ((navigationDirection == GridNavitation.Left || navigationDirection == GridNavitation.Right) && m_useHorizontalClamp)
			{
				HorizontalIndexClampOnRow(ref targetIndex, selectedIndex / m_columnCount);
			}
			Selection(targetIndex, selected: true);
		}
	}

	private void HorizontalIndexClampOnRow(ref int targetIndex, int activeRowIndex)
	{
		targetIndex = Mathf.Clamp(targetIndex, activeRowIndex * m_columnCount, (activeRowIndex + 1) * m_columnCount - 1);
	}
}
