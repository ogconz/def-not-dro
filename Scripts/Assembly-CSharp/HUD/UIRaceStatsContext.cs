using System;
using System.Collections.Generic;
using Binding;
using Binding.Base;
using Binding.Components;
using CarX.Platform;
using DB;
using DB.Meta;
using DI;
using GameInput;
using GameOverlay;
using SyncMultiplayer;
using UnityEngine;

namespace HUD
{
	[GameHotkeyBlocker(true)]
	[RaycastBlocker(true)]
	public class UIRaceStatsContext : BaseBindingBehaviourTarget
	{
		public class FriendsItem : BaseListElementData
		{
			public StringProperty nickName = new StringProperty();

			public SpriteProperty avatar = new SpriteProperty();

			public IntProperty number = new IntProperty();

			public BoolProperty isOnline = new BoolProperty();

			public IPlayer player { get; private set; }

			public FriendsItem(IPlayer player, int number)
			{
				FriendsItem friendsItem = this;
				this.player = player;
				this.number.value = number;
				Texture2D texture2D = player.avatar;
				if (texture2D != null)
				{
					avatar.value = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), Vector2.one * 0.5f);
				}
				nickName.value = UIUtils.ConvertNGUIColorToRich(player.nickName);
				isOnline.value = player.state == PlayerState.Online;
				IGameOverlay instance = Overlay.instance;
				instance.friendPresenceChangedAction = (Action<PlayerId, PlayerState>)Delegate.Combine(instance.friendPresenceChangedAction, (Action<PlayerId, PlayerState>)delegate(PlayerId playerId, PlayerState state)
				{
					if (player.id.Equals(playerId))
					{
						friendsItem.isOnline.value = state == PlayerState.Online;
					}
				});
			}
		}

		public class PlayerStatsItem : BaseListElementData, IDisposable
		{
			public StringProperty nickName = new StringProperty();

			public SpriteProperty avatar = new SpriteProperty();

			public BoolProperty watching = new BoolProperty(false);

			public BoolProperty showPlatformIcon = new BoolProperty();

			public BoolProperty badConnection = new BoolProperty(false);

			public BoolProperty showReportIcon = new BoolProperty();

			public BoolProperty isOwner = new BoolProperty();

			public BoolProperty isModdable = new BoolProperty();

			public BoolProperty pingEmpty = new BoolProperty();

			public StringProperty points = new StringProperty();

			public IntProperty number = new IntProperty();

			public StringProperty ping = new StringProperty();

			public IntProperty platform = new IntProperty();

			public IntProperty showMuteIcon = new IntProperty();

			public IntProperty playerInputType = new IntProperty();

			private bool m_badConnectionBool;

			private RaceStatsPlayersContainer m_raceStatsPlayers;

			public NetworkPlayer player { get; private set; }

			public PlayerStatsItem(RaceStatsPlayersContainer raceStatsPlayers, NetworkPlayer player, int number)
			{
				if (player == null)
				{
					return;
				}
				this.player = player;
				player.onFilteredNicknameUpdated = (System.Action)Delegate.Combine(player.onFilteredNicknameUpdated, new System.Action(OnNicknameUpdatedHandler));
				m_raceStatsPlayers = raceStatsPlayers;
				nickName.value = player.FilteredNickName;
				this.number.value = number;
				PlayerId playerId = player.PlayerId;
				UserPlatform.Id id = playerId.platform;
				showPlatformIcon.value = NetworkController.InstanceGame.IsCrossPlatformGloballyEnabled() && UserPlatform.IsNeedShowLocalPlatformIcon(id);
				platform.value = (int)((id == UserPlatform.Id.MyGames || id == UserPlatform.Id.EpicGames) ? UserPlatform.Id.Steam : id);
				SetSprite(avatar, GameManager.instance.defaultAvatar);
				if (m_raceStatsPlayers != null)
				{
					if (playerId == Overlay.playerId)
					{
						SetSprite(avatar, GameManager.GetMainPlayerAvatarNotNull());
					}
					else if (!m_raceStatsPlayers.ContainsAvatar(playerId))
					{
						Overlay.instance.RequestUserAvatarAndName(playerId, OnLoadedAvatar);
					}
					else
					{
						SetSprite(avatar, (Texture2D)m_raceStatsPlayers.GetCachedAvatar(playerId));
					}
				}
				Refresh();
			}

			private void CheckConnectionInStats()
			{
				if (player.BadConnection != m_badConnectionBool)
				{
					m_badConnectionBool = player.BadConnection;
					badConnection.value = m_badConnectionBool;
				}
			}

			public void Refresh()
			{
				if (player != null)
				{
					points.value = UIUtils.DigitsWithSeparate(player.points.points);
					ping.value = ((player.Ping > 0) ? player.Ping.ToString() : ping.value);
					pingEmpty.value = ping.value == string.Empty || ping.value == null;
					playerInputType.value = (int)player.PlayerProperties.InputDeviceType;
					isModdable.value = !player.PlayerProperties.isDefaultBranchUser;
					watching.value = CameraSwitch.instance.GetCar() == player.userCar;
					CheckConnectionInStats();
					NetworkGame instanceGame = NetworkController.InstanceGame;
					isOwner.value = instanceGame.playerRoomCreator?.PlayerId == player.PlayerId;
					showReportIcon.value = UIReportContext.alreadyBannedInThisSessionPlayers.Contains(player.PlayerId.ToString());
				}
			}

			private void OnLoadedAvatar(PlayerId id, string name, Texture2D texture)
			{
				SetSprite(avatar, texture);
				m_raceStatsPlayers?.CacheAvatar(id, texture);
			}

			private void SetSprite(SpriteProperty property, Texture2D texture)
			{
				Texture2D texture2D = ((texture != null) ? texture : GameManager.instance.defaultAvatar);
				property.value = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), Vector2.one * 0.5f);
			}

			public override void Dispose()
			{
				if (player != null)
				{
					base.Dispose();
					player.onFilteredNicknameUpdated = null;
					player = null;
				}
			}

			private void OnNicknameUpdatedHandler()
			{
				if (player != null)
				{
					nickName.value = player.FilteredNickName;
				}
			}
		}

		private const int ROWS = 8;

		private const int ITEM_MAX = 16;

		[SerializeField]
		private UIRaceHintsContext m_hintsPanel;

		private StringProperty m_topTitle = new StringProperty();

		private BoolProperty m_friendsMode = new BoolProperty(false);

		private BoolProperty m_visibleInviteHint = new BoolProperty(startValue: true);

		private BoolProperty m_canNextPage = new BoolProperty(false);

		private BoolProperty m_canPrevPage = new BoolProperty(false);

		private ListProperty m_playersList = new ListProperty();

		private ListProperty m_friendsList = new ListProperty();

		private StringProperty m_pageInfo = new StringProperty();

		private StringProperty m_roomName = new StringProperty();

		private StringProperty m_serverName = new StringProperty();

		private StringProperty m_carName = new StringProperty();

		private StringProperty m_trackName = new StringProperty();

		public Property<bool> m_nightModeAllowed = new BoolProperty();

		public Property<bool> m_driftAllowed = new BoolProperty();

		public Property<bool> m_voteAllowed = new BoolProperty();

		public Property<int> m_collisionType = new IntProperty();

		public Property<int> m_currentAdh = new IntProperty();

		public Property<int> m_powerDensityLimit = new IntProperty();

		public Property<int> m_wheelDriveLimit = new IntProperty();

		public Property<bool> m_isAssistAllowed = new BoolProperty();

		public Property<bool> m_tractionControlAllowed = new BoolProperty();

		private StringProperty m_playerExp = new StringProperty();

		private StringProperty m_playerLevel = new StringProperty();

		private StringProperty m_roomOwner = new StringProperty();

		private FloatProperty m_playerExpBar = new FloatProperty();

		private TextureProperty m_playerAvatar = new TextureProperty();

		private readonly BoolProperty m_secondColumnIconVisibility = new BoolProperty();

		private string m_cooldownRemainingText;

		private int m_prevCooldownValue;

		private ListProperty m_selectedList;

		private int m_pagesCount = 1;

		private int m_currentPages;

		private GamePrefs m_prefs;

		private UIReportContext m_reportContext;

		private RaceStatsPlayersContainer m_raceStatsPlayers;

		private ActionType m_openProfileActionType = ActionType.NavAdditional0BtnX;

		private ActionElementBinding m_openProfileBinding;

		public Action<PlayerStatsItem> onClickedItem;

		public Action<bool> showUIRaceStatsAction;

		private RoomOptionsEvent m_roomOptionUpdAction;

		public ListProperty playersList => m_playersList;

		public ListProperty friendsList => m_friendsList;

		public bool isFriendsMode => m_friendsMode.value;

		public bool visibleInviteHint
		{
			get
			{
				return m_visibleInviteHint.value;
			}
			set
			{
				m_visibleInviteHint.value = value;
			}
		}

		private void Awake()
		{
			if (m_hintsPanel == null)
			{
				m_hintsPanel = GetComponentInChildren<UIRaceHintsContext>();
			}
			NetworkGame.onEnterRoomEvent += OnEnterRoomHandler;
			NetworkGame.onLeaveRoomEvent += OnLeaveRoomHandler;
			SubscribeOnRoomOptionsChanged(subscribe: true);
			UpdateRoomOptions(NetworkController.InstanceGame?.CurrentRoomOptions);
		}

		protected override void OnActivate()
		{
			base.OnActivate();
			BringForwardByDepth();
			if (m_raceStatsPlayers == null)
			{
				m_raceStatsPlayers = new RaceStatsPlayersContainer();
			}
			if (m_reportContext == null)
			{
				m_reportContext = base.owner.Get<UIReportContext>();
			}
			InitFriendList();
			SetVisibleFriends(visibleFriends: false);
			PlayerStatsItem itemWithSelectedCar = m_raceStatsPlayers.GetItemWithSelectedCar();
			m_playersList.Selection(itemWithSelectedCar, selected: true);
			m_raceStatsPlayers.SetSelectedIndex(itemWithSelectedCar);
			InputManager.instance.ignoreDuplicateActions = false;
			if (m_prefs == null)
			{
				m_prefs = DependencyInjector.Resolve<GamePrefs>();
			}
			m_playerAvatar.value = GameManager.GetMainPlayerAvatarNotNull();
			m_playerLevel.value = "lvl " + m_prefs.playerProfile.level;
			m_playerExp.value = UIUtils.DigitsWithSeparate(m_prefs.playerProfile.exp) + " / " + UIUtils.DigitsWithSeparate(m_prefs.playerProfile.nextLevelExp);
			m_playerExpBar.value = (float)(m_prefs.playerProfile.exp - m_prefs.playerProfile.prevLevelExp) / (float)(m_prefs.playerProfile.nextLevelExp - m_prefs.playerProfile.prevLevelExp);
			m_roomName.value = LocalizationKeys.network_game_room_name.LocalizedFormat(NetworkController.GetCurrentRoomName());
			m_serverName.value = LocalizationKeys.server_current.LocalizedFormat(WebConfigManager.GetLocalizedServer(WebConfigManager.GetCurrentServer()));
			m_carName.value = DependencyInjector.Resolve<BaseModel>().GetNameById<PlayerCar>(m_prefs.globals.lastCarId);
			m_playersList.OnSelectionChanged += OnListSelectionChangedHandler;
			showUIRaceStatsAction.SafeCall(param: true);
			UpdateRoomCreatorInfo();
			XRModeManager.TryApplyCockpitCamera();
			ShowHints();
		}

		protected override void OnDeactivate()
		{
			m_playersList.OnSelectionChanged -= OnListSelectionChangedHandler;
			visibleInviteHint = true;
			showUIRaceStatsAction.SafeCall(param: false);
			base.OnDeactivate();
		}

		protected override bool OnEscape()
		{
			return true;
		}

		protected override void OnDestroy()
		{
			NetworkGame.onEnterRoomEvent -= OnEnterRoomHandler;
			NetworkGame.onLeaveRoomEvent -= OnLeaveRoomHandler;
			ClearContainerData();
			SubscribeOnRoomOptionsChanged(subscribe: false);
			base.OnDestroy();
		}

		public void ClearContainerData()
		{
			if (m_raceStatsPlayers != null)
			{
				m_raceStatsPlayers.ClearData();
				m_raceStatsPlayers = null;
			}
		}

		public void NavRaceStats()
		{
			Hide();
		}

		public void NavCancelBtnB()
		{
			if (isFriendsMode)
			{
				SetVisibleFriends(visibleFriends: false);
			}
			else
			{
				Hide();
			}
		}

		private void TeleportToPlayer()
		{
			PlayerStatsItem playerStatsItem = m_raceStatsPlayers.TryGetSelectedItem();
			if (playerStatsItem != null)
			{
				NetworkPlayer player = playerStatsItem.player;
				if (PlayerCarControl.instance.car.TryResetToPlayer(player.userCar))
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.you_are_teleported.Localized(), 3f);
				}
			}
		}

		public void NavigationReportPlayer()
		{
			if (IsCurrSelectedItemLocalPlayer())
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.report_select_player.Localized(), 3f);
				return;
			}
			if (IsCurrSelectedItemReportedPlayer())
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.player_already_reported.Localized(), 3f);
				return;
			}
			List<PlayerStatsItem> list = m_raceStatsPlayers.playersList;
			PlayerStatsItem playerStatsItem = m_raceStatsPlayers.TryGetSelectedItem();
			if (playerStatsItem == null)
			{
				return;
			}
			NetworkPlayer player = playerStatsItem.player;
			List<string> list2 = new List<string>();
			foreach (PlayerStatsItem item in list)
			{
				list2.Add(item.player.PlayerId.ToString());
			}
			NetworkGame instanceGame = NetworkController.InstanceGame;
			string roomId = ((instanceGame == null) ? string.Empty : instanceGame.CurrentRoom.Name);
			string currentRoomName = NetworkController.GetCurrentRoomName();
			Hide();
			m_reportContext.Show(base.owner, player, currentRoomName, roomId, list2);
		}

		public void NavSelectBtnA()
		{
			PlayerStatsItem playerStatsItem = m_raceStatsPlayers.TryGetSelectedItem();
			if (playerStatsItem != null)
			{
				onClickedItem.SafeCall(playerStatsItem);
				if (States.current is FreeDroneState && !IsCurrSelectedItemReportedPlayer())
				{
					base.owner.HideStackAndPush<UIFreeDroneCameraContext>().SetCameraToCar(playerStatsItem.player.userCar, forceEnableOptions: true);
				}
			}
		}

		public void NavAdditional0BtnX()
		{
			if (!isFriendsMode)
			{
				PlayerStatsItem playerStatsItem = m_raceStatsPlayers.TryGetSelectedItem();
				if (playerStatsItem != null)
				{
					Overlay.instance.ShowProfile(playerStatsItem.player.PlayerId);
				}
			}
		}

		public void StartWatchPlayer(PlayerStatsItem item)
		{
			if (item == null)
			{
				return;
			}
			RaceCar userCar = item.player.userCar;
			NetworkPlayer player = item.player;
			if (userCar != null && userCar.gameObject.activeSelf)
			{
				Hide();
				UIWatchPlayerContext uIWatchPlayerContext = base.owner.Get<UIWatchPlayerContext>();
				if (uIWatchPlayerContext != null && uIWatchPlayerContext.isActiveAndEnabled)
				{
					base.owner.Hide<UIWatchPlayerContext>();
				}
				if (!item.player.IsLocal)
				{
					base.owner.Push<UIWatchPlayerContext>().WatchPlayer(player);
					base.owner.Get<UIWatchPlayerContext>().SetPlayersListData(m_raceStatsPlayers);
				}
			}
		}

		private void OnListSelectionChangedHandler(BaseListElementData baseListElement)
		{
			PlayerStatsItem selectedIndex = baseListElement as PlayerStatsItem;
			m_raceStatsPlayers.SetSelectedIndex(selectedIndex);
			ShowHints();
		}

		private PlayerId GetSelectedItemPlayerId()
		{
			return m_raceStatsPlayers.TryGetSelectedItem()?.player.PlayerId ?? PlayerId.Invalid;
		}

		private bool IsCurrSelectedItemLocalPlayer()
		{
			return GetSelectedItemPlayerId() == Overlay.instance.player.id;
		}

		private bool IsCurrSelectedItemReportedPlayer()
		{
			return UIReportContext.alreadyBannedInThisSessionPlayers.Contains(GetSelectedItemPlayerId().ToString());
		}

		private void HideProfileBindingIfNeed(PlayerStatsItem playerStatsItem)
		{
			if (playerStatsItem == null || m_hintsPanel == null)
			{
				return;
			}
			if (playerStatsItem.player.PlayerId.platform != UserPlatform.Id.Xbox)
			{
				if (m_hintsPanel.HaveElement(m_openProfileActionType) && m_openProfileBinding != null)
				{
					m_hintsPanel.RemoveElement(m_openProfileBinding);
				}
			}
			else
			{
				AddOpenProfileBinding();
			}
		}

		public void NavAdditional1BtnY()
		{
			IInviteService inviteService = Overlay.instance.inviteService;
			if (inviteService.isSupportIndividualInvitation)
			{
				SetVisibleFriends(visibleFriends: true);
			}
			else
			{
				inviteService.OpenSystemInviteDialog();
			}
		}

		[GameHotkey(ActionType.NavDown, false, true)]
		public void NavDown()
		{
			m_selectedList.Selection(m_selectedList.GetSelectedIndex() + 1, selected: true);
		}

		[GameHotkey(ActionType.NavUp, false, true)]
		public void NavUp()
		{
			m_selectedList.Selection(m_selectedList.GetSelectedIndex() - 1, selected: true);
		}

		[GameHotkey(ActionType.NavRight, false, false)]
		public void NavRight()
		{
			if (isFriendsMode)
			{
				int selectedIndex = m_selectedList.GetSelectedIndex();
				if (selectedIndex + 8 >= 16 && m_currentPages < m_pagesCount - 1)
				{
					FillFriendsList(m_currentPages + 1);
					selectedIndex = Mathf.Clamp(selectedIndex - 8, 0, m_selectedList.count);
					m_selectedList.Selection(selectedIndex, selected: true);
					return;
				}
			}
			m_selectedList.Selection(m_selectedList.GetSelectedIndex() + 8, selected: true);
		}

		[GameHotkey(ActionType.NavLeft, false, false)]
		public void NavLeft()
		{
			if (isFriendsMode)
			{
				int selectedIndex = m_selectedList.GetSelectedIndex();
				if (selectedIndex - 8 < 0 && m_currentPages > 0)
				{
					FillFriendsList(m_currentPages - 1);
					selectedIndex = Mathf.Clamp(selectedIndex + 8, 0, m_selectedList.count);
					m_selectedList.Selection(selectedIndex, selected: true);
					return;
				}
			}
			m_selectedList.Selection(m_selectedList.GetSelectedIndex() - 8, selected: true);
		}

		[GameHotkey(ActionType.NavNextPage, false, false)]
		public void NextPage()
		{
			if (isFriendsMode && m_canNextPage.value)
			{
				FillFriendsList(m_currentPages + 1);
				m_friendsList.Selection(0, selected: true);
			}
		}

		[GameHotkey(ActionType.NavPrevPage, false, false)]
		public void PrevPage()
		{
			if (isFriendsMode && m_canPrevPage.value)
			{
				FillFriendsList(m_currentPages - 1);
				m_friendsList.Selection(0, selected: true);
			}
		}

		[GameHotkey(ActionType.RaceStats, false, false)]
		public void OnClose()
		{
			Hide();
			SoundController.Play2DSound(SoundSample.DefaultBtn);
		}

		private void ShowHints()
		{
			m_topTitle.value = (isFriendsMode ? LocalizationKeys.friends_title.Localized() : LocalizationKeys.race_stats_title.Localized());
			if (m_hintsPanel == null)
			{
				return;
			}
			m_hintsPanel.Clear();
			if (isFriendsMode)
			{
				m_hintsPanel.AddElement(LocalizationKeys.nav_hint_goto_back.Localized(), ActionType.NavCancelBtnB, NavCancelBtnB);
				m_hintsPanel.AddElement(LocalizationKeys.nav_hint_invite.Localized(), ActionType.NavSelectBtnA, NavSelectBtnA);
				return;
			}
			m_hintsPanel.AddElement(LocalizationKeys.nav_hint_exit.Localized(), ActionType.NavCancelBtnB, NavCancelBtnB);
			if (!XRModeManager.IsXRActive() && !IsCurrSelectedItemReportedPlayer())
			{
				m_hintsPanel.AddElement(LocalizationKeys.nav_hint_watch.Localized(), ActionType.NavSelectBtnA, NavSelectBtnA);
			}
			if (!visibleInviteHint)
			{
				return;
			}
			m_hintsPanel.AddElement(LocalizationKeys.nav_hint_invtite_friend.Localized(), ActionType.NavAdditional1BtnY, NavAdditional1BtnY);
			if (!IsCurrSelectedItemLocalPlayer())
			{
				m_hintsPanel.AddElement(LocalizationKeys.nav_select_report.Localized(), ActionType.NavReport, NavigationReportPlayer);
				if (!IsCurrSelectedItemReportedPlayer())
				{
					m_hintsPanel.AddElement(LocalizationKeys.teleport_to_player.Localized() + " " + m_cooldownRemainingText, ActionType.NavBtnExpandRight, TeleportToPlayer);
				}
			}
		}

		private void AddOpenProfileBinding()
		{
			if (!(m_hintsPanel == null) && !m_hintsPanel.HaveElement(m_openProfileActionType))
			{
				m_openProfileBinding = m_hintsPanel.AddElement(LocalizationKeys.cht_popup_overlayprofile.Localized(), m_openProfileActionType, NavAdditional0BtnX);
			}
		}

		public override bool IsTopOrBottomContext(BaseContext context)
		{
			return context == m_hintsPanel;
		}

		private void SetVisibleFriends(bool visibleFriends)
		{
			m_friendsMode.value = visibleFriends;
			if (isFriendsMode)
			{
				FillFriendsList(m_currentPages);
			}
			else
			{
				FillPlayersList();
			}
			if (m_selectedList != null)
			{
				m_selectedList.OnClick -= OnClickPlayer;
			}
			m_selectedList = (isFriendsMode ? m_friendsList : m_playersList);
			m_selectedList.Selection(0, selected: true);
			if (isFriendsMode)
			{
				int selectedIndex = m_selectedList.GetSelectedIndex();
				m_selectedList.Selection(selectedIndex, selected: true);
			}
			ShowHints();
			m_selectedList.OnClick += OnClickPlayer;
		}

		private void Update()
		{
			if (!isFriendsMode)
			{
				if (m_raceStatsPlayers.UpdateListIfNeed())
				{
					int selectedIndex = m_playersList.GetSelectedIndex();
					FillPlayersList();
					selectedIndex = Mathf.Clamp(selectedIndex, 0, m_playersList.count - 1);
					m_playersList.Selection(selectedIndex, selected: true);
				}
				else
				{
					m_raceStatsPlayers.RefreshListViewData();
				}
				int remainingCooldownTime = PlayerCarControl.instance.car.carPlacementModel.remainingCooldownTime;
				m_cooldownRemainingText = ((remainingCooldownTime > 0) ? $" ({remainingCooldownTime})" : string.Empty);
				if (remainingCooldownTime != m_prevCooldownValue)
				{
					ShowHints();
					m_prevCooldownValue = remainingCooldownTime;
				}
			}
		}

		private void FixedUpdate()
		{
			if (InputManager.instance.IsActionPressed(ActionType.FlyStatistics) && InputManager.instance.lastActionDevice == GameInput.DeviceType.Keyboard)
			{
				Hide();
			}
		}

		private void FillPlayersList()
		{
			m_playersList.Clear();
			m_raceStatsPlayers.FillPlayersList();
			m_playersList.value = m_raceStatsPlayers.playersList;
			m_secondColumnIconVisibility.value = m_raceStatsPlayers.playersList.Count > 8;
		}

		private void InitFriendList()
		{
			FillFriendsList(0);
			m_friendsList.Selection(0, selected: true);
		}

		private void FillFriendsList(int page)
		{
			m_friendsList.Clear();
			List<IPlayer> list = new List<IPlayer>(Overlay.instance.friends);
			FilterFriendList(list);
			list.Sort(PlayerCompareSort);
			int num = page * 16;
			int num2 = 0;
			while (num < list.Count && num2 < 16)
			{
				IPlayer player = list[num];
				player.nickName = UIUtils.FilterInvalidTags(player.nickName);
				m_friendsList.Add(new FriendsItem(player, num2 + 1));
				num++;
				num2++;
			}
			m_currentPages = page;
			m_pagesCount = Mathf.CeilToInt((float)list.Count / 16f);
			m_pageInfo.value = $"{m_currentPages + 1} / {m_pagesCount}";
			m_canNextPage.value = page < m_pagesCount - 1;
			m_canPrevPage.value = page > 0;
		}

		private void FilterFriendList(List<IPlayer> friends)
		{
			List<IPlayer> list = new List<IPlayer>();
			foreach (IPlayer friend in friends)
			{
				if (IsFriendInRoom(friend))
				{
					list.Add(friend);
				}
			}
			foreach (IPlayer item in list)
			{
				friends.Remove(item);
			}
			list.Clear();
		}

		private int PlayerCompareSort(IPlayer left, IPlayer right)
		{
			int num = left.state.CompareTo(right.state);
			if (num != 0)
			{
				return num;
			}
			return string.Compare(left.nickName, right.nickName, StringComparison.OrdinalIgnoreCase);
		}

		private bool IsFriendInRoom(IPlayer friend)
		{
			if (friend != null)
			{
				return NetworkController.InstanceGame?.FindPlayer(friend.id) != null;
			}
			return false;
		}

		private void OnClickPlayer(BaseListElementData data)
		{
			if (!data.selected)
			{
				m_selectedList.Selection(data, selected: true);
			}
			else
			{
				NavSelectBtnA();
			}
		}

		private void OnRoomOptionsChangedHandler(SmartfoxRoomClient client, SmartfoxRoomOptions options, HashSet<string> changedProps)
		{
			UpdateRoomOptions(options, changedProps);
		}

		private void UpdateRoomOptions(SmartfoxRoomOptions roomOptions, HashSet<string> changed = null)
		{
			if (roomOptions == null)
			{
				return;
			}
			if (m_prefs != null && m_prefs.globals != null)
			{
				UpdateRoomCreatorInfo();
			}
			if (changed == null || changed.Contains("2") || changed.Contains("et"))
			{
				BaseModel baseModel = DependencyInjector.Resolve<BaseModel>();
				if (roomOptions.ExternalTrackId != 0L)
				{
					m_trackName.value = roomOptions.TrackName;
				}
				else
				{
					DB.Meta.Track byId = baseModel.GetById<DB.Meta.Track>(roomOptions.TrackID);
					m_trackName.value = LocalizationService.Localize(byId.name);
				}
			}
			if (changed == null || changed.Contains("nm"))
			{
				m_nightModeAllowed.value = roomOptions.RoomNightMode;
			}
			if (changed == null || changed.Contains("s2"))
			{
				m_driftAllowed.value = (int)(roomOptions.NewRoomSettings & RoomData.RoomSettingFlags.DRIFT_ALLOWED) > 0;
				m_voteAllowed.value = (int)(roomOptions.NewRoomSettings & RoomData.RoomSettingFlags.VOTE_ALLOWED) > 0;
				m_collisionType.value = roomOptions.CollisionType;
				m_currentAdh.value = RoomAdhToIndex((RoomData.AdhLimitFlags)roomOptions.RoomAdhLimit);
				m_powerDensityLimit.value = roomOptions.RoomPowerDensityLimit;
				m_wheelDriveLimit.value = GetWDLimitType((RoomData.WheelDriveType)roomOptions.RoomWheelDriveType);
				m_isAssistAllowed.value = roomOptions.AssistVersion < 1;
				m_tractionControlAllowed.value = roomOptions.RoomTractionControlMode < 1;
			}
		}

		private void UpdateRoomCreatorInfo()
		{
			NetworkGame instanceGame = NetworkController.InstanceGame;
			if (instanceGame != null)
			{
				string text = instanceGame.playerRoomCreator?.FilteredNickNameWithPlatformIcon;
				if (string.IsNullOrEmpty(text))
				{
					text = instanceGame.roomCreatorName;
				}
				m_roomOwner.value = LocalizationKeys.network_room_creator.LocalizedFormat(text);
			}
		}

		private static int RoomAdhToIndex(RoomData.AdhLimitFlags limit)
		{
			switch (limit)
			{
			default:
				_ = 15;
				goto case RoomData.AdhLimitFlags.NONE;
			case RoomData.AdhLimitFlags.NONE:
				return 0;
			case RoomData.AdhLimitFlags.ADH_90:
				return 1;
			case RoomData.AdhLimitFlags.ADH_100:
				return 2;
			case RoomData.AdhLimitFlags.ADH_110:
				return 3;
			case RoomData.AdhLimitFlags.ADH_120:
				return 4;
			case RoomData.AdhLimitFlags.ADH_90 | RoomData.AdhLimitFlags.ADH_100:
			case RoomData.AdhLimitFlags.ADH_90 | RoomData.AdhLimitFlags.ADH_110:
			case RoomData.AdhLimitFlags.ADH_100 | RoomData.AdhLimitFlags.ADH_110:
			case RoomData.AdhLimitFlags.ADH_90 | RoomData.AdhLimitFlags.ADH_100 | RoomData.AdhLimitFlags.ADH_110:
				return 0;
			}
		}

		public static int GetWDLimitType(RoomData.WheelDriveType limit)
		{
			switch (limit)
			{
			case RoomData.WheelDriveType.NONE:
			case RoomData.WheelDriveType.ALL:
				return 0;
			case RoomData.WheelDriveType.WD_RWD:
				return 1;
			case RoomData.WheelDriveType.WD_FOURWD:
				return 2;
			default:
				Debug.LogError($"Unsupported WheelDrive flag, unable to get sprite index for: '{limit}'");
				return 0;
			}
		}

		private void SubscribeOnRoomOptionsChanged(bool subscribe)
		{
			if (NetworkController.InstanceGame.Client == null)
			{
				m_roomOptionUpdAction = null;
			}
			else if (subscribe && m_roomOptionUpdAction == null)
			{
				m_roomOptionUpdAction = OnRoomOptionsChangedHandler;
				NetworkController.InstanceGame.Client.RoomOptionsChanged += m_roomOptionUpdAction;
			}
			else if (!subscribe && m_roomOptionUpdAction != null)
			{
				NetworkController.InstanceGame.Client.RoomOptionsChanged -= m_roomOptionUpdAction;
				m_roomOptionUpdAction = null;
			}
		}

		private void OnEnterRoomHandler()
		{
			SubscribeOnRoomOptionsChanged(subscribe: true);
			UpdateRoomOptions(NetworkController.InstanceGame?.CurrentRoomOptions);
		}

		private void OnLeaveRoomHandler()
		{
			SubscribeOnRoomOptionsChanged(subscribe: false);
		}
	}
}
