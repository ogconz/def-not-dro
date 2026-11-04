using System.Collections.Generic;
using UnityEngine;

public class ElementList<T> : MonoBehaviour where T : class, IElementItem
{
	protected int m_selectionIndex;

	protected readonly List<T> m_subItems = new List<T>();

	public T GetSelectedItem()
	{
		if (m_selectionIndex < 0 || m_selectionIndex >= m_subItems.Count)
		{
			return null;
		}
		return m_subItems[m_selectionIndex];
	}

	private void UpdateList()
	{
		foreach (T subItem in m_subItems)
		{
			subItem.UpdateElement();
		}
	}

	public T NavigationPrev()
	{
		T selectedItem = GetSelectedItem();
		if (selectedItem == null)
		{
			return null;
		}
		int selectionIndex = m_selectionIndex;
		m_selectionIndex--;
		if (m_selectionIndex < 0)
		{
			m_selectionIndex = 0;
		}
		if (selectionIndex != m_selectionIndex)
		{
			selectedItem.UnSelect();
			GetSelectedItem()?.Select();
		}
		UpdateList();
		return GetSelectedItem();
	}

	public T NavigationNext()
	{
		T selectedItem = GetSelectedItem();
		if (selectedItem == null)
		{
			return null;
		}
		int selectionIndex = m_selectionIndex;
		m_selectionIndex++;
		if (m_selectionIndex >= m_subItems.Count)
		{
			m_selectionIndex = m_subItems.Count - 1;
		}
		if (selectionIndex != m_selectionIndex)
		{
			selectedItem.UnSelect();
			GetSelectedItem()?.Select();
		}
		UpdateList();
		return GetSelectedItem();
	}

	public void Select(int index)
	{
		if (m_selectionIndex != index)
		{
			GetSelectedItem()?.UnSelect();
			m_selectionIndex = index;
			GetSelectedItem()?.Select();
		}
	}
}
