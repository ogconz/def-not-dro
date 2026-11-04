using System;
using System.Collections;
using System.Collections.Generic;
using Binding;
using Binding.Base;
using CodeStage.AntiCheat.ObscuredTypes;
using Cysharp.Threading.Tasks;
using DB;
using DB.Meta;
using DI;
using GameInput;
using GameOverlay;
using ModIO;
using PrefsModels;
using Rewired;
using Services.UGC.Interfaces;
using Services.UGC.Utils;
using SyncMultiplayer;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[GameHotkeyBlocker(true)]
[AudioFXBlocker(true)]
public class UINetworkRoomsContext : BaseBindingBehaviourTarget
{
	public enum ENavMode
	{
		List = 0,
		Header = 1,
		Filters = 2
	}

	public enum ESortMode
	{
		Locked = 0,
		Caption = 1,
		Name = 2,
		Settings = 3,
		Players = 4
	}

	[Serializable]
	public class OptionsFilterData
	{
		public Button button;

		public FilterButtonType filterType;

		public void AddListener(Action<FilterButtonType> action)
		{
			button.onClick.AddListener(delegate
			{
				action.SafeCall(filterType);
			});
		}

		public void RemoveAllListeners()
		{
			button.onClick.RemoveAllListeners();
		}
	}

	public enum IntPropertyFilterState
	{
		Ignored = 0,
		Enabled = 1,
		Disabled = 2
	}

	private class SortDescriptor
	{
		public ESortMode mode;

		public Property<bool> reversedProperty;

		public Func<RoomData, RoomData, bool, int> comparator;
	}

	private const float INPUT_FILTER_DELAY = 0.5f;

	private const float DOUBLE_CLICK_TIME = 0.2f;

	private const float SCROLL_COOLDOWN = 5f;

	public static List<string> ignoredRooms = new List<string>();

	[SerializeField]
	private const float MAX_ROOM_LIFE_TIME = 15f;

	[SerializeField]
	private const float SELECTED_ROOM_LIFE_TIME = 40f;

	private const float SELECTED_ROOM_LIFE_TIME_ACTIVE_FILTER = 3f;

	[SerializeField]
	private RoomUILine m_roomLineTemplate;

	private List<RoomUILine> m_roomLinesPool = new List<RoomUILine>(64);

	private List<RoomData> m_roomList = new List<RoomData>(64);

	private RoomUILine m_currentFocusedRoom;

	[SerializeField]
	private TMP_InputField m_searchByNameInputField;

	[SerializeField]
	private ElementNavigate m_trackSwitcher;

	[SerializeField]
	private ElementNavigate m_vinylsVisibility;

	[SerializeField]
	private ElementNavigate m_adhLimitFilter;

	[SerializeField]
	private ElementNavigate m_powerDensityLimitFilter;

	[SerializeField]
	private ElementNavigate m_roomType;

	[SerializeField]
	protected ScrollRect m_scrollRect;

	public List<OptionsFilterData> optionsFilterData;

	[Dependency]
	private BaseModel m_model;

	[Dependency]
	private GamePrefs m_prefs;

	private float m_onEndEditTime;

	private float m_scrollTime;

	private bool m_ignoreScrollCoolTimeToNextUpdate;

	private bool m_forceOldRoomListOnNextUpdate;

	private float m_scrollerLastY;

	private string m_lastSelectedRoom;

	private bool m_navigationSubmitActionCalled;

	private bool m_onJoinPassPressed;

	private List<SortDescriptor> m_sortPriorities = new List<SortDescriptor>();

	private ENavMode navigateMode = ENavMode.Header;

	private List<string> m_trackNameOptions = new List<string>();

	private readonly List<RoomData.AdhLimitFlags> m_adhLimitOptions = new List<RoomData.AdhLimitFlags>();

	private readonly List<RoomData.PowerDensityLimitFlag> m_powerDensityLimitOptions = new List<RoomData.PowerDensityLimitFlag>();

	private readonly List<RoomData.CollisionTypeFilter> m_collisionTypeOptions = new List<RoomData.CollisionTypeFilter>();

	private readonly List<RoomData.RoomSettingFlags> m_roomTypeOptions = new List<RoomData.RoomSettingFlags>();

	private readonly List<RoomData.WheelDriveType> m_wheelDriveTypeOptions = new List<RoomData.WheelDriveType>();

	private UIReportContext m_reportContext;

	private LobbyFilterState m_lobbyFilterState;

	private RoomUILine m_lastClickLine;

	private float m_lastClickTime;

	public readonly Property<int> playerCount = new IntProperty();

	public readonly Property<bool> canJoin = new BoolProperty();

	public readonly Property<bool> canCreate = new BoolProperty();

	public readonly Property<int> activeSorter = new IntProperty();

	public readonly Property<bool> captionReversed = new BoolProperty();

	public readonly Property<bool> nameReversed = new BoolProperty();

	public readonly Property<bool> lockedReversed = new BoolProperty();

	public readonly Property<bool> playersReversed = new BoolProperty();

	public readonly Property<bool> settingsReversed = new BoolProperty();

	public readonly Property<int> indexHeaderSelection = new IntProperty();

	private static int? m_savedIndexHeaderSelection;

	public IntProperty visibleArrow = new IntProperty(-1);

	private bool m_needSkipReserveArrows;

	public readonly BoolProperty enableCrossMP = new BoolProperty();

	public readonly BoolProperty crossMpToggle = new BoolProperty();

	public readonly BoolProperty enableWorkshop = new BoolProperty();

	public readonly BoolProperty workshopToggle = new BoolProperty();

	public readonly StringProperty serverRegion = new StringProperty();

	private readonly StringProperty m_vinylOptionsCaption = new StringProperty();

	private readonly StringProperty m_trackName = new StringProperty();

	private readonly StringProperty m_adhLimitString = new StringProperty();

	private readonly StringProperty m_powerDensityLimitString = new StringProperty();

	private readonly StringProperty m_roomTypeString = new StringProperty();

	private readonly StringProperty m_roomsCount = new StringProperty();

	private readonly BoolProperty m_activeRoomNameInput = new BoolProperty();

	private readonly BoolProperty m_showFilterQueryEmpty = new BoolProperty();

	private readonly BoolProperty m_showEnableCrossPlatformText = new BoolProperty();

	private readonly BoolProperty m_roomNameActiveFilter = new BoolProperty();

	private readonly BoolProperty m_showLoadingSpinner = new BoolProperty();

	private readonly BoolProperty m_friendsFilterActive = new BoolProperty();

	private readonly BoolProperty m_showGamePadHotkeysTracks = new BoolProperty();

	private readonly BoolProperty m_showGamePadHotkeysVinyls = new BoolProperty();

	private readonly BoolProperty m_showGamePadHotkeysPowerDensity = new BoolProperty();

	private readonly BoolProperty m_showGamePadHotkeysRoomType = new BoolProperty();

	private readonly BoolProperty m_showGamePadHotkeysAdh = new BoolProperty();

	private readonly BoolProperty m_trackSharingSupported = new BoolProperty();

	private readonly IntProperty m_driftModeIndexView = new IntProperty();

	private readonly IntProperty m_timeAttackIndexView = new IntProperty();

	private readonly IntProperty m_clippingPointsIndexView = new IntProperty();

	private readonly IntProperty m_collisionsIndexView = new IntProperty();

	private readonly IntProperty m_voteIndexView = new IntProperty();

	private readonly IntProperty m_nightModeIndexView = new IntProperty();

	private readonly IntProperty m_passwordIndexView = new IntProperty();

	private readonly IntProperty m_fullRoomIndexView = new IntProperty();

	private readonly IntProperty m_friendsIndexView = new IntProperty();

	private readonly IntProperty m_assistIndexView = new IntProperty();

	private readonly IntProperty m_tractionControlIndexView = new IntProperty();

	private readonly IntProperty m_wheelDriveIndexView = new IntProperty();

	private HashSet<ulong> m_hiddenExternalTracks = new HashSet<ulong>();

	protected override void OnActivate()
	{
		m_roomLineTemplate.gameObject.SetActive(value: false);
		base.OnActivate();
		Overlay.instance.richPresence.Show(RichPresenceStatus.MainMenu);
		if (!base.owner.Get<UIBackgroundContext>().gameObject.activeSelf)
		{
			base.owner.Add<UIBackgroundContext>();
		}
		if (m_reportContext == null)
		{
			m_reportContext = base.owner.Get<UIReportContext>();
		}
		m_hiddenExternalTracks.Clear();
		UIReportContext.ClearBanPlayersList();
		BringForwardByDepth();
		InitiateSorter();
		if (!NetworkController.IsActive)
		{
			NetworkController.LobbyEnter();
		}
		NetworkGame instanceGame = NetworkController.InstanceGame;
		instanceGame.OnGameListUpdate = (Action<List<string>>)Delegate.Combine(instanceGame.OnGameListUpdate, new Action<List<string>>(OnGameListUpdate));
		WebConfigManager.changeRegionAction = (Action<string>)Delegate.Combine(WebConfigManager.changeRegionAction, new Action<string>(OnChangeRegionHandler));
		SmartfoxLobby.sendLobbyRequestAction = (Action<bool>)Delegate.Combine(SmartfoxLobby.sendLobbyRequestAction, new Action<bool>(SendLobbyRequestActionHandler));
		m_scrollRect.onValueChanged.AddListener(OnScrollValueChangeHandler);
		m_searchByNameInputField.onEndEdit.AddListener(OnEndEditInputHandler);
		m_searchByNameInputField.onValueChanged.AddListener(OnValueChangedHandler);
		optionsFilterData.ForEach(delegate(OptionsFilterData data)
		{
			data.AddListener(OnClickFilterHandler);
		});
		GameInput.InputManager instance = GameInput.InputManager.instance;
		instance.onChangeActiveDevice = (Action<Controller>)Delegate.Combine(instance.onChangeActiveDevice, new Action<Controller>(OnChangeActiveDeviceHandler));
		OnChangeRegionHandler(m_prefs.globals.currentRegion);
		playerCount.value = 0;
		enableCrossMP.value = WebConfigManager.ServerAllowCrossPlatform;
		if (Overlay.instance.IsCrossPlayRestricted)
		{
			m_prefs.globals.crossPlatformPlay = false;
		}
		NetworkController.InstanceGame?.SetToNullCrossMPCreatedRoom();
		crossMpToggle.value = m_prefs.globals.crossPlatformPlay;
		enableWorkshop.value = Overlay.instance.GetPlatform() == UserPlatform.Id.Steam;
		workshopToggle.value = m_prefs.globals.workshopNetworkCars;
		m_vinylOptionsCaption.value = GetLocalizedVinylOption();
		m_showGamePadHotkeysTracks.value = false;
		m_showGamePadHotkeysVinyls.value = false;
		m_showGamePadHotkeysAdh.value = false;
		m_showGamePadHotkeysPowerDensity.value = false;
		m_showGamePadHotkeysRoomType.value = false;
		ClearGameList();
		m_showFilterQueryEmpty.value = false;
		RefreshBottomPanel();
		InitFilters();
		InitRoomName();
		InitTrackSwitcher();
		InitAdhLimitFilter();
		InitPowerDensityFilter();
		InitRoomTypeFilter();
		InitCollisionFilter();
		InitWheelDriveTypeFilter();
		InitNavigationMode();
		StartCoroutine(DisabeFilterSelectionAtStart());
		m_forceOldRoomListOnNextUpdate = false;
		SubscribePS4TextReceive();
		SubscribeSteamDeckTextReceive();
		NetworkController.UpdateCurrentCarProfile();
	}

	protected override void OnDeactivate()
	{
		NetworkGame instanceGame = NetworkController.InstanceGame;
		instanceGame.OnGameListUpdate = (Action<List<string>>)Delegate.Remove(instanceGame.OnGameListUpdate, new Action<List<string>>(OnGameListUpdate));
		WebConfigManager.changeRegionAction = (Action<string>)Delegate.Remove(WebConfigManager.changeRegionAction, new Action<string>(OnChangeRegionHandler));
		SmartfoxLobby.sendLobbyRequestAction = (Action<bool>)Delegate.Remove(SmartfoxLobby.sendLobbyRequestAction, new Action<bool>(SendLobbyRequestActionHandler));
		m_scrollRect.onValueChanged.RemoveListener(OnScrollValueChangeHandler);
		m_searchByNameInputField.onEndEdit.RemoveListener(OnEndEditInputHandler);
		m_searchByNameInputField.onValueChanged.RemoveListener(OnValueChangedHandler);
		optionsFilterData.ForEach(delegate(OptionsFilterData data)
		{
			data.RemoveAllListeners();
		});
		GameInput.InputManager instance = GameInput.InputManager.instance;
		instance.onChangeActiveDevice = (Action<Controller>)Delegate.Remove(instance.onChangeActiveDevice, new Action<Controller>(OnChangeActiveDeviceHandler));
		SaveSorter();
		base.owner.Hide<UIBackgroundContext>();
		m_hiddenExternalTracks.Clear();
		UnSubscribePS4TextReceive();
		UnSubscribeSteamDeckTextReceive();
		base.OnDeactivate();
	}

	private void SubscribePS4TextReceive()
	{
	}

	private void UnSubscribePS4TextReceive()
	{
	}

	private void SubscribeSteamDeckTextReceive()
	{
		if (GameManager.IsLinuxOsPlatform() && Overlay.instance is SteamOverlay steamOverlay)
		{
			steamOverlay.onCloseOverlayKeyboard = (Action<string>)Delegate.Combine(steamOverlay.onCloseOverlayKeyboard, new Action<string>(OnReceiveInputText));
		}
	}

	private void UnSubscribeSteamDeckTextReceive()
	{
		if (GameManager.IsLinuxOsPlatform() && Overlay.instance is SteamOverlay steamOverlay)
		{
			steamOverlay.onCloseOverlayKeyboard = (Action<string>)Delegate.Remove(steamOverlay.onCloseOverlayKeyboard, new Action<string>(OnReceiveInputText));
		}
	}

	public static void ResetSavedHeaderSortSelection()
	{
		m_savedIndexHeaderSelection = null;
	}

	public override bool IsAllowNavigation()
	{
		return false;
	}

	private void InitNavigationMode()
	{
		if (!m_savedIndexHeaderSelection.HasValue)
		{
			ForceSetListNavigationMode();
			return;
		}
		m_needSkipReserveArrows = true;
		SubmitSort(m_savedIndexHeaderSelection.Value);
	}

	internal void OnClickRoomLine(RoomUILine roomLine)
	{
		GameInput.InputManager.PlayClickSound();
		ForceSetListNavigationMode();
		SetFocusUI(roomLine);
		if (IsDoubleClick(roomLine))
		{
			NavigationSubmit();
		}
	}

	private bool IsDoubleClick(RoomUILine roomLine)
	{
		float unscaledTime = Time.unscaledTime;
		float num = unscaledTime - m_lastClickTime;
		RoomUILine lastClickLine = m_lastClickLine;
		m_lastClickLine = roomLine;
		m_lastClickTime = unscaledTime;
		if (roomLine != null && lastClickLine == roomLine)
		{
			return num <= 0.2f;
		}
		return false;
	}

	private IEnumerator DisabeFilterSelectionAtStart()
	{
		yield return null;
		base.selectedItem = null;
	}

	private void OnScrollValueChangeHandler(Vector2 val)
	{
		if (!m_ignoreScrollCoolTimeToNextUpdate)
		{
			if (Mathf.Abs(m_scrollerLastY - val.y) > 0.01f)
			{
				m_scrollTime = Time.unscaledTime;
			}
			m_scrollerLastY = val.y;
		}
	}

	private void OnChangeActiveDeviceHandler(Controller controller)
	{
		if (m_showGamePadHotkeysTracks.value || m_showGamePadHotkeysVinyls.value || m_showGamePadHotkeysAdh.value || m_showGamePadHotkeysPowerDensity.value || m_showGamePadHotkeysRoomType.value)
		{
			ShowTrackSwitcherHotkeysIfNeed();
		}
	}

	private void RefreshBottomPanel()
	{
		ShowBottomPanel(new ActionHintData[6]
		{
			new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_goto_back.Localized(),
				type = ActionType.NavCancelBtnB,
				action = NavigationCancel
			},
			new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_select.Localized(),
				type = ActionType.NavSelectBtnA,
				action = NavigationSubmit
			},
			new ActionHintData
			{
				hint = LocalizationKeys.network_game_btn_joinpass.Localized(),
				type = ActionType.NavAdditional0BtnX,
				action = NavigationJoinByPass
			},
			new ActionHintData
			{
				hint = LocalizationKeys.change_server.Localized(),
				type = ActionType.NavBtnExpandRight,
				action = NavigationChangeServerRegion
			},
			new ActionHintData
			{
				hint = LocalizationKeys.nav_select_report.Localized(),
				type = ActionType.NavReport,
				action = NavigationReportRoom
			},
			new ActionHintData
			{
				hint = LocalizationKeys.network_game_btn_create.Localized(),
				type = ActionType.NavAdditional1BtnY,
				action = NavigationCreateRoom
			}
		});
	}

	private void InitRoomName()
	{
		m_searchByNameInputField.text = m_prefs.globals.lobbyRoomNameInputFilter;
		m_roomNameActiveFilter.value = !string.IsNullOrEmpty(m_searchByNameInputField.text);
	}

	private void InitTrackSwitcher()
	{
		List<DB.Meta.Track> list = m_model.QueryAll<DB.Meta.Track>();
		list.Sort(DB.Meta.Track.Sort);
		m_trackNameOptions.Add(LocalizationKeys.lobby_all_tracks_filter.KeyValue);
		if (!Overlay.instance.IsUserGeneratedContentRestricted)
		{
			m_trackNameOptions.Add(LocalizationKeys.external_tracks.KeyValue);
			m_trackNameOptions.Add(LocalizationKeys.standard_tracks.KeyValue);
		}
		foreach (DB.Meta.Track item in list)
		{
			if (item.enabled && !item.external && (GameConsole.Bool["ChampionshipMode"] || !item.hidden) && !(item.configuration.ToLower() != "default"))
			{
				m_trackNameOptions.Add(item.name);
			}
		}
		string lobbyTrackNameSwitcherFilter = m_prefs.globals.lobbyTrackNameSwitcherFilter;
		string andSaveTrackNameOption = (string.IsNullOrEmpty(lobbyTrackNameSwitcherFilter) ? m_trackNameOptions[0] : lobbyTrackNameSwitcherFilter);
		SetAndSaveTrackNameOption(andSaveTrackNameOption);
	}

	private void InitAdhLimitFilter()
	{
		m_adhLimitOptions.Clear();
		m_adhLimitOptions.Add(RoomData.AdhLimitFlags.NONE);
		m_adhLimitOptions.Add(RoomData.AdhLimitFlags.ADH_90);
		m_adhLimitOptions.Add(RoomData.AdhLimitFlags.ADH_100);
		m_adhLimitOptions.Add(RoomData.AdhLimitFlags.ADH_110);
		m_adhLimitOptions.Add(RoomData.AdhLimitFlags.ADH_120);
		SetAndSaveAdhLimitOption(m_prefs.globals.lobbyAdhLimitFilter);
	}

	private void InitPowerDensityFilter()
	{
		m_powerDensityLimitOptions.Clear();
		m_powerDensityLimitOptions.Add(RoomData.PowerDensityLimitFlag.NONE);
		m_powerDensityLimitOptions.Add(RoomData.PowerDensityLimitFlag.PD_0_2);
		m_powerDensityLimitOptions.Add(RoomData.PowerDensityLimitFlag.PD_0_3);
		m_powerDensityLimitOptions.Add(RoomData.PowerDensityLimitFlag.PD_0_4);
		m_powerDensityLimitOptions.Add(RoomData.PowerDensityLimitFlag.PD_0_5);
		m_powerDensityLimitOptions.Add(RoomData.PowerDensityLimitFlag.PD_0_6);
		m_powerDensityLimitOptions.Add(RoomData.PowerDensityLimitFlag.PD_0_7);
		m_powerDensityLimitOptions.Add(RoomData.PowerDensityLimitFlag.PD_0_8);
		m_powerDensityLimitOptions.Add(RoomData.PowerDensityLimitFlag.PD_0_9);
		m_powerDensityLimitOptions.Add(RoomData.PowerDensityLimitFlag.PD_1_0);
		SetAndSavePowerDensityLimitOption(0, 0);
	}

	private void InitCollisionFilter()
	{
		m_collisionTypeOptions.Clear();
		m_collisionTypeOptions.Add(RoomData.CollisionTypeFilter.None);
		m_collisionTypeOptions.Add(RoomData.CollisionTypeFilter.Classic);
		m_collisionTypeOptions.Add(RoomData.CollisionTypeFilter.Dynamic);
		m_collisionTypeOptions.Add(RoomData.CollisionTypeFilter.Disabled);
		SetAndSaveCollisionFilterTypeOption(0);
	}

	private void InitRoomTypeFilter()
	{
		m_roomTypeOptions.Clear();
		m_roomTypeOptions.Add(RoomData.RoomSettingFlags.NONE);
		m_roomTypeOptions.Add(RoomData.RoomSettingFlags.DRIFT_ALLOWED);
		m_roomTypeOptions.Add(RoomData.RoomSettingFlags.TIME_ALLOWED);
		SetAndSaveGameRoomTypeOption(0);
	}

	private void InitWheelDriveTypeFilter()
	{
		m_wheelDriveTypeOptions.Clear();
		m_wheelDriveTypeOptions.Add(RoomData.WheelDriveType.NONE);
		m_wheelDriveTypeOptions.Add(RoomData.WheelDriveType.WD_RWD);
		m_wheelDriveTypeOptions.Add(RoomData.WheelDriveType.WD_FOURWD);
		SetAndSaveWheelDriveType(0);
	}

	private void InitFilters()
	{
		m_lobbyFilterState = NetworkController.InstanceGame.lobbyFilterState;
		SetFilterDataValues();
	}

	private void SetFilterDataValues()
	{
		m_trackSharingSupported.value = true;
		foreach (OptionsFilterData optionsFilterDatum in optionsFilterData)
		{
			SetFiltersUIAndSave(optionsFilterDatum.filterType);
		}
	}

	private void OnClickFilterHandler(FilterButtonType filterType)
	{
		ProceedFilterSelectionAsync(filterType).Forget();
	}

	private async UniTaskVoid ProceedFilterSelectionAsync(FilterButtonType selectedFilterType)
	{
		if (selectedFilterType == FilterButtonType.ExternalTrack && !m_lobbyFilterState.IsFlagEnabled(FilterButtonType.ExternalTrack) && !(await EnsureUGCTracksSelectionAbility()))
		{
			return;
		}
		ClearRoomNameFilter();
		if (IsFilterEnabled(FilterButtonType.Friends) && selectedFilterType != FilterButtonType.Friends)
		{
			m_friendsFilterActive.value = true;
			return;
		}
		switch (selectedFilterType)
		{
		case FilterButtonType.wdLimit:
			SetWheelDriveTypeOption(next: true);
			break;
		case FilterButtonType.CollisionType:
			SetCollisionFilterTypeOption(next: true);
			m_lobbyFilterState.SaveFlag(selectedFilterType);
			return;
		}
		m_lobbyFilterState.SetNextFlag(selectedFilterType);
		SetFiltersUIAndSave(selectedFilterType);
		SetRoomsOld();
	}

	private async UniTask<bool> EnsureUGCTracksSelectionAbility()
	{
		IGameOverlay instance = Overlay.instance;
		if (instance.IsUserGeneratedContentRestricted)
		{
			instance.ShowUGCRestrictionDialog();
			return false;
		}
		if (!(await CheckUGCServiceAuthorizationAsync()))
		{
			return false;
		}
		return true;
	}

	private async UniTask<bool> CheckUGCServiceAuthorizationAsync()
	{
		IUGCAuthentication ugcAuthentication = UGCServiceProvider.instance.GetServiceBy(IUGCManager.ServiceProvider.ModIO).authentication;
		bool flag = await ugcAuthentication.IsAuthenticatedAsync();
		if (!flag)
		{
			flag = await ugcAuthentication.AuthenticateAsync();
		}
		return flag;
	}

	private bool IsFilterEnabled(FilterButtonType filterType)
	{
		return m_lobbyFilterState.IsFlagEnabled(filterType);
	}

	private void SetFiltersUIAndSave(FilterButtonType filterType)
	{
		IntProperty intProperty = null;
		switch (filterType)
		{
		case FilterButtonType.DriftMode:
			intProperty = m_driftModeIndexView;
			break;
		case FilterButtonType.TimeAttack:
			intProperty = m_timeAttackIndexView;
			break;
		case FilterButtonType.ClippingPoints:
			intProperty = m_clippingPointsIndexView;
			break;
		case FilterButtonType.CollisionType:
			intProperty = m_collisionsIndexView;
			break;
		case FilterButtonType.Vote:
			intProperty = m_voteIndexView;
			break;
		case FilterButtonType.NightMode:
			intProperty = m_nightModeIndexView;
			break;
		case FilterButtonType.Password:
			intProperty = m_passwordIndexView;
			break;
		case FilterButtonType.FullRoom:
			intProperty = m_fullRoomIndexView;
			break;
		case FilterButtonType.Friends:
			intProperty = m_friendsIndexView;
			break;
		case FilterButtonType.AssistVersion:
			intProperty = m_assistIndexView;
			break;
		case FilterButtonType.tcMode:
			intProperty = m_tractionControlIndexView;
			break;
		case FilterButtonType.wdLimit:
			intProperty = m_wheelDriveIndexView;
			break;
		}
		intProperty.value = (int)m_lobbyFilterState.GetFlagValue(filterType);
		m_friendsFilterActive.value = IsFilterEnabled(FilterButtonType.Friends);
		m_lobbyFilterState.SaveFlag(filterType);
	}

	private void SetAndSaveTrackNameOption(string trackName)
	{
		m_trackName.value = LocalizationService.Localize(trackName);
		m_prefs.globals.lobbyTrackNameSwitcherFilter = trackName;
		if (trackName == LocalizationKeys.standard_tracks.KeyValue || trackName == LocalizationKeys.external_tracks.KeyValue)
		{
			m_lobbyFilterState.ignoredFilterDict[FilterButtonType.ExternalTrack] = false;
			m_lobbyFilterState.filterDict[FilterButtonType.ExternalTrack] = LobbyFilterState.Flag.On;
		}
		else
		{
			m_lobbyFilterState.ignoredFilterDict[FilterButtonType.ExternalTrack] = true;
			m_lobbyFilterState.filterDict[FilterButtonType.ExternalTrack] = LobbyFilterState.Flag.Ignore;
		}
		SetRoomsOld();
	}

	public void SetNextTrackOption(bool next)
	{
		if (!IsFilterEnabled(FilterButtonType.Friends))
		{
			ClearRoomNameFilter();
			int num = m_trackNameOptions.FindIndex((string el) => el.Equals(m_prefs.globals.lobbyTrackNameSwitcherFilter));
			num = ((!next) ? (--num) : (++num));
			if (num > m_trackNameOptions.Count - 1)
			{
				num = 0;
			}
			if (num < 0)
			{
				num = m_trackNameOptions.Count - 1;
			}
			SetAndSaveTrackNameOption(m_trackNameOptions[num]);
		}
	}

	private void SetAndSaveAdhLimitOption(int limit)
	{
		m_adhLimitString.value = LocalizationKeys.network_game_params_adh_limit_short.Localized() + " " + RoomData.AdhLimitToString((RoomData.AdhLimitFlags)limit);
		m_prefs.globals.lobbyAdhLimitFilter = limit;
		SetRoomsOld();
	}

	private void SetAndSavePowerDensityLimitOption(int limit, int index)
	{
		m_powerDensityLimitString.value = LocalizationKeys.garage_setup_power_density.Localized() + ": " + RoomData.PowerDensityLimitToString((RoomData.PowerDensityLimitFlag)limit);
		m_prefs.globals.lobbyPowerDensityLimitFilter = limit;
		m_prefs.globals.lobbyPowerDensityIndexFilter = index;
		SetRoomsOld();
	}

	public void SetNextAdhLimitOption(bool next)
	{
		if (!IsFilterEnabled(FilterButtonType.Friends))
		{
			int index = SetOptionSwitchParam(m_adhLimitOptions, next, m_prefs.globals.lobbyAdhLimitFilter);
			SetAndSaveAdhLimitOption((int)m_adhLimitOptions[index]);
		}
	}

	public void SetNextPowerDensityOption(bool next)
	{
		if (!IsFilterEnabled(FilterButtonType.Friends))
		{
			int index = SetOptionSwitchParam(m_powerDensityLimitOptions, next, m_prefs.globals.lobbyPowerDensityLimitFilter);
			SetAndSavePowerDensityLimitOption((int)m_powerDensityLimitOptions[index], index);
		}
	}

	public void SetNextGameRoomTypeOption(bool next)
	{
		if (!IsFilterEnabled(FilterButtonType.Friends))
		{
			int index = SetOptionSwitchParam(m_roomTypeOptions, next, m_prefs.globals.roomType);
			SetAndSaveGameRoomTypeOption((int)m_roomTypeOptions[index]);
		}
	}

	public void SetWheelDriveTypeOption(bool next)
	{
		if (!IsFilterEnabled(FilterButtonType.Friends))
		{
			int index = SetOptionSwitchParam(m_wheelDriveTypeOptions, next, m_prefs.globals.lobbyWheelDriveTypeFilter);
			SetAndSaveWheelDriveType((int)m_wheelDriveTypeOptions[index]);
		}
	}

	public void SetCollisionFilterTypeOption(bool next)
	{
		if (!IsFilterEnabled(FilterButtonType.Friends))
		{
			int index = SetOptionSwitchParam(m_collisionTypeOptions, next, m_prefs.globals.collisionsIndexFilter);
			SetAndSaveCollisionFilterTypeOption((int)m_collisionTypeOptions[index]);
		}
	}

	private int SetOptionSwitchParam<T>(List<T> options, bool next, int globalValue) where T : Enum
	{
		int num = options.FindIndex((T a) => Convert.ToInt32(a) == globalValue);
		num = ((!next) ? (--num) : (++num));
		if (num > options.Count - 1)
		{
			num = 0;
		}
		if (num < 0)
		{
			num = options.Count - 1;
		}
		return num;
	}

	private void SetAndSaveGameRoomTypeOption(int roomType)
	{
		m_roomTypeString.value = LocalizationKeys.network_game_room_type.Localized() + ": " + RoomData.RoomTypeToString((RoomData.RoomTypeFlags)roomType);
		m_prefs.globals.roomType = roomType;
		m_lobbyFilterState.filterDict[FilterButtonType.ClippingPoints] = LobbyFilterState.Flag.Ignore;
		m_lobbyFilterState.ignoredFilterDict[FilterButtonType.ClippingPoints] = true;
		m_lobbyFilterState.filterDict[FilterButtonType.Vote] = LobbyFilterState.Flag.Ignore;
		m_lobbyFilterState.ignoredFilterDict[FilterButtonType.Vote] = true;
		switch (roomType)
		{
		case 0:
			m_lobbyFilterState.filterDict[FilterButtonType.DriftMode] = LobbyFilterState.Flag.Ignore;
			m_lobbyFilterState.filterDict[FilterButtonType.TimeAttack] = LobbyFilterState.Flag.Ignore;
			m_lobbyFilterState.ignoredFilterDict[FilterButtonType.DriftMode] = true;
			m_lobbyFilterState.ignoredFilterDict[FilterButtonType.TimeAttack] = true;
			break;
		case 1:
			m_lobbyFilterState.filterDict[FilterButtonType.DriftMode] = LobbyFilterState.Flag.On;
			m_lobbyFilterState.filterDict[FilterButtonType.TimeAttack] = LobbyFilterState.Flag.Ignore;
			m_lobbyFilterState.ignoredFilterDict[FilterButtonType.DriftMode] = false;
			m_lobbyFilterState.ignoredFilterDict[FilterButtonType.TimeAttack] = true;
			break;
		case 2:
			m_lobbyFilterState.filterDict[FilterButtonType.DriftMode] = LobbyFilterState.Flag.Ignore;
			m_lobbyFilterState.filterDict[FilterButtonType.TimeAttack] = LobbyFilterState.Flag.On;
			m_lobbyFilterState.ignoredFilterDict[FilterButtonType.DriftMode] = true;
			m_lobbyFilterState.ignoredFilterDict[FilterButtonType.TimeAttack] = false;
			break;
		default:
			throw new Exception("Invalid roomtypeflag");
		}
		SetRoomsOld();
	}

	private void SetAndSaveCollisionFilterTypeOption(int collisionType)
	{
		m_collisionsIndexView.value = collisionType;
		m_prefs.globals.collisionsIndexFilter = collisionType;
		Dictionary<FilterButtonType, LobbyFilterState.Flag> filterDict = m_lobbyFilterState.filterDict;
		LobbyFilterState.Flag value = default(LobbyFilterState.Flag);
		switch (collisionType)
		{
		case 0:
			value = LobbyFilterState.Flag.Ignore;
			break;
		case 1:
			value = LobbyFilterState.Flag.On;
			break;
		case 2:
			value = LobbyFilterState.Flag.On;
			break;
		case 3:
			value = LobbyFilterState.Flag.Off;
			break;
		default:
			global::_003CPrivateImplementationDetails_003E.ThrowSwitchExpressionException((RoomData.CollisionTypeFilter)collisionType);
			break;
		}
		filterDict[FilterButtonType.CollisionType] = value;
		SetRoomsOld();
	}

	private void SetAndSaveWheelDriveType(int type)
	{
		m_wheelDriveIndexView.value = type;
		m_prefs.globals.lobbyWheelDriveTypeFilter = type;
		SetRoomsOld();
	}

	public void OnResetFilters()
	{
		ClearRoomNameFilter();
		SetAndSaveTrackNameOption(m_trackNameOptions[0]);
		SetAndSaveAdhLimitOption((int)m_adhLimitOptions[0]);
		SetAndSavePowerDensityLimitOption((int)m_powerDensityLimitOptions[0], 0);
		SetAndSaveGameRoomTypeOption(0);
		SetAndSaveWheelDriveType(0);
		SetAndSaveCollisionFilterTypeOption(0);
		m_lobbyFilterState.Reset();
		SetFilterDataValues();
	}

	private void ClearRoomNameFilter()
	{
		GlobalModel globals = m_prefs.globals;
		string lobbyRoomNameInputFilter = (m_searchByNameInputField.text = string.Empty);
		globals.lobbyRoomNameInputFilter = lobbyRoomNameInputFilter;
	}

	public void OnReActivate()
	{
		RefreshBottomPanel();
		SubscribePS4TextReceive();
		SubscribeSteamDeckTextReceive();
	}

	private void OnChangeRegionHandler(string newRegion)
	{
		string localizedServer = WebConfigManager.GetLocalizedServer(newRegion);
		serverRegion.value = LocalizationKeys.server_current.LocalizedFormat(localizedServer);
		ClearGameList();
	}

	private void SendLobbyRequestActionHandler(bool startLobbyRequest)
	{
		m_showLoadingSpinner.value = startLobbyRequest;
	}

	private void ForceSetListNavigationMode(bool needResetHeaderSelection = true)
	{
		navigateMode = ENavMode.List;
		if (needResetHeaderSelection)
		{
			indexHeaderSelection.value = -1;
			ResetFilterMode();
		}
	}

	private void UpdateList(bool forceClearList, string removedRoom)
	{
		List<SmartfoxLobby.RoomLobbyInfo> rooms = NetworkController.InstanceGame.GetRooms();
		bool num = !m_ignoreScrollCoolTimeToNextUpdate && Time.unscaledTime - m_scrollTime < 5f;
		m_ignoreScrollCoolTimeToNextUpdate = false;
		if (forceClearList)
		{
			m_roomList.Clear();
			m_ignoreScrollCoolTimeToNextUpdate = true;
		}
		if (!num)
		{
			if (!string.IsNullOrEmpty(removedRoom))
			{
				m_roomList.RemoveAll((RoomData room) => room.name == (ObscuredString)removedRoom);
			}
			for (int num2 = 0; num2 < rooms.Count; num2++)
			{
				AddOrUpdateRoomData(rooms[num2], onlyUpdate: false);
			}
			RemoveOldRooms();
		}
		else
		{
			for (int num3 = 0; num3 < rooms.Count; num3++)
			{
				AddOrUpdateRoomData(rooms[num3], onlyUpdate: true);
			}
		}
		if (m_forceOldRoomListOnNextUpdate)
		{
			m_forceOldRoomListOnNextUpdate = false;
			SetRoomsOld();
		}
		RemoveIgnoredRooms();
		RemoveHiddenExternalTracks();
		UpdateRoomUILinesFromRoomData();
		EnsureVisibleCurrentFocus();
		RequestRoomsCount();
		m_showFilterQueryEmpty.value = m_roomList.Count == 0;
		m_showEnableCrossPlatformText.value = NeedShowEnableCrossMpText();
	}

	private void EnsureVisibleCurrentFocus()
	{
		if (m_currentFocusedRoom != null && !m_scrollRect.IsScrollItemVerticalyVisible(m_currentFocusedRoom.rectTransform))
		{
			m_scrollRect.SnapScrollTo(m_currentFocusedRoom.rectTransform);
		}
	}

	private void UpdateRoomUILinesFromRoomData()
	{
		string focus = SaveCurrentFocus();
		m_roomList.Sort(CompareToSort);
		GOPool.Allocate(m_roomLineTemplate, ref m_roomLinesPool, m_roomList.Count, delegate(RoomUILine uiRoom)
		{
			uiRoom.context = this;
		}, OnRoomUIDeactivated);
		int num = 0;
		for (int num2 = 0; num2 < m_roomList.Count; num2++)
		{
			RoomData roomData = m_roomList[num2];
			roomData.UpdateDynamicProps();
			roomData.UpdateTrackName(GetTrackName(roomData.roomOptions));
			num += roomData.userCount;
			UpdateUIRoomInfo(roomData, m_roomLinesPool[num2]);
		}
		playerCount.value = num;
		SetFocus(focus);
	}

	private void UpdateUIRoomInfo(RoomData roomData, RoomUILine roomLine)
	{
		roomLine.UpdateFrom(roomData);
	}

	private void OnRoomUIDeactivated(RoomUILine roomLine)
	{
		roomLine.gameObject.SetActive(value: false);
		roomLine = null;
	}

	private void RemoveOldRooms()
	{
		for (int i = 0; i < m_roomList.Count; i++)
		{
			if (m_roomList[i].timeLeft <= 0f)
			{
				m_roomList.RemoveAt(i--);
			}
		}
	}

	private void SetRoomsOld()
	{
		for (int i = 0; i < m_roomList.Count; i++)
		{
			m_roomList[i].timeLeft = 0f;
		}
		m_ignoreScrollCoolTimeToNextUpdate = true;
		if (SmartfoxLobby.lobbyPostRequestInProgress)
		{
			m_forceOldRoomListOnNextUpdate = true;
		}
	}

	private void RemoveIgnoredRooms()
	{
		for (int i = 0; i < m_roomList.Count; i++)
		{
			RoomData roomData = m_roomList[i];
			if (ignoredRooms.Contains(roomData.roomId))
			{
				m_roomList.RemoveAt(i--);
			}
		}
	}

	private void RemoveHiddenExternalTracks()
	{
		m_roomList.RemoveAll((RoomData room) => m_hiddenExternalTracks.Contains(room.roomOptions.ExternalTrackId));
	}

	private void RemoveModdedRooms()
	{
		for (int i = 0; i < m_roomList.Count; i++)
		{
			RoomData roomData = m_roomList[i];
			if (roomData.isModedRoom || roomData.isExternalMap)
			{
				m_roomList.RemoveAt(i--);
			}
		}
	}

	private void Update()
	{
		UpdateRooms();
	}

	private void UpdateRooms()
	{
		for (int i = 0; i < m_roomList.Count; i++)
		{
			m_roomList[i].Update();
		}
	}

	private void AddOrUpdateRoomData(SmartfoxLobby.RoomLobbyInfo roomLobbyInfo, bool onlyUpdate)
	{
		RoomData roomData = FindRoomData(roomLobbyInfo.roomId);
		if (roomData == null)
		{
			if (onlyUpdate)
			{
				return;
			}
			roomData = new RoomData(roomLobbyInfo, GetTrackName(roomLobbyInfo.options));
			m_roomList.Add(roomData);
		}
		else
		{
			roomData.roomLobbyInfo = roomLobbyInfo;
		}
		roomData.timeLeft = 15f;
	}

	private string GetTrackName(SmartfoxRoomOptions roomOptions)
	{
		if (roomOptions.ExternalTrackId == 0L)
		{
			DB.Meta.Track byId = m_model.GetById<DB.Meta.Track>(roomOptions.TrackID);
			if (byId == null)
			{
				return string.Empty;
			}
			return byId.name;
		}
		return roomOptions.TrackName;
	}

	private RoomData FindRoomData(string roomId)
	{
		for (int i = 0; i < m_roomList.Count; i++)
		{
			RoomData roomData = m_roomList[i];
			if (roomData.roomId == (ObscuredString)roomId)
			{
				return roomData;
			}
		}
		return null;
	}

	private RoomData FindFirstRoomData()
	{
		if (m_roomList != null && m_roomList.Count > 0)
		{
			return m_roomList[0];
		}
		return null;
	}

	private RoomUILine FindRoomUI(RoomData item)
	{
		if (item != null)
		{
			for (int i = 0; i < m_roomLinesPool.Count; i++)
			{
				RoomUILine roomUILine = m_roomLinesPool[i];
				if (roomUILine.gameObject.activeInHierarchy && roomUILine.roomData.roomId == item.roomId)
				{
					return roomUILine;
				}
			}
		}
		return null;
	}

	private void SetFocus(string roomId)
	{
		RoomData item = FindRoomData(roomId);
		SetFocusUI(FindRoomUI(item));
	}

	private bool SetFocusUI(RoomUILine roomLine)
	{
		bool result = false;
		bool flag = IsActiveFilter();
		if (m_currentFocusedRoom != null)
		{
			m_currentFocusedRoom.SetFocus(flag: false);
			if (m_currentFocusedRoom.roomData.timeLeft > 0f)
			{
				m_currentFocusedRoom.roomData.timeLeft = (flag ? 3f : 15f);
			}
			m_currentFocusedRoom = null;
			result = true;
		}
		if (roomLine == null)
		{
			return result;
		}
		result = true;
		m_currentFocusedRoom = roomLine;
		m_currentFocusedRoom.SetFocus(flag: true);
		if (m_currentFocusedRoom.roomData.timeLeft > 0f)
		{
			m_currentFocusedRoom.roomData.timeLeft = (flag ? 3f : 40f);
		}
		return result;
	}

	private string SaveCurrentFocus()
	{
		if (!(m_currentFocusedRoom != null))
		{
			return null;
		}
		if (m_currentFocusedRoom.roomData == null)
		{
			return string.Empty;
		}
		return m_currentFocusedRoom.roomData.roomId.ToString();
	}

	private void OnGameListUpdate(List<string> removedRooms = null)
	{
		bool flag = NetworkController.InstanceGame.isRestrictCrossMpByAdmin && crossMpToggle.value;
		if (flag)
		{
			crossMpToggle.value = false;
		}
		bool forceClearList = removedRooms == null || flag;
		UpdateList(forceClearList, (removedRooms != null && removedRooms.Count > 0) ? removedRooms[0] : null);
	}

	private void RequestRoomsCount()
	{
		NetworkRequestsUtils.GetRoomsUsersCount(WebConfigManager.MultiplayerMasterServerUrl, delegate(int totalRoomsCount, int usersCount)
		{
			m_roomsCount.value = $"{m_roomList.Count}/{totalRoomsCount}";
		});
	}

	private bool NeedShowEnableCrossMpText()
	{
		NetworkGame instanceGame = NetworkController.InstanceGame;
		if (instanceGame == null || !enableCrossMP.value || crossMpToggle.value)
		{
			return false;
		}
		int num = 0;
		foreach (SmartfoxLobby.RoomLobbyInfo unfilteredRoom in instanceGame.GetUnfilteredRooms())
		{
			if (NetworkGame.GetJoinFlag(unfilteredRoom.options) == JOIN_FLAG.CROSS_DISABLE)
			{
				num++;
			}
		}
		if (num > m_roomList.Count)
		{
			return !instanceGame.isRestrictCrossMpByAdmin;
		}
		return false;
	}

	private void Resort(bool needScroll = true)
	{
		UpdateRoomUILinesFromRoomData();
		EnsureVisibleCurrentFocus();
	}

	private int CompareToSort(RoomData lhs, RoomData rhs)
	{
		if (ComparatorByRoomInfo(lhs, rhs, out var result))
		{
			return result;
		}
		foreach (SortDescriptor sortPriority in m_sortPriorities)
		{
			result = sortPriority.comparator(lhs, rhs, sortPriority.reversedProperty.value);
			if (result != 0)
			{
				return result;
			}
		}
		return result;
	}

	private void ClearGameList()
	{
		UpdateList(forceClearList: true, null);
	}

	private bool IsActiveFilter()
	{
		bool num = string.IsNullOrEmpty(m_searchByNameInputField.text);
		bool flag = m_trackNameOptions.Count > 0 && m_trackNameOptions[0] == m_prefs.globals.lobbyTrackNameSwitcherFilter;
		bool flag2 = m_lobbyFilterState != null && m_lobbyFilterState.IsDefaultState();
		if (num && flag)
		{
			return !flag2;
		}
		return true;
	}

	private void OnReceiveInputText(string text)
	{
		if (m_onJoinPassPressed)
		{
			m_onJoinPassPressed = false;
			if (!string.IsNullOrEmpty(text))
			{
				NetworkController.JoinByPass(NetworkController.DecorateRoomCode(text));
			}
		}
		else
		{
			GlobalModel globals = m_prefs.globals;
			string lobbyRoomNameInputFilter = (m_searchByNameInputField.text = text);
			globals.lobbyRoomNameInputFilter = lobbyRoomNameInputFilter;
			SetRoomsOld();
		}
	}

	public void OnJoinPress()
	{
		RoomData roomData = ((m_currentFocusedRoom != null) ? m_currentFocusedRoom.roomData : null);
		if (roomData == null)
		{
			return;
		}
		if (roomData.userCount >= roomData.roomOptions.MaxPlayers)
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.network_game_room_full.Localized());
			return;
		}
		if (roomData.isLocked)
		{
			OnJoinPassPress();
			return;
		}
		SmartfoxRoomOptions selectedRoomOptions = roomData.roomOptions;
		if (selectedRoomOptions.ModName != ModelValidator.modelName)
		{
			UIMessageBox.ShowHint("", ModelValidator.isValid ? LocalizationKeys.mod_wrong_version_info.Localized() : LocalizationKeys.mod_wrong_version_info2.Localized(), LocalizationKeys.mod_wrong_version.Localized());
			return;
		}
		if (selectedRoomOptions.ExternalTrackId == 0L)
		{
			CreateRoom(selectedRoomOptions);
			return;
		}
		if (UGCServiceProvider.instance.IsContentInstalled(selectedRoomOptions.ExternalTrackId))
		{
			CreateRoom(selectedRoomOptions);
			return;
		}
		UIMessageBox.ShowComplexDialog(LocalizationKeys.do_you_want_download_ugc_map.Localized(), null, LocalizationKeys.Cancel.Localized(), delegate
		{
			TryDownloadExternalTrack(selectedRoomOptions);
		}, LocalizationKeys.nav_download.Localized());
		void CreateRoom(SmartfoxRoomOptions roomOptions)
		{
			UILockInputContext.SetLock(base.owner);
			RoomSettings roomSetting = roomOptions;
			roomSetting.crossPlay = m_prefs.globals.crossPlatformPlay;
			m_prefs.globals.lastRoomSpawnPoint = roomOptions.SpawnPointIndex;
			m_prefs.globals.lastRoomAssist = roomOptions.AssistVersion;
			m_prefs.globals.lastRoomTractionControl = roomOptions.RoomTractionControlMode;
			NetworkController.CreateRoom(roomSetting);
		}
		void TryDownloadExternalTrack(SmartfoxRoomOptions smartfoxRoomOptions)
		{
			bool cancelRequested = false;
			IUGCManager.ServiceProvider ugcServiceId = (IUGCManager.ServiceProvider)smartfoxRoomOptions.ugcServiceId;
			if (ugcServiceId == IUGCManager.ServiceProvider.ModIO && ModIOUnity.IsModManagementBusy())
			{
				UIMessageBox.ShowWarning(LocalizationKeys.ugc_manager_is_not_ready.Localized(), delegate
				{
					cancelRequested = true;
				}, LocalizationKeys.Ok.Localized());
			}
			else
			{
				IUGCManager ugcManager = UGCServiceProvider.instance.GetManagerBy(IUGCManager.ContentType.Tracks, ugcServiceId);
				UIMessageBox.ShowProgressBar(LocalizationKeys.downloading_ugc_item.Localized(), delegate(Action<float> progressFill, System.Action closeProgressBar)
				{
					Action<bool, string> onFinished = delegate(bool success, string errorMessage)
					{
						closeProgressBar.SafeCall();
						if (!cancelRequested)
						{
							if (success)
							{
								CreateRoom(smartfoxRoomOptions);
							}
							else if (!string.IsNullOrEmpty(errorMessage))
							{
								UIMessageBox.ShowWarning(errorMessage, null, LocalizationKeys.Ok.Localized());
								m_hiddenExternalTracks.Add(smartfoxRoomOptions.ExternalTrackId);
							}
						}
					};
					ugcManager.RequestPackage(smartfoxRoomOptions.ExternalTrackId, progressFill, onFinished);
				}, LocalizationKeys.Cancel.Localized(), delegate
				{
					ugcManager.TryStopRequestedPackage(smartfoxRoomOptions.ExternalTrackId);
					cancelRequested = true;
				});
			}
		}
	}

	public void OnChangeServerRegionPress()
	{
		if (Overlay.instance.ConnectedToInternet())
		{
			UIChangeServerRegionContext.Show(base.owner);
		}
		else
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.no_server_connection.Localized(), 3f);
		}
	}

	public void OnJoinPassPress()
	{
		m_onJoinPassPressed = true;
		if (Overlay.instance is SteamOverlay steamOverlay && GameManager.IsLinuxOsPlatform())
		{
			steamOverlay.ShowOverlayKeyboard(LocalizationKeys.network_game_join_hint.Localized(), string.Empty, 20);
		}
		else
		{
			ShowInputDialog();
		}
		void ShowInputDialog()
		{
			UIMessageBox.ShowInputDialog(LocalizationKeys.network_game_join_hint.Localized(), string.Empty, LocalizationKeys.network_game_join_edit.Localized(), GameConsole.Bool["ChampionshipMode"] ? InputField.ContentType.Password : InputField.ContentType.IntegerNumber, null, LocalizationKeys.Cancel.Localized(), OnReceiveInputText, LocalizationKeys.network_game_btn_join.Localized());
		}
	}

	public void OnCreateRoomPress()
	{
		if (Overlay.instance.ConnectedToInternet())
		{
			UICreateRoomContext.Show(base.owner);
		}
		else
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.no_server_connection.Localized(), 3f);
		}
	}

	public void OnChangeVinylShowOption(bool next)
	{
		Array values = Enum.GetValues(typeof(GlobalModel.VinylsShowOptions));
		int num = Array.IndexOf(values, m_prefs.globals.vinylsShowOption);
		num = ((!next) ? (--num) : (++num));
		if (num > values.Length - 1)
		{
			num = 0;
		}
		if (num < 0)
		{
			num = values.Length - 1;
		}
		m_prefs.globals.vinylsShowOption = (GlobalModel.VinylsShowOptions)num;
		if (!enableCrossMP.value && m_prefs.globals.vinylsShowOption == GlobalModel.VinylsShowOptions.CurrentPlatform)
		{
			OnChangeVinylShowOption(next);
		}
		else
		{
			m_vinylOptionsCaption.value = GetLocalizedVinylOption();
		}
	}

	private string GetLocalizedVinylOption()
	{
		LocalizationKeys.Key key = null;
		switch (m_prefs.globals.vinylsShowOption)
		{
		case GlobalModel.VinylsShowOptions.NotShow:
			key = LocalizationKeys.show_vinyls_option_none;
			break;
		case GlobalModel.VinylsShowOptions.CurrentPlatform:
			key = LocalizationKeys.show_vinyls_option_current_platform;
			break;
		case GlobalModel.VinylsShowOptions.All:
			key = LocalizationKeys.show_vinyls_option_all;
			break;
		}
		if (key != null)
		{
			return key.Localized();
		}
		return string.Empty;
	}

	public void OnEnableCrossMpPress()
	{
		NetworkGame instanceGame = NetworkController.InstanceGame;
		if (instanceGame == null)
		{
			return;
		}
		if (instanceGame.isRestrictCrossMpByAdmin)
		{
			crossMpToggle.value = false;
			m_prefs.globals.crossPlatformPlay = false;
			instanceGame.ShowAdminRestrictAccessError(instanceGame.restrictCrossMpByAdminTimeEnd, LocalizationKeys.admin_restricted_cross_mp);
			return;
		}
		if (!crossMpToggle.value)
		{
			Overlay.instance.CheckSystemCrossPlatformAbility(delegate(bool restrictedCrossMp)
			{
				if (restrictedCrossMp)
				{
					Overlay.instance.ShowCrossPlayRestrictionDialog();
				}
				crossMpToggle.value = !restrictedCrossMp;
				m_prefs.globals.crossPlatformPlay = !restrictedCrossMp;
			});
		}
		else
		{
			crossMpToggle.value = false;
			m_prefs.globals.crossPlatformPlay = false;
		}
		SetRoomsOld();
	}

	private void OnReportRoomPress()
	{
		if (navigateMode == ENavMode.List)
		{
			if (m_currentFocusedRoom == null)
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.report_select_room.Localized(), 3f);
				return;
			}
			RoomData roomData = m_currentFocusedRoom.roomData;
			if (roomData == null)
			{
				return;
			}
			ObscuredString roomId = roomData.roomId;
			string roomCaption = roomData.roomCaption;
			int userCount = roomData.userCount;
			List<RoomData> roomList = m_roomList;
			int count = roomList.Count;
			int num = 0;
			foreach (RoomData item in roomList)
			{
				num += item.roomOptions.PlayerCount;
			}
			string value = string.Empty;
			foreach (RoomData item2 in roomList)
			{
				if (item2.roomId == roomId)
				{
					value = item2.roomOptions.RoomCreatorId;
					break;
				}
			}
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			dictionary.Add("roomName", roomCaption);
			dictionary.Add("roomSize", userCount.ToString());
			dictionary.Add("roomId", roomId);
			dictionary.Add("lobbyRoomsCount", count.ToString());
			dictionary.Add("lobbyPlayersCount", num.ToString());
			dictionary.Add("creatorId", value);
			UnSubscribePS4TextReceive();
			UnSubscribeSteamDeckTextReceive();
			m_reportContext.Show(base.owner, dictionary);
		}
		else
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.report_select_room.Localized(), 3f);
		}
	}

	public void OnEnableWorkshopPress()
	{
		workshopToggle.value = !workshopToggle.value;
		m_prefs.globals.workshopNetworkCars = workshopToggle.value;
	}

	public void OnClickSortByCaption()
	{
		ToggleSorter(ESortMode.Caption);
		Resort();
	}

	public void OnClickSortBySettings()
	{
		ToggleSorter(ESortMode.Settings);
		Resort();
	}

	public void OnClickSortByLocked()
	{
		ToggleSorter(ESortMode.Locked);
		Resort();
	}

	public void OnClickSortByPlayers()
	{
		ToggleSorter(ESortMode.Players);
		Resort();
	}

	public void OnClickSortByName()
	{
		ToggleSorter(ESortMode.Name);
		Resort();
	}

	private void InitiateSorter()
	{
		List<SortDescriptor> defaultSorter = GetDefaultSorter();
		string text = GameConsole.String["#network_lobby_sorter"].Trim();
		string[] array = text.Split(' ');
		if (string.IsNullOrEmpty(text) || !m_savedIndexHeaderSelection.HasValue)
		{
			m_sortPriorities = defaultSorter;
			return;
		}
		try
		{
			for (int i = 0; i < array.Length / 2; i++)
			{
				ESortMode mode = (ESortMode)int.Parse(array[2 * i]);
				bool value = int.Parse(array[2 * i + 1]) > 0;
				SortDescriptor sortDescriptor = defaultSorter.Find((SortDescriptor x) => x.mode == mode);
				if (sortDescriptor != null)
				{
					sortDescriptor.reversedProperty.value = value;
					m_sortPriorities.Add(sortDescriptor);
				}
			}
			if (m_sortPriorities.Count != defaultSorter.Count)
			{
				m_sortPriorities = defaultSorter;
			}
		}
		catch
		{
		}
	}

	private void SaveSorter()
	{
		string text = "";
		foreach (SortDescriptor sortPriority in m_sortPriorities)
		{
			string[] obj = new string[5] { text, null, null, null, null };
			int mode = (int)sortPriority.mode;
			obj[1] = mode.ToString();
			obj[2] = " ";
			obj[3] = (sortPriority.reversedProperty.value ? "1" : "0");
			obj[4] = " ";
			text = string.Concat(obj);
		}
		GameConsole.String["#network_lobby_sorter"] = text;
	}

	private List<SortDescriptor> GetDefaultSorter()
	{
		List<SortDescriptor> list = new List<SortDescriptor>();
		ESortMode[] array = (ESortMode[])Enum.GetValues(typeof(ESortMode));
		foreach (ESortMode mode in array)
		{
			list.Add(new SortDescriptor
			{
				mode = mode
			});
		}
		foreach (SortDescriptor item in list)
		{
			switch (item.mode)
			{
			case ESortMode.Locked:
				item.comparator = ComparatorByRoomLocked;
				item.reversedProperty = lockedReversed;
				continue;
			case ESortMode.Players:
				item.comparator = ComparatorByPlayerCount;
				item.reversedProperty = playersReversed;
				continue;
			case ESortMode.Caption:
				item.comparator = ComparatorByRoomCaption;
				item.reversedProperty = captionReversed;
				continue;
			case ESortMode.Name:
				item.comparator = ComparatorByRoomName;
				item.reversedProperty = nameReversed;
				continue;
			case ESortMode.Settings:
				item.comparator = ComparatorByRoomSettings;
				item.reversedProperty = settingsReversed;
				continue;
			}
			item.comparator = (RoomData roomData, RoomData j, bool k) => 0;
			item.reversedProperty = new Property<bool>(startValue: false);
		}
		return list;
	}

	private void SetVisibilitiArrow(int sort)
	{
		if (visibleArrow.value != sort)
		{
			visibleArrow.value = sort;
		}
	}

	private void ToggleSorter(ESortMode mode)
	{
		indexHeaderSelection.value = (int)mode;
		m_savedIndexHeaderSelection = indexHeaderSelection.value;
		SetFocusUI(null);
		navigateMode = ENavMode.Header;
		ResetFilterMode();
		SortDescriptor sortDescriptor = m_sortPriorities.Find((SortDescriptor x) => x.mode == mode);
		if (sortDescriptor != null)
		{
			if (m_sortPriorities[0] == sortDescriptor && !m_needSkipReserveArrows)
			{
				sortDescriptor.reversedProperty.value = !sortDescriptor.reversedProperty.value;
			}
			else
			{
				m_sortPriorities.Remove(sortDescriptor);
				m_sortPriorities.Insert(0, sortDescriptor);
			}
			m_needSkipReserveArrows = false;
			activeSorter.value = (int)m_sortPriorities[0].mode;
			SetVisibilitiArrow(indexHeaderSelection.value);
		}
	}

	private int ComparatorByRoomName(RoomData lhs, RoomData rhs, bool reversed)
	{
		int num = string.Compare(lhs.trackName, rhs.trackName, StringComparison.Ordinal);
		if (!reversed)
		{
			return -num;
		}
		return num;
	}

	private int ComparatorByRoomSettings(RoomData lhs, RoomData rhs, bool reversed)
	{
		int num = 0;
		if (lhs.flagsCount > rhs.flagsCount)
		{
			num = -1;
		}
		else if (lhs.flagsCount < rhs.flagsCount)
		{
			num = 1;
		}
		if (!reversed)
		{
			return num;
		}
		return -num;
	}

	private int ComparatorByRoomCaption(RoomData lhs, RoomData rhs, bool reversed)
	{
		int num = string.Compare(lhs.notFilteredCaption, rhs.notFilteredCaption, StringComparison.Ordinal);
		if (!reversed)
		{
			return -num;
		}
		return num;
	}

	private int ComparatorByRoomLocked(RoomData lhs, RoomData rhs, bool reversed)
	{
		int num = 0;
		if (lhs.isLocked && !rhs.isLocked)
		{
			num = 1;
		}
		else if (!lhs.isLocked && rhs.isLocked)
		{
			num = -1;
		}
		if (!reversed)
		{
			return num;
		}
		return -num;
	}

	private int ComparatorByPlayerCount(RoomData lhs, RoomData rhs, bool reversed)
	{
		int num = 0;
		if (lhs.roomOptions.PlayerCount > rhs.roomOptions.PlayerCount)
		{
			num = -1;
		}
		else if (lhs.roomOptions.PlayerCount < rhs.roomOptions.PlayerCount)
		{
			num = 1;
		}
		if (!reversed)
		{
			return num;
		}
		return -num;
	}

	private bool ComparatorByRoomInfo(RoomData lhs, RoomData rhs, out int result)
	{
		result = 0;
		bool flag = lhs.roomOptions == null;
		bool flag2 = rhs.roomOptions == null;
		if (flag && !flag2)
		{
			result = -1;
			return true;
		}
		if (!flag && flag2)
		{
			result = 1;
			return true;
		}
		if (flag && flag2)
		{
			result = 0;
			return true;
		}
		return false;
	}

	[GameHotkey(ActionType.NavUp, false, true)]
	public void NavigationUp()
	{
		switch (navigateMode)
		{
		case ENavMode.List:
			if (!MoveRoomFocus(-1))
			{
				indexHeaderSelection.value = 1;
				navigateMode = ENavMode.Header;
				SetFocusUI(null);
			}
			break;
		case ENavMode.Filters:
			if (!m_activeRoomNameInput.value)
			{
				base.selectedNavigate.Move(NavigateDirection.Up);
				ShowTrackSwitcherHotkeysIfNeed();
			}
			break;
		case ENavMode.Header:
			break;
		}
	}

	[GameHotkey(ActionType.NavDown, false, true)]
	public void NavigationDown()
	{
		switch (navigateMode)
		{
		case ENavMode.List:
			MoveRoomFocus(1);
			break;
		case ENavMode.Header:
			if (m_roomList.Count != 0)
			{
				indexHeaderSelection.value = -1;
				SetFocusToFirst();
				navigateMode = ENavMode.List;
			}
			break;
		case ENavMode.Filters:
			if (!m_activeRoomNameInput.value)
			{
				ElementNavigate elementNavigate = base.selectedNavigate;
				base.selectedNavigate.Move(NavigateDirection.Down);
				if (!base.selectedNavigate.isActiveAndEnabled)
				{
					base.selectedItem = elementNavigate.gameObject;
				}
				ShowTrackSwitcherHotkeysIfNeed();
			}
			break;
		}
	}

	private bool MoveRoomFocus(int next)
	{
		if (m_currentFocusedRoom == null)
		{
			return SetFocusToFirst();
		}
		int num = m_roomLinesPool.IndexOf(m_currentFocusedRoom);
		if (num == -1)
		{
			return false;
		}
		num += next;
		if (num >= 0 && num < m_roomLinesPool.Count)
		{
			RoomUILine roomUILine = m_roomLinesPool[num];
			if (roomUILine != null && roomUILine.gameObject.activeInHierarchy)
			{
				SetFocusUI(roomUILine);
				EnsureVisibleCurrentFocus();
				return true;
			}
		}
		return false;
	}

	private bool SetFocusToFirst()
	{
		if (m_roomList.Count > 0)
		{
			return SetFocusUI(FindRoomUI(m_roomList[0]));
		}
		return false;
	}

	private void ShowTrackSwitcherHotkeysIfNeed()
	{
		bool flag = GameInput.InputManager.instance.lastController.type == ControllerType.Joystick;
		m_showGamePadHotkeysTracks.value = base.selectedItem != null && base.selectedItem.GetComponent<ElementNavigate>() == m_trackSwitcher && flag;
		m_showGamePadHotkeysAdh.value = base.selectedItem != null && base.selectedItem.GetComponent<ElementNavigate>() == m_adhLimitFilter && flag;
		m_showGamePadHotkeysVinyls.value = base.selectedItem != null && base.selectedItem.GetComponent<ElementNavigate>() == m_vinylsVisibility && flag;
		m_showGamePadHotkeysPowerDensity.value = base.selectedItem != null && base.selectedItem.GetComponent<ElementNavigate>() == m_powerDensityLimitFilter && flag;
		m_showGamePadHotkeysRoomType.value = base.selectedItem != null && base.selectedItem.GetComponent<ElementNavigate>() == m_roomType && flag;
	}

	[GameHotkey(ActionType.NavLeft, false, true)]
	public void NavigationLeft()
	{
		switch (navigateMode)
		{
		case ENavMode.Header:
			indexHeaderSelection.value--;
			if (indexHeaderSelection.value <= 0)
			{
				indexHeaderSelection.value = 1;
			}
			break;
		case ENavMode.Filters:
		{
			if (m_activeRoomNameInput.value)
			{
				break;
			}
			ControllerType type = GameInput.InputManager.instance.lastController.type;
			if (base.selectedItem.GetComponent<ElementNavigate>() == m_trackSwitcher && type != ControllerType.Joystick)
			{
				SetNextTrackOption(next: false);
				break;
			}
			if (base.selectedItem.GetComponent<ElementNavigate>() == m_vinylsVisibility && type != ControllerType.Joystick)
			{
				OnChangeVinylShowOption(next: false);
				break;
			}
			if (base.selectedItem.GetComponent<ElementNavigate>() == m_adhLimitFilter && type != ControllerType.Joystick)
			{
				SetNextAdhLimitOption(next: false);
				break;
			}
			if (base.selectedItem.GetComponent<ElementNavigate>() == m_powerDensityLimitFilter && type != ControllerType.Joystick)
			{
				SetNextPowerDensityOption(next: false);
				break;
			}
			if (base.selectedItem.GetComponent<ElementNavigate>() == m_roomType && type != ControllerType.Joystick)
			{
				SetNextGameRoomTypeOption(next: false);
				break;
			}
			ElementNavigate elementNavigate = base.selectedNavigate;
			base.selectedNavigate.Move(NavigateDirection.Left);
			if (elementNavigate == base.selectedNavigate)
			{
				navigateMode = ENavMode.List;
				ResetFilterMode();
				if (m_lastSelectedRoom != null && FindRoomData(m_lastSelectedRoom) != null)
				{
					SetFocus(m_lastSelectedRoom);
				}
				else
				{
					SetFocusUI(FindRoomUI(FindFirstRoomData()));
				}
				m_lastSelectedRoom = null;
			}
			ShowTrackSwitcherHotkeysIfNeed();
			break;
		}
		case ENavMode.List:
			break;
		}
	}

	[GameHotkey(ActionType.NavRight, false, true)]
	public void NavigationRight()
	{
		switch (navigateMode)
		{
		case ENavMode.List:
			m_lastSelectedRoom = SaveCurrentFocus();
			SetFocusUI(null);
			if (base.navElements != null && base.navElements.Count > 0)
			{
				base.selectedItem = (m_trackSharingSupported.value ? base.navElements[0].gameObject : base.navElements[1].gameObject);
			}
			navigateMode = ENavMode.Filters;
			break;
		case ENavMode.Header:
			indexHeaderSelection.value++;
			if (indexHeaderSelection.value > 4)
			{
				indexHeaderSelection.value = -1;
				if (base.navElements != null && base.navElements.Count > 0)
				{
					base.selectedItem = (m_trackSharingSupported.value ? base.navElements[0].gameObject : base.navElements[1].gameObject);
				}
				navigateMode = ENavMode.Filters;
			}
			break;
		case ENavMode.Filters:
			if (!m_activeRoomNameInput.value)
			{
				ControllerType type = GameInput.InputManager.instance.lastController.type;
				if (base.selectedItem.GetComponent<ElementNavigate>() == m_trackSwitcher && type != ControllerType.Joystick)
				{
					SetNextTrackOption(next: true);
				}
				else if (base.selectedItem.GetComponent<ElementNavigate>() == m_vinylsVisibility && type != ControllerType.Joystick)
				{
					OnChangeVinylShowOption(next: true);
				}
				else if (base.selectedItem.GetComponent<ElementNavigate>() == m_adhLimitFilter && type != ControllerType.Joystick)
				{
					SetNextAdhLimitOption(next: true);
				}
				else if (base.selectedItem.GetComponent<ElementNavigate>() == m_powerDensityLimitFilter && type != ControllerType.Joystick)
				{
					SetNextPowerDensityOption(next: true);
				}
				else if (base.selectedItem.GetComponent<ElementNavigate>() == m_roomType && type != ControllerType.Joystick)
				{
					SetNextGameRoomTypeOption(next: true);
				}
				else
				{
					base.selectedNavigate.Move(NavigateDirection.Right);
				}
			}
			break;
		}
	}

	public void NavigationSubmit()
	{
		switch (navigateMode)
		{
		case ENavMode.List:
			OnJoinPress();
			break;
		case ENavMode.Header:
			SubmitSort(indexHeaderSelection.value);
			break;
		case ENavMode.Filters:
		{
			m_navigationSubmitActionCalled = true;
			Toggle component = base.selectedNavigate.gameObject.GetComponent<Toggle>();
			if ((bool)component)
			{
				component.isOn = !component.isOn;
				break;
			}
			Button component2 = base.selectedNavigate.gameObject.GetComponent<Button>();
			if ((bool)component2)
			{
				component2.onClick.Invoke();
			}
			else if ((bool)base.selectedNavigate.gameObject.GetComponent<TMP_InputField>())
			{
				if (Overlay.instance is SteamOverlay steamOverlay && GameManager.IsLinuxOsPlatform())
				{
					steamOverlay.ShowOverlayKeyboard(LocalizationKeys.network_room_change_name_title.Localized(), m_searchByNameInputField.text, 40);
				}
				else
				{
					ActivateInputIfNeed();
				}
			}
			break;
		}
		}
	}

	private void OnEndEditInputHandler(string s)
	{
		ReleaseInputIfNeed();
		SetRoomsOld();
	}

	private void OnValueChangedHandler(string input)
	{
		bool flag = string.IsNullOrEmpty(input);
		m_roomNameActiveFilter.value = !flag;
		m_friendsFilterActive.value = flag && IsFilterEnabled(FilterButtonType.Friends);
		m_prefs.globals.lobbyRoomNameInputFilter = input;
		SetRoomsOld();
	}

	private void ReleaseInputIfNeed()
	{
		if (m_activeRoomNameInput.value)
		{
			m_onEndEditTime = Time.realtimeSinceStartup;
			GameInput.InputManager.ResetLockInput(this);
			m_activeRoomNameInput.value = false;
			m_searchByNameInputField.DeactivateInputField();
			m_prefs.globals.lobbyRoomNameInputFilter = m_searchByNameInputField.text;
		}
	}

	private void ActivateInputIfNeed()
	{
		if (!m_activeRoomNameInput.value && !NeedSkipInputFilterPress())
		{
			m_activeRoomNameInput.value = true;
			GameInput.InputManager.LockInput(this);
			m_searchByNameInputField.ActivateInputField();
		}
	}

	private bool NeedSkipInputFilterPress()
	{
		return Time.realtimeSinceStartup - m_onEndEditTime < 0.5f;
	}

	private void ShowConsoleInputDialog()
	{
	}

	public void NavigationCancel()
	{
		if (!NeedSkipInputFilterPress())
		{
			GarageGUIState.ExitToGarage();
		}
	}

	private void ResetFilterMode()
	{
		base.selectedItem = null;
		ReleaseInputIfNeed();
	}

	public void OnClick(ElementNavigate element)
	{
		if (m_navigationSubmitActionCalled)
		{
			m_navigationSubmitActionCalled = false;
			return;
		}
		if (base.selectedItem != element.gameObject)
		{
			base.selectedItem = element.gameObject;
		}
		ReleaseInputIfNeed();
		if (indexHeaderSelection.value >= 0)
		{
			indexHeaderSelection.value = -1;
		}
		m_lastSelectedRoom = SaveCurrentFocus();
		SetFocusUI(null);
		navigateMode = ENavMode.Filters;
		if (element.gameObject.GetComponent<TMP_InputField>() != null)
		{
			if (Overlay.instance is SteamOverlay steamOverlay && GameManager.IsLinuxOsPlatform())
			{
				steamOverlay.ShowOverlayKeyboard(LocalizationKeys.network_room_change_name_title.Localized(), m_searchByNameInputField.text, 40);
			}
			else
			{
				ActivateInputIfNeed();
			}
		}
	}

	public void NavigationJoinByPass()
	{
		OnJoinPassPress();
	}

	public void NavigationChangeServerRegion()
	{
		OnChangeServerRegionPress();
	}

	private void NavigationReportRoom()
	{
		OnReportRoomPress();
	}

	public void NavigationCreateRoom()
	{
		if (!m_activeRoomNameInput.value)
		{
			OnCreateRoomPress();
		}
	}

	public void NavigationEnableCrossMP()
	{
		OnEnableCrossMpPress();
	}

	public void NavigationEnableWorkshop()
	{
		OnEnableWorkshopPress();
	}

	private void SubmitSort(int index)
	{
		if (index >= 0 && index < Enum.GetValues(typeof(ESortMode)).Length)
		{
			ToggleSorter((ESortMode)index);
			Resort();
		}
	}
}
