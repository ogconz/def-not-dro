using System;
using System.Collections.Generic;
using Binding.Components;
using UnityEngine;

[Serializable]
public class SwitcherActions
{
	public GameObject actionPrefab;

	public Transform actionsContainer;

	private List<HeaderActionBinding> m_listActionsPool = new List<HeaderActionBinding>();

	public List<HeaderActionBinding> listActions { get; } = new List<HeaderActionBinding>();

	public int selectedActionIndex { get; private set; } = -1;

	private HeaderActionBinding InstantiateHint(string text, Action action, bool hasNotification, HeaderActionBinding.ViewType viewType)
	{
		if (actionPrefab == null)
		{
			return null;
		}
		HeaderActionBinding headerActionBinding = Allocate();
		if (headerActionBinding != null)
		{
			headerActionBinding.SetData(this, text, action, hasNotification, viewType);
		}
		return headerActionBinding;
	}

	private HeaderActionBinding Allocate()
	{
		HeaderActionBinding headerActionBinding = null;
		if (m_listActionsPool.Count > 0)
		{
			headerActionBinding = m_listActionsPool[0];
			headerActionBinding.gameObject.SetActive(value: true);
			m_listActionsPool.RemoveAt(0);
		}
		if (headerActionBinding == null)
		{
			headerActionBinding = UnityEngine.Object.Instantiate(actionPrefab, actionsContainer).GetComponent<HeaderActionBinding>();
		}
		if (headerActionBinding != null)
		{
			listActions.Add(headerActionBinding);
			headerActionBinding.transform.SetAsLastSibling();
		}
		return headerActionBinding;
	}

	public void AddActions(HeaderActionData[] headerActions)
	{
		if (headerActions != null)
		{
			int num = listActions.Count;
			for (int i = 0; i < headerActions.Length; i++)
			{
				HeaderActionData headerActionData = headerActions[i];
				InstantiateHint(headerActionData.text, headerActionData.action, headerActionData.hasNotification, headerActionData.viewType).indexAction = num;
				num++;
			}
		}
	}

	public void RemoveAction(HeaderActionBinding binding)
	{
		binding.gameObject.SetActive(value: false);
		listActions.Remove(binding);
		m_listActionsPool.Add(binding);
	}

	public void ClearActions()
	{
		selectedActionIndex = 0;
		foreach (HeaderActionBinding listAction in listActions)
		{
			listAction.gameObject.SetActive(value: false);
			m_listActionsPool.Add(listAction);
		}
		listActions.Clear();
	}

	public void Select(int index)
	{
		ResetSelectionAndSetIndexOnly(index);
		if (index >= 0 && index < listActions.Count)
		{
			listActions[index].Select();
		}
	}

	public void ResetSelectionAndSetIndexOnly(int index)
	{
		foreach (HeaderActionBinding listAction in listActions)
		{
			listAction.ResetSelection();
		}
		selectedActionIndex = index;
	}

	public void SelectionAction(int index)
	{
		ResetSelectionAndSetIndexOnly(index);
		if (index >= 0 && index < listActions.Count)
		{
			listActions[index].OnClick();
		}
		else if (listActions.Count > 0)
		{
			listActions[0].OnClick();
		}
	}

	public void OnNextPage()
	{
		selectedActionIndex++;
		if (selectedActionIndex >= listActions.Count)
		{
			selectedActionIndex = listActions.Count - 1;
		}
		if (selectedActionIndex >= 0 && listActions.Count > 0)
		{
			listActions[selectedActionIndex].OnClick();
		}
	}

	public void OnPrevPage()
	{
		selectedActionIndex--;
		if (selectedActionIndex < 0)
		{
			selectedActionIndex = 0;
		}
		if (selectedActionIndex >= 0 && listActions.Count > 0)
		{
			listActions[selectedActionIndex].OnClick();
		}
	}
}
