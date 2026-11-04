using System;
using System.Collections;
using System.Collections.Generic;
using Binding;
using Binding.Base;
using CarProfile;
using Customization;
using DB;
using DB.Meta;
using DI;
using GameInput;
using GameOverlay;
using Inventory;
using LocalCaching.Manager;
using LocalCaching.Model;
using PrefsModels;
using UnityEngine;

[AudioFXBlocker(false)]
public class UISetupsContext : BaseBindingBehaviourTarget
{
	public enum Mode
	{
		Setups = 0,
		Swaps = 1,
		CustomSetups = 2
	}

	public readonly IntProperty selectedIndex = new IntProperty();

	[ContextDependency]
	private GarageCar3dViewContext m_car3dView;

	[Dependency]
	private GamePrefs m_prefs;

	[Dependency]
	private BaseModel m_db;

	[Dependency]
	private PlayerCarFactory m_carFactory;

	[SerializeField]
	private CarSpecsTableContext m_carSpecsTable;

	private Coroutine m_refreshItemCoroutine;

	private List<UISetupsItem> m_items = new List<UISetupsItem>();

	private bool m_firstEnter;

	private int m_carId = -1;

	private int m_swapsPrevCarId = -1;

	private int m_currProfileId = -1;

	private readonly HashSet<int> m_prevParts = new HashSet<int>();

	private CarPartDatabase.SlotScheme m_slotScheme;

	private CarVisualManager.VisualSettings m_previewSettings;

	private int m_emptyHoodId = -1;

	private int m_currentHoodId = -1;

	private bool m_prevHoodVisible = true;

	private readonly BoolProperty m_swapsAvailable = new BoolProperty(false);

	private readonly BoolProperty m_swapsMode = new BoolProperty(false);

	private readonly BoolProperty m_canShowSwaps = new BoolProperty(false);

	private readonly IntProperty m_tabMode = new IntProperty();

	private readonly StringProperty m_setupsSlotsCount = new StringProperty();

	private Mode m_mode;

	private CameraRotation m_cameraRotation;

	[SerializeField]
	private EngineSwapList m_engineSwapList;

	[SerializeField]
	private UIEngineSwapInfoTable m_engineSwapQuestTable;

	[SerializeField]
	private CustomSetupsList m_customSetupsList;

	private SwapsAsset m_swapsAsset;

	private ActionType m_revEngineAction = ActionType.NavAdditional0BtnX;

	private List<ActionHintData> m_actionHints = new List<ActionHintData>();

	private FMODCarTurbo m_turboComponent;

	private int m_engineId;

	private int m_customSetupsIndexSelected = -1;

	private int m_lastSelectedSetupId;

	private int m_correlationAction;

	private List<CustomSetupItem> m_customSetupsUIitems = new List<CustomSetupItem>();

	private List<HeaderActionData> m_headerData = new List<HeaderActionData>();

	private CustomSetupSaveHelper m_customSetupSaveHelper;

	private RaceCar m_userCar;

	private PlayerCarProfile m_playerCarProfile;

	private ProfilesInfo.Profile m_profileInfoPreview;

	private int m_profileId;

	private bool m_isCustomCreation;

	private EngineInfo m_cachedInstalledSwap;

	private ProfilesInfo.Profile m_profileInfo;

	private PrefsCarSlotsModel m_carSettings => m_prefs.carSettings;

	public GamePrefs prefs => m_prefs;

	public int carId => m_carId;

	private void Awake()
	{
		m_swapsAsset = SwapsAsset.LoadAndInitDB();
		GetComponentsInChildren(includeInactive: true, m_items);
	}

	private void UpdateTopPanel()
	{
		m_correlationAction = 0;
		m_headerData.Clear();
		ShowTopPanel(ETopPanelState.Actions, null, m_headerData.ToArray());
		m_headerData.Add(new HeaderActionData
		{
			text = LocalizationKeys.setups_window_title.Localized().ToUpper(),
			action = SelectSetupsMode
		});
		if (CanShowSwapsMenu())
		{
			m_headerData.Add(new HeaderActionData
			{
				text = LocalizationKeys.swaps_mode_swaps.Localized().ToUpper(),
				action = SelectSwapsMode
			});
		}
		else
		{
			m_correlationAction++;
		}
		m_headerData.Add(new HeaderActionData
		{
			text = LocalizationKeys.customs_window_title.Localized().ToUpper(),
			action = SelectCustomsMode
		});
		ShowTopPanel(ETopPanelState.Actions, null, m_headerData.ToArray());
		m_topPanelContext.switcherActions.SelectionAction((int)(m_mode - m_correlationAction));
	}

	private void UpdateModeSelection()
	{
		int index = (int)(GetOpenModeAt() - m_correlationAction);
		m_topPanelContext.switcherActions.SelectionAction(index);
	}

	private Mode GetOpenModeAt()
	{
		if (m_carSettings.GetHaveCustomSetupsPerCar(m_carId).haveInstalledCustom)
		{
			return Mode.CustomSetups;
		}
		if (m_engineId > 0)
		{
			return Mode.Swaps;
		}
		return Mode.Setups;
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		Overlay.instance.richPresence.Show(RichPresenceStatus.CarTuning);
		Singleton<CustomCameraPresets>.instance.CameraTo(CustomizationCameraPreset.Garage);
		m_firstEnter = true;
		m_car3dView.Set3DModelVisible(value: true);
		m_car3dView.SetGarageCarStatePhysAndHoldPosition(disablePhysics: false, holdPosition: false);
		m_car3dView.SetCarsSound(enable: false);
		m_cameraRotation = CameraRotation.instance;
		if (base.owner.IsActive<UIBackgroundContext>())
		{
			base.owner.Hide<UIBackgroundContext>();
		}
		m_carId = m_prefs.globals.lastCarId;
		CarSwap.SaveStockEngineIfNeeded(carId, m_carSettings);
		m_carSpecsTable.InitCarInfo(m_db.GetById<PlayerCar>(m_carId));
		m_mode = Mode.Setups;
		m_tabMode.value = (int)m_mode;
		selectedIndex.value = -1;
		m_engineId = m_carSettings.GetEngineIdForCar(m_carId);
		m_turboComponent = m_car3dView.currentCar.GetComponentInChildren<FMODCarTurbo>();
		EngineSwapList engineSwapList = m_engineSwapList;
		engineSwapList.onClick = (Action<int>)Delegate.Combine(engineSwapList.onClick, new Action<int>(OnEngineSwapItemClickedHandler));
		EngineSwapList engineSwapList2 = m_engineSwapList;
		engineSwapList2.onDoubleClick = (Action<int>)Delegate.Combine(engineSwapList2.onDoubleClick, new Action<int>(OnEngineSwapItemDoubleClickedHandler));
		EngineSwapList engineSwapList3 = m_engineSwapList;
		engineSwapList3.onChangeSelectedIndex = (System.Action)Delegate.Combine(engineSwapList3.onChangeSelectedIndex, new System.Action(OnEngineSwapSelectedIndexChangedHandler));
		InitSetupItems();
		UpdateTopPanel();
		UpdateModeSelection();
		RefreshSetupItems();
		SetupEmptyHood();
		RefreshCustomSetupData();
		if (m_customSetupSaveHelper == null)
		{
			m_customSetupSaveHelper = new CustomSetupSaveHelper();
		}
		m_customSetupSaveHelper.InitHelper(m_userCar, m_playerCarProfile, m_profileInfoPreview, m_profileId, carId, swapApplied: false);
		UpdateCurrProfileId();
		SubscribePS4TextReceive();
		bool flag = GetOpenModeAt() == Mode.Swaps;
		m_engineSwapQuestTable.SetVisible(flag);
		if (flag)
		{
			SetHoodVisible(m_currProfileId < 0);
		}
		UpdateBottomPanel();
	}

	private void RefreshCustomSetupData()
	{
		PrefsCarSlotsModel carSettings = m_prefs.carSettings;
		m_profileId = carSettings.GetProfileIdForCar(carId);
		m_playerCarProfile = m_db.QueryRecordsByColumnValue("carId", carId, (PlayerCarProfile x) => x.carId == carId).Find((PlayerCarProfile x) => x.id == m_profileId).Copy();
		m_userCar = m_car3dView.GetRaceCar();
		CustomProfile installedCustomProfile = carSettings.GetInstalledCustomProfile(m_prefs.globals.lastCarId);
		m_profileInfo = carSettings.GetProfileInfo(m_profileId, m_userCar.carId, installedCustomProfile?.id);
		m_profileInfoPreview = new ProfilesInfo.Profile(m_profileInfo.id)
		{
			controllers = new Dictionary<string, float>(m_profileInfo.controllers)
		};
	}

	private void UpdateCurrProfileId()
	{
		UISetupsItem itemById = GetItemById(GetCurrentProfileId());
		CustomProfile installedCustomProfile = m_carSettings.GetInstalledCustomProfile(m_carId);
		if (installedCustomProfile != null)
		{
			m_currProfileId = installedCustomProfile.profile.id;
		}
		else if (itemById != null && itemById.profileProxy != null)
		{
			m_currProfileId = itemById.profileProxy.id;
		}
	}

	protected override void OnDeactivate()
	{
		StopRefreshCoroutine();
		m_carSpecsTable.ClearCachedInitParams();
		Singleton<CustomCameraPresets>.instance.CameraTo(CustomizationCameraPreset.Garage);
		m_car3dView.SetGarageCarStatePhysAndHoldPosition(disablePhysics: true, holdPosition: false);
		HideTopPanel();
		HideBottomPanel();
		UpdateCurrProfileId();
		if (m_turboComponent != null)
		{
			m_turboComponent.SetTurboSoundsEnable(enable: false);
		}
		SetHoodVisible(hoodVisible: true);
		m_car3dView.IconGenerate();
		UnSubscribePS4TextReceive();
		EngineSwapList engineSwapList = m_engineSwapList;
		engineSwapList.onClick = (Action<int>)Delegate.Remove(engineSwapList.onClick, new Action<int>(OnEngineSwapItemClickedHandler));
		EngineSwapList engineSwapList2 = m_engineSwapList;
		engineSwapList2.onDoubleClick = (Action<int>)Delegate.Remove(engineSwapList2.onDoubleClick, new Action<int>(OnEngineSwapItemDoubleClickedHandler));
		EngineSwapList engineSwapList3 = m_engineSwapList;
		engineSwapList3.onChangeSelectedIndex = (System.Action)Delegate.Remove(engineSwapList3.onChangeSelectedIndex, new System.Action(OnEngineSwapSelectedIndexChangedHandler));
		base.OnDeactivate();
	}

	private void Update()
	{
		CustomCameraPresets instance = Singleton<CustomCameraPresets>.instance;
		if (instance != null && instance.currentPreset.stateType != CustomizationCameraPreset.Garage && m_cameraRotation != null)
		{
			if (InputManager.instance.IsLastActiveJoystickAxis2D(1, out var axis) && axis != Vector2.zero)
			{
				instance.CameraTo(CustomizationCameraPreset.Garage);
			}
			if (!UIUtils.IsPointerOverGameObject() && (Input.touchCount > 0 || Input.GetMouseButton(0)) && !m_cameraRotation.skipToRotateByMouse)
			{
				instance.CameraTo(CustomizationCameraPreset.Garage);
			}
		}
		PlayEngineSound();
	}

	private void PlayEngineSound()
	{
		RaceCar currentCar = m_car3dView.currentCar;
		if (m_mode == Mode.Swaps && !(currentCar == null) && !m_canShowSwaps.value)
		{
			if (!currentCar.engineController.engineWorking)
			{
				currentCar.engineController.ForceStartEngine(bumpRpm: true);
			}
			float actionValue = InputManager.instance.GetActionValue(m_revEngineAction);
			currentCar.carX.accelerate = actionValue;
			currentCar.lockState = true;
		}
	}

	private void SetupEmptyHood()
	{
		RaceCar currentCar = m_car3dView.currentCar;
		CarVisualManager.VisualSettings visualForCar = m_carSettings.GetVisualForCar(m_carId);
		m_previewSettings = visualForCar.Copy();
		m_slotScheme = new CarPartDatabase.SlotScheme(m_carId, m_previewSettings.parts);
		m_prevParts.Clear();
		foreach (int part in m_previewSettings.parts)
		{
			m_prevParts.Add(part);
		}
		int emptyBonnetId = currentCar.metaInfo.emptyBonnetId;
		if (emptyBonnetId == -1)
		{
			m_emptyHoodId = -1;
			return;
		}
		int num = 20 + currentCar.metaInfo.emptyBonnetId;
		List<CarPart> parts = CarPartDatabase.GetParts(m_slotScheme, "bonnet", "", forceAdd: true);
		bool flag = false;
		bool flag2 = false;
		foreach (CarPart item in parts)
		{
			if (item != null && m_carId == item.carId)
			{
				if (!flag && (item.nativeid == emptyBonnetId || item.nativeid == num))
				{
					m_emptyHoodId = item.id;
					flag = true;
				}
				if (!flag2 && m_previewSettings.parts.Contains(item.id))
				{
					m_currentHoodId = item.id;
					flag2 = true;
				}
				if (flag && flag2)
				{
					break;
				}
			}
		}
		CarPart carPart = CarPartDatabase.GetParts(m_slotScheme, "chassis", "", forceAdd: true).Find((CarPart part) => m_previewSettings.parts.Contains(part.id));
		foreach (CarPart item2 in parts)
		{
			if (carPart == null || carPart.overrideEmptyBonnetId == -1)
			{
				break;
			}
			if (item2 != null && m_carId == item2.carId && item2.nativeid == carPart.overrideEmptyBonnetId)
			{
				m_emptyHoodId = item2.id;
				break;
			}
		}
		m_prevHoodVisible = m_currentHoodId != m_emptyHoodId;
	}

	private void InitSetupItems()
	{
		List<CarProfileProxy> profileList = CarProfileCacheManager.GetProfileList(m_db, m_carFactory, m_carId);
		for (int i = 0; i < m_items.Count; i++)
		{
			CarSpecsTableContext.ProfileName profileName = (CarSpecsTableContext.ProfileName)i;
			string value = profileName.ToString();
			foreach (CarProfileProxy item in profileList)
			{
				if (item.name.Equals(value))
				{
					m_items[i].Init(m_prefs, item);
					break;
				}
			}
		}
	}

	private void RefreshSetupItems()
	{
		int num = -1;
		int currentProfileId = GetCurrentProfileId();
		for (int i = 0; i < m_items.Count; i++)
		{
			m_items[i].Refresh(currentProfileId);
			if (m_items[i].isCurrent.value)
			{
				num = i;
			}
		}
		if (num != -1)
		{
			SelectItem(num);
		}
		else
		{
			UpdateBottomPanel();
		}
	}

	private void GotoGarage()
	{
		SetHoodVisible(hoodVisible: true);
		RaceCar currentCar = m_car3dView.currentCar;
		TimeController.StartNow(currentCar.swap.Refresh(m_engineId));
		int profileIdForCar = m_carSettings.GetProfileIdForCar(m_carId);
		PlayerCarProfile playerCarProfile = (currentCar.profileMetaInfo = m_db.GetById<PlayerCarProfile>(profileIdForCar));
		currentCar.InitDesc(playerCarProfile.asset, skipWheelBase: true);
		CustomProfile installedCustomProfile = m_carSettings.GetInstalledCustomProfile(m_carId);
		bool flag = installedCustomProfile != null;
		ProfilesInfo.Profile profileInfo = m_prefs.carSettings.GetProfileInfo(profileIdForCar, m_carId, installedCustomProfile?.id);
		if (profileInfo != null)
		{
			currentCar.ApplyAdvancedProfile(m_db, profileInfo, m_prefs.carSettings, reset: false, flag, null, "", !flag);
			currentCar.engineController.ShutdownEngine();
		}
		m_car3dView.ApplyDefaultCustomization();
		m_car3dView.currentCar.AlignCarPositionAtSpawn(adjustRotation: false);
		Hide();
		base.owner.Push<UIGarageContext>();
	}

	public void SubmitInput()
	{
		UISetupsItem item = GetSelectedItem();
		TryInstallSetupItem(out var _, item);
	}

	private void TryInstallSetupItem(out bool installed, UISetupsItem item)
	{
		installed = false;
		if (!(item == null))
		{
			if (item.canBeUnlocked)
			{
				TryBuySetup(item);
			}
			else if (item.lockedByLevel.value)
			{
				UIMessageBox.ShowHint(LocalizationKeys.locked_level_message_track.LocalizedFormat(item.requiredLevel.value), "", null, base.owner);
			}
			else if (!item.locked.value)
			{
				CarProfileProxy profileProxy = item.profileProxy;
				PerformSetupInstall(profileProxy.id, profileProxy);
				installed = true;
			}
			m_cachedInstalledSwap = null;
			UpdateEngineSwapsAvailability(item);
		}
	}

	private void ResetHaveCustomSetupForCar()
	{
		(string, bool, int) haveCustomSetupsPerCar = m_carSettings.GetHaveCustomSetupsPerCar(m_carId);
		if (haveCustomSetupsPerCar.Item2)
		{
			m_carSettings.SetHaveCustomSetupsPerCar(m_carId, m_engineId, haveCustomSetupsPerCar.Item1, isHaveCustomSetup: false);
		}
	}

	private void CopySetup(bool createCustom)
	{
		UIGarageContext uIGarageContext = base.owner.Get<UIGarageContext>();
		m_isCustomCreation = createCustom;
		if ((bool)uIGarageContext && !uIGarageContext.IsCanEnterToDyno())
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.garage_hint_profile_dyno_stock.Localized());
		}
		else if (Overlay.instance is SteamOverlay steamOverlay && GameManager.IsLinuxOsPlatform())
		{
			steamOverlay.ShowOverlayKeyboard(LocalizationKeys.custom_setup_save_promt.Localized(), string.Empty, 16);
		}
		else
		{
			UIMessageBox.ShowInputDialog(LocalizationKeys.custom_setup_save_promt.Localized(), string.Empty, null, null, LocalizationKeys.Cancel.Localized(), OnReceiveInputText, LocalizationKeys.Ok.Localized(), 16);
		}
	}

	private void OnReceiveInputText(string text)
	{
		RefreshCustomSetupData();
		m_customSetupSaveHelper.InitHelper(m_userCar, m_playerCarProfile, m_profileInfoPreview, m_profileId, carId, swapApplied: false);
		bool haveCustomSetupInstalled = m_customSetupSaveHelper.HaveInstalledCustomProfile();
		m_customSetupSaveHelper.SetNewSetupName(text, out var isNameValid);
		UIDynostandContext uIDynostandContext = base.owner.Get<UIDynostandContext>();
		if ((bool)uIDynostandContext)
		{
			uIDynostandContext.m_haveCustomSetupInstalled = haveCustomSetupInstalled;
		}
		if (!isNameValid)
		{
			return;
		}
		m_customSetupSaveHelper.SaveSetup(out var finalSaveResult);
		FillCustomSetupsList(null, finalSaveResult == CustomSetupsSaveResult.Rewrite);
		if (m_isCustomCreation)
		{
			if (finalSaveResult == CustomSetupsSaveResult.Rewrite)
			{
				m_customSetupSaveHelper.InitHelper(m_userCar, m_playerCarProfile, m_profileInfoPreview, m_profileId, carId, swapApplied: false);
				m_customSetupSaveHelper.ApplySavedSwap(m_profileInfo, m_prefs.carSettings.GetHaveCustomSetupsPerCar(m_carId).haveInstalledCustom, openedFromCreation: true);
			}
			else
			{
				CustomSetupItem selected = m_customSetupsUIitems.Last();
				InstallCustomSetup(selected);
				GoToDyno(creation: true);
			}
		}
	}

	private void SubscribePS4TextReceive()
	{
	}

	private void UnSubscribePS4TextReceive()
	{
	}

	private void GoToDyno(bool creation = false)
	{
		UISetupsItem uISetupsItem = GetSelectedItem();
		if (m_mode != Mode.Setups || (!(uISetupsItem == null) && !uISetupsItem.locked.value && uISetupsItem.isCurrent.value))
		{
			SetHoodVisible(hoodVisible: true);
			Hide();
			if (m_prefs.globals.lastTrackId == -1)
			{
				m_prefs.globals.lastTrackId = FindDefaultUnlockedTrackId();
			}
			m_car3dView.Set3DModelVisible(value: true);
			m_car3dView.SetGarageCarStatePhysAndHoldPosition(disablePhysics: false, holdPosition: false);
			UIDynostandContext.ResetCachedSwap();
			UIDynostandContext uIDynostandContext = base.owner.Push<UIDynostandContext>().Init(m_userCar);
			if (uIDynostandContext != null)
			{
				uIDynostandContext.SetOpenedFrom(GetOpenedFromMode());
			}
		}
		UIDynostandContext.OpenedFrom GetOpenedFromMode()
		{
			return m_mode switch
			{
				Mode.Setups => UIDynostandContext.OpenedFrom.Setups, 
				Mode.Swaps => UIDynostandContext.OpenedFrom.Swaps, 
				Mode.CustomSetups => (!creation) ? UIDynostandContext.OpenedFrom.CustomSetups : UIDynostandContext.OpenedFrom.SetupCreation, 
				_ => UIDynostandContext.OpenedFrom.Setups, 
			};
		}
	}

	private void StopRefreshCoroutine()
	{
		if (m_refreshItemCoroutine != null)
		{
			TimeController.StopNow(m_refreshItemCoroutine);
			m_refreshItemCoroutine = null;
		}
	}

	public void SelectItem(int index)
	{
		if (index < 0 || index >= m_items.Count)
		{
			return;
		}
		selectedIndex.value = index;
		if (m_mode != Mode.CustomSetups && m_mode != Mode.Swaps)
		{
			UISetupsItem uISetupsItem = GetSelectedItem();
			if ((bool)uISetupsItem)
			{
				uISetupsItem.PlayHover();
				StopRefreshCoroutine();
				UpdateEngineSwapsAvailability(uISetupsItem);
				m_refreshItemCoroutine = TimeController.StartNow(RefreshItemViewCoroutine(uISetupsItem, uISetupsItem.profileProxy.GetCurrentEngineId()));
			}
		}
	}

	private void UpdateEngineSwapsAvailability(ISetupsItem item)
	{
		if (item != null)
		{
			m_swapsAvailable.value = m_car3dView.currentCar.metaInfo.enableSwaps && item.current && item.profileProxy.GetProfile().CanSwap();
		}
	}

	private void SelectSetupsMode()
	{
		SelectMode(Mode.Setups);
	}

	private void SelectSwapsMode()
	{
		SelectMode(Mode.Swaps);
	}

	private void SelectCustomsMode()
	{
		SelectMode(Mode.CustomSetups);
	}

	public void OpenTabFromDyno(Mode mode)
	{
		m_topPanelContext.switcherActions.SelectionAction((int)(mode - m_correlationAction));
	}

	private void SelectMode(Mode mode)
	{
		if (m_mode == mode)
		{
			return;
		}
		m_mode = mode;
		int profileIdForCar = m_carSettings.GetProfileIdForCar(m_carId);
		m_engineId = m_carSettings.GetEngineIdForCar(m_carId);
		if (m_mode == Mode.Setups)
		{
			bool item = m_carSettings.GetHaveCustomSetupsPerCar(m_carId).haveInstalledCustom;
			m_lastSelectedSetupId = selectedIndex.value;
			for (int i = 0; i < m_items.Count; i++)
			{
				UISetupsItem uISetupsItem = m_items[i];
				uISetupsItem.Refresh(profileIdForCar);
				if (uISetupsItem.isCurrent.value)
				{
					uISetupsItem.isCurrent.value &= !item;
					if (uISetupsItem.isCurrent.value)
					{
						m_lastSelectedSetupId = i;
					}
				}
			}
			SelectItem(m_lastSelectedSetupId);
			m_lastSelectedSetupId = selectedIndex.value;
		}
		int id = m_carSettings.GetInstalledCustomProfile(m_carId)?.profile.id ?? profileIdForCar;
		PlayerCarProfile byId = m_db.GetById<PlayerCarProfile>(id);
		bool flag = m_mode == Mode.Swaps && byId.CanSwap();
		m_engineSwapQuestTable.SetVisible(flag);
		m_canShowSwaps.value = !flag;
		m_swapsMode.value = flag;
		m_tabMode.value = (int)m_mode;
		if (m_turboComponent != null)
		{
			m_turboComponent.SetTurboSoundsEnable(flag);
		}
		RaceCar currentCar = m_car3dView.currentCar;
		if (flag)
		{
			FillEngineList();
			m_engineSwapList.ShowFirstAvailableCategory();
			m_engineSwapList.selectedIndex = m_engineSwapList.GetEngineIndexById(m_engineId);
			PreviewSelectedEngine();
			FocusCameraOnHood();
			currentCar.engineController.ForceStartEngine(bumpRpm: true);
		}
		else
		{
			currentCar.engineController.ShutdownEngine();
		}
		if (m_mode == Mode.CustomSetups)
		{
			FillCustomSetupsList();
		}
		SetHoodVisible(!flag);
		UpdateBottomPanel();
	}

	private void FillEngineList()
	{
		if (!m_car3dView.currentCar.metaInfo.enableSwaps)
		{
			m_engineSwapList.Clear();
		}
		else
		{
			if (m_swapsPrevCarId == m_carId && m_engineSwapList.GetCount() != 0)
			{
				return;
			}
			if (m_carId != m_swapsPrevCarId)
			{
				m_swapsPrevCarId = m_carId;
				m_engineSwapList.Clear();
			}
			EngineSwapListItem item = new EngineSwapListItem(new EngineInfo
			{
				id = 0,
				type = m_car3dView.currentCar.metaInfo.stockEngineType
			}, carId);
			m_engineSwapList.AddItem(item);
			EngineSwapData engineSwapDataForCar = m_prefs.carSettings.GetEngineSwapDataForCar(m_carId);
			foreach (EngineInfo swap in m_swapsAsset.swaps)
			{
				if (m_car3dView.currentCar.metaInfo.availableSwaps.Contains(swap.id))
				{
					if (ConstructEngineSwapData(swap.id, out var swapData))
					{
						m_prefs.carSettings.SetEngineSwapDataForCar(m_carId, swapData);
					}
					m_engineSwapList.AddItem(new EngineSwapListItem(swap, carId));
				}
			}
			m_prefs.carSettings.SetEngineSwapDataForCar(m_carId, engineSwapDataForCar);
		}
	}

	private void OnEngineSwapItemClickedHandler(int index)
	{
		m_engineSwapList.selectedIndex = index;
		FocusCameraOnHood();
	}

	private void OnEngineSwapItemDoubleClickedHandler(int index)
	{
		m_engineSwapList.selectedIndex = index;
		InstallSelectedEngine();
		FocusCameraOnHood();
	}

	private void OnEngineSwapSelectedIndexChangedHandler()
	{
		PreviewSelectedEngine();
	}

	private bool ConstructEngineSwapData(int engineId, out EngineSwapData swapData)
	{
		int profileId = m_carSettings.GetProfileIdForCar(m_carId);
		PlayerCarProfile playerCarProfile = m_db.QueryRecordsByColumnValue("carId", m_carId, (PlayerCarProfile x) => x.carId == m_carId).Find((PlayerCarProfile x) => x.id == profileId);
		Predicate<string> neededCtrl = (string ctrlName) => ctrlName.StartsWith("gearbox") || ctrlName == "final_drive";
		swapData = m_prefs.carSettings.GetEngineSwapDataForCar(m_carId, engineId);
		if (swapData.controllers.Exists((EngineSwapData.SwapController el) => neededCtrl(el.key)))
		{
			return false;
		}
		bool result = false;
		foreach (CarProfileController controller in playerCarProfile.dynoInfo.controllers)
		{
			if (controller != null && controller.changers.Count >= 1)
			{
				string text = controller.name;
				if (neededCtrl(text))
				{
					CarProfileChanger carProfileChanger = controller.changers[0];
					swapData.AddController(text, carProfileChanger.EvaluatePhase(carProfileChanger.defaultValue + carProfileChanger.shift));
					result = true;
				}
			}
		}
		return result;
	}

	private bool CanShowSwapsMenu()
	{
		bool item = m_carSettings.GetHaveCustomSetupsPerCar(carId).haveInstalledCustom;
		int num = 0;
		foreach (EngineInfo swap in m_swapsAsset.swaps)
		{
			if (item)
			{
				break;
			}
			if (m_car3dView.currentCar.metaInfo.availableSwaps.Contains(swap.id))
			{
				num++;
			}
		}
		return num != 0;
	}

	private void FillCustomSetupsList(CustomProfile prevCustomProfile = null, bool rewritten = false)
	{
		List<CustomProfile> customProfilesByCarId = m_carSettings.GetCustomProfilesByCarId(carId);
		CustomProfile installedCustomProfile = m_carSettings.GetInstalledCustomProfile(m_carId);
		CustomProfile customProfile = prevCustomProfile ?? installedCustomProfile;
		bool flag = false;
		m_customSetupsUIitems.Clear();
		m_customSetupsList.Clear();
		CustomSetupItem customSetupItem = null;
		if (customProfilesByCarId.Count < 16)
		{
			PlayerCarProfile byId = m_db.GetById<PlayerCarProfile>(GetCurrentProfileId());
			CarProfileCache carProfileCache = CarProfileCacheManager.GetCarProfileCache(m_carFactory, byId);
			CarProfileProxy proxy = new CarProfileProxy(byId, carProfileCache);
			CustomSetupItem item = new CustomSetupItem(this, proxy);
			if (customProfile != null)
			{
				byId = m_db.GetById<PlayerCarProfile>(customProfile.profile.id);
				carProfileCache = CarProfileCacheManager.GetCarProfileCache(m_carFactory, byId);
				proxy = new CarProfileProxy(byId, carProfileCache);
				item = new CustomSetupItem(this, customProfile, proxy, isPlusElem: true);
			}
			else if (m_cachedInstalledSwap != null)
			{
				item = new CustomSetupItem(this, m_cachedInstalledSwap.id, proxy);
			}
			m_customSetupsUIitems.Add(item);
			flag = true;
		}
		foreach (CustomProfile item2 in customProfilesByCarId)
		{
			if (item2 == null)
			{
				continue;
			}
			int profileId = item2.profile.id;
			PlayerCarProfileMetaExtensions.FixProfileIdIfNeed(m_carId, ref profileId);
			PlayerCarProfile byId2 = m_db.GetById<PlayerCarProfile>(profileId);
			CarProfileCache carProfileCache2 = CarProfileCacheManager.GetCarProfileCache(m_carFactory, byId2);
			CarProfileProxy profileProxy = new CarProfileProxy(byId2, carProfileCache2);
			CustomSetupItem customSetupItem2 = new CustomSetupItem(this, item2, profileProxy, isPlusElem: false);
			if (customProfile?.id == item2.id)
			{
				customSetupItem2.current = true;
				customSetupItem = customSetupItem2;
				if (m_carSettings.GetEngineIdForCar(m_carId) != customSetupItem2.engineId)
				{
					m_carSettings.SetEngineForCar(m_carId, customSetupItem2.engineId);
				}
			}
			m_customSetupsUIitems.Add(customSetupItem2);
		}
		m_customSetupsList.AddRange(m_customSetupsUIitems);
		int count = m_customSetupsList.GetCount();
		string arg = (flag ? $"{count - 1}" : $"{count}");
		m_setupsSlotsCount.value = $"{arg}/{16}";
		int num = ((!(customProfilesByCarId.Count == 0 || rewritten)) ? 1 : 0);
		if (count == 16)
		{
			num = 0;
		}
		if (customSetupItem != null)
		{
			num = m_customSetupsList.IndexOf(customSetupItem);
		}
		m_customSetupsList.RecalcList();
		m_customSetupsList.ScrollTo(num);
		m_customSetupsList.selectedIndex = num;
		m_customSetupsIndexSelected = num;
		OnSelectCustomSetup();
		if (installedCustomProfile == null && prevCustomProfile != null)
		{
			InstallCustomSetup(m_customSetupsList.GetSelectedItem());
		}
	}

	public void PreviewSelectedEngine()
	{
		EngineSwapListItem engineSwapListItem = m_engineSwapList.GetSelectedItem();
		if (engineSwapListItem != null)
		{
			m_engineSwapQuestTable.UpdateTable(engineSwapListItem);
			if (m_turboComponent != null)
			{
				EngineParamInfo turboPressure = engineSwapListItem.info.turboPressure;
				bool turboSoundsEnable = turboPressure == null || turboPressure.min != 0f || turboPressure == null || turboPressure.max != 0f;
				m_turboComponent.SetTurboSoundsEnable(turboSoundsEnable);
			}
			ISetupsItem itemById = GetItemById(GetCurrentProfileId());
			TimeController.StartNow(RefreshStats(itemById, engineSwapListItem?.info.id ?? 0));
			m_car3dView.ApplyProfile(itemById.profileProxy.id);
			UpdateBottomPanel();
		}
	}

	public void InstallEngine(int engineId)
	{
		EngineSwapListItem engineById = m_engineSwapList.GetSelectedItem();
		if (engineById == null)
		{
			engineById = m_engineSwapList.GetEngineById(engineId);
			if (engineById == null)
			{
				GameLog.Print(AlertLevel.Error, "System", "Selected engine null");
				return;
			}
		}
		if (engineById.IsAbleToInstall())
		{
			EngineInfo engineInfo = m_swapsAsset.Find(engineById.info.id);
			if (engineInfo == null)
			{
				Debug.LogError($"[Swaps]: Unable to install engine with id {engineById.info.id}, swap items is not found");
				return;
			}
			int currentProfileId = GetCurrentProfileId();
			m_cachedInstalledSwap = engineInfo;
			ResetHaveCustomSetupForCar();
			m_engineId = engineById.info.id;
			m_carSettings.SetEngineForCar(m_carId, m_engineId);
			m_carSettings.SetProfilePerCar(m_carId, currentProfileId);
			PlayerCarProfileMetaExtensions.ValidateEngineSwapProfileControllers(currentProfileId);
			RefreshCustomSetupData();
			m_carSettings.SetProfileInfo(carId, currentProfileId, m_profileInfoPreview.Copy());
			SoundController.Play2DUISound(SoundSample.ProSetup);
			if (m_engineId != 0)
			{
				string engineIdFromCar = engineInfo.GetEngineIdFromCar(carId);
				InventoryItem item = prefs.inventory.GetItem(engineIdFromCar);
				bool flag = item != null && item.count > 0;
				if (!GameManager.SwapsDLCInstalled && !flag)
				{
					m_prefs.inventory.RemoveItem(engineById.info.partItem, engineById.info.partsToInstall);
					m_prefs.inventory.AddItem(engineIdFromCar, 1, permanentItem: true, addCountIfExist: false);
				}
			}
		}
		else
		{
			ShowMessageEngineInstallForbidden();
		}
		m_engineSwapQuestTable.UpdateTable(engineById);
		UpdateBottomPanel();
		m_engineSwapList.UpdateList();
	}

	private void ShowMessageEngineInstallForbidden()
	{
		EngineSwapListItemController controller = m_engineSwapList.GetController(m_engineSwapList.GetSelectedIndex());
		UIMessageBox.ShowMessageWithTitle(LocalizationKeys.swaps_not_enough_parts_template.LocalizedFormat(controller.requiredPartCountText), m_engineSwapQuestTable.GetCurrentQuestRequirementsString(), null, LocalizationKeys.buy_dlc.Localized(), delegate
		{
			Overlay.instance.ShowBuyDLCPage(DLCCode.Swaps);
		}, LocalizationKeys.buy_season_pass.Localized(), delegate
		{
			Overlay.instance.ShowBuyDLCPage(DLCCode.SeasonPass);
		});
	}

	private IEnumerator RefreshStats(ISetupsItem setup, int engineId)
	{
		RaceCar car = m_car3dView.currentCar;
		if (!(car == null))
		{
			CarSwap swap = car.swap;
			yield return swap.carAttached ? swap.Refresh(engineId) : swap.Initialize(car, m_prefs, m_db);
			yield return m_carSpecsTable.InitProfileInfo(m_car3dView, m_mode, setup.profileProxy, m_db, car, setup.customSetupId);
		}
	}

	private bool IsSetupUsedBetweenSlots(int engineId, string customSetupId, out int links)
	{
		links = 0;
		List<int> usedSlots = m_prefs.carSettings.GetUsedSlots(m_carId);
		for (int i = 0; i < usedSlots.Count; i++)
		{
			PrefsCarSlotDataModel slotById = m_prefs.carSettings.GetSlotById(m_carId, usedSlots[i]);
			if (slotById.swapData.currentId == engineId && slotById.customSetupId == customSetupId)
			{
				links++;
			}
		}
		return links > 1;
	}

	private void DeleteCustomSetup()
	{
		CustomSetupItem customSetupItem = m_customSetupsList.GetSelectedItem();
		if (customSetupItem == null)
		{
			return;
		}
		UIMessageBox.ShowDialog(IsSetupUsedBetweenSlots(customSetupItem.engineId, customSetupItem.setupId, out var links) ? LocalizationKeys.car_slots_custom_setup_already_in_use_delete_warning.LocalizedFormat(links) : LocalizationKeys.custom_setup_delete_promt.Localized(), null, new DialogButtonData[2]
		{
			new DialogButtonData
			{
				action = delegate
				{
					CustomSetupItem customSetupItem2 = m_customSetupsList.GetSelectedItem();
					if (customSetupItem2 != null)
					{
						m_customSetupsList.GetSelectedIndex();
						m_carSettings.ClearEnginesPerCarCustomSetupsCache(m_carId, customSetupItem2.engineId, customSetupItem2.setupId);
						m_carSettings.RemoveCustomSetup(m_carId, customSetupItem2.setupId);
						m_customSetupsList.RemoveItem(customSetupItem2);
						CustomProfile installedCustomProfile = m_carSettings.GetInstalledCustomProfile(carId);
						ResetSetupForAllSlots(customSetupItem2.setupId, customSetupItem2.engineId);
						if (m_customSetupsUIitems.Count - 2 > 0)
						{
							FillCustomSetupsList(installedCustomProfile);
						}
						(string, bool, int) haveCustomSetupsPerCar = m_carSettings.GetHaveCustomSetupsPerCar(m_carId);
						if (haveCustomSetupsPerCar.Item2 && haveCustomSetupsPerCar.Item1 == customSetupItem2.setupId && haveCustomSetupsPerCar.Item3 == customSetupItem2.engineId)
						{
							SelectFirstUnlockedSetupAndSwitch(customSetupItem2.profileProxy.name, installedCustomProfile == null);
						}
						else if (m_customSetupsUIitems.Count < 2)
						{
							m_topPanelContext.switcherActions.SelectionAction(0);
						}
					}
					UpdateBottomPanel();
					UpdateTopPanel();
				},
				text = LocalizationKeys.Yes.Localized()
			},
			new DialogButtonData
			{
				action = null,
				text = LocalizationKeys.No.Localized()
			}
		});
	}

	private void SelectFirstUnlockedSetupAndSwitch(string setupName, bool skipNameCheck)
	{
		if (m_customSetupsUIitems.Count != 2)
		{
			return;
		}
		bool flag = false;
		for (int num = m_items.Count - 1; num >= 0; num--)
		{
			if (!(GetItemAt(num).profileProxy.name != setupName) || skipNameCheck)
			{
				TryInstallSetupItem(out var installed, GetItemAt(num));
				if (installed)
				{
					flag = true;
					break;
				}
			}
		}
		if (flag)
		{
			m_topPanelContext.switcherActions.SelectionAction(0);
		}
	}

	private void InstallCustomSetup(CustomSetupItem selected)
	{
		if (selected != null)
		{
			int i = 0;
			for (int count = m_customSetupsList.GetCount(); i < count; i++)
			{
				CustomSetupItem customSetupItem = m_customSetupsList[i];
				customSetupItem.current = customSetupItem == selected;
			}
			m_customSetupsList.UpdateList();
			m_engineId = selected.engineId;
			ResetHaveCustomSetupForCar();
			m_carSettings.SetHaveCustomSetupsPerCar(m_carId, selected.engineId, selected.setupId, isHaveCustomSetup: true);
			if (!selected.isPlusElement)
			{
				m_carSettings.SetProfilePerCar(m_carId, selected.profileProxy.id);
				FillCustomSetupsList();
			}
			SoundController.Play2DUISound(SoundSample.ProSetup);
			UpdateBottomPanel();
			UpdateTopPanel();
		}
	}

	private void InstallSelectedEngine()
	{
		EngineSwapListItem engineSwapListItem = m_engineSwapList.GetSelectedItem();
		if (engineSwapListItem != null)
		{
			InstallEngine(engineSwapListItem.info.id);
		}
	}

	private void SetHoodVisible(bool hoodVisible)
	{
		if (m_currentHoodId == -1 || m_emptyHoodId == m_currentHoodId)
		{
			return;
		}
		RaceCar currentCar = m_car3dView.currentCar;
		if (m_prevHoodVisible != hoodVisible)
		{
			if (currentCar.IsLoadingVisualInProgress)
			{
				currentCar.OnCarVisualLoadFinishEvent += DelayedApplyVisuals;
			}
			else
			{
				RefreshHood();
			}
		}
		void DelayedApplyVisuals(RaceCar userCar)
		{
			if (!(userCar == null))
			{
				RefreshHood();
				userCar.OnCarVisualLoadFinishEvent -= DelayedApplyVisuals;
			}
		}
		void RefreshCarParts()
		{
			if (!hoodVisible)
			{
				if (CarPartDatabase.GetPartById(m_emptyHoodId).take.Count > 0)
				{
					m_slotScheme.Equip(m_emptyHoodId, ignoreForceParts: true);
					m_slotScheme.ToPartList(m_previewSettings.parts, forceAdd: true);
				}
				return;
			}
			m_previewSettings.parts.Clear();
			foreach (int prevPart in m_prevParts)
			{
				m_previewSettings.parts.Add(prevPart);
			}
		}
		void RefreshHood()
		{
			if (m_firstEnter)
			{
				SetupEmptyHood();
			}
			RefreshCarParts();
			m_car3dView.ApplyCustomization(CarVisualManager.CustomizationFlags.BodyParts, m_previewSettings, forceProfileApply: true);
			m_prevHoodVisible = hoodVisible;
		}
	}

	public void FocusCameraOnHood()
	{
		if (Singleton<CustomCameraPresets>.instance.currentPreset.stateType != CustomizationCameraPreset.Bonnet)
		{
			Singleton<CustomCameraPresets>.instance.CameraTo(CustomizationCameraPreset.Bonnet);
		}
	}

	private void UpdateBottomSetups()
	{
		UISetupsItem uISetupsItem = GetSelectedItem();
		if (uISetupsItem == null)
		{
			SelectItem(0);
			uISetupsItem = GetSelectedItem();
		}
		if (!uISetupsItem.locked.value)
		{
			if (!uISetupsItem.isCurrent.value)
			{
				m_actionHints.Add(new ActionHintData
				{
					hint = LocalizationKeys.nav_hint_select.Localized(),
					type = ActionType.NavSelectBtnA,
					action = SubmitInput
				});
			}
		}
		else
		{
			m_actionHints.Add(new ActionHintData
			{
				hint = LocalizationKeys.ShopBuy.Localized(),
				type = ActionType.NavSelectBtnA,
				action = SubmitInput
			});
		}
		if (uISetupsItem != null && uISetupsItem.current && UIDynostandContext.IsCanDynoAdjust(uISetupsItem.profileProxy.name, uISetupsItem.profileProxy.dyno))
		{
			m_actionHints.Add(new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_dyno.Localized(),
				type = ActionType.NavSelectBtnA,
				action = delegate
				{
					GoToDyno();
				}
			});
		}
	}

	private void UpdateBottomSwaps()
	{
		if (m_canShowSwaps.value)
		{
			return;
		}
		EngineSwapListItem engineSwapListItem = m_engineSwapList.GetSelectedItem();
		if (engineSwapListItem != null && engineSwapListItem.IsInstalled())
		{
			m_actionHints.Add(new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_dyno.Localized(),
				type = ActionType.NavSelectBtnA,
				action = delegate
				{
					GoToDyno();
				}
			});
		}
		else
		{
			m_actionHints.Add(new ActionHintData
			{
				hint = LocalizationKeys.swaps_install_button.Localized(),
				type = ActionType.NavSelectBtnA,
				action = InstallSelectedEngine
			});
		}
		m_actionHints.Add(new ActionHintData
		{
			hint = LocalizationKeys.swaps_rev_engine_button.Localized(),
			type = m_revEngineAction
		});
	}

	private void UpdateBottomCustomSetups()
	{
		CustomSetupItem customSetupItem = m_customSetupsList.GetSelectedItem();
		if (customSetupItem == null)
		{
			return;
		}
		if (customSetupItem.current && !customSetupItem.isPlusElement)
		{
			m_actionHints.Add(new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_dyno.Localized(),
				type = ActionType.NavSelectBtnA,
				action = delegate
				{
					GoToDyno();
				}
			});
			m_actionHints.Add(new ActionHintData
			{
				hint = LocalizationKeys.generic_copy.Localized(),
				type = ActionType.NavAdditional0BtnX,
				action = delegate
				{
					CopySetup(createCustom: false);
				}
			});
			m_actionHints.Add(new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_input_settings_remove_bind.Localized(),
				type = ActionType.NavAdditional1BtnY,
				action = DeleteCustomSetup
			});
		}
		else if (customSetupItem.isPlusElement)
		{
			m_actionHints.Add(new ActionHintData
			{
				hint = LocalizationKeys.network_game_btn_create.Localized(),
				type = ActionType.NavSelectBtnA,
				action = delegate
				{
					CopySetup(createCustom: true);
				}
			});
		}
		else
		{
			m_actionHints.Add(new ActionHintData
			{
				hint = LocalizationKeys.swaps_install_button.Localized(),
				type = ActionType.NavSelectBtnA,
				action = delegate
				{
					InstallCustomSetup(m_customSetupsList.GetSelectedItem());
				}
			});
			m_actionHints.Add(new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_input_settings_remove_bind.Localized(),
				type = ActionType.NavAdditional1BtnY,
				action = DeleteCustomSetup
			});
		}
	}

	private void UpdateBottomPanel()
	{
		m_actionHints.Clear();
		m_actionHints.Add(new ActionHintData
		{
			hint = LocalizationKeys.nav_hint_goto_back.Localized(),
			type = ActionType.NavCancelBtnB,
			action = GotoGarage
		});
		switch (m_mode)
		{
		case Mode.Setups:
			UpdateBottomSetups();
			break;
		case Mode.Swaps:
			UpdateBottomSwaps();
			break;
		case Mode.CustomSetups:
			UpdateBottomCustomSetups();
			break;
		}
		ShowBottomPanel(m_actionHints.ToArray());
	}

	private IEnumerator RefreshItemViewCoroutine(ISetupsItem item, int engineId)
	{
		if (item == null)
		{
			m_refreshItemCoroutine = null;
			yield break;
		}
		yield return null;
		yield return RefreshStats(item, engineId);
		UpdateBottomPanel();
		m_refreshItemCoroutine = null;
	}

	private UISetupsItem GetSelectedItem()
	{
		return GetItemAt(selectedIndex.value);
	}

	private UISetupsItem GetItemAt(int index)
	{
		if (index >= 0 && index < m_items.Count)
		{
			return m_items[index];
		}
		return null;
	}

	private UISetupsItem GetItemById(int id)
	{
		foreach (UISetupsItem item in m_items)
		{
			if (item.profileProxy.id == id)
			{
				return item;
			}
		}
		return null;
	}

	private int GetCurrentProfileId()
	{
		return m_carSettings.GetProfileIdForCar(m_carId);
	}

	private void SaveProfileId(int profileId)
	{
		m_carSettings.SetProfilePerCar(m_carId, profileId);
		SoundController.Play2DUISound(SoundSample.ProSetup);
		CarSwap.SaveStockEngineIfNeeded(carId, m_carSettings);
	}

	private void OnSelectCustomSetup()
	{
		StopRefreshCoroutine();
		CustomSetupItem customSetupItem = m_customSetupsList.GetSelectedItem();
		if (customSetupItem == null)
		{
			UpdateBottomPanel();
			return;
		}
		int engineId = m_engineId;
		CustomSetupItem customSetupItem2 = m_customSetupsList.GetSelectedItem();
		if (customSetupItem2 != null && !customSetupItem2.isPlusElement)
		{
			engineId = customSetupItem2.engineId;
		}
		m_refreshItemCoroutine = TimeController.StartNow(RefreshItemViewCoroutine(customSetupItem, engineId));
	}

	public void OnClickCustomUIItem(string setupId, bool isPlusItem)
	{
		bool flag = false;
		int i = 0;
		for (int count = m_customSetupsList.GetCount(); i < count; i++)
		{
			CustomSetupItem customSetupItem = m_customSetupsList[i];
			if (customSetupItem != null && (isPlusItem || !customSetupItem.isPlusElement) && customSetupItem.setupId == setupId)
			{
				flag = m_customSetupsIndexSelected == i;
				m_customSetupsIndexSelected = i;
				m_customSetupsList.selectedIndex = i;
				break;
			}
		}
		if (flag)
		{
			if (isPlusItem)
			{
				CopySetup(createCustom: true);
			}
			else
			{
				InstallCustomSetup(m_customSetupsList.GetSelectedItem());
			}
		}
		else
		{
			OnSelectCustomSetup();
		}
	}

	[GameHotkey(ActionType.NavRight, false, false)]
	private void NextSetup()
	{
		switch (m_mode)
		{
		case Mode.Setups:
			if (selectedIndex.value < m_items.Count)
			{
				SelectItem(selectedIndex.value + 1);
			}
			break;
		case Mode.Swaps:
			if (!m_canShowSwaps.value)
			{
				int count = m_engineSwapList.GetCount();
				int num2 = m_engineSwapList.GetSelectedIndex();
				num2++;
				if (num2 > count)
				{
					num2 = count;
				}
				else
				{
					m_engineSwapList.selectedIndex = num2;
				}
			}
			break;
		case Mode.CustomSetups:
		{
			m_customSetupsIndexSelected++;
			int num = m_customSetupsList.GetCount() - 1;
			if (m_customSetupsIndexSelected > num)
			{
				m_customSetupsIndexSelected = num;
				break;
			}
			m_customSetupsList.selectedIndex = m_customSetupsIndexSelected;
			OnSelectCustomSetup();
			break;
		}
		}
	}

	[GameHotkey(ActionType.NavLeft, false, false)]
	private void PrevSetup()
	{
		switch (m_mode)
		{
		case Mode.Setups:
			if (selectedIndex.value > 0)
			{
				SelectItem(selectedIndex.value - 1);
			}
			break;
		case Mode.Swaps:
			if (!m_canShowSwaps.value)
			{
				int num = m_engineSwapList.GetSelectedIndex();
				num--;
				if (num < 0)
				{
					num = 0;
				}
				else
				{
					m_engineSwapList.selectedIndex = num;
				}
			}
			break;
		case Mode.CustomSetups:
			m_customSetupsIndexSelected--;
			if (m_customSetupsIndexSelected < 0)
			{
				m_customSetupsIndexSelected = 0;
				break;
			}
			m_customSetupsList.selectedIndex = m_customSetupsIndexSelected;
			OnSelectCustomSetup();
			break;
		}
	}

	private void TryBuySetup(UISetupsItem setupItem)
	{
		CarProfileProxy carProfile = setupItem.profileProxy;
		if (m_prefs.money.coins < carProfile.cost)
		{
			UIMessageBox.ShowHint(LocalizationKeys.not_enough_money.Localized(), "", null, base.owner);
			return;
		}
		List<DialogButtonData> list = new List<DialogButtonData>();
		list.Add(new DialogButtonData
		{
			action = delegate
			{
				ECurrencyType currencyType = ECurrencyType.Coins;
				if (DependencyInjector.Resolve<TransactionManager>().TryToPerformUnlockTransaction(TransactionManager.UnlockType.Profile, carProfile.id, carProfile.cost, currencyType))
				{
					AnalyticsService.LogUnlockProfile(m_db.GetNameById<PlayerCar>(m_carId), carProfile.name);
					PerformSetupInstall(carProfile.id, carProfile);
					UpdateEngineSwapsAvailability(setupItem);
				}
			},
			text = LocalizationKeys.ShopBuy.Localized()
		});
		list.Add(new DialogButtonData
		{
			action = delegate
			{
			},
			text = LocalizationKeys.Cancel.Localized()
		});
		UIMessageBox.ShowDialog(LocalizationKeys.message_buy_track.LocalizedFormat(carProfile.name, UIUtils.DigitsWithSeparate(setupItem.priceValue)), list.Last()?.action, list.ToArray());
	}

	private void PerformSetupInstall(int profileId, CarProfileProxy profileProxy)
	{
		ResetHaveCustomSetupForCar();
		SaveProfileId(profileId);
		RefreshSetupItems();
		UpdateBottomPanel();
		UpdateTopPanel();
		if (!profileProxy.GetProfile().CanSwap())
		{
			m_engineId = 0;
		}
	}

	private void ResetSetupForAllSlots(string customSetupId, int engineId)
	{
		int activeSlotId = m_carSettings.GetActiveSlotId(m_carId);
		foreach (int usedSlot in m_carSettings.GetUsedSlots(m_carId))
		{
			m_carSettings.SetActiveSlotId(m_carId, usedSlot);
			(string, bool, int) haveCustomSetupsPerCar = m_carSettings.GetHaveCustomSetupsPerCar(m_carId);
			if (haveCustomSetupsPerCar.Item2 && haveCustomSetupsPerCar.Item1 == customSetupId && haveCustomSetupsPerCar.Item3 == engineId)
			{
				m_carSettings.SetHaveCustomSetupsPerCar(m_carId, 0, null, isHaveCustomSetup: false);
				CarSwap.SaveStockEngineIfNeeded(carId, m_carSettings);
			}
		}
		m_carSettings.SetActiveSlotId(m_carId, activeSlotId);
	}

	public int FindDefaultUnlockedTrackId()
	{
		foreach (DB.Meta.Track item in m_db.QueryAll<DB.Meta.Track>())
		{
			if (!item.defaultLocked)
			{
				return item.id;
			}
		}
		return -1;
	}
}
