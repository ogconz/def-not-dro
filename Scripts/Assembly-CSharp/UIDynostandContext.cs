using System;
using System.Collections.Generic;
using System.Globalization;
using Binding;
using Binding.Base;
using CarProfile;
using CarX;
using DB;
using DB.Meta;
using DI;
using Db.Meta.CarProfile;
using GameInput;
using GameOverlay;
using LocalCaching.Manager;
using PrefsModels;
using SyncMultiplayer;
using TMPro;
using UnityEngine;

[AudioFXBlocker(false)]
public class UIDynostandContext : BaseBindingBehaviourTarget
{
	public enum OpenedFrom
	{
		Garage = 0,
		Setups = 1,
		Swaps = 2,
		CustomSetups = 3,
		SetupCreation = 4
	}

	public enum NavLayer
	{
		Left = 0,
		Right = 1
	}

	private enum SwitcherSuspensionSubCategoryMode
	{
		Angles = 0,
		Stiffness = 1
	}

	public const string KEY_DIFF_TYPE = "diff_type";

	public const string KEY_DIFF_LOCK_COEF = "diff_lock_coef";

	public const string KEY_DIFF_PRELOAD = "diff_preload";

	public const string KEY_DIFF_LOCK_POWER = "diff_lock_power";

	public const string KEY_DIFF_LOCK_COAST = "diff_lock_coast";

	public const string KEY_SEQ_TRANSMISSION_SOUND = "seq_transmision_sound";

	public const string KEY_WASTEGATE_SOUND = "wastegate_sound";

	public Action<NavLayer, int> navigationEvent;

	[SerializeField]
	private TextMeshProUGUI m_description;

	[SerializeField]
	private DynoParamsList m_suspensionRearParams;

	[SerializeField]
	private DynoParamsList m_suspensionFrontParams;

	[SerializeField]
	private DynoParamsList m_stiffnessRearParams;

	[SerializeField]
	private DynoParamsList m_stiffnessFrontParams;

	[SerializeField]
	private DynoParamsList m_wheelsRearParams;

	[SerializeField]
	private DynoParamsList m_wheelsFrontParams;

	[SerializeField]
	private DynoParamsList m_brakesParams;

	[SerializeField]
	private DynoParamsList m_engineParams;

	[SerializeField]
	private DynoParamsList m_transmissionParams;

	[SerializeField]
	private DynoParamsList m_weightParams;

	[SerializeField]
	private UIWheelAnglePanel m_wheelAnglePanel;

	private DynoSteeringModel m_dynoSteeringModel = new DynoSteeringModel();

	private Dictionary<CarProfileControllerType, List<DynoParamsList>> m_params;

	[SerializeField]
	private EngineGraph m_engineGraph;

	[Dependency]
	private GamePrefs m_prefs;

	[Dependency]
	private BaseModel m_model;

	[Dependency]
	private PlayerCarFactory m_carFactory;

	[Dependency]
	private BaseModel m_db;

	private static EngineSwapData m_cachedSwap;

	private static List<CarProfileControllerType> m_forceCategoriesToActivate = new List<CarProfileControllerType> { CarProfileControllerType.SuspensionStiffness };

	private PlayerCarProfile m_playerCarProfile;

	private PlayerCarProfile m_defaultCarProfile;

	private DynoMenuState m_dynoState;

	private BaseSharingConsole m_setupSharingManager;

	private float m_entryTime;

	private NavLayer m_navLayer;

	private List<BaseDynoParamsItemViewModel> m_itemsModel = new List<BaseDynoParamsItemViewModel>();

	private List<string> m_controllersToCheckRelyOnStates = new List<string> { "traction_control", "drivetrain_type", "front_tire_type", "rear_tire_type", "max_torque", "max_torque_rpm", "rev_limiter", "turbo_charged", "turbo_pressure", "mass" };

	private bool m_rebuildingElements;

	private bool m_isDynoStateCurrent;

	private bool m_contextWasHidden;

	public bool m_haveCustomSetupInstalled;

	private UIInGameMenuContext m_exitDialogOwner;

	private const float UPDATE_VISUAL_TIMER_LIMIT = 0.03f;

	private float? m_updateVisualTimer;

	private bool m_needWaitFrame;

	private CarProfileControllerType m_currentCategory;

	public readonly Property<string> categoryFilterTitle = new StringProperty();

	public readonly Property<bool> suspensionHintsVisible = new BoolProperty(false);

	public readonly Property<bool> showInfo = new BoolProperty(false);

	public readonly Property<bool> needSubmitIcon = new BoolProperty();

	public readonly Property<string> titleParameter = new StringProperty();

	public readonly Property<string> descriptionParameter = new StringProperty();

	public readonly Property<bool> showCustomSetupName = new BoolProperty(false);

	public readonly Property<string> customSetupName = new StringProperty();

	public readonly Property<string> engineHp = new StringProperty();

	public readonly Property<string> engineTorque = new StringProperty();

	public readonly Property<string> engineSpeed = new StringProperty();

	public readonly Property<string> engineAccel = new StringProperty();

	public readonly Property<string> powerDensity = new StringProperty();

	public readonly Property<string> engineAccelerationTo = new StringProperty();

	public readonly Property<string> powerDensityTitle = new StringProperty();

	public readonly Property<string> engineGraphMark1 = new StringProperty();

	public readonly Property<string> engineGraphMark2 = new StringProperty();

	public readonly Property<string> engineGraphMark3 = new StringProperty();

	public readonly Property<string> frontCamberHint = new StringProperty();

	public readonly Property<string> rearCamberHint = new StringProperty();

	public readonly Property<string> frontToeHint = new StringProperty();

	public readonly Property<string> rearToeHint = new StringProperty();

	private readonly IntProperty m_currDynoPanelVisibility = new IntProperty();

	private readonly IntProperty m_switcherSubCategoryMode = new IntProperty();

	private readonly BoolProperty m_switcherSubCategoryVisibility = new BoolProperty();

	private readonly BoolProperty m_steeringResetHintVisibility = new BoolProperty();

	private Dictionary<CarProfileControllerType, string> m_categoriesLocalizationKeys = new Dictionary<CarProfileControllerType, string>
	{
		{
			CarProfileControllerType.Suspension,
			"car_profile_category_caption_suspension"
		},
		{
			CarProfileControllerType.Wheels,
			"car_profile_category_caption_wheels"
		},
		{
			CarProfileControllerType.Engine,
			"car_profile_category_caption_engine"
		},
		{
			CarProfileControllerType.Transmission,
			"car_profile_category_caption_transmission_1"
		},
		{
			CarProfileControllerType.Brakes,
			"car_profile_category_caption_brakes_1"
		},
		{
			CarProfileControllerType.Weight,
			"car_profile_category_caption_weight"
		}
	};

	private float m_cameraLastZoom;

	private BaseDynoParamsItemViewModel m_diffLockCoefParam;

	private BaseDynoParamsItemViewModel m_diffPreloadParam;

	private BaseDynoParamsItemViewModel m_diffLockPowerParam;

	private BaseDynoParamsItemViewModel m_diffLockCoastParam;

	private OpenedFrom m_openedFrom;

	private SwapsAsset m_swapsAsset;

	private CustomProfile m_installedCustomProfile;

	private bool m_swapApplied;

	private bool m_openedFromCustomCreation;

	private List<HeaderActionData> m_categoriesList = new List<HeaderActionData>();

	private List<DynoParamsList> m_currDynoParamsListsView = new List<DynoParamsList>();

	private List<ActionHintData> m_bottomListHintsData = new List<ActionHintData>();

	private CustomSetupSaveHelper m_customSetupSaveHelper;

	private readonly Property<bool> m_anglePanelVisible = new BoolProperty();

	public ProfilesInfo.Profile profileInfoCached { get; private set; }

	public ProfilesInfo.Profile profileInfoPreview { get; private set; }

	public int profileId { get; private set; }

	public int carId { get; private set; }

	public RaceCar userCar { get; private set; }

	public PlayerCarFactory carFactory => m_carFactory;

	public BaseModel model => m_model;

	public List<BaseDynoParamsItemViewModel> items => m_itemsModel;

	public CarProfileControllerType currentCategory => m_currentCategory;

	protected override void OnActivate()
	{
		base.OnActivate();
		Overlay.instance.richPresence.Show(RichPresenceStatus.CarTuning);
		if (m_customSetupSaveHelper == null)
		{
			m_customSetupSaveHelper = new CustomSetupSaveHelper();
		}
		m_swapApplied = false;
		m_swapsAsset = SwapsAsset.LoadAndInitDB();
		m_dynoState = States.State<DynoMenuState>();
		m_isDynoStateCurrent = States.current is DynoMenuState;
		if (!m_isDynoStateCurrent)
		{
			ResetCachedSwap();
		}
		CameraRotation instance = CameraRotation.instance;
		if (instance != null)
		{
			m_cameraLastZoom = instance.distanceFromCenter;
		}
		SetActiveCameraRotation(active: false);
		EnableCameraIdleRotate(cameraIdleRotate: false);
		carId = m_prefs.globals.lastCarId;
		CarSwap.SaveStockEngineIfNeeded(carId, m_prefs.carSettings);
		InitCustomSetupInfo();
		profileId = m_prefs.carSettings.GetProfileIdForCar(carId);
		profileInfoCached = m_prefs.carSettings.GetProfileInfo(profileId, carId, m_installedCustomProfile?.id);
		m_playerCarProfile = m_model.QueryRecordsByColumnValue("carId", carId, (PlayerCarProfile x) => x.carId == carId).Find((PlayerCarProfile x) => x.id == profileId).Copy();
		m_defaultCarProfile = m_playerCarProfile.Copy();
		m_openedFromCustomCreation = m_openedFrom == OpenedFrom.SetupCreation;
		if (m_params == null)
		{
			InitParamsList();
		}
		BasePrefsModel.LockSaves(flag: true);
		if (m_setupSharingManager == null)
		{
			m_setupSharingManager = BaseSharingConsole.Create<SetupSharingManager>();
		}
		m_setupSharingManager?.RegisterConsoleCommands();
		SubscribePS4TextReceive();
		if (Overlay.instance is SteamOverlay steamOverlay && GameManager.IsLinuxOsPlatform())
		{
			steamOverlay.onCloseOverlayKeyboard = (Action<string>)Delegate.Combine(steamOverlay.onCloseOverlayKeyboard, new Action<string>(CreateCustomSetup));
		}
		if (States.current is GarageGUIState && userCar != null)
		{
			Init(userCar);
		}
	}

	public UIDynostandContext Init(RaceCar car)
	{
		userCar = car;
		m_customSetupSaveHelper.InitHelper(userCar, m_playerCarProfile, profileInfoPreview, profileId, carId, m_swapApplied);
		m_customSetupSaveHelper.ApplySavedSwap(profileInfoCached, m_haveCustomSetupInstalled, m_openedFromCustomCreation);
		if (profileInfoPreview == null || States.current is GarageGUIState)
		{
			ResetPreview();
		}
		else
		{
			userCar.ApplyAdvancedProfile(m_model, profileInfoPreview, m_prefs.carSettings, reset: false, skipSwapControllers: false, m_cachedSwap, m_installedCustomProfile?.id);
		}
		m_carFactory.GarageApplyProfile(userCar, profileId);
		m_dynoSteeringModel.InitCar(userCar);
		m_wheelAnglePanel.SetDynoSteeringModel(m_dynoSteeringModel);
		m_wheelAnglePanel.ResetSteeringValue();
		InitCategoriesList();
		FillParamsListsByCategory(currentCategory);
		UpdateToeCamberTopAndBottomLists();
		UpdateBottomPanel();
		return this;
	}

	public void Import(DynoDto dynoDto)
	{
		DependencyInjector.InjectObjectsInto(this);
		carId = dynoDto.carId;
		profileId = dynoDto.profileId;
		profileInfoPreview = dynoDto.preview;
		profileInfoCached = dynoDto.cached;
		m_playerCarProfile = dynoDto.currentDbData;
		m_defaultCarProfile = dynoDto.defaultDbData;
	}

	public DynoDto Export()
	{
		return new DynoDto
		{
			carId = carId,
			profileId = profileId,
			preview = profileInfoPreview,
			cached = profileInfoCached,
			currentDbData = m_playerCarProfile,
			defaultDbData = m_defaultCarProfile
		};
	}

	private void InitCustomSetupInfo()
	{
		m_installedCustomProfile = m_prefs.carSettings.GetInstalledCustomProfile(m_prefs.globals.lastCarId);
		customSetupName.value = m_installedCustomProfile?.name;
		showCustomSetupName.value = !string.IsNullOrEmpty(customSetupName.value);
	}

	public static void ResetCachedSwap()
	{
		m_cachedSwap = null;
	}

	public static EngineSwapData GetCachedSwap()
	{
		return m_cachedSwap;
	}

	private void UpdateBottomPanel()
	{
		m_bottomListHintsData.Clear();
		m_bottomListHintsData.Add(new ActionHintData
		{
			hint = LocalizationKeys.nav_hint_goto_back.Localized(),
			type = ActionType.NavCancelBtnB,
			action = NavigationCancel
		});
		if (GetSelectedItem(m_navLayer) is DynoParamsItemViewModel dynoParamsItemViewModel && dynoParamsItemViewModel.CanAquireInputDialog())
		{
			m_bottomListHintsData.Add(new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_edit.Localized(),
				type = ActionType.NavSelectBtnA,
				action = NavigationSelect
			});
		}
		m_bottomListHintsData.Add(new ActionHintData
		{
			hint = LocalizationKeys.nav_hint_reset.Localized(),
			type = ActionType.NavAdditional0BtnX,
			action = NavigationReset
		});
		m_bottomListHintsData.Add(new ActionHintData
		{
			hint = LocalizationKeys.nav_hint_reset_all.Localized(),
			type = ActionType.NavBtnExpand,
			action = NavigationResetAll
		});
		if (States.mainCurrent is GarageGUIState)
		{
			m_bottomListHintsData.Add(new ActionHintData
			{
				hint = LocalizationKeys.save_custom_setup.Localized(),
				type = ActionType.NavBtnExpandRight,
				action = OnSaveSetup
			});
			m_bottomListHintsData.Add(new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_goto_testdrive.Localized(),
				type = ActionType.NavAdditional1BtnY,
				action = NavigationGoToTestDrive
			});
		}
		m_bottomListHintsData.Add(new ActionHintData
		{
			hint = "-",
			type = ActionType.NavAdditionalPrevBtnL2,
			action = NavDecValue
		});
		m_bottomListHintsData.Add(new ActionHintData
		{
			hint = "+",
			type = ActionType.NavAdditionalNextBtnR2,
			action = NavIncValue
		});
		ShowBottomPanel(m_bottomListHintsData);
	}

	protected override void OnDeactivate()
	{
		m_dynoSteeringModel.ReleaseCar();
		m_setupSharingManager?.UnregisterConsoleCommands();
		UnSubscribePS4TextReceive();
		if (Overlay.instance is SteamOverlay steamOverlay && GameManager.IsLinuxOsPlatform())
		{
			steamOverlay.onCloseOverlayKeyboard = (Action<string>)Delegate.Remove(steamOverlay.onCloseOverlayKeyboard, new Action<string>(CreateCustomSetup));
		}
		EnableCameraIdleRotate();
		HideTopPanel();
		base.OnDeactivate();
	}

	public void SetOpenedFrom(OpenedFrom openedFrom)
	{
		m_openedFrom = openedFrom;
	}

	private void ResetPreview()
	{
		profileInfoPreview = new ProfilesInfo.Profile(profileInfoCached.id)
		{
			controllers = new Dictionary<string, float>(profileInfoCached.controllers)
		};
		if (userCar != null)
		{
			bool skipSwapControllers = m_prefs.carSettings.GetInstalledCustomProfile(carId) != null;
			userCar.ApplyAdvancedProfile(m_model, profileInfoPreview, m_prefs.carSettings, reset: false, skipSwapControllers, m_cachedSwap, m_installedCustomProfile?.id);
		}
	}

	private void CreateCustomSetup(string text)
	{
		m_customSetupSaveHelper.SetNewSetupName(text, out var isNameValid);
		if (isNameValid)
		{
			m_customSetupSaveHelper.InitHelper(userCar, m_playerCarProfile, profileInfoPreview, profileId, carId, swapApplied: true);
			m_customSetupSaveHelper.SaveSetup(out var _);
		}
	}

	protected override bool OnEscape()
	{
		if (!(States.current is DynoMenuState))
		{
			return States.current is GarageGUIState;
		}
		return true;
	}

	public void DirtyChanges()
	{
		m_needWaitFrame = true;
		if (!m_updateVisualTimer.HasValue)
		{
			m_updateVisualTimer = Time.time + 0.03f;
		}
	}

	private void FixedUpdate()
	{
		if (m_updateVisualTimer.HasValue && (!m_needWaitFrame || m_updateVisualTimer < Time.time))
		{
			ApplyChanges();
			m_updateVisualTimer = null;
		}
		m_needWaitFrame = false;
	}

	private void Update()
	{
		if (m_dynoSteeringModel.IsCarValid())
		{
			m_dynoSteeringModel.UpdateTargetSteer();
			if (m_currentCategory == CarProfileControllerType.SuspensionStiffness)
			{
				UpdateSuspensionHints();
			}
		}
	}

	private void UpdateSuspensionHints()
	{
		if (!(userCar == null) && userCar.carModel.carWheels.Length >= 3)
		{
			UpdateToeCamberTopAndBottomLists();
		}
	}

	private void UpdateToeCamberTopAndBottomLists()
	{
		UpdateToeCamberViewList(m_currDynoParamsListsView[0], NavLayer.Left);
		UpdateToeCamberViewList(m_currDynoParamsListsView[1], NavLayer.Right);
	}

	private void UpdateToeCamberViews(DynoParamsItemViewModel item, DynoParamsItemView viewItem)
	{
		if (item != null && !(viewItem == null) && item.IsToeCamberItem())
		{
			m_dynoSteeringModel.GetFrontToe();
			(float, float) frontWheelsCambersTuple = m_dynoSteeringModel.GetFrontWheelsCambersTuple();
			frontToeHint.value = item.outputValue;
			rearToeHint.value = m_dynoSteeringModel.GetRearToe().ToString("N2");
			frontCamberHint.value = item.outputValue;
			rearCamberHint.value = m_dynoSteeringModel.GetRearCamber().ToString("N2");
			string value = string.Empty;
			string value2 = string.Empty;
			string value3 = string.Empty;
			switch (item.controller.name)
			{
			case "front_toe":
				value2 = frontToeHint.value;
				break;
			case "rear_toe":
				value2 = rearToeHint.value;
				break;
			case "front_camber":
				value = frontWheelsCambersTuple.Item1.ToString("N2");
				value3 = (0f - frontWheelsCambersTuple.Item2).ToString("N2");
				break;
			case "rear_camber":
				value2 = rearCamberHint.value;
				break;
			}
			viewItem.additionalOutputValueLeft.value = value;
			viewItem.outputValue.value = value2;
			viewItem.additionalOutputValueRight.value = value3;
		}
	}

	private void UpdateToeCamberViewList(DynoParamsList dynoParamsListView, NavLayer navLayer)
	{
		for (int i = 0; i < dynoParamsListView.items.Count; i++)
		{
			DynoParamsItemViewModel item = dynoParamsListView.GetItem(i) as DynoParamsItemViewModel;
			DynoParamsItemView itemView = GetItemView(navLayer, i);
			UpdateToeCamberViews(item, itemView);
		}
	}

	private void SetActiveCameraRotation(bool active)
	{
		CameraRotation instance = CameraRotation.instance;
		if (!(instance == null))
		{
			instance.canIdle = active;
			instance.useZoom = !active;
			if (active)
			{
				instance.SetDistanceFromCenter(m_cameraLastZoom);
				instance.SetIdle();
			}
			else
			{
				instance.Stop();
			}
		}
	}

	private void EnableCameraIdleRotate(bool cameraIdleRotate = true)
	{
		CameraRotation instance = CameraRotation.instance;
		if (!(instance == null))
		{
			instance.disableReturnToIdle = !cameraIdleRotate;
		}
	}

	private void InitParamsList()
	{
		m_params = new Dictionary<CarProfileControllerType, List<DynoParamsList>>
		{
			{
				CarProfileControllerType.Suspension,
				new List<DynoParamsList> { m_suspensionFrontParams, m_suspensionRearParams }
			},
			{
				CarProfileControllerType.SuspensionStiffness,
				new List<DynoParamsList> { m_stiffnessFrontParams, m_stiffnessRearParams }
			},
			{
				CarProfileControllerType.Wheels,
				new List<DynoParamsList> { m_wheelsFrontParams, m_wheelsRearParams }
			},
			{
				CarProfileControllerType.Brakes,
				new List<DynoParamsList> { m_brakesParams }
			},
			{
				CarProfileControllerType.Engine,
				new List<DynoParamsList> { m_engineParams }
			},
			{
				CarProfileControllerType.Transmission,
				new List<DynoParamsList> { m_transmissionParams }
			},
			{
				CarProfileControllerType.Weight,
				new List<DynoParamsList> { m_weightParams }
			}
		};
	}

	private void InitCategoriesList()
	{
		m_categoriesList.Clear();
		AddCategoriesList(CarProfileControllerType.Suspension);
		AddCategoriesList(CarProfileControllerType.Wheels);
		AddCategoriesList(CarProfileControllerType.Brakes);
		AddCategoriesList(CarProfileControllerType.Engine);
		AddCategoriesList(CarProfileControllerType.Transmission);
		AddCategoriesList(CarProfileControllerType.Weight);
		ShowTopPanel(ETopPanelState.Actions, null, m_categoriesList.ToArray());
		if (m_categoriesList.Count > 0)
		{
			m_currentCategory = CarProfileControllerType.None;
			m_switcherSubCategoryMode.value = 0;
			SelectCategory(CarProfileControllerType.Suspension);
		}
		void AddCategoriesList(CarProfileControllerType category)
		{
			if (IsCategoryEnabledByDynoInfo(category, m_playerCarProfile.dynoInfo) && !m_forceCategoriesToActivate.Contains(category))
			{
				m_categoriesList.Add(new HeaderActionData
				{
					text = LocalizationService.Localize(m_categoriesLocalizationKeys[category]),
					action = delegate
					{
						SelectCategory(category);
					}
				});
			}
		}
	}

	private void RebuildParametersForCategory(CarProfileControllerType category)
	{
		if (m_currentCategory != category || m_contextWasHidden)
		{
			m_currentCategory = category;
			m_rebuildingElements = true;
			m_currDynoPanelVisibility.value = (int)category;
			if (category != CarProfileControllerType.SuspensionStiffness)
			{
				categoryFilterTitle.value = LocalizationKeys.car_profile_category_caption_.FormatLocalized(m_currentCategory.ToString().ToLower(CultureInfo.InvariantCulture));
			}
			FillParamsListsByCategory(category);
			switch (category)
			{
			case CarProfileControllerType.Engine:
				userCar.ApplyAdvancedProfile(m_model, profileInfoPreview, m_prefs.carSettings, reset: false, skipSwapControllers: true, m_cachedSwap, m_installedCustomProfile?.id);
				RebuildEngineDescriptionParams();
				break;
			case CarProfileControllerType.Transmission:
				CalculateHPTorque();
				CalculateSpeedAccel();
				break;
			}
			showInfo.value = false;
			m_contextWasHidden = false;
			m_rebuildingElements = false;
		}
	}

	private void FillParamsListsByCategory(CarProfileControllerType category)
	{
		m_currDynoParamsListsView.ForEach(delegate(DynoParamsList list)
		{
			list.Clear();
		});
		m_currDynoParamsListsView = m_params[category];
		m_itemsModel.Clear();
		(DynoParamsList, DynoParamsList) currListsViews = GetCurrListsViews();
		DynoParamsList leftList = currListsViews.Item1;
		DynoParamsList rightList = currListsViews.Item2;
		DiffType currentDiffType = GetCurrentDiffType();
		m_diffLockCoefParam = null;
		m_diffPreloadParam = null;
		m_diffLockPowerParam = null;
		m_diffLockCoastParam = null;
		for (int num = 0; num < m_playerCarProfile.dynoInfo.controllers.Count; num++)
		{
			CarProfileController carProfileController = m_playerCarProfile.dynoInfo.controllers[num];
			CarProfileControllerType carProfileControllerType = carProfileController.TryGetOverridedCategory();
			if (carProfileController != null && carProfileControllerType == category && (category != CarProfileControllerType.Engine || SetupEngineController(carProfileController)) && (category != CarProfileControllerType.Engine || IsWastegateAllowed(carProfileController)) && (category != CarProfileControllerType.Wheels || IsAdhesionAllowed(carProfileController)) && (category != CarProfileControllerType.Brakes || IsTractionControlAllowed(carProfileController)) && (category != CarProfileControllerType.Transmission || IsWheelDriveAllowed(carProfileController)) && (category != CarProfileControllerType.Engine || IsPowerDensityAllowed(carProfileController)) && (category != CarProfileControllerType.Weight || IsPowerDensityAllowed(carProfileController) || !IsMassController(carProfileController)))
			{
				bool isShortItem = carProfileControllerType == CarProfileControllerType.Suspension || carProfileControllerType == CarProfileControllerType.Wheels || carProfileControllerType == CarProfileControllerType.SuspensionStiffness;
				BaseDynoParamsItemViewModel dynoParamItem = GetDynoParamItem(carProfileController, num, carProfileController.sort, isShortItem);
				FilterItemToProperList(dynoParamItem);
				m_itemsModel.Add(dynoParamItem);
				switch (carProfileController.name)
				{
				case "diff_type":
				{
					DynoParamsItemViewModel obj = (DynoParamsItemViewModel)dynoParamItem;
					obj.onValueChanged = (Action<DynoParamsItemViewModel>)Delegate.Remove(obj.onValueChanged, new Action<DynoParamsItemViewModel>(OnDiffTypeChanged));
					DynoParamsItemViewModel obj2 = (DynoParamsItemViewModel)dynoParamItem;
					obj2.onValueChanged = (Action<DynoParamsItemViewModel>)Delegate.Combine(obj2.onValueChanged, new Action<DynoParamsItemViewModel>(OnDiffTypeChanged));
					break;
				}
				case "diff_lock_coef":
					m_diffLockCoefParam = dynoParamItem;
					break;
				case "diff_preload":
					m_diffPreloadParam = dynoParamItem;
					break;
				case "diff_lock_power":
					m_diffLockPowerParam = dynoParamItem;
					break;
				case "diff_lock_coast":
					m_diffLockCoastParam = dynoParamItem;
					break;
				}
			}
		}
		AddOrRemoveDiffControllers(currentDiffType, category);
		SortAndRecalList(leftList);
		SortAndRecalList(rightList);
		SetSelectedId(NavLayer.Left, 0);
		if (userCar != null && m_model != null && !userCar.swap.isStockEngine)
		{
			m_customSetupSaveHelper.ApplySavedSwap(profileInfoPreview, m_haveCustomSetupInstalled, m_openedFromCustomCreation);
			userCar.ApplyAdvancedProfile(m_model, profileInfoPreview, m_prefs.carSettings, reset: false, m_customSetupSaveHelper.HaveInstalledCustomProfile(), m_cachedSwap, m_installedCustomProfile?.id);
		}
		void FilterItemToProperList(BaseDynoParamsItemViewModel baseDynoParamItem)
		{
			if (baseDynoParamItem is DynoParamsItemViewModel dynoParamsItemViewModel)
			{
				if (dynoParamsItemViewModel.IsFrontSuspension())
				{
					leftList.AddItem(dynoParamsItemViewModel);
					return;
				}
				if (dynoParamsItemViewModel.IsRearSuspension())
				{
					rightList.AddItem(dynoParamsItemViewModel);
					return;
				}
			}
			leftList.AddItem(baseDynoParamItem);
		}
	}

	private void SortAndRecalList(DynoParamsList paramsList)
	{
		if (!(paramsList == null))
		{
			paramsList.scrollRect.horizontalNormalizedPosition = 0f;
			paramsList.ClearSelection();
			paramsList.Sort((BaseDynoParamsItemViewModel item1, BaseDynoParamsItemViewModel item2) => item1.CompareTo(item2));
			paramsList.RecalcList();
		}
	}

	private BaseDynoParamsItemViewModel GetDynoParamItem(CarProfileController ctrl, int id, int sort, bool isShortItem)
	{
		return (CarProfileParameterType)ctrl.changers[0].parameter.id switch
		{
			CarProfileParameterType.DrivetrainType => new DrivetrainSwitchDynoParamViewModel(this, ctrl), 
			CarProfileParameterType.Mass => new MassDynoParamViewModel(this, ctrl, userCar), 
			_ => new DynoParamsItemViewModel(this, ctrl, id, sort, isShortItem), 
		};
	}

	private bool IsWastegateAllowed(CarProfileController controller)
	{
		if (IsWastegateControllerValid(controller))
		{
			return true;
		}
		if (!userCar.swap.isStockEngine)
		{
			return Mathf.Approximately(userCar.swap.meta.turboCharged.value, 1f);
		}
		return true;
	}

	private bool IsAdhesionAllowed(CarProfileController controller)
	{
		if (IsControllerValidInCurrState(controller))
		{
			RoomData.AdhLimitFlags value = (RoomData.AdhLimitFlags)(NetworkController.InstanceGame.lastRoomOptions?.RoomAdhLimit).Value;
			if (value != RoomData.AdhLimitFlags.NONE)
			{
				return value == RoomData.AdhLimitFlags.ALL;
			}
			return true;
		}
		return true;
	}

	private bool IsTractionControlAllowed(CarProfileController controller)
	{
		if (IsControllerValidInCurrState(controller))
		{
			return (NetworkController.InstanceGame.lastRoomOptions?.RoomTractionControlMode).Value == 0;
		}
		return true;
	}

	private bool IsWheelDriveAllowed(CarProfileController controller)
	{
		if (IsControllerValidInCurrState(controller))
		{
			RoomData.WheelDriveType value = (RoomData.WheelDriveType)(NetworkController.InstanceGame.lastRoomOptions?.RoomWheelDriveType).Value;
			if (value != RoomData.WheelDriveType.NONE)
			{
				return value == RoomData.WheelDriveType.ALL;
			}
			return true;
		}
		return true;
	}

	private bool IsPowerDensityAllowed(CarProfileController controller)
	{
		if (IsControllerValidInCurrState(controller))
		{
			RoomData.PowerDensityLimitFlag value = (RoomData.PowerDensityLimitFlag)(NetworkController.InstanceGame.lastRoomOptions?.RoomPowerDensityLimit).Value;
			if (value != RoomData.PowerDensityLimitFlag.NONE)
			{
				return value == RoomData.PowerDensityLimitFlag.ALL;
			}
			return true;
		}
		return true;
	}

	private bool IsControllerValidInCurrState(CarProfileController controller)
	{
		if (controller != null && (!m_controllersToCheckRelyOnStates.Contains(controller.name) || !(States.mainCurrent is SyncNetFreerideRaceModeState)))
		{
			return NetworkController.InstanceGame.lastRoomOptions == null;
		}
		return true;
	}

	private bool IsWastegateControllerValid(CarProfileController controller)
	{
		if (controller != null)
		{
			return controller.changers[0].parameter.type != CarProfileParameterType.WastegateSound;
		}
		return true;
	}

	private bool IsMassController(CarProfileController controller)
	{
		if (controller != null)
		{
			return controller.changers[0].parameter.type != CarProfileParameterType.Mass;
		}
		return false;
	}

	private bool SetupEngineController(CarProfileController controller)
	{
		if (controller == null)
		{
			return true;
		}
		if (userCar.swap.isStockEngine)
		{
			foreach (CarProfileController controller2 in m_defaultCarProfile.dynoInfo.controllers)
			{
				if (controller2.name == controller.name)
				{
					CarProfileChanger carProfileChanger = controller.changers[0];
					CarProfileChanger carProfileChanger2 = controller2.changers[0];
					carProfileChanger.values.Clear();
					carProfileChanger.values.AddRange(carProfileChanger2.values);
					carProfileChanger.multiplier = carProfileChanger2.multiplier;
					carProfileChanger.shift = carProfileChanger2.shift;
					carProfileChanger.defaultValue = carProfileChanger2.defaultValue;
					break;
				}
			}
			return true;
		}
		EngineInfo meta = userCar.swap.meta;
		bool flag = !Mathf.Approximately(meta.turboCharged.value, 0f);
		string text = controller.name;
		if (text == meta.turboCharged.name)
		{
			if (!flag)
			{
				return false;
			}
			SetupController(controller, meta.turboCharged);
		}
		else if (text == meta.turboPressure.name)
		{
			if (!flag)
			{
				return false;
			}
			SetupController(controller, meta.turboPressure);
		}
		else if (text == meta.revLimiter.name)
		{
			SetupController(controller, meta.revLimiter);
		}
		else if (text == meta.maxTorque.name)
		{
			SetupController(controller, meta.maxTorque, shiftMode: true);
		}
		else if (text == meta.maxTorqueRpm.name)
		{
			SetupController(controller, meta.maxTorqueRpm);
		}
		return true;
	}

	private static void SetupController(CarProfileController controller, EngineParamInfo param, bool shiftMode = false)
	{
		if (controller != null && controller.changers != null && controller.changers.Count != 0)
		{
			CarProfileChanger carProfileChanger = controller.changers[0];
			carProfileChanger.values = new List<float> { param.min, param.max };
			carProfileChanger.defaultValue = param.value;
			if (shiftMode && param is EngineParamShiftInfo engineParamShiftInfo)
			{
				carProfileChanger.multiplier = engineParamShiftInfo.multiplier;
				carProfileChanger.shift = engineParamShiftInfo.shift;
			}
		}
	}

	private DiffType GetCurrentDiffType()
	{
		foreach (CarProfileController controller in m_playerCarProfile.dynoInfo.controllers)
		{
			if (controller.name != "diff_type")
			{
				continue;
			}
			if (!profileInfoPreview.controllers.TryGetValue(controller.name, out var value))
			{
				if (controller.changers.Count != 1)
				{
					continue;
				}
				CarProfileChanger carProfileChanger = controller.changers[0];
				value = carProfileChanger.EvaluatePhase(carProfileChanger.defaultValue);
			}
			foreach (CarProfileChanger changer in controller.changers)
			{
				if (changer != null)
				{
					CarProfileParameterMeta byId = model.GetById<CarProfileParameterMeta>(changer.parameterId);
					if (byId != null && byId.type == CarProfileParameterType.DiffType)
					{
						return (DiffType)Mathf.RoundToInt(changer.EvaluateOriginalValue(value));
					}
				}
			}
		}
		return DiffType.Viscous;
	}

	private void OnDiffTypeChanged(DynoParamsItemViewModel item)
	{
		if (item?.controller != null && item.controller.type == CarProfileControllerType.Transmission)
		{
			AddOrRemoveDiffControllers(GetCurrentDiffType(), CarProfileControllerType.Transmission);
		}
	}

	private void AddOrRemoveDiffControllers(DiffType diffType, CarProfileControllerType controllerType)
	{
		bool flag = diffType == DiffType.Welded || diffType == DiffType.Open;
		bool flag2 = diffType == DiffType.ClutchType;
		bool flag3 = diffType == DiffType.Viscous;
		List<BaseDynoParamsItemViewModel> list = new List<BaseDynoParamsItemViewModel>(8);
		bool flag4 = false;
		bool flag5 = false;
		bool flag6 = false;
		bool flag7 = false;
		BaseDynoParamsItemViewModel baseDynoParamsItemViewModel = null;
		DynoParamsList dynoParamsList = m_currDynoParamsListsView[0];
		foreach (BaseDynoParamsItemViewModel item in dynoParamsList.items)
		{
			if (item?.controller == null || item.controller.type != CarProfileControllerType.Transmission)
			{
				continue;
			}
			if (item.controller.name == "diff_type")
			{
				baseDynoParamsItemViewModel = item;
				continue;
			}
			bool flag8 = false;
			switch (item.controller.name)
			{
			case "diff_lock_coef":
				flag4 = true;
				flag8 = !flag3 || flag;
				break;
			case "diff_preload":
				flag5 = true;
				flag8 = !flag2 || flag;
				break;
			case "diff_lock_power":
				flag6 = true;
				flag8 = !flag2 || flag;
				break;
			case "diff_lock_coast":
				flag7 = true;
				flag8 = !flag2 || flag;
				break;
			}
			if (flag8)
			{
				list.Add(item);
			}
		}
		if (list.Count > 0)
		{
			foreach (BaseDynoParamsItemViewModel item2 in list)
			{
				int index = dynoParamsList.IndexOf(item2);
				dynoParamsList.RemoveItem(index);
			}
			dynoParamsList.ApplyChangeList();
		}
		if (flag || controllerType != CarProfileControllerType.Transmission)
		{
			return;
		}
		if (baseDynoParamsItemViewModel == null)
		{
			GameLog.Print(AlertLevel.Error, "UI", "Diff. type controller is not found");
			return;
		}
		int index2 = dynoParamsList.IndexOf(baseDynoParamsItemViewModel) + 1;
		if (flag3 && !flag4)
		{
			dynoParamsList.InsertItem(index2, m_diffLockCoefParam);
			dynoParamsList.ApplyChangeList();
		}
		else if (flag2)
		{
			if (!flag5)
			{
				dynoParamsList.InsertItem(index2++, m_diffPreloadParam);
				dynoParamsList.ApplyChangeList();
			}
			if (!flag6)
			{
				dynoParamsList.InsertItem(index2++, m_diffLockPowerParam);
				dynoParamsList.ApplyChangeList();
			}
			if (!flag7)
			{
				dynoParamsList.InsertItem(index2, m_diffLockCoastParam);
				dynoParamsList.ApplyChangeList();
			}
		}
	}

	private int GetSelectedId(NavLayer layerNav)
	{
		var (dynoParamsList, dynoParamsList2) = GetCurrListsViews();
		return layerNav switch
		{
			NavLayer.Left => dynoParamsList.selectedIndex, 
			NavLayer.Right => dynoParamsList2.selectedIndex, 
			_ => -1, 
		};
	}

	private BaseDynoParamsItemViewModel GetSelectedItem(NavLayer layerNav)
	{
		var (dynoParamsList, dynoParamsList2) = GetCurrListsViews();
		return layerNav switch
		{
			NavLayer.Left => dynoParamsList.GetSelectedItem(), 
			NavLayer.Right => dynoParamsList2.GetSelectedItem(), 
			_ => null, 
		};
	}

	private DynoParamsItemView GetSelectedItemView(NavLayer layerNav)
	{
		var (dynoParamsList, dynoParamsList2) = GetCurrListsViews();
		return layerNav switch
		{
			NavLayer.Left => dynoParamsList.GetSelectedController(), 
			NavLayer.Right => dynoParamsList2.GetSelectedController(), 
			_ => null, 
		};
	}

	private DynoParamsItemView GetItemView(NavLayer layerNav, int index)
	{
		var (dynoParamsList, dynoParamsList2) = GetCurrListsViews();
		return layerNav switch
		{
			NavLayer.Left => dynoParamsList.GetController(index), 
			NavLayer.Right => dynoParamsList2.GetController(index), 
			_ => null, 
		};
	}

	private int GetItemsCount(NavLayer layerNav)
	{
		var (dynoParamsList, dynoParamsList2) = GetCurrListsViews();
		return layerNav switch
		{
			NavLayer.Left => dynoParamsList.GetCount(), 
			NavLayer.Right => dynoParamsList2.GetCount(), 
			_ => -1, 
		};
	}

	private (DynoParamsList, DynoParamsList) GetCurrListsViews()
	{
		return m_currDynoParamsListsView.Count switch
		{
			1 => (m_currDynoParamsListsView[0], null), 
			2 => (m_currDynoParamsListsView[0], m_currDynoParamsListsView[1]), 
			_ => (null, null), 
		};
	}

	private void SetSelectedId(NavLayer navLayer, int newIndex, bool isNavigation = false)
	{
		var (dynoParamsList, dynoParamsList2) = GetCurrListsViews();
		switch (navLayer)
		{
		case NavLayer.Left:
			ProcessSelect(dynoParamsList, newIndex);
			DeselectList(dynoParamsList2);
			break;
		case NavLayer.Right:
			ProcessSelect(dynoParamsList2, newIndex);
			DeselectList(dynoParamsList);
			break;
		}
		Reselect(navLayer);
		UpdateBottomPanel();
		needSubmitIcon.value = GetSelectedItem(m_navLayer) is DynoParamsItemViewModel dynoParamsItemViewModel && dynoParamsItemViewModel.CanAquireInputDialog();
		static void DeselectList(DynoParamsList list)
		{
			if (!(list == null))
			{
				list.selectedIndex = -1;
			}
		}
		void ProcessSelect(DynoParamsList activeList, int selectedIndex)
		{
			int selectedIndex2 = activeList.selectedIndex;
			activeList.selectedIndex = selectedIndex;
			if (isNavigation && selectedIndex != selectedIndex2)
			{
				navigationEvent.SafeCall(navLayer, selectedIndex);
			}
		}
	}

	private void Reselect(NavLayer layerNav)
	{
		(DynoParamsList, DynoParamsList) currListsViews = GetCurrListsViews();
		DynoParamsList item = currListsViews.Item1;
		DynoParamsList item2 = currListsViews.Item2;
		m_navLayer = layerNav;
		BaseDynoParamsItemViewModel baseDynoParamsItemViewModel = null;
		switch (layerNav)
		{
		case NavLayer.Left:
			if (item.GetCount() == 0 && item2.GetCount() > 0)
			{
				Reselect(NavLayer.Right);
				return;
			}
			if (item.GetCount() == 0)
			{
				titleParameter.value = string.Empty;
				descriptionParameter.value = string.Empty;
				return;
			}
			baseDynoParamsItemViewModel = item.GetSelectedItem();
			if (baseDynoParamsItemViewModel == null)
			{
				item.selectedIndex = 0;
				baseDynoParamsItemViewModel = item.GetSelectedItem();
			}
			break;
		case NavLayer.Right:
			if (item2.GetCount() == 0 && item.GetCount() > 0)
			{
				Reselect(NavLayer.Left);
				return;
			}
			if (item2.GetCount() == 0)
			{
				titleParameter.value = string.Empty;
				descriptionParameter.value = string.Empty;
				return;
			}
			baseDynoParamsItemViewModel = item2.GetSelectedItem();
			if (baseDynoParamsItemViewModel == null)
			{
				item2.selectedIndex = 0;
				baseDynoParamsItemViewModel = item2.GetSelectedItem();
			}
			break;
		}
		titleParameter.value = baseDynoParamsItemViewModel.titleDescription;
		descriptionParameter.value = baseDynoParamsItemViewModel.description;
		Vector2 preferredValues = m_description.GetPreferredValues(descriptionParameter.value, 362.5f, 0f);
		preferredValues.x = 362.5f;
		m_description.rectTransform.sizeDelta = preferredValues;
	}

	public void RebuildEngineDescriptionParams()
	{
		CalculateHPTorque();
		CalculateSpeedAccel();
		FillEngineGraph();
		void FillEngineGraph()
		{
			if (!BaseCar.IsNull(userCar))
			{
				m_engineGraph.SetCar(userCar.carX, out var maxGraphAxisY);
				engineGraphMark1.value = UIUtils.DigitsWithSeparate((int)(maxGraphAxisY * 0f / 10f));
				engineGraphMark2.value = UIUtils.DigitsWithSeparate((int)(maxGraphAxisY * 5f / 10f));
				engineGraphMark3.value = UIUtils.DigitsWithSeparate((int)maxGraphAxisY);
			}
		}
	}

	private void CalculateHPTorque()
	{
		if (!BaseCar.IsNull(userCar))
		{
			userCar.carX.CalcMaxPowerMaxTorque(out var max_hp, out var _, out var max_tq, out var _);
			engineHp.value = UIUtils.DigitsWithSeparate(Mathf.RoundToInt(max_hp)) + " " + LocalizationKeys.car_profile_prm_units_hp.Localized();
			engineTorque.value = UIUtils.DigitsWithSeparate(Mathf.RoundToInt(Car.ConvertNMtoLbFt(max_tq))) + " " + LocalizationKeys.car_profile_prm_units_lbft.Localized() + " (" + UIUtils.DigitsWithSeparate(Mathf.RoundToInt(max_tq)) + " " + LocalizationKeys.car_profile_prm_units_.FormatLocalized("n_m") + ")";
			CalculatePowerDensity(max_hp);
		}
	}

	private void CalculatePowerDensity(float maxHP)
	{
		CarDesc desc = null;
		userCar.carX.GetCarDesc(ref desc);
		float value = maxHP / desc.weight.mass;
		powerDensity.value = UIUtils.DigitsWithSeparate(value) + " " + LocalizationKeys.garage_setup_hp.Localized() + "/" + LocalizationKeys.garage_setup_kg.Localized();
		powerDensityTitle.value = LocalizationKeys.garage_setup_power_density.Localized() ?? "";
	}

	private void CalculateSpeedAccel()
	{
		if (!BaseCar.IsNull(userCar))
		{
			string text;
			float num;
			float num2;
			if (m_prefs.globals.speedKmh)
			{
				text = LocalizationKeys.settings_game_speed_unit_kmh.Localized().ToLower();
				num = 3.6f;
				num2 = 100f;
			}
			else
			{
				text = LocalizationKeys.settings_game_speed_unit_mph.Localized().ToLower();
				num = 2.2369363f;
				num2 = 60f;
			}
			Car carX = userCar.carX;
			string text2 = $"(0-{num2} {text})";
			engineSpeed.value = UIUtils.DigitsWithSeparate(Mathf.RoundToInt(carX.CalcTopSpeed() * num)) + " " + text;
			engineAccelerationTo.value = LocalizationKeys.car_profile_engine_accel.Localized() + " " + text2;
			engineAccel.value = $"{carX.CalcAccelTime(0f, num2 / num):F2} {LocalizationKeys.car_profile_prm_units_sec.Localized()}";
		}
	}

	private void SelectCategory(CarProfileControllerType category)
	{
		m_wheelAnglePanel.SetSelected(isSelected: false);
		m_anglePanelVisible.value = category == CarProfileControllerType.Suspension || category == CarProfileControllerType.Wheels || category == CarProfileControllerType.SuspensionStiffness;
		if (IsCategoryEnabledByDynoInfo(category, m_playerCarProfile.dynoInfo) || m_forceCategoriesToActivate.Contains(category))
		{
			HandleSubcategoryActivity(ref category);
			RebuildParametersForCategory(category);
		}
		else
		{
			UIMessageBox.ShowMode0(LocalizationKeys.car_profile_window_category_disabled_text.Localized(), LocalizationKeys.car_profile_window_category_disabled_caption.Localized(), allowClose: true, LocalizationKeys.Close.Localized());
		}
		void HandleSubcategoryActivity(ref CarProfileControllerType reference)
		{
			bool flag = reference == CarProfileControllerType.Suspension || reference == CarProfileControllerType.SuspensionStiffness;
			m_switcherSubCategoryVisibility.value = flag;
			m_topPanelContext.SetActiveActionHintsVisibility(!flag);
			if (flag)
			{
				switch (m_currentCategory)
				{
				case CarProfileControllerType.None:
					reference = CarProfileControllerType.Suspension;
					break;
				default:
					reference = CarProfileControllerType.SuspensionStiffness;
					break;
				case CarProfileControllerType.Suspension:
				case CarProfileControllerType.SuspensionStiffness:
					break;
				}
				IntProperty switcherSubCategoryMode = m_switcherSubCategoryMode;
				switcherSubCategoryMode.value = reference switch
				{
					CarProfileControllerType.Suspension => 0, 
					CarProfileControllerType.SuspensionStiffness => 1, 
					_ => throw new NotImplementedException(), 
				};
			}
		}
	}

	public void OnClickSubCategory(int index)
	{
		if (m_currentCategory == CarProfileControllerType.Suspension || m_currentCategory == CarProfileControllerType.SuspensionStiffness)
		{
			int value = m_switcherSubCategoryMode.value;
			if (index != value)
			{
				OnSwitchSubCategory(index > value);
			}
		}
	}

	public void OnSwitchSubCategory(bool next)
	{
		if (m_currentCategory != CarProfileControllerType.Suspension && m_currentCategory != CarProfileControllerType.SuspensionStiffness)
		{
			return;
		}
		int value = m_switcherSubCategoryMode.value;
		value = (next ? (value + 1) : (value - 1));
		int length = Enum.GetValues(typeof(SwitcherSuspensionSubCategoryMode)).Length;
		if (value < 0)
		{
			return;
		}
		if (value > length - 1)
		{
			m_topPanelContext.SetActiveActionHintsVisibility(needActivate: true);
			m_topPanelContext.switcherActions.SelectionAction(1);
			return;
		}
		m_switcherSubCategoryMode.value = value;
		switch ((SwitcherSuspensionSubCategoryMode)value)
		{
		case SwitcherSuspensionSubCategoryMode.Angles:
			SelectCategory(CarProfileControllerType.Suspension);
			break;
		case SwitcherSuspensionSubCategoryMode.Stiffness:
			SelectCategory(CarProfileControllerType.SuspensionStiffness);
			break;
		}
	}

	public static bool IsCanDynoAdjust(string profileName, PlayerCarProfile.DynoInfo dynoInfo)
	{
		if (profileName.Equals("Stock"))
		{
			return false;
		}
		Array values = Enum.GetValues(typeof(CarProfileControllerType));
		for (int i = 1; i < values.Length; i++)
		{
			if (IsCategoryEnabledByDynoInfo((CarProfileControllerType)i, dynoInfo))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsCategoryEnabledByDynoInfo(CarProfileControllerType controllerType, PlayerCarProfile.DynoInfo dynoInfo)
	{
		return dynoInfo.controllers.Exists((CarProfileController x) => x.type == controllerType && x.enabled);
	}

	private HashSet<string> GetChangedVarsId()
	{
		HashSet<string> hashSet = new HashSet<string>();
		Dictionary<string, float> controllers = profileInfoPreview.controllers;
		Dictionary<string, float> controllers2 = profileInfoCached.controllers;
		foreach (KeyValuePair<string, float> item in controllers)
		{
			if (controllers2.TryGetValue(item.Key, out var value))
			{
				if (item.Value != value)
				{
					hashSet.Add(item.Key);
				}
			}
			else
			{
				hashSet.Add(item.Key);
			}
		}
		foreach (KeyValuePair<string, float> item2 in controllers2)
		{
			if (!controllers.ContainsKey(item2.Key))
			{
				hashSet.Add(item2.Key);
			}
		}
		return hashSet;
	}

	private void SaveControllers()
	{
		foreach (CarProfileController controller in m_playerCarProfile.dynoInfo.controllers)
		{
			if (controller.changers.Count <= 0)
			{
				continue;
			}
			string key = controller.name;
			if (!profileInfoCached.controllers.ContainsKey(key) && profileInfoPreview.controllers.TryGetValue(key, out var value))
			{
				CarProfileChanger carProfileChanger = controller.changers[0];
				if (Mathf.Abs(carProfileChanger.EvaluatePhase(carProfileChanger.defaultValue) - value) < 1E-05f)
				{
					profileInfoPreview.controllers.Remove(key);
				}
			}
		}
	}

	public void HandleExit()
	{
		if (States.mainCurrent is DynoRaceModeState dynoRaceModeState)
		{
			Import(dynoRaceModeState.GetDynoData());
		}
		SaveControllers();
		HashSet<string> changedVarsId = GetChangedVarsId();
		if (changedVarsId.Count == 0)
		{
			ExitDyno();
			return;
		}
		UIMessageBox.ShowComplexDialog(LocalizationKeys.car_profile_window_save_changes_text.Localized(), null, LocalizationKeys.Cancel.Localized(), delegate
		{
			CarProfileCacheManager.ClearCarProfile(profileId);
			InitCustomSetupInfo();
			if (BaseCar.IsNull(userCar) && States.mainCurrent is BaseRaceModeState baseRaceModeState)
			{
				userCar = baseRaceModeState.userCar;
			}
			PrefsCarSlotsModel carSettings = m_prefs.carSettings;
			if (showCustomSetupName.value)
			{
				int engineId = userCar.swap.engineId;
				carSettings.SetCustomProfileInfo(out var _, out var _, customSetupName.value, profileId, carId, engineId, profileInfoPreview.Copy());
			}
			else
			{
				carSettings.SetProfileInfo(carId, profileId, profileInfoPreview.Copy());
			}
			if (States.mainCurrent is SyncNetFreerideRaceModeState)
			{
				NetworkGame instanceGame = NetworkController.InstanceGame;
				if (instanceGame.CurrentRoom != null)
				{
					instanceGame.systems.Get<NetGameDynoSystem>().SEND_CarSettingsChanged();
					instanceGame.FillLocalPlayerData();
				}
			}
			SendAnalytics(changedVarsId);
			ExitDyno();
			SoundController.Play2DUISound(SoundSample.ProSetup);
			BasePrefsModel.SaveAllQueries(inOneFrame: false);
		}, LocalizationKeys.Yes.Localized(), delegate
		{
			m_prefs.carSettings.Save();
			ResetPreview();
			ExitDyno();
		}, LocalizationKeys.No.Localized());
	}

	private void ExitDyno()
	{
		if (m_customSetupSaveHelper == null)
		{
			m_customSetupSaveHelper = new CustomSetupSaveHelper();
		}
		m_customSetupSaveHelper.InitHelper(userCar, m_playerCarProfile, profileInfoPreview, profileId, carId, m_swapApplied);
		m_customSetupSaveHelper.SaveSwapIfNeeded();
		AnalyticsService.DynoTime(Time.time - m_entryTime);
		if (States.current is DynoMenuState)
		{
			States.Pop();
			return;
		}
		SetActiveCameraRotation(active: true);
		if (States.mainCurrent is GarageGUIState)
		{
			SwitchToLastContext();
		}
		else
		{
			States.Change<GarageGUIState>();
		}
		TimeController.AddCallback(1f, delegate
		{
			int lastCarId = m_prefs.globals.lastCarId;
			int activeSlotId = m_prefs.carSettings.GetActiveSlotId(lastCarId);
			IconStorageSystem.DeleteLiveryPreview(lastCarId, activeSlotId);
			IconGeneratorContext.AddToCarHashQueue(new IconCarData(lastCarId, string.Empty));
		});
	}

	private void SwitchToLastContext()
	{
		Hide();
		switch (m_openedFrom)
		{
		case OpenedFrom.Garage:
		{
			UISetupsContext.Mode mode = UISetupsContext.Mode.Setups;
			if (m_installedCustomProfile != null)
			{
				mode = UISetupsContext.Mode.CustomSetups;
			}
			else if (!userCar.swap.isStockEngine)
			{
				mode = UISetupsContext.Mode.Swaps;
			}
			base.owner.Push<UISetupsContext>().OpenTabFromDyno(mode);
			break;
		}
		case OpenedFrom.Swaps:
			base.owner.Push<UISetupsContext>().OpenTabFromDyno(UISetupsContext.Mode.Swaps);
			break;
		case OpenedFrom.Setups:
			base.owner.Push<UISetupsContext>().OpenTabFromDyno(UISetupsContext.Mode.Setups);
			break;
		case OpenedFrom.CustomSetups:
			base.owner.Push<UISetupsContext>().OpenTabFromDyno(UISetupsContext.Mode.CustomSetups);
			break;
		case OpenedFrom.SetupCreation:
			base.owner.Push<UISetupsContext>().OpenTabFromDyno(UISetupsContext.Mode.CustomSetups);
			break;
		}
		GamePrefs prefs = DependencyInjector.Resolve<GamePrefs>();
		if (prefs != null)
		{
			TimeController.AddCallback(1f, delegate
			{
				IconGeneratorContext.AddToCarHashQueue(new IconCarData(prefs.globals.lastCarId, string.Empty));
			});
		}
	}

	private void SendAnalytics(ICollection<string> changedVars)
	{
		AnalyticsService.DynoChangedParams(changedVars);
		string paramName = "front_lock";
		if (changedVars.Contains(paramName))
		{
			int carId = m_prefs.globals.lastCarId;
			int profileId = m_prefs.carSettings.GetProfileIdForCar(carId);
			CarProfileChanger carProfileChanger = m_model.QueryRecordsByColumnValue("carId", carId, (PlayerCarProfile x) => x.carId == carId).Find((PlayerCarProfile x) => x.id == profileId).dynoInfo.controllers.Find((CarProfileController x) => x.name == paramName).changers[0];
			AnalyticsService.DynoWheelAngle(value: (!profileInfoPreview.controllers.TryGetValue(paramName, out var value)) ? carProfileChanger.defaultValue : (carProfileChanger.EvaluateValue(value) * carProfileChanger.multiplier), carName: m_model.GetNameById<PlayerCar>(carId));
		}
	}

	public void ApplyChanges()
	{
		if (m_rebuildingElements)
		{
			return;
		}
		if (userCar.IsPhysicsBroken())
		{
			userCar.ResetBrokenPhysicsInGarage();
		}
		userCar.ApplyAdvancedProfile(m_model, profileInfoPreview, m_prefs.carSettings, reset: false, skipSwapControllers: true, m_cachedSwap, m_installedCustomProfile?.id);
		userCar.carX.ResetTransmission();
		if (currentCategory == CarProfileControllerType.Engine)
		{
			RebuildEngineDescriptionParams();
		}
		else if (currentCategory == CarProfileControllerType.Transmission)
		{
			CalculateSpeedAccel();
		}
		BaseDynoParamsItemViewModel baseDynoParamsItemViewModel = GetSelectedItem(m_navLayer);
		if (baseDynoParamsItemViewModel?.controller != null)
		{
			switch (baseDynoParamsItemViewModel.controller.type)
			{
			case CarProfileControllerType.Suspension:
				userCar.carX.SetSteer(m_dynoSteeringModel.currentSteer, useAssistent: false, Time.deltaTime);
				break;
			case CarProfileControllerType.Wheels:
				if (baseDynoParamsItemViewModel.controller.changers[0].parameter.type == CarProfileParameterType.FrontTireType || baseDynoParamsItemViewModel.controller.changers[0].parameter.type == CarProfileParameterType.RearTireType)
				{
					carFactory?.visualManager?.ApplyAllTires(userCar);
				}
				break;
			}
		}
		UpdateLists();
	}

	private void ResetCategory()
	{
		foreach (BaseDynoParamsItemViewModel item in m_itemsModel)
		{
			item.RevertToDefault();
		}
		DirtyChanges();
	}

	private void ResetParameter()
	{
		GetSelectedItem(m_navLayer)?.RevertToDefault();
		DirtyChanges();
	}

	private float GetControllerDefValue(CarProfileController profileController)
	{
		return profileController.changers[0].EvaluatePhase(profileController.changers[0].defaultValue);
	}

	public void OnClick(BaseDynoParamsItemViewModel item)
	{
		var (list, list2) = GetCurrListsViews();
		TrySelectItemInList(NavLayer.Left, list);
		TrySelectItemInList(NavLayer.Right, list2);
		m_wheelAnglePanel.SetSelected(isSelected: false);
		void TrySelectItemInList(NavLayer layer, DynoParamsList dynoParamsList)
		{
			if (!(dynoParamsList == null))
			{
				int num = dynoParamsList.IndexOf(item);
				if (num >= 0)
				{
					SetSelectedId(layer, num);
				}
			}
		}
	}

	public void OnDoubleClick(DynoParamsItemViewModel item)
	{
	}

	public void UpdateLists()
	{
		m_currDynoParamsListsView.ForEach(delegate(DynoParamsList list)
		{
			list.UpdateList();
		});
	}

	public void SetHidden(bool hidden)
	{
		if (hidden)
		{
			HideBottomPanel();
			m_topPanelContext.gameObject.SetActive(value: false);
			base.gameObject.SetActive(value: false);
			m_contextWasHidden = true;
		}
		else
		{
			base.gameObject.SetActive(value: true);
			m_topPanelContext.gameObject.SetActive(value: true);
			UpdateBottomPanel();
			SelectCategory(currentCategory);
		}
	}

	[GameHotkey(ActionType.NavUp, false, false)]
	public void NavigationUp()
	{
		int selectedId = GetSelectedId(m_navLayer);
		if (--selectedId < 0 && m_wheelAnglePanel.isActive)
		{
			SelectWheelAnglePanel();
		}
		else
		{
			SetSelectedId(m_navLayer, selectedId, isNavigation: true);
		}
	}

	public void ResetListsSelection()
	{
		(DynoParamsList, DynoParamsList) currListsViews = GetCurrListsViews();
		currListsViews.Item1?.ClearSelection();
		currListsViews.Item2?.ClearSelection();
	}

	private void SelectWheelAnglePanel()
	{
		ResetListsSelection();
		m_wheelAnglePanel.SetSelected(isSelected: true);
	}

	[GameHotkey(ActionType.NavDown, false, false)]
	public void NavigationDown()
	{
		int selectedId = GetSelectedId(m_navLayer);
		int itemsCount = GetItemsCount(m_navLayer);
		m_wheelAnglePanel.SetSelected(isSelected: false);
		if (++selectedId < itemsCount)
		{
			SetSelectedId(m_navLayer, selectedId, isNavigation: true);
		}
	}

	[GameHotkey(ActionType.NavRight, false, true)]
	public void NavigationRight()
	{
		var (dynoParamsList, dynoParamsList2) = GetCurrListsViews();
		if (m_navLayer != NavLayer.Left)
		{
			_ = 1;
		}
		else if (dynoParamsList2 != null && dynoParamsList2.GetCount() > 0)
		{
			int newIndex = Math.Min(dynoParamsList2.GetCount() - 1, dynoParamsList.selectedIndex);
			SetSelectedId(NavLayer.Right, newIndex, isNavigation: true);
			m_wheelAnglePanel.SetSelected(isSelected: false);
		}
	}

	[GameHotkey(ActionType.NavLeft, false, true)]
	public void NavigationLeft()
	{
		if (!m_wheelAnglePanel.isSelected)
		{
			(DynoParamsList, DynoParamsList) currListsViews = GetCurrListsViews();
			DynoParamsList item = currListsViews.Item1;
			DynoParamsList item2 = currListsViews.Item2;
			NavLayer navLayer = m_navLayer;
			if (navLayer != NavLayer.Left && navLayer == NavLayer.Right && item != null && item.GetCount() > 0)
			{
				int newIndex = Math.Min(item.GetCount() - 1, item2.selectedIndex);
				SetSelectedId(NavLayer.Left, newIndex, isNavigation: true);
			}
		}
	}

	private void OnSaveSetup()
	{
		if (Overlay.instance is SteamOverlay steamOverlay && GameManager.IsLinuxOsPlatform())
		{
			steamOverlay.ShowOverlayKeyboard(LocalizationKeys.custom_setup_save_promt.Localized(), string.Empty, 16);
		}
		else
		{
			UIMessageBox.ShowInputDialog(LocalizationKeys.custom_setup_save_promt.Localized(), string.Empty, null, null, LocalizationKeys.Cancel.Localized(), CreateCustomSetup, LocalizationKeys.Ok.Localized(), 16);
		}
	}

	private void SubscribePS4TextReceive()
	{
	}

	private void UnSubscribePS4TextReceive()
	{
	}

	private void NavigationSelect()
	{
		if (GetSelectedItem(m_navLayer) is DynoParamsItemViewModel dynoParamsItemViewModel)
		{
			dynoParamsItemViewModel.TryShowInputDialog();
		}
	}

	public void NavigationCancel()
	{
		UnSubscribePS4TextReceive();
		if (States.mainCurrent is DynoRaceModeState)
		{
			SaveControllers();
			ExitDyno();
		}
		else
		{
			HandleExit();
		}
	}

	public void NavigationReset()
	{
		if (m_wheelAnglePanel.isSelected)
		{
			m_wheelAnglePanel.ResetSteeringValue();
		}
		BaseDynoParamsItemViewModel baseDynoParamsItemViewModel = GetSelectedItem(m_navLayer);
		if (baseDynoParamsItemViewModel != null && baseDynoParamsItemViewModel.title != null)
		{
			UIMessageBox.ShowQuestion(LocalizationKeys.dyno_ask_reset_single_to_default.Localized(), null, LocalizationKeys.Cancel.Localized(), delegate
			{
				ResetParameter();
			}, LocalizationKeys.Ok.Localized());
		}
	}

	public void NavigationResetAll()
	{
		UIMessageBox.ShowQuestion(LocalizationKeys.dyno_ask_reset_all_to_defaults.Localized(), null, LocalizationKeys.Cancel.Localized(), delegate
		{
			ResetCategory();
		}, LocalizationKeys.Ok.Localized());
	}

	public void NavigationGoToTestDrive()
	{
		if (XRModeManager.IsXRActive())
		{
			XRModeManager.ShowDialogNotAvailable();
			return;
		}
		States.State<DynoRaceModeState>().SetupDynoData(Export());
		m_customSetupSaveHelper.InitHelper(userCar, m_playerCarProfile, profileInfoPreview, profileId, carId, m_swapApplied);
		m_cachedSwap = m_customSetupSaveHelper.ConstructEngineSwapData();
		if (m_isDynoStateCurrent)
		{
			BasePrefsModel.LockSaves(flag: true);
			States.Change<DynoRaceModeState>();
		}
		else if (!(GameManager.raceDisabledByPhysExplo_Time > Time.unscaledTime))
		{
			base.owner.HideStackAndPush<UISelectorTrackContext>().SetRaceMode(RaceMode.Dyno);
			m_isDynoStateCurrent = true;
		}
	}

	public void NavIncValue()
	{
		m_wheelAnglePanel.IncSteering();
		DynoParamsItemView selectedItemView = GetSelectedItemView(m_navLayer);
		if (selectedItemView != null && !m_wheelAnglePanel.isSelected)
		{
			selectedItemView.IncValue();
		}
	}

	public void NavDecValue()
	{
		m_wheelAnglePanel.DecSteering();
		DynoParamsItemView selectedItemView = GetSelectedItemView(m_navLayer);
		if (selectedItemView != null && !m_wheelAnglePanel.isSelected)
		{
			selectedItemView.DecValue();
		}
	}
}
