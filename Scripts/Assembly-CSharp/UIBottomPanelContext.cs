using System;
using System.Collections.Generic;
using Binding.Components;
using GameInput;
using UnityEngine;

public class UIBottomPanelContext : BaseHintPanelContext
{
	[SerializeField]
	private BottomOnlyHint m_prefabOnlyHint;

	[SerializeField]
	private Transform hintsContainer;

	[SerializeField]
	private Transform hintsOnlyContainer;

	private List<BottomOnlyHint> m_onlyHintsPool = new List<BottomOnlyHint>();

	public static UIBottomPanelContext Show(ContextOwner owner, IReadOnlyList<ActionHintData> actions, IReadOnlyList<HintOnlyData> hintOnlies = null, bool ignoreRepeat = false)
	{
		if (owner == null)
		{
			UIUtils.LogNullReference("UIBottomPanelContext.Show", "owner");
			return null;
		}
		UIBottomPanelContext uIBottomPanelContext = (owner.IsActive<UIBottomPanelContext>() ? owner.Get<UIBottomPanelContext>() : owner.Add<UIBottomPanelContext>());
		if (uIBottomPanelContext != null)
		{
			uIBottomPanelContext.Clear();
			foreach (ActionHintData action in actions)
			{
				uIBottomPanelContext.AddElement(action.hint, action.type, action.action, ignoreRepeat);
			}
			if (hintOnlies != null && hintOnlies.Count > 0)
			{
				uIBottomPanelContext.AddOnlyHints(hintOnlies);
			}
			uIBottomPanelContext.BringForwardByDepth();
		}
		uIBottomPanelContext.UpdateCanvasOffsetInXrMode();
		return uIBottomPanelContext;
	}

	public override bool IsAllowNavigation()
	{
		return false;
	}

	private ActionElementBinding InstantiateHint(string hint, ActionType actionType, System.Action action, bool ignoreRepeat = false)
	{
		if (prefabHint == null)
		{
			return null;
		}
		ActionElementBinding actionElementBinding = m_aebPool.AllocateNext(prefabHint, hintsContainer);
		if (actionElementBinding != null)
		{
			actionElementBinding.SetData(hint, actionType, action, ignoreRepeat);
		}
		return actionElementBinding;
	}

	public virtual void Clear()
	{
		m_aebPool.Clear();
		for (int i = 0; i < m_onlyHintsPool.Count; i++)
		{
			m_onlyHintsPool[i].gameObject.SetActive(value: false);
		}
	}

	public void RemoveElement(ActionElementBinding hint)
	{
		m_aebPool.ReleaseElement(hint);
	}

	public ActionElementBinding AddElement(string hintText, ActionType typeAction, System.Action action, bool ignoreRepeat = false)
	{
		return InstantiateHint(hintText, typeAction, action, ignoreRepeat);
	}

	public void AddOnlyHints(IReadOnlyList<HintOnlyData> hintOnlyDatas)
	{
		GOPool.Allocate(m_prefabOnlyHint, ref m_onlyHintsPool, hintOnlyDatas.Count, delegate
		{
		}, delegate(BottomOnlyHint item)
		{
			item.gameObject.SetActive(value: false);
		});
		for (int num = 0; num < hintOnlyDatas.Count; num++)
		{
			BottomOnlyHint bottomOnlyHint = m_onlyHintsPool[num];
			bottomOnlyHint.SetHintText(hintOnlyDatas[num].hint);
			if (!bottomOnlyHint.gameObject.activeSelf)
			{
				bottomOnlyHint.gameObject.SetActive(value: true);
			}
		}
	}

	public ActionElementBinding GetElement(ActionType typeAction)
	{
		return m_aebPool.GetElement(typeAction);
	}

	public ActionElementBinding GetElement(Predicate<ActionElementBinding> predicate)
	{
		return m_aebPool.GetElement(predicate);
	}
}
