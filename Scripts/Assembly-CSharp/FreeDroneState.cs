using HUD;
using UnityEngine;

[AddComponentMenu("Scripts/States/FreeDroneState")]
public class FreeDroneState : BaseMenuState
{
	public static void Show()
	{
		if (KeepAliveManager.isUIHidden)
		{
			KeepAliveManager.SetUIVisible(isVisible: true);
			return;
		}
		BaseMenuState.m_pauseOnShow = States.mainCurrent as SyncNetFreerideRaceModeState == null;
		BaseMenuState.m_pauseSfxOnShow = BaseMenuState.m_pauseOnShow;
		States.Push<FreeDroneState>();
	}

	private void ShowMenu(RaceCar followCar = null)
	{
		TimeController.AddCallback(0f, delegate
		{
			base.contextOwner.Push<UIFreeDroneCameraContext>().SetCameraToCar(followCar, forceEnableOptions: true);
		});
	}

	public override void OnEnter()
	{
		base.OnEnter();
		base.contextOwner.Preload<UIFreeDroneCameraContext>();
		base.contextOwner.Preload<UIRaceStatsContext>();
		ShowMenu(PlayerCarControl.instance.car);
	}

	public override void OnPaused()
	{
		base.OnPaused();
		base.contextOwner.ClearAll();
	}

	public override void OnResume()
	{
		base.OnResume();
		ShowMenu();
	}
}
