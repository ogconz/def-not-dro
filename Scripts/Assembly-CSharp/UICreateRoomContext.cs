using System;
using System.Collections.Generic;
using Binding;
using Binding.Base;
using CodeStage.AntiCheat.ObscuredTypes;
using DB;
using DB.Meta;
using DI;
using GameInput;
using GameOverlay;
using Services.UGC.Interfaces;
using Services.UGC.Utils;
using SyncMultiplayer;
using TrackLayout.Data;
using UnityEngine;

[RaycastBlocker(true)]
[GameHotkeyBlocker(true)]
[AudioFXBlocker(true)]
public class UICreateRoomContext : BaseBindingBehaviourTarget
{
	public struct NetworkMapItem
	{
		public string name;

		public string icon;

		public int spawnPoint;

		public int id;

		public DLCCode dlcCode;

		public bool isExternal;

		public override string ToString()
		{
			return name;
		}
	}

	public struct NetworkLightItem
	{
		public string name;

		public string target;

		public override string ToString()
		{
			return name;
		}
	}

	private enum SwitcherMode
	{
		Tracks = 0,
		WorkshopTracks = 1
	}

	private const float CreateRoomCooldownSec = 10f;

	[Dependency]
	protected GamePrefs m_prefs;

	private BaseModel m_model;

	private List<DB.Meta.Track> m_tracks;

	private List<DB.Meta.Track> m_externalTracks = new List<DB.Meta.Track>();

	private List<DB.Meta.Track> m_normalTracks = new List<DB.Meta.Track>();

	public CreateRoomParamsList paramsList;

	public readonly Property<Sprite> trackIcon = new SpriteProperty();

	public readonly Property<string> roomName = new StringProperty();

	public readonly Property<string> dlcName = new StringProperty();

	public readonly Property<bool> isLockedDLC = new BoolProperty();

	private readonly BoolProperty m_versionVisibility = new BoolProperty();

	private readonly StringProperty m_version = new StringProperty();

	private readonly BoolProperty m_showUgcService = new BoolProperty();

	private readonly IntProperty m_ugcServiceId = new IntProperty();

	private readonly StringProperty m_ugcServiceTitle = new StringProperty();

	private BoolProperty m_showMapDescription = new BoolProperty();

	private StringProperty m_mapDescription = new StringProperty(string.Empty);

	private SpriteProperty m_mapImage = new SpriteProperty();

	private ObscuredString m_passwordRoom = "";

	private BoolProperty m_trackAnimation = new BoolProperty();

	public Sprite driftIcon;

	public Sprite nightModeIcon;

	public Sprite passwordIcon;

	public Sprite crossPlayIcon;

	public Sprite lightModeIcon;

	public Sprite spawnPointIcon;

	public Sprite assistIcon;

	public Sprite tractionControlIcon;

	public Sprite nightModeInactiveIcon;

	public Sprite passwordInactiveIcon;

	public Sprite crossPlayInactiveIcon;

	public Sprite assistInactiveIcon;

	public Sprite spawnPointInactiveIcon;

	public Sprite tractionControlInactiveIcon;

	public RoomData.AdhIconPair[] adhIcons;

	public RoomData.RaceTypePair[] raceTypeIcons;

	public RoomData.PowerDensityPair[] powerDensityIcons;

	public RoomData.WheelDriveTypePair[] wheelDriveIcons;

	public RoomData.CollisionTypePair[] collisionIcons;

	private SwitchListSettingsItem m_trackSwitcher;

	private ToggleListSettingsItem m_nightMode;

	private SwitchListSettingsItem m_raceType;

	private SwitchListSettingsItem m_lightSwitcher;

	private ToggleListSettingsItem m_crossPlay;

	private SwitchListSettingsItem m_passwordMode;

	private SwitchListSettingsItem m_spawnPoints;

	private SwitchListSettingsItem m_adhLimit;

	private SwitchListSettingsItem m_assist;

	private SwitchListSettingsItem m_powerDendityLimit;

	private SwitchListSettingsItem m_tractionControl;

	private SwitchListSettingsItem m_wheelDriveType;

	private SwitchListSettingsItem m_collisionType;

	private NetworkMapItem m_selectedNetworkMap;

	private bool m_prevTrackIsExternal;

	private Texture2D m_externalTrackTexture;

	public string trackIconLastPath;

	private IUGCManager m_ugcManager;

	[SerializeField]
	private SpawnpointElementList m_spawnpointElementList;

	private SwitcherMode m_switcherModeType;

	private readonly IntProperty m_switcherMode = new IntProperty();

	private readonly BoolProperty m_showSwitcher = new BoolProperty();

	private int m_lastExternalTrackId;

	private int m_lastTrackId;

	private SmartfoxLobby m_sfxLobby;

	private static float s_lastTimeCooldownActivated;

	private string DEFAULT_ROOM_NAME => LocalizationKeys.network_game_servername_default.Localized();

	private void Awake()
	{
		m_model = DependencyInjector.Resolve<BaseModel>();
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		m_switcherModeType = SwitcherMode.Tracks;
		m_switcherMode.value = 0;
		m_lastExternalTrackId = m_prefs.globals.lastExternalRoomTrack;
		m_lastTrackId = m_prefs.globals.lastRoomTrack;
		m_sfxLobby = NetworkController.InstanceGame.lobby;
		if (m_sfxLobby != null)
		{
			m_sfxLobby.CreateRoomError += OnCreateRoomErrorHandler;
		}
		m_showSwitcher.value = false;
		if (!base.owner.IsActive<UIBackgroundContext>())
		{
			base.owner.Add<UIBackgroundContext>();
		}
		BringForwardByDepth();
		m_tracks = m_model.QueryAll<DB.Meta.Track>();
		RebuildTrackLists();
		roomName.value = m_prefs.globals.lastRoomName;
		if (string.IsNullOrEmpty(roomName.value))
		{
			roomName.value = DEFAULT_ROOM_NAME;
		}
		RefreshRoomParams();
		m_showSwitcher.value = m_externalTracks.Count > 0 && !Overlay.instance.IsUserGeneratedContentRestricted;
		void RebuildTrackLists()
		{
			m_externalTracks.Clear();
			m_normalTracks.Clear();
			foreach (DB.Meta.Track track in m_tracks)
			{
				if (track.external)
				{
					m_externalTracks.Add(track);
				}
				else
				{
					m_normalTracks.Add(track);
				}
			}
		}
	}

	private void OnCreateRoomErrorHandler(Error.EType errorType, string message)
	{
		TryApplyCooldownBy(errorType);
	}

	private void TryApplyCooldownBy(Error.EType errorType)
	{
		if ((uint)(errorType - 19) <= 2u)
		{
			s_lastTimeCooldownActivated = Time.time;
		}
	}

	private void RefreshRoomParams()
	{
		paramsList.Clear();
		if (m_trackSwitcher == null)
		{
			m_trackSwitcher = new SwitchListSettingsItem
			{
				title = LocalizationKeys.network_game_params_track.Localized()
			};
		}
		else
		{
			m_trackSwitcher.list.Clear();
		}
		SwitchListSettingsItem trackSwitcher = m_trackSwitcher;
		trackSwitcher.onChangedValue = (System.Action)Delegate.Remove(trackSwitcher.onChangedValue, new System.Action(TrackSwitcherChangedHandler));
		SwitchListSettingsItem trackSwitcher2 = m_trackSwitcher;
		trackSwitcher2.onChangedValue = (System.Action)Delegate.Combine(trackSwitcher2.onChangedValue, new System.Action(TrackSwitcherChangedHandler));
		int lastIdTrack = 0;
		switch (m_switcherModeType)
		{
		case SwitcherMode.Tracks:
			lastIdTrack = m_lastTrackId;
			FillMapSwitcher(m_normalTracks);
			break;
		case SwitcherMode.WorkshopTracks:
			lastIdTrack = m_lastExternalTrackId;
			FillMapSwitcher(m_externalTracks);
			break;
		}
		paramsList.AddItem(m_trackSwitcher);
		int num = m_trackSwitcher.list.FindIndex((object x) => ((NetworkMapItem)x).id == lastIdTrack);
		m_trackSwitcher.index = ((num != -1) ? num : 0);
		m_trackSwitcher.OnChangeValue();
		if (m_adhLimit == null)
		{
			m_adhLimit = new SwitchListSettingsItem
			{
				title = LocalizationKeys.network_game_params_adh_limit.Localized()
			};
		}
		else
		{
			m_adhLimit.list.Clear();
		}
		SwitchListSettingsItem adhLimit = m_adhLimit;
		adhLimit.onChangedValue = (System.Action)Delegate.Remove(adhLimit.onChangedValue, new System.Action(AdhLimitChangedHandler));
		SwitchListSettingsItem adhLimit2 = m_adhLimit;
		adhLimit2.onChangedValue = (System.Action)Delegate.Combine(adhLimit2.onChangedValue, new System.Action(AdhLimitChangedHandler));
		m_adhLimit.title = LocalizationKeys.network_game_params_adh_limit.Localized();
		m_adhLimit.list.Add(RoomData.AdhLimitToString(RoomData.AdhLimitFlags.NONE));
		m_adhLimit.list.Add(RoomData.AdhLimitToString(RoomData.AdhLimitFlags.ADH_90));
		m_adhLimit.list.Add(RoomData.AdhLimitToString(RoomData.AdhLimitFlags.ADH_100));
		m_adhLimit.list.Add(RoomData.AdhLimitToString(RoomData.AdhLimitFlags.ADH_110));
		m_adhLimit.list.Add(RoomData.AdhLimitToString(RoomData.AdhLimitFlags.ADH_120));
		m_adhLimit.index = m_prefs.globals.lastRoomAdhLimitIndex;
		m_adhLimit.OnChangeValue();
		paramsList.AddItem(m_adhLimit);
		if (m_powerDendityLimit == null)
		{
			m_powerDendityLimit = new SwitchListSettingsItem
			{
				title = LocalizationKeys.lobby_filter_power_density_limitation.Localized()
			};
		}
		else
		{
			m_powerDendityLimit.list.Clear();
		}
		SwitchListSettingsItem powerDendityLimit = m_powerDendityLimit;
		powerDendityLimit.onChangedValue = (System.Action)Delegate.Remove(powerDendityLimit.onChangedValue, new System.Action(PowerDensityChangedHandler));
		SwitchListSettingsItem powerDendityLimit2 = m_powerDendityLimit;
		powerDendityLimit2.onChangedValue = (System.Action)Delegate.Combine(powerDendityLimit2.onChangedValue, new System.Action(PowerDensityChangedHandler));
		m_powerDendityLimit.title = LocalizationKeys.lobby_filter_power_density_limitation.Localized();
		m_powerDendityLimit.list.Add(RoomData.PowerDensityLimitToString(RoomData.PowerDensityLimitFlag.NONE));
		m_powerDendityLimit.list.Add(RoomData.PowerDensityLimitToString(RoomData.PowerDensityLimitFlag.PD_0_2));
		m_powerDendityLimit.list.Add(RoomData.PowerDensityLimitToString(RoomData.PowerDensityLimitFlag.PD_0_3));
		m_powerDendityLimit.list.Add(RoomData.PowerDensityLimitToString(RoomData.PowerDensityLimitFlag.PD_0_4));
		m_powerDendityLimit.list.Add(RoomData.PowerDensityLimitToString(RoomData.PowerDensityLimitFlag.PD_0_5));
		m_powerDendityLimit.list.Add(RoomData.PowerDensityLimitToString(RoomData.PowerDensityLimitFlag.PD_0_6));
		m_powerDendityLimit.list.Add(RoomData.PowerDensityLimitToString(RoomData.PowerDensityLimitFlag.PD_0_7));
		m_powerDendityLimit.list.Add(RoomData.PowerDensityLimitToString(RoomData.PowerDensityLimitFlag.PD_0_8));
		m_powerDendityLimit.list.Add(RoomData.PowerDensityLimitToString(RoomData.PowerDensityLimitFlag.PD_0_9));
		m_powerDendityLimit.list.Add(RoomData.PowerDensityLimitToString(RoomData.PowerDensityLimitFlag.PD_1_0));
		m_powerDendityLimit.index = m_prefs.globals.lastRoomPowerDensityLimit;
		m_powerDendityLimit.OnChangeValue();
		paramsList.AddItem(m_powerDendityLimit);
		if (m_raceType == null)
		{
			m_raceType = new SwitchListSettingsItem
			{
				title = LocalizationKeys.network_game_room_type.Localized(),
				sprite = driftIcon
			};
		}
		else
		{
			m_raceType.list.Clear();
		}
		SwitchListSettingsItem raceType = m_raceType;
		raceType.onChangedValue = (System.Action)Delegate.Remove(raceType.onChangedValue, new System.Action(RaceTypeChanchedHandler));
		SwitchListSettingsItem raceType2 = m_raceType;
		raceType2.onChangedValue = (System.Action)Delegate.Combine(raceType2.onChangedValue, new System.Action(RaceTypeChanchedHandler));
		m_raceType.title = LocalizationKeys.network_game_room_type.Localized();
		m_raceType.list.Add(LocalizationKeys.network_game_params_drift_mode.Localized());
		m_raceType.list.Add(LocalizationKeys.network_game_params_timeattack_mode.Localized());
		m_raceType.index = m_prefs.globals.lastTrackType;
		m_raceType.OnChangeValue();
		paramsList.AddItem(m_raceType);
		if (m_wheelDriveType == null)
		{
			m_wheelDriveType = new SwitchListSettingsItem
			{
				title = LocalizationKeys.network_game_params_drivetrain_type.Localized()
			};
		}
		else
		{
			m_wheelDriveType.list.Clear();
		}
		m_wheelDriveType.title = LocalizationKeys.carProfileParameterCaptionDyno.FormatLocalized("drivetrain_type");
		m_wheelDriveType.list.Add(LocalizationKeys.network_game_params_adh_all.Localized());
		m_wheelDriveType.list.Add("RWD");
		m_wheelDriveType.list.Add("AWD");
		m_wheelDriveType.index = m_prefs.globals.lastRoomWheelDriveType;
		SwitchListSettingsItem wheelDriveType = m_wheelDriveType;
		wheelDriveType.onChangedValue = (System.Action)Delegate.Remove(wheelDriveType.onChangedValue, new System.Action(DriveTypeChangeHandler));
		SwitchListSettingsItem wheelDriveType2 = m_wheelDriveType;
		wheelDriveType2.onChangedValue = (System.Action)Delegate.Combine(wheelDriveType2.onChangedValue, new System.Action(DriveTypeChangeHandler));
		m_wheelDriveType.OnChangeValue();
		paramsList.AddItem(m_wheelDriveType);
		if (m_tractionControl == null)
		{
			m_tractionControl = new SwitchListSettingsItem
			{
				title = LocalizationKeys.network_game_params_track.Localized()
			};
		}
		else
		{
			m_tractionControl.list.Clear();
		}
		m_tractionControl.title = LocalizationKeys.netwotk_game_params_traction_control.Localized();
		m_tractionControl.list.Add(LocalizationKeys.network_game_params_adh_all.Localized());
		m_tractionControl.list.Add(LocalizationKeys.control_off.Localized());
		m_tractionControl.index = m_prefs.globals.lastRoomTractionControl;
		SwitchListSettingsItem tractionControl = m_tractionControl;
		tractionControl.onChangedValue = (System.Action)Delegate.Remove(tractionControl.onChangedValue, new System.Action(TractionControlChangeHandler));
		SwitchListSettingsItem tractionControl2 = m_tractionControl;
		tractionControl2.onChangedValue = (System.Action)Delegate.Combine(tractionControl2.onChangedValue, new System.Action(TractionControlChangeHandler));
		m_tractionControl.OnChangeValue();
		paramsList.AddItem(m_tractionControl);
		if (m_collisionType == null)
		{
			m_collisionType = new SwitchListSettingsItem();
		}
		else
		{
			m_collisionType.list.Clear();
		}
		m_collisionType.title = LocalizationKeys.network_game_params_collisions_enable.Localized();
		m_collisionType.list.Add(LocalizationKeys.control_off.Localized());
		m_collisionType.list.Add(LocalizationKeys.сollisions_classic.Localized());
		m_collisionType.list.Add(LocalizationKeys.collisions_dynamic.Localized());
		m_collisionType.index = m_prefs.globals.lastRoomCollisionType;
		SwitchListSettingsItem collisionType = m_collisionType;
		collisionType.onChangedValue = (System.Action)Delegate.Remove(collisionType.onChangedValue, new System.Action(CollisionTypeChangeHandler));
		SwitchListSettingsItem collisionType2 = m_collisionType;
		collisionType2.onChangedValue = (System.Action)Delegate.Combine(collisionType2.onChangedValue, new System.Action(CollisionTypeChangeHandler));
		paramsList.AddItem(m_collisionType);
		m_collisionType.OnChangeValue();
		GetOrCreateToggle(ref m_nightMode, NightModeChangedHandler);
		m_nightMode.value = m_prefs.globals.lastRoomNight && GameManager.NightModeInstalled;
		m_nightMode.sprite = nightModeIcon;
		m_nightMode.title = LocalizationKeys.network_game_params_night_mode.Localized();
		paramsList.AddItem(m_nightMode);
		m_nightMode.OnChangeValue();
		if (m_lightSwitcher == null)
		{
			m_lightSwitcher = new SwitchListSettingsItem
			{
				title = LocalizationKeys.network_game_params_light_mode.Localized(),
				sprite = lightModeIcon
			};
		}
		else
		{
			m_lightSwitcher.list.Clear();
		}
		FillLightSwitcher();
		paramsList.AddItem(m_lightSwitcher);
		if (m_lightSwitcher.onChangedValue == null)
		{
			SwitchListSettingsItem lightSwitcher = m_lightSwitcher;
			lightSwitcher.onChangedValue = (System.Action)Delegate.Combine(lightSwitcher.onChangedValue, new System.Action(LightSwitcherChangedHandler));
		}
		m_lightSwitcher.OnChangeValue();
		if (m_spawnPoints == null)
		{
			m_spawnPoints = new SwitchListSettingsItem
			{
				title = LocalizationKeys.network_game_params_spawnpoints.Localized()
			};
		}
		else
		{
			m_spawnPoints.list.Clear();
		}
		m_spawnPoints.title = LocalizationKeys.network_game_params_spawnpoints.Localized();
		if (m_spawnPoints.onChangedValue == null)
		{
			SwitchListSettingsItem spawnPoints = m_spawnPoints;
			spawnPoints.onChangedValue = (System.Action)Delegate.Combine(spawnPoints.onChangedValue, new System.Action(SpawnPointChangedHandler));
		}
		m_spawnPoints.OnChangeValue();
		paramsList.AddItem(m_spawnPoints);
		if (m_assist == null)
		{
			m_assist = new SwitchListSettingsItem
			{
				title = LocalizationKeys.network_game_params_track.Localized()
			};
		}
		else
		{
			m_assist.list.Clear();
		}
		m_assist.title = LocalizationKeys.network_game_params_assist.Localized();
		m_assist.list.Add(LocalizationKeys.network_game_params_adh_all.Localized());
		m_assist.list.Add(LocalizationKeys.network_game_params_without_assist.Localized());
		m_assist.index = m_prefs.globals.lastRoomAssist;
		if (m_assist.onChangedValue == null)
		{
			SwitchListSettingsItem assist = m_assist;
			assist.onChangedValue = (System.Action)Delegate.Combine(assist.onChangedValue, new System.Action(AssistChangedHandler));
		}
		m_assist.OnChangeValue();
		paramsList.AddItem(m_assist);
		bool isRestrictCrossMpByAdmin = NetworkController.InstanceGame.isRestrictCrossMpByAdmin;
		GetOrCreateToggle(ref m_crossPlay, CrossPlayChangedHandler);
		m_crossPlay.title = LocalizationKeys.settings_game_cross_platform_play.Localized();
		if (m_prefs.globals.crossPlatformPlay)
		{
			m_crossPlay.value = !isRestrictCrossMpByAdmin && m_prefs.globals.lastRoomCrossPlay;
		}
		else
		{
			m_crossPlay.value = false;
		}
		m_crossPlay.sprite = (m_crossPlay.value ? crossPlayIcon : crossPlayInactiveIcon);
		ToggleListSettingsItem crossPlay = m_crossPlay;
		crossPlay.onChangedValue = (System.Action)Delegate.Combine(crossPlay.onChangedValue, new System.Action(CrossPlayChangedHandler));
		paramsList.AddItem(m_crossPlay);
		m_crossPlay.SetEditingPermission(!isRestrictCrossMpByAdmin);
		m_crossPlay.OnChangeValue();
		if (!m_prefs.globals.crossPlatformPlay)
		{
			m_crossPlay.SetEditingPermission(isPermitted: false);
		}
		if (m_passwordMode == null)
		{
			m_passwordMode = new SwitchListSettingsItem
			{
				title = LocalizationKeys.network_game_params_track.Localized()
			};
		}
		else
		{
			m_passwordMode.list.Clear();
		}
		m_passwordMode.title = LocalizationKeys.network_game_params_password.Localized();
		m_passwordMode.list.Add(LocalizationKeys.control_off.Localized());
		m_passwordMode.list.Add(LocalizationKeys.control_on.Localized());
		m_passwordMode.index = (m_prefs.globals.lastRoomLocked ? 1 : 0);
		if (m_passwordMode.onChangedValue == null)
		{
			SwitchListSettingsItem passwordMode = m_passwordMode;
			passwordMode.onChangedValue = (System.Action)Delegate.Combine(passwordMode.onChangedValue, new System.Action(PasswordModeChangedHandler));
		}
		m_passwordMode.OnChangeValue();
		paramsList.AddItem(m_passwordMode);
		paramsList.RecalcList();
		paramsList.selectedIndex = 0;
		if (Overlay.instance is SteamOverlay steamOverlay && GameManager.IsLinuxOsPlatform())
		{
			steamOverlay.onCloseOverlayKeyboard = (Action<string>)Delegate.Combine(steamOverlay.onCloseOverlayKeyboard, new Action<string>(OnReceiveInputText));
		}
		ShowBottomPanel(new ActionHintData[2]
		{
			new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_goto_back.Localized(),
				type = ActionType.NavCancelBtnB,
				action = Cancel
			},
			new ActionHintData
			{
				hint = LocalizationKeys.network_game_btn_create.Localized(),
				type = ActionType.NavSelectBtnA,
				action = CreateRoom
			}
		});
		TrackSwitcherChangedHandler();
		m_spawnpointElementList.Select(0);
	}

	public void OnSwitchMode(bool next)
	{
		int switcherModeType = (int)m_switcherModeType;
		int num = (next ? 1 : (-1));
		switcherModeType += num;
		int length = Enum.GetValues(typeof(SwitcherMode)).Length;
		if (switcherModeType >= 0 && switcherModeType <= length - 1)
		{
			SetSwitcherMode(switcherModeType);
		}
	}

	public void SetSwitcherMode(int switcherMode)
	{
		if (m_switcherMode.value != switcherMode)
		{
			m_switcherModeType = (SwitcherMode)switcherMode;
			m_switcherMode.value = switcherMode;
			RefreshRoomParams();
		}
	}

	protected override void OnDeactivate()
	{
		UILockInputContext.DisableLock();
		if (Overlay.instance is SteamOverlay steamOverlay && GameManager.IsLinuxOsPlatform())
		{
			steamOverlay.onCloseOverlayKeyboard = (Action<string>)Delegate.Remove(steamOverlay.onCloseOverlayKeyboard, new Action<string>(OnReceiveInputText));
		}
		m_prefs.globals.lastExternalRoomTrack = m_lastExternalTrackId;
		m_prefs.globals.lastRoomTrack = m_lastTrackId;
		SwitchListSettingsItem trackSwitcher = m_trackSwitcher;
		trackSwitcher.onChangedValue = (System.Action)Delegate.Remove(trackSwitcher.onChangedValue, new System.Action(TrackSwitcherChangedHandler));
		SwitchListSettingsItem adhLimit = m_adhLimit;
		adhLimit.onChangedValue = (System.Action)Delegate.Remove(adhLimit.onChangedValue, new System.Action(AdhLimitChangedHandler));
		SwitchListSettingsItem powerDendityLimit = m_powerDendityLimit;
		powerDendityLimit.onChangedValue = (System.Action)Delegate.Remove(powerDendityLimit.onChangedValue, new System.Action(PowerDensityChangedHandler));
		SwitchListSettingsItem raceType = m_raceType;
		raceType.onChangedValue = (System.Action)Delegate.Remove(raceType.onChangedValue, new System.Action(RaceTypeChanchedHandler));
		SwitchListSettingsItem wheelDriveType = m_wheelDriveType;
		wheelDriveType.onChangedValue = (System.Action)Delegate.Remove(wheelDriveType.onChangedValue, new System.Action(DriveTypeChangeHandler));
		SwitchListSettingsItem collisionType = m_collisionType;
		collisionType.onChangedValue = (System.Action)Delegate.Remove(collisionType.onChangedValue, new System.Action(CollisionTypeChangeHandler));
		ToggleListSettingsItem nightMode = m_nightMode;
		nightMode.onChangedValue = (System.Action)Delegate.Remove(nightMode.onChangedValue, new System.Action(NightModeChangedHandler));
		SwitchListSettingsItem lightSwitcher = m_lightSwitcher;
		lightSwitcher.onChangedValue = (System.Action)Delegate.Remove(lightSwitcher.onChangedValue, new System.Action(LightSwitcherChangedHandler));
		ToggleListSettingsItem crossPlay = m_crossPlay;
		crossPlay.onChangedValue = (System.Action)Delegate.Remove(crossPlay.onChangedValue, new System.Action(CrossPlayChangedHandler));
		SwitchListSettingsItem passwordMode = m_passwordMode;
		passwordMode.onChangedValue = (System.Action)Delegate.Remove(passwordMode.onChangedValue, new System.Action(PasswordModeChangedHandler));
		SwitchListSettingsItem spawnPoints = m_spawnPoints;
		spawnPoints.onChangedValue = (System.Action)Delegate.Remove(spawnPoints.onChangedValue, new System.Action(SpawnPointChangedHandler));
		SwitchListSettingsItem spawnPoints2 = m_spawnPoints;
		spawnPoints2.onChangedValue = (System.Action)Delegate.Remove(spawnPoints2.onChangedValue, new System.Action(AssistChangedHandler));
		SwitchListSettingsItem tractionControl = m_tractionControl;
		tractionControl.onChangedValue = (System.Action)Delegate.Remove(tractionControl.onChangedValue, new System.Action(TractionControlChangeHandler));
		UnloadResources();
		SingletonScriptableObject<MinimapDBAsset>.Unload();
		if (m_sfxLobby != null)
		{
			m_sfxLobby.CreateRoomError -= OnCreateRoomErrorHandler;
		}
		base.OnDeactivate();
	}

	public static void Show(ContextOwner owner)
	{
		if (owner == null)
		{
			UIUtils.LogNullReference("UICreateRoomContext.Show", "owner");
		}
		else
		{
			owner.HideStackAndPush<UICreateRoomContext>();
		}
	}

	public void Cancel()
	{
		NetworkGame.USER_CREATED_ROOM_NAME = null;
		Hide();
	}

	public void CreateRoom()
	{
		if (s_lastTimeCooldownActivated + 10f > Time.time)
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.please_wait.Localized(), 1f);
			return;
		}
		if (m_crossPlay.value)
		{
			bool systemRestrictCrossPlatform = false;
			Overlay.instance.CheckSystemCrossPlatformAbility(delegate(bool isSystemRestrictCrossPlatform)
			{
				systemRestrictCrossPlatform = isSystemRestrictCrossPlatform;
			});
			if (systemRestrictCrossPlatform)
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.wrong_mask.Localized(), 3f);
				return;
			}
		}
		NetworkMapItem map = default(NetworkMapItem);
		if (m_trackSwitcher.selectedItem != null)
		{
			map = (NetworkMapItem)m_trackSwitcher.selectedItem;
		}
		if (map.dlcCode != DLCCode.Unknown && !Overlay.instance.IsDLCInstalled(map.dlcCode))
		{
			UIMessageBox.ShowDLCDialog(LocalizationKeys.no_dlc_track.Localized(), map.dlcCode);
			return;
		}
		int adhLimit = (int)RoomData.StringToAdhLimit((string)m_adhLimit.selectedItem);
		NetworkController.CheckForCriticalParamsAndJoin(adhLimit, m_powerDendityLimit.index, m_wheelDriveType.index, m_tractionControl.index, delegate(bool powerDensityJoin)
		{
			if (powerDensityJoin)
			{
				NetworkController.CheckAssistAndJoin(m_assist.index, delegate(bool assistCanJoin)
				{
					if (assistCanJoin)
					{
						NetworkController.CheckTractionControlAndJoin(m_tractionControl.index, delegate(bool tractionControlCanJoin)
						{
							if (tractionControlCanJoin)
							{
								NetworkController.CheckAdhAndJoin(adhLimit, delegate(bool adhCanJoin)
								{
									if (adhCanJoin)
									{
										NetworkController.CheckWheelDriveTypeAndJoin(m_wheelDriveType.index, delegate(bool wheelDriveCanJoin)
										{
											if (wheelDriveCanJoin)
											{
												UILockInputContext.SetLock(base.owner);
												byte newFlags = (byte)SerializeRoomSettingsFlags();
												byte oldFlags = 0;
												if (string.IsNullOrEmpty(roomName.value.Trim()))
												{
													roomName.value = DEFAULT_ROOM_NAME;
												}
												if (roomName.value.Length > 150)
												{
													roomName.value = roomName.value.Substring(0, 150);
												}
												NetworkGame.USER_CREATED_ROOM_NAME = roomName.value;
												Overlay.filterTextManager.GetFilteredCachedText(TextFilterType.Roomname, Overlay.instance.GetPlatform(), roomName.value, null);
												string text = (Overlay.instance.IsUserGeneratedContentRestricted ? DEFAULT_ROOM_NAME : roomName.value);
												string lightPreset = string.Empty;
												if (m_lightSwitcher.index >= 0 && m_lightSwitcher.index < m_lightSwitcher.list.Count)
												{
													lightPreset = ((NetworkLightItem)m_lightSwitcher.list[m_lightSwitcher.index]).target;
												}
												DB.Meta.Track track = m_tracks.Find((DB.Meta.Track x) => x.id == map.id);
												if (track != null)
												{
													if (m_version.value == "2.21.0")
													{
														GUICommonGodVoice.ShowText("Map is created for an old version, there may be issues");
													}
													NetworkController.CreateRoom(new RoomSettings(map.id, track.externalId, NetworkController.DecorateRoomCode(m_passwordRoom), text, track.name, newFlags, oldFlags, m_nightMode.value, adhLimit, m_crossPlay.value, lightPreset, m_spawnPoints.index, m_assist.index, track.ugcServiceId, m_powerDendityLimit.index, m_tractionControl.index, m_wheelDriveType.index, m_collisionType.index));
												}
											}
										});
									}
								});
							}
						});
					}
				});
			}
		});
	}

	public void ChangeRoomName()
	{
		if (Overlay.instance is SteamOverlay steamOverlay && GameManager.IsLinuxOsPlatform())
		{
			steamOverlay.ShowOverlayKeyboard(LocalizationKeys.network_room_change_name_title.Localized(), roomName.value, 40);
		}
		else
		{
			ShowInputDialog();
		}
		void ShowInputDialog()
		{
			UIMessageBox.ShowInputDialog(LocalizationKeys.network_room_change_name_title.Localized(), roomName.value, null, null, LocalizationKeys.Cancel.Localized(), OnReceiveInputText, LocalizationKeys.Ok.Localized());
		}
	}

	private void OnReceiveInputText(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		UILockInputContext.SetLock(base.owner);
		roomName.value = text;
		NetworkGame.USER_CREATED_ROOM_NAME = text;
		Overlay.filterTextManager.GetFilteredCachedText(TextFilterType.Roomname, Overlay.instance.GetPlatform(), text, delegate(string filteredText)
		{
			UILockInputContext.DisableLock();
			if (!string.IsNullOrEmpty(filteredText))
			{
				roomName.value = UIUtils.ConvertNGUIColorToRich(filteredText);
				roomName.value = roomName.value.Replace("\n", "").Replace("\r", "");
				if (roomName.value.Length > 150)
				{
					roomName.value = roomName.value.Substring(0, 150);
				}
			}
			else
			{
				roomName.value = DEFAULT_ROOM_NAME;
			}
			m_prefs.globals.lastRoomName = roomName.value.Trim();
		});
	}

	[GameHotkey(ActionType.NavUp, false, true)]
	public void NavigationUp()
	{
		paramsList.selectedIndex--;
		if (paramsList.selectedIndex < 0)
		{
			paramsList.selectedIndex = 0;
		}
		paramsList.UpdateList();
	}

	[GameHotkey(ActionType.NavDown, false, true)]
	public void NavigationDown()
	{
		paramsList.selectedIndex++;
		if (paramsList.selectedIndex >= paramsList.GetCount())
		{
			paramsList.selectedIndex = paramsList.GetCount() - 1;
		}
		paramsList.UpdateList();
	}

	[GameHotkey(ActionType.NavRight, false, false)]
	public void NavigationRight()
	{
		paramsList.GetSelectedItem()?.NextValue();
	}

	[GameHotkey(ActionType.NavLeft, false, false)]
	public void NavigationLeft()
	{
		paramsList.GetSelectedItem()?.PrevValue();
	}

	private void LoadResources()
	{
		DB.Meta.Track track = m_normalTracks.Find((DB.Meta.Track track2) => track2.id == m_lastTrackId);
		if (m_switcherModeType == SwitcherMode.WorkshopTracks)
		{
			track = m_externalTracks.Find((DB.Meta.Track track2) => track2.id == m_lastExternalTrackId);
		}
		m_versionVisibility.value = false;
		m_version.value = string.Empty;
		if (track != null && track.external)
		{
			UnloadResources();
			TryAcquireExternalTrackClientVersion(track);
			m_showUgcService.value = true;
			m_ugcServiceId.value = track.ugcServiceId;
			m_ugcServiceTitle.value = UGCServiceProvider.instance.GetManagerBy(IUGCManager.ContentType.Tracks, (IUGCManager.ServiceProvider)track.ugcServiceId).serviceTitle;
			m_externalTrackTexture = UIUtils.CreateTextureFromPath(track.icon);
			trackIcon.value = UIUtils.CreateSprite(m_externalTrackTexture);
			m_showMapDescription.value = true;
			m_mapDescription.value = track.description;
		}
		else
		{
			m_showUgcService.value = false;
			string text = "ui/tracks/" + ((NetworkMapItem)m_trackSwitcher.list[m_trackSwitcher.index]).icon;
			if (trackIconLastPath != text)
			{
				UnloadResources();
				m_showMapDescription.value = false;
				m_mapDescription.value = string.Empty;
				m_mapImage.value = ResourcesWrapper.Load<Sprite>("ui/tracks/minimapsonlyroad/" + track.largeIcon, "temp");
				trackIcon.value = ResourcesWrapper.Load<Sprite>(text, "temp");
			}
		}
	}

	private void TryAcquireExternalTrackClientVersion(DB.Meta.Track trackMeta)
	{
		m_ugcManager = UGCServiceProvider.instance.GetManagerBy(IUGCManager.ContentType.Tracks, (IUGCManager.ServiceProvider)trackMeta.ugcServiceId);
		if (m_ugcManager.TryGetInstalledItem(trackMeta.externalId) is IUGCMeta meta)
		{
			m_ugcManager.ItemTargetVersionExist(meta, out var targetVersion);
			m_versionVisibility.value = targetVersion == "2.21.0";
			m_version.value = targetVersion;
		}
	}

	private void UnloadResources()
	{
		if (m_externalTrackTexture == null)
		{
			ResourcesWrapper.UnloadAsset(trackIcon.value);
			ResourcesWrapper.UnloadAsset(m_mapImage.value);
			m_mapImage.value = null;
		}
		else
		{
			UnityEngine.Object.Destroy(trackIcon.value);
			UnityEngine.Object.Destroy(m_externalTrackTexture);
			m_externalTrackTexture = null;
		}
		trackIcon.value = null;
	}

	private RoomVirtualItem.RoomSettingFlags SerializeRoomSettingsFlags()
	{
		RoomVirtualItem.RoomSettingFlags roomSettingFlags = RoomVirtualItem.RoomSettingFlags.NONE;
		if (m_selectedNetworkMap.isExternal)
		{
			roomSettingFlags |= RoomVirtualItem.RoomSettingFlags.EXTERNAL_TRACK;
		}
		if (m_raceType.index == 0)
		{
			roomSettingFlags |= RoomVirtualItem.RoomSettingFlags.DRIFT_ALLOWED;
		}
		if (m_raceType.index == 1)
		{
			roomSettingFlags |= RoomVirtualItem.RoomSettingFlags.TIME_ALLOWED;
		}
		roomSettingFlags |= RoomVirtualItem.RoomSettingFlags.CLIPPING_DUEL_ALLOWED;
		roomSettingFlags |= RoomVirtualItem.RoomSettingFlags.VOTE_ALLOWED;
		BaseRaceState.NightModeActive = m_nightMode.value;
		if (GameConsole.Bool["ChampionshipMode"])
		{
			roomSettingFlags |= RoomVirtualItem.RoomSettingFlags.CHAMPIONSHIP_MODE;
		}
		return roomSettingFlags;
	}

	private void SaveFlag(bool enable, RoomVirtualItem.RoomSettingFlags flag)
	{
		RoomVirtualItem.RoomSettingFlags roomSettingFlags = (RoomVirtualItem.RoomSettingFlags)m_prefs.globals.lastRoomFlags;
		roomSettingFlags = ((!enable) ? ((RoomVirtualItem.RoomSettingFlags)((uint)roomSettingFlags & (uint)(byte)(~(int)flag))) : (roomSettingFlags | flag));
		m_prefs.globals.lastRoomFlags = (int)roomSettingFlags;
	}

	private void GetOrCreateToggle(ref ToggleListSettingsItem item, System.Action callback)
	{
		if (item == null)
		{
			item = new ToggleListSettingsItem();
		}
		item.value = true;
		if (item.onChangedValue == null)
		{
			ToggleListSettingsItem obj = item;
			obj.onChangedValue = (System.Action)Delegate.Combine(obj.onChangedValue, callback);
		}
	}

	private void RefreshDLC()
	{
		NetworkMapItem networkMapItem = (NetworkMapItem)m_trackSwitcher.list[m_trackSwitcher.index];
		if (networkMapItem.dlcCode != DLCCode.Unknown)
		{
			isLockedDLC.value = !Overlay.instance.IsDLCInstalled(networkMapItem.dlcCode);
			dlcName.value = (isLockedDLC.value ? Overlay.instance.GetDLCName(networkMapItem.dlcCode) : string.Empty);
		}
		else
		{
			isLockedDLC.value = false;
			dlcName.value = string.Empty;
		}
	}

	private void FillMapSwitcher(List<DB.Meta.Track> tracks)
	{
		if (tracks == null || tracks.Count == 0)
		{
			return;
		}
		tracks.Sort(DB.Meta.Track.Sort);
		foreach (DB.Meta.Track track in tracks)
		{
			if (track.enabled && (GameConsole.Bool["ChampionshipMode"] || !track.hidden) && !(track.configuration.ToLower() != "default"))
			{
				m_trackSwitcher.list.Add(new NetworkMapItem
				{
					name = (track.external ? track.name : LocalizationService.Localize(track.name)),
					id = track.id,
					icon = track.icon,
					dlcCode = track.dlcCode,
					isExternal = track.external
				});
			}
		}
	}

	private void FillLightSwitcher()
	{
		m_lightSwitcher.list.Clear();
		if (m_selectedNetworkMap.isExternal)
		{
			m_lightSwitcher.list.Add(new NetworkLightItem
			{
				name = LocalizationKeys.default_light_preset.Localized()
			});
			return;
		}
		TrackLightVariantsBase instance = ScriptableSingleton<TrackLightVariantsBase>.instance;
		int index = m_trackSwitcher.index;
		int trackId = 0;
		if (index >= 0 && index < m_trackSwitcher.list.Count)
		{
			trackId = ((NetworkMapItem)m_trackSwitcher.list[index]).id;
		}
		DB.Meta.Track track = m_tracks.Find((DB.Meta.Track t) => t.id == trackId);
		if (track == null)
		{
			return;
		}
		string sceneName = instance.GetSceneName(track.scene, m_nightMode.value);
		string[] allLightVariantForScene = instance.GetAllLightVariantForScene(sceneName);
		if (allLightVariantForScene == null || allLightVariantForScene.Length == 0)
		{
			return;
		}
		Array.Sort(allLightVariantForScene);
		int num = -1;
		for (int num2 = 0; num2 < allLightVariantForScene.Length; num2++)
		{
			string lightPreset = allLightVariantForScene[num2];
			if (instance.IsDefaultPresetForLightScene(sceneName, lightPreset))
			{
				num = num2;
				break;
			}
		}
		if (num != -1)
		{
			ref string reference = ref allLightVariantForScene[0];
			ref string reference2 = ref allLightVariantForScene[num];
			string text = allLightVariantForScene[num];
			string text2 = allLightVariantForScene[0];
			reference = text;
			reference2 = text2;
		}
		for (int num3 = 0; num3 < allLightVariantForScene.Length; num3++)
		{
			string text3 = allLightVariantForScene[num3];
			bool num4 = instance.IsDefaultPresetForLightScene(sceneName, text3);
			string text4 = (num4 ? LocalizationKeys.default_light_preset.Localized() : LocalizationService.Localize(instance.GetVisibleNameForLightPreset(text3)));
			m_lightSwitcher.list.Add(new NetworkLightItem
			{
				name = text4,
				target = text3
			});
			if (num4)
			{
				m_lightSwitcher.index = num3;
			}
		}
	}

	private void TrackSwitcherChangedHandler()
	{
		m_selectedNetworkMap = (NetworkMapItem)m_trackSwitcher.list[m_trackSwitcher.index];
		if (m_selectedNetworkMap.isExternal)
		{
			m_lastExternalTrackId = m_selectedNetworkMap.id;
		}
		else
		{
			m_lastTrackId = m_selectedNetworkMap.id;
		}
		m_trackAnimation.value = true;
		if (!m_selectedNetworkMap.isExternal)
		{
			if (m_prevTrackIsExternal)
			{
				m_nightMode.value = m_prefs.globals.lastRoomNight;
			}
			m_prevTrackIsExternal = false;
		}
		else
		{
			m_prevTrackIsExternal = true;
			if (m_nightMode != null)
			{
				m_nightMode.value = false;
			}
			if (m_lightSwitcher != null)
			{
				m_lightSwitcher.index = 0;
			}
		}
		NightModeChangedHandler();
		if (m_spawnPoints != null)
		{
			m_spawnPoints.index = 0;
			SpawnPointChangedHandler();
		}
		SetEditableEvents(!m_selectedNetworkMap.isExternal);
		LoadResources();
		paramsList.UpdateList();
		RefreshDLC();
	}

	private void SetEditableEvents(bool availableToEdit)
	{
		m_nightMode?.SetEditingPermission(availableToEdit);
		m_lightSwitcher?.SetBlockToEditing(!availableToEdit);
	}

	private void AdhLimitChangedHandler()
	{
		m_prefs.globals.lastRoomAdhLimitIndex = m_adhLimit.index;
		m_adhLimit.sprite = adhIcons[m_adhLimit.index].sprite;
		paramsList.UpdateList();
	}

	private void RaceTypeChanchedHandler()
	{
		m_prefs.globals.lastTrackType = m_raceType.index;
		m_raceType.sprite = raceTypeIcons[m_raceType.index].sprite;
		paramsList.UpdateList();
	}

	private void DriveTypeChangeHandler()
	{
		m_prefs.globals.lastRoomWheelDriveType = m_wheelDriveType.index;
		m_wheelDriveType.sprite = wheelDriveIcons[m_wheelDriveType.index].sprite;
		paramsList.UpdateList();
	}

	private void CollisionTypeChangeHandler()
	{
		m_prefs.globals.lastRoomCollisionType = m_collisionType.index;
		m_collisionType.sprite = collisionIcons[m_collisionType.index].sprite;
		paramsList.UpdateList();
	}

	private void TractionControlChangeHandler()
	{
		m_prefs.globals.lastRoomTractionControl = m_tractionControl.index;
		m_tractionControl.sprite = ((m_tractionControl.index == 0) ? tractionControlInactiveIcon : tractionControlIcon);
		paramsList.UpdateList();
	}

	private void PowerDensityChangedHandler()
	{
		m_prefs.globals.lastRoomPowerDensityLimit = m_powerDendityLimit.index;
		m_powerDendityLimit.sprite = powerDensityIcons[m_powerDendityLimit.index].sprite;
		paramsList.UpdateList();
	}

	private void NightModeChangedHandler()
	{
		if (m_nightMode == null)
		{
			return;
		}
		if (!GameManager.NightModeInstalled && m_nightMode.value)
		{
			Overlay.ShowNoNightModeDLCDialog(base.owner);
			m_nightMode.value = !m_nightMode.value;
			m_nightMode.sprite = (m_nightMode.value ? nightModeIcon : nightModeInactiveIcon);
			return;
		}
		if (m_lightSwitcher != null)
		{
			FillLightSwitcher();
		}
		if (!m_selectedNetworkMap.isExternal)
		{
			m_prefs.globals.lastRoomNight = m_nightMode.value;
		}
		m_nightMode.sprite = (m_nightMode.value ? nightModeIcon : nightModeInactiveIcon);
		paramsList.UpdateList();
	}

	private void LightSwitcherChangedHandler()
	{
		paramsList.UpdateList();
	}

	private void CrossPlayChangedHandler()
	{
		m_prefs.globals.isCrossMPCreatedRoom = m_crossPlay.value;
		m_prefs.globals.lastRoomCrossPlay = m_crossPlay.value;
		m_crossPlay.sprite = (m_crossPlay.value ? crossPlayIcon : crossPlayInactiveIcon);
		paramsList.UpdateList();
	}

	private void PasswordModeChangedHandler()
	{
		m_prefs.globals.lastRoomLocked = m_passwordMode.index > 0;
		if (m_passwordMode.index == 0)
		{
			m_passwordRoom = "";
			m_passwordMode.sprite = passwordInactiveIcon;
			paramsList.UpdateList();
		}
		else
		{
			m_passwordRoom = DependencyInjector.Resolve<NetworkController>().GenerateRoomCode();
			m_passwordMode.sprite = passwordIcon;
			m_passwordMode.list[1] = (GameConsole.Bool["ChampionshipMode"] ? "****" : m_passwordRoom.ToString());
			m_passwordMode.ForceEnableNextElement();
			paramsList.UpdateList();
		}
	}

	private void SpawnPointChangedHandler()
	{
		if (m_spawnPoints == null)
		{
			return;
		}
		m_selectedNetworkMap.spawnPoint = m_spawnPoints.index;
		m_prefs.globals.lastRoomSpawnPoint = m_selectedNetworkMap.spawnPoint;
		m_spawnPoints.list.Clear();
		m_spawnpointElementList.ClearForce();
		DB.Meta.Track track = m_tracks.Find((DB.Meta.Track track2) => track2.id == m_selectedNetworkMap.id);
		if (track != null)
		{
			List<TrackLayoutData> list = TrackMetaDB.LoadConfig<TrackLayoutData>("Assets/Settings/Tracks/" + track.scene + "/_Network/");
			if (list != null)
			{
				m_spawnPoints.title = LocalizationKeys.network_game_params_spawnpoints.Localized();
				TrackLayoutData trackLayoutData = list.Find((TrackLayoutData trackLayoutData2) => trackLayoutData2.name.Equals("Default", StringComparison.OrdinalIgnoreCase));
				if (trackLayoutData != null && trackLayoutData.spawnPoints != null)
				{
					MinimapData minimapData = SingletonScriptableObject<MinimapDBAsset>.instance.Get(track.id);
					for (int num = 0; num < trackLayoutData.spawnPoints.Length; num++)
					{
						m_spawnPoints.list.Add((char)(num + 65));
						if (minimapData != null && minimapData.boundsSize.x + minimapData.boundsSize.y > 0f)
						{
							m_spawnpointElementList.Add(((char)(num + 65)).ToString().ToUpper(), trackLayoutData.spawnPoints[num], minimapData);
						}
					}
				}
			}
		}
		m_spawnPoints.sprite = ((m_spawnPoints.list.Count > 0) ? spawnPointIcon : spawnPointInactiveIcon);
		paramsList.UpdateList();
		m_spawnpointElementList.Select(m_selectedNetworkMap.spawnPoint);
	}

	private void AssistChangedHandler()
	{
		m_prefs.globals.lastRoomAssist = m_assist.index;
		if (m_assist.index == 0)
		{
			m_assist.sprite = assistInactiveIcon;
			paramsList.UpdateList();
		}
		else
		{
			m_assist.sprite = assistIcon;
			paramsList.UpdateList();
		}
	}
}
