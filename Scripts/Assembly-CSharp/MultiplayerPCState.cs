using GameOverlay;
using JukeBox;
using SyncMultiplayer;

public class MultiplayerPCState : BaseGameState
{
	public override void OnEnter()
	{
		base.OnEnter();
		SoundController.instance.fxAudioPause = true;
		NetworkController.LobbyEnter();
		if (Overlay.instance.IsUserGeneratedContentRestricted)
		{
			Overlay.instance.ShowUGCRestrictionDialog();
		}
		if (NetworkController.NeedAutoJoin())
		{
			NetworkController.StartAutoJoin();
			return;
		}
		base.contextOwner.Preload<UINetworkRoomsContext>();
		base.contextOwner.Preload<UICreateRoomContext>();
		base.contextOwner.Preload<UIInputDialogContext>();
		base.contextOwner.Preload<UIReportContext>();
		base.contextOwner.Add<UIBackgroundContext>();
		UINetworkRoomsContext.ResetSavedHeaderSortSelection();
		base.contextOwner.Push<UINetworkRoomsContext>();
		SoundController.PlayMusic(MusicSignal.Garage);
	}

	public static void ReturnToLobby()
	{
		SoundController.instance.fxAudioPause = true;
		BaseRaceModeState baseRaceModeState = States.mainCurrent as BaseRaceModeState;
		if ((bool)baseRaceModeState)
		{
			baseRaceModeState.ForceFinishRace();
		}
		if (NetworkController.IsActive)
		{
			NetworkController.LobbyExit();
		}
		Overlay.NotifyMPFeature(notify: false);
		States.Change<MultiplayerPCState>();
	}

	private void OnIsBusyEvent(bool isBusy)
	{
		if (isBusy)
		{
			UIWaitDialogContext.ShowPleaseWait(base.contextOwner);
		}
		else
		{
			UIWaitDialogContext.Close(base.contextOwner);
		}
	}
}
