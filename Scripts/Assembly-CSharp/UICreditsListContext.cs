using Binding;
using Binding.Base;
using GameInput;
using UnityEngine;
using UnityEngine.UI;

[RaycastBlocker(true)]
[GameHotkeyBlocker(true)]
public class UICreditsListContext : BaseBindingBehaviourTarget
{
	private const float START_SCROLL_POSITION = 1.13f;

	[SerializeField]
	private GitInfoAsset m_gitInfoAsset;

	[SerializeField]
	private ScrollRect m_scrollView;

	[SerializeField]
	private float m_speedScroll = 1f;

	private float m_coeff = 1f;

	private float m_speed;

	private StringProperty m_gitSHA = new StringProperty("");

	protected override void OnActivate()
	{
		base.OnActivate();
		RefreshScroll();
		ShowTopPanel(ETopPanelState.Title, LocalizationKeys.credits.Localized());
		ShowBottomPanel(new ActionHintData[1]
		{
			new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_goto_back.Localized(),
				type = ActionType.NavCancelBtnB,
				action = NavCancel
			}
		});
		m_gitSHA.value = m_gitInfoAsset.ShaShort;
	}

	private void Update()
	{
		ProcessHoldNavBtn();
		float num = ((Time.timeScale < 1f) ? Time.unscaledDeltaTime : Time.deltaTime);
		m_speed += num * m_speedScroll * m_coeff;
		m_scrollView.verticalNormalizedPosition = 1.13f - m_speed;
		if (m_scrollView.verticalNormalizedPosition < -0.14f || m_scrollView.verticalNormalizedPosition > 1.16f)
		{
			RefreshScroll();
		}
	}

	public void NavCancel()
	{
		Hide();
		if (States.mainCurrent is GarageGUIState garageGUIState)
		{
			UIMainMenuContext uIMainMenuContext = garageGUIState.contextOwner.Get<UIMainMenuContext>();
			if (uIMainMenuContext != null)
			{
				uIMainMenuContext.OnSwitchToSettings();
			}
		}
	}

	private void ProcessHoldNavBtn()
	{
		InputManager instance = InputManager.instance;
		m_coeff = 1f;
		if (instance.IsActionHold(ActionType.NavUp))
		{
			m_coeff += 2f;
		}
		if (instance.IsActionHold(ActionType.NavDown))
		{
			m_coeff -= 3f;
		}
	}

	private void RefreshScroll()
	{
		m_scrollView.verticalNormalizedPosition = 1.13f;
		m_speed = 0f;
	}
}
