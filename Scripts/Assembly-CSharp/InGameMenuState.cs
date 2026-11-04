using SyncMultiplayer;
using UnityEngine;

[AddComponentMenu("Scripts/States/InGameMenu")]
public class InGameMenuState : BaseMenuState
{
	public static void Show()
	{
		if (KeepAliveManager.isUIHidden)
		{
			KeepAliveManager.SetUIVisible(isVisible: true);
			return;
		}
		BaseMenuState.m_pauseOnShow = States.current as SyncNetFreerideRaceModeState == null;
		BaseMenuState.m_pauseSfxOnShow = true;
		BaseMenuState.m_resumeSfxOnExit = false;
		States.Push<InGameMenuState>();
	}

	private void ShowMenu()
	{
		TimeController.AddCallback(0f, delegate
		{
			base.contextOwner.Push<UIInGameMenuContext>();
		});
	}

	public override void OnEnter()
	{
		base.OnEnter();
		if (TryGetStateSystem(out var system))
		{
			system.AddStateForLocalPlayer(PlayerCarState.Pause);
		}
		ShowMenu();
	}

	public override void OnExit()
	{
		base.OnExit();
		if (TryGetStateSystem(out var system))
		{
			system.RemoveStateForLocalPlayer(PlayerCarState.Pause);
		}
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

	private bool TryGetStateSystem(out NetGamePlayerStateSystem system)
	{
		system = null;
		if (!(States.mainCurrent is SyncNetFreerideRaceModeState))
		{
			return false;
		}
		NetworkGame instanceGame = NetworkController.InstanceGame;
		if (instanceGame == null)
		{
			return false;
		}
		system = instanceGame.systems.Get<NetGamePlayerStateSystem>();
		return true;
	}
}
