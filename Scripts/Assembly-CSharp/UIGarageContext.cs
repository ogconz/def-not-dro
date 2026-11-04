using System;
using System.Collections.Generic;
using System.Globalization;
using Binding;
using Binding.Base;
using Customization;
using DB;
using DB.Meta;
using DI;
using GameInput;
using GameOverlay;
using PrefsModels;
using TrackLayout.Data;
using UI;
using UnityEngine;

[AudioFXBlocker(false)]
public class UIGarageContext : BaseBindingBehaviourTarget
{
	private enum EStateType
	{
		Default = 0,
		CarLocked = 1
	}

	public enum CarFilterType
	{
		All = 0,
		Vendor = 1,
		Favourite = 2
	}

	public ElementNavigate[] buttonsNavigate;

	public Sprite[] classGradients;

	public Color[] classColors;

	public SwitcherActions switcherActions = new SwitcherActions();

	public readonly Property<int> indexCategory = new IntProperty(1);

	public readonly Property<string> carName = new StringProperty();

	public readonly Property<string> carClass = new StringProperty();

	public readonly Property<Sprite> spriteClassGradient = new SpriteProperty();

	public readonly Property<string> profileName = new StringProperty();

	public readonly Property<string> coinFactor = new StringProperty();

	public readonly Property<bool> isShowUnlock = new BoolProperty();

	public readonly Property<bool> isShowUnlockDLC = new BoolProperty();

	public readonly Property<bool> isLockedByLevel = new BoolProperty();

	public readonly Property<int> requiredLevel = new IntProperty();

	public readonly Property<int> moneyType = new IntProperty();

	public readonly Property<string> cost = new StringProperty();

	public readonly Property<string> dlcName = new StringProperty();

	public readonly Property<string> setupIcon = new StringProperty(null);

	public readonly Property<string> setupsCaption = new StringProperty();

	public readonly Property<string> customSetupName = new StringProperty();

	public readonly Property<bool> showCustomSetupName = new BoolProperty();

	public readonly Property<int> engineSwap = new IntProperty();

	private readonly StringProperty m_selectType = new StringProperty();

	private readonly Property<bool> m_customSetup = new BoolProperty();

	private readonly SpriteProperty m_vendorIcon = new SpriteProperty();

	private readonly BoolProperty m_isVendorFilter = new BoolProperty();

	[ContextDependency]
	private GarageCar3dViewContext m_car3dView;

	[Dependency]
	private GamePrefs m_prefs;

	[Dependency]
	private BaseModel m_baseModel;

	[Dependency]
	private PlayerCarFactory m_carFactory;

	private CarVendorsAsset m_carVendorsAsset;

	private CameraRotation m_cameraRotation;

	private EventSettings m_eventSettings;

	private BaseSharingConsole m_vinylSharingManager;

	private int m_eventsTracksCount;

	private float m_smoothVelo;

	private float m_targetSteer;

	private float m_currentSteer;

	private CarFilterType m_currCarFilterType;

	private bool m_isMetaVendorApplied;

	public GarageCar3dViewContext car3dView => m_car3dView;

	public GamePrefs prefs => m_prefs;

	public BaseModel baseModel => m_baseModel;

	public PlayerCarFactory carFactory => m_carFactory;

	public void SwitchTab(bool next)
	{
		Array carFilterTypes;
		if (m_car3dView.allCarLoaded)
		{
			carFilterTypes = Enum.GetValues(typeof(CarFilterType));
			int num = GetNextFilterIndex((int)m_currCarFilterType);
			if ((CarFilterType)carFilterTypes.GetValue(num) == CarFilterType.Favourite && m_prefs.favoriteCars.cars.Count == 0)
			{
				num = GetNextFilterIndex(num);
			}
			m_currCarFilterType = (CarFilterType)carFilterTypes.GetValue(num);
			UpdateCarFilterSelection();
			m_car3dView.RebuildCarList(m_car3dView.selectedCarData.vendorId);
			SetVendorIcon(m_car3dView.currentCar.metaInfo.vendorId);
		}
		int GetNextFilterIndex(int currIndex)
		{
			int length = carFilterTypes.Length;
			int num2 = ((!next) ? (--currIndex) : (++currIndex));
			if (num2 > length - 1)
			{
				num2 = 0;
			}
			if (num2 < 0)
			{
				num2 = length - 1;
			}
			return num2;
		}
	}

	private void SetVendorIcon(int vendorId)
	{
		if (m_currCarFilterType != CarFilterType.Vendor)
		{
			return;
		}
		foreach (CarVendor vendor in m_carVendorsAsset.Vendors)
		{
			if (vendorId == vendor.id)
			{
				m_vendorIcon.value = vendor.sprite;
				break;
			}
		}
	}

	public CarFilterType GetCurrentFilter()
	{
		return m_currCarFilterType;
	}

	private void UpdateCarFilterSelection()
	{
		StringProperty selectType = m_selectType;
		selectType.value = m_currCarFilterType switch
		{
			CarFilterType.All => LocalizationKeys.car_catalog_category_all.Localized(), 
			CarFilterType.Vendor => LocalizationKeys.car_garage_vendor_filter.Localized(), 
			CarFilterType.Favourite => LocalizationKeys.car_catalog_category_favorites.Localized(), 
			_ => throw new NotImplementedException(), 
		};
		m_isVendorFilter.value = m_currCarFilterType == CarFilterType.Vendor;
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		Overlay.instance.richPresence.Show(RichPresenceStatus.MainMenu);
		base.owner.Hide<UIBackgroundContext>();
		m_prefs.money.ApplyDLCMoney();
		m_carVendorsAsset = (States.mainCurrent as GarageGUIState)?.carVendorsAsset;
		if (m_prefs.money.coins < 0)
		{
			GlobalGameSettings single = DependencyInjector.Resolve<BaseModel>().GetSingle<GlobalGameSettings>();
			m_prefs.money.coins = single.startCoins * 10;
		}
		m_currCarFilterType = m_prefs.globals.carFilterType;
		if ((m_currCarFilterType == CarFilterType.Favourite && !m_prefs.favoriteCars.IsFavorite(car3dView.currentCar.carId)) || !Overlay.instance.IsDLCInstalled(car3dView.currentCar.metaInfo.dlcCode))
		{
			m_currCarFilterType = CarFilterType.All;
		}
		car3dView.RebuildCarList(m_car3dView.selectedCarData.vendorId, selectedFromList: true);
		UICameraRotationContext uICameraRotationContext = base.owner.Get<UICameraRotationContext>();
		if (uICameraRotationContext != null && !uICameraRotationContext.gameObject.activeSelf)
		{
			base.owner.Add<UICameraRotationContext>();
		}
		m_cameraRotation = CameraRotation.instance;
		if (m_cameraRotation != null)
		{
			m_cameraRotation.useZoom = true;
		}
		setupsCaption.value = LocalizationKeys.setups_window_title.Localized() + " / " + LocalizationKeys.swaps_mode_swaps.Localized();
		m_car3dView.Set3DModelVisible(value: true);
		SwitchToGarage();
		m_car3dView.SelectedCarChangedEvent += OnCarChangedHandler;
		OnCarChangedHandler();
		GameEvents.OnOverlayStateChanged += new GameEventHandler<GameEvents.OverlayStateInfo>(OnOverlayStateChangedHander);
		if (buttonsNavigate.Length != 0 && base.selectedItem == null)
		{
			buttonsNavigate[0].Select();
		}
		BringForwardByDepth();
		ChangeNotPurchasedCar();
		m_car3dView.SetGarageCarStatePhysAndHoldPosition(disablePhysics: true, holdPosition: false);
		if (m_vinylSharingManager == null)
		{
			m_vinylSharingManager = BaseSharingConsole.Create<VinylSharingManager>();
		}
		m_vinylSharingManager?.RegisterConsoleCommands();
		m_car3dView.CreateDlcCarsIfNeeded();
		UpdateCarFilterSelection();
		if (!m_isMetaVendorApplied)
		{
			SetVendorIcon(m_car3dView.currentCar.metaInfo.vendorId);
		}
	}

	private void UpdateEventInfo()
	{
		if (m_eventSettings == null)
		{
			m_eventSettings = ResourcesWrapper.Load<EventSettings>("HalloweenEventSettings");
		}
		if (m_eventsTracksCount == 0)
		{
			foreach (DB.Meta.Track item in m_baseModel.QueryAll<DB.Meta.Track>())
			{
				if (!(item.configuration != "Default") && item.enabled && !item.hidden && TrackMetaDB.HaveConfig(item.scene, RaceMode.HalloweenEvent))
				{
					m_eventsTracksCount++;
				}
			}
		}
		if (m_prefs.eventsData.pumpkinsCount >= m_eventSettings.maxCountPumpkins && m_prefs.eventsData.halloweenLocksTracks.Count == m_eventsTracksCount)
		{
			m_prefs.eventsData.halloweenComplete = true;
			StatsManager.IncStat(StatsManager.EStats.HalloweenWin);
		}
	}

	protected override void OnDeactivate()
	{
		m_vinylSharingManager?.UnregisterConsoleCommands();
		m_prefs.globals.carFilterType = m_currCarFilterType;
		m_prefs.globals.Save();
		if (m_cameraRotation != null)
		{
			m_cameraRotation.useZoom = false;
		}
		GameEvents.OnOverlayStateChanged -= new GameEventHandler<GameEvents.OverlayStateInfo>(OnOverlayStateChangedHander);
		base.owner.Hide<UICameraRotationContext>();
		HideTopPanel();
		HideBottomPanel();
		m_car3dView.SetCarsSound(enable: true);
		m_car3dView.SelectedCarChangedEvent -= OnCarChangedHandler;
		base.OnDeactivate();
	}

	private void Update()
	{
		if (m_car3dView != null)
		{
			RaceCar raceCar = m_car3dView.GetRaceCar();
			if (raceCar != null)
			{
				m_currentSteer = Mathf.SmoothDamp(raceCar.carX.GetSteer(), m_targetSteer, ref m_smoothVelo, 0.2f);
				raceCar.carX.SetSteer(m_currentSteer, useAssistent: false, Time.deltaTime);
			}
		}
	}

	public void UpdateInfo()
	{
		PlayerCar selectedCarData = m_car3dView.selectedCarData;
		carName.value = selectedCarData.name;
		carClass.value = FormatClassCar(selectedCarData);
		profileName.value = PlayerCarProfileMetaExtensions.GetProfileIconNameByCar(m_car3dView.GetCarId().Value);
		coinFactor.value = selectedCarData.coinsFactor.ToString(CultureInfo.InvariantCulture);
		int lastCarId = m_prefs.globals.lastCarId;
		PrefsCarSlotsModel carSettings = m_prefs.carSettings;
		CustomProfile installedCustomProfile = carSettings.GetInstalledCustomProfile(lastCarId);
		m_customSetup.value = installedCustomProfile != null;
		customSetupName.value = installedCustomProfile?.name;
		showCustomSetupName.value = !string.IsNullOrEmpty(customSetupName.value);
		if (selectedCarData.coinsFactor <= (float)classGradients.Length)
		{
			spriteClassGradient.value = GetClassGradient(selectedCarData);
		}
		int engineIdForCar = carSettings.GetEngineIdForCar(selectedCarData.id);
		engineSwap.value = engineIdForCar;
		setupIcon.value = PlayerCarProfileMetaExtensions.GetProfileIconNameByCar(m_car3dView.GetCarId().Value);
	}

	public static string FormatClassCar(float classCar)
	{
		return LocalizationService.LocalizeFormat("catalog_group_title", $"{classCar:F0}");
	}

	public string FormatClassCar(PlayerCar car)
	{
		return FormatClassCar(car.coinsFactor);
	}

	public Sprite GetClassGradient(float classCar)
	{
		return classGradients[(int)classCar - 1];
	}

	public Sprite GetClassGradient(PlayerCar car)
	{
		return GetClassGradient(car.coinsFactor);
	}

	public Color GetClassColor(float classCar)
	{
		return classColors[(int)classCar - 1];
	}

	public Color GetClassColor(PlayerCar car)
	{
		return GetClassColor(car.coinsFactor);
	}

	private void ClearUnlcokItems()
	{
		isShowUnlock.value = false;
		isShowUnlockDLC.value = false;
	}

	private void SetupUnlockButtons(GarageCar3dViewContext.CarLockedInfo info)
	{
		if (info.dlcCode == DLCCode.Unknown)
		{
			isShowUnlock.value = info.locked;
			isShowUnlockDLC.value = false;
			moneyType.value = (int)info.currencyType;
			cost.value = UIUtils.DigitsWithSeparate(info.cost);
		}
		else
		{
			isShowUnlock.value = false;
			isShowUnlockDLC.value = !Overlay.instance.IsDLCInstalled(info.dlcCode);
		}
	}

	private void UpdateLockedInfo()
	{
		ClearUnlcokItems();
		if (m_car3dView.IsSelected())
		{
			GarageCar3dViewContext.CarLockedInfo carLockedInfo = m_car3dView.GetCarLockedInfo();
			DLCCode dLCCode = m_car3dView.GetDLCCode();
			requiredLevel.value = m_prefs.globals.GetLockLevelForFeature(m_car3dView.AsGameFeature());
			isLockedByLevel.value = dLCCode == DLCCode.Unknown && !CarPartDatabase.IsCarUnlockedByDLC(m_car3dView.GetCarId().Value) && m_prefs.playerProfile.level < requiredLevel.value;
			if (isLockedByLevel.value)
			{
				base.state = 1;
				dlcName.value = "";
				return;
			}
			if (carLockedInfo.locked)
			{
				SetupUnlockButtons(carLockedInfo);
				base.state = 1;
			}
			else
			{
				base.state = 0;
			}
			dlcName.value = ((dLCCode == DLCCode.Unknown) ? "" : Overlay.instance.GetDLCName(dLCCode));
		}
		else
		{
			base.state = 0;
		}
	}

	public int FindDefaultUnlockedTrackId()
	{
		foreach (DB.Meta.Track item in m_baseModel.QueryAll<DB.Meta.Track>())
		{
			if (!item.defaultLocked)
			{
				return item.id;
			}
		}
		return -1;
	}

	private void OnCarChangedHandler()
	{
		UpdateInfo();
	}

	private void OnOverlayStateChangedHander(GameEvents.OverlayStateInfo info)
	{
		OnCarChangedHandler();
	}

	public void GoToCustomize()
	{
		TryEnterContextIfCarLoaded<UIBodyCustomizationContext>();
	}

	public void GoToPaint()
	{
		TryEnterContextIfCarLoaded<UIPaintCustomizationContext>();
	}

	public void GoToDesign()
	{
		TryEnterContextIfCarLoaded<UIDesignMenuContext>();
	}

	public void GoToSetup()
	{
		TryEnterContextIfCarLoaded<UISetupsContext>();
	}

	public void GoToSlots()
	{
		TryEnterContextIfCarLoaded<UICarSlotsContext>();
		if (base.owner.IsActive<UICarSlotsContext>())
		{
			base.owner.Get<UICarSlotsContext>().Init(car3dView.GetCarId().Value);
		}
	}

	private void TryEnterContextIfCarLoaded<T>() where T : BaseContext
	{
		RaceCar raceCar = m_car3dView.GetRaceCar();
		if (raceCar != null && raceCar.IsVisualLoaded)
		{
			Hide();
			base.owner.Push<T>();
		}
		else
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.loading_car.Localized(), 2f);
		}
	}

	public void GoToOldDyno()
	{
		Hide();
		if (m_prefs.globals.lastTrackId == -1)
		{
			m_prefs.globals.lastTrackId = FindDefaultUnlockedTrackId();
		}
		UIDynostandContext.ResetCachedSwap();
		base.owner.Push<UIDynostandContext>();
	}

	public bool IsCanEnterToDyno()
	{
		int? carId = m_car3dView.GetCarId();
		if (carId.HasValue)
		{
			int profileIdForCar = m_prefs.carSettings.GetProfileIdForCar(carId.Value);
			PlayerCarProfile byId = m_baseModel.GetById<PlayerCarProfile>(profileIdForCar);
			if (byId != null)
			{
				return UIDynostandContext.IsCanDynoAdjust(byId.name, byId.dynoInfo);
			}
		}
		return false;
	}

	private bool HasQuestRewardCar(int carId)
	{
		return m_prefs.questsInventory.rewardAvailableCarItems.Contains(carId);
	}

	public void GoToDyno()
	{
		RaceCar raceCar = m_car3dView.GetRaceCar();
		if (raceCar == null)
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.loading_car.Localized(), 2f);
			return;
		}
		if (!IsCanEnterToDyno())
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.garage_hint_profile_dyno_stock.Localized());
			return;
		}
		Hide();
		if (m_prefs.globals.lastTrackId == -1)
		{
			m_prefs.globals.lastTrackId = FindDefaultUnlockedTrackId();
		}
		m_car3dView.Set3DModelVisible(value: true);
		m_car3dView.SetGarageCarStatePhysAndHoldPosition(disablePhysics: false, holdPosition: false);
		UIDynostandContext.ResetCachedSwap();
		UIDynostandContext uIDynostandContext = base.owner.Push<UIDynostandContext>().Init(raceCar);
		if (uIDynostandContext != null)
		{
			uIDynostandContext.SetOpenedFrom(UIDynostandContext.OpenedFrom.Garage);
		}
	}

	public void Selection()
	{
		if (indexCategory.value == 0)
		{
			SubmitCurrentSelected();
		}
	}

	public void GoToBack()
	{
		switch (indexCategory.value)
		{
		case 0:
			Hide();
			base.owner.Push<UIMainMenuContext>();
			break;
		case 1:
			SwitchToGarage();
			BringForwardByDepth();
			break;
		}
	}

	public void SelectNextCar()
	{
		if (!base.owner.IsActive<UIMainMenuContext>())
		{
			RaceCar raceCar = m_car3dView.GetRaceCar();
			if ((bool)raceCar && raceCar.IsVisualLoaded && raceCar.engineModelLoaded)
			{
				m_car3dView.SelectNextCar();
				switcherActions.Select(1);
			}
		}
	}

	public void SelectPrevCar()
	{
		if (!base.owner.IsActive<UIMainMenuContext>())
		{
			RaceCar raceCar = m_car3dView.GetRaceCar();
			if ((bool)raceCar && raceCar.IsVisualLoaded && raceCar.engineModelLoaded)
			{
				m_car3dView.SelectPrevCar();
				switcherActions.Select(0);
			}
		}
	}

	public void SwitchToCatalog()
	{
		if (m_car3dView.GetRaceCar() == null)
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.loading_car.Localized(), 2f);
			return;
		}
		SwitchTo(1);
		UpdateInfo();
	}

	public void SwitchToGarage()
	{
		SwitchTo(0);
		ShowTopPanel(ETopPanelState.Title, LocalizationKeys.garage_window_title.Localized());
		if (switcherActions.listActions.Count == 0)
		{
			switcherActions.AddActions(new HeaderActionData[2]
			{
				new HeaderActionData
				{
					text = LocalizationKeys.garage_switch_prev_car.Localized(),
					action = SelectPrevCar
				},
				new HeaderActionData
				{
					text = LocalizationKeys.garage_switch_next_car.Localized(),
					action = SelectNextCar
				}
			});
		}
		switcherActions.Select(0);
		BringForwardByDepth();
	}

	public void SwitchTo(int index)
	{
		indexCategory.value = index;
		switch (index)
		{
		case 0:
			if (base.owner.IsActive<UICarSelectorContext>())
			{
				base.owner.Hide<UICarSelectorContext>();
			}
			ShowTopPanel(ETopPanelState.Title, LocalizationKeys.garage_window_title.Localized());
			m_car3dView.Set3DModelVisible(value: true);
			if ((bool)m_cameraRotation)
			{
				m_cameraRotation.enabled = true;
			}
			ShowBottomPanel(new ActionHintData[2]
			{
				new ActionHintData
				{
					hint = LocalizationKeys.nav_hint_goto_back.Localized(),
					type = ActionType.NavCancelBtnB,
					action = GoToBack
				},
				new ActionHintData
				{
					hint = LocalizationKeys.nav_hint_select.Localized(),
					type = ActionType.NavSelectBtnA,
					action = Selection
				}
			});
			break;
		case 1:
		{
			UICarSelectorContext uICarSelectorContext = base.owner.HideStackAndPush<UICarSelectorContext>();
			uICarSelectorContext.onCarSelected = OnCarSelected;
			uICarSelectorContext.goBackAction = base.owner.Hide<UICarSelectorContext>;
			if ((bool)m_cameraRotation)
			{
				m_cameraRotation.enabled = false;
			}
			break;
		}
		}
	}

	private void OnCarSelected(UICarSelectorCar car)
	{
		PlayerCar meta = car.meta;
		int num = ((!meta.questReward) ? prefs.globals.GetLockLevelForFeature(meta.AsGameFeature(), meta.id) : 0);
		if (m_currCarFilterType == CarFilterType.Favourite && !car.isFavorite)
		{
			m_currCarFilterType = CarFilterType.All;
			m_prefs.globals.carFilterType = m_currCarFilterType;
			UpdateCarFilterSelection();
		}
		m_car3dView.RebuildCarList(meta.vendorId, selectedFromList: true);
		SetVendorIcon(meta.vendorId);
		m_isMetaVendorApplied = true;
		if (Singleton<GameplayEvents>.instance.IsContentAvailable(meta.eventCode) && CarPartDatabase.IsCarUnlockedByDLC(meta.id))
		{
			SelectCar(meta);
			return;
		}
		if (meta.eventCode != GameEventCode.Unknown)
		{
			if (!Singleton<GameplayEvents>.instance.IsContentAvailable(meta.eventCode))
			{
				Singleton<GameplayEvents>.instance.ShowEventDialog(base.owner);
			}
			else
			{
				SelectCar(meta);
			}
			return;
		}
		if (meta.dlcCode != DLCCode.Unknown)
		{
			if (!Overlay.instance.IsDLCInstalled(meta.dlcCode))
			{
				UIMessageBox.ShowDLCDialog(LocalizationKeys.no_dlc_car.Localized(), meta.dlcCode);
			}
			else
			{
				SelectCar(meta);
			}
			return;
		}
		if (prefs.playerProfile.level < num)
		{
			UIMessageBox.ShowHint(LocalizationKeys.locked_level_message_track.LocalizedFormat(num), "", null, base.owner);
			return;
		}
		if (!prefs.locks.GetCarLockState(meta.id) || HasQuestRewardCar(meta.id))
		{
			SelectCar(meta);
			return;
		}
		if (meta.questReward)
		{
			UIMessageBox.ShowHint(LocalizationKeys.available_only_in_quests.Localized(), "", null, base.owner);
			return;
		}
		if (prefs.money.coins < meta.cost)
		{
			UIMessageBox.ShowHint(LocalizationKeys.not_enough_money.Localized(), "", null, base.owner);
			return;
		}
		List<DialogButtonData> list = new List<DialogButtonData>
		{
			new DialogButtonData
			{
				action = delegate
				{
					if (DependencyInjector.Resolve<TransactionManager>().TryToPerformUnlockTransaction(TransactionManager.UnlockType.Car, meta.id, meta.cost, ECurrencyType.Coins))
					{
						AnalyticsService.LogUnlockCar(meta.name);
					}
					SelectCar(meta);
					ActivityFeedService.ProcessStory(ActivityFeedService.StoryType.BuyCar, meta.name);
				},
				text = LocalizationKeys.ShopBuy.Localized()
			},
			new DialogButtonData
			{
				action = delegate
				{
				},
				text = LocalizationKeys.Cancel.Localized()
			}
		};
		UIMessageBox.ShowDialog(LocalizationKeys.message_buy_track.LocalizedFormat(meta.name, car.costStr), list.Last()?.action, list.ToArray());
	}

	private void SelectCar(PlayerCar car)
	{
		m_prefs.globals.lastCarId = car.id;
		m_prefs.globals.Save();
		car3dView.SelectCarById(car.id);
		SwitchToGarage();
	}

	private void ChangeNotPurchasedCar()
	{
		if (car3dView.GetCarLockedInfo().locked)
		{
			GlobalGameSettings single = m_baseModel.GetSingle<GlobalGameSettings>();
			car3dView.SelectCarById(single.startFocusedCar);
			GoToBack();
		}
	}
}
