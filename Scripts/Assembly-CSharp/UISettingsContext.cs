using System;
using System.Collections;
using Binding.Base;
using GameInput;
using UnityEngine;

[GameHotkeyBlocker(true)]
[AudioFXBlocker(true)]
public class UISettingsContext : BaseBindingBehaviourTarget
{
	private UISettingsButtonsPanelContext m_settingsBtnsPanel;

	private ElementNavigate[] m_settingsNavigates;

	protected override void OnActivate()
	{
		base.OnActivate();
		BringForwardByDepth();
		ShowTopPanel(ETopPanelState.Title, LocalizationKeys.screen_title_settings.Localized());
		ShowBottomPanel(new ActionHintData[2]
		{
			new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_goto_back.Localized(),
				type = ActionType.NavCancelBtnB,
				action = NavigateCancel
			},
			new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_select.Localized(),
				type = ActionType.NavSelectBtnA,
				action = OnSelect
			}
		});
		StartCoroutine(DeferredCall());
	}

	private void OnSelect()
	{
		if (m_settingsBtnsPanel != null)
		{
			m_settingsBtnsPanel.SubmitCurrentSelected();
		}
	}

	protected override void OnDeactivate()
	{
		HideTopPanel();
		HideBottomPanel();
		base.OnDeactivate();
	}

	private IEnumerator DeferredCall()
	{
		yield return new WaitForEndOfFrame();
		if (m_settingsBtnsPanel == null)
		{
			m_settingsBtnsPanel = GetComponentInChildren<UISettingsButtonsPanelContext>();
			if ((bool)m_settingsBtnsPanel)
			{
				m_settingsBtnsPanel.SetOwner(this);
			}
			SelectFirstElement(m_settingsBtnsPanel);
			ContextOwner contextOwner = base.owner;
			contextOwner.onTopContextChanged = (Action<BaseContext>)Delegate.Remove(contextOwner.onTopContextChanged, new Action<BaseContext>(CheckTopContext));
			ContextOwner contextOwner2 = base.owner;
			contextOwner2.onTopContextChanged = (Action<BaseContext>)Delegate.Combine(contextOwner2.onTopContextChanged, new Action<BaseContext>(CheckTopContext));
		}
	}

	private void SelectFirstElement(BaseContext context)
	{
		if (!context)
		{
			return;
		}
		if (m_settingsNavigates == null)
		{
			m_settingsNavigates = m_settingsBtnsPanel.GetComponentsInChildren<ElementNavigate>();
		}
		if (m_settingsNavigates != null && m_settingsNavigates.Length != 0)
		{
			if (base.selectedItem != null)
			{
				base.selectedNavigate.Deselect();
			}
			m_settingsNavigates[0].Select();
		}
	}

	private void CheckTopContext(BaseContext prevContext)
	{
		if (base.owner.topContext == this && m_settingsBtnsPanel != null)
		{
			m_settingsBtnsPanel.RegisterPanel();
		}
	}

	public void OnClickCancel()
	{
		base.owner.Pop();
	}

	public void NavigateCancel()
	{
		OnClickCancel();
	}

	public static void Show(ContextOwner owner)
	{
		if (owner == null)
		{
			UIUtils.LogNullReference("UISettingsContext.Show", "owner");
		}
		else
		{
			owner.HideStackAndPush<UISettingsContext>();
		}
	}
}
