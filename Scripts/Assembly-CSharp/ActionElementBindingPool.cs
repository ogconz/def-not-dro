using System;
using System.Collections.Generic;
using Binding.Components;
using GameInput;
using UnityEngine;

public class ActionElementBindingPool
{
	private List<ActionElementBinding> m_hints = new List<ActionElementBinding>(10);

	public ActionElementBinding AllocateNext(GameObject prefabHint, Transform hintsContainer)
	{
		for (int i = 0; i < m_hints.Count; i++)
		{
			ActionElementBinding actionElementBinding = m_hints[i];
			if (!actionElementBinding.gameObject.activeSelf)
			{
				actionElementBinding.gameObject.SetActive(value: true);
				return actionElementBinding;
			}
		}
		ActionElementBinding component = UnityEngine.Object.Instantiate(prefabHint, hintsContainer).GetComponent<ActionElementBinding>();
		if (component != null)
		{
			m_hints.Add(component);
			return component;
		}
		return null;
	}

	public void Clear()
	{
		foreach (ActionElementBinding hint in m_hints)
		{
			hint.Release();
		}
	}

	public void CleanUp()
	{
		Clear();
		for (int i = 0; i < m_hints.Count; i++)
		{
			ActionElementBinding actionElementBinding = m_hints[i];
			if (actionElementBinding != null && actionElementBinding.gameObject != null)
			{
				UnityEngine.Object.Destroy(actionElementBinding.gameObject);
			}
			m_hints[i] = null;
		}
		m_hints.Clear();
	}

	public void ReleaseElement(ActionElementBinding hint)
	{
		hint.Release();
	}

	public ActionElementBinding GetElement(ActionType typeAction)
	{
		return m_hints.Find((ActionElementBinding x) => x.actionType == typeAction && x.gameObject.activeInHierarchy);
	}

	public ActionElementBinding GetElement(Predicate<ActionElementBinding> predicate)
	{
		return m_hints.Find(predicate);
	}
}
