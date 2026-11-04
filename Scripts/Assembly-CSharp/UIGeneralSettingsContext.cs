using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Binding;
using Binding.Base;
using Chat;
using DI;
using GameInput;
using GameOverlay;
using PrefsModels;
using UnityEngine;
using UnityEngine.Analytics;

[RaycastBlocker(true)]
[GameHotkeyBlocker(true)]
[AudioFXBlocker(true)]
public class UIGeneralSettingsContext : BaseBindingBehaviourTarget
{
	private struct ItemParams
	{
		public LocalizationKeys.Key key;

		public bool firstIsOff;

		public int count;

		public Func<int> getter;

		public Action<int> setter;

		public bool requiredToRestart;

		public ItemParams(LocalizationKeys.Key key, bool firstIsOff, int count, Func<int> getter, Action<int> setter, bool requiredToRestart = true)
		{
			this.key = key;
			this.firstIsOff = firstIsOff;
			this.count = count;
			this.getter = getter;
			this.setter = setter;
			this.requiredToRestart = requiredToRestart;
		}
	}

	private enum VolumeTargetType
	{
		Music = 0,
		SoundFX = 1,
		Ambience = 2,
		TargetCar = 3,
		OtherCars = 4,
		SeqTransmission = 5
	}

	private SwitchListSettingsItem m_language;

	private ToggleListSettingsItem m_xr;

	private bool m_languageChanged;

	private ToggleListSettingsItem m_dynamicCam;

	private SwitchListSettingsItem m_dynamicCamCockpit;

	private ToggleListSettingsItem m_highRevsShakeCamera;

	private ToggleListSettingsItem m_showGhost;

	private ToggleListSettingsItem m_showMinimap;

	private ToggleListSettingsItem m_hideDriver;

	private ToggleListSettingsItem m_hideWheel;

	private ToggleListSettingsItem m_hideWindows;

	private ToggleListSettingsItem m_rearviewMirror;

	private ToggleListSettingsItem m_chatSingleplayer;

	private ToggleListSettingsItem m_showNickNames;

	private SwitchListSettingsItem m_speedInKmh;

	private SwitchListSettingsItem m_inclinometerMode;

	private SwitchListSettingsItem m_rotationCamMode;

	private ToggleListSettingsItem m_crossPlatformPlay;

	private ToggleListSettingsItem m_autoConnectToEpicPlatform;

	private ToggleListSettingsItem m_pedalsIndicator;

	private ToggleListSettingsItem m_showDriftCounter;

	private ToggleListSettingsItem m_showKissTheWallHUD;

	private ToggleListSettingsItem m_displayBoostGauge;

	private ToggleListSettingsItem m_syncHeadlights;

	private ButtonListSettingsItem m_privacyPolicy;

	private ButtonListSettingsItem m_eula;

	private SystemLanguage m_prevLanguage = SystemLanguage.Unknown;

	private bool m_hideWheelPrevValue;

	[Dependency]
	protected GraphicsQuality m_graphicsQuality;

	private static ItemParams[] m_itemParamsArray;

	private string NR_POSTFIX = " *";

	private SwitchListSettingsItem m_resolution;

	private ReadOnlyCollection<GraphicSettings.ResolutionInfo> m_resolutions;

	private SwitchListSettingsItem m_fullScreenModes;

	private ToggleListSettingsItem m_vsync;

	private SwitchListSettingsItem m_skidmarkLenght;

	private TrackListSettingsItem m_overallLevel;

	private List<BaseListSettingsItem> m_itemList = new List<BaseListSettingsItem>();

	private float m_overallLevelValue;

	private bool m_applyChanges;

	private TrackListSettingsItem m_vFov;

	private TrackListSettingsItem m_vFovCockpit;

	private ToggleListSettingsItem m_showFPS;

	private ToggleListSettingsItem m_smoke;

	private TrackListSettingsItem m_opponentSmokeOpacity;

	private TrackListSettingsItem m_playerSmokeOpacity;

	private TrackListSettingsItem m_opponentLightOpacity;

	private TrackListSettingsItem m_playerLightOpacity;

	private ToggleListSettingsItem m_showSkidMark;

	private ToggleListSettingsItem m_showSsgi;

	private ToggleListSettingsItem m_ssr;

	private ToggleListSettingsItem m_compressVinyls;

	private const float SOUND_LEVEL_COEFF = 100f;

	private const float SLIDER_STEP = 5f;

	private const string FORMAT = "{0:F0}";

	private TrackListSettingsItem m_masterVolume;

	private TrackListSettingsItem m_musicVolume;

	private TrackListSettingsItem m_soundFXVolume;

	private TrackListSettingsItem m_targetCarVolume;

	private TrackListSettingsItem m_otherCarsVolume;

	private TrackListSettingsItem m_seqTransmissionVolume;

	private TrackListSettingsItem m_ambienceVolume;

	private ToggleListSettingsItem m_padSound;

	private HeaderListSettingsItem m_additionalFXSteeings;

	[Dependency]
	protected GamePrefs m_prefs;

	protected GlobalModel m_globals;

	public Property<bool> languageEnabled = new BoolProperty();

	public Property<string> categoryTitle = new StringProperty("");

	public Property<string> categoryDescription = new StringProperty("");

	public Property<bool> graphicsSettings = new BoolProperty();

	public SettingsGameList gameSettingsList;

	private List<ActionHintData> m_bottomPanelHints = new List<ActionHintData>();

	private int m_currentSettingsIndex;

	private string m_currentCategoryName;

	public SystemLanguage languageType { get; private set; }

	public void ShowGameSettings()
	{
		gameSettingsList.Clear();
		InitLangSettings();
		InitGameSettings();
		gameSettingsList.RecalcList();
		gameSettingsList.selectedIndex = 0;
	}

	private void SaveGameSettings()
	{
		if (m_rotationCamMode != null)
		{
			m_globals.showGhost = m_showGhost.value;
			m_globals.speedKmh = Convert.ToBoolean(m_speedInKmh.index);
			m_globals.showMinimap = m_showMinimap.value;
			m_globals.hideDriver = m_hideDriver.value;
			m_globals.syncHeadlights = m_syncHeadlights.value;
			m_globals.hideWheel = m_hideWheel.value;
			m_globals.hideWindows = m_hideWindows.value;
			m_globals.chatSingleplayer = m_chatSingleplayer.value;
			m_globals.showNickNames = m_showNickNames.value;
			m_globals.isRearviewMirrorVisible = m_rearviewMirror.value;
			m_globals.camDynamic = m_dynamicCam.value;
			if (!IsXRActive())
			{
				m_globals.dynamicCamParams.cockpitCamMode = (GlobalModel.DynamicCockpitCamMode)(m_dynamicCamCockpit.index - 1);
			}
			m_globals.highRevsShakeCam = m_highRevsShakeCamera.value;
			Analytics.initializeOnStartup = true;
			Analytics.CustomEvent("highRevsShakeCam", new Dictionary<string, object> { { "enabled", m_highRevsShakeCamera.value } });
			m_globals.inclinometerMode = (GlobalModel.InclinometerMode)m_inclinometerMode.index;
			m_globals.cameraOrbitStick = m_rotationCamMode.index - 1;
			m_globals.crossPlatformPlay = m_crossPlatformPlay.value;
			m_globals.autoConnectToEpicServices = m_autoConnectToEpicPlatform.value;
			m_globals.pedalsIndicator = m_pedalsIndicator.value;
			m_globals.hideDriftCounter = !m_showDriftCounter.value;
			m_globals.showKissTheWallHUD = m_showKissTheWallHUD.value;
			m_globals.displayBoostGauge = m_displayBoostGauge.value;
			m_prefs.globals.Save();
			if (m_languageChanged)
			{
				OnLanguageChange();
			}
		}
	}

	private void InitLangSettings()
	{
		if (gameSettingsList == null)
		{
			return;
		}
		if (m_language == null)
		{
			m_language = new SwitchListSettingsItem
			{
				title = LocalizationKeys.settings_game_lang.Localized()
			};
			SystemLanguage systemLanguage = LocalizationService.languageMeta.languageType;
			for (int i = 0; i < LocalizationService.allLanguagesMeta.Count; i++)
			{
				LocalizationLanguageMeta localizationLanguageMeta = LocalizationService.allLanguagesMeta[i];
				m_language.list.Add(localizationLanguageMeta.title);
				if (localizationLanguageMeta.languageType == systemLanguage)
				{
					m_language.index = i;
				}
			}
			SwitchListSettingsItem language = m_language;
			language.onChangedValue = (System.Action)Delegate.Combine(language.onChangedValue, (System.Action)delegate
			{
				m_languageChanged = true;
			});
			m_language.GetCurrentValueTitle();
			UIPrivacyPolicyEULAContext.InitializeLanguageDialog();
		}
		gameSettingsList.AddItem(m_language);
	}

	private void InitGameSettings()
	{
		if (m_dynamicCam == null)
		{
			m_dynamicCam = new ToggleListSettingsItem
			{
				title = LocalizationKeys.settings_game_camera_dynamic.Localized()
			};
		}
		m_dynamicCam.value = m_globals.camDynamic;
		gameSettingsList.AddItem(m_dynamicCam);
		if (!IsXRActive())
		{
			if (m_dynamicCamCockpit == null)
			{
				m_dynamicCamCockpit = new SwitchListSettingsItem();
				m_dynamicCamCockpit.title = LocalizationKeys.settings_game_camera_cockpit.Localized();
				m_dynamicCamCockpit.list.AddRange(new string[3]
				{
					LocalizationKeys.control_off.Localized(),
					LocalizationKeys.settings_game_camera_dynamic_mode_legacy.Localized(),
					LocalizationKeys.settings_game_camera_dynamic_mode_improved.Localized()
				});
			}
			m_dynamicCamCockpit.index = (int)(m_globals.dynamicCamParams.cockpitCamMode + 1);
			gameSettingsList.AddItem(m_dynamicCamCockpit);
		}
		if (m_highRevsShakeCamera == null)
		{
			m_highRevsShakeCamera = new ToggleListSettingsItem
			{
				title = LocalizationKeys.settings_game_camera_highrevs_shaking.Localized()
			};
		}
		m_highRevsShakeCamera.value = m_globals.highRevsShakeCam;
		gameSettingsList.AddItem(m_highRevsShakeCamera);
		if (m_showGhost == null)
		{
			m_showGhost = new ToggleListSettingsItem
			{
				title = LocalizationKeys.settings_game_sghost.Localized()
			};
		}
		m_showGhost.value = m_globals.showGhost;
		gameSettingsList.AddItem(m_showGhost);
		if (m_showMinimap == null)
		{
			m_showMinimap = new ToggleListSettingsItem
			{
				title = LocalizationKeys.settings_game_sminimap.Localized()
			};
		}
		m_showMinimap.value = m_globals.showMinimap;
		gameSettingsList.AddItem(m_showMinimap);
		if (m_hideDriver == null)
		{
			m_hideDriver = new ToggleListSettingsItem
			{
				title = LocalizationKeys.input_action_.AddLocalized("hidedriver")
			};
		}
		m_hideDriver.value = m_globals.hideDriver;
		gameSettingsList.AddItem(m_hideDriver);
		if (m_hideWheel == null)
		{
			m_hideWheel = new ToggleListSettingsItem
			{
				title = LocalizationKeys.input_action_.AddLocalized("hidewheel")
			};
			ToggleListSettingsItem hideWheel = m_hideWheel;
			hideWheel.onChangedValue = (System.Action)Delegate.Combine(hideWheel.onChangedValue, new System.Action(OnChangeWheelVisibility));
		}
		m_hideWheel.value = m_globals.hideWheel;
		m_hideWheelPrevValue = m_globals.hideWheel;
		gameSettingsList.AddItem(m_hideWheel);
		if (m_hideWheel.value)
		{
			m_hideDriver.value = true;
		}
		m_hideDriver.SetEditingPermission(!m_hideWheel.value);
		if (m_hideWindows == null)
		{
			m_hideWindows = new ToggleListSettingsItem
			{
				title = LocalizationKeys.settings_game_hidewindows.Localized()
			};
		}
		m_hideWindows.value = m_globals.hideWindows;
		gameSettingsList.AddItem(m_hideWindows);
		if (m_rearviewMirror == null)
		{
			m_rearviewMirror = new ToggleListSettingsItem
			{
				title = LocalizationKeys.rearview_mirror_text.Localized()
			};
		}
		m_rearviewMirror.value = m_globals.isRearviewMirrorVisible;
		gameSettingsList.AddItem(m_rearviewMirror);
		if (m_chatSingleplayer == null)
		{
			m_chatSingleplayer = new ToggleListSettingsItem
			{
				title = LocalizationKeys.settings_game_chat.Localized()
			};
			ToggleListSettingsItem chatSingleplayer = m_chatSingleplayer;
			chatSingleplayer.onChangedValue = (System.Action)Delegate.Combine(chatSingleplayer.onChangedValue, new System.Action(OnChangeChatWithEULA));
		}
		m_chatSingleplayer.value = m_globals.chatSingleplayer;
		if (ChatOverlay.IsInitialized())
		{
			gameSettingsList.AddItem(m_chatSingleplayer);
		}
		if (m_showNickNames == null)
		{
			m_showNickNames = new ToggleListSettingsItem
			{
				title = LocalizationKeys.settings_game_show_nicknames.Localized()
			};
		}
		m_showNickNames.value = m_globals.showNickNames;
		gameSettingsList.AddItem(m_showNickNames);
		if (m_speedInKmh == null)
		{
			m_speedInKmh = new SwitchListSettingsItem
			{
				title = LocalizationKeys.settings_game_speed_unit.Localized()
			};
			m_speedInKmh.list.AddRange(new string[2]
			{
				LocalizationKeys.settings_game_speed_unit_mph.Localized(),
				LocalizationKeys.settings_game_speed_unit_kmh.Localized()
			});
		}
		m_speedInKmh.index = (m_globals.speedKmh ? 1 : 0);
		gameSettingsList.AddItem(m_speedInKmh);
		if (m_pedalsIndicator == null)
		{
			m_pedalsIndicator = new ToggleListSettingsItem
			{
				title = LocalizationKeys.settings_game_pedals_indicator.Localized()
			};
		}
		m_pedalsIndicator.value = m_globals.pedalsIndicator;
		gameSettingsList.AddItem(m_pedalsIndicator);
		if (m_showDriftCounter == null)
		{
			m_showDriftCounter = new ToggleListSettingsItem
			{
				title = LocalizationKeys.settings_game_show_drift_hud.Localized()
			};
		}
		m_showDriftCounter.value = !m_globals.hideDriftCounter;
		gameSettingsList.AddItem(m_showDriftCounter);
		if (m_showKissTheWallHUD == null)
		{
			m_showKissTheWallHUD = new ToggleListSettingsItem
			{
				title = LocalizationKeys.settings_game_show_kissthewall_hud.Localized()
			};
		}
		m_showKissTheWallHUD.value = m_globals.showKissTheWallHUD;
		gameSettingsList.AddItem(m_showKissTheWallHUD);
		if (m_displayBoostGauge == null)
		{
			m_displayBoostGauge = new ToggleListSettingsItem
			{
				title = LocalizationKeys.settings_game_display_boost_gauge.Localized()
			};
		}
		m_displayBoostGauge.value = m_globals.displayBoostGauge;
		gameSettingsList.AddItem(m_displayBoostGauge);
		if (m_inclinometerMode == null)
		{
			m_inclinometerMode = new SwitchListSettingsItem
			{
				title = LocalizationKeys.settings_game_inclinometer.Localized()
			};
			m_inclinometerMode.list.AddRange(new string[3]
			{
				LocalizationKeys.settings_game_inclinometer_autohide.Localized(),
				LocalizationKeys.settings_game_inclinometer_off.Localized(),
				LocalizationKeys.settings_game_inclinometer_on.Localized()
			});
		}
		m_inclinometerMode.index = (int)m_globals.inclinometerMode;
		gameSettingsList.AddItem(m_inclinometerMode);
		if (m_rotationCamMode == null)
		{
			m_rotationCamMode = new SwitchListSettingsItem
			{
				title = LocalizationKeys.input_action_freecamview.Localized()
			};
			m_rotationCamMode.list.AddRange(new string[3]
			{
				LocalizationKeys.camera_orbit_stick_disable.Localized(),
				LocalizationKeys.camera_orbit_stick_left.Localized(),
				LocalizationKeys.camera_orbit_stick_right.Localized()
			});
		}
		m_rotationCamMode.index = m_globals.cameraOrbitStick + 1;
		gameSettingsList.AddItem(m_rotationCamMode);
		bool flag = true;
		if (m_crossPlatformPlay == null)
		{
			m_crossPlatformPlay = new ToggleListSettingsItem
			{
				title = LocalizationKeys.settings_game_cross_platform_play.Localized()
			};
		}
		m_crossPlatformPlay.value = m_globals.crossPlatformPlay;
		if (WebConfigManager.ServerAllowCrossPlatform && flag)
		{
			gameSettingsList.AddItem(m_crossPlatformPlay);
		}
		if (m_autoConnectToEpicPlatform == null)
		{
			m_autoConnectToEpicPlatform = new ToggleListSettingsItem
			{
				title = LocalizationKeys.settings_game_auto_connect_to_epic.Localized()
			};
		}
		m_autoConnectToEpicPlatform.value = m_globals.autoConnectToEpicServices;
		gameSettingsList.AddItem(m_autoConnectToEpicPlatform);
		if (m_syncHeadlights == null)
		{
			m_syncHeadlights = new ToggleListSettingsItem
			{
				title = LocalizationKeys.input_action_.AddLocalized("sync_headlights")
			};
		}
		m_syncHeadlights.value = m_globals.syncHeadlights;
		gameSettingsList.AddItem(m_syncHeadlights);
		if (m_privacyPolicy == null)
		{
			m_privacyPolicy = new ButtonListSettingsItem
			{
				title = LocalizationKeys.eula_privacy_policy_title.Localized()
			};
			ButtonListSettingsItem privacyPolicy = m_privacyPolicy;
			privacyPolicy.onSubmit = (System.Action)Delegate.Combine(privacyPolicy.onSubmit, new System.Action(UIPrivacyPolicyEULAContext.OpenPrivacyPolicy));
		}
		gameSettingsList.AddItem(m_privacyPolicy);
		if (m_eula == null)
		{
			m_eula = new ButtonListSettingsItem
			{
				title = LocalizationKeys.eula_licence_agreegment_title.Localized()
			};
			ButtonListSettingsItem eula = m_eula;
			eula.onSubmit = (System.Action)Delegate.Combine(eula.onSubmit, new System.Action(UIPrivacyPolicyEULAContext.OpenEULA));
		}
		gameSettingsList.AddItem(m_eula);
	}

	private void OnChangeWheelVisibility()
	{
		if (m_hideWheelPrevValue || m_hideWheel.value)
		{
			m_hideDriver.value = m_hideWheel.value;
		}
		m_hideWheelPrevValue = m_hideWheel.value;
		m_hideDriver.SetEditingPermission(!m_hideWheel.value);
		gameSettingsList.UpdateList();
	}

	public void ResetGameSettings()
	{
		UIMessageBox.ShowQuestion(LocalizationKeys.dyno_ask_reset_all_to_defaults.Localized(), delegate
		{
		}, LocalizationKeys.Cancel.Localized(), delegate
		{
			m_globals.ResetGameSettings();
			ShowGameSettings();
		}, LocalizationKeys.Ok.Localized());
	}

	private void SelectPreviousLanguage()
	{
		if (m_prevLanguage != SystemLanguage.Unknown)
		{
			LocalizationService.SetLanguage(m_prevLanguage, null, restorePreviousLanguage: true);
		}
	}

	private void SetIndexOfPrevLanguage()
	{
		for (int i = 0; i < LocalizationService.allLanguagesMeta.Count; i++)
		{
			if (LocalizationService.allLanguagesMeta[i].languageType == m_prevLanguage)
			{
				m_language.index = i;
			}
		}
	}

	private void OnLanguageChange()
	{
		SystemLanguage systemLanguage = LocalizationService.languageMeta.languageType;
		if (m_language.index < 0 || m_language.index >= LocalizationService.allLanguagesMeta.Count || systemLanguage == LocalizationService.allLanguagesMeta[m_language.index].languageType)
		{
			return;
		}
		SystemLanguage langType = LocalizationService.allLanguagesMeta[m_language.index].languageType;
		m_prevLanguage = LocalizationService.languageMeta.languageType;
		UIMessageBox.ShowQuestion(LocalizationKeys.apply_changes_and_restart.Localized(), SetIndexOfPrevLanguage, LocalizationKeys.Cancel.Localized(), delegate
		{
			UIWaitDialogContext.ShowPleaseWait(base.owner);
			LocalizationService.SetLanguage(langType, delegate(bool successful)
			{
				UIWaitDialogContext.Close(base.owner);
				if (successful)
				{
					GameManager.GameRestart();
				}
				else
				{
					UIMessageBox.ShowHint("", LocalizationKeys.no_server_connection.Localized());
					SelectPreviousLanguage();
					GameManager.GameRestart();
				}
			});
		}, LocalizationKeys.Ok.Localized());
	}

	private void OnXRModeChanged()
	{
		if (m_xr.value)
		{
			XRModeManager instance = XRModeManager.instance;
			instance.OnXRSubsystemDeactivated = (System.Action)Delegate.Remove(instance.OnXRSubsystemDeactivated, new System.Action(ForceTurnOffXRMode));
			XRModeManager instance2 = XRModeManager.instance;
			instance2.OnXRSubsystemDeactivated = (System.Action)Delegate.Combine(instance2.OnXRSubsystemDeactivated, new System.Action(ForceTurnOffXRMode));
			m_globals.dynamicCamParams.cockpitCamMode = GlobalModel.DynamicCockpitCamMode.OFF;
			m_resolution.SetBlockToEditing(value: true);
			gameSettingsList.RecalcList();
		}
		m_globals.XRActivated = m_xr.value;
		XRModeManager.instance.SetXRModeActive(m_xr.value);
	}

	private void ForceTurnOffXRMode()
	{
		m_xr.value = false;
		m_globals.XRActivated = false;
		m_resolution.SetBlockToEditing(value: false);
		ShowGraphicsSettings();
	}

	public static void ResetVSyncIfNeed()
	{
		GraphicsQuality graphicsQuality = DependencyInjector.Resolve<GraphicsQuality>();
		if (graphicsQuality.settings.vSyncPrevVal.HasValue)
		{
			graphicsQuality.vSync = graphicsQuality.settings.vSyncPrevVal.Value;
			graphicsQuality.settings.vSyncPrevVal = null;
		}
	}

	public void ShowGraphicsSettings()
	{
		gameSettingsList.Clear();
		m_applyChanges = false;
		if (m_itemParamsArray == null)
		{
			m_itemParamsArray = new ItemParams[9]
			{
				new ItemParams(LocalizationKeys.settings_graphics_antialiasing, firstIsOff: false, 4, () => m_graphicsQuality.antiAliasing, delegate(int i)
				{
					m_graphicsQuality.antiAliasing = i;
				}),
				new ItemParams(LocalizationKeys.settings_graphics_texturequality, firstIsOff: false, 4, () => m_graphicsQuality.textureQuality, delegate(int i)
				{
					m_graphicsQuality.textureQuality = i;
				}),
				new ItemParams(LocalizationKeys.settings_graphics_anisolevel, firstIsOff: false, 4, () => m_graphicsQuality.anisoLevel, delegate(int i)
				{
					m_graphicsQuality.anisoLevel = i;
				}),
				new ItemParams(LocalizationKeys.settings_graphics_lod, firstIsOff: false, 4, () => m_graphicsQuality.lod, delegate(int i)
				{
					m_graphicsQuality.lod = i;
				}),
				new ItemParams(LocalizationKeys.settings_graphics_volumetricfog, firstIsOff: true, 2, () => m_graphicsQuality.volumetricFog, delegate(int i)
				{
					m_graphicsQuality.volumetricFog = i;
				}),
				new ItemParams(LocalizationKeys.settings_graphics_shadows, firstIsOff: false, 4, () => m_graphicsQuality.shadows, delegate(int i)
				{
					m_graphicsQuality.shadows = i;
				}),
				new ItemParams(LocalizationKeys.settings_graphics_ao, firstIsOff: true, 4, () => m_graphicsQuality.ao, delegate(int i)
				{
					m_graphicsQuality.ao = i;
				}),
				new ItemParams(LocalizationKeys.settings_graphics_bloom, firstIsOff: true, 4, () => m_graphicsQuality.bloom, delegate(int i)
				{
					m_graphicsQuality.bloom = i;
				}),
				new ItemParams(LocalizationKeys.settings_graphics_motionblur, firstIsOff: true, 4, () => m_graphicsQuality.motionBlur, delegate(int i)
				{
					m_graphicsQuality.motionBlur = i;
				})
			};
		}
		int width = Screen.width;
		int height = Screen.height;
		FullScreenMode fullScreenMode = Screen.fullScreenMode;
		RefreshFullscreenSetting();
		if (m_resolution == null)
		{
			m_resolution = new SwitchListSettingsItem
			{
				title = LocalizationKeys.settings_graphics_resolution.Localized() + NR_POSTFIX
			};
			m_resolutions = m_graphicsQuality.supportedResolutionList;
			foreach (GraphicSettings.ResolutionInfo resolution4 in m_resolutions)
			{
				m_resolution.list.Add(resolution4.width + "x" + resolution4.height);
			}
		}
		GraphicSettings.ResolutionInfo resolution = new GraphicSettings.ResolutionInfo(width, height);
		m_resolution.index = (int)m_graphicsQuality.GetResolutionIndex(resolution);
		SwitchListSettingsItem resolution2 = m_resolution;
		resolution2.onChangedValue = (System.Action)Delegate.Remove(resolution2.onChangedValue, new System.Action(OnChangeGraphicsSettings));
		SwitchListSettingsItem resolution3 = m_resolution;
		resolution3.onChangedValue = (System.Action)Delegate.Combine(resolution3.onChangedValue, new System.Action(OnChangeGraphicsSettings));
		gameSettingsList.AddItem(m_resolution);
		if (m_fullScreenModes == null)
		{
			m_fullScreenModes = new SwitchListSettingsItem
			{
				title = LocalizationKeys.settings_graphics_screenMode.Localized() + NR_POSTFIX
			};
			m_fullScreenModes.list.Add(LocalizationKeys.settings_graphics_windowed.Localized());
			m_fullScreenModes.list.Add(LocalizationKeys.settings_graphics_fullscreen.Localized());
			m_fullScreenModes.list.Add(LocalizationKeys.settings_graphics_borderlessWindowed.Localized());
		}
		m_fullScreenModes.index = m_graphicsQuality.GetScreenModeUISwtcherIndex(fullScreenMode);
		SwitchListSettingsItem fullScreenModes = m_fullScreenModes;
		fullScreenModes.onChangedValue = (System.Action)Delegate.Remove(fullScreenModes.onChangedValue, new System.Action(OnChangeGraphicsSettings));
		SwitchListSettingsItem fullScreenModes2 = m_fullScreenModes;
		fullScreenModes2.onChangedValue = (System.Action)Delegate.Combine(fullScreenModes2.onChangedValue, new System.Action(OnChangeGraphicsSettings));
		gameSettingsList.AddItem(m_fullScreenModes);
		BaseState mainCurrent = States.mainCurrent;
		bool flag = mainCurrent is DynoRaceModeState || mainCurrent is DynoMenuState;
		XRModeManager instance = XRModeManager.instance;
		if (instance != null && !flag)
		{
			if (m_xr == null)
			{
				m_xr = new ToggleListSettingsItem
				{
					title = LocalizationKeys.vr_mode_title.Localized()
				};
				ToggleListSettingsItem xr = m_xr;
				xr.onChangedValue = (System.Action)Delegate.Combine(xr.onChangedValue, new System.Action(OnXRModeChanged));
			}
			m_xr.value = instance.isHMDWearing;
			if (m_xr.value)
			{
				m_resolution.SetBlockToEditing(value: true);
				instance.OnXRSubsystemDeactivated = (System.Action)Delegate.Remove(instance.OnXRSubsystemDeactivated, new System.Action(ForceTurnOffXRMode));
				instance.OnXRSubsystemDeactivated = (System.Action)Delegate.Combine(instance.OnXRSubsystemDeactivated, new System.Action(ForceTurnOffXRMode));
			}
			gameSettingsList.AddItem(m_xr);
		}
		if (m_vFov == null)
		{
			m_vFov = new TrackListSettingsItem(20f, 120f, 1f)
			{
				title = LocalizationKeys.settings_graphics_vfov.Localized()
			};
		}
		m_vFov.value = m_prefs.globals.fov;
		TrackListSettingsItem vFov = m_vFov;
		vFov.onChangedValue = (System.Action)Delegate.Remove(vFov.onChangedValue, new System.Action(OnVFovChanged));
		TrackListSettingsItem vFov2 = m_vFov;
		vFov2.onChangedValue = (System.Action)Delegate.Combine(vFov2.onChangedValue, new System.Action(OnVFovChanged));
		gameSettingsList.AddItem(m_vFov);
		if (m_vFovCockpit == null)
		{
			m_vFovCockpit = new TrackListSettingsItem(20f, 120f, 1f)
			{
				title = LocalizationKeys.settings_graphics_vfov_cockpit.Localized()
			};
		}
		m_vFovCockpit.value = m_prefs.globals.cockpitFov;
		TrackListSettingsItem vFovCockpit = m_vFovCockpit;
		vFovCockpit.onChangedValue = (System.Action)Delegate.Remove(vFovCockpit.onChangedValue, new System.Action(OnVFovCockpitChanged));
		TrackListSettingsItem vFovCockpit2 = m_vFovCockpit;
		vFovCockpit2.onChangedValue = (System.Action)Delegate.Combine(vFovCockpit2.onChangedValue, new System.Action(OnVFovCockpitChanged));
		if (!IsXRActive())
		{
			gameSettingsList.AddItem(m_vFovCockpit);
		}
		if (m_showFPS == null)
		{
			m_showFPS = new ToggleListSettingsItem
			{
				title = LocalizationKeys.settings_graphics_fps.Localized()
			};
		}
		m_showFPS.value = m_prefs.globals.showFPSSetting;
		ToggleListSettingsItem showFPS = m_showFPS;
		showFPS.onChangedValue = (System.Action)Delegate.Remove(showFPS.onChangedValue, new System.Action(OnChangeShowFPS));
		ToggleListSettingsItem showFPS2 = m_showFPS;
		showFPS2.onChangedValue = (System.Action)Delegate.Combine(showFPS2.onChangedValue, new System.Action(OnChangeShowFPS));
		gameSettingsList.AddItem(m_showFPS);
		if (m_smoke == null)
		{
			m_smoke = new ToggleListSettingsItem
			{
				title = LocalizationKeys.settings_graphics_smoke.Localized()
			};
		}
		m_smoke.value = m_graphicsQuality.smokeEnabled;
		ToggleListSettingsItem smoke = m_smoke;
		smoke.onChangedValue = (System.Action)Delegate.Remove(smoke.onChangedValue, new System.Action(OnSmokeLevelChange));
		ToggleListSettingsItem smoke2 = m_smoke;
		smoke2.onChangedValue = (System.Action)Delegate.Combine(smoke2.onChangedValue, new System.Action(OnSmokeLevelChange));
		gameSettingsList.AddItem(m_smoke);
		if (m_opponentSmokeOpacity == null)
		{
			m_opponentSmokeOpacity = new TrackListSettingsItem(0f, 1f, 0.01f, "{0:F2}")
			{
				title = LocalizationKeys.settings_graphics_opponent_smoke_opacity.Localized()
			};
		}
		m_opponentSmokeOpacity.value = m_graphicsQuality.opponentSmokeOpacity;
		TrackListSettingsItem opponentSmokeOpacity = m_opponentSmokeOpacity;
		opponentSmokeOpacity.onChangedValue = (System.Action)Delegate.Remove(opponentSmokeOpacity.onChangedValue, new System.Action(OnOpponentSmokeOpacityChange));
		TrackListSettingsItem opponentSmokeOpacity2 = m_opponentSmokeOpacity;
		opponentSmokeOpacity2.onChangedValue = (System.Action)Delegate.Combine(opponentSmokeOpacity2.onChangedValue, new System.Action(OnOpponentSmokeOpacityChange));
		gameSettingsList.AddItem(m_opponentSmokeOpacity);
		if (m_playerSmokeOpacity == null)
		{
			m_playerSmokeOpacity = new TrackListSettingsItem(0f, 1f, 0.01f, "{0:F2}")
			{
				title = LocalizationKeys.settings_graphics_player_smoke_opacity.Localized()
			};
		}
		m_playerSmokeOpacity.value = m_graphicsQuality.playerSmokeOpacity;
		TrackListSettingsItem playerSmokeOpacity = m_playerSmokeOpacity;
		playerSmokeOpacity.onChangedValue = (System.Action)Delegate.Remove(playerSmokeOpacity.onChangedValue, new System.Action(OnPlayerSmokeOpacityChange));
		TrackListSettingsItem playerSmokeOpacity2 = m_playerSmokeOpacity;
		playerSmokeOpacity2.onChangedValue = (System.Action)Delegate.Combine(playerSmokeOpacity2.onChangedValue, new System.Action(OnPlayerSmokeOpacityChange));
		gameSettingsList.AddItem(m_playerSmokeOpacity);
		if (m_opponentLightOpacity == null)
		{
			m_opponentLightOpacity = new TrackListSettingsItem(0.01f, 1f, 0.01f, "{0:F2}")
			{
				title = LocalizationKeys.settings_graphics_opponent_light_opacity.Localized()
			};
		}
		m_opponentLightOpacity.value = m_graphicsQuality.lightOpponentPower;
		TrackListSettingsItem opponentLightOpacity = m_opponentLightOpacity;
		opponentLightOpacity.onChangedValue = (System.Action)Delegate.Remove(opponentLightOpacity.onChangedValue, new System.Action(OnOpponentLightOpacityChange));
		TrackListSettingsItem opponentLightOpacity2 = m_opponentLightOpacity;
		opponentLightOpacity2.onChangedValue = (System.Action)Delegate.Combine(opponentLightOpacity2.onChangedValue, new System.Action(OnOpponentLightOpacityChange));
		gameSettingsList.AddItem(m_opponentLightOpacity);
		if (m_playerLightOpacity == null)
		{
			m_playerLightOpacity = new TrackListSettingsItem(0.01f, 1f, 0.01f, "{0:F2}")
			{
				title = LocalizationKeys.settings_graphics_player_light_opacity.Localized()
			};
		}
		m_playerLightOpacity.value = m_graphicsQuality.lightPlayerPower;
		TrackListSettingsItem playerLightOpacity = m_playerLightOpacity;
		playerLightOpacity.onChangedValue = (System.Action)Delegate.Remove(playerLightOpacity.onChangedValue, new System.Action(OnPlayerLightOpacityChange));
		TrackListSettingsItem playerLightOpacity2 = m_playerLightOpacity;
		playerLightOpacity2.onChangedValue = (System.Action)Delegate.Combine(playerLightOpacity2.onChangedValue, new System.Action(OnPlayerLightOpacityChange));
		gameSettingsList.AddItem(m_playerLightOpacity);
		string value = "SyncNetFreerideRaceModeState";
		if (!States.mainCurrent.name.Contains(value))
		{
			if (m_compressVinyls == null)
			{
				m_compressVinyls = new ToggleListSettingsItem
				{
					title = LocalizationKeys.settings_graphics_enable_vinyl_compression.Localized()
				};
			}
			m_compressVinyls.value = m_graphicsQuality.compressVinyls;
			ToggleListSettingsItem compressVinyls = m_compressVinyls;
			compressVinyls.onChangedValue = (System.Action)Delegate.Remove(compressVinyls.onChangedValue, new System.Action(OnVinylCompressionModeChange));
			ToggleListSettingsItem compressVinyls2 = m_compressVinyls;
			compressVinyls2.onChangedValue = (System.Action)Delegate.Combine(compressVinyls2.onChangedValue, new System.Action(OnVinylCompressionModeChange));
			gameSettingsList.AddItem(m_compressVinyls);
		}
		if (m_vsync == null)
		{
			m_vsync = new ToggleListSettingsItem
			{
				title = "VSYNC" + NR_POSTFIX
			};
		}
		m_vsync.value = m_graphicsQuality.vSync > 0;
		ToggleListSettingsItem vsync = m_vsync;
		vsync.onChangedValue = (System.Action)Delegate.Remove(vsync.onChangedValue, new System.Action(OnVSyncChanged));
		ToggleListSettingsItem vsync2 = m_vsync;
		vsync2.onChangedValue = (System.Action)Delegate.Combine(vsync2.onChangedValue, new System.Action(OnVSyncChanged));
		gameSettingsList.AddItem(m_vsync);
		if (m_skidmarkLenght == null)
		{
			m_skidmarkLenght = new SwitchListSettingsItem
			{
				title = LocalizationKeys.settings_graphics_skidmark_length.Localized(),
				description = LocalizationKeys.settings_graphics_skidmark_description.Localized()
			};
		}
		m_skidmarkLenght.list.Clear();
		m_skidmarkLenght.list.Add(LocalizationKeys.control_off.Localized());
		m_skidmarkLenght.list.Add(LocalizationKeys.settings_graphics_quality_low.Localized());
		m_skidmarkLenght.list.Add(LocalizationKeys.settings_graphics_quality_normal.Localized());
		m_skidmarkLenght.list.Add(LocalizationKeys.settings_graphics_quality_high.Localized());
		m_skidmarkLenght.index = m_graphicsQuality.skidmarkLength;
		SwitchListSettingsItem skidmarkLenght = m_skidmarkLenght;
		skidmarkLenght.onChangedValue = (System.Action)Delegate.Remove(skidmarkLenght.onChangedValue, new System.Action(OnSkidmarkChanged));
		SwitchListSettingsItem skidmarkLenght2 = m_skidmarkLenght;
		skidmarkLenght2.onChangedValue = (System.Action)Delegate.Combine(skidmarkLenght2.onChangedValue, new System.Action(OnSkidmarkChanged));
		gameSettingsList.AddItem(m_skidmarkLenght);
		if (m_showSsgi == null)
		{
			m_showSsgi = new ToggleListSettingsItem
			{
				title = "SSGI"
			};
		}
		m_showSsgi.value = m_graphicsQuality.ssgi > 0;
		ToggleListSettingsItem showSsgi = m_showSsgi;
		showSsgi.onChangedValue = (System.Action)Delegate.Remove(showSsgi.onChangedValue, new System.Action(OnSsgiChanged));
		ToggleListSettingsItem showSsgi2 = m_showSsgi;
		showSsgi2.onChangedValue = (System.Action)Delegate.Combine(showSsgi2.onChangedValue, new System.Action(OnSsgiChanged));
		gameSettingsList.AddItem(m_showSsgi);
		if (m_ssr == null)
		{
			m_ssr = new ToggleListSettingsItem
			{
				title = LocalizationKeys.settings_graphics_ssr_description.Localized()
			};
		}
		m_ssr.value = m_graphicsQuality.ssrLevel > 0;
		ToggleListSettingsItem ssr = m_ssr;
		ssr.onChangedValue = (System.Action)Delegate.Remove(ssr.onChangedValue, new System.Action(OnSsrChanged));
		ToggleListSettingsItem ssr2 = m_ssr;
		ssr2.onChangedValue = (System.Action)Delegate.Combine(ssr2.onChangedValue, new System.Action(OnSsrChanged));
		gameSettingsList.AddItem(m_ssr);
		if (m_overallLevel == null)
		{
			m_overallLevel = new TrackListSettingsItem(0f, 3f, 1f, "")
			{
				title = LocalizationKeys.settings_graphics_quality.Localized() + NR_POSTFIX
			};
		}
		m_overallLevelValue = m_graphicsQuality.GetOverallLevel();
		m_overallLevel.SetTrackValue(m_overallLevelValue);
		TrackListSettingsItem overallLevel = m_overallLevel;
		overallLevel.onChangedValue = (System.Action)Delegate.Remove(overallLevel.onChangedValue, new System.Action(ChangeOverallLevel));
		TrackListSettingsItem overallLevel2 = m_overallLevel;
		overallLevel2.onChangedValue = (System.Action)Delegate.Combine(overallLevel2.onChangedValue, new System.Action(ChangeOverallLevel));
		gameSettingsList.AddItem(m_overallLevel);
		m_itemList.Clear();
		for (int num = 0; num < m_itemParamsArray.Length; num++)
		{
			m_itemList.Add(CreateItem(m_itemParamsArray[num]));
		}
		gameSettingsList.AddRange(m_itemList);
		SettingsGameList settingsGameList = gameSettingsList;
		settingsGameList.onChangeSelectedIndex = (System.Action)Delegate.Combine(settingsGameList.onChangeSelectedIndex, new System.Action(OnChangeSelectedIndex));
		gameSettingsList.RecalcList();
		gameSettingsList.selectedIndex = 0;
	}

	private void RefreshFullscreenSetting()
	{
		m_graphicsQuality.fullscreen = (int)Screen.fullScreenMode;
	}

	private void OnChangeSelectedIndex()
	{
		BaseListSettingsItem baseListSettingsItem = gameSettingsList.GetSelectedItem();
		categoryDescription.value = (string.IsNullOrWhiteSpace(baseListSettingsItem?.description) ? LocalizationKeys.settings_category__description.FormatLocalized("graphics") : baseListSettingsItem.description);
	}

	private BaseListSettingsItem CreateItem(ItemParams item)
	{
		string text = (item.requiredToRestart ? NR_POSTFIX : string.Empty);
		if (item.firstIsOff && item.count == 2)
		{
			ToggleListSettingsItem obj = new ToggleListSettingsItem
			{
				title = item.key.Localized() + text,
				value = (item.getter() > 0)
			};
			obj.onChangedValue = (System.Action)Delegate.Combine(obj.onChangedValue, new System.Action(OnChangeGraphicsSettings));
			return obj;
		}
		if (item.count > 1)
		{
			SwitchListSettingsItem switchListSettingsItem = new SwitchListSettingsItem
			{
				title = item.key.Localized() + text
			};
			if (item.firstIsOff)
			{
				switchListSettingsItem.list.Add(LocalizationKeys.control_off.Localized());
				switchListSettingsItem.list.Add(LocalizationKeys.settings_graphics_quality_low.Localized());
				if (item.count > 2)
				{
					switchListSettingsItem.list.Add(LocalizationKeys.settings_graphics_quality_normal.Localized());
				}
				if (item.count > 3)
				{
					switchListSettingsItem.list.Add(LocalizationKeys.settings_graphics_quality_high.Localized());
				}
			}
			else
			{
				switchListSettingsItem.list.Add(LocalizationKeys.settings_graphics_quality_low.Localized());
				switchListSettingsItem.list.Add(LocalizationKeys.settings_graphics_quality_normal.Localized());
				if (item.count > 2)
				{
					switchListSettingsItem.list.Add(LocalizationKeys.settings_graphics_quality_high.Localized());
				}
				if (item.count > 3)
				{
					switchListSettingsItem.list.Add(LocalizationKeys.settings_graphics_quality_veryhigh.Localized());
				}
			}
			switchListSettingsItem.index = item.getter();
			switchListSettingsItem.onChangedValue = (System.Action)Delegate.Combine(switchListSettingsItem.onChangedValue, new System.Action(OnChangeGraphicsSettings));
			return switchListSettingsItem;
		}
		return null;
	}

	private void SaveGraphicsSettings()
	{
		ShowQuitDialog();
		SwitchListSettingsItem resolution = m_resolution;
		resolution.onChangedValue = (System.Action)Delegate.Remove(resolution.onChangedValue, new System.Action(OnChangeGraphicsSettings));
		SwitchListSettingsItem fullScreenModes = m_fullScreenModes;
		fullScreenModes.onChangedValue = (System.Action)Delegate.Remove(fullScreenModes.onChangedValue, new System.Action(OnChangeGraphicsSettings));
		ToggleListSettingsItem vsync = m_vsync;
		vsync.onChangedValue = (System.Action)Delegate.Remove(vsync.onChangedValue, new System.Action(OnVSyncChanged));
		TrackListSettingsItem overallLevel = m_overallLevel;
		overallLevel.onChangedValue = (System.Action)Delegate.Remove(overallLevel.onChangedValue, new System.Action(ChangeOverallLevel));
		int i = 0;
		for (int count = m_itemList.Count; i < count; i++)
		{
			BaseListSettingsItem baseListSettingsItem = m_itemList[i];
			baseListSettingsItem.onChangedValue = (System.Action)Delegate.Remove(baseListSettingsItem.onChangedValue, new System.Action(OnChangeGraphicsSettings));
		}
		TrackListSettingsItem vFov = m_vFov;
		vFov.onChangedValue = (System.Action)Delegate.Remove(vFov.onChangedValue, new System.Action(OnVFovChanged));
		TrackListSettingsItem vFovCockpit = m_vFovCockpit;
		vFovCockpit.onChangedValue = (System.Action)Delegate.Remove(vFovCockpit.onChangedValue, new System.Action(OnVFovCockpitChanged));
		ToggleListSettingsItem showFPS = m_showFPS;
		showFPS.onChangedValue = (System.Action)Delegate.Remove(showFPS.onChangedValue, new System.Action(OnChangeShowFPS));
		ToggleListSettingsItem smoke = m_smoke;
		smoke.onChangedValue = (System.Action)Delegate.Remove(smoke.onChangedValue, new System.Action(OnSmokeLevelChange));
		TrackListSettingsItem opponentSmokeOpacity = m_opponentSmokeOpacity;
		opponentSmokeOpacity.onChangedValue = (System.Action)Delegate.Remove(opponentSmokeOpacity.onChangedValue, new System.Action(OnOpponentSmokeOpacityChange));
		TrackListSettingsItem playerSmokeOpacity = m_playerSmokeOpacity;
		playerSmokeOpacity.onChangedValue = (System.Action)Delegate.Remove(playerSmokeOpacity.onChangedValue, new System.Action(OnPlayerSmokeOpacityChange));
		TrackListSettingsItem playerLightOpacity = m_playerLightOpacity;
		playerLightOpacity.onChangedValue = (System.Action)Delegate.Remove(playerLightOpacity.onChangedValue, new System.Action(OnPlayerLightOpacityChange));
		TrackListSettingsItem opponentLightOpacity = m_opponentLightOpacity;
		opponentLightOpacity.onChangedValue = (System.Action)Delegate.Remove(opponentLightOpacity.onChangedValue, new System.Action(OnOpponentLightOpacityChange));
		TrackListSettingsItem playerSmokeOpacity2 = m_playerSmokeOpacity;
		playerSmokeOpacity2.onChangedValue = (System.Action)Delegate.Remove(playerSmokeOpacity2.onChangedValue, new System.Action(OnVinylCompressionModeChange));
	}

	private void ResetGraphicsSettings()
	{
		UIMessageBox.ShowQuestion(LocalizationKeys.dyno_ask_reset_all_to_defaults.Localized(), delegate
		{
		}, LocalizationKeys.Cancel.Localized(), delegate
		{
			m_globals.ResetGraphicsSettings();
			m_graphicsQuality.settings.ResetGraphicSettings(out m_applyChanges);
			ShowGraphicsSettings();
			ApplyChanges();
		}, LocalizationKeys.Ok.Localized());
	}

	private void ApplyChanges()
	{
		ProcessItemListSetters();
		m_graphicsQuality.fullscreen = m_graphicsQuality.GetScreenModeIndex((GraphicsQuality.AllowedWindowModeOptions)m_fullScreenModes.index);
		m_graphicsQuality.resolution = GetResolution();
		DependencyInjector.Resolve<GraphicsQuality>().ApplySettings();
	}

	private void ShowQuitDialog()
	{
		if (m_applyChanges)
		{
			bool num = CheckNeedReboot();
			ApplyChanges();
			if (num)
			{
				UIMessageBox.ShowQuestion(LocalizationKeys.apply_changes_and_restart.Localized(), null, LocalizationKeys.Cancel.Localized(), GameManager.GameRestart, LocalizationKeys.Ok.Localized());
			}
		}
	}

	private bool CheckNeedReboot()
	{
		for (int i = 0; i < m_itemList.Count; i++)
		{
			bool flag = false;
			BaseListSettingsItem baseListSettingsItem = m_itemList[i];
			if (baseListSettingsItem is ToggleListSettingsItem toggleListSettingsItem)
			{
				if (toggleListSettingsItem.value != m_itemParamsArray[i].getter() > 0)
				{
					flag = true;
				}
			}
			else if (baseListSettingsItem is SwitchListSettingsItem switchListSettingsItem && switchListSettingsItem.index != m_itemParamsArray[i].getter())
			{
				flag = true;
			}
			if (flag && m_itemParamsArray[i].requiredToRestart)
			{
				return true;
			}
		}
		return false;
	}

	private GraphicSettings.ResolutionInfo GetResolution()
	{
		int num = m_resolution.index;
		if (num >= m_graphicsQuality.settings.supportedResolutionsList.Count)
		{
			num = m_graphicsQuality.settings.supportedResolutionsList.Count - 1;
		}
		GraphicSettings.ResolutionInfo resolutionInfo = m_graphicsQuality.settings.supportedResolutionsList[num];
		return new GraphicSettings.ResolutionInfo(resolutionInfo.width, resolutionInfo.height);
	}

	private void ProcessItemListSetters()
	{
		int i = 0;
		for (int count = m_itemList.Count; i < count; i++)
		{
			BaseListSettingsItem baseListSettingsItem = m_itemList[i];
			if (baseListSettingsItem is ToggleListSettingsItem toggleListSettingsItem)
			{
				m_itemParamsArray[i].setter(toggleListSettingsItem.value ? 1 : 0);
			}
			else if (baseListSettingsItem is SwitchListSettingsItem switchListSettingsItem)
			{
				m_itemParamsArray[i].setter(switchListSettingsItem.index);
			}
		}
	}

	private void OnVSyncChanged()
	{
		m_graphicsQuality.vSync = (m_vsync.value ? 1 : 0);
		if (m_vsync.value && InputManager.CheckIsNeedDisableVSync())
		{
			m_vsync.value = false;
		}
	}

	private void ChangeOverallLevel()
	{
		float normalizedValue = m_overallLevel.normalizedValue;
		if (Math.Abs(normalizedValue - m_overallLevelValue) < 0.001f)
		{
			return;
		}
		for (int i = 0; i < m_itemList.Count; i++)
		{
			BaseListSettingsItem baseListSettingsItem = m_itemList[i];
			if (baseListSettingsItem is ToggleListSettingsItem toggleListSettingsItem)
			{
				toggleListSettingsItem.value = Mathf.RoundToInt(normalizedValue) > 0;
			}
			else if (baseListSettingsItem is SwitchListSettingsItem switchListSettingsItem)
			{
				switchListSettingsItem.index = Mathf.RoundToInt(normalizedValue * (float)(switchListSettingsItem.list.Count - 1));
			}
		}
		OnChangeGraphicsSettings();
	}

	private void OnChangeGraphicsSettings()
	{
		float num = 0f;
		for (int i = 0; i < m_itemList.Count; i++)
		{
			BaseListSettingsItem baseListSettingsItem = m_itemList[i];
			if (baseListSettingsItem is ToggleListSettingsItem toggleListSettingsItem)
			{
				num += (float)(toggleListSettingsItem.value ? 1 : 0);
			}
			else if (baseListSettingsItem is SwitchListSettingsItem switchListSettingsItem)
			{
				num += (float)switchListSettingsItem.index / (float)(switchListSettingsItem.list.Count - 1);
			}
		}
		num /= (float)m_itemList.Count;
		m_overallLevelValue = num;
		m_overallLevel.SetTrackValue(m_overallLevelValue);
		gameSettingsList.UpdateList();
		m_applyChanges = true;
	}

	private void OnVFovChanged()
	{
		m_prefs.globals.fov = Mathf.RoundToInt(m_vFov.value);
	}

	private void OnVFovCockpitChanged()
	{
		m_prefs.globals.cockpitFov = Mathf.RoundToInt(m_vFovCockpit.value);
	}

	private void OnChangeShowFPS()
	{
		bool value = m_showFPS.value;
		if (m_prefs.globals.showFPSSetting != value)
		{
			m_prefs.globals.showFPSSetting = value;
		}
	}

	private void OnSmokeLevelChange()
	{
		m_graphicsQuality.smokeEnabled = m_smoke.value;
	}

	private void OnOpponentSmokeOpacityChange()
	{
		m_graphicsQuality.opponentSmokeOpacity = m_opponentSmokeOpacity.value;
	}

	private void OnPlayerSmokeOpacityChange()
	{
		m_graphicsQuality.playerSmokeOpacity = m_playerSmokeOpacity.value;
	}

	private void OnPlayerLightOpacityChange()
	{
		m_graphicsQuality.playerLightOpacity = m_playerLightOpacity.value;
	}

	private void OnOpponentLightOpacityChange()
	{
		m_graphicsQuality.opponentLightOpacity = m_opponentLightOpacity.value;
	}

	private void OnSsgiChanged()
	{
		m_graphicsQuality.ssgi = (m_showSsgi.value ? 1 : 0);
	}

	private void OnSkidmarkChanged()
	{
		m_graphicsQuality.skidmarkLength = m_skidmarkLenght.index;
	}

	private void OnSsrChanged()
	{
		m_graphicsQuality.ssrLevel = (m_ssr.value ? 1 : 0);
	}

	private void OnVinylCompressionModeChange()
	{
		m_graphicsQuality.compressVinyls = m_compressVinyls.value;
	}

	public void ShowSoundsSettings()
	{
		gameSettingsList.Clear();
		if (m_masterVolume == null)
		{
			m_masterVolume = new TrackListSettingsItem(0f, 100f, 5f)
			{
				title = LocalizationKeys.settings_sound_master.Localized()
			};
		}
		m_masterVolume.value = m_globals.masterVolume * 100f;
		TrackListSettingsItem masterVolume = m_masterVolume;
		masterVolume.onChangedValue = (System.Action)Delegate.Remove(masterVolume.onChangedValue, new System.Action(OnChangeMasterSettings));
		TrackListSettingsItem masterVolume2 = m_masterVolume;
		masterVolume2.onChangedValue = (System.Action)Delegate.Combine(masterVolume2.onChangedValue, new System.Action(OnChangeMasterSettings));
		gameSettingsList.AddItem(m_masterVolume);
		if (m_musicVolume == null)
		{
			m_musicVolume = new TrackListSettingsItem(0f, 100f, 5f)
			{
				title = LocalizationKeys.settings_sound_music.Localized()
			};
		}
		m_musicVolume.value = GetInitSliderVal(VolumeTargetType.Music);
		TrackListSettingsItem musicVolume = m_musicVolume;
		musicVolume.onChangedValue = (System.Action)Delegate.Remove(musicVolume.onChangedValue, new System.Action(OnChangeSoundsSettings));
		TrackListSettingsItem musicVolume2 = m_musicVolume;
		musicVolume2.onChangedValue = (System.Action)Delegate.Combine(musicVolume2.onChangedValue, new System.Action(OnChangeSoundsSettings));
		gameSettingsList.AddItem(m_musicVolume);
		if (m_soundFXVolume == null)
		{
			m_soundFXVolume = new TrackListSettingsItem(0f, 100f, 5f)
			{
				title = LocalizationKeys.settings_sound_sound_fx.Localized()
			};
		}
		m_soundFXVolume.value = GetInitSliderVal(VolumeTargetType.SoundFX);
		TrackListSettingsItem soundFXVolume = m_soundFXVolume;
		soundFXVolume.onChangedValue = (System.Action)Delegate.Remove(soundFXVolume.onChangedValue, new System.Action(OnChangeSoundsSettings));
		TrackListSettingsItem soundFXVolume2 = m_soundFXVolume;
		soundFXVolume2.onChangedValue = (System.Action)Delegate.Combine(soundFXVolume2.onChangedValue, new System.Action(OnChangeSoundsSettings));
		gameSettingsList.AddItem(m_soundFXVolume);
		if (m_additionalFXSteeings == null)
		{
			m_additionalFXSteeings = new HeaderListSettingsItem
			{
				title = LocalizationKeys.settings_sound_additional_fx.Localized()
			};
		}
		gameSettingsList.AddItem(m_additionalFXSteeings);
		if (m_ambienceVolume == null)
		{
			m_ambienceVolume = new TrackListSettingsItem(0f, 100f, 5f)
			{
				title = LocalizationKeys.settings_sound_ambience_volume.Localized()
			};
		}
		m_ambienceVolume.value = GetInitSliderVal(VolumeTargetType.Ambience);
		TrackListSettingsItem ambienceVolume = m_ambienceVolume;
		ambienceVolume.onChangedValue = (System.Action)Delegate.Remove(ambienceVolume.onChangedValue, new System.Action(OnChangeAmbienceVolumeSettings));
		TrackListSettingsItem ambienceVolume2 = m_ambienceVolume;
		ambienceVolume2.onChangedValue = (System.Action)Delegate.Combine(ambienceVolume2.onChangedValue, new System.Action(OnChangeAmbienceVolumeSettings));
		gameSettingsList.AddItem(m_ambienceVolume);
		if (m_targetCarVolume == null)
		{
			m_targetCarVolume = new TrackListSettingsItem(0f, 100f, 5f)
			{
				title = LocalizationKeys.settings_sound_target_car_volume.Localized()
			};
		}
		m_targetCarVolume.value = GetInitSliderVal(VolumeTargetType.TargetCar);
		TrackListSettingsItem targetCarVolume = m_targetCarVolume;
		targetCarVolume.onChangedValue = (System.Action)Delegate.Remove(targetCarVolume.onChangedValue, new System.Action(OnChangeTargetCarVolume));
		TrackListSettingsItem targetCarVolume2 = m_targetCarVolume;
		targetCarVolume2.onChangedValue = (System.Action)Delegate.Combine(targetCarVolume2.onChangedValue, new System.Action(OnChangeTargetCarVolume));
		gameSettingsList.AddItem(m_targetCarVolume);
		if (m_otherCarsVolume == null)
		{
			m_otherCarsVolume = new TrackListSettingsItem(0f, 100f, 5f)
			{
				title = LocalizationKeys.settings_sound_other_cars_volume.Localized()
			};
		}
		m_otherCarsVolume.value = GetInitSliderVal(VolumeTargetType.OtherCars);
		TrackListSettingsItem otherCarsVolume = m_otherCarsVolume;
		otherCarsVolume.onChangedValue = (System.Action)Delegate.Remove(otherCarsVolume.onChangedValue, new System.Action(OnChangeOtherCarsVolume));
		TrackListSettingsItem otherCarsVolume2 = m_otherCarsVolume;
		otherCarsVolume2.onChangedValue = (System.Action)Delegate.Combine(otherCarsVolume2.onChangedValue, new System.Action(OnChangeOtherCarsVolume));
		gameSettingsList.AddItem(m_otherCarsVolume);
		if (m_seqTransmissionVolume == null)
		{
			m_seqTransmissionVolume = new TrackListSettingsItem(0f, 100f, 5f)
			{
				title = LocalizationKeys.settings_sound_seq_transmission_volume.Localized()
			};
		}
		m_seqTransmissionVolume.value = GetInitSliderVal(VolumeTargetType.SeqTransmission);
		TrackListSettingsItem seqTransmissionVolume = m_seqTransmissionVolume;
		seqTransmissionVolume.onChangedValue = (System.Action)Delegate.Remove(seqTransmissionVolume.onChangedValue, new System.Action(OnChangeSeqTransmissionVolume));
		TrackListSettingsItem seqTransmissionVolume2 = m_seqTransmissionVolume;
		seqTransmissionVolume2.onChangedValue = (System.Action)Delegate.Combine(seqTransmissionVolume2.onChangedValue, new System.Action(OnChangeSeqTransmissionVolume));
		gameSettingsList.AddItem(m_seqTransmissionVolume);
		gameSettingsList.RecalcList();
		gameSettingsList.selectedIndex = 0;
	}

	private void OnChangeMasterSettings()
	{
		m_globals.masterVolume = m_masterVolume.normalizedValue;
		foreach (object value in Enum.GetValues(typeof(VolumeTargetType)))
		{
			SetSoundLevel((VolumeTargetType)value);
		}
	}

	private void OnChangeTargetCarVolume()
	{
		SetSoundLevel(VolumeTargetType.TargetCar);
	}

	private void OnChangeOtherCarsVolume()
	{
		SetSoundLevel(VolumeTargetType.OtherCars);
	}

	private void OnChangeSeqTransmissionVolume()
	{
		SetSoundLevel(VolumeTargetType.SeqTransmission);
	}

	private void OnChangeAmbienceVolumeSettings()
	{
		SetSoundLevel(VolumeTargetType.Ambience);
	}

	private void OnChangeSoundsSettings()
	{
		if (m_padSound != null)
		{
			m_globals.padSound = m_padSound.value;
		}
		SetSoundLevel(VolumeTargetType.Music);
		SetSoundLevel(VolumeTargetType.SoundFX);
	}

	private float GetSoundLevel(float normalizedValue)
	{
		return normalizedValue * m_masterVolume.normalizedValue;
	}

	private void SetSoundLevel(VolumeTargetType volumeType)
	{
		switch (volumeType)
		{
		case VolumeTargetType.Music:
		{
			float normalizedValue6 = m_musicVolume.normalizedValue;
			m_globals.musicVolume = GetSoundLevel(normalizedValue6);
			m_globals.musicVolumeSliderVal = normalizedValue6;
			break;
		}
		case VolumeTargetType.SoundFX:
		{
			float normalizedValue5 = m_soundFXVolume.normalizedValue;
			m_globals.soundFXVolume = GetSoundLevel(normalizedValue5);
			m_globals.soundFXVolumeSliderVal = normalizedValue5;
			break;
		}
		case VolumeTargetType.Ambience:
		{
			float normalizedValue4 = m_ambienceVolume.normalizedValue;
			m_globals.ambienceVolume = GetSoundLevel(normalizedValue4);
			m_globals.ambienceVolumeSliderVal = normalizedValue4;
			break;
		}
		case VolumeTargetType.TargetCar:
		{
			float normalizedValue3 = m_targetCarVolume.normalizedValue;
			m_globals.targetCarVolume = GetSoundLevel(normalizedValue3);
			m_globals.targetCarVolumeSliderVal = normalizedValue3;
			break;
		}
		case VolumeTargetType.OtherCars:
		{
			float normalizedValue2 = m_otherCarsVolume.normalizedValue;
			m_globals.otherCarsVolume = GetSoundLevel(normalizedValue2);
			m_globals.otherCarsVolumeSliderVal = normalizedValue2;
			break;
		}
		case VolumeTargetType.SeqTransmission:
		{
			float normalizedValue = m_seqTransmissionVolume.normalizedValue;
			m_globals.seqTransmisonVolume = GetSoundLevel(normalizedValue);
			m_globals.seqTransmisonVolumeSliderVal = normalizedValue;
			break;
		}
		}
		m_globals.Save();
	}

	private void SaveSoundsSettings()
	{
		TrackListSettingsItem musicVolume = m_musicVolume;
		musicVolume.onChangedValue = (System.Action)Delegate.Remove(musicVolume.onChangedValue, new System.Action(OnChangeSoundsSettings));
		TrackListSettingsItem soundFXVolume = m_soundFXVolume;
		soundFXVolume.onChangedValue = (System.Action)Delegate.Remove(soundFXVolume.onChangedValue, new System.Action(OnChangeSoundsSettings));
		if (m_padSound != null)
		{
			ToggleListSettingsItem padSound = m_padSound;
			padSound.onChangedValue = (System.Action)Delegate.Remove(padSound.onChangedValue, new System.Action(OnChangeSoundsSettings));
		}
	}

	public void ResetSoundsSettings()
	{
		UIMessageBox.ShowQuestion(LocalizationKeys.dyno_ask_reset_all_to_defaults.Localized(), delegate
		{
		}, LocalizationKeys.Cancel.Localized(), delegate
		{
			m_globals.ResetSoundsSettings();
			ShowSoundsSettings();
		}, LocalizationKeys.Ok.Localized());
	}

	private float GetInitSliderVal(VolumeTargetType volumeType)
	{
		float num = 0.5f;
		switch (volumeType)
		{
		case VolumeTargetType.Music:
			num = m_globals.musicVolumeSliderVal;
			break;
		case VolumeTargetType.SoundFX:
			num = m_globals.soundFXVolumeSliderVal;
			break;
		case VolumeTargetType.Ambience:
			num = m_globals.ambienceVolumeSliderVal;
			break;
		case VolumeTargetType.TargetCar:
			num = m_globals.targetCarVolumeSliderVal;
			break;
		case VolumeTargetType.OtherCars:
			num = m_globals.otherCarsVolumeSliderVal;
			break;
		case VolumeTargetType.SeqTransmission:
			num = m_globals.seqTransmisonVolumeSliderVal;
			break;
		}
		return num * 100f;
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		m_globals = m_prefs.globals;
		if (!base.owner.IsActive<UIBackgroundContext>())
		{
			base.owner.Add<UIBackgroundContext>();
		}
		BringForwardByDepth();
		UpdateHints(m_currentSettingsIndex);
		UpdateBottomPanel();
		languageEnabled.value = !(States.current is InGameMenuState);
		SettingsGameList settingsGameList = gameSettingsList;
		settingsGameList.onChangeSelectedIndex = (System.Action)Delegate.Combine(settingsGameList.onChangeSelectedIndex, new System.Action(OnChangeSelectedItem));
		gameSettingsList.selectedIndex = 0;
		StartCoroutine(DefferedRecalcList());
	}

	private void OnChangeSelectedItem()
	{
		UpdateDescriptionPanelBySelectedItem();
		UpdateBottomPanel();
	}

	private void UpdateBottomPanel()
	{
		BaseListSettingsItem baseListSettingsItem = gameSettingsList.GetSelectedItem();
		m_bottomPanelHints.Clear();
		if (baseListSettingsItem is ButtonListSettingsItem)
		{
			m_bottomPanelHints.AddRange(new ActionHintData[2]
			{
				new ActionHintData
				{
					hint = LocalizationKeys.nav_hint_goto_back.Localized(),
					type = ActionType.NavCancelBtnB,
					action = NavCancel
				},
				new ActionHintData
				{
					hint = LocalizationKeys.nav_hint_select.Localized(),
					type = ActionType.NavSelectBtnA,
					action = SubmitSelectedButtonSetting
				}
			});
		}
		else
		{
			m_bottomPanelHints.AddRange(new ActionHintData[2]
			{
				new ActionHintData
				{
					hint = LocalizationKeys.nav_hint_goto_back.Localized(),
					type = ActionType.NavCancelBtnB,
					action = NavCancel
				},
				new ActionHintData
				{
					hint = LocalizationKeys.nav_hint_reset.Localized(),
					type = ActionType.NavBtnExpand,
					action = NavReset
				}
			});
		}
		ShowBottomPanel(m_bottomPanelHints.ToArray());
	}

	protected override void OnDeactivate()
	{
		XRModeManager instance = XRModeManager.instance;
		instance.OnXRSubsystemDeactivated = (System.Action)Delegate.Remove(instance.OnXRSubsystemDeactivated, new System.Action(ForceTurnOffXRMode));
		SettingsGameList settingsGameList = gameSettingsList;
		settingsGameList.onChangeSelectedIndex = (System.Action)Delegate.Remove(settingsGameList.onChangeSelectedIndex, new System.Action(UpdateDescriptionPanelBySelectedItem));
		base.owner.Hide<UIBackgroundContext>();
		base.OnDeactivate();
	}

	private void UpdateDescriptionPanelBySelectedItem()
	{
		BaseListSettingsItem baseListSettingsItem = gameSettingsList.GetSelectedItem();
		if (baseListSettingsItem != null)
		{
			string description = baseListSettingsItem.description;
			if (string.IsNullOrEmpty(description))
			{
				categoryDescription.value = GetSettingsDescription(m_currentCategoryName);
			}
			else
			{
				categoryDescription.value = description;
			}
		}
	}

	private IEnumerator DefferedRecalcList()
	{
		yield return new WaitForEndOfFrame();
		gameSettingsList.RecalcList();
		gameSettingsList.ScrollTo(0);
		gameSettingsList.selectedIndex = 0;
	}

	public static void ShowSettings(ContextOwner contextOwner, ETypeSettings typeSettings)
	{
		contextOwner.HideStackAndPush<UIGeneralSettingsContext>().SetSettingsHints((int)typeSettings);
	}

	public static string GetSettingsTitle(string categoryName)
	{
		return LocalizationKeys.settings_category__title.FormatLocalized(categoryName);
	}

	public static string GetSettingsDescription(string categoryName)
	{
		return LocalizationKeys.settings_category__description.FormatLocalized(categoryName);
	}

	private void UpdateHints(string categoryName)
	{
		m_currentCategoryName = categoryName;
		categoryTitle.value = GetSettingsTitle(categoryName);
		categoryDescription.value = GetSettingsDescription(categoryName);
		graphicsSettings.value = categoryName == "graphics";
		ShowTopPanel(ETopPanelState.Title, categoryTitle.value);
	}

	private void SetSettingsHints(int settingsIndex)
	{
		m_currentSettingsIndex = settingsIndex;
		gameSettingsList.Clear();
		switch (settingsIndex)
		{
		case 2:
			ShowGraphicsSettings();
			break;
		case 3:
			ShowSoundsSettings();
			break;
		default:
			ShowGameSettings();
			break;
		}
		foreach (BaseListSettingsItem item in gameSettingsList.items)
		{
			item.onClick = null;
			BaseListSettingsItem baseListSettingsItem = item;
			baseListSettingsItem.onClick = (System.Action)Delegate.Combine(baseListSettingsItem.onClick, (System.Action)delegate
			{
				gameSettingsList.Reselect(item);
			});
		}
		UpdateHints(settingsIndex);
	}

	private bool IsXRActive()
	{
		return XRModeManager.IsXRActive();
	}

	private void UpdateHints(int settingsIndex)
	{
		switch (settingsIndex)
		{
		case 1:
			UpdateHints("steer");
			break;
		case 2:
			UpdateHints("graphics");
			break;
		case 3:
			UpdateHints("sounds");
			break;
		default:
			UpdateHints("game");
			break;
		}
	}

	private void OnChangeChatWithEULA()
	{
		if (!UIPrivacyPolicyEULAContext.CheckIsEulaAccess())
		{
			TrySetActiveChat(value: false);
			UIPrivacyPolicyEULAContext.onEulaClosedResult = (Action<bool>)Delegate.Combine(UIPrivacyPolicyEULAContext.onEulaClosedResult, new Action<bool>(OnEulaClosedResultHandler));
			UIPrivacyPolicyEULAContext.Show();
		}
		else
		{
			TrySetActiveChat(m_chatSingleplayer.value);
		}
	}

	private void OnEulaClosedResultHandler(bool isApproved)
	{
		UIPrivacyPolicyEULAContext.onEulaClosedResult = (Action<bool>)Delegate.Remove(UIPrivacyPolicyEULAContext.onEulaClosedResult, new Action<bool>(OnEulaClosedResultHandler));
		if (isApproved)
		{
			TrySetActiveChat(value: true);
		}
	}

	private void TrySetActiveChat(bool value)
	{
		if (Overlay.instance.IsCommunicationRestricted)
		{
			m_chatSingleplayer.value = false;
			Overlay.instance.ShowCommunicationRestrictionDialog();
		}
		else
		{
			m_chatSingleplayer.value = value;
			gameSettingsList.RecalcList();
		}
	}

	[GameHotkey(ActionType.NavUp, false, true)]
	public void NavigationUp()
	{
		if (gameSettingsList.GetCount() > 0 && gameSettingsList.selectedIndex > 0)
		{
			gameSettingsList.selectedIndex--;
			if (gameSettingsList[gameSettingsList.selectedIndex].GetType() == typeof(HeaderListSettingsItem))
			{
				if (gameSettingsList.selectedIndex == 0)
				{
					gameSettingsList.selectedIndex++;
				}
				else
				{
					gameSettingsList.selectedIndex--;
				}
			}
		}
		gameSettingsList?.UpdateList();
	}

	[GameHotkey(ActionType.NavDown, false, true)]
	public void NavigationDown()
	{
		if (gameSettingsList.GetCount() > 0 && gameSettingsList.selectedIndex + 1 < gameSettingsList.GetCount())
		{
			gameSettingsList.selectedIndex++;
			if (gameSettingsList[gameSettingsList.selectedIndex].GetType() == typeof(HeaderListSettingsItem))
			{
				gameSettingsList.selectedIndex++;
			}
		}
		gameSettingsList?.UpdateList();
	}

	[GameHotkey(ActionType.NavLeft, false, true)]
	public void NavigationLeft()
	{
		int count = gameSettingsList.GetCount();
		if (count != 0 && gameSettingsList.selectedIndex >= 0 && gameSettingsList.selectedIndex < count)
		{
			gameSettingsList.GetSelectedItem().PrevValue();
			gameSettingsList?.UpdateList();
		}
	}

	[GameHotkey(ActionType.NavRight, false, true)]
	public void NavigationRight()
	{
		int count = gameSettingsList.GetCount();
		if (count != 0 && gameSettingsList.selectedIndex >= 0 && gameSettingsList.selectedIndex < count)
		{
			gameSettingsList.GetSelectedItem().NextValue();
			gameSettingsList?.UpdateList();
		}
	}

	public void NavCancel()
	{
		switch (m_currentSettingsIndex)
		{
		case 2:
			SaveGraphicsSettings();
			break;
		case 3:
			SaveSoundsSettings();
			break;
		default:
			SaveGameSettings();
			break;
		case 1:
			break;
		}
		Hide();
		if (States.mainCurrent is GarageGUIState garageGUIState)
		{
			UIMainMenuContext uIMainMenuContext = garageGUIState.contextOwner.Get<UIMainMenuContext>();
			if (uIMainMenuContext != null)
			{
				uIMainMenuContext.OnSwitchToSettings();
			}
		}
	}

	public void NavReset()
	{
		switch (m_currentSettingsIndex)
		{
		case 2:
			ResetGraphicsSettings();
			break;
		case 3:
			ResetSoundsSettings();
			break;
		default:
			ResetGameSettings();
			break;
		case 1:
			break;
		}
	}

	private void SubmitSelectedButtonSetting()
	{
		(gameSettingsList.GetSelectedItem() as ButtonListSettingsItem)?.OnSubmit();
	}
}
