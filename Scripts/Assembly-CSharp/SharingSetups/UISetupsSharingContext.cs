using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Binding;
using Binding.Base;
using CarProfile;
using CarX;
using DB;
using DB.Meta;
using DG.Tweening;
using DI;
using Db.Meta.CarProfile;
using GameInput;
using GameOverlay;
using Inventory;
using LocalCaching.Manager;
using LocalCaching.Model;
using PrefsModels;
using Quests;
using UnityEngine;
using UnityEngine.UI;

namespace SharingSetups
{
	[AudioFXBlocker(false)]
	public class UISetupsSharingContext : BaseBindingBehaviourTarget
	{
		private enum SharingMode
		{
			None = -1,
			FetchedSetups = 0,
			LocalCustomSetups = 1
		}

		private enum SelectedViewBlock
		{
			SetupsView = 0,
			ParamsView = 1
		}

		private enum SetupsListType
		{
			GeneralSetups = 0,
			OfficialSetups = 1,
			MyUploadedSetups = 2
		}

		private class TestDriveParams
		{
			public int profileId;

			public int engineId;

			public (string setupId, bool haveInstalledCustom, int engineId) customDataTuple;
		}

		private static TestDriveParams m_testDriveParams;

		private static HashSet<string> m_reportedSetups = new HashSet<string>();

		private static EngineSwapData m_cachedSwap;

		private static string m_currFrontToe;

		private static string m_currRearToe;

		private static string m_currFrontCamber;

		private static string m_currRearCamber;

		private const int REQUEST_DATA_PERIOD = 60;

		private const int PARAMS_SCROLL_STEP = 10;

		private const float SCROLL_GAP = 0.15f;

		private const string FILTER_FORMAT = "<color=yellow>{0}</color>";

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

		[SerializeField]
		private GameObject m_paramsViewItemPrefab;

		[SerializeField]
		private SetupSharingListElement m_setupSharingLineTemplate;

		[SerializeField]
		private RectTransform m_paramsListContainer;

		[SerializeField]
		private ScrollRect m_setupsScrollRect;

		[SerializeField]
		private ScrollRect m_paramsScrollRect;

		[SerializeField]
		private UISetupsSharingPublication m_uiSetupsSharingPublication;

		private readonly BoolProperty m_selectedSetupsViewBlock = new BoolProperty();

		private readonly BoolProperty m_showEmptyListMessage = new BoolProperty();

		private readonly BoolProperty m_showPublicationBlock = new BoolProperty();

		private readonly BoolProperty m_showParamsBlock = new BoolProperty(startValue: true);

		private readonly BoolProperty m_showRequestBg = new BoolProperty();

		private readonly IntProperty m_ctrlCategoryImg = new IntProperty();

		private readonly IntProperty m_tabMode = new IntProperty();

		private readonly IntProperty m_showCategoryIndicator = new IntProperty();

		private readonly StringProperty m_setupsSlotsCount = new StringProperty();

		private readonly StringProperty m_controllerTypeCaption = new StringProperty();

		private readonly StringProperty m_setupsType = new StringProperty();

		private readonly List<GameInput.DeviceType> m_allowDeviceTypesForPublish = new List<GameInput.DeviceType>
		{
			GameInput.DeviceType.Keyboard,
			GameInput.DeviceType.Gamepad,
			GameInput.DeviceType.Wheel
		};

		private SwapsAsset m_swapsAsset;

		private List<UISetupSharingParamItem> m_sharingParamsItems = new List<UISetupSharingParamItem>(32);

		private List<SetupSharingListElement> m_setupSharingLinesPool = new List<SetupSharingListElement>(128);

		private List<SetupSharingListElementData> m_setupSharingList = new List<SetupSharingListElementData>(128);

		private List<SetupSharingListElementData> m_setupSharingListCache = new List<SetupSharingListElementData>(128);

		private List<SetupSharingListElementData> m_setupSharingListOfficialCache = new List<SetupSharingListElementData>(32);

		private List<SetupSharingListElementData> m_setupSharingListMyUploadedCache = new List<SetupSharingListElementData>(128);

		private List<CarProfileControllerType> m_availableControllerTypes = new List<CarProfileControllerType>(6);

		private List<SdrSetupFull> m_publicationCodeCacheList = new List<SdrSetupFull>(1);

		private Dictionary<CarProfileControllerType, List<UISetupSharingParamItem>> m_viewParamsCurrentDict = new Dictionary<CarProfileControllerType, List<UISetupSharingParamItem>>(6);

		private List<ActionHintData> m_actionHints = new List<ActionHintData>();

		private List<HeaderActionData> m_headerData = new List<HeaderActionData>();

		private WaitForEndOfFrame m_waitEndFrame = new WaitForEndOfFrame();

		private SetupSharingListElement m_currentFocusedSetup;

		private UIReportContext m_reportContext;

		private Coroutine m_focusCoroutine;

		private IEnumerator m_setFirstFocusCoroutine;

		private IEnumerator m_setFocusUICoroutine;

		private Coroutine m_initSpecsAtStartCoroutine;

		private QuestsAsset m_questsAsset;

		private string m_lastCustomSetupId;

		private float m_time;

		private int m_carId = -1;

		private int m_prevProfileId = -1;

		private int m_currentCtrlCategory = 1;

		private PlayerCarProfile m_playerCarProfile;

		private PlayerCarProfile m_defaultCarProfile;

		private SetupsSharingService m_sharingService;

		private SharingFilterController m_sharingFilterController;

		private SharingMode m_currTabMode;

		private SetupsListType m_currSetupsListType;

		private SelectedViewBlock m_selectedViewBlock;

		private bool m_searchByCode;

		private bool m_deletingCustom;

		private bool m_activeActionOptionsContext;

		private bool m_activeRateContext;

		private bool m_activeFilterContext;

		private bool m_activeReportContext;

		private bool m_activeDialogContext;

		private bool m_activeConsoleContext;

		private bool m_swapApplied;

		private bool m_consoleRestrictionIsOn;

		private bool m_filterChanged;

		private bool m_showBottomWithDelay;

		private float m_requestDataTimer;

		private string[] m_headers = new string[7] { "car_profile_category_caption_suspension", "car_profile_category_caption_wheels", "car_profile_category_caption_engine", "car_profile_category_caption_transmission_1", "car_profile_category_caption_brakes_1", "car_profile_category_caption_suspensionstiffness", "car_profile_category_caption_weight" };

		public static bool isSetupsSharingActive => m_testDriveParams != null;

		public ProfilesInfo.Profile profileInfoPreview { get; private set; }

		protected override void OnActivate()
		{
			base.OnActivate();
			if (Overlay.instance.IsUserGeneratedContentRestricted)
			{
				m_consoleRestrictionIsOn = true;
				Overlay.instance.ShowUGCRestrictionDialog();
				TimeController.AddCallback(0f, CallSharingMenuContext);
				return;
			}
			Overlay.instance.richPresence.Show(RichPresenceStatus.CarTuning);
			ContextOwner contextOwner = base.owner;
			contextOwner.onContextActiveChanged = (Action<BaseContext>)Delegate.Combine(contextOwner.onContextActiveChanged, new Action<BaseContext>(OnContextActiveChangedHandler));
			m_carId = m_prefs.globals.lastCarId;
			PrefsCarSlotsModel carSettings = m_prefs.carSettings;
			TestDriveResetProfileIfNeed(m_carId, carSettings);
			m_swapsAsset = SwapsAsset.LoadAndInitDB();
			m_swapApplied = false;
			ResetTabMode();
			SetSelectedViewBlock(SelectedViewBlock.SetupsView);
			m_currSetupsListType = SetupsListType.GeneralSetups;
			m_showEmptyListMessage.value = m_setupSharingList.Count == 0;
			m_setupSharingLineTemplate.gameObject.SetActive(value: false);
			CarSwap.SaveStockEngineIfNeeded(m_carId, carSettings);
			m_car3dView.SetCarsSound(enable: false);
			m_car3dView.SetGarageCarStatePhysAndHoldPosition(disablePhysics: false, holdPosition: false);
			m_carSpecsTable.InitCarInfo(m_db.GetById<PlayerCar>(m_carId));
			m_prevProfileId = -1;
			m_filterChanged = false;
			InitParams();
			InitSharingServiceIfNeed();
			InitReportContextIfNeed();
			InitFilters();
			SetSetupsTypeVal();
			Overlay.instance.CheckSystemCrossPlatformAbility(delegate(bool restrictedCrossMp)
			{
				if (restrictedCrossMp)
				{
					m_sharingFilterController.SetFilterValue(SetupSharingFilterType.CrossMask, (int)Overlay.instance.GetPlatform());
					m_sharingFilterController.SaveFilter<SharingSetupsFilter>();
					OnApplyFilterHandler();
				}
			});
			UpdateTopPanel();
			SubscribePS4TextReceive();
			SubscribeSteamDeckTextReceive();
		}

		private void SubscribeSteamDeckTextReceive()
		{
			if (Overlay.instance is SteamOverlay steamOverlay && GameManager.IsLinuxOsPlatform())
			{
				steamOverlay.onCloseOverlayKeyboard = (Action<string>)Delegate.Combine(steamOverlay.onCloseOverlayKeyboard, new Action<string>(OnReceiveInputText));
			}
		}

		private void UnSubscribeSteamDeckTextReceive()
		{
			if (Overlay.instance is SteamOverlay steamOverlay && GameManager.IsLinuxOsPlatform())
			{
				steamOverlay.onCloseOverlayKeyboard = (Action<string>)Delegate.Remove(steamOverlay.onCloseOverlayKeyboard, new Action<string>(OnReceiveInputText));
			}
		}

		private void SubscribePS4TextReceive()
		{
		}

		private void UnSubscribePS4TextReceive()
		{
		}

		private void ApplySavedSwap(ProfilesInfo.Profile profile)
		{
			if (m_swapApplied || !m_playerCarProfile.CanSwap() || HaveInstalledCustomProfile())
			{
				return;
			}
			m_swapApplied = true;
			if (!m_prefs.carSettings.SwapAvailableForCar(m_carId))
			{
				return;
			}
			EngineSwapData engineSwapData = m_prefs.carSettings.GetEngineSwapDataForCar(m_carId);
			if (engineSwapData.id == 0 && engineSwapData.controllers.Count == 0)
			{
				int engineIdForCar = m_prefs.carSettings.GetEngineIdForCar(m_carId);
				EngineInfo engineInfo = m_swapsAsset.Find(engineIdForCar);
				if (engineInfo.id == 0)
				{
					EngineInfo engineInfo2 = m_swapsAsset.swaps.First();
					if (engineInfo2 == null)
					{
						return;
					}
					FillDefaultEngineParams(engineInfo, engineInfo2);
				}
				engineSwapData = new EngineSwapData(engineIdForCar);
				FillSwapControllers(engineInfo, engineSwapData);
			}
			if (engineSwapData.controllers == null)
			{
				return;
			}
			foreach (EngineSwapData.SwapController controller in engineSwapData.controllers)
			{
				profile.controllers[controller.key] = controller.value;
			}
		}

		private void FillDefaultEngineParams(EngineInfo info, EngineInfo referenceInfo)
		{
			info.turboCharged = CreateStockParam(referenceInfo.turboCharged);
			info.turboPressure = CreateStockParam(referenceInfo.turboPressure);
			info.revLimiter = CreateStockParam(referenceInfo.revLimiter);
			info.maxTorque = (EngineParamShiftInfo)CreateStockParam(referenceInfo.maxTorque);
			info.maxTorqueRpm = CreateStockParam(referenceInfo.maxTorqueRpm);
			EngineParamInfo CreateStockParam(EngineParamInfo reference)
			{
				EngineParamInfo engineParamInfo = null;
				if (m_defaultCarProfile == null || m_defaultCarProfile.dynoInfo == null)
				{
					return null;
				}
				foreach (CarProfileController controller in m_defaultCarProfile.dynoInfo.controllers)
				{
					if (!(controller.name != reference.name))
					{
						CarProfileChanger carProfileChanger = controller.changers[0];
						if (carProfileChanger == null || carProfileChanger.values.Count < 2)
						{
							engineParamInfo = reference.Copy();
						}
						else
						{
							engineParamInfo = ((reference is EngineParamShiftInfo) ? new EngineParamShiftInfo() : new EngineParamInfo());
							engineParamInfo.name = controller.name;
							engineParamInfo.value = carProfileChanger.defaultValue;
							engineParamInfo.min = carProfileChanger.values[0];
							engineParamInfo.max = carProfileChanger.values[1];
							if (engineParamInfo is EngineParamShiftInfo engineParamShiftInfo)
							{
								engineParamShiftInfo.multiplier = carProfileChanger.multiplier;
								engineParamShiftInfo.shift = carProfileChanger.shift;
							}
						}
						break;
					}
				}
				return engineParamInfo;
			}
		}

		private void FillSwapControllers(EngineInfo info, EngineSwapData swap)
		{
			AddControllerIfNeeded(info.turboCharged);
			AddControllerIfNeeded(info.turboPressure);
			AddControllerIfNeeded(info.revLimiter);
			AddControllerIfNeeded(info.maxTorque);
			AddControllerIfNeeded(info.maxTorqueRpm);
			void AddControllerIfNeeded(EngineParamInfo param)
			{
				if (param != null && swap.controllers.FindIndex((EngineSwapData.SwapController sc) => sc.key == param.name) == -1)
				{
					swap.AddController(CarSwapData.GetController(param));
				}
			}
		}

		private bool HaveInstalledCustomProfile()
		{
			return m_prefs.carSettings.GetHaveCustomSetupsPerCar(m_carId).haveInstalledCustom;
		}

		private void InitReportContextIfNeed()
		{
			if (!(m_reportContext != null))
			{
				m_reportContext = base.owner.Preload<UIReportContext>();
			}
		}

		private void InitParams(ProfilesInfo.Profile info = null, System.Action onParamsInited = null)
		{
			m_initSpecsAtStartCoroutine = TimeController.StartNow(InitParamsAsync(info, onParamsInited));
			IEnumerator InitParamsAsync(ProfilesInfo.Profile profile2 = null, System.Action action = null)
			{
				int profileId = m_prefs.carSettings.GetProfileIdForCar(m_carId);
				m_playerCarProfile = m_db.QueryRecordsByColumnValue("carId", m_carId, (PlayerCarProfile x) => x.carId == m_carId).Find((PlayerCarProfile x) => x.id == profileId).Copy();
				m_defaultCarProfile = m_playerCarProfile.Copy();
				RaceCar car = m_car3dView.currentCar;
				CustomProfile installedCp = m_prefs.carSettings.GetInstalledCustomProfile(m_carId);
				ProfilesInfo.Profile profile = profile2 ?? m_prefs.carSettings.GetProfileInfo(profileId, m_carId, installedCp?.id).Copy();
				ApplySavedSwap(profile);
				RefreshProfilePreview(profile);
				int engineId = m_prefs.carSettings.GetEngineIdForCar(m_carId, installedCp?.id);
				CarSwap swap = car.swap;
				yield return m_waitEndFrame;
				yield return swap.carAttached ? swap.Refresh(engineId, force: true) : swap.Initialize(car, m_prefs, m_db, engineId, force: false, m_playerCarProfile);
				yield return InitSpecs(installedCp);
				InitParamsListViewItems();
				m_ctrlCategoryImg.value = GetAvailableCtrlCategoriesCount();
				InflateParamsViewList_Current(m_availableControllerTypes[0]);
				m_initSpecsAtStartCoroutine = null;
				m_deletingCustom = false;
				action.SafeCall();
			}
		}

		protected override void OnDeactivate()
		{
			if (m_consoleRestrictionIsOn)
			{
				m_consoleRestrictionIsOn = false;
				return;
			}
			UnSubscribePS4TextReceive();
			UnSubscribeSteamDeckTextReceive();
			ContextOwner contextOwner = base.owner;
			contextOwner.onContextActiveChanged = (Action<BaseContext>)Delegate.Remove(contextOwner.onContextActiveChanged, new Action<BaseContext>(OnContextActiveChangedHandler));
			m_sharingService.onRequestProcessing = null;
			m_car3dView.SetCarsSound(enable: true);
			m_car3dView.SetGarageCarStatePhysAndHoldPosition(disablePhysics: true, holdPosition: false);
			m_carSpecsTable.ClearCachedInitParams();
			FlushCoroutineIfNeed(m_focusCoroutine);
			FlushCoroutineIfNeed(m_setFirstFocusCoroutine);
			FlushCoroutineIfNeed(m_setFocusUICoroutine);
			FlushCoroutineIfNeed(m_initSpecsAtStartCoroutine);
			ClearListsCache();
			HideTopPanel();
			HideBottomPanel();
			base.OnDeactivate();
		}

		private void FlushCoroutineIfNeed(Coroutine coroutine)
		{
			if (coroutine != null)
			{
				TimeController.StopNow(coroutine);
				coroutine = null;
			}
		}

		private void FlushCoroutineIfNeed(IEnumerator coroutine)
		{
			if (coroutine != null)
			{
				TimeController.StopNow(coroutine);
				coroutine = null;
			}
		}

		public static void TestDriveResetProfileIfNeed(int carId, PrefsCarSlotsModel carSettings)
		{
			if (isSetupsSharingActive)
			{
				(string, bool, int) haveCustomSetupsPerCar = carSettings.GetHaveCustomSetupsPerCar(carId);
				int engineIdForCar = carSettings.GetEngineIdForCar(carId);
				if (haveCustomSetupsPerCar.Item2)
				{
					carSettings.SetHaveCustomSetupsPerCar(carId, engineIdForCar, haveCustomSetupsPerCar.Item1, isHaveCustomSetup: false);
				}
				(string, bool, int) customDataTuple = m_testDriveParams.customDataTuple;
				carSettings.SetHaveCustomSetupsPerCar(carId, customDataTuple.Item3, customDataTuple.Item1, customDataTuple.Item2);
				carSettings.SetProfilePerCar(carId, m_testDriveParams.profileId);
				carSettings.SetEngineForCar(carId, m_testDriveParams.engineId);
			}
		}

		public static void SnapCurrentCamberToeValues(RaceCar car)
		{
			Vector3 eulerAngles = car.carModel.carWheels[0].hub.parent.localRotation.eulerAngles;
			Vector3 eulerAngles2 = car.carModel.carWheels[2].hub.parent.localRotation.eulerAngles;
			float num = Mathf.DeltaAngle(0f, eulerAngles.y);
			float num2 = Mathf.DeltaAngle(0f, eulerAngles2.y);
			float num3 = Mathf.DeltaAngle(0f, eulerAngles.z);
			float num4 = Mathf.DeltaAngle(0f, eulerAngles2.z);
			m_currFrontToe = num.ToString("N2");
			m_currRearToe = num2.ToString("N2");
			m_currFrontCamber = num3.ToString("N2");
			m_currRearCamber = num4.ToString("N2");
		}

		public static void ClearTestDriveParams()
		{
			m_testDriveParams = null;
		}

		private void RefreshProfilePreview(ProfilesInfo.Profile profile)
		{
			if (profile != null)
			{
				profileInfoPreview = new ProfilesInfo.Profile(profile.id)
				{
					controllers = new Dictionary<string, float>(profile.controllers)
				};
			}
		}

		private void ClearListsCache()
		{
			m_setupSharingList.Clear();
			m_setupSharingListCache.Clear();
			m_setupSharingListOfficialCache.Clear();
			m_setupSharingListMyUploadedCache.Clear();
		}

		private void OnContextActiveChangedHandler(BaseContext ctx)
		{
			ProcessActionsContext(ctx);
			ProcessRateContext(ctx);
			ProcessFilterContext(ctx);
			ProcessReportContext(ctx);
			ProcessInputContext(ctx);
			ProcessGameConsoleContext(ctx);
		}

		private void ProcessGameConsoleContext(BaseContext ctx)
		{
			if (ctx is UIConsoleUGUIContext uIConsoleUGUIContext)
			{
				m_activeConsoleContext = uIConsoleUGUIContext.isActive;
			}
		}

		private void ProcessActionsContext(BaseContext ctx)
		{
			if (ctx is UISharingSetupsOptionsContext uISharingSetupsOptionsContext)
			{
				m_activeActionOptionsContext = uISharingSetupsOptionsContext.isActive;
				if (uISharingSetupsOptionsContext.isActive)
				{
					uISharingSetupsOptionsContext.onDeleteCustomSetup = (System.Action)Delegate.Combine(uISharingSetupsOptionsContext.onDeleteCustomSetup, new System.Action(OnDeleteCustomSetupHandler));
					uISharingSetupsOptionsContext.onRate = (System.Action)Delegate.Combine(uISharingSetupsOptionsContext.onRate, new System.Action(OnRateHandler));
					uISharingSetupsOptionsContext.onGoTestdrive = (System.Action)Delegate.Combine(uISharingSetupsOptionsContext.onGoTestdrive, new System.Action(OnGoTestdrive));
				}
				else
				{
					uISharingSetupsOptionsContext.onDeleteCustomSetup = (System.Action)Delegate.Remove(uISharingSetupsOptionsContext.onDeleteCustomSetup, new System.Action(OnDeleteCustomSetupHandler));
					uISharingSetupsOptionsContext.onRate = (System.Action)Delegate.Remove(uISharingSetupsOptionsContext.onRate, new System.Action(OnRateHandler));
					uISharingSetupsOptionsContext.onGoTestdrive = (System.Action)Delegate.Remove(uISharingSetupsOptionsContext.onGoTestdrive, new System.Action(OnGoTestdrive));
				}
			}
		}

		private void ProcessRateContext(BaseContext ctx)
		{
			if (ctx is UISharingRateContext uISharingRateContext)
			{
				m_activeRateContext = uISharingRateContext.isActive;
				if (uISharingRateContext.isActive)
				{
					uISharingRateContext.onRateDone = (Action<int>)Delegate.Combine(uISharingRateContext.onRateDone, new Action<int>(OnRateDoneHandler));
				}
				else
				{
					uISharingRateContext.onRateDone = (Action<int>)Delegate.Remove(uISharingRateContext.onRateDone, new Action<int>(OnRateDoneHandler));
				}
			}
		}

		private void ProcessFilterContext(BaseContext ctx)
		{
			if (ctx is UISharingSetupsFilterContext uISharingSetupsFilterContext)
			{
				m_activeFilterContext = uISharingSetupsFilterContext.isActive;
				if (uISharingSetupsFilterContext.isActive)
				{
					uISharingSetupsFilterContext.onApplyFilter = (System.Action)Delegate.Combine(uISharingSetupsFilterContext.onApplyFilter, new System.Action(OnApplyFilterHandler));
				}
				else
				{
					uISharingSetupsFilterContext.onApplyFilter = (System.Action)Delegate.Remove(uISharingSetupsFilterContext.onApplyFilter, new System.Action(OnApplyFilterHandler));
				}
			}
		}

		private void ProcessReportContext(BaseContext ctx)
		{
			if (ctx is UIReportContext uIReportContext)
			{
				m_activeReportContext = uIReportContext.isActive;
				if (uIReportContext.isActive)
				{
					UnSubscribePS4TextReceive();
					UnSubscribeSteamDeckTextReceive();
					uIReportContext.onSendSharingReport = (Action<string>)Delegate.Combine(uIReportContext.onSendSharingReport, new Action<string>(OnSendSetupReport));
					HideBottomPanel();
				}
				else
				{
					SubscribePS4TextReceive();
					SubscribeSteamDeckTextReceive();
					UpdateBottomPanel();
					uIReportContext.onSendSharingReport = (Action<string>)Delegate.Remove(uIReportContext.onSendSharingReport, new Action<string>(OnSendSetupReport));
				}
			}
		}

		private void ProcessInputContext(BaseContext ctx)
		{
			if (ctx is UIBaseDialogContext uIBaseDialogContext)
			{
				m_activeDialogContext = uIBaseDialogContext.isActive;
			}
		}

		private void OnRateDoneHandler(int rate)
		{
			if (m_currentFocusedSetup == null)
			{
				return;
			}
			string code = m_currentFocusedSetup.code;
			UILockInputContext.EnableLock();
			m_sharingService.MakeRequestOfType((code, rate), delegate(SdrRestSetupRating response)
			{
				UILockInputContext.DisableLock();
				if (response != null)
				{
					GUICommonGodVoice.ShowText((response.updated ? LocalizationKeys.setup_rate_updated : LocalizationKeys.setup_rate_success).LocalizedFormat(m_currentFocusedSetup.elementName), 4f);
				}
			});
		}

		private void OnRateHandler()
		{
			base.owner.Push<UISharingRateContext>().SetTitle(LocalizationKeys.sharing_rate_dialog_title.Localized());
		}

		private void OnGoTestdrive()
		{
			SaveProfileAndCustomState();
			base.owner.HideAllAndPush<UISelectorTrackContext>().SetRaceMode(RaceMode.Freeride);
		}

		private void OnApplyFilterHandler()
		{
			m_filterChanged = true;
			ResetRequestDataTimer();
			ProcessSetupsListTypeView();
		}

		private void OnDeleteCustomSetupHandler()
		{
			UIMessageBox.ShowQuestion(LocalizationKeys.custom_setup_delete_promt.Localized(), null, LocalizationKeys.Cancel.Localized(), delegate
			{
				if (m_currentFocusedSetup != null)
				{
					m_deletingCustom = true;
					CustomProfile customProfile = m_currentFocusedSetup.customProfile;
					CustomProfile installedCustomProfile = m_prefs.carSettings.GetInstalledCustomProfile(m_carId);
					m_prefs.carSettings.RemoveCustomSetup(m_carId, customProfile.id);
					ResetTabMode();
					OpenLocalCustoms();
					if (m_prevProfileId > -1)
					{
						m_prefs.carSettings.SetProfilePerCar(m_carId, m_prevProfileId);
						m_prevProfileId = -1;
					}
					if (m_setupSharingList.Count > 0 && installedCustomProfile != null)
					{
						string customSetupId = installedCustomProfile?.id ?? m_setupSharingList[0].customSetupId;
						SetupSharingListElementData item = FindSetupSharingData(customSetupId);
						m_currentFocusedSetup = FindSetupUI(item);
						m_currentFocusedSetup?.SetFocus(isFocused: true);
						InstallSelectedCustomSetup();
					}
					else
					{
						InitParams(null, delegate
						{
							UpdateUILinesFromData();
							UpdateBottomPanel();
						});
					}
				}
			}, LocalizationKeys.Ok.Localized());
		}

		private void OnSendSetupReport(string setupCode)
		{
			m_showBottomWithDelay = true;
			m_reportedSetups.Add(setupCode);
			RemoveReportedSetups();
			UpdateUILinesFromData();
		}

		private void ResetTabMode()
		{
			m_currTabMode = SharingMode.None;
		}

		private void InitSharingServiceIfNeed()
		{
			if (m_sharingService == null)
			{
				m_sharingService = BaseServerSharingService.Create<SetupsSharingService>();
			}
			SetupsSharingService sharingService = m_sharingService;
			sharingService.onRequestProcessing = (Action<bool, Type>)Delegate.Combine(sharingService.onRequestProcessing, (Action<bool, Type>)delegate(bool requestInProcess, Type type)
			{
				if (type == typeof(SdrRestSetupSingle) && !m_searchByCode)
				{
					m_showRequestBg.value = requestInProcess;
					m_uiSetupsSharingPublication.ResetNames();
				}
			});
		}

		private void InitFilters()
		{
			if (m_sharingFilterController == null)
			{
				m_sharingFilterController = new SharingFilterController(m_prefs);
			}
			m_sharingFilterController.InitFilter<SharingSetupsFilter>();
			if (m_sharingFilterController.sharingFilter is SharingSetupsFilter sharingSetupsFilter)
			{
				sharingSetupsFilter.SetCurrentProfilesCallBack(GetCurrentCarProfilesNames);
				sharingSetupsFilter.SetProfileIdCallBack(GetLastCarProfileByName);
				sharingSetupsFilter.RefreshProfilesIfNeeded();
			}
		}

		private string GetCurrentCarProfilesNames()
		{
			List<CarProfileProxy> profileList = CarProfileCacheManager.GetProfileList(m_db, m_carFactory, m_prefs.globals.lastCarId);
			string text = string.Empty;
			bool flag = Overlay.instance.IsDLCInstalled(DLCCode.FreeContent);
			foreach (CarProfileProxy item in profileList)
			{
				if (!(item.name == "Stock") && !(item.name == "Turbo") && (flag || m_prefs.locks.availableProfiles.Contains(item.id)))
				{
					text = text + item.name + ",";
				}
			}
			return string.Format("{0}{1},{2}", "ANY_PROFILE: ", UISharingSetupsFilterContext.ProfileType.Swaps, text.TrimEnd(','));
		}

		private int GetLastCarProfileByName(string profileName)
		{
			int lastCarId = m_prefs.globals.lastCarId;
			return GetCarProfileByName(lastCarId, profileName)?.id ?? 0;
		}

		private PlayerCarProfile GetCarProfileByName(int carId, string profileName)
		{
			if (!string.IsNullOrEmpty(profileName))
			{
				return m_db.QueryRecordsByColumnValue("carId", carId, (PlayerCarProfile x) => x.carId == carId).Find((PlayerCarProfile x) => x.name == profileName);
			}
			return null;
		}

		private void UpdateTopPanel()
		{
			m_headerData.Clear();
			m_headerData.Add(new HeaderActionData
			{
				text = LocalizationKeys.sharing_server_items.Localized(),
				action = OpenServerCustoms
			});
			m_headerData.Add(new HeaderActionData
			{
				text = LocalizationKeys.customs_window_title.Localized(),
				action = OpenLocalCustoms
			});
			ShowTopPanel(ETopPanelState.Actions, null, m_headerData.ToArray());
			int index = (isSetupsSharingActive ? 1 : 0);
			m_topPanelContext.switcherActions.SelectionAction(index);
		}

		private void OpenServerCustoms()
		{
			if (m_currTabMode != SharingMode.FetchedSetups)
			{
				ClearSetupsList();
				m_currTabMode = SharingMode.FetchedSetups;
				m_tabMode.value = (int)m_currTabMode;
				ProcessSetupsListTypeView();
			}
		}

		private void OpenLocalCustoms()
		{
			if (m_currTabMode != SharingMode.LocalCustomSetups)
			{
				SnapLastFocused();
				ClearSetupsList();
				m_currTabMode = SharingMode.LocalCustomSetups;
				m_tabMode.value = (int)m_currTabMode;
				FillCustomSetupsList();
				m_setupsSlotsCount.value = $"{m_setupSharingList.Count}/{16}";
				UpdateBottomPanel();
			}
		}

		private void SnapLastFocused()
		{
			m_lastCustomSetupId = m_currentFocusedSetup?.customSetupId;
		}

		private void UpdateBottomPanel()
		{
			m_actionHints.Clear();
			m_actionHints.Add(new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_goto_back.Localized(),
				type = ActionType.NavCancelBtnB,
				action = GoToSharingMenu
			});
			switch (m_currTabMode)
			{
			case SharingMode.FetchedSetups:
				UpdateFetchedSetupsBottom();
				break;
			case SharingMode.LocalCustomSetups:
				UpdateCustomsBottom();
				break;
			}
			m_actionHints.Add(new ActionHintData
			{
				hint = LocalizationKeys.toggle_sharing_params_view.Localized(),
				type = ActionType.NavAdditional1BtnY,
				action = ToggleShowParamsView
			});
			TimeController.AddCallback(m_showBottomWithDelay ? 0.3f : 0f, delegate
			{
				m_showBottomWithDelay = false;
				ShowBottomPanel(m_actionHints.ToArray());
			});
		}

		private void ToggleShowParamsView()
		{
			m_showParamsBlock.value = !m_showParamsBlock.value;
			if (!m_showParamsBlock.value)
			{
				SetSetupsSelected();
			}
		}

		private void UpdateFetchedSetupsBottom()
		{
			TryAddSaveSetupHintButton();
			if (m_currSetupsListType != SetupsListType.MyUploadedSetups)
			{
				string hint = (m_sharingFilterController.IsActiveFilter() ? $"<color=yellow>{LocalizationKeys.active_filter.Localized()}</color>" : LocalizationKeys.filters.Localized());
				m_actionHints.Add(new ActionHintData
				{
					hint = hint,
					type = ActionType.NavAdditional0BtnX,
					action = OnPressFilter
				});
				TryAddReportButton();
				m_actionHints.Add(new ActionHintData
				{
					hint = LocalizationKeys.input_publication_code.Localized(),
					type = ActionType.NavBtnExpandRight,
					action = OnPastePublicationCode
				});
			}
			void TryAddReportButton()
			{
				if (m_setupSharingList.Count != 0 && (!PlayerId.TryParse(m_currentFocusedSetup?.publisherPlayerId, out var playerId) || !playerId.Equals(Overlay.instance.player.id)))
				{
					m_actionHints.Add(new ActionHintData
					{
						hint = LocalizationKeys.nav_select_report.Localized(),
						type = ActionType.NavReport,
						action = NavigationReportRoom
					});
				}
			}
		}

		private void OnPressFilter()
		{
			base.owner.Push<UISharingSetupsFilterContext>().InitFilter(m_sharingFilterController);
		}

		private void OnPastePublicationCode()
		{
			if (Overlay.instance is SteamOverlay steamOverlay && GameManager.IsLinuxOsPlatform())
			{
				steamOverlay.ShowOverlayKeyboard(LocalizationKeys.input_publication_code.Localized(), string.Empty, 22);
			}
			else
			{
				ShowInputDialog();
			}
			void ShowInputDialog()
			{
				UIMessageBox.ShowInputDialog(LocalizationKeys.input_publication_code.Localized(), string.Empty, LocalizationKeys.publication_code.Localized(), InputField.ContentType.Standard, null, LocalizationKeys.Cancel.Localized(), OnReceiveInputText, LocalizationKeys.nav_hint_apply.Localized(), 22);
			}
		}

		private void NavigationReportRoom()
		{
			if (m_currentFocusedSetup == null)
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.report_select_setup.Localized(), 3f);
			}
			else if (m_setFirstFocusCoroutine == null && m_setFocusUICoroutine == null)
			{
				if (m_reportContext == null || !m_reportContext.isActive)
				{
					m_reportContext = base.owner.Get<UIReportContext>();
				}
				m_reportContext.ShowSharingReport(base.owner, m_currentFocusedSetup.code, m_currentFocusedSetup.publisherPlayerId, ReasonType.SETUP);
			}
		}

		private void RemoveReportedSetups()
		{
			foreach (string ignoredCode in m_reportedSetups)
			{
				m_setupSharingList.RemoveAll((SetupSharingListElementData item) => item.code == ignoredCode);
			}
		}

		private void OnReceiveInputText(string code)
		{
			if (IsEmptyCode())
			{
				return;
			}
			code = string.Join(string.Empty, code.Split((string[])null, StringSplitOptions.RemoveEmptyEntries));
			if (IsEmptyCode())
			{
				return;
			}
			m_searchByCode = true;
			UILockInputContext.EnableLock();
			MakeSingleSetupRequest(code, delegate(SdrRestSetupSingle response)
			{
				m_searchByCode = false;
				PlayerId playerId;
				if (response == null || response.setup == null)
				{
					UILockInputContext.DisableLock();
					GUICommonGodVoice.ShowText(LocalizationKeys.wrong_publication_code.Localized(), 3f);
				}
				else if (!PlayerId.TryParse(response.setup.createPlayerId, out playerId))
				{
					UILockInputContext.DisableLock();
				}
				else
				{
					m_sharingService.MakeRequestOfType(playerId.ToString(), delegate(SdrNickname result)
					{
						UILockInputContext.DisableLock();
						if (result == null)
						{
							GUICommonGodVoice.ShowText(LocalizationKeys.wrong_publication_code.Localized(), 3f);
						}
						else
						{
							m_publicationCodeCacheList.Clear();
							TryAddResultToList(response.setup);
							FillSetupsSharingList(m_publicationCodeCacheList, result.name);
							UpdateBottomPanel();
						}
					});
				}
			});
			bool IsEmptyCode()
			{
				if (string.IsNullOrEmpty(code))
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.wrong_publication_code.Localized(), 3f);
					return true;
				}
				return false;
			}
			void TryAddResultToList(SdrSetupFull item)
			{
				if (item != null)
				{
					bool flag = m_currSetupsListType == SetupsListType.OfficialSetups;
					bool flag2 = m_reportedSetups.Contains(item.code);
					if (!(item.carId != m_carId || SkipSetup(item.profile, item.profileId, item.physicsVersion, item.engineId, item.crossMask, item.createPlayerId) || (flag && !item.official) || flag2))
					{
						m_publicationCodeCacheList.Add(item);
					}
				}
			}
		}

		private void TryAddSaveSetupHintButton()
		{
			if (m_setupSharingList.Count != 0 && m_currSetupsListType != SetupsListType.MyUploadedSetups && !(m_currentFocusedSetup == null))
			{
				string focusedSetupId = m_currentFocusedSetup.customSetupId;
				if (!m_prefs.carSettings.GetCustomProfilesByCarId(m_carId).Exists((CustomProfile cp) => cp.id == focusedSetupId))
				{
					m_actionHints.Add(new ActionHintData
					{
						hint = LocalizationKeys.save_custom_setup.Localized(),
						type = ActionType.NavSelectBtnA,
						action = SaveFetchedCustomSetup
					});
				}
			}
		}

		private void SaveFetchedCustomSetup()
		{
			if (m_currentFocusedSetup == null)
			{
				return;
			}
			RequestProfileDataIfNeed(delegate
			{
				string code = m_currentFocusedSetup.code;
				string setupName = m_currentFocusedSetup.elementName;
				string customSetupId = m_currentFocusedSetup.customSetupId;
				bool isOfficial = m_currentFocusedSetup.isOfficial;
				int profileId = m_currentFocusedSetup.profileData.id;
				int engineId = m_currentFocusedSetup.engineId;
				ProfilesInfo.Profile profileInfo = m_currentFocusedSetup.profileData;
				CustomProfile cp;
				bool flag = m_prefs.carSettings.CheckHaveCustomSetup(out cp, m_carId, setupName, profileId, engineId);
				CustomProfile installedCustomProfile = m_prefs.carSettings.GetInstalledCustomProfile(m_carId);
				bool needUpdateSpecs = flag && cp != null && installedCustomProfile?.id == cp.id;
				UIMessageBox.ShowQuestion(flag ? LocalizationKeys.rewrite_custom_setup_question.LocalizedFormat(cp.name) : LocalizationKeys.save_custom_setup_question.Localized(), null, LocalizationKeys.Cancel.Localized(), delegate
				{
					UILockInputContext.EnableLock();
					m_sharingService.MakeRequestOfType(code, delegate(SdrRestSetupMarkDownload result)
					{
						UILockInputContext.DisableLock();
						if (result != null)
						{
							if (needUpdateSpecs)
							{
								m_prefs.carSettings.RemoveCustomSetup(m_carId, cp.id);
							}
							PerformSave();
							if (needUpdateSpecs)
							{
								InstallSelectedCustomSetup();
							}
						}
					});
				}, LocalizationKeys.Ok.Localized());
				void PerformSave()
				{
					CarSwap.SaveStockEngineIfNeeded(m_carId, m_prefs.carSettings, profileInfo);
					m_prefs.carSettings.SetCustomProfileInfo(out var reason, out var customProfile, setupName, profileId, m_carId, engineId, profileInfo.Copy(), customSetupId);
					if (reason == CustomSetupsSaveResult.Success || reason == CustomSetupsSaveResult.Rewrite)
					{
						customProfile?.SetServerData(code, isOfficial);
					}
					GUICommonGodVoice.ShowText(GetSaveCustomMsg(setupName, reason), 3f);
					m_currentFocusedSetup.UpdateDownloadLabelStatus(download: true);
					m_currentFocusedSetup.UpdateDownloadIconStatus(reason == CustomSetupsSaveResult.Success);
					UpdateBottomPanel();
				}
			});
			static string GetSaveCustomMsg(string setupName, CustomSetupsSaveResult result)
			{
				return result switch
				{
					CustomSetupsSaveResult.Success => LocalizationKeys.custom_setup_success_save.LocalizedFormat(setupName), 
					CustomSetupsSaveResult.Rewrite => LocalizationKeys.custom_setup_rewrite_save.LocalizedFormat(setupName), 
					CustomSetupsSaveResult.SlotExceed => LocalizationKeys.custom_setup_slots_exceed.Localized(), 
					_ => LocalizationKeys.custom_setup_fail_save.Localized(), 
				};
			}
		}

		private void UpdateCustomsBottom()
		{
			if (m_currentFocusedSetup == null)
			{
				return;
			}
			if (!m_currentFocusedSetup.isDownloaded)
			{
				m_actionHints.Add(new ActionHintData
				{
					hint = LocalizationKeys.publish.Localized(),
					type = ActionType.NavSelectBtnA,
					action = UploadSetupToRemote
				});
			}
			if (m_setupSharingList.Count > 0)
			{
				if (m_currentFocusedSetup.isDownloaded)
				{
					m_actionHints.Add(new ActionHintData
					{
						hint = LocalizationKeys.actions.Localized(),
						type = ActionType.NavAdditional0BtnX,
						action = CallActionsMenu
					});
				}
				CustomProfile installedCustomProfile = m_prefs.carSettings.GetInstalledCustomProfile(m_carId);
				CustomProfile customProfile = m_currentFocusedSetup.customProfile;
				if ((installedCustomProfile != null && !installedCustomProfile.Equals(customProfile)) || installedCustomProfile == null)
				{
					m_actionHints.Add(new ActionHintData
					{
						hint = LocalizationKeys.swaps_install_button.Localized(),
						type = ActionType.NavBtnExpandRight,
						action = InstallSelectedCustomSetup
					});
				}
			}
		}

		private void CallActionsMenu()
		{
			base.owner.Push<UISharingSetupsOptionsContext>();
		}

		private void InstallSelectedCustomSetup()
		{
			if (!(m_currentFocusedSetup == null))
			{
				int engineId = m_currentFocusedSetup.engineId;
				int profileId = m_currentFocusedSetup.profileId;
				string customSetupId = m_currentFocusedSetup.customSetupId;
				PrefsCarSlotsModel carSettings = m_prefs.carSettings;
				(string, bool, int) haveCustomSetupsPerCar = carSettings.GetHaveCustomSetupsPerCar(m_carId);
				if (haveCustomSetupsPerCar.Item2)
				{
					carSettings.SetHaveCustomSetupsPerCar(m_carId, engineId, haveCustomSetupsPerCar.Item1, isHaveCustomSetup: false);
				}
				m_prevProfileId = m_prefs.carSettings.GetProfileIdForCar(m_carId);
				carSettings.SetHaveCustomSetupsPerCar(m_carId, engineId, customSetupId, isHaveCustomSetup: true);
				carSettings.SetProfilePerCar(m_carId, profileId);
				carSettings.SetEngineForCar(m_carId, engineId);
				SoundController.Play2DUISound(SoundSample.ProSetup);
				ProfilesInfo.Profile info = m_currentFocusedSetup?.profileData;
				InitParams(info);
				SnapCurrentCamberToeValues(m_car3dView.currentCar);
				RequestProfileDataIfNeed(null);
				UpdateUILinesFromData();
			}
		}

		private void SaveProfileAndCustomState()
		{
			CustomProfile customProfile = m_currentFocusedSetup.customProfile;
			PrefsCarSlotsModel carSettings = m_prefs.carSettings;
			m_testDriveParams = new TestDriveParams
			{
				profileId = carSettings.GetProfileIdForCar(m_carId),
				engineId = carSettings.GetEngineIdForCar(m_carId),
				customDataTuple = carSettings.GetHaveCustomSetupsPerCar(m_carId)
			};
			carSettings.SetHaveCustomSetupsPerCar(m_carId, customProfile.engineId, customProfile.id, isHaveCustomSetup: true);
			carSettings.SetProfilePerCar(m_carId, customProfile.profile.id);
			carSettings.SetEngineForCar(m_carId, customProfile.engineId);
			CarSwap.SaveStockEngineIfNeeded(m_carId, carSettings, customProfile.profile);
			m_cachedSwap = ConstructEngineSwapData();
		}

		private EngineSwapData ConstructEngineSwapData()
		{
			EngineSwapData engineSwapData = new EngineSwapData(m_prefs.carSettings.GetEngineIdForCar(m_carId));
			foreach (CarProfileController controller in m_playerCarProfile.dynoInfo.controllers)
			{
				if (controller == null || controller.changers.Count < 1 || !profileInfoPreview.controllers.TryGetValue(controller.name, out var value))
				{
					continue;
				}
				string text = controller.name;
				if (!text.StartsWith("gearbox"))
				{
					switch (text)
					{
					case "turbo_charged":
					case "turbo_pressure":
					case "rev_limiter":
					case "max_torque":
					case "max_torque_rpm":
					case "final_drive":
						break;
					default:
						continue;
					}
				}
				engineSwapData.AddController(text, value);
			}
			return engineSwapData;
		}

		public static EngineSwapData GetCachedSwap()
		{
			return m_cachedSwap;
		}

		private static void ResetCachedSwap()
		{
			m_cachedSwap = null;
		}

		private void ClearSetupsList()
		{
			m_setupSharingList.Clear();
			ClearNewParams();
			UpdateUILinesFromData();
		}

		private void FillCustomSetupsList()
		{
			List<CustomProfile> customProfilesByCarId = m_prefs.carSettings.GetCustomProfilesByCarId(m_carId);
			if (customProfilesByCarId == null)
			{
				return;
			}
			foreach (CustomProfile item2 in customProfilesByCarId)
			{
				if (item2 != null)
				{
					PlayerCarProfile byId = m_db.GetById<PlayerCarProfile>(item2.profile.id);
					if (byId != null)
					{
						SetupSharingListElementData item = new SetupSharingListElementData(item2, byId.name);
						m_setupSharingList.Add(item);
					}
				}
			}
			UpdateUILinesFromData();
		}

		private int GetAvailableCtrlCategoriesCount()
		{
			m_availableControllerTypes.Clear();
			Array values = Enum.GetValues(typeof(CarProfileControllerType));
			for (int i = 1; i < values.Length; i++)
			{
				CarProfileControllerType controllerType = (CarProfileControllerType)i;
				if (IsCategoryEnabled(m_playerCarProfile.dynoInfo, controllerType))
				{
					m_availableControllerTypes.Add((CarProfileControllerType)i);
				}
			}
			if (m_availableControllerTypes.Count == 0)
			{
				return 0;
			}
			m_currentCtrlCategory = 0;
			SetCtrlSwitcherValues(m_availableControllerTypes[m_currentCtrlCategory]);
			return m_availableControllerTypes.Count;
		}

		public void OnSelectNextControllerCategory()
		{
			m_currentCtrlCategory++;
			if (m_currentCtrlCategory > m_availableControllerTypes.Count - 1)
			{
				m_currentCtrlCategory = 0;
			}
			OnInflateParamViewList();
		}

		public void OnSelectPrevControllerCategory()
		{
			m_currentCtrlCategory--;
			if (m_currentCtrlCategory < 0)
			{
				m_currentCtrlCategory = m_availableControllerTypes.Count - 1;
			}
			OnInflateParamViewList();
		}

		public void OnInflateParamViewList(int ctrlCategory)
		{
			m_currentCtrlCategory = ctrlCategory;
			OnInflateParamViewList();
		}

		private void OnInflateParamViewList()
		{
			CarProfileControllerType carProfileControllerType = m_availableControllerTypes[m_currentCtrlCategory];
			InflateParamsViewList_Current(carProfileControllerType);
			SetCtrlSwitcherValues(carProfileControllerType);
			RequestProfileDataIfNeed(SetNewParams);
		}

		private void SetCtrlSwitcherValues(CarProfileControllerType carProfileCtrlType)
		{
			m_controllerTypeCaption.value = LocalizationService.Localize(m_headers[(int)(carProfileCtrlType - 1)]);
			m_showCategoryIndicator.value = m_availableControllerTypes.IndexOf(carProfileCtrlType);
		}

		private void InitParamsListViewItems()
		{
			int count = m_playerCarProfile.dynoInfo.controllers.Count;
			Array values = Enum.GetValues(typeof(CarProfileControllerType));
			m_viewParamsCurrentDict.Clear();
			DiffType currentDiffType = GetCurrentDiffType(m_playerCarProfile, profileInfoPreview);
			for (int i = 1; i < values.Length; i++)
			{
				CarProfileControllerType carProfileControllerType = (CarProfileControllerType)i;
				if (!IsCategoryEnabled(m_playerCarProfile.dynoInfo, carProfileControllerType))
				{
					continue;
				}
				List<UISetupSharingParamItem> list = new List<UISetupSharingParamItem>();
				for (int j = 0; j < count; j++)
				{
					CarProfileController carProfileController = m_playerCarProfile.dynoInfo.controllers[j];
					if (carProfileController == null || carProfileController.type != carProfileControllerType)
					{
						continue;
					}
					bool flag = false;
					if (carProfileControllerType == CarProfileControllerType.Engine)
					{
						flag = !SetupEngineController(carProfileController);
					}
					if (!flag)
					{
						DynoParamsItemViewModel dynoParamsItemViewModel = new DynoParamsItemViewModel(this, carProfileController, j, carProfileController.sort);
						if (!SkipAddOrRemoveDiffControllers(dynoParamsItemViewModel, currentDiffType, carProfileControllerType))
						{
							UISetupSharingParamItem component = UnityEngine.Object.Instantiate(m_paramsViewItemPrefab, m_paramsListContainer).GetComponent<UISetupSharingParamItem>();
							component.SetItem(carProfileController.name, dynoParamsItemViewModel, m_car3dView.currentCar, GetOverrideValue);
							component.gameObject.SetActive(value: false);
							list.Add(component);
						}
					}
				}
				m_viewParamsCurrentDict[carProfileControllerType] = list;
			}
			static string GetOverrideValue(string ctrlName)
			{
				return ctrlName switch
				{
					"front_toe" => m_currFrontToe, 
					"rear_toe" => m_currRearToe, 
					"front_camber" => m_currFrontCamber, 
					"rear_camber" => m_currRearCamber, 
					_ => null, 
				};
			}
		}

		private bool SkipAddOrRemoveDiffControllers(DynoParamsItemViewModel item, DiffType diffType, CarProfileControllerType controllerType)
		{
			if (controllerType != CarProfileControllerType.Transmission || item?.controller == null)
			{
				return false;
			}
			bool flag = diffType == DiffType.Welded || diffType == DiffType.Open;
			bool flag2 = diffType == DiffType.ClutchType;
			bool flag3 = diffType == DiffType.Viscous;
			bool result = false;
			switch (item.controller.name)
			{
			case "diff_lock_coef":
				result = !flag3 || flag;
				break;
			case "diff_preload":
				result = !flag2 || flag;
				break;
			case "diff_lock_power":
				result = !flag2 || flag;
				break;
			case "diff_lock_coast":
				result = !flag2 || flag;
				break;
			}
			return result;
		}

		private DiffType GetCurrentDiffType(PlayerCarProfile playerCarProfile, ProfilesInfo.Profile profile)
		{
			foreach (CarProfileController controller in playerCarProfile.dynoInfo.controllers)
			{
				if (controller.name != "diff_type")
				{
					continue;
				}
				if (!profile.controllers.TryGetValue(controller.name, out var value))
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
						CarProfileParameterMeta byId = m_db.GetById<CarProfileParameterMeta>(changer.parameterId);
						if (byId != null && byId.type == CarProfileParameterType.DiffType)
						{
							return (DiffType)Mathf.RoundToInt(changer.EvaluateOriginalValue(value));
						}
					}
				}
			}
			return DiffType.Viscous;
		}

		private void InflateParamsViewList_Current(CarProfileControllerType controllerType)
		{
			for (int i = 0; i < m_paramsListContainer.childCount; i++)
			{
				m_paramsListContainer.GetChild(i).gameObject.SetActive(value: false);
			}
			if (m_viewParamsCurrentDict.TryGetValue(controllerType, out var value))
			{
				m_paramsScrollRect.normalizedPosition = Vector3.up;
				m_sharingParamsItems.Clear();
				value.ForEach(delegate(UISetupSharingParamItem item)
				{
					item.gameObject.SetActive(value: true);
				});
				m_sharingParamsItems.AddRange(value);
			}
		}

		public void SwitchTab(bool next)
		{
			int currSetupsListType = (int)m_currSetupsListType;
			int num = ((!next) ? (--currSetupsListType) : (++currSetupsListType));
			Array values = Enum.GetValues(typeof(SetupsListType));
			int length = values.Length;
			if (num > length - 1)
			{
				num = 0;
			}
			if (num < 0)
			{
				num = length - 1;
			}
			m_currSetupsListType = (SetupsListType)values.GetValue(num);
			SetSetupsTypeVal();
			if (m_filterChanged)
			{
				ResetRequestDataTimer();
				m_filterChanged = false;
			}
			ProcessSetupsListTypeView();
		}

		private void ProcessSetupsListTypeView()
		{
			ClearSetupsList();
			switch (m_currSetupsListType)
			{
			case SetupsListType.GeneralSetups:
				FetchSetupsShortListData();
				break;
			case SetupsListType.OfficialSetups:
				ShowOfficialSetups();
				break;
			case SetupsListType.MyUploadedSetups:
				FetchSetupsFullListData();
				break;
			}
			TimeController.AddCallback(0f, UpdateBottomPanel);
		}

		private void SetSetupsTypeVal()
		{
			m_setupsType.value = GetSetupsTypeVal();
			string GetSetupsTypeVal()
			{
				return m_currSetupsListType switch
				{
					SetupsListType.GeneralSetups => LocalizationKeys.car_catalog_category_all.Localized(), 
					SetupsListType.OfficialSetups => LocalizationKeys.official_setups.Localized(), 
					SetupsListType.MyUploadedSetups => LocalizationKeys.my_uploaded_setups.Localized(), 
					_ => throw new NotImplementedException(), 
				};
			}
		}

		private void ClearNewParams()
		{
			m_sharingParamsItems.ForEach(delegate(UISetupSharingParamItem item)
			{
				item.ClearNewVal();
			});
		}

		private void RequestProfileDataIfNeed(System.Action canProceed)
		{
			if (m_currentFocusedSetup == null)
			{
				canProceed.SafeCall();
			}
			else if (m_currentFocusedSetup.profileData == null)
			{
				UILockInputContext.EnableLock(showBgAtStart: false);
				MakeSingleSetupRequest(m_currentFocusedSetup.code, delegate(SdrRestSetupSingle response)
				{
					UILockInputContext.DisableLock();
					if (response == null)
					{
						canProceed.SafeCall();
					}
					else
					{
						SetupSharingListElementData setupSharingListElementData = m_setupSharingList.Find((SetupSharingListElementData el) => el.customSetupId == m_currentFocusedSetup.customSetupId);
						if (setupSharingListElementData != null)
						{
							setupSharingListElementData.SetProfileData(response.setup);
							m_currentFocusedSetup.SetData(setupSharingListElementData);
						}
						canProceed.SafeCall();
					}
				});
			}
			else
			{
				canProceed.SafeCall();
			}
		}

		private void SetNewParams()
		{
			if (m_currentFocusedSetup == null)
			{
				return;
			}
			ProfilesInfo.Profile profileData = m_currentFocusedSetup.profileData;
			if (profileData == null || m_setupSharingList.Count == 0)
			{
				ClearNewParams();
				return;
			}
			int id = profileData.id;
			PlayerCarProfile byId = m_db.GetById<PlayerCarProfile>(id);
			if (byId == null)
			{
				return;
			}
			PlayerCarProfile playerCarProfile = byId.Copy();
			DiffType currentDiffType = GetCurrentDiffType(playerCarProfile, profileData);
			bool flag = true;
			foreach (UISetupSharingParamItem sharingUiParam in m_sharingParamsItems)
			{
				CarProfileController carProfileController = playerCarProfile.dynoInfo.controllers.Find((CarProfileController c) => c.name == sharingUiParam.ctrlName);
				if (carProfileController == null)
				{
					sharingUiParam.SetNewParam("-", string.Empty);
					continue;
				}
				if (!IsCategoryEnabled(playerCarProfile.dynoInfo, carProfileController.type))
				{
					sharingUiParam.SetNewParam("-", string.Empty);
					continue;
				}
				bool flag2 = false;
				if (carProfileController.type == CarProfileControllerType.Engine)
				{
					flag2 = !SetupEngineController(carProfileController, m_currentFocusedSetup.engineId);
				}
				if (flag2)
				{
					sharingUiParam.SetNewParam("-", string.Empty);
					continue;
				}
				if (SkipAddOrRemoveDiffControllers(sharingUiParam.dynoViewModel, currentDiffType, carProfileController.type))
				{
					sharingUiParam.SetNewParam("-", string.Empty);
					continue;
				}
				string currVal = sharingUiParam.currVal;
				if (profileData.controllers.TryGetValue(sharingUiParam.ctrlName, out var value))
				{
					currVal = sharingUiParam.dynoViewModel.GetControllerOutputValue(carProfileController, forceTrimOutput: false, value);
				}
				else
				{
					if (flag && !m_deletingCustom)
					{
						RefreshProfilePreview(profileData);
						flag = false;
					}
					currVal = sharingUiParam.dynoViewModel.GetControllerOutputValue(carProfileController);
				}
				sharingUiParam.SetNewParam(currVal, sharingUiParam.units);
			}
		}

		private void MakeSingleSetupRequest(string code, Action<SdrRestSetupSingle> onDone)
		{
			m_sharingService.MakeRequestOfType(code, onDone);
		}

		private bool SetupEngineController(CarProfileController controller, int engineId = -1)
		{
			if (controller == null)
			{
				return true;
			}
			if (m_car3dView.currentCar.swap.isStockEngine)
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
			EngineInfo engineInfo = ((engineId > 0) ? m_swapsAsset.Find(engineId) : m_car3dView.currentCar.swap.meta);
			bool flag = !Mathf.Approximately(engineInfo.turboCharged.value, 0f);
			string text = controller.name;
			if (text == engineInfo.turboCharged.name)
			{
				if (!flag)
				{
					return false;
				}
				SetupController(controller, engineInfo.turboCharged);
			}
			else if (text == engineInfo.turboPressure.name)
			{
				if (!flag)
				{
					return false;
				}
				SetupController(controller, engineInfo.turboPressure);
			}
			else if (text == engineInfo.revLimiter.name)
			{
				SetupController(controller, engineInfo.revLimiter);
			}
			else if (text == engineInfo.maxTorque.name)
			{
				SetupController(controller, engineInfo.maxTorque, shiftMode: true);
			}
			else if (text == engineInfo.maxTorqueRpm.name)
			{
				SetupController(controller, engineInfo.maxTorqueRpm);
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

		private bool IsCategoryEnabled(PlayerCarProfile.DynoInfo dynoInfo, CarProfileControllerType controllerType)
		{
			return dynoInfo.controllers.Exists((CarProfileController x) => x.type == controllerType && x.enabled);
		}

		private bool ShouldRequestData(SetupsListType setupsListType)
		{
			bool flag = true;
			switch (setupsListType)
			{
			case SetupsListType.GeneralSetups:
				flag = IsEmptyCollection(m_setupSharingListCache);
				break;
			case SetupsListType.OfficialSetups:
				flag = IsEmptyCollection(m_setupSharingListOfficialCache);
				break;
			case SetupsListType.MyUploadedSetups:
				flag = IsEmptyCollection(m_setupSharingListMyUploadedCache);
				break;
			}
			flag |= Time.unscaledTime - m_requestDataTimer > 60f;
			m_requestDataTimer = Time.unscaledTime;
			return flag;
			static bool IsEmptyCollection(List<SetupSharingListElementData> collection)
			{
				return collection.Count == 0;
			}
		}

		private void UploadSetupToRemote()
		{
			if (m_currentFocusedSetup == null)
			{
				return;
			}
			UIMessageBox.ShowDeviceTypeListQuestion(LocalizationKeys.publish_setup_question.Localized(), LocalizationKeys.input_settings_device_column_type.Localized() + ":", m_allowDeviceTypesForPublish, null, LocalizationKeys.Cancel.Localized(), delegate(GameInput.DeviceType selectedDeviceType)
			{
				CustomProfile customProfile = m_currentFocusedSetup.customProfile;
				PlayerCarProfile byId = m_db.GetById<PlayerCarProfile>(m_currentFocusedSetup.profileId);
				UILockInputContext.EnableLock();
				m_sharingService.MakeRequestOfType((customProfile, byId, selectedDeviceType), delegate(SdrRestSetupPublic response)
				{
					UILockInputContext.DisableLock();
					if (response != null)
					{
						ResetRequestDataTimer();
						GUICommonGodVoice.ShowText(LocalizationKeys.publish_setup_success.LocalizedFormat(m_currentFocusedSetup.elementName), 4f);
					}
				});
			}, LocalizationKeys.Ok.Localized());
		}

		private void ResetRequestDataTimer()
		{
			m_requestDataTimer = float.NegativeInfinity;
		}

		private bool SkipSetup(string profile, int profileId, int physicsVersion, int engineId, int platform, string playerId)
		{
			IGameOverlay instance = Overlay.instance;
			bool profileLockState = m_prefs.locks.GetProfileLockState(profileId);
			int lockLevelForFeature = m_prefs.globals.GetLockLevelForFeature(AsGameFeature());
			bool num = m_prefs.playerProfile.level < lockLevelForFeature;
			bool flag = Simulator.version != physicsVersion;
			bool flag2 = IsNeedSkipSwap(engineId);
			bool flag3 = false;
			if (instance.IsCrossPlayRestricted)
			{
				flag3 = platform != (int)instance.GetPlatform();
			}
			bool flag4 = false;
			PlayerId.TryParse(playerId, out var playerId2);
			if (instance.IsUserGeneratedContentRestricted && instance.IsFriendShowUGCAllowed)
			{
				flag4 = !instance.IsFriend(playerId2);
			}
			bool flag5 = instance.IsBlockedUser(playerId2);
			return num || flag || profileLockState || flag2 || flag3 || flag4 || flag5;
			GameGlobalFeature AsGameFeature()
			{
				return profile switch
				{
					"Drift" => GameGlobalFeature.SetupDrift, 
					"Racing" => GameGlobalFeature.SetupRace, 
					"Ultimate" => GameGlobalFeature.SetupUltimate, 
					_ => GameGlobalFeature.Unknown, 
				};
			}
			bool IsNeedSkipSwap(int eid)
			{
				if (eid == 0)
				{
					return false;
				}
				EngineInfo engineInfo = m_swapsAsset.Find(eid);
				int num2 = m_prefs.inventory.GetItem(engineInfo.partItem)?.count ?? 0;
				string id = $"{m_carId}_{engineInfo.itemId.ToLower()}";
				InventoryItem item = m_prefs.inventory.GetItem(id);
				bool flag6 = item != null && item.count > 0;
				bool flag7 = num2 < engineInfo.partsToInstall;
				if (!GameManager.SwapsDLCInstalled)
				{
					if (flag6)
					{
						return !flag6 && flag7;
					}
					return true;
				}
				return false;
			}
		}

		private void FetchSetupsShortListData()
		{
			SetupsListType setupsListType = SetupsListType.GeneralSetups;
			if (!ShouldRequestData(setupsListType))
			{
				FillSetupsListFromCacheAndUpdate(setupsListType);
				return;
			}
			UILockInputContext.EnableLock();
			m_sharingService.MakeRequestOfType(m_sharingFilterController.sharingFilter, delegate(SdrRestSetupLobby response)
			{
				UILockInputContext.DisableLock();
				if (response != null)
				{
					m_setupSharingList.Clear();
					foreach (SdrSetupShort setup in response.setups)
					{
						if (setup != null && !SkipSetup(setup.profile, setup.profileId, setup.physicsVersion, setup.engineId, setup.crossMask, setup.createPlayerId))
						{
							SetupSharingListElementData item = new SetupSharingListElementData(setup);
							m_setupSharingList.Add(item);
						}
					}
					m_setupSharingListCache.Clear();
					m_setupSharingListCache.AddRange(m_setupSharingList);
					RemoveReportedSetups();
					UpdateUILinesFromData();
				}
			});
		}

		private void ShowOfficialSetups()
		{
			SetupsListType setupsListType = SetupsListType.OfficialSetups;
			if (!ShouldRequestData(setupsListType))
			{
				FillSetupsListFromCacheAndUpdate(setupsListType);
				return;
			}
			UILockInputContext.EnableLock();
			m_sharingService.MakeRequestOfType(m_sharingFilterController.sharingFilter, delegate(SdrRestSetupLobby response)
			{
				UILockInputContext.DisableLock();
				if (response != null)
				{
					m_setupSharingList.Clear();
					foreach (SdrSetupShort setup in response.setups)
					{
						if (setup != null && !SkipSetup(setup.profile, setup.profileId, setup.physicsVersion, setup.engineId, setup.crossMask, setup.createPlayerId) && setup.official)
						{
							SetupSharingListElementData item = new SetupSharingListElementData(setup);
							m_setupSharingList.Add(item);
						}
					}
					m_setupSharingListOfficialCache.Clear();
					m_setupSharingListOfficialCache.AddRange(m_setupSharingList);
					RemoveReportedSetups();
					UpdateUILinesFromData();
				}
			});
		}

		private void FetchSetupsFullListData()
		{
			SetupsListType setupsListType = SetupsListType.MyUploadedSetups;
			if (!ShouldRequestData(setupsListType))
			{
				FillSetupsListFromCacheAndUpdate(setupsListType);
				return;
			}
			UILockInputContext.EnableLock();
			m_sharingService.MakeRequestOfType(m_carId, delegate(SdrRestSetupList response)
			{
				UILockInputContext.DisableLock();
				if (response != null)
				{
					FillSetupsSharingList(response.setups);
					m_setupSharingListMyUploadedCache.AddRange(m_setupSharingList);
				}
			});
		}

		private void FillSetupsListFromCacheAndUpdate(SetupsListType setupsListType)
		{
			List<SetupSharingListElementData> list = null;
			switch (setupsListType)
			{
			case SetupsListType.GeneralSetups:
				list = m_setupSharingListCache;
				break;
			case SetupsListType.OfficialSetups:
				list = m_setupSharingListOfficialCache;
				break;
			case SetupsListType.MyUploadedSetups:
				list = m_setupSharingListMyUploadedCache;
				break;
			}
			if (list != null)
			{
				m_setupSharingList.Clear();
				m_setupSharingList.AddRange(list);
				RemoveReportedSetups();
				UpdateUILinesFromData();
			}
		}

		private void FillSetupsSharingList(List<SdrSetupFull> fullSetups, string nickName = null)
		{
			m_setupSharingList.Clear();
			foreach (SdrSetupFull fullSetup in fullSetups)
			{
				SetupSharingListElementData setupSharingListElementData = new SetupSharingListElementData(fullSetup);
				if (!string.IsNullOrEmpty(nickName))
				{
					setupSharingListElementData.SetNickname(nickName);
				}
				if (PlayerCarProfileMetaExtensions.GetProfileById(setupSharingListElementData.profileId) != null)
				{
					m_setupSharingList.Add(setupSharingListElementData);
				}
			}
			UpdateUILinesFromData();
		}

		private void UpdateUILinesFromData()
		{
			string currentFocusedSetupSharingCode = GetCurrentFocusedSetupSharingCode();
			GOPool.Allocate(m_setupSharingLineTemplate, ref m_setupSharingLinesPool, m_setupSharingList.Count, delegate(SetupSharingListElement uiElement)
			{
				uiElement.SetContext(this);
			}, OnSetupUIDeactivated);
			for (int num = 0; num < m_setupSharingList.Count; num++)
			{
				UpdateUISetupSharingInfo(m_setupSharingList[num], m_setupSharingLinesPool[num]);
			}
			SetFocus(currentFocusedSetupSharingCode);
			m_showEmptyListMessage.value = m_setupSharingList.Count == 0;
			m_showPublicationBlock.value = !m_showEmptyListMessage.value && m_currTabMode != SharingMode.LocalCustomSetups;
		}

		private void SetFocus(string customSetupId)
		{
			if (string.IsNullOrEmpty(customSetupId))
			{
				SetFocusToFirst();
				return;
			}
			SetupSharingListElementData setupSharingListElementData = FindSetupSharingData(customSetupId);
			if (setupSharingListElementData == null)
			{
				SetFocusToFirst();
				return;
			}
			FlushCoroutineIfNeed(m_setFocusUICoroutine);
			m_setFocusUICoroutine = TimeController.RunCoroutine(SetFocusUI(FindSetupUI(setupSharingListElementData)));
		}

		private IEnumerator SetFocusUI(SetupSharingListElement sharingElement, System.Action onDone = null)
		{
			if (m_currentFocusedSetup != null)
			{
				m_currentFocusedSetup.SetFocus(isFocused: false);
				m_currentFocusedSetup = null;
			}
			if (sharingElement == null)
			{
				onDone.SafeCall();
				yield break;
			}
			m_currentFocusedSetup = sharingElement;
			m_currentFocusedSetup.SetFocus(isFocused: true);
			bool requesting = true;
			RequestProfileDataIfNeed(delegate
			{
				requesting = false;
			});
			while (requesting)
			{
				yield return null;
			}
			if (sharingElement.profileData == null)
			{
				onDone.SafeCall();
				yield break;
			}
			yield return RefreshSpecs(sharingElement);
			SetNewParams();
			yield return EnsureVisibleCurrentFocus();
			RefreshPublicationInfo(sharingElement);
			UpdateBottomPanel();
			m_setFocusUICoroutine = null;
			onDone.SafeCall();
		}

		private void SetFocusToFirst()
		{
			FlushCoroutineIfNeed(m_setFirstFocusCoroutine);
			m_setFirstFocusCoroutine = TimeController.RunCoroutine(SetFocusToFirstAsync());
		}

		private IEnumerator SetFocusToFirstAsync()
		{
			if (m_setupSharingList.Count == 0)
			{
				m_setFirstFocusCoroutine = null;
				yield break;
			}
			FlushCoroutineIfNeed(m_setFocusUICoroutine);
			m_setFocusUICoroutine = SetFocusUI(m_setupSharingLinesPool[0]);
			yield return m_setFocusUICoroutine;
			yield return EnsureVisibleCurrentFocus();
			m_setFirstFocusCoroutine = null;
		}

		private SetupSharingListElement FindSetupUI(SetupSharingListElementData item)
		{
			if (item != null)
			{
				foreach (SetupSharingListElement item2 in m_setupSharingLinesPool)
				{
					if (item2.gameObject.activeInHierarchy && item2.customSetupId == item.customSetupId)
					{
						return item2;
					}
				}
			}
			return null;
		}

		private SetupSharingListElementData FindSetupSharingData(string customSetupId)
		{
			return m_setupSharingList.Find((SetupSharingListElementData item) => item.customSetupId == customSetupId);
		}

		private string GetCurrentFocusedSetupSharingCode()
		{
			if (m_currTabMode == SharingMode.LocalCustomSetups)
			{
				return m_prefs.carSettings.GetInstalledCustomProfile(m_carId)?.id;
			}
			if (m_currentFocusedSetup == null)
			{
				return string.Empty;
			}
			if (!string.IsNullOrEmpty(m_lastCustomSetupId))
			{
				return m_lastCustomSetupId;
			}
			return m_currentFocusedSetup.customSetupId;
		}

		private void UpdateUISetupSharingInfo(SetupSharingListElementData setupSharingData, SetupSharingListElement setupLine)
		{
			List<CustomProfile> customProfilesByCarId = m_prefs.carSettings.GetCustomProfilesByCarId(m_carId);
			setupSharingData.UpdateLocalCustomSetupStatus(customProfilesByCarId.Exists((CustomProfile cp) => cp.id == setupSharingData.customSetupId));
			setupLine.UpdateFrom(setupSharingData);
		}

		private void OnSetupUIDeactivated(SetupSharingListElement setupsUILine)
		{
			if (!(setupsUILine == null) && !(setupsUILine.gameObject == null))
			{
				setupsUILine.gameObject.SetActive(value: false);
				setupsUILine = null;
			}
		}

		private IEnumerator ResetProfile()
		{
			RaceCar currentCar = m_car3dView.currentCar;
			currentCar.profileMetaInfo = m_playerCarProfile;
			currentCar.InitDesc(m_playerCarProfile.asset);
			string customSetupId = m_prefs.carSettings.GetInstalledCustomProfile(m_carId)?.id;
			int engineIdForCar = m_prefs.carSettings.GetEngineIdForCar(m_carId, customSetupId);
			yield return currentCar.swap.Refresh(engineIdForCar, force: true);
		}

		private void GoToSharingMenu()
		{
			TimeController.AddCallback(1f, m_car3dView.IconGenerate);
			ClearTestDriveParams();
			m_currentFocusedSetup = null;
			m_lastCustomSetupId = null;
			ResetCachedSwap();
			TimeController.StartNow(ResetProfile());
			CallSharingMenuContext();
			int profileIdForCar = m_prefs.carSettings.GetProfileIdForCar(m_carId);
			m_car3dView.ApplyProfile(profileIdForCar);
			m_car3dView.ApplyDefaultCustomization();
			m_car3dView.currentCar.AlignCarPositionAtSpawn(adjustRotation: false);
		}

		private void CallSharingMenuContext()
		{
			base.owner.HideAllExcept<UIMainMenuContext>();
		}

		private void FixedUpdate()
		{
			InputManager inputManager = InputManager.instance;
			if (!SkipFixedUpdate())
			{
				ActionType type = ActionType.NavUp;
				ActionType type2 = ActionType.NavDown;
				if (inputManager.IsActionPressed(type) || inputManager.IsActionHold(type))
				{
					ProcessNavigationUp();
				}
				else if (inputManager.IsActionPressed(type2) || inputManager.IsActionHold(type2))
				{
					ProcessNavigationDown();
				}
			}
			bool SkipFixedUpdate()
			{
				if (!(inputManager == null) && !m_activeActionOptionsContext && !m_activeRateContext && !m_activeFilterContext && !m_activeReportContext && !m_activeDialogContext && !m_activeConsoleContext)
				{
					return UILockInputContext.IsActive();
				}
				return true;
			}
		}

		private void ProcessNavigationUp()
		{
			switch (m_selectedViewBlock)
			{
			case SelectedViewBlock.SetupsView:
				MoveFocus(-1);
				break;
			case SelectedViewBlock.ParamsView:
				MoveParamsScroll(m_paramsScrollRect, down: false, 10f);
				break;
			}
		}

		private void ProcessNavigationDown()
		{
			switch (m_selectedViewBlock)
			{
			case SelectedViewBlock.SetupsView:
				MoveFocus(1);
				break;
			case SelectedViewBlock.ParamsView:
				MoveParamsScroll(m_paramsScrollRect, down: true, 10f);
				break;
			}
		}

		private void MoveParamsScroll(ScrollRect scrollRect, bool down, float step)
		{
			Vector3 vector = (down ? Vector3.up : Vector3.down);
			float y = scrollRect.normalizedPosition.y;
			float height = scrollRect.content.rect.height;
			float height2 = scrollRect.viewport.rect.height;
			if ((down || !(y > 1f)) && (!down || !(y < 0f)) && !(height < height2))
			{
				scrollRect.content.transform.DOLocalMoveY((scrollRect.content.localPosition + vector * step).y, Time.deltaTime);
				scrollRect.content.transform.DOPlay();
			}
		}

		[GameHotkey(ActionType.NavLeft, false, false)]
		public void NavigationLeft()
		{
			SetSelectedViewBlock(SelectedViewBlock.SetupsView);
		}

		[GameHotkey(ActionType.NavRight, false, false)]
		public void NavigationRIght()
		{
			if (m_showParamsBlock.value)
			{
				SetSelectedViewBlock(SelectedViewBlock.ParamsView);
			}
		}

		private void SetSelectedViewBlock(SelectedViewBlock viewBlock)
		{
			m_selectedViewBlock = viewBlock;
			m_selectedSetupsViewBlock.value = m_selectedViewBlock == SelectedViewBlock.SetupsView;
		}

		public void SetParamsSelected()
		{
			SetSelectedViewBlock(SelectedViewBlock.ParamsView);
		}

		public void SetSetupsSelected()
		{
			SetSelectedViewBlock(SelectedViewBlock.SetupsView);
		}

		private bool SkipFocusing()
		{
			if (!(Time.unscaledTime - m_time < 0.15f))
			{
				return m_focusCoroutine != null;
			}
			return true;
		}

		private void MoveFocus(int next)
		{
			if (!SkipFocusing())
			{
				m_time = Time.unscaledTime;
				m_focusCoroutine = TimeController.StartNow(MoveFocusAsync(next));
			}
			IEnumerator MoveFocusAsync(int num2)
			{
				if (m_currentFocusedSetup == null)
				{
					FlushCoroutineIfNeed(m_setFirstFocusCoroutine);
					m_setFirstFocusCoroutine = SetFocusToFirstAsync();
					yield return m_setFirstFocusCoroutine;
					m_focusCoroutine = null;
				}
				else
				{
					int num = m_setupSharingLinesPool.IndexOf(m_currentFocusedSetup);
					if (num == -1)
					{
						m_focusCoroutine = null;
					}
					else
					{
						num += num2;
						if (num >= 0 && num < m_setupSharingLinesPool.Count)
						{
							SetupSharingListElement setupSharingListElement = m_setupSharingLinesPool[num];
							if (setupSharingListElement != null && setupSharingListElement.gameObject.activeInHierarchy)
							{
								FlushCoroutineIfNeed(m_setFocusUICoroutine);
								m_setFocusUICoroutine = SetFocusUI(setupSharingListElement);
								yield return m_setFocusUICoroutine;
								yield return EnsureVisibleCurrentFocus();
							}
						}
						yield return null;
						m_focusCoroutine = null;
					}
				}
			}
		}

		public void OnClickSharingElement(SetupSharingListElement uiItem)
		{
			if (!(m_currentFocusedSetup == uiItem) && !SkipFocusing())
			{
				m_time = Time.unscaledTime;
				InputManager.PlayClickSound();
				FlushCoroutineIfNeed(m_setFocusUICoroutine);
				m_setFocusUICoroutine = TimeController.RunCoroutine(SetFocusUI(uiItem));
				SetSelectedViewBlock(SelectedViewBlock.SetupsView);
			}
		}

		private IEnumerator InitSpecs(CustomProfile customProfile)
		{
			CarProfileCache carProfileCache = CarProfileCacheManager.GetCarProfileCache(m_carFactory, m_playerCarProfile);
			CarProfileProxy profileProxy = new CarProfileProxy(m_playerCarProfile, carProfileCache);
			yield return m_carSpecsTable.InitProfileInfo(m_car3dView, null, profileProxy, m_db, m_car3dView.currentCar, customProfile?.id, customProfile != null);
		}

		private void RefreshPublicationInfo(SetupSharingListElement item)
		{
			bool isFilterCrossplatformEnabled = m_sharingFilterController.GetFilterValue<SetupSharingFilterType, int>(SetupSharingFilterType.CrossMask) == 0;
			m_uiSetupsSharingPublication.SetData(item, isFilterCrossplatformEnabled);
		}

		private IEnumerator RefreshSpecs(SetupSharingListElement item)
		{
			while (m_initSpecsAtStartCoroutine != null)
			{
				yield return null;
			}
			RaceCar car = m_car3dView.currentCar;
			CarSwap swap = car.swap;
			ProfilesInfo.Profile profileData = item.profileData;
			if (profileData != null)
			{
				int profileId = profileData.id;
				PlayerCarProfileMetaExtensions.FixProfileIdIfNeed(car.carId, ref profileId);
				PlayerCarProfile profileMeta = m_db.GetById<PlayerCarProfile>(profileId).Copy();
				int engineId = item.engineId;
				string customSetupId = item.customSetupId;
				yield return m_waitEndFrame;
				yield return swap.carAttached ? swap.Refresh(engineId, force: true) : swap.Initialize(car, m_prefs, m_db, engineId, force: false, profileMeta);
				CarProfileCache carProfileCache = CarProfileCacheManager.GetCarProfileCache(m_carFactory, profileMeta);
				CarProfileProxy profileProxy = new CarProfileProxy(profileMeta, carProfileCache);
				yield return m_carSpecsTable.InitProfileInfo(m_car3dView, null, profileProxy, m_db, car, customSetupId, skipSwap: true, profileData, engineId, updateFromCarSpecsTable: false);
			}
		}

		private IEnumerator EnsureVisibleCurrentFocus()
		{
			if (m_currentFocusedSetup == null)
			{
				yield break;
			}
			RectTransform focusedRect = m_currentFocusedSetup.rectTransform;
			if (!(focusedRect == null))
			{
				while (!m_setupsScrollRect.IsScrollItemVerticalyVisible(focusedRect))
				{
					m_setupsScrollRect.SnapScrollTo(focusedRect);
					yield return null;
				}
			}
		}
	}
}
