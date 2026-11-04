using System;
using DI;
using GameOverlay;
using JukeBox;
using SharingSetups;
using SyncMultiplayer;
using UnityEngine;

public class GarageGUIState : BaseGameState
{
	private Type m_startContext;

	private GarageCar3dViewContext m_car3dViewContext;

	private CarVendorsAsset m_carVendorsAsset;

	public CarVendorsAsset carVendorsAsset => m_carVendorsAsset;

	public override void OnEnter()
	{
		base.OnEnter();
		base.contextOwner.Preload<UIBackgroundContext>();
		base.contextOwner.Preload<UITopPanelContext>();
		base.contextOwner.Preload<UIBottomPanelContext>();
		base.contextOwner.Preload<UISidePanelContext>();
		base.contextOwner.Preload<UISettingsContext>();
		base.contextOwner.Preload<UICarSelectorContext>();
		base.contextOwner.Preload<UIGarageContext>();
		base.contextOwner.Preload<UIBodyCustomizationContext>();
		base.contextOwner.Preload<UICarSlotsContext>();
		base.contextOwner.Preload<UISelectorTrackContext>();
		base.contextOwner.Preload<UIUGCBrowserContext>();
		base.contextOwner.Preload<UIMainMenuContext>();
		base.contextOwner.Preload<UIPromotionContext>();
		base.contextOwner.Preload<UIGeneralSettingsContext>();
		base.contextOwner.Preload<UIInputSettingsContext>();
		base.contextOwner.Preload<UIInputBindsSettingsContext>();
		base.contextOwner.Preload<UIPollingInputDialogContext>();
		base.contextOwner.Preload<UICreditsListContext>();
		base.contextOwner.Preload<UIDynostandContext>();
		base.contextOwner.Preload<UISharingSetupsFilterContext>();
		m_car3dViewContext = base.contextOwner.Add<GarageCar3dViewContext>();
		base.contextOwner.Add<UIBackgroundContext>();
		base.contextOwner.Add<UITopPanelContext>();
		base.contextOwner.Add<UIBottomPanelContext>();
		base.contextOwner.Add<IconGeneratorContext>();
		base.contextOwner.Add<UIPromotionContext>();
		if (m_startContext == null)
		{
			m_startContext = typeof(UIMainMenuContext);
		}
		base.contextOwner.Push(m_startContext);
		m_startContext = null;
		GameEvents.GarageEnter();
		SoundController.PlayMusic(MusicSignal.Garage);
		SoundController.instance.fxAudioPause = false;
		DependencyInjector.Resolve<GamePrefs>().playerProfile.UnlockNewLevelFeatures();
		m_carVendorsAsset = CarVendorsAsset.LoadAndInitDB();
		Application.backgroundLoadingPriority = ThreadPriority.High;
	}

	public bool IsRaceCarLoaded()
	{
		if ((bool)m_car3dViewContext && m_car3dViewContext.GetRaceCar() != null)
		{
			return m_car3dViewContext.GetRaceCar().IsVisualLoaded;
		}
		return false;
	}

	public void GotoContext(Type typeContext)
	{
		m_startContext = typeContext;
		States.Change<GarageGUIState>();
	}

	public void GotoContext<T>() where T : BaseContext
	{
		GotoContext(typeof(GarageGUIState));
	}

	public void GotoGarage()
	{
		GotoContext<UIGarageContext>();
	}

	public void GotoMainMenu()
	{
		GotoContext<UIMainMenuContext>();
	}

	public void GotoSetups()
	{
		GotoContext<UISetupsContext>();
	}

	public static void ExitToGarage()
	{
		BaseState mainCurrent = States.mainCurrent;
		SoundController.instance.fxAudioPause = true;
		BaseRaceModeState baseRaceModeState = mainCurrent as BaseRaceModeState;
		if ((bool)baseRaceModeState)
		{
			baseRaceModeState.ForceFinishRace();
		}
		if (NetworkController.IsActive)
		{
			NetworkController.LobbyExit();
		}
		Overlay.NotifyMPFeature(notify: false);
		States.Change<GarageGUIState>();
	}
}
