using System;
using Binding;
using DB.Meta;
using GameInput;
using GameOverlay;
using SyncMultiplayer;
using UnityEngine;
using UnityEngine.U2D;

namespace HUD
{
	public class UIRaceNetworkFreerideModeContext : UIRaceNetworkModeContext
	{
		private enum HUDAction
		{
			None = -1,
			Create = 0,
			Join = 1,
			Vote = 2
		}

		[SerializeField]
		private SpriteAtlas m_subroomModeIcons;

		private IntProperty m_allNotifyCount = new IntProperty();

		private BoolProperty m_multiplayerButtonsVisible = new BoolProperty(startValue: true);

		private ActionElementBindingProperty m_raceStatsAction = new ActionElementBindingProperty();

		private NetGameServerVoteSystem m_votesController;

		private UIRaceStatsContext m_stats;

		private UIReportContext m_report;

		private UIRadialMenuContext m_radialMenuContext;

		private RadialMenuFactory m_radialMenuFactory;

		private Color m_currentNotificationColor;

		private int m_countVote;

		private int m_countNotify;

		public bool canUseHotkey => m_subroomModeInt.value < 0;

		public SpriteAtlas subroomModeIcons => m_subroomModeIcons;

		protected override void Awake()
		{
			base.Awake();
			m_votesController = NetworkController.InstanceGame.systems.Get<NetGameServerVoteSystem>();
		}

		protected override void Start()
		{
			base.Start();
			m_votesController.onVoteStart += OnVoteStart;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			m_votesController.onVoteStart -= OnVoteStart;
		}

		protected override void OnActivate()
		{
			base.OnActivate();
			if (m_stats == null)
			{
				m_stats = base.owner.Get<UIRaceStatsContext>();
			}
			if (m_stats != null)
			{
				UIRaceStatsContext stats = m_stats;
				stats.onClickedItem = (Action<UIRaceStatsContext.PlayerStatsItem>)Delegate.Remove(stats.onClickedItem, new Action<UIRaceStatsContext.PlayerStatsItem>(OnClickedStats));
				UIRaceStatsContext stats2 = m_stats;
				stats2.onClickedItem = (Action<UIRaceStatsContext.PlayerStatsItem>)Delegate.Combine(stats2.onClickedItem, new Action<UIRaceStatsContext.PlayerStatsItem>(OnClickedStats));
				UIRaceStatsContext stats3 = m_stats;
				stats3.showUIRaceStatsAction = (Action<bool>)Delegate.Remove(stats3.showUIRaceStatsAction, new Action<bool>(OnShowUIRaceStatsHandler));
				UIRaceStatsContext stats4 = m_stats;
				stats4.showUIRaceStatsAction = (Action<bool>)Delegate.Combine(stats4.showUIRaceStatsAction, new Action<bool>(OnShowUIRaceStatsHandler));
				m_raceStatsAction.BindAction(ShowStatsPanel);
			}
			if (m_report == null)
			{
				m_report = base.owner.Get<UIReportContext>();
			}
			if (m_report != null)
			{
				UIReportContext report = m_report;
				report.showUIReportPanelAction = (Action<bool>)Delegate.Combine(report.showUIReportPanelAction, new Action<bool>(OnShowUIReportPanelHandler));
			}
			UIVoteContext.voteContextActivatedAction = (Action<bool>)Delegate.Combine(UIVoteContext.voteContextActivatedAction, new Action<bool>(OnVoteContextActivatedHandler));
			InitializeRadialMenu();
			GUICommonNickNames.SetVisibleAllNickNames(m_gamePrefs.globals.showNickNames);
		}

		private void InitializeRadialMenu()
		{
			UIRadialMenuContext radialMenuTemplate = base.owner.Get<UIRadialMenuContext>();
			if (m_radialMenuFactory == null)
			{
				m_radialMenuFactory = new RadialMenuFactory(radialMenuTemplate);
			}
			if ((object)m_radialMenuContext == null)
			{
				m_radialMenuContext = m_radialMenuFactory.CreateRadialMenuContextWithActions(new RadialActionData(base.MoveToWaypoint, ActionType.TeleportToWaypoint, LocalizationKeys.input_action_teleporttowaypoint), new RadialActionData(ShowVotePanel, ActionType.VoteMap, LocalizationKeys.input_action_votemap), new RadialActionData(ShowRaceJoinPanel, ActionType.RaceJoin, LocalizationKeys.input_action_racejoin), new RadialActionData(ShowRaceCreatePanel, ActionType.RaceCreate, LocalizationKeys.input_action_racecreate), new RadialActionData(base.GotoDynostand, ActionType.GoToDynostand, LocalizationKeys.input_action_gotodynostand), new RadialActionData(base.CreateWaypoint, ActionType.CreateWaypoint, LocalizationKeys.input_action_createwaypoint));
			}
		}

		protected override void Update()
		{
			base.Update();
			m_countVote = ((m_votesController.currentVote != null) ? 1 : 0);
			UpdateRadialMenuNotifications();
		}

		private void UpdateRadialMenuNotifications()
		{
			foreach (UIRadialAction radialActionView in m_radialMenuContext.radialActionViews)
			{
				switch (radialActionView.actionType)
				{
				case ActionType.VoteMap:
					radialActionView.SetNotificationCount(m_countVote);
					break;
				case ActionType.RaceJoin:
					radialActionView.SetNotificationCount(m_countNotify);
					radialActionView.SetNotificationColor(m_currentNotificationColor);
					break;
				default:
					radialActionView.SetNotificationCount(0);
					break;
				}
			}
			m_allNotifyCount.value = m_countNotify + m_countVote;
		}

		protected override void FixedUpdate()
		{
			base.FixedUpdate();
			if (!IsRaceContextActive() && base.roomsController.myRoom == null)
			{
				HandleRadialMenuVisibility();
				HandleShortCuts();
				m_enableRadialMenuButton.value = true;
			}
			else
			{
				m_enableRadialMenuButton.value = false;
			}
		}

		private void HandleShortCuts()
		{
			if (CanProcessRadialMenu() && !base.owner.IsActive<UIRadialMenuContext>())
			{
				if (m_inputManager.IsActionPressed(ActionType.VoteMap))
				{
					ShowVotePanel();
				}
				if (m_inputManager.IsActionPressed(ActionType.RaceCreate))
				{
					ShowRaceCreatePanel();
				}
				if (m_inputManager.IsActionPressed(ActionType.RaceJoin))
				{
					ShowRaceJoinPanel();
				}
				if (m_inputManager.IsActionPressed(ActionType.GoToDynostand))
				{
					GotoDynostand();
				}
				if (m_inputManager.IsActionPressed(ActionType.CreateWaypoint))
				{
					CreateWaypoint();
				}
				if (m_inputManager.IsActionPressed(ActionType.TeleportToWaypoint))
				{
					MoveToWaypoint();
				}
			}
		}

		protected override void OnDeactivate()
		{
			base.OnDeactivate();
			if (m_stats != null)
			{
				UIRaceStatsContext stats = m_stats;
				stats.onClickedItem = (Action<UIRaceStatsContext.PlayerStatsItem>)Delegate.Remove(stats.onClickedItem, new Action<UIRaceStatsContext.PlayerStatsItem>(OnClickedStats));
				UIRaceStatsContext stats2 = m_stats;
				stats2.showUIRaceStatsAction = (Action<bool>)Delegate.Remove(stats2.showUIRaceStatsAction, new Action<bool>(OnShowUIRaceStatsHandler));
				m_raceStatsAction.ReleaseAction();
			}
			if (m_report != null)
			{
				UIReportContext report = m_report;
				report.showUIReportPanelAction = (Action<bool>)Delegate.Remove(report.showUIReportPanelAction, new Action<bool>(OnShowUIReportPanelHandler));
			}
			UIVoteContext.voteContextActivatedAction = (Action<bool>)Delegate.Remove(UIVoteContext.voteContextActivatedAction, new Action<bool>(OnVoteContextActivatedHandler));
		}

		protected override bool OnEscape()
		{
			if (base.roomsController.myRoom != null && (int)base.roomsController.myRoom.state > 1 && !KeepAliveManager.isUIHidden)
			{
				XRModeManager.TryApplyCockpitCamera();
				UIMessageBox.ShowQuestion(LocalizationKeys.subroom_quit_question.Localized(), null, LocalizationKeys.Cancel.Localized(), base.roomsController.QuitMyRoom, LocalizationKeys.Yes.Localized());
				return true;
			}
			return false;
		}

		public void RefreshNotifyCount()
		{
			SubroomInfo subroomInfo = null;
			int num = 0;
			bool flag = true;
			foreach (SubroomInfo roomInfo in base.roomsController.roomInfos)
			{
				bool flag2 = false;
				if (flag)
				{
					flag2 = roomInfo.ContainsPlayer(m_networkLocalPlayer);
				}
				flag = !flag2;
				if (roomInfo.state == SubroomState.WaitingPlayers && !flag2 && roomInfo.runtimePlayers.Count > 0)
				{
					num++;
					subroomInfo = roomInfo;
				}
			}
			if (subroomInfo != null)
			{
				m_currentNotificationColor = UIRaceNetworkModeContext.subroomColor[subroomInfo.subroomMode];
			}
			m_countNotify = num;
		}

		public bool IsRaceContextActive()
		{
			if (!UIRaceNetworkModeContext.IsContextNonEmptyAndEnabled(base.owner.Get<UIRaceStatsContext>()) && !UIRaceNetworkModeContext.IsContextNonEmptyAndEnabled(base.owner.Get<UIPlayersWaitingContext>()) && !UIRaceNetworkModeContext.IsContextNonEmptyAndEnabled(base.owner.Get<UIWatchPlayerContext>()) && !UIRaceNetworkModeContext.IsContextNonEmptyAndEnabled(base.owner.Get<UIVoteContext>()) && !UIRaceNetworkModeContext.IsContextNonEmptyAndEnabled(base.owner.Get<UICreateCompetitionContext>()))
			{
				return UIRaceNetworkModeContext.IsContextNonEmptyAndEnabled(base.owner.Get<UIJoinCompetitionContext>());
			}
			return true;
		}

		private void SetLocalPlayerCarSpawn(SubroomInfo info)
		{
			if (info.ContainsPlayer(NetworkController.InstanceGame.LocalPlayer))
			{
				PlayerCarControl.instance.SetCanSpawnCarInMP(canSpawn: true);
			}
		}

		protected override void OnRoomOperation(SubroomInfo info, SubroomOperation operation)
		{
			base.OnRoomOperation(info, operation);
			RefreshNotifyCount();
		}

		protected override void OnSubroomSpawn(SubroomInfo info)
		{
			base.OnSubroomSpawn(info);
			NetworkPlayer networkPlayer = NetworkController.InstanceGame.FindPlayer(info.creatorId);
			string text = ((networkPlayer != null) ? networkPlayer.FilteredNickNameWithPlatformIcon : string.Empty);
			string text2 = LocalizationKeys.network_room_mode_.AddLocalized(info.subroomMode.ToString().ToLower());
			string text3 = ((info.subroomMode == SubroomMode.TandemXDS && !info.settings.xdsFlags.HasFlag(SubroomFlags.XDSCollide)) ? " <sprite name=\"SafeMode\"/>" : string.Empty);
			text2 += text3;
			string text4 = LocalizationKeys.network_notif_create_room.LocalizedFormat(text, text2);
			UINotifications.instance.Add(text4, UIRaceNetworkModeContext.subroomColor[info.subroomMode], () => info.state == SubroomState.WaitingPlayers && base.roomsController.ContainsRoom(info));
			if (info.creatorId == Overlay.instance.player.id)
			{
				UIPlayersWaitingContext.Show(base.owner, info);
			}
		}

		protected override void OnSubroomJoined(SubroomInfo info)
		{
			base.OnSubroomJoined(info);
			if (info != null)
			{
				UIPlayersWaitingContext.Show(base.owner, info);
			}
		}

		protected override void OnSubroomStartRace(SubroomInfo info)
		{
			SubroomMode subroomMode = info.subroomMode;
			if (subroomMode == SubroomMode.TandemXDS || subroomMode == SubroomMode.Tandem || subroomMode == SubroomMode.XDS)
			{
				PlayerCarControl.instance.SetCanSpawnCarInMP(canSpawn: false);
			}
			base.OnSubroomStartRace(info);
			HideWidgets();
		}

		protected override void OnSubroomRaceResult(SubroomInfo info, bool isTop32 = false)
		{
			SetLocalPlayerCarSpawn(info);
			base.OnSubroomRaceResult(info, isTop32);
		}

		protected override void OnSubroomLocalFinished(SubroomInfo info)
		{
			base.OnSubroomLocalFinished(info);
			if (info.disqualifyReason != DisqualifyReason.Unknown)
			{
				string text = LocalizationKeys.player_disqualified.Localized() + ": " + LocalizationKeys.xds_rsn_.AddLocalized(info.disqualifyReason.ToString().ToLower());
				UINotifications.instance.Add(text, Color.red);
				return;
			}
			string asString = base.roomsController.myLastResult.asString;
			if (LocalizationService.languageMeta.languageType == SystemLanguage.Russian)
			{
				UINotifications.instance.Add(LocalizationKeys.network_race_msg_finish.LocalizedFormat("\n" + asString), Color.cyan);
			}
			else
			{
				UINotifications.instance.Add(LocalizationKeys.network_race_msg_finish.LocalizedFormat(asString), Color.cyan);
			}
		}

		protected override void OnSubroomDestroy(SubroomInfo info)
		{
			SetLocalPlayerCarSpawn(info);
			base.OnSubroomDestroy(info);
			if (info.creatorId != Overlay.instance.player.id)
			{
				base.owner.Get<UIJoinCompetitionContext>().RemoveRoomById(info.id);
			}
		}

		private void OnShowUIRaceStatsHandler(bool showStats)
		{
			SetBlockPlayerController(showStats);
		}

		private void OnShowUIReportPanelHandler(bool showReport)
		{
			SetBlockPlayerController(showReport);
		}

		private void OnVoteContextActivatedHandler(bool activated)
		{
			SetBlockPlayerController(activated);
		}

		private void OnVoteStart(VoteType voteType, string creatorUID)
		{
			NetworkGame instanceGame = NetworkController.InstanceGame;
			if (instanceGame != null)
			{
				NetworkPlayer networkPlayerByUID = GetNetworkPlayerByUID(instanceGame, creatorUID);
				string text = ((networkPlayerByUID != null) ? networkPlayerByUID.FilteredNickNameWithPlatformIcon : string.Empty);
				string text2 = m_votesController.currentVote.GetTarget();
				switch (voteType)
				{
				case VoteType.Map:
					text2 = LocalizationService.Localize(text2);
					break;
				case VoteType.Night:
					text2 = LocalizationService.Localize(text2).ToLower();
					break;
				case VoteType.Light:
					text2 = LocalizationService.Localize(text2);
					break;
				case VoteType.Kick:
				case VoteType.Ban:
				{
					NetworkPlayer networkPlayerByUID2 = GetNetworkPlayerByUID(instanceGame, text2);
					text2 = ((networkPlayerByUID2 != null) ? networkPlayerByUID2.FilteredNickNameWithPlatformIcon : string.Empty);
					break;
				}
				}
				string text3 = LocalizationService.LocalizeFormat(LocalizationKeys.network_notif_create_vote_.KeyValue + voteType.ToString().ToLower(), text, text2);
				UINotifications.instance.Add(text3, Color.blue, () => m_votesController != null && m_votesController.currentVote != null);
			}
		}

		private void OnClickedStats(UIRaceStatsContext.PlayerStatsItem item)
		{
			if (m_stats.isFriendsMode)
			{
				if (m_stats.friendsList.GetSelected() is UIRaceStatsContext.FriendsItem friendsItem)
				{
					Overlay.instance.InviteFriend(friendsItem.player.id);
					NetworkController.InstanceGame?.AddToInvitedFriends(friendsItem.player.id);
				}
			}
			else if ((1u & ((!XRModeManager.IsXRActive()) ? 1u : 0u)) != 0)
			{
				m_stats.StartWatchPlayer(item);
			}
		}

		public void ShowUIHUDMultiplayerButtonsPanel(bool value)
		{
			m_multiplayerButtonsVisible.value = value;
		}

		public void ShowVotePanel()
		{
			if (!NetworkController.IsVoteStartSystemAllowed())
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.vote_only_admin.Localized());
			}
			else if (canUseHotkey)
			{
				if (AllowedPerformHUDActionOfType(HUDAction.Vote))
				{
					base.owner.HideAllAndPush<UIVoteContext>();
				}
				else
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.unable_perform_mp_hud_action.Localized(), 3f);
				}
			}
		}

		public void ShowRaceCreatePanel()
		{
			if (!canUseHotkey)
			{
				return;
			}
			if (AllowedPerformHUDActionOfType(HUDAction.Create))
			{
				DB.Meta.Track byId = m_model.GetById<DB.Meta.Track>(m_gamePrefs.globals.lastTrackId);
				if (byId != null && byId.dlcCode != DLCCode.Unknown && !Overlay.instance.IsDLCInstalled(byId.dlcCode))
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.no_dlc_track_goodvoice.LocalizedFormat(LocalizationService.Localize(byId.name)));
				}
				else
				{
					base.owner.HideAllAndPush<UICreateCompetitionContext>();
				}
			}
			else
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.unable_perform_mp_hud_action.Localized(), 3f);
			}
		}

		public void ShowRaceJoinPanel()
		{
			if (canUseHotkey)
			{
				if (AllowedPerformHUDActionOfType(HUDAction.Join))
				{
					base.owner.HideAllAndPush<UIJoinCompetitionContext>();
				}
				else
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.unable_perform_mp_hud_action.Localized(), 3f);
				}
			}
		}

		public void ShowStatsPanel()
		{
			if (!UIRaceNetworkModeContext.IsContextNonEmptyAndEnabled(base.owner.Get<UIPlayersWaitingContext>()))
			{
				if (UIRaceNetworkModeContext.IsContextNonEmptyAndEnabled(base.owner.Get<UIWatchPlayerContext>()))
				{
					base.owner.Push<UIRaceStatsContext>();
				}
				else if (UIRaceNetworkModeContext.IsContextNonEmptyAndEnabled(base.owner.Get<UIRaceStatsContext>()))
				{
					base.owner.Hide<UIRaceStatsContext>();
				}
				else
				{
					base.owner.HideAllAndPush<UIRaceStatsContext>();
				}
			}
		}

		private bool AllowedPerformHUDActionOfType(HUDAction actionType)
		{
			NetworkGame instanceGame = NetworkController.InstanceGame;
			bool flag = instanceGame != null && instanceGame.CurrentRoomOptions != null;
			switch (actionType)
			{
			case HUDAction.Create:
			case HUDAction.Join:
				if (flag && (int)(instanceGame.CurrentRoomOptions.NewRoomSettings & (RoomData.RoomSettingFlags.DRIFT_ALLOWED | RoomData.RoomSettingFlags.TIME_ALLOWED | RoomData.RoomSettingFlags.CLIPPING_DUEL_ALLOWED)) > 0)
				{
					return (instanceGame.CurrentRoomOptions.NewRoomSettings & RoomData.RoomSettingFlags.EXTERNAL_TRACK) == 0;
				}
				return false;
			case HUDAction.Vote:
				if (flag)
				{
					return (int)(instanceGame.CurrentRoomOptions.NewRoomSettings & RoomData.RoomSettingFlags.VOTE_ALLOWED) > 0;
				}
				return false;
			default:
				return false;
			}
		}

		public void SpawnRoom(SubroomSettings settings)
		{
			base.roomsController.SpawnRoom(settings);
		}

		public void HideWidgets()
		{
			base.owner.Hide<UICreateCompetitionContext>();
			base.owner.Hide<UIPlayersWaitingContext>();
			base.owner.Hide<UIJoinCompetitionContext>();
			base.owner.Hide<UIRaceStatsContext>();
		}
	}
}
