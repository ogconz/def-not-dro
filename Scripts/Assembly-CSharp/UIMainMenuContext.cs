using System;
using System.Collections.Generic;
using Binding;
using Binding.Base;
using Binding.Components;
using Customization;
using DB;
using DB.Meta;
using DI;
using GameInput;
using GameOverlay;
using PrefsModels;
using Services.UGC.Interfaces;
using Services.UGC.Utils;
using SharingLiveries;
using SharingSetups;
using SyncMultiplayer;
using UnityEngine;

[AudioFXBlocker(false)]
public class UIMainMenuContext : BaseBindingBehaviourTarget
{
	private const string CATEGORY_PARAM = "#mainmenu_category";

	private const string TICKET_SINGLEPLAYER_PARAM = "#mainmenu_singleplayer_ticket";

	private const string TICKET_MULTIPLAYER_PARAM = "#mainmenu_multiplayer_ticket";

	private const string TICKET_SHARING_PARAM = "#mainmenu_sharing_ticket";

	private const string DISCORD_URL = "https://discord.gg/bXpFeuU";

	private const int SingleplayerIndex = 0;

	private const int MultyplayerIndex = 1;

	public const int SharingIndex = 2;

	public const int SettingsIndex = 3;

	public const int PatchnodeIndex = 4;

	public const int MenuItemsCount = 5;

	public readonly Property<int> indexCategory = new IntProperty(0);

	public readonly BoolProperty showTrackSharing = new BoolProperty();

	public readonly BoolProperty showCarSharing = new BoolProperty();

	public readonly Property<string> gameVersion = new StringProperty();

	public readonly Property<string> eventsTimer = new StringProperty();

	public readonly Property<bool> eventsLocked = new BoolProperty();

	private bool m_initializedSelection;

	[Dependency]
	private CarVisualManager m_carVisualManager;

	[SerializeField]
	private Transform m_singleplayerContainer;

	[SerializeField]
	private Transform m_multiplayerContainer;

	[SerializeField]
	private Transform m_settingsStandaloneContainer;

	[SerializeField]
	private Transform m_settingsConsoleContainer;

	[SerializeField]
	private Transform m_sharingContainer;

	[SerializeField]
	private Transform m_sharingContainerWOTracks;

	[SerializeField]
	private VisibilityBoolBinding m_visibilitySettingsBoolBinding;

	[SerializeField]
	private VisibilityBoolBinding m_visibilitySharingBoolBinding;

	private ElementNavigate[] m_singleplayerNavigates;

	private ElementNavigate[] m_multiplayerNavigates;

	private ElementNavigate[] m_sharingNavigates;

	private ElementNavigate[] m_settingsNavigates;

	private int m_lastSingleplayerTicket;

	private int m_lastMultiplayerTicket;

	private int m_lastSettingTicket;

	private int m_lastSharingTicket;

	private bool m_nightModeActive;

	[SerializeField]
	private int m_modsInfoShowTimes;

	private static bool m_menuShown;

	[NonSerialized]
	private UISettingsButtonsPanelContext m_settingsBtnsPanel;

	public SoundSample soundSwitchPages = SoundSample.SwitchPages;

	[Dependency]
	private GamePrefs m_prefs;

	[Dependency]
	private BaseModel m_baseModel;

	[Dependency]
	private PlayerCarFactory m_playerCarFactory;

	[ContextDependency]
	private GarageCar3dViewContext m_garageContext;

	private MainMenuCamera m_menuCamera;

	private bool initializedMainCamera;

	public bool isDayNightTimeoutOver
	{
		get
		{
			if (m_dayNightController != null)
			{
				return m_dayNightController.isTimeoutToChangeModeOver;
			}
			return false;
		}
	}

	private DayNightController m_dayNightController => DayNightController.instance;

	public RaceCar userCar => m_garageContext.currentCar;

	private void Awake()
	{
		if (m_singleplayerContainer != null)
		{
			m_singleplayerNavigates = m_singleplayerContainer.GetComponentsInChildren<ElementNavigate>();
		}
		if (GameConsole.Int["#mainmenu_category"] > 1)
		{
			GameConsole.Int["#mainmenu_category"] = 0;
		}
		m_settingsNavigates = m_settingsStandaloneContainer.GetComponentsInChildren<ElementNavigate>();
		m_multiplayerNavigates = m_multiplayerContainer.GetComponentsInChildren<ElementNavigate>();
		m_sharingNavigates = m_sharingContainer.GetComponentsInChildren<ElementNavigate>();
		showTrackSharing.value = true;
		showCarSharing.value = true;
		m_visibilitySettingsBoolBinding.reference = 3.0;
		m_visibilitySharingBoolBinding.reference = 2.0;
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		Overlay.instance.richPresence.Show(RichPresenceStatus.MainMenu);
		if (m_menuCamera == null)
		{
			m_menuCamera = UnityEngine.Object.FindObjectOfType<MainMenuCamera>();
		}
		UpdateValues();
		m_prefs.compensationModel.ObtainCompensation(showUIMessage: false, m_prefs, m_baseModel).Forget();
		UISetupsSharingContext.TestDriveResetProfileIfNeed(m_prefs.globals.lastCarId, m_prefs.carSettings);
		UISetupsSharingContext.ClearTestDriveParams();
		m_carVisualManager.ClearVisualIfNeed(m_prefs.globals.lastCarId);
		m_garageContext.Set3DModelVisible(value: true);
		m_garageContext.SelectCarById(m_prefs.globals.lastCarId);
		m_garageContext.CreateDlcCarsIfNeeded();
		indexCategory.OnValueChanged -= OnChangeCategory;
		indexCategory.OnValueChanged += OnChangeCategory;
		if (base.owner.IsActive<UIBackgroundContext>())
		{
			base.owner.Hide<UIBackgroundContext>();
		}
		m_nightModeActive = m_prefs.globals.nightModeActive;
		SetDayNightMode(!m_nightModeActive);
		UpdateTopAndBottomPanels();
		if (m_menuCamera != null)
		{
			m_menuCamera.StartCamera();
		}
		RestoreSelection();
		if ((bool)CameraRotation.instance)
		{
			CameraRotation.instance.useZoom = true;
		}
		m_garageContext.SetCarsSound(enable: false);
		ContextOwner contextOwner = base.owner;
		contextOwner.onTopContextChanged = (Action<BaseContext>)Delegate.Remove(contextOwner.onTopContextChanged, new Action<BaseContext>(CheckTopContext));
		ContextOwner contextOwner2 = base.owner;
		contextOwner2.onTopContextChanged = (Action<BaseContext>)Delegate.Combine(contextOwner2.onTopContextChanged, new Action<BaseContext>(CheckTopContext));
		BasePrefsModel.LockSaves(flag: false);
		if (!UIPatchNotesContext.enteredInGame)
		{
			UIPatchNotesContext.enteredInGame = true;
			if (UIPatchNotesContext.CheckVersionAndShowPatchNote())
			{
				m_topPanelContext.switcherActions.SelectionAction(4);
			}
		}
		if (!m_menuShown)
		{
			m_menuShown = true;
			m_prefs.globals.modsNotificationShown = 0;
		}
	}

	protected override void OnDeactivate()
	{
		UIPromotionContext.HideWidgetHandler();
		m_garageContext.SetCarsSound(enable: true);
		BasePrefsModel.LockSaves(flag: true);
		SaveSelection();
		if (m_menuCamera != null)
		{
			m_menuCamera.StopCamera();
		}
		if ((bool)CameraRotation.instance)
		{
			CameraRotation.instance.useZoom = true;
		}
		UIPatchNotesContext uIPatchNotesContext = base.owner.Get<UIPatchNotesContext>();
		if ((bool)uIPatchNotesContext && uIPatchNotesContext.isActive)
		{
			uIPatchNotesContext.Hide();
		}
		base.OnDeactivate();
	}

	private void Update()
	{
		if (m_menuCamera == null && !initializedMainCamera)
		{
			m_menuCamera = DependencyInjector.Resolve<MainMenuCamera>();
			initializedMainCamera = true;
			if (m_menuCamera != null)
			{
				m_menuCamera.enabled = true;
			}
		}
		if (!m_initializedSelection)
		{
			RestoreTickets();
			m_initializedSelection = true;
		}
		UpdateEventsTimer();
	}

	private void UpdateBottomPanelForPatchNote()
	{
		List<ActionHintData> list = new List<ActionHintData>();
		list.Add(new ActionHintData
		{
			hint = LocalizationKeys.Close.Localized(),
			type = ActionType.NavCancelBtnB,
			action = BackToSingleMenu
		});
		list.Add(new ActionHintData
		{
			hint = (m_nightModeActive ? LocalizationKeys.nav_hint_day.Localized() : LocalizationKeys.nav_hint_night.Localized()),
			type = ActionType.NavAdditionalPrevBtnL2,
			action = ToggleDayNightMode
		});
		ShowBottomPanel(list.ToArray());
	}

	private void UpdateTopAndBottomPanels()
	{
		List<HeaderActionData> list = new List<HeaderActionData>
		{
			new HeaderActionData
			{
				text = LocalizationKeys.screen_title_singleplayer.Localized().ToUpper(),
				action = OnSwitchToSingleplayer
			},
			new HeaderActionData
			{
				text = LocalizationKeys.screen_title_multiplayer.Localized().ToUpper(),
				action = OnSwitchToMultiplayer
			},
			new HeaderActionData
			{
				text = LocalizationKeys.sharing_menu_title.Localized().ToUpper(),
				action = OnSwitchToSharing
			},
			new HeaderActionData
			{
				text = LocalizationKeys.screen_title_settings.Localized().ToUpper(),
				action = OnSwitchToSettings,
				viewType = HeaderActionBinding.ViewType.AsSettingsIcon
			}
		};
		if (UIPatchNotesContext.CheckPatchNoteLocalization())
		{
			list.Add(new HeaderActionData
			{
				text = LocalizationKeys.whats_new.Localized().ToUpper(),
				action = OnSwitchToPatchNotes,
				viewType = HeaderActionBinding.ViewType.AsPatchnoteIcon
			});
		}
		ShowTopPanel(ETopPanelState.Actions, null, list.ToArray());
		m_topPanelContext.switcherActions.SelectionAction(3);
		UpdateBottomPanel();
	}

	public void UpdateBottomPanel()
	{
		List<ActionHintData> list = new List<ActionHintData>();
		if (!IsExitDisabled())
		{
			list.Add(new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_exit.Localized(),
				type = ActionType.NavCancelBtnB,
				action = Exit
			});
		}
		list.Add(new ActionHintData
		{
			hint = LocalizationKeys.nav_hint_select.Localized(),
			type = ActionType.NavSelectBtnA,
			action = SubmitInput
		});
		if (NetworkController.ReconnectFailed)
		{
			list.Add(new ActionHintData
			{
				hint = LocalizationKeys.change_server.Localized(),
				type = ActionType.NavBtnExpandRight,
				action = NavigationChangeServerRegion
			});
		}
		list.Add(new ActionHintData
		{
			hint = (m_nightModeActive ? LocalizationKeys.nav_hint_day.Localized() : LocalizationKeys.nav_hint_night.Localized()),
			type = ActionType.NavAdditionalPrevBtnL2,
			action = ToggleDayNightMode
		});
		ShowBottomPanel(list.ToArray());
	}

	private void ToggleDayNightMode()
	{
		if (isDayNightTimeoutOver)
		{
			m_nightModeActive = !m_nightModeActive;
			SetDayNightMode(!m_nightModeActive);
			m_prefs.globals.nightModeActive = m_nightModeActive;
			if (indexCategory.value == 4)
			{
				UpdateBottomPanelForPatchNote();
			}
			else
			{
				UpdateBottomPanel();
			}
		}
	}

	private void SetDayNightMode(bool isDay)
	{
		if (!(m_dayNightController == null))
		{
			m_dayNightController.SetDayNightMode(isDay);
			UpdateHeadlights(!isDay);
		}
	}

	private void UpdateHeadlights(bool lightsEnabled)
	{
		RaceCar currentCar = m_garageContext.currentCar;
		if (!(currentCar == null) && currentCar.headlightsEnabled != lightsEnabled && currentCar.engineController.isIgnitionOn)
		{
			currentCar.SetHeadlightsEnabled(lightsEnabled, ignoreIgnition: true);
		}
	}

	private void CheckTopContext(BaseContext prevContext)
	{
		if (base.owner.topContext == this && indexCategory.value == 3 && m_settingsBtnsPanel != null)
		{
			m_settingsBtnsPanel.RegisterPanel();
			UpdateTopAndBottomPanels();
		}
	}

	public static bool IsExitDisabled()
	{
		return false;
	}

	private void UpdateValues()
	{
		if (m_prefs == null)
		{
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
		}
		indexCategory.value = 0;
		if (m_garageContext == null)
		{
			m_garageContext = DependencyInjector.Resolve<GarageCar3dViewContext>();
		}
		if (m_playerCarFactory == null)
		{
			m_playerCarFactory = DependencyInjector.Resolve<PlayerCarFactory>();
		}
		gameVersion.value = GameVersion.versionAsString;
		gameVersion.value += " moddable";
	}

	private void SaveSelection()
	{
		if (indexCategory.value <= 5)
		{
			GameConsole.Int["#mainmenu_category"] = indexCategory.value;
		}
		GameConsole.Int["#mainmenu_singleplayer_ticket"] = m_lastSingleplayerTicket;
		GameConsole.Int["#mainmenu_multiplayer_ticket"] = m_lastMultiplayerTicket;
		GameConsole.Int["#mainmenu_sharing_ticket"] = m_lastSharingTicket;
		GameConsole.Save();
	}

	private void RestoreSelection()
	{
		int value = GameConsole.Int["#mainmenu_category"];
		value = Mathf.Clamp(value, 0, 5);
		m_topPanelContext.switcherActions.SelectionAction(value);
		m_lastSingleplayerTicket = GameConsole.Int["#mainmenu_singleplayer_ticket"];
		m_lastSingleplayerTicket = Mathf.Clamp(m_lastSingleplayerTicket, 0, 5);
		m_lastMultiplayerTicket = GameConsole.Int["#mainmenu_multiplayer_ticket"];
		m_lastMultiplayerTicket = Mathf.Clamp(m_lastMultiplayerTicket, 0, 5);
		m_lastSharingTicket = GameConsole.Int["#mainmenu_sharing_ticket"];
		m_lastSharingTicket = Mathf.Clamp(m_lastSharingTicket, 0, 5);
		ObtainSettingsPanelIfNeed();
		AddSettingsBtnsPanelPostEventIfNeed();
		RestoreTickets();
	}

	private void RestoreTickets()
	{
		switch (indexCategory.value)
		{
		case 0:
			if (m_singleplayerNavigates.Length != 0)
			{
				if (base.selectedItem != null)
				{
					base.selectedNavigate.Deselect();
				}
				if (m_lastSingleplayerTicket >= 0 && m_lastSingleplayerTicket < m_singleplayerNavigates.Length)
				{
					m_singleplayerNavigates[m_lastSingleplayerTicket].Select();
				}
			}
			else
			{
				GameLog.Print(AlertLevel.Error, "MainMenu", "Array of singleplayer navigate elements is empty");
			}
			break;
		case 1:
			if (m_multiplayerNavigates.Length != 0)
			{
				if (base.selectedItem != null)
				{
					base.selectedNavigate.Deselect();
				}
				if (m_lastMultiplayerTicket >= 0 && m_lastMultiplayerTicket < m_multiplayerNavigates.Length)
				{
					m_multiplayerNavigates[m_lastMultiplayerTicket].Select();
				}
			}
			else
			{
				GameLog.Print(AlertLevel.Error, "MainMenu", "Array of multiplayer navigate elements is empty");
			}
			break;
		case 3:
			if (m_settingsNavigates.Length != 0)
			{
				if (m_settingsBtnsPanel.selectedItem != null)
				{
					m_settingsBtnsPanel.selectedNavigate.Deselect();
				}
				if (m_lastSettingTicket >= 0)
				{
					m_settingsNavigates[m_lastSettingTicket].Select();
				}
			}
			else
			{
				GameLog.Print(AlertLevel.Error, "MainMenu", "Array of settings navigate elements is empty");
			}
			break;
		case 4:
		{
			UIPatchNotesContext uIPatchNotesContext = base.owner.Get<UIPatchNotesContext>();
			if (!uIPatchNotesContext || !uIPatchNotesContext.isActive)
			{
				base.owner.Add<UIPatchNotesContext>();
			}
			break;
		}
		case 2:
			if (m_sharingNavigates.Length != 0)
			{
				if (base.selectedItem != null)
				{
					base.selectedNavigate.Deselect();
				}
				if (m_lastSharingTicket >= 0 && m_lastSharingTicket < m_sharingNavigates.Length)
				{
					m_sharingNavigates[m_lastSharingTicket].Select();
				}
			}
			else
			{
				GameLog.Print(AlertLevel.Error, "MainMenu", "Array of multiplayer navigate elements is empty");
			}
			break;
		}
	}

	private void AddSettingsBtnsPanelPostEventIfNeed()
	{
		if (!(m_settingsBtnsPanel == null))
		{
			m_settingsBtnsPanel.UnsubscribePressSettingsButton(SetLastSettingNavigate);
			m_settingsBtnsPanel.SubscribePressSettingsButton(SetLastSettingNavigate);
		}
	}

	private void ObtainSettingsPanelIfNeed()
	{
		if (!(m_settingsBtnsPanel != null))
		{
			m_settingsBtnsPanel = GetComponentInChildren<UISettingsButtonsPanelContext>();
			if (!(m_settingsBtnsPanel == null))
			{
				m_settingsBtnsPanel.SetOwner(this);
			}
		}
	}

	private void SetLastSettingNavigate(UISettingsButtonsPanelContext.SettingsButtonType settingsButtonType)
	{
		m_lastSettingTicket = (int)settingsButtonType;
	}

	private int CheckElementSelection(ref ElementNavigate[] navigates, ElementNavigate selection)
	{
		if (navigates != null && navigates.Length != 0)
		{
			for (int i = 0; i < navigates.Length; i++)
			{
				if (navigates[i] == selection)
				{
					return i;
				}
			}
		}
		return -1;
	}

	private void OnChangeCategory()
	{
		m_initializedSelection = false;
	}

	private void SwitchPageTo(int index)
	{
		if (indexCategory.value == 4 && index != 4)
		{
			UIPatchNotesContext uIPatchNotesContext = base.owner.Get<UIPatchNotesContext>();
			if ((bool)uIPatchNotesContext && uIPatchNotesContext.isActive)
			{
				uIPatchNotesContext.Hide();
				UpdateBottomPanel();
			}
		}
		if (m_settingsBtnsPanel != null)
		{
			if (indexCategory.value != 3 && index == 3)
			{
				m_settingsBtnsPanel.RegisterPanel();
			}
			else if (indexCategory.value == 3 && index != 3)
			{
				m_settingsBtnsPanel.UnregisterPanel();
			}
		}
		if (index != 0)
		{
			m_lastSingleplayerTicket = 0;
		}
		if (index != 1)
		{
			m_lastMultiplayerTicket = 0;
		}
		if (index != 3)
		{
			m_lastSettingTicket = 0;
		}
		if (index != 2)
		{
			m_lastSharingTicket = 0;
		}
		SetWidgetVisibilityByIndex(index);
		indexCategory.value = index;
		SoundController.Play2DSound(soundSwitchPages);
		base.owner.Get<UITopPanelContext>().UpdateProfileButton();
	}

	private void SetWidgetVisibilityByIndex(int index)
	{
		if (index == 3 || index == 4)
		{
			UIPromotionContext.HideWidgetHandler();
		}
		else
		{
			UIPromotionContext.ShowWidgetHandler();
		}
	}

	private bool CheckChatVisibleValidateByIndex(int index)
	{
		if (index != 0 && index != 1)
		{
			return index == 2;
		}
		return true;
	}

	public void OnSwitchToSingleplayer()
	{
		SwitchPageTo(0);
	}

	public void OnSwitchToMultiplayer()
	{
		SwitchPageTo(1);
	}

	public void OnSwitchToSettings()
	{
		SwitchPageTo(3);
	}

	public void OnSwitchToSharing()
	{
		SwitchPageTo(2);
	}

	public void OnSwitchToPatchNotes()
	{
		SwitchPageTo(4);
		UpdateBottomPanelForPatchNote();
	}

	public void BringForwardByDepthTopPanel()
	{
		m_topPanelContext.BringForwardByDepth();
	}

	protected override void OnFailedMoveSelection(NavigateDirection direction)
	{
		if (m_topPanelContext != null && m_topPanelContext.isActiveAndEnabled)
		{
			if (direction == NavigateDirection.Right)
			{
				m_topPanelContext.OnSwitchNextPage();
			}
			if (direction == NavigateDirection.Left)
			{
				m_topPanelContext.OnSwitchPrevPage();
			}
		}
	}

	public void OnFailedMoveByNavigation(NavigateDirection direction)
	{
		OnFailedMoveSelection(direction);
	}

	public bool GarageCarsLoaded()
	{
		if (m_garageContext != null)
		{
			return m_garageContext.allCarLoaded;
		}
		return false;
	}

	public void GoToGarage(bool multiplayer)
	{
		if (GarageCarsLoaded())
		{
			HideTopPanel();
			HideBottomPanel();
			if (multiplayer)
			{
				m_lastMultiplayerTicket = CheckElementSelection(ref m_multiplayerNavigates, base.selectedNavigate);
			}
			else
			{
				m_lastSingleplayerTicket = CheckElementSelection(ref m_singleplayerNavigates, base.selectedNavigate);
			}
			Hide();
			base.owner.Push<UIGarageContext>();
		}
	}

	public void HideMainMenuContext()
	{
		Hide();
	}

	public void GoToDriftRaces()
	{
		if (!(GameManager.raceDisabledByPhysExplo_Time > Time.unscaledTime) && GarageCarsLoaded())
		{
			m_lastSingleplayerTicket = CheckElementSelection(ref m_singleplayerNavigates, base.selectedNavigate);
			base.owner.HideStackAndPush<UISelectorTrackContext>().SetRaceMode(RaceMode.Drift);
		}
	}

	public void GoToTimeAttack()
	{
		if (!(GameManager.raceDisabledByPhysExplo_Time > Time.unscaledTime) && GarageCarsLoaded())
		{
			m_lastSingleplayerTicket = CheckElementSelection(ref m_singleplayerNavigates, base.selectedNavigate);
			base.owner.HideStackAndPush<UISelectorTrackContext>().SetRaceMode(RaceMode.TimeAttack);
		}
	}

	public void GoToTraining()
	{
		if (!(GameManager.raceDisabledByPhysExplo_Time > Time.unscaledTime) && GarageCarsLoaded())
		{
			m_lastSingleplayerTicket = CheckElementSelection(ref m_singleplayerNavigates, base.selectedNavigate);
			base.owner.HideStackAndPush<UISelectorTrackContext>().SetRaceMode(RaceMode.Freeride);
		}
	}

	public void GoToHalloweenEvent()
	{
		if (!(GameManager.raceDisabledByPhysExplo_Time > Time.unscaledTime) && !eventsLocked.value)
		{
			m_lastSingleplayerTicket = CheckElementSelection(ref m_singleplayerNavigates, base.selectedNavigate);
			base.owner.HideStackAndPush<UISelectorTrackContext>().SetRaceMode(RaceMode.HalloweenEvent);
		}
	}

	public void GoToXDS()
	{
		if (!(GameManager.raceDisabledByPhysExplo_Time > Time.unscaledTime) && GarageCarsLoaded())
		{
			m_lastSingleplayerTicket = CheckElementSelection(ref m_singleplayerNavigates, base.selectedNavigate);
			base.owner.HideStackAndPush<UISelectorTrackContext>().SetRaceMode(RaceMode.XDS);
		}
	}

	public void GoToRoomsList()
	{
		if (!UIPrivacyPolicyEULAContext.CheckIsEulaAccess())
		{
			UIPrivacyPolicyEULAContext.onEulaClosedResult = (Action<bool>)Delegate.Combine(UIPrivacyPolicyEULAContext.onEulaClosedResult, new Action<bool>(ApprovedGoToRoomsList));
			UIPrivacyPolicyEULAContext.Show();
		}
		else
		{
			ApprovedGoToRoomsList(isApproved: true);
		}
	}

	private void DisabledEnterToSharing(string msg)
	{
		UILockInputContext.DisableLock();
		GUICommonGodVoice.ShowText(msg);
	}

	public void GoToSetupSharing()
	{
		if (GameManager.raceDisabledByPhysExplo_Time > Time.unscaledTime || !GarageCarsLoaded())
		{
			return;
		}
		int profileIdForCar = m_prefs.carSettings.GetProfileIdForCar(m_prefs.globals.lastCarId);
		string text = m_baseModel.GetById<PlayerCarProfile>(profileIdForCar).name;
		if (text == "Stock" || text == "Turbo")
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.sharing_setups_unavailable.Localized(), 3f);
			return;
		}
		m_lastSharingTicket = CheckElementSelection(ref m_sharingNavigates, base.selectedNavigate);
		UILockInputContext.EnableLock();
		GameSync.TryEnterToMPZone(delegate
		{
			UISetupsSharingContext.SnapCurrentCamberToeValues(userCar);
			UISetupsSharingContext.ClearTestDriveParams();
			TimeController.AddCallback(0.1f, delegate
			{
				UILockInputContext.DisableLock();
				Hide();
				base.owner.Push<UISetupsSharingContext>();
			});
		}, DisabledEnterToSharing);
	}

	public void GoToLiveriesSharing()
	{
		if (!GarageCarsLoaded())
		{
			return;
		}
		if (userCar.vinylsNotSupport)
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.vinyls_not_support.Localized(), 3f);
			return;
		}
		if (CarPartDatabase.IsCarBodypartsListContainsUGC(userCar.carId))
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.not_allowed_mod_chassis.Localized(), 3f);
			return;
		}
		m_lastSharingTicket = CheckElementSelection(ref m_sharingNavigates, base.selectedNavigate);
		UILockInputContext.EnableLock();
		GameSync.TryEnterToMPZone(delegate
		{
			UILockInputContext.DisableLock();
			Hide();
			base.owner.Push<UILiveriesSharingContext>();
		}, DisabledEnterToSharing);
	}

	public void GoToTracksSharing()
	{
		TryOpenSharing(IUGCManager.ContentType.Tracks);
	}

	public void GoToCarSharing()
	{
		TryOpenSharing(IUGCManager.ContentType.Cars);
	}

	private void TryOpenSharing(IUGCManager.ContentType ugcServiceType)
	{
		m_lastSharingTicket = CheckElementSelection(ref m_sharingNavigates, base.selectedNavigate);
		UILockInputContext.EnableLock();
		GameSync.TryEnterToMPZone(delegate
		{
			UILockInputContext.DisableLock();
			UIMessageBox.ShowDialog(LocalizationKeys.ugc_browser_select_service_provider_title.Localized(), null, BuildDialogUGCServiceButtons(ugcServiceType));
		}, DisabledEnterToSharing);
	}

	private DialogButtonData[] BuildDialogUGCServiceButtons(IUGCManager.ContentType ugcServiceType)
	{
		IUGCManager modIOManager = UGCServiceProvider.instance.GetManagerBy(ugcServiceType, IUGCManager.ServiceProvider.ModIO);
		IUGCManager steamworkshopManager = UGCServiceProvider.instance.GetManagerBy(ugcServiceType, IUGCManager.ServiceProvider.Steamworkshop);
		return new DialogButtonData[2]
		{
			new DialogButtonData
			{
				action = delegate
				{
					HideAndPushUGCBrowser(modIOManager);
				},
				text = modIOManager.serviceTitle
			},
			new DialogButtonData
			{
				action = delegate
				{
					HideAndPushUGCBrowser(steamworkshopManager);
				},
				text = steamworkshopManager.serviceTitle
			}
		};
	}

	private void HideAndPushUGCBrowser(IUGCManager manager)
	{
		Hide();
		base.owner.Get<UIUGCBrowserContext>().InitUGCManager(manager);
		base.owner.Push<UIUGCBrowserContext>();
	}

	public void GoToChampionship()
	{
		if (!(GameManager.raceDisabledByPhysExplo_Time > Time.unscaledTime))
		{
			if (!UIPrivacyPolicyEULAContext.CheckIsEulaAccess())
			{
				UIPrivacyPolicyEULAContext.onEulaClosedResult = (Action<bool>)Delegate.Combine(UIPrivacyPolicyEULAContext.onEulaClosedResult, new Action<bool>(ApprovedGoToChampionship));
				UIPrivacyPolicyEULAContext.Show();
			}
			else
			{
				ApprovedGoToChampionship(isApproved: true);
			}
		}
	}

	private void ApprovedGoToChampionship(bool isApproved)
	{
		UIPrivacyPolicyEULAContext.onEulaClosedResult = (Action<bool>)Delegate.Remove(UIPrivacyPolicyEULAContext.onEulaClosedResult, new Action<bool>(ApprovedGoToChampionship));
		if (isApproved && GarageCarsLoaded())
		{
			m_lastMultiplayerTicket = CheckElementSelection(ref m_multiplayerNavigates, base.selectedNavigate);
			GUICommonGodVoice.ShowText(LocalizationKeys.top32_disabled_on_mono.Localized(), 5f);
		}
	}

	private void ApprovedGoToRoomsList(bool isApproved)
	{
		UIPrivacyPolicyEULAContext.onEulaClosedResult = (Action<bool>)Delegate.Remove(UIPrivacyPolicyEULAContext.onEulaClosedResult, new Action<bool>(ApprovedGoToRoomsList));
		if (isApproved && GarageCarsLoaded() && !(GameManager.raceDisabledByPhysExplo_Time > Time.unscaledTime))
		{
			m_lastMultiplayerTicket = CheckElementSelection(ref m_multiplayerNavigates, base.selectedNavigate);
			base.owner.Hide<UIBackgroundContext>();
			NetworkController.TryEnterToMultiplayer(base.owner);
		}
	}

	public void GoToDiscord()
	{
		UIMessageBox.ShowQuestion(LocalizationKeys.prevention_discord_message.Localized(), null, LocalizationKeys.Cancel.Localized(), delegate
		{
			Application.OpenURL("https://discord.gg/bXpFeuU");
		}, LocalizationKeys.Yes.Localized());
	}

	public void SubmitInput()
	{
		NavigationInputModule.instance.Submit();
	}

	public void NavigationChangeServerRegion()
	{
		OnChangeServerRegionPress();
	}

	public void OnChangeServerRegionPress()
	{
		UIChangeServerRegionContext.Show(base.owner, lobbyReconnect: false);
	}

	public void Exit()
	{
		if (IsExitDisabled())
		{
			return;
		}
		UIMessageBox.ShowQuestion(LocalizationKeys.exit_question.Localized(), delegate
		{
			if (indexCategory.value == 3 && m_settingsBtnsPanel != null)
			{
				m_settingsBtnsPanel.RegisterPanel();
			}
		}, LocalizationKeys.Cancel.Localized(), delegate
		{
			BasePrefsModel.LockSaves(flag: true);
			SaveSelection();
			if (m_menuCamera != null)
			{
				m_menuCamera.StopCamera();
			}
			KeepAliveManager.Quit();
		}, LocalizationKeys.Exit.Localized());
	}

	public void BackToSingleMenu()
	{
		UpdateBottomPanel();
		m_topPanelContext.switcherActions.SelectionAction(0);
	}

	private void UpdateEventsTimer()
	{
		if (Singleton<GameplayEvents>.instance != null)
		{
			if (Singleton<GameplayEvents>.instance.GetTimeForEndEvent(GameEventCode.Halloween, out var _, out var resultStr, out var locked))
			{
				eventsTimer.value = resultStr;
			}
			eventsLocked.value = locked;
		}
	}
}
