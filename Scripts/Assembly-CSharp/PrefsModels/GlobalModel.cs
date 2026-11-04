using System;
using System.Collections;
using System.Collections.Generic;
using CarX;
using Championship;
using Chat;
using CodeStage.AntiCheat.ObscuredTypes;
using DB;
using DB.Meta;
using DI;
using GameOverlay;
using SharingLiveries;
using SharingSetups;
using SharingTracks;
using SimpleJSON;
using SyncMultiplayer;
using Tayx.Graphy;
using Tayx.Graphy.Utils;
using TrackLayout.Data;
using UnityEngine;

namespace PrefsModels
{
	public class GlobalModel : BasePrefsModel
	{
		public enum InclinometerMode
		{
			AutoHide = 0,
			Off = 1,
			On = 2
		}

		public class RearviewMirrorInfo
		{
			public Vector2 pos;

			public Vector2 size;
		}

		public enum SteeringAssist
		{
			None = 0,
			AssistV2 = 1,
			AssistV1 = 2
		}

		public enum TractionControlActivity
		{
			None = 0,
			Off = 1,
			On = 2
		}

		public enum MouseMoveType
		{
			Delta = 0,
			Coord = 1
		}

		public enum VinylsShowOptions
		{
			All = 0,
			NotShow = 1,
			CurrentPlatform = 2
		}

		public enum DynamicCockpitCamMode
		{
			OFF = -1,
			Legacy = 0,
			Improved = 1,
			Count = 2
		}

		private FreeDroneCameraSettings m_freeDroneSettings = new FreeDroneCameraSettings();

		public SteeringAssist steeringAssistentWheel;

		public SteeringAssist steeringAssistentGamepad = SteeringAssist.AssistV2;

		public SteeringAssist steeringAssistentKeyboard = SteeringAssist.AssistV2;

		public MouseMoveType moveMouseAxisType;

		public UIGarageContext.CarFilterType carFilterType;

		public VinylsShowOptions vinylsShowOption;

		public float autoBrake;

		public List<Top32ChampRaceAttempt> trainAttempts;

		public List<Top32ChampRaceAttempt> qualificationAttempts;

		public HashSet<int> carsVisualUpdate = new HashSet<int>();

		[JsonIgnore]
		private int m_lastCarId = -1;

		public int lastTrackId = -1;

		public ulong lastExternalID;

		public Dictionary<int, (Vector3, Quaternion)> waypoints = new Dictionary<int, (Vector3, Quaternion)>();

		[JsonIgnore]
		public TrackMetaConfig lastMetaConfig;

		public int lastCameraId;

		public int lastHintId = 1;

		public int lastLevelNotify = 1;

		private int m_lastReplayCameraIndex;

		public string currentRegion;

		public bool respawnCarOnlyOnSpline;

		private int m_chatFont;

		private string m_lastGameVersion = string.Empty;

		private int m_iconGeneratorVersion;

		private int m_carSlotVersion;

		private bool m_ageConfirmApproved;

		private bool m_licenseApproved;

		private int m_roomType;

		private bool m_privacyPolicyApproved;

		private bool m_nightModeActive;

		[JsonIgnore]
		public string lobbyTrackNameSwitcherFilter = string.Empty;

		[JsonIgnore]
		public string lobbyRoomNameInputFilter = string.Empty;

		[JsonIgnore]
		public int lobbyAdhLimitFilter;

		[JsonIgnore]
		public int lobbyPowerDensityLimitFilter;

		[JsonIgnore]
		public int lobbyPowerDensityIndexFilter;

		[JsonIgnore]
		public int lobbyWheelDriveTypeFilter;

		[JsonIgnore]
		public int collisionsIndexFilter = 1;

		public SharingSetupsFilter sharingSetupsFilter;

		public SharingLiveriesFilter sharingLiveriesFilter;

		public SharingTracksFilter sharingTracksFilter;

		[JsonIgnore]
		public Action PlayerExpChanged;

		private ObscuredInt m_playerExp = 0;

		private string m_syncKey = "";

		[JsonIgnore]
		private string m_installationGUID = "";

		[JsonIgnore]
		public Dictionary<GameGlobalFeature, int> gameFeaturesRequiredLevels = new Dictionary<GameGlobalFeature, int>();

		[JsonIgnore]
		private Dictionary<GameGlobalFeature, int> m_gameFeaturesDefaultRequiredLevels = new Dictionary<GameGlobalFeature, int>();

		[JsonIgnore]
		private Dictionary<int, List<GameGlobalFeature>> m_unlockedCarExceptionFeatures = new Dictionary<int, List<GameGlobalFeature>>
		{
			{
				3,
				new List<GameGlobalFeature>
				{
					GameGlobalFeature.SetupTurbo,
					GameGlobalFeature.SetupRace,
					GameGlobalFeature.SetupDrift,
					GameGlobalFeature.SetupUltimate,
					GameGlobalFeature.SetupUltimatePro
				}
			},
			{
				40,
				new List<GameGlobalFeature>
				{
					GameGlobalFeature.SetupTurbo,
					GameGlobalFeature.SetupRace,
					GameGlobalFeature.SetupDrift,
					GameGlobalFeature.SetupUltimate,
					GameGlobalFeature.Cars2,
					GameGlobalFeature.SetupUltimatePro
				}
			},
			{
				9,
				new List<GameGlobalFeature>
				{
					GameGlobalFeature.SetupTurbo,
					GameGlobalFeature.SetupRace,
					GameGlobalFeature.SetupDrift,
					GameGlobalFeature.SetupUltimate,
					GameGlobalFeature.Cars3,
					GameGlobalFeature.SetupUltimatePro
				}
			}
		};

		private bool m_XRActivated;

		private bool m_camDynamic = true;

		private PhotoCamParameters m_photoCamParams = new PhotoCamParameters();

		private DynamicCamParameters m_dynamicCamParams = new DynamicCamParameters();

		private bool m_highRevsShakeCam;

		private bool m_showGhost = true;

		[JsonIgnore]
		public Action onShowMinimapChanged;

		private bool m_showMinimap = true;

		private bool m_autoConnectToEpicServices;

		private bool m_hideDriver;

		private bool m_hideWheel;

		private bool m_hideWindows;

		private bool m_rearviewMirror;

		[JsonIgnore]
		public Action onRearviewMirrorChanged;

		public RearviewMirrorInfo rearviewMirrorInfo;

		private bool m_chatSingleplayer = true;

		public ChatPrefsDto chatPrefsDto = new ChatPrefsDto();

		private bool m_showNickNames = true;

		public bool speedKmh = true;

		public bool pedalsIndicator = true;

		private bool m_hideDriftHud;

		private bool m_hideDriftCounter;

		private bool m_showKissTheWallHUD = true;

		private bool m_displayBoostGauge = true;

		private InclinometerMode m_inclinometerMode;

		private int m_cameraOrbitStick = 1;

		private bool m_crossPlatformSteam = true;

		[JsonIgnore]
		public Action onCrossPlatformPlayChanged;

		private bool m_syncHeadlights = true;

		private int m_fov = 60;

		private int m_cockpitFov = 60;

		private bool m_showFPSSetting;

		private float m_masterVolume = 1f;

		private float m_musicVolume = 1f;

		private float? m_musicVolumeSliderVal;

		private float m_soundFXVolume = 1f;

		private float? m_soundFXVolumeSliderVal;

		private float m_targetCarVolume = 1f;

		private float? m_targetCarVolumeSliderVal;

		private float m_otherCarsVolume = 0.6f;

		private float? m_otherCarsVolumeSliderVal;

		private float m_ambienceVolume = 1f;

		private float? m_ambienceVolumeSliderVal;

		private float m_seqTransmisonVolume = 1f;

		private float? m_seqTransmisonVolumeSliderVal;

		public bool padSound = true;

		public readonly float VELOCITY_SCALE = 0.7f;

		private bool m_workshopNetworkCars = true;

		public List<CarColorFavorite> favoriteColors = new List<CarColorFavorite>();

		public Dictionary<UIPaintCustomizationContext.StateType, string> selectedFavoriteId = new Dictionary<UIPaintCustomizationContext.StateType, string>();

		public bool colorSchemeRGB;

		private int m_newsInfoVersion;

		private string m_lastRoomName;

		private int m_lastRoomTrack;

		private int m_lastExternalRoomTrack;

		private bool m_lastRoomLocked;

		private int m_lastRoomAssist;

		private int m_lastTrackType;

		private int m_lastRoomTractionControl;

		private int m_lastRoomPowerDensityLimit;

		private int m_lastRoomFlags = 31;

		private bool m_lastRoomNight;

		private bool m_lastRoomCrossPlay = true;

		private int m_lastRoomSpawnPoint;

		private int m_lastRoomWheelDriveType;

		private int m_lastRoomCollisionType = 1;

		private int m_lastRoomAdhLimitIndex;

		private int m_modsNotificationShown;

		public FreeDroneCameraSettings freeDroneSettings
		{
			get
			{
				return m_freeDroneSettings;
			}
			set
			{
				m_freeDroneSettings = value;
			}
		}

		[JsonIgnore]
		public string lightPresetName { get; set; }

		[JsonIgnore]
		public string sceneName { get; set; }

		public int lastCarId
		{
			get
			{
				return m_lastCarId;
			}
			set
			{
				if (m_lastCarId != value)
				{
					m_lastCarId = value;
					GameEvents.ChangeLastCarId(value);
				}
			}
		}

		public int lastReplayCameraIndex
		{
			get
			{
				return m_lastReplayCameraIndex;
			}
			set
			{
				m_lastReplayCameraIndex = value;
				Save();
			}
		}

		public int chatFont
		{
			get
			{
				return m_chatFont;
			}
			set
			{
				if (value != m_chatFont)
				{
					m_chatFont = value;
					Save();
				}
			}
		}

		public string lastGameVersion
		{
			get
			{
				return m_lastGameVersion;
			}
			set
			{
				if (value != m_lastGameVersion)
				{
					m_lastGameVersion = value;
					Save();
				}
			}
		}

		public int iconGeneratorVersion
		{
			get
			{
				return m_iconGeneratorVersion;
			}
			set
			{
				if (value != m_iconGeneratorVersion)
				{
					m_iconGeneratorVersion = value;
					Save();
				}
			}
		}

		[Obsolete("Initial version holder for slots. Use actual version in PrefsCarSlotDataModel")]
		public int carSlotVersion
		{
			get
			{
				return m_carSlotVersion;
			}
			set
			{
				if (value != m_carSlotVersion)
				{
					m_carSlotVersion = value;
					Save();
				}
			}
		}

		public bool ageConfirmApproved
		{
			get
			{
				return m_ageConfirmApproved;
			}
			set
			{
				if (value != m_ageConfirmApproved)
				{
					m_ageConfirmApproved = value;
					Save();
				}
			}
		}

		public bool licenseApproved
		{
			get
			{
				return m_licenseApproved;
			}
			set
			{
				if (value != m_licenseApproved)
				{
					m_licenseApproved = value;
					Save();
				}
			}
		}

		public int roomType
		{
			get
			{
				return m_roomType;
			}
			set
			{
				if (value != m_roomType)
				{
					m_roomType = value;
					Save();
				}
			}
		}

		public bool privacyPolicyApprovedV2
		{
			get
			{
				return m_privacyPolicyApproved;
			}
			set
			{
				if (value != m_privacyPolicyApproved)
				{
					m_privacyPolicyApproved = value;
					Save();
				}
			}
		}

		[JsonIgnore]
		public bool eulaAccepted
		{
			get
			{
				if (ageConfirmApproved && licenseApproved)
				{
					return privacyPolicyApprovedV2;
				}
				return false;
			}
		}

		public bool nightModeActive
		{
			get
			{
				return m_nightModeActive;
			}
			set
			{
				if (value != m_nightModeActive)
				{
					m_nightModeActive = value;
					Save();
				}
			}
		}

		public int playerExp
		{
			get
			{
				return m_playerExp;
			}
			set
			{
				if (value != (int)m_playerExp)
				{
					m_playerExp = value;
					Save();
					PlayerExpChanged.SafeCall();
				}
			}
		}

		public string syncKey
		{
			get
			{
				return m_syncKey;
			}
			set
			{
				if (value != m_syncKey)
				{
					m_syncKey = value;
					Save();
				}
			}
		}

		public string installationGUID => m_installationGUID;

		public bool XRActivated
		{
			get
			{
				return m_XRActivated;
			}
			set
			{
				if (m_XRActivated != value)
				{
					m_XRActivated = value;
				}
			}
		}

		public bool camDynamic
		{
			get
			{
				return m_camDynamic;
			}
			set
			{
				if (m_camDynamic != value)
				{
					m_camDynamic = value;
					ApplyCamEffects();
				}
			}
		}

		public PhotoCamParameters photoCamParams
		{
			get
			{
				return m_photoCamParams;
			}
			set
			{
				m_photoCamParams = value;
			}
		}

		public DynamicCamParameters dynamicCamParams
		{
			get
			{
				return m_dynamicCamParams;
			}
			set
			{
				m_dynamicCamParams = value;
				ApplyCamEffects();
			}
		}

		public bool highRevsShakeCam
		{
			get
			{
				return m_highRevsShakeCam;
			}
			set
			{
				if (m_highRevsShakeCam != value)
				{
					m_highRevsShakeCam = value;
					ApplyCamEffects();
				}
			}
		}

		public bool showGhost
		{
			get
			{
				return m_showGhost;
			}
			set
			{
				if (m_showGhost != value)
				{
					m_showGhost = value;
					this.ShowGhostChanged.Call(value);
				}
			}
		}

		public bool showMinimap
		{
			get
			{
				return m_showMinimap;
			}
			set
			{
				if (m_showMinimap != value)
				{
					m_showMinimap = value;
					onShowMinimapChanged.Call();
				}
			}
		}

		public bool autoConnectToEpicServices
		{
			get
			{
				return m_autoConnectToEpicServices;
			}
			set
			{
				m_autoConnectToEpicServices = value;
			}
		}

		public bool hideDriver
		{
			get
			{
				return m_hideDriver;
			}
			set
			{
				m_hideDriver = value;
				UpdateDriverVisibility();
				Save();
			}
		}

		public bool hideWheel
		{
			get
			{
				return m_hideWheel;
			}
			set
			{
				m_hideWheel = value;
				UpdateDriverVisibility();
				Save();
			}
		}

		public bool hideWindows
		{
			get
			{
				return m_hideWindows;
			}
			set
			{
				if (value != m_hideWindows)
				{
					m_hideWindows = value;
					UpdateWindowVisibility();
				}
			}
		}

		public bool isRearviewMirrorVisible
		{
			get
			{
				return m_rearviewMirror;
			}
			set
			{
				m_rearviewMirror = value;
				Save();
				onRearviewMirrorChanged.Call();
			}
		}

		public bool chatSingleplayer
		{
			get
			{
				if (m_chatSingleplayer && eulaAccepted)
				{
					return !Overlay.instance.IsCommunicationRestricted;
				}
				return false;
			}
			set
			{
				bool flag = value;
				bool flag2 = true;
				if (Overlay.instance.IsCommunicationRestricted || !eulaAccepted)
				{
					flag = false;
					flag2 = false;
				}
				if (m_chatSingleplayer != flag)
				{
					if (flag2)
					{
						m_chatSingleplayer = flag;
					}
					this.showChatChanged.SafeCall();
				}
			}
		}

		public bool showNickNames
		{
			get
			{
				return m_showNickNames;
			}
			set
			{
				m_showNickNames = value;
				Save();
			}
		}

		public bool hideDriftHud
		{
			get
			{
				return m_hideDriftHud;
			}
			set
			{
				m_hideDriftHud = value;
			}
		}

		public bool hideDriftCounter
		{
			get
			{
				return m_hideDriftCounter;
			}
			set
			{
				m_hideDriftCounter = value;
			}
		}

		public bool showKissTheWallHUD
		{
			get
			{
				return m_showKissTheWallHUD;
			}
			set
			{
				if (value != m_showKissTheWallHUD)
				{
					m_showKissTheWallHUD = value;
					this.ShowKissTheWallChanged.Call(value);
				}
			}
		}

		public bool displayBoostGauge
		{
			get
			{
				return m_displayBoostGauge;
			}
			set
			{
				if (m_displayBoostGauge != value)
				{
					m_displayBoostGauge = value;
					Save();
				}
			}
		}

		public InclinometerMode inclinometerMode
		{
			get
			{
				return m_inclinometerMode;
			}
			set
			{
				m_inclinometerMode = value;
				Save();
			}
		}

		public int cameraOrbitStick
		{
			get
			{
				return m_cameraOrbitStick;
			}
			set
			{
				m_cameraOrbitStick = value;
			}
		}

		public bool crossPlatformPlay
		{
			get
			{
				return m_crossPlatformSteam;
			}
			set
			{
				if (value != m_crossPlatformSteam)
				{
					m_crossPlatformSteam = value;
					Save();
					onCrossPlatformPlayChanged.Call();
				}
			}
		}

		public bool syncHeadlights
		{
			get
			{
				return m_syncHeadlights;
			}
			set
			{
				if (value == m_syncHeadlights)
				{
					return;
				}
				m_syncHeadlights = value;
				if (!m_syncHeadlights)
				{
					DayNightController instance = DayNightController.instance;
					bool lightsEnabled = instance != null && !instance.IsDayLight;
					if (NetworkController.InstanceGame != null)
					{
						foreach (NetworkPlayer player in NetworkController.InstanceGame.Players)
						{
							if (player != null && player.userCar != null)
							{
								player.userCar.SetHeadlightsEnabled(lightsEnabled, ignoreIgnition: false, force: true);
							}
						}
					}
				}
				Save();
			}
		}

		public int fov
		{
			get
			{
				return m_fov;
			}
			set
			{
				m_fov = value;
				ApplyCamEffects();
			}
		}

		public int cockpitFov
		{
			get
			{
				return m_cockpitFov;
			}
			set
			{
				m_cockpitFov = value;
				ApplyCamEffects();
			}
		}

		public bool showFPSSetting
		{
			get
			{
				return m_showFPSSetting;
			}
			set
			{
				m_showFPSSetting = value;
				TimeController.StartNow(EnableGraphy());
			}
		}

		public float masterVolume
		{
			get
			{
				return m_masterVolume;
			}
			set
			{
				if (m_masterVolume != value)
				{
					m_masterVolume = value;
				}
			}
		}

		public float musicVolume
		{
			get
			{
				return m_musicVolume;
			}
			set
			{
				if (m_musicVolume != value)
				{
					m_musicVolume = value;
				}
			}
		}

		public float musicVolumeSliderVal
		{
			get
			{
				if (!m_musicVolumeSliderVal.HasValue)
				{
					return m_musicVolume;
				}
				return m_musicVolumeSliderVal.Value;
			}
			set
			{
				m_musicVolumeSliderVal = value;
			}
		}

		public float soundFXVolume
		{
			get
			{
				return m_soundFXVolume;
			}
			set
			{
				if (m_soundFXVolume != value)
				{
					m_soundFXVolume = value;
					ApplySoundFXVolume();
				}
			}
		}

		public float soundFXVolumeSliderVal
		{
			get
			{
				if (!m_soundFXVolumeSliderVal.HasValue)
				{
					return m_soundFXVolume;
				}
				return m_soundFXVolumeSliderVal.Value;
			}
			set
			{
				m_soundFXVolumeSliderVal = value;
			}
		}

		public float targetCarVolume
		{
			get
			{
				return m_targetCarVolume;
			}
			set
			{
				if (!(Mathf.Abs(m_targetCarVolume - value) < Mathf.Epsilon))
				{
					m_targetCarVolume = value;
				}
			}
		}

		public float targetCarVolumeSliderVal
		{
			get
			{
				if (!m_targetCarVolumeSliderVal.HasValue)
				{
					return m_targetCarVolume;
				}
				return m_targetCarVolumeSliderVal.Value;
			}
			set
			{
				m_targetCarVolumeSliderVal = value;
			}
		}

		public float otherCarsVolume
		{
			get
			{
				return m_otherCarsVolume;
			}
			set
			{
				if (!(Mathf.Abs(m_otherCarsVolume - value) < Mathf.Epsilon))
				{
					m_otherCarsVolume = value;
				}
			}
		}

		public float otherCarsVolumeSliderVal
		{
			get
			{
				if (!m_otherCarsVolumeSliderVal.HasValue)
				{
					return m_otherCarsVolume;
				}
				return m_otherCarsVolumeSliderVal.Value;
			}
			set
			{
				m_otherCarsVolumeSliderVal = value;
			}
		}

		public float ambienceVolume
		{
			get
			{
				return m_ambienceVolume;
			}
			set
			{
				if (!(Mathf.Abs(m_ambienceVolume - value) < Mathf.Epsilon))
				{
					m_ambienceVolume = value;
					ApplyAmbienceVolume();
				}
			}
		}

		public float ambienceVolumeSliderVal
		{
			get
			{
				if (!m_ambienceVolumeSliderVal.HasValue)
				{
					return m_ambienceVolume;
				}
				return m_ambienceVolumeSliderVal.Value;
			}
			set
			{
				m_ambienceVolumeSliderVal = value;
			}
		}

		public float seqTransmisonVolume
		{
			get
			{
				return m_seqTransmisonVolume;
			}
			set
			{
				if (!Mathf.Approximately(m_seqTransmisonVolume, value))
				{
					m_seqTransmisonVolume = value;
					Save();
				}
			}
		}

		public float seqTransmisonVolumeSliderVal
		{
			get
			{
				if (!m_seqTransmisonVolumeSliderVal.HasValue)
				{
					return m_seqTransmisonVolume;
				}
				return m_seqTransmisonVolumeSliderVal.Value;
			}
			set
			{
				m_seqTransmisonVolumeSliderVal = value;
			}
		}

		public bool workshopNetworkCars
		{
			get
			{
				return m_workshopNetworkCars;
			}
			set
			{
				m_workshopNetworkCars = value;
				Save();
			}
		}

		public int newsInfoVersion
		{
			get
			{
				return m_newsInfoVersion;
			}
			set
			{
				m_newsInfoVersion = value;
				Save();
			}
		}

		public string lastRoomName
		{
			get
			{
				return m_lastRoomName;
			}
			set
			{
				m_lastRoomName = value;
				Save();
			}
		}

		public int lastRoomTrack
		{
			get
			{
				return m_lastRoomTrack;
			}
			set
			{
				m_lastRoomTrack = value;
				Save();
			}
		}

		public int lastExternalRoomTrack
		{
			get
			{
				return m_lastExternalRoomTrack;
			}
			set
			{
				m_lastExternalRoomTrack = value;
				Save();
			}
		}

		public bool lastRoomLocked
		{
			get
			{
				return m_lastRoomLocked;
			}
			set
			{
				m_lastRoomLocked = value;
				Save();
			}
		}

		public int lastRoomAssist
		{
			get
			{
				return m_lastRoomAssist;
			}
			set
			{
				m_lastRoomAssist = value;
				Save();
			}
		}

		public int lastTrackType
		{
			get
			{
				return m_lastTrackType;
			}
			set
			{
				m_lastTrackType = value;
				Save();
			}
		}

		public int lastRoomTractionControl
		{
			get
			{
				return m_lastRoomTractionControl;
			}
			set
			{
				m_lastRoomTractionControl = value;
				Save();
			}
		}

		public int lastRoomPowerDensityLimit
		{
			get
			{
				return m_lastRoomPowerDensityLimit;
			}
			set
			{
				m_lastRoomPowerDensityLimit = value;
				Save();
			}
		}

		public int lastRoomFlags
		{
			get
			{
				return m_lastRoomFlags;
			}
			set
			{
				m_lastRoomFlags = value;
				Save();
			}
		}

		public bool lastRoomNight
		{
			get
			{
				return m_lastRoomNight;
			}
			set
			{
				m_lastRoomNight = value;
				Save();
			}
		}

		public bool lastRoomCrossPlay
		{
			get
			{
				return m_lastRoomCrossPlay;
			}
			set
			{
				m_lastRoomCrossPlay = value;
				Save();
			}
		}

		public int lastRoomSpawnPoint
		{
			get
			{
				return m_lastRoomSpawnPoint;
			}
			set
			{
				m_lastRoomSpawnPoint = value;
				Save();
			}
		}

		public int lastRoomWheelDriveType
		{
			get
			{
				return m_lastRoomWheelDriveType;
			}
			set
			{
				m_lastRoomWheelDriveType = value;
				Save();
			}
		}

		public int lastRoomCollisionType
		{
			get
			{
				return m_lastRoomCollisionType;
			}
			set
			{
				m_lastRoomCollisionType = value;
				Save();
			}
		}

		[JsonIgnore]
		public bool? isCrossMPCreatedRoom { get; set; }

		public int lastRoomAdhLimitIndex
		{
			get
			{
				return m_lastRoomAdhLimitIndex;
			}
			set
			{
				m_lastRoomAdhLimitIndex = value;
				Save();
			}
		}

		public int modsNotificationShown
		{
			get
			{
				return m_modsNotificationShown;
			}
			set
			{
				m_modsNotificationShown = value;
				Save();
			}
		}

		[JsonIgnore]
		public event Action<bool> ShowGhostChanged;

		public event Action showChatChanged;

		public event Action<bool> ShowKissTheWallChanged;

		public ISharingFilter GetSharingFilter<T>()
		{
			Type typeFromHandle = typeof(T);
			if (typeFromHandle == typeof(SharingSetupsFilter))
			{
				return sharingSetupsFilter;
			}
			if (typeFromHandle == typeof(SharingLiveriesFilter))
			{
				return sharingLiveriesFilter;
			}
			_ = typeFromHandle == typeof(SharingTracksFilter);
			return null;
		}

		public void SaveSharingFilter<T>(ISharingFilter filter)
		{
			if (!(filter is SharingSetupsFilter sharingSetupsFilter))
			{
				if (!(filter is SharingLiveriesFilter sharingLiveriesFilter))
				{
					_ = filter is SharingTracksFilter;
				}
				else
				{
					this.sharingLiveriesFilter = sharingLiveriesFilter;
				}
			}
			else
			{
				this.sharingSetupsFilter = sharingSetupsFilter;
			}
		}

		public void ResetGameSettings()
		{
			camDynamic = true;
			dynamicCamParams.cockpitCamMode = DynamicCockpitCamMode.OFF;
			highRevsShakeCam = false;
			showGhost = true;
			showMinimap = true;
			hideDriver = false;
			hideWheel = false;
			hideWindows = false;
			isRearviewMirrorVisible = false;
			chatSingleplayer = eulaAccepted;
			showNickNames = true;
			speedKmh = true;
			pedalsIndicator = true;
			hideDriftCounter = false;
			showKissTheWallHUD = true;
			displayBoostGauge = true;
			inclinometerMode = InclinometerMode.AutoHide;
			cameraOrbitStick = 1;
			crossPlatformPlay = true;
			autoConnectToEpicServices = false;
			syncHeadlights = true;
			Save();
		}

		private IEnumerator EnableGraphy()
		{
			yield return new WaitUntil(() => G_Singleton<GraphyManager>.IsInstantiated);
			GraphyManager instance = G_Singleton<GraphyManager>.Instance;
			if (m_showFPSSetting)
			{
				instance.SetPreset(2);
				instance.GraphModulePosition = GraphyManager.ModulePosition.TOP_LEFT;
				instance.SetSize(GraphyManager.ModuleSize.SMALL);
				instance.BackgroundColor = Color.clear;
			}
			else
			{
				instance.SetPreset(0);
			}
			Save();
		}

		public void ResetGraphicsSettings()
		{
			fov = 60;
			cockpitFov = 60;
			showFPSSetting = false;
			Save();
		}

		public void ResetSoundsSettings()
		{
			masterVolume = 1f;
			musicVolume = 1f;
			soundFXVolume = 1f;
			targetCarVolume = 1f;
			otherCarsVolume = 0.6f;
			ambienceVolume = 1f;
			seqTransmisonVolume = 1f;
			UpdateSlidersToCurrentValues();
			Save();
		}

		private void UpdateSlidersToCurrentValues()
		{
			m_musicVolumeSliderVal = musicVolume;
			m_soundFXVolumeSliderVal = soundFXVolume;
			m_targetCarVolumeSliderVal = targetCarVolume;
			m_otherCarsVolumeSliderVal = otherCarsVolume;
			m_ambienceVolumeSliderVal = ambienceVolume;
			m_seqTransmisonVolumeSliderVal = seqTransmisonVolume;
		}

		public GlobalModel()
		{
			m_playerExp.ApplyNewCryptoKey();
		}

		public override void PostLoad()
		{
			if (string.IsNullOrEmpty(m_installationGUID))
			{
				m_installationGUID = Guid.NewGuid().ToString();
				Save();
			}
			gameFeaturesRequiredLevels = DependencyInjector.Resolve<BaseModel>().GetSingle<FeatuersLocksInfo>().GetDispatchedLocks();
			foreach (KeyValuePair<GameGlobalFeature, int> gameFeaturesRequiredLevel in gameFeaturesRequiredLevels)
			{
				m_gameFeaturesDefaultRequiredLevels[gameFeaturesRequiredLevel.Key] = gameFeaturesRequiredLevel.Value;
			}
			if (DependencyInjector.Resolve<CompensationModel>().IsNeedCompensateQuaranta(lastCarId))
			{
				lastCarId = 3;
			}
			GameEvents.OnGameEngineStart.Subscribe(GameEventFlag.OneCall, delegate
			{
				TryUnlockGameFeaturesRequiredLevels();
			});
		}

		private void UpdateDriverVisibility()
		{
			if ((bool)PlayerCarControl.instance)
			{
				RaceCar car = PlayerCarControl.instance.car;
				if ((bool)car)
				{
					car.UpdateDriverVisibility();
				}
			}
		}

		private void UpdateWindowVisibility()
		{
			if ((bool)PlayerCarControl.instance)
			{
				RaceCar car = PlayerCarControl.instance.car;
				if ((bool)car)
				{
					car.UpdateWindowsVisability();
				}
			}
		}

		protected override bool CompareRecovePrefs(BasePrefsModel reserveModel)
		{
			if (!(reserveModel is GlobalModel globalModel))
			{
				return true;
			}
			return playerExp == globalModel.playerExp;
		}

		public void TryUnlockGameFeaturesRequiredLevels()
		{
			if (!Overlay.IsReady() || !Overlay.instance.IsDLCInstalled(DLCCode.FreeContent))
			{
				return;
			}
			foreach (GameGlobalFeature value in Enum.GetValues(typeof(GameGlobalFeature)))
			{
				gameFeaturesRequiredLevels[value] = 1;
			}
		}

		public int GetLockLevelForFeature(GameGlobalFeature feature, int carId = -1)
		{
			if (Overlay.IsReady() && Overlay.instance.IsDLCInstalled(DLCCode.FreeContent))
			{
				if (gameFeaturesRequiredLevels[feature] != 1)
				{
					TryUnlockGameFeaturesRequiredLevels();
				}
				return gameFeaturesRequiredLevels[feature];
			}
			if (TryGetLockException(out var result))
			{
				return result;
			}
			return m_gameFeaturesDefaultRequiredLevels[feature];
			bool TryGetLockException(out int reference)
			{
				reference = 0;
				if (m_unlockedCarExceptionFeatures.TryGetValue((carId > 0) ? carId : lastCarId, out var value))
				{
					return value.Contains(feature);
				}
				return false;
			}
		}

		public void Apply()
		{
			ApplyCamEffects();
			ApplySoundFXVolume();
			ApplyAmbienceVolume();
		}

		private void ApplyCamEffects()
		{
			int num = m_fov;
			if (num < 1)
			{
				num = 60;
			}
			int num2 = m_cockpitFov;
			if (num2 < 1)
			{
				num2 = 60;
			}
			BaseCamera[] array = UnityEngine.Object.FindObjectsOfType<BaseCamera>();
			if (array == null)
			{
				return;
			}
			BaseCamera[] array2 = array;
			foreach (BaseCamera baseCamera in array2)
			{
				CARXRearCamera cARXRearCamera = baseCamera as CARXRearCamera;
				if ((bool)cARXRearCamera)
				{
					cARXRearCamera.effectIsOn = camDynamic;
					cARXRearCamera.shakeCamOn = highRevsShakeCam;
				}
				CARXCockpitCamera cARXCockpitCamera = baseCamera as CARXCockpitCamera;
				if ((bool)cARXCockpitCamera)
				{
					cARXCockpitCamera.velocityScale = dynamicCamParams.velocityScale;
					cARXCockpitCamera.cameraSmooth = dynamicCamParams.cameraSmooth;
					cARXCockpitCamera.accelSmooth = dynamicCamParams.accelSmooth;
					cARXCockpitCamera.dynamicCameraMode = dynamicCamParams.cockpitCamMode;
					cARXCockpitCamera.maxRotAngle = dynamicCamParams.maxRotAngle;
					cARXCockpitCamera.maxRotSpeed = dynamicCamParams.maxRotSpeed;
					cARXCockpitCamera.useAccel = dynamicCamParams.moveWithAccel;
					cARXCockpitCamera.dynamicCamDeadZone = dynamicCamParams.dynamicCamDeadZone;
					cARXCockpitCamera.dynamicCameraAimingMode = dynamicCamParams.camAimMode;
					baseCamera.SetFOV(num2);
				}
				else
				{
					baseCamera.SetFOV(num);
				}
			}
		}

		private void ApplySoundFXVolume()
		{
			SoundController.SetSoundFXVolume(soundFXVolume);
		}

		private void ApplyAmbienceVolume()
		{
			SoundController.SetSoundAmbienceVolume(ambienceVolume);
		}
	}
}
