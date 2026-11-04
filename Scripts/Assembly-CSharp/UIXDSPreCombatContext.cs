using System;
using Binding.Base;
using DI;
using GameInput;

[AudioFXBlocker(false)]
public class UIXDSPreCombatContext : BaseBindingBehaviourTarget
{
	private GamePrefs m_prefs;

	private int m_selectedId = -1;

	public Action<int> onCarSelected;

	public UICarSelectorContext catalog { get; private set; }

	protected override void OnActivate()
	{
		m_prefs = DependencyInjector.Resolve<GamePrefs>();
		base.OnActivate();
		ShowTopPanel(ETopPanelState.Title, LocalizationKeys.xds_ui_select_car.Localized());
		ShowBottomPanel(new ActionHintData[2]
		{
			new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_exit.Localized(),
				type = ActionType.NavCancelBtnB,
				action = GotoGarage
			},
			new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_select.Localized(),
				type = ActionType.NavSelectBtnA,
				action = OnContinueClick
			}
		});
		catalog = base.owner.Get<UICarSelectorContext>();
		if (catalog != null && catalog.isActive)
		{
			base.owner.Hide<UICarSelectorContext>();
		}
		if (!catalog.isActive)
		{
			catalog = base.owner.Push<UICarSelectorContext>();
		}
		PlayerCarControl.instance.SetBlockControl(value: true);
		catalog.onCarSelected = OnContinueWithCar;
		UICarSelectorContext uICarSelectorContext = catalog;
		uICarSelectorContext.goBackAction = (System.Action)Delegate.Remove(uICarSelectorContext.goBackAction, new System.Action(GotoGarage));
		UICarSelectorContext uICarSelectorContext2 = catalog;
		uICarSelectorContext2.goBackAction = (System.Action)Delegate.Combine(uICarSelectorContext2.goBackAction, new System.Action(GotoGarage));
	}

	public void GotoGarage()
	{
		States.Change<GarageGUIState>();
	}

	protected override bool OnEscape()
	{
		GotoGarage();
		return true;
	}

	private void OnCarSelected(UICarSelectorCar car)
	{
		m_selectedId = car.meta.id;
	}

	public void OnContinueWithCar(UICarSelectorCar carItem)
	{
		OnCarSelected(carItem);
		OnContinueClick();
	}

	public void OnContinueClick()
	{
		if (m_selectedId != -1)
		{
			Hide();
			base.owner.Hide<UICarSelectorContext>();
			HideBottomPanel();
			HideTopPanel();
			onCarSelected.SafeCall(m_selectedId);
		}
	}
}
