using System.Threading.Tasks;
using Championship;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using EpicUnity;
using SyncMultiplayer;

namespace GameOverlay
{
	public class EOSInviteService
	{
		private const int MaxPlayerInLobby = 16;

		private const int DelayRoomCreate = 1000;

		private const int DelayAwaitConnect = 10;

		private const float DelaySecondGodVoice = 3f;

		private Utf8String m_currLobbyId;

		public void SubscribeOnAllEvent()
		{
			NetworkGame.onEnterRoomEvent += OnEnterLobbyAsync;
			EOSManager.instance.SubscribeUILobbySendInvite(OnLobbySendInvite);
			EOSManager.instance.SubscribeUIInviteAcceptedLobby(OnInviteLobbyUI);
			EOSManager.instance.SubscribeUILeaveLobby(OnLeaveLobbyUI);
			NetworkController.OnDisconnectDetected -= OnLeaveLobby;
			NetworkController.OnDisconnectDetected += OnLeaveLobby;
			NetworkController.onAutoJoinRoomError -= OnLeaveLobby;
			NetworkController.onAutoJoinRoomError += OnLeaveLobby;
			NetworkGame.onLeaveRoomEvent -= OnLeaveLobby;
			NetworkGame.onLeaveRoomEvent += OnLeaveLobby;
		}

		public void SendInvite(EpicAccountId epicAccountId)
		{
			if (NetworkController.InstanceGame.CurrentRoom == null)
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.god_voice_failed_invite.Localized(), 3f);
				return;
			}
			EOSManager.instance.SendInviteInLobbyToPlayer(epicAccountId, m_currLobbyId);
			GUICommonGodVoice.ShowText(LocalizationKeys.god_voice_invite_sent.Localized(), 3f);
		}

		private void OnLobbySendInvite(LobbyInviteReceivedCallbackInfo info)
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.god_voice_invite_sent.Localized(), 3f);
		}

		private void OnInviteLobbyUI(Utf8String lobbyId)
		{
			if (!string.IsNullOrEmpty(lobbyId))
			{
				NetworkController.SaveInvitation(lobbyId);
			}
		}

		private void OnLeaveLobbyUI(Utf8String _)
		{
			if (!string.IsNullOrEmpty(m_currLobbyId) && States.mainCurrent is SyncNetFreerideRaceModeState)
			{
				MultiplayerPCState.ReturnToLobby();
			}
		}

		private async void OnEnterLobbyAsync()
		{
			NetworkGame game = NetworkController.InstanceGame;
			if (game == null)
			{
				return;
			}
			await Task.Delay(1000);
			string text = game.CurrentRoom.Name + "|" + WebConfigManager.GetCurrentServer();
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			CreateLobbyOptions lobbyOptions = new CreateLobbyOptions
			{
				LobbyId = text,
				BucketId = WebConfigManager.GetCurrentServer(),
				AllowInvites = true,
				PermissionLevel = (game.isLastRoomPassword ? LobbyPermissionLevel.Inviteonly : LobbyPermissionLevel.Publicadvertised),
				PresenceEnabled = true,
				RejoinAfterKickRequiresInvite = false,
				EnableJoinById = true,
				MaxLobbyMembers = 16u
			};
			if (m_currLobbyId != null)
			{
				EOSManager.instance.LeaveLobby(m_currLobbyId, delegate
				{
					AwaitAndConnectAsync(lobbyOptions);
				});
			}
			else
			{
				AwaitAndConnectAsync(lobbyOptions);
			}
		}

		private async void AwaitAndConnectAsync(CreateLobbyOptions createLobbyOptions)
		{
			while ((bool)(States.mainCurrent as LoadRaceState))
			{
				await Task.Delay(10);
			}
			if ((bool)(States.mainCurrent as SyncNetFreerideRaceModeState) && !(States.mainCurrent as Top32NetRaceModeState))
			{
				m_currLobbyId = createLobbyOptions.LobbyId;
				EOSManager.instance.TryConnectOrCreateLobby(createLobbyOptions);
			}
		}

		private void OnLeaveLobby()
		{
			if (!string.IsNullOrEmpty(m_currLobbyId))
			{
				EOSManager.instance.LeaveLobby(m_currLobbyId);
				EOSManager.instance.RejectInvite(m_currLobbyId);
				m_currLobbyId = null;
			}
		}
	}
}
