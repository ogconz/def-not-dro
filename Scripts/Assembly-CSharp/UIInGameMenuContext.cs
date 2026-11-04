using System;
using System.Collections.Generic;
using System.Threading;
using Binding;
using Binding.Base;
using Championship;
using DB;
using DB.Meta;
using DI;
using GameInput;
using SharingSetups;
using SyncMultiplayer;

[AudioFXBlocker(true)]
public class UIInGameMenuContext : BaseBindingBehaviourTarget
{
	private const float CarLoadingMsgDuration = 3f;

	private BaseRaceState m_raceState;

	private CancellationTokenSource m_cancellationFetchTokenSource;

	public readonly BoolProperty isFinishRace = new BoolProperty();

	public readonly BoolProperty isVoteRace = new BoolProperty();

	public readonly BoolProperty isDynoMode = new BoolProperty();

	public readonly BoolProperty isChampMode = new BoolProperty();

	public readonly BoolProperty isCommonMode = new BoolProperty();

	public readonly BoolProperty isNoRestartMode = new BoolProperty();

	public readonly BoolProperty isTestDriveMode = new BoolProperty();

	public readonly BoolProperty hasUpdateQuests = new BoolProperty();

	public readonly BoolProperty showQuestsPanel = new BoolProperty();

	public readonly BoolProperty showSwapCarPanel = new BoolProperty();

	public readonly BoolProperty showDynoPanel = new BoolProperty();

	public readonly StringProperty roomPass = new StringProperty();

	public readonly StringProperty exitRace = new StringProperty();

	[Dependency]
	private GamePrefs m_prefs;

	[Dependency]
	private BaseModel m_baseModel;

	protected override void OnActivate()
	{
		base.OnActivate();
		UIBackgroundContext uIBackgroundContext = base.owner.Get<UIBackgroundContext>();
		if ((bool)uIBackgroundContext)
		{
			uIBackgroundContext.Hide();
		}
		m_raceState = States.mainCurrent as BaseRaceState;
		isChampMode.value = false;
		isFinishRace.value = m_raceState is FreerideRaceModeState || m_raceState is NetworkRaceState || isChampMode.value;
		exitRace.value = LocalizationKeys.menu_pause_finish_race.Localized();
		isDynoMode.value = m_raceState is DynoRaceModeState;
		isNoRestartMode.value = m_raceState is NetworkRaceState || m_raceState is SyncNetFreerideRaceModeState;
		isCommonMode.value = !isNoRestartMode.value && !isDynoMode.value;
		isTestDriveMode.value = UISetupsSharingContext.isSetupsSharingActive;
		showQuestsPanel.value = m_raceState != null && !m_raceState.isExternalTrack;
		showSwapCarPanel.value = !isDynoMode.value && m_raceState is FreerideRaceModeState && !isTestDriveMode.value;
		showDynoPanel.value = !isDynoMode.value && m_raceState is FreerideRaceModeState;
		if (m_raceState is FreerideRaceModeState && isCommonMode.value)
		{
			showQuestsPanel.value = false;
		}
		if (States.mainCurrent is SyncNetFreerideRaceModeState)
		{
			NetworkGame instanceGame = NetworkController.InstanceGame;
			if (instanceGame.lastRoomOptions != null)
			{
				isVoteRace.value = (int)(instanceGame.lastRoomOptions.NewRoomSettings & RoomData.RoomSettingFlags.VOTE_ALLOWED) > 0;
			}
			else
			{
				isVoteRace.value = false;
			}
		}
		else
		{
			isVoteRace.value = false;
		}
		ShowTopPanel();
		XRModeManager.TryApplyCockpitCamera();
		hasUpdateQuests.value = false;
		CreateBottomPanel();
	}

	private void CreateBottomPanel()
	{
		List<ActionHintData> list = new List<ActionHintData>();
		list.Add(new ActionHintData
		{
			hint = LocalizationKeys.nav_hint_continue.Localized(),
			type = ActionType.NavCancelBtnB,
			action = PopInGameMenuState
		});
		list.Add(new ActionHintData
		{
			hint = LocalizationKeys.nav_hint_select.Localized(),
			type = ActionType.NavSelectBtnA,
			action = Selection
		});
		list.Add(new ActionHintData
		{
			hint = LocalizationKeys.screen_title_settings.Localized(),
			type = ActionType.NavAdditional0BtnX,
			action = GoToSettings
		});
		if (!isDynoMode.value)
		{
			list.Add(new ActionHintData
			{
				hint = LocalizationKeys.customization_cameras.Localized(),
				type = ActionType.NavAdditional1BtnY,
				action = GoToCustomizatoinCameras
			});
			list.Add(new ActionHintData
			{
				hint = LocalizationKeys.free_camera_button.Localized(),
				type = ActionType.NavBtnExpand,
				action = GoToDroneCamera
			});
			if (States.mainCurrent is FreerideRaceModeState && !(States.mainCurrent is Top32NetRaceModeState) && !(States.mainCurrent is Top32ViewReplayState))
			{
				list.Add(new ActionHintData
				{
					hint = LocalizationKeys.delete_waypoint.Localized(),
					type = ActionType.NavBtnExpandRight,
					action = DeleteCurrentWaypoint
				});
			}
		}
		XRModeManager instance = XRModeManager.instance;
		if (instance != null)
		{
			if (instance.GetVRCamResetKeyboardBind() != null && XRModeManager.IsXRActive())
			{
				list.Add(new ActionHintData
				{
					hint = LocalizationKeys.vr_reset_position_and_rotation.Localized(),
					type = ActionType.VRCamReset,
					action = ResetXRCamera
				});
			}
			instance.OnHeadDeviceUnmounted = (System.Action)Delegate.Remove(instance.OnHeadDeviceUnmounted, new System.Action(CreateBottomPanel));
			instance.OnHeadDeviceUnmounted = (System.Action)Delegate.Combine(instance.OnHeadDeviceUnmounted, new System.Action(CreateBottomPanel));
			instance.OnHeadDeviceMounted = (System.Action)Delegate.Remove(instance.OnHeadDeviceMounted, new System.Action(CreateBottomPanel));
			instance.OnHeadDeviceMounted = (System.Action)Delegate.Combine(instance.OnHeadDeviceMounted, new System.Action(CreateBottomPanel));
		}
		ShowBottomPanel(list.ToArray());
	}

	protected override void OnDeactivate()
	{
		base.OnDeactivate();
		m_cancellationFetchTokenSource?.Cancel();
		XRModeManager instance = XRModeManager.instance;
		if (instance != null)
		{
			instance.OnHeadDeviceMounted = (System.Action)Delegate.Remove(instance.OnHeadDeviceMounted, new System.Action(CreateBottomPanel));
			instance.OnHeadDeviceUnmounted = (System.Action)Delegate.Remove(instance.OnHeadDeviceUnmounted, new System.Action(CreateBottomPanel));
		}
	}

	private void ShowTopPanel()
	{
		if (States.mainCurrent is SyncNetFreerideRaceModeState)
		{
			NetworkGame instanceGame = NetworkController.InstanceGame;
			if (instanceGame == null)
			{
				return;
			}
			string text = instanceGame.CurrentRoom?.Name;
			if (!string.IsNullOrEmpty(text) && text[0] == '@')
			{
				string text2 = "";
				if (instanceGame.lastRoomOptions != null && (int)(instanceGame.lastRoomOptions.NewRoomSettings & RoomData.RoomSettingFlags.CHAMPIONSHIP_MODE) > 0)
				{
					text2 = LocalizationKeys.top32_menu_title.Localized() + " ";
				}
				ShowTopPanel(ETopPanelState.Title, text2 + (GameConsole.Bool["ChampionshipMode"] ? "****" : LocalizationKeys.menu_inroom_pass.LocalizedFormat(text.Substring(1))));
			}
			else
			{
				ShowTopPanel(ETopPanelState.Title, LocalizationKeys.network_game_room_name.LocalizedFormat(NetworkController.GetCurrentRoomName()));
			}
		}
		else
		{
			string text3 = LocalizationKeys.menu_pause_title.Localized() ?? "";
			DB.Meta.Track byId = m_baseModel.GetById<DB.Meta.Track>(m_prefs.globals.lastTrackId);
			if (byId != null)
			{
				text3 += (byId.external ? (" | " + byId.name) : (" | " + LocalizationService.Localize(byId.name).ToUpper()));
			}
			ShowTopPanel(ETopPanelState.Title, text3);
		}
	}

	private bool IsCarAvailable()
	{
		if ((bool)m_raceState.userCar)
		{
			return m_raceState.userCar.IsVisualLoaded;
		}
		return false;
	}

	private void GoToSettings()
	{
		UISettingsContext.Show(base.owner);
	}

	private void GoToDroneCamera()
	{
		if (XRModeManager.IsXRActive())
		{
			XRModeManager.ShowDialogNotAvailable();
			return;
		}
		States.Pop();
		CameraSwitch instance = CameraSwitch.instance;
		if (instance != null)
		{
			instance.SaveLastActiveCam();
		}
		FreeDroneState.Show();
	}

	private void DeleteCurrentWaypoint()
	{
		m_prefs.globals.waypoints.Remove(m_prefs.globals.lastTrackId);
		PlayerCarControl.instance.RefreshCurrentWaypoint();
	}

	private void GoToCustomizatoinCameras()
	{
		if (XRModeManager.IsXRActive())
		{
			XRModeManager.ShowDialogNotAvailable();
			return;
		}
		SetHidden(hidden: true);
		base.owner.HideStackAndPush<UICameraCustomizationContext>();
	}

	private void Selection()
	{
		SubmitCurrentSelected();
	}

	public void NavCancelBtnB()
	{
		PopInGameMenuState();
	}

	public void OnResumeClick()
	{
		PopInGameMenuState();
	}

	public void OnMinimapClick()
	{
		ShowGlobalMap();
	}

	private void ResetXRCamera()
	{
		XRModeManager.instance.TryResetXRCamPosAndRotation();
	}

	private void PopInGameMenuState()
	{
		if (States.current is InGameMenuState)
		{
			States.Pop();
		}
	}

	private void ShowGlobalMap()
	{
		SetHidden(hidden: true);
		base.owner.Push<UIGlobalMinimapContext>();
	}

	public void SetHidden(bool hidden)
	{
		if (hidden)
		{
			HideBottomPanel();
			HideTopPanel();
			base.gameObject.SetActive(value: false);
		}
		else
		{
			base.gameObject.SetActive(value: true);
			CreateBottomPanel();
			ShowTopPanel();
		}
	}

	public void OnPhotoSession()
	{
		OnPhotoSessionClick();
	}

	public void OnRestartClick()
	{
		BaseRaceModeState baseRaceModeState = States.mainCurrent as BaseRaceModeState;
		if ((bool)baseRaceModeState)
		{
			PopInGameMenuState();
			baseRaceModeState.RestartRace();
			AnalyticsService.LogRetryRace(baseRaceModeState.raceMode);
		}
		else
		{
			PopInGameMenuState();
			m_raceState.RestartRace();
		}
	}

	public void OnPhotoSessionClick()
	{
		if (XRModeManager.IsXRActive())
		{
			XRModeManager.ShowDialogNotAvailable();
		}
		else if (IsCarAvailable())
		{
			base.owner.HideStackAndPush<UIPhotoModeContext>();
		}
	}

	public void OnVoteClick()
	{
		PopInGameMenuState();
	}

	public void OnQuestsClick()
	{
		GUICommonGodVoice.ShowText(LocalizationKeys.quests_unavailable.Localized() + ". " + LocalizationKeys.need_set_main_branch.Localized(), 3f);
	}

	public void OpenCarCatalog()
	{
		UICarSelectorFreerideContext uICarSelectorFreerideContext = base.owner.HideStackAndPush<UICarSelectorFreerideContext>();
		uICarSelectorFreerideContext.OnSelectCar = (System.Action)Delegate.Remove(uICarSelectorFreerideContext.OnSelectCar, new System.Action(PopInGameMenuState));
		uICarSelectorFreerideContext.OnSelectCar = (System.Action)Delegate.Combine(uICarSelectorFreerideContext.OnSelectCar, new System.Action(PopInGameMenuState));
	}

	public void OnEndRaceClick()
	{
		if (!IsCarAvailable())
		{
			return;
		}
		if (m_raceState is DynoRaceModeState)
		{
			States.Change<DynoMenuState>();
			return;
		}
		if (isFinishRace.value)
		{
			PopInGameMenuState();
			m_raceState.ForceFinishRace();
			return;
		}
		States.Change<GarageGUIState>();
		BaseRaceModeState baseRaceModeState = m_raceState as BaseRaceModeState;
		if (baseRaceModeState != null)
		{
			baseRaceModeState.ForceExitRace();
		}
	}

	public void OnExitToTheGarageClick()
	{
		if (!IsCarAvailable())
		{
			return;
		}
		if (States.mainCurrent is DynoRaceModeState dynoRaceModeState)
		{
			UIDynostandContext.ResetCachedSwap();
			dynoRaceModeState.contextOwner.Get<UIDynostandContext>().HandleExit();
			return;
		}
		States.Change<GarageGUIState>();
		BaseRaceModeState baseRaceModeState = m_raceState as BaseRaceModeState;
		if (baseRaceModeState != null)
		{
			baseRaceModeState.ForceExitRace();
		}
	}

	public void OnTestDriveClick()
	{
		if (States.current.parent is DynoMenuState)
		{
			States.Change<DynoRaceModeState>();
		}
		else
		{
			OnEndRaceClick();
		}
	}

	public void GotoDyno()
	{
		if (m_raceState == null)
		{
			return;
		}
		if (!IsCarAvailable())
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.loading_car.Localized(), 3f);
			return;
		}
		if (XRModeManager.IsXRActive())
		{
			XRModeManager.ShowDialogNotAvailable();
			return;
		}
		PlayerCarProfile profileMetaInfo = m_raceState.userCar.profileMetaInfo;
		if (UIDynostandContext.IsCanDynoAdjust(profileMetaInfo.name, profileMetaInfo.dynoInfo))
		{
			States.Pop();
			States.Push<DynoMenuState>();
		}
		else
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.garage_hint_profile_dyno_stock.Localized(), 5f);
		}
	}

	public override void SelectionMove(NavigateDirection direction)
	{
		if (direction == NavigateDirection.Up || direction == NavigateDirection.Down)
		{
			VerticalMove(base.m_currActiveNavElement, direction);
		}
		else
		{
			SelectNavElement(NavigateTo(base.m_currActiveNavElement, direction));
		}
	}

	private void VerticalMove(ElementNavigate current, NavigateDirection direction)
	{
		ElementNavigate navigate = GetNavigate(current, direction);
		if (IsElementAvailable(navigate))
		{
			SelectNavElement(navigate);
			return;
		}
		navigate = NavigateTo(navigate, NavigateDirection.Left);
		if (!IsElementAvailable(navigate))
		{
			navigate = NavigateTo(navigate, NavigateDirection.Right);
		}
		if (!IsElementAvailable(navigate))
		{
			navigate = current;
		}
		SelectNavElement(navigate);
	}

	private ElementNavigate NavigateTo(ElementNavigate element, NavigateDirection direction)
	{
		ElementNavigate elementNavigate = element;
		ElementNavigate elementNavigate2 = elementNavigate;
		while (true)
		{
			elementNavigate2 = GetNavigate(elementNavigate, direction);
			if (IsElementAvailable(elementNavigate2))
			{
				return elementNavigate2;
			}
			if (elementNavigate2 == elementNavigate)
			{
				break;
			}
			elementNavigate = elementNavigate2;
		}
		return element;
	}

	private ElementNavigate GetNavigate(ElementNavigate element, NavigateDirection direction)
	{
		if (element == null)
		{
			return null;
		}
		return direction switch
		{
			NavigateDirection.Up => element.up, 
			NavigateDirection.Down => element.down, 
			NavigateDirection.Left => element.left, 
			NavigateDirection.Right => element.right, 
			_ => null, 
		};
	}

	private bool IsElementAvailable(ElementNavigate element)
	{
		if (element != null)
		{
			return element.gameObject.activeInHierarchy;
		}
		return false;
	}

	private void SelectNavElement(ElementNavigate element)
	{
		base.m_currActiveNavElement.Deselect();
		element.Select();
		SetCurrent(element);
	}
}
