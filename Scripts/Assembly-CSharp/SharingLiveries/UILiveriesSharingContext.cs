using System;
using System.Collections;
using System.Collections.Generic;
using Binding;
using Binding.Base;
using CarModelSystem;
using Cysharp.Threading.Tasks;
using DB.Meta;
using DI;
using GameInput;
using GameOverlay;
using SyncMultiplayer;
using UnityEngine;
using UnityEngine.UI;
using VinylSystem;

namespace SharingLiveries
{
	[AudioFXBlocker(false)]
	public class UILiveriesSharingContext : BaseBindingBehaviourTarget
	{
		private enum SharingMode
		{
			None = -1,
			FetchedLiveries = 0,
			LocalLiveries = 1
		}

		private enum LiveriesListType
		{
			GeneralLiveries = 0,
			OfficialLiveries = 1,
			MyUploadedLiveries = 2
		}

		private static HashSet<string> m_reportedLiveries = new HashSet<string>();

		private const float FocusLoadLiveryDelay = 1f;

		private const int RequestDataPeriod = 60;

		private const float ScrollDelay = 0.2f;

		private const string EnabledFilterFormat = "<color=yellow>{0}</color>";

		[SerializeField]
		private LiverySharingListElement m_liverySharingLineTemplate;

		[SerializeField]
		private ScrollRect m_liveriesScrollRect;

		[SerializeField]
		private RectTransform m_liveriesListContainer;

		[SerializeField]
		private UILiveriesSharingPublication m_liverySharingPublication;

		[SerializeField]
		private Vector2Int m_textureSize = new Vector2Int(220, 124);

		[SerializeField]
		private float m_focusDelay = 1f;

		[ContextDependency]
		private GarageCar3dViewContext m_car3dView;

		[Dependency]
		private GamePrefs m_prefs;

		[Dependency]
		private CarVisualManager m_carVisualManager;

		private readonly ListProperty m_presetsList = new ListProperty();

		private readonly BoolProperty m_showLiveryLoadingSpinner = new BoolProperty();

		private readonly BoolProperty m_showEmptyListMessage = new BoolProperty();

		private readonly BoolProperty m_showPublicationBlock = new BoolProperty();

		private readonly BoolProperty m_showNotFullVinylsPack = new BoolProperty();

		private readonly BoolProperty m_showRequestBg = new BoolProperty();

		private readonly BoolProperty m_activeSearchByAuthor = new BoolProperty();

		private readonly BoolProperty m_activeSearchByCode = new BoolProperty();

		private readonly IntProperty m_tabMode = new IntProperty();

		private readonly StringProperty m_liveriesSlotsCount = new StringProperty();

		private readonly StringProperty m_liveriesType = new StringProperty();

		private (float width, float widthExpanded) m_uiResizeWidths = (width: 450f, widthExpanded: 1314f);

		private LiveriesListType m_currLiveriesListType;

		private SharingMode m_currTabMode;

		private int m_carId;

		private int m_lastCarId = -1;

		private Dictionary<Type, bool> m_requestsInProgress = new Dictionary<Type, bool>();

		private Dictionary<string, (long time, Texture tex)> m_cachePreview = new Dictionary<string, (long, Texture)>();

		private Dictionary<string, Texture> m_cacheServerPreview = new Dictionary<string, Texture>();

		private Dictionary<LiveriesListType, string> m_lastFocusedUIDict = new Dictionary<LiveriesListType, string>();

		private List<string> m_categories;

		private List<ActionHintData> m_actionHints = new List<ActionHintData>();

		private List<HeaderActionData> m_headerData = new List<HeaderActionData>();

		private List<LiverySharingListElement> m_liverySharingLinesPool = new List<LiverySharingListElement>(128);

		private List<LiverySharingListElementData> m_liverySharingList = new List<LiverySharingListElementData>(128);

		private List<LiverySharingListElementData> m_liverySharingListCache = new List<LiverySharingListElementData>(128);

		private List<LiverySharingListElementData> m_liverySharingListOfficialCache = new List<LiverySharingListElementData>(32);

		private List<LiverySharingListElementData> m_liverySharingListMyUploadedCache = new List<LiverySharingListElementData>(128);

		private List<SdrLiveryFull> m_publicationCodeCacheList = new List<SdrLiveryFull>(1);

		private VinylsSystemMeta m_vinylProviderMeta;

		private VinylWrapEditor m_vinylsEditor;

		private CarVilylsInfo.VinylPreset m_startPreset;

		private VinylSharingManager m_vinylSharingManager;

		private LiveriesSharingService m_sharingService;

		private SharingFilterController m_sharingFilterController;

		private LiverySharingListElement m_currentFocusedLivery;

		private UIReportContext m_reportContext;

		private Coroutine m_renderPreviewCoroutine;

		private Coroutine m_focusCoroutine;

		private IEnumerator m_setFirstFocusCoroutine;

		private IEnumerator m_setFocusUICoroutine;

		private IEnumerator m_ensureVisibleCurrentFocusCoroutine;

		private readonly WaitForSecondsRealtime m_loadLiveryDelayObject = new WaitForSecondsRealtime(1f);

		private float m_requestDataTimer;

		private float m_time;

		private bool m_needResetLiveryToDefault;

		private bool m_searchByCode;

		private bool m_filterChanged;

		private bool m_activeDialogContext;

		private bool m_activeRateContext;

		private bool m_activeFilterContext;

		private bool m_activeReportContext;

		private bool m_activeConsoleContext;

		private bool m_consoleRestrictionIsOn;

		private bool m_showBottomWithDelay;

		private bool m_isUIListExpanded;

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
			Overlay.instance.richPresence.Show(RichPresenceStatus.CarDesigning);
			ContextOwner contextOwner = base.owner;
			contextOwner.onContextActiveChanged = (Action<BaseContext>)Delegate.Combine(contextOwner.onContextActiveChanged, new Action<BaseContext>(OnContextActiveChangedHandler));
			m_presetsList.OnSelectionChanged += OnSelectionChanged_PresetsList;
			m_presetsList.OnClick += OnClick_PresetsList;
			m_currLiveriesListType = LiveriesListType.GeneralLiveries;
			m_carId = m_prefs.globals.lastCarId;
			m_vinylsEditor = VinylSystemKernel.instance.editor;
			m_vinylProviderMeta = Singleton<VinylMetaProvider>.instance.meta;
			m_startPreset = AcquireCurrentCarVinylPreset();
			if (m_vinylSharingManager == null)
			{
				m_vinylSharingManager = BaseSharingConsole.Create<VinylSharingManager>();
			}
			m_vinylSharingManager?.RegisterConsoleCommands();
			m_showEmptyListMessage.value = m_liverySharingList.Count == 0;
			m_liverySharingLineTemplate.gameObject.SetActive(value: false);
			m_filterChanged = false;
			m_needResetLiveryToDefault = true;
			InitCategories();
			ResetTabMode();
			InitSharingServiceIfNeed();
			InitReportContextIfNeed();
			InitFilters();
			SetLiveriesTypeVal();
			UpdateTopPanel();
			SubscribePS4TextReceive();
			SubscribeSteamDeckTextReceive();
			AnalyticsService.SendLiverySharingEnter(m_carId);
			Overlay.instance.CheckSystemCrossPlatformAbility(delegate(bool restrictedCrossMp)
			{
				if (restrictedCrossMp)
				{
					m_sharingFilterController.SetFilterValue(LiveriesSharingFilterType.CrossMask, (int)Overlay.instance.GetPlatform());
					m_sharingFilterController.SaveFilter<SharingLiveriesFilter>();
					OnApplyFilterHandler();
				}
			});
		}

		protected override async void OnDeactivate()
		{
			if (m_consoleRestrictionIsOn)
			{
				m_consoleRestrictionIsOn = false;
				return;
			}
			m_vinylSharingManager?.UnregisterConsoleCommands();
			UnSubscribePS4TextReceive();
			UnSubscribeSteamDeckTextReceive();
			ContextOwner contextOwner = base.owner;
			contextOwner.onContextActiveChanged = (Action<BaseContext>)Delegate.Remove(contextOwner.onContextActiveChanged, new Action<BaseContext>(OnContextActiveChangedHandler));
			m_presetsList.OnSelectionChanged -= OnSelectionChanged_PresetsList;
			m_presetsList.OnClick -= OnClick_PresetsList;
			m_sharingService.onRequestProcessing = null;
			FlushAllCoroutines();
			if ((m_needResetLiveryToDefault && m_currTabMode == SharingMode.FetchedLiveries) || (m_presetsList.count == 0 && m_currTabMode == SharingMode.LocalLiveries))
			{
				ApplyPreset(m_startPreset);
			}
			ResetActiveCustomSearches();
			ResetPublicationInfo();
			ClearListsCache();
			HideTopPanel();
			HideBottomPanel();
			StopAllCoroutines();
			m_requestsInProgress.Clear();
			UILockInputContext.EnableLock();
			await Resources.UnloadUnusedAssets();
			UILockInputContext.DisableLock();
			base.OnDeactivate();
		}

		private void ResetActiveCustomSearches()
		{
			m_activeSearchByAuthor.value = false;
			m_activeSearchByCode.value = false;
		}

		private CarVilylsInfo.VinylPreset AcquireCurrentCarVinylPreset()
		{
			CarVisualManager.VisualSettings visualForCar = m_prefs.carSettings.GetVisualForCar(m_carId);
			CarVilylsInfo.VinylPreset preset = m_prefs.carSettings.GetVinylsInfoForCar(m_carId).GetPreset(visualForCar.vinylPresetLiveryId);
			if (visualForCar.vinylLayers.Count > 0 && preset != null && preset.layers.Count == 0)
			{
				CopyVinylLayersFromVisualsToPreset(visualForCar, preset);
			}
			return preset;
		}

		private void CopyVinylLayersFromVisualsToPreset(CarVisualManager.VisualSettings visualSettings, CarVilylsInfo.VinylPreset preset)
		{
			preset.layers.AddRange(visualSettings.vinylLayers);
			m_carVisualManager.SaveVisualSettingsForCar(m_carId, visualSettings);
		}

		private void FlushAllCoroutines()
		{
			FlushCoroutineIfNeed(ref m_renderPreviewCoroutine);
			FlushCoroutineIfNeed(ref m_focusCoroutine);
			FlushCoroutineIfNeed(ref m_setFirstFocusCoroutine);
			FlushCoroutineIfNeed(ref m_setFocusUICoroutine);
			FlushCoroutineIfNeed(ref m_ensureVisibleCurrentFocusCoroutine);
			m_liverySharingLinesPool.ForEach(delegate(LiverySharingListElement el)
			{
				el.StopSetDataCoroutine();
			});
		}

		private void ClearListsCache()
		{
			m_liverySharingList.Clear();
			m_liverySharingListCache.Clear();
			m_liverySharingListOfficialCache.Clear();
			m_liverySharingListMyUploadedCache.Clear();
			m_presetsList.Clear();
			foreach (var value in m_cachePreview.Values)
			{
				UnityEngine.Object.Destroy(value.tex);
			}
			m_cachePreview.Clear();
			foreach (Texture value2 in m_cacheServerPreview.Values)
			{
				UnityEngine.Object.Destroy(value2);
			}
			m_cacheServerPreview.Clear();
			m_lastFocusedUIDict.Clear();
		}

		private void ResetPublicationInfo()
		{
			m_liverySharingPublication.Reset();
		}

		private void SubscribePS4TextReceive()
		{
		}

		private void UnSubscribePS4TextReceive()
		{
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

		private void OnContextActiveChangedHandler(BaseContext ctx)
		{
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
			if (ctx is UISharingLiveriesFilterContext uISharingLiveriesFilterContext)
			{
				m_activeFilterContext = uISharingLiveriesFilterContext.isActive;
				if (uISharingLiveriesFilterContext.isActive)
				{
					uISharingLiveriesFilterContext.onApplyFilter = (System.Action)Delegate.Combine(uISharingLiveriesFilterContext.onApplyFilter, new System.Action(OnApplyFilterHandler));
				}
				else
				{
					uISharingLiveriesFilterContext.onApplyFilter = (System.Action)Delegate.Remove(uISharingLiveriesFilterContext.onApplyFilter, new System.Action(OnApplyFilterHandler));
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
					uIReportContext.onSendSharingReport = (Action<string>)Delegate.Combine(uIReportContext.onSendSharingReport, new Action<string>(OnSendReport));
					HideBottomPanel();
				}
				else
				{
					SubscribePS4TextReceive();
					SubscribeSteamDeckTextReceive();
					UpdateBottomPanel();
					uIReportContext.onSendSharingReport = (Action<string>)Delegate.Remove(uIReportContext.onSendSharingReport, new Action<string>(OnSendReport));
				}
			}
			void OnSendReport(string code)
			{
				m_showBottomWithDelay = true;
				m_reportedLiveries.Add(code);
				ResetPublicationInfo();
				RemoveReportedLiveries();
				UpdateUILinesFromData();
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
			string code = string.Empty;
			string elementName = string.Empty;
			switch (m_currTabMode)
			{
			case SharingMode.FetchedLiveries:
				if (m_currentFocusedLivery == null)
				{
					return;
				}
				code = m_currentFocusedLivery.code;
				elementName = m_currentFocusedLivery.elementName;
				break;
			case SharingMode.LocalLiveries:
				if (m_presetsList.GetSelected() is VinylPresetElementData vinylPresetElementData)
				{
					code = vinylPresetElementData.preset.ownerInfo.code;
				}
				break;
			}
			UILockInputContext.EnableLock();
			m_sharingService.MakeRequestOfType((code, rate), delegate(SdrRestLiveryRating response)
			{
				UILockInputContext.DisableLock();
				if (response != null)
				{
					GUICommonGodVoice.ShowText((response.updated ? LocalizationKeys.livery_rate_updated : LocalizationKeys.livery_rate_success).LocalizedFormat(elementName), 4f);
					AnalyticsService.SendLiveryRate(m_carId, code);
				}
			});
		}

		private void OnRateLivery()
		{
			base.owner.Push<UISharingRateContext>().SetTitle(LocalizationKeys.sharing_rate_livery_dialog_title.Localized());
		}

		private void InitSharingServiceIfNeed()
		{
			if (m_sharingService == null)
			{
				m_sharingService = BaseServerSharingService.Create<LiveriesSharingService>();
			}
			LiveriesSharingService sharingService = m_sharingService;
			sharingService.onRequestProcessing = (Action<bool, Type>)Delegate.Combine(sharingService.onRequestProcessing, (Action<bool, Type>)delegate(bool requestInProgress, Type type)
			{
				bool value;
				if (!requestInProgress)
				{
					m_requestsInProgress.Remove(type);
				}
				else if (!m_requestsInProgress.TryGetValue(type, out value))
				{
					m_requestsInProgress[type] = requestInProgress;
				}
				if (type == typeof(SdrRestLiverySingle) && !m_searchByCode)
				{
					m_showRequestBg.value = requestInProgress;
					m_liverySharingPublication.Reset();
				}
			});
		}

		private void InitFilters()
		{
			if (m_sharingFilterController == null)
			{
				m_sharingFilterController = new SharingFilterController(m_prefs);
			}
			m_sharingFilterController.InitFilter<SharingLiveriesFilter>();
		}

		private void InitReportContextIfNeed()
		{
			if (!(m_reportContext != null))
			{
				m_reportContext = base.owner.Preload<UIReportContext>();
			}
		}

		private void UpdateTopPanel()
		{
			m_headerData.Clear();
			m_headerData.Add(new HeaderActionData
			{
				text = LocalizationKeys.server_sharing_liveries.Localized(),
				action = OpenServerLiveries
			});
			m_headerData.Add(new HeaderActionData
			{
				text = LocalizationKeys.my_vinyls.Localized(),
				action = OpenLocalLiveries
			});
			ShowTopPanel(ETopPanelState.Actions, null, m_headerData.ToArray());
			m_topPanelContext.switcherActions.SelectionAction(0);
		}

		private void OnReturnToCommonList()
		{
			m_activeSearchByAuthor.value = false;
			m_activeSearchByCode.value = false;
			SetLiveriesTypeVal();
			ProcessLiveriesListTypeView();
		}

		private async void UpdateBottomPanel()
		{
			await UpdateBottomPanelAsync();
			async UniTask UpdateBottomPanelAsync()
			{
				m_actionHints.Clear();
				if (m_activeSearchByAuthor.value || m_activeSearchByCode.value)
				{
					m_actionHints.Add(new ActionHintData
					{
						hint = LocalizationKeys.Cancel.Localized(),
						type = ActionType.NavCancelBtnB,
						action = OnReturnToCommonList
					});
				}
				else
				{
					m_actionHints.Add(new ActionHintData
					{
						hint = LocalizationKeys.nav_hint_goto_back.Localized(),
						type = ActionType.NavCancelBtnB,
						action = GoToSharingMenu
					});
				}
				switch (m_currTabMode)
				{
				case SharingMode.FetchedLiveries:
					UpdateFetchedLiveriesBottom();
					break;
				case SharingMode.LocalLiveries:
					UpdateLocalLiveriesBottom();
					break;
				}
				await UniTask.Delay(m_showBottomWithDelay ? 300 : 0);
				m_showBottomWithDelay = false;
				await UniTask.WaitWhile(() => base.owner.topContext.GetType() != typeof(UILiveriesSharingContext));
				if (base.isActive)
				{
					ShowBottomPanel(m_actionHints);
				}
			}
		}

		private void UpdateLocalLiveriesBottom()
		{
			if (!(m_presetsList.GetSelected() is VinylPresetElementData vinylPresetElementData))
			{
				return;
			}
			bool flag = vinylPresetElementData.preset.ownerInfo.playerId.Equals(Overlay.instance.player.id);
			bool downloaded = vinylPresetElementData.preset.downloaded;
			if (!vinylPresetElementData.preset.locked)
			{
				if (!downloaded)
				{
					m_actionHints.Add(new ActionHintData
					{
						hint = LocalizationKeys.publish.Localized(),
						type = ActionType.NavSelectBtnA,
						action = UploadToRemote
					});
				}
				else if (!flag)
				{
					m_actionHints.Add(new ActionHintData
					{
						hint = LocalizationKeys.rate.Localized(),
						type = ActionType.NavSelectBtnA,
						action = OnRateLivery
					});
				}
				m_actionHints.Add(new ActionHintData
				{
					hint = LocalizationKeys.nav_hint_remove_slot.Localized(),
					type = ActionType.NavAdditional1BtnY,
					action = RemoveSlot
				});
			}
		}

		private void RemoveSlot()
		{
			int selectedIndex = m_presetsList.GetSelectedIndex();
			if (!(m_presetsList.GetByIndex(selectedIndex) as VinylPresetElementData).isPlus.value)
			{
				UIMessageBox.ShowQuestion(LocalizationKeys.window_caption_remove_slot.Localized(), delegate
				{
				}, LocalizationKeys.Cancel.Localized(), delegate
				{
					RemoveSlot(selectedIndex);
				}, LocalizationKeys.Yes.Localized());
			}
		}

		private void RemoveSlot(int selectedIndex)
		{
			VinylPresetElementData vinylPresetElementData = m_presetsList.GetByIndex(selectedIndex) as VinylPresetElementData;
			m_presetsList.Remove(vinylPresetElementData);
			if (m_presetsList.count == 0)
			{
				m_startPreset = null;
			}
			m_prefs.carSettings.GetVinylsInfoForCar(m_carId).RemovePreset(vinylPresetElementData.preset);
			if (selectedIndex == m_presetsList.count)
			{
				selectedIndex--;
			}
			ClearPreset();
			m_presetsList.Selection(selectedIndex, selected: true);
			if (m_startPreset?.liveryId == vinylPresetElementData.preset.liveryId && m_presetsList.GetByIndex(selectedIndex) is VinylPresetElementData vinylPresetElementData2)
			{
				m_startPreset = vinylPresetElementData2.preset;
			}
			m_liveriesSlotsCount.value = $"{m_presetsList.count} / {10}";
			m_showEmptyListMessage.value = m_presetsList.count == 0;
			UpdateBottomPanel();
		}

		private void ClearPreset()
		{
			CarVisualManager.VisualSettings visualForCar = m_prefs.carSettings.GetVisualForCar(m_carId);
			visualForCar.vinylPresetLiveryId = null;
			visualForCar.vinylPresetId = -1;
			visualForCar.vinylLayers.Clear();
			m_carVisualManager.SaveVisualSettingsForCar(m_carId, visualForCar);
			try
			{
				m_vinylsEditor.StartEditLivery(m_car3dView.GetRaceCar().carModel);
				m_vinylsEditor.RemoveAllLayers();
				m_vinylsEditor.CompleteEditLivery();
			}
			catch (Exception ex)
			{
				GameLog.Print(AlertLevel.Error, "System", ex.ToString());
			}
		}

		private void UploadToRemote()
		{
			VinylPresetElementData localPresetData = m_presetsList.GetSelected() as VinylPresetElementData;
			if (localPresetData == null || localPresetData.preset == null)
			{
				return;
			}
			UnSubscribeSteamDeckTextReceive();
			UIMessageBox.ShowInputCategoryDialog(LocalizationKeys.publish_livery_question.Localized(), string.Empty, m_categories, LocalizationKeys.input_livery_name.Localized(), LocalizationKeys.publish_livery_description.Localized(), InputField.ContentType.Standard, LocalizationKeys.Ok.Localized(), LocalizationKeys.Cancel.Localized(), delegate(string inputText, List<int> indexesList, bool? isPrivatePublication)
			{
				string text = UIUtils.FilterInvalidTags(inputText);
				text = text.Replace("\n", "").Replace("\r", "");
				text = text.Trim();
				if (text.Length > 22)
				{
					text = text.Substring(0, 22);
				}
				SharingLiveryCategoryType sharingLiveryCategoryType = SharingLiveryCategoryType.Any;
				foreach (int indexes in indexesList)
				{
					if (Enum.TryParse<SharingLiveryCategoryType>(m_categories[indexes], out var result))
					{
						sharingLiveryCategoryType |= result;
					}
				}
				SharingLiveryDLCFlags sharingLiveryDLCFlags = SharingLiveryDLCFlags.Any;
				foreach (SharingLiveryDLCFlags value in Enum.GetValues(typeof(SharingLiveryDLCFlags)))
				{
					if (value != SharingLiveryDLCFlags.Any && Enum.TryParse<DLCCode>(value.ToString(), ignoreCase: true, out var result2) && Overlay.instance.IsDLCInstalled(result2))
					{
						sharingLiveryDLCFlags |= value;
					}
				}
				UILockInputContext.EnableLock();
				byte[] item = CustomTypes.SerializeLiveryLayers(localPresetData.preset.layers);
				byte[] rawTextureData = ((Texture2D)localPresetData.m_preview.value).GetRawTextureData();
				(string, int, int, int, string, bool?, byte[], byte[]) tuple = (text, m_carId, (int)sharingLiveryCategoryType, (int)sharingLiveryDLCFlags, localPresetData.preset.liveryId, isPrivatePublication, item, rawTextureData);
				m_sharingService.MakeRequestOfType(tuple, delegate(SdrRestLiveryPublic response)
				{
					UILockInputContext.DisableLock();
					if (response != null)
					{
						AnalyticsService.SendLiveryPublish(m_carId, localPresetData.preset.liveryId);
						ResetRequestDataTimer();
						GUICommonGodVoice.ShowText(LocalizationKeys.publish_livery_success.LocalizedFormat(inputText), 4f);
					}
				});
				SubscribeSteamDeckTextReceive();
			}, SubscribeSteamDeckTextReceive, 22, CheckFuncData);
			bool CheckFuncData(string inputText, List<int> indexes)
			{
				if (!IsInputValid(UIUtils.FilterInvalidTags(inputText)))
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.wrong_name.Localized(), 3f);
					return false;
				}
				SharingLiveryCategoryType sharingLiveryCategoryType = SharingLiveryCategoryType.Any;
				foreach (int index in indexes)
				{
					if (Enum.TryParse<SharingLiveryCategoryType>(m_categories[index], out var result))
					{
						sharingLiveryCategoryType |= result;
					}
				}
				if (sharingLiveryCategoryType == SharingLiveryCategoryType.Any)
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.publish_livery_no_tag_error.Localized(), 3f);
					return false;
				}
				return true;
			}
			static bool IsInputValid(string text)
			{
				if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(text.Trim()))
				{
					return false;
				}
				if (!UIUtils.IsMatchRegularExpression(text, "^[\\p{L}0-9_\\s!?;:№\u02dc~@#$%^ˆ&`*{}\\[\\]()=+\"',/|.-]*$"))
				{
					return false;
				}
				return true;
			}
		}

		private void UpdateFetchedLiveriesBottom()
		{
			TryAddSaveLiveryHintButton();
			if (m_currLiveriesListType != LiveriesListType.MyUploadedLiveries)
			{
				string hint = (m_sharingFilterController.IsActiveFilter() ? $"<color=yellow>{LocalizationKeys.active_filter.Localized()}</color>" : LocalizationKeys.filters.Localized());
				int num;
				if (!m_activeSearchByAuthor.value)
				{
					num = (m_activeSearchByCode.value ? 1 : 0);
					if (num == 0)
					{
						m_actionHints.Add(new ActionHintData
						{
							hint = hint,
							type = ActionType.NavAdditional0BtnX,
							action = OnPressFilter
						});
					}
				}
				else
				{
					num = 1;
				}
				TryAddReportButton();
				if (num == 0)
				{
					m_actionHints.Add(new ActionHintData
					{
						hint = LocalizationKeys.input_publication_code.Localized(),
						type = ActionType.NavBtnExpandRight,
						action = OnPastePublicationCode
					});
				}
			}
			LocalizationKeys.Key key = (m_isUIListExpanded ? LocalizationKeys.shrink_ui_list : LocalizationKeys.expand_ui_list);
			m_actionHints.Add(new ActionHintData
			{
				hint = key.Localized(),
				type = ActionType.NavAdditional1BtnY,
				action = ToggleExpandLiveriesPanel
			});
			TryAddFetchAuthorLiveriesButton();
			void FetchAuthorLiveries()
			{
				if (!(m_currentFocusedLivery == null))
				{
					ResetPublicationInfo();
					m_activeSearchByAuthor.value = true;
					m_liverySharingList.Clear();
					UpdateBottomPanel();
					FlushAllCoroutines();
					string code = m_currentFocusedLivery.code;
					UILockInputContext.EnableLock();
					m_sharingService.MakeRequestOfType((code, m_carId), delegate(SdrRestLiveryCreaterList response)
					{
						SetLiveriesTypeVal();
						if (response != null && response.liveries != null)
						{
							foreach (SdrLiveryShort livery in response.liveries)
							{
								if (livery != null && !SkipSetup(livery.crossMask, livery.createPlayerId))
								{
									LiverySharingListElementData item = new LiverySharingListElementData(livery);
									m_liverySharingList.Add(item);
								}
							}
							RemoveReportedLiveries();
							UpdateUILinesFromData();
							AnalyticsService.SendLiveryShowAuthorList(m_carId, code);
							UILockInputContext.DisableLock();
						}
					});
				}
			}
			void NavigationReportRoom()
			{
				if (m_currentFocusedLivery == null)
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.report_select_livery.Localized(), 3f);
				}
				else
				{
					if (m_reportContext == null || !m_reportContext.isActive)
					{
						m_reportContext = base.owner.Get<UIReportContext>();
					}
					m_reportContext.ShowSharingReport(base.owner, m_currentFocusedLivery.code, m_currentFocusedLivery.publisherPlayerId, ReasonType.LIVERY, m_currentFocusedLivery.preset);
				}
			}
			void OnPastePublicationCode()
			{
				if (Overlay.instance is SteamOverlay steamOverlay && GameManager.IsLinuxOsPlatform())
				{
					steamOverlay.ShowOverlayKeyboard(LocalizationKeys.input_publication_code.Localized(), string.Empty, 22);
				}
				else
				{
					ShowInputDialog();
				}
			}
			void OnPressFilter()
			{
				base.owner.Push<UISharingLiveriesFilterContext>().InitFilter(m_sharingFilterController);
			}
			void ShowInputDialog()
			{
				UIMessageBox.ShowInputDialog(LocalizationKeys.input_publication_code.Localized(), string.Empty, LocalizationKeys.publication_code.Localized(), InputField.ContentType.Standard, null, LocalizationKeys.Cancel.Localized(), OnReceiveInputText, LocalizationKeys.nav_hint_apply.Localized(), 22);
			}
			void ToggleExpandLiveriesPanel()
			{
				m_isUIListExpanded = !m_isUIListExpanded;
				Vector2 vector = new Vector2(m_isUIListExpanded ? m_uiResizeWidths.widthExpanded : m_uiResizeWidths.width, m_liveriesListContainer.GetHeight());
				m_liveriesListContainer.SetWidth(vector.x);
				TimeController.AddCallback(0.05f, delegate
				{
					MoveFocus(0);
				});
				if (m_liverySharingList.Count == 0)
				{
					UpdateBottomPanel();
				}
			}
			void TryAddFetchAuthorLiveriesButton()
			{
				if (!m_activeSearchByAuthor.value && !m_activeSearchByCode.value && m_currLiveriesListType == LiveriesListType.GeneralLiveries && m_liverySharingList.Count != 0 && !(m_currentFocusedLivery == null) && (!PlayerId.TryParse(m_currentFocusedLivery.publisherPlayerId, out var playerId) || !playerId.Equals(Overlay.instance.player.id)))
				{
					m_actionHints.Add(new ActionHintData
					{
						hint = LocalizationKeys.show_author_liveries.Localized(),
						type = ActionType.NavStart,
						action = FetchAuthorLiveries
					});
				}
			}
			void TryAddReportButton()
			{
				if (m_liverySharingList.Count != 0 && !(m_currentFocusedLivery == null) && (!PlayerId.TryParse(m_currentFocusedLivery.publisherPlayerId, out var playerId) || !playerId.Equals(Overlay.instance.player.id)))
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
			FlushAllCoroutines();
			UILockInputContext.EnableLock();
			MakeSingleLiveryRequest(code, delegate(SdrRestLiverySingle response)
			{
				m_searchByCode = false;
				PlayerId playerId;
				if (response == null || response.livery == null)
				{
					UILockInputContext.DisableLock();
					GUICommonGodVoice.ShowText(LocalizationKeys.wrong_publication_code.Localized(), 3f);
				}
				else if (!PlayerId.TryParse(response.livery.createPlayerId, out playerId))
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
							m_activeSearchByCode.value = true;
							SetLiveriesTypeVal();
							ResetPublicationInfo();
							m_publicationCodeCacheList.Clear();
							TryAddResultToList(response.livery);
							FillLiveriesSharingList(m_publicationCodeCacheList, result.name);
							UpdateBottomPanel();
							AnalyticsService.SendLiveryPublicationCodeSearch(m_carId, code);
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
			void TryAddResultToList(SdrLiveryFull item)
			{
				if (item != null)
				{
					bool flag = m_currLiveriesListType == LiveriesListType.OfficialLiveries;
					bool flag2 = m_reportedLiveries.Contains(item.code);
					if (!(item.carId != m_carId || SkipSetup(item.crossMask, item.createPlayerId) || (flag && !item.official) || flag2))
					{
						m_publicationCodeCacheList.Add(item);
					}
				}
			}
		}

		private void TryAddSaveLiveryHintButton()
		{
			if (m_liverySharingList.Count != 0 && !(m_currentFocusedLivery == null))
			{
				m_actionHints.Add(new ActionHintData
				{
					hint = LocalizationKeys.save_custom_livery.Localized(),
					type = ActionType.NavSelectBtnA,
					action = SaveServerLivery
				});
			}
		}

		private void SaveServerLivery()
		{
			if (m_currentFocusedLivery == null || m_currentFocusedLivery.preset == null)
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.please_wait.Localized(), 1f);
				return;
			}
			CarVilylsInfo vinylsInfo = m_prefs.carSettings.GetVinylsInfoForCar(m_carId);
			CarVilylsInfo.VinylPreset newPreset = null;
			CarVilylsInfo.VinylPreset preset = m_currentFocusedLivery.preset;
			List<VinylLayer> layers = preset.layers;
			int layersCount = layers.Count;
			bool flag = false;
			DLCCode dlcCode = DLCCode.Unknown;
			for (int i = 0; i < layersCount; i++)
			{
				VinylLayer vinylLayer = layers[i];
				if (vinylLayer != null)
				{
					VinylCategorysMeta categoryByVinylId = m_vinylProviderMeta.GetCategoryByVinylId(vinylLayer.rawId);
					if (categoryByVinylId != null && categoryByVinylId.dlcCode != DLCCode.Unknown && !Overlay.instance.IsDLCInstalled(categoryByVinylId.dlcCode))
					{
						flag = true;
						dlcCode = categoryByVinylId.dlcCode;
						break;
					}
				}
			}
			if (flag)
			{
				UIMessageBox.ShowComplexDialog(LocalizationKeys.no_dlc_livery_apply.Localized(), null, LocalizationKeys.Cancel.Localized(), delegate
				{
					Overlay.instance.ShowBuyDLCPage(dlcCode);
					AnalyticsService.SendLiveryBuyDLCPressed(dlcCode);
				}, LocalizationKeys.buy_dlc.Localized(), delegate
				{
					Overlay.instance.ShowBuyDLCPage(DLCCode.SeasonPass);
					AnalyticsService.SendLiveryBuyDLCPressed(DLCCode.SeasonPass);
				}, LocalizationKeys.buy_season_pass.Localized(), delegate
				{
					CreateCroppedPresetAndSave();
				}, LocalizationKeys.nav_hint_continue.Localized());
			}
			else
			{
				FinalSave(LocalizationKeys.save_server_livery_question);
			}
			void CreateCroppedPresetAndSave()
			{
				List<VinylLayer> list = new List<VinylLayer>();
				for (int j = 0; j < layersCount; j++)
				{
					VinylLayer vinylLayer2 = layers[j];
					if (vinylLayer2 != null)
					{
						VinylCategorysMeta categoryByVinylId2 = m_vinylProviderMeta.GetCategoryByVinylId(vinylLayer2.rawId);
						if (categoryByVinylId2 != null && categoryByVinylId2.dlcCode != DLCCode.Unknown && !Overlay.instance.IsDLCInstalled(categoryByVinylId2.dlcCode))
						{
							list.Add(vinylLayer2);
						}
					}
				}
				newPreset = vinylsInfo.CreatePreset();
				newPreset.liveryId = preset.liveryId;
				foreach (VinylLayer layer in preset.layers)
				{
					if (!list.Contains(layer))
					{
						newPreset.layers.Add(layer);
					}
				}
				if (newPreset.layers.Count == 0)
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.error_save_not_full_livery.Localized());
				}
				else
				{
					preset = newPreset;
					FinalSave(LocalizationKeys.save_server_cropped_livery_question);
				}
			}
			void FinalSave(LocalizationKeys.Key key)
			{
				RequestProfileDataIfNeed(delegate
				{
					bool haveLocalPreset = vinylsInfo.GetPreset(m_currentFocusedLivery.liveryId) != null;
					if (haveLocalPreset)
					{
						key = LocalizationKeys.rewrite_livery_question;
					}
					UIMessageBox.ShowQuestion(key.Localized(), null, LocalizationKeys.Cancel.Localized(), delegate
					{
						if (!haveLocalPreset && vinylsInfo.GetPresetsList().Count >= 10)
						{
							GUICommonGodVoice.ShowText(LocalizationKeys.liveries_slots_exceed.Localized());
						}
						else
						{
							UILockInputContext.EnableLock();
							m_sharingService.MakeRequestOfType(m_currentFocusedLivery.code, delegate(SdrRestLiveryMarkDownload result)
							{
								UILockInputContext.DisableLock();
								if (result != null)
								{
									PerformSave();
								}
							});
						}
					}, LocalizationKeys.Ok.Localized());
					void PerformSave()
					{
						GUICommonGodVoice.ShowText((haveLocalPreset ? LocalizationKeys.livery_rewrite_save : LocalizationKeys.livery_success_save).LocalizedFormat(m_currentFocusedLivery.elementName), 3f);
						AnalyticsService.SendLiverySave(m_carId, m_currentFocusedLivery.liveryId);
						NetworkController.InstanceGame.CaptureLocalCarTextures(m_car3dView.currentCar).Forget();
						m_needResetLiveryToDefault = true;
						PlayerId item = PlayerId.Parse(m_currentFocusedLivery.publisherPlayerId);
						preset.ownerInfo = (code: m_currentFocusedLivery.code, playerId: item);
						m_startPreset = newPreset ?? preset;
						preset.time = DateTime.Now.ToBinary();
						vinylsInfo.AddPreset(preset);
						m_currentFocusedLivery.UpdateDownloadLabelStatus(preset.downloaded);
						foreach (VinylPresetElementData item2 in m_presetsList.value)
						{
							item2.Refresh();
						}
						UpdateBottomPanel();
						if (!item.Equals(Overlay.instance.player.id) && !haveLocalPreset)
						{
							TimeController.AddCallback(0f, OnRateLivery);
						}
					}
				});
			}
		}

		private void GoToSharingMenu()
		{
			m_currentFocusedLivery = null;
			CallSharingMenuContext();
			m_car3dView.IconGenerate();
		}

		private void CallSharingMenuContext()
		{
			base.owner.HideAllExcept<UIMainMenuContext>();
		}

		private void OpenServerLiveries()
		{
			if (m_currTabMode != SharingMode.FetchedLiveries)
			{
				ClearLiveriesList();
				SetTabMode(SharingMode.FetchedLiveries);
				ProcessLiveriesListTypeView();
				RemoveReportedLiveries();
				UpdateUILinesFromData();
			}
		}

		private bool HaveRequestsInProgress()
		{
			foreach (KeyValuePair<Type, bool> item in m_requestsInProgress)
			{
				if (item.Value)
				{
					return true;
				}
			}
			return false;
		}

		private void OpenLocalLiveries()
		{
			if (m_currTabMode != SharingMode.LocalLiveries)
			{
				if (HaveRequestsInProgress())
				{
					FlushAllCoroutines();
					ShowLiveryLoadingSpinner(show: false);
				}
				ResetActiveCustomSearches();
				SetLiveriesTypeVal();
				ClearLiveriesList();
				SetTabMode(SharingMode.LocalLiveries);
				FillLocalLiveriesList();
				m_liveriesSlotsCount.value = $"{m_presetsList.count}/{10}";
				UpdateBottomPanel();
			}
		}

		private void FillLocalLiveriesList()
		{
			FillList();
			UpdateUILinesFromData();
		}

		private void FillList(bool force = false)
		{
			CarVisualManager.VisualSettings vs = m_prefs.carSettings.GetVisualForCar(m_carId);
			List<CarVilylsInfo.VinylPreset> presetsList = m_prefs.carSettings.GetVinylsInfoForCar(m_carId).GetPresetsList();
			presetsList.Sort((CarVilylsInfo.VinylPreset x1, CarVilylsInfo.VinylPreset x2) => x2.time.CompareTo(x1.time) + x2.locked.CompareTo(x1.locked));
			if (string.IsNullOrEmpty(vs.vinylPresetLiveryId) && presetsList.Count > 0)
			{
				ApplyPreset(presetsList[0]);
			}
			int num = presetsList.Count + 1;
			bool flag = m_presetsList.count != num;
			if (force || m_lastCarId != m_carId || flag)
			{
				m_presetsList.Clear();
				for (int num2 = 0; num2 < presetsList.Count; num2++)
				{
					CarVilylsInfo.VinylPreset vinylPreset = presetsList[num2];
					if (vinylPreset.layers.Count > 0)
					{
						m_presetsList.Add(new VinylPresetElementData(vinylPreset));
					}
				}
			}
			else
			{
				for (int num3 = 0; num3 < 10; num3++)
				{
					m_presetsList.GetByIndex<VinylPresetElementData>(num3).Init(presetsList[num3]);
				}
			}
			int num4 = m_presetsList.IndexOf((VinylPresetElementData x) => x.preset != null && x.preset.liveryId == vs.vinylPresetLiveryId);
			if (num4 != -1)
			{
				m_presetsList.Selection(num4, selected: true);
			}
			else
			{
				m_presetsList.Selection(0, selected: true);
			}
			if (m_lastCarId != m_carId)
			{
				m_cachePreview.Clear();
			}
			else if (num4 >= 0)
			{
				m_cachePreview.Remove(vs.vinylPresetLiveryId);
			}
			m_renderPreviewCoroutine = TimeController.StartNow(RenderPreview());
			m_lastCarId = m_carId;
		}

		private IEnumerator RenderPreview()
		{
			yield return WaitBakeVinyls();
			CarModel carModel = m_car3dView.GetRaceCar().carModel;
			CarVisualManager.VisualSettings vs = m_prefs.carSettings.GetVisualForCar(m_carId);
			Camera previewCamera = Singleton<CustomCameraPresets>.instance.vinylPreviewCamera;
			string lastPresetLiveryId = vs.vinylPresetLiveryId;
			bool lockIsSetted = false;
			try
			{
				int i = 0;
				while (i < m_presetsList.count)
				{
					VinylPresetElementData data = m_presetsList.GetByIndex<VinylPresetElementData>(i);
					if (data.preset != null)
					{
						if (m_cachePreview.TryGetValue(data.preset.liveryId, out (long, Texture) value) && value.Item1 == data.preset.time)
						{
							data.m_preview.value = value.Item2;
						}
						else
						{
							bool oldAsyncLoading = VinylSystemConfig.config.asyncLoading;
							VinylSystemConfig.config.asyncLoading = true;
							bool isCurrentPresset = data.preset.liveryId == vs.vinylPresetLiveryId;
							if (!isCurrentPresset)
							{
								if (!lockIsSetted)
								{
									lockIsSetted = true;
									UILockInputContext.EnableLock();
								}
								m_vinylsEditor.StartEditLivery(carModel);
								m_vinylsEditor.ReplaceAllLayers(data.preset.layers);
								lastPresetLiveryId = data.preset.liveryId;
							}
							while (carModel.IsLoading())
							{
								yield return null;
							}
							previewCamera.gameObject.SetActive(value: true);
							Texture2D texture2D = RenderPreviewToTexture(previewCamera);
							previewCamera.gameObject.SetActive(value: false);
							data.m_preview.value = texture2D;
							m_cachePreview[data.preset.liveryId] = (data.preset.time, texture2D);
							if (!isCurrentPresset)
							{
								m_vinylsEditor.ReplaceAllLayers(vs.vinylLayers);
								m_vinylsEditor.CompleteEditLivery();
							}
							VinylSystemConfig.config.asyncLoading = oldAsyncLoading;
							yield return null;
						}
					}
					int num = i + 1;
					i = num;
				}
			}
			finally
			{
				UILiveriesSharingContext uILiveriesSharingContext = this;
				if (lastPresetLiveryId != vs.vinylPresetLiveryId)
				{
					carModel.livery.ReplaceLayers(vs.vinylLayers, forceValidate: false);
					uILiveriesSharingContext.m_vinylsEditor.CompleteEditLivery();
				}
				if (lockIsSetted)
				{
					UILockInputContext.DisableLock();
				}
			}
			m_renderPreviewCoroutine = null;
		}

		private Texture2D RenderPreviewToTexture(Camera renderCamera)
		{
			RenderTexture renderTexture = (renderCamera.targetTexture = RenderTexture.GetTemporary(m_textureSize.x, m_textureSize.y, 16, RenderTextureFormat.Default));
			renderCamera.Render();
			Texture2D texture2D = new Texture2D(m_textureSize.x, m_textureSize.y, TextureFormat.RGB24, mipChain: false);
			RenderTexture active = RenderTexture.active;
			RenderTexture.active = renderTexture;
			texture2D.ReadPixels(new Rect(0f, 0f, m_textureSize.x, m_textureSize.y), 0, 0);
			texture2D.Apply();
			RenderTexture.active = active;
			renderCamera.targetTexture = null;
			RenderTexture.ReleaseTemporary(renderTexture);
			return texture2D;
		}

		private IEnumerator WaitBakeVinyls()
		{
			do
			{
				yield return null;
			}
			while (m_car3dView.GetRaceCar() == null || !m_car3dView.GetRaceCar().IsLiveryReady);
		}

		private void ApplyPreset(CarVilylsInfo.VinylPreset preset)
		{
			if (preset == null)
			{
				ClearPreset();
				return;
			}
			CarVisualManager.VisualSettings visualForCar = m_prefs.carSettings.GetVisualForCar(m_carId);
			m_vinylsEditor.StartEditLivery(m_car3dView.GetRaceCar().carModel);
			m_vinylsEditor.ReplaceAllLayers(preset.layers);
			m_vinylsEditor.CompleteEditLivery();
			visualForCar.vinylPresetLiveryId = preset.liveryId;
			visualForCar.vinylPresetId = preset.id;
			visualForCar.vinylLayers.Clear();
			visualForCar.vinylLayers.AddRange(preset.layers);
			m_carVisualManager.SaveVisualSettingsForCar(m_carId, visualForCar);
		}

		private void ApplyServerPreset(CarVilylsInfo.VinylPreset preset)
		{
			if (preset != null)
			{
				m_showNotFullVinylsPack.value = preset.layers.Exists((VinylLayer layer) => !Singleton<VinylMetaProvider>.instance.IsValidLayer(layer));
				CarVisualManager.VisualSettings visualForCar = m_prefs.carSettings.GetVisualForCar(m_carId);
				m_vinylsEditor.StartEditLivery(m_car3dView.GetRaceCar().carModel);
				m_vinylsEditor.ReplaceAllLayers(preset.layers);
				m_vinylsEditor.CompleteEditLivery();
				visualForCar.vinylPresetLiveryId = preset.liveryId;
				visualForCar.vinylPresetId = preset.id;
				visualForCar.vinylLayers.Clear();
				visualForCar.vinylLayers.AddRange(preset.layers);
				ShowLiveryLoadingSpinner(show: false);
			}
		}

		private void InitCategories()
		{
			Array values = Enum.GetValues(typeof(SharingLiveryCategoryType));
			if (m_categories == null)
			{
				m_categories = new List<string>(values.Length)
				{
					SharingLiveryCategoryType.Original.ToString(),
					SharingLiveryCategoryType.Replica.ToString(),
					SharingLiveryCategoryType.Itasha.ToString(),
					SharingLiveryCategoryType.Racing.ToString(),
					SharingLiveryCategoryType.Badges.ToString(),
					SharingLiveryCategoryType.Abstract.ToString(),
					SharingLiveryCategoryType.Colorful.ToString(),
					SharingLiveryCategoryType.Minimalistic.ToString(),
					SharingLiveryCategoryType.Artistic.ToString(),
					SharingLiveryCategoryType.USDM.ToString(),
					SharingLiveryCategoryType.EUDM.ToString(),
					SharingLiveryCategoryType.AUDM.ToString(),
					SharingLiveryCategoryType.ASDM.ToString(),
					SharingLiveryCategoryType.PRO.ToString(),
					SharingLiveryCategoryType.STREET.ToString()
				};
			}
		}

		private void ResetTabMode()
		{
			SetTabMode(SharingMode.None);
		}

		private void SetTabMode(SharingMode mode)
		{
			if (m_currTabMode != mode)
			{
				UILockInputContext.DisableLock();
				FlushAllCoroutines();
				ShowLiveryLoadingSpinner(show: false);
				m_currTabMode = mode;
				m_tabMode.value = (int)mode;
			}
		}

		private void OnApplyFilterHandler()
		{
			m_filterChanged = true;
			ResetRequestDataTimer();
			ProcessLiveriesListTypeView();
			AnalyticsService.SendLiveryFilterUse(m_carId);
		}

		private void ProcessLiveriesListTypeView()
		{
			ResetPublicationInfo();
			ClearLiveriesList();
			switch (m_currLiveriesListType)
			{
			case LiveriesListType.GeneralLiveries:
				FetchLiveriesShortListData(delegate
				{
					TimeController.AddCallback(0f, UpdateBottomPanel);
				});
				break;
			case LiveriesListType.OfficialLiveries:
				ShowOfficialLiveries(delegate
				{
					TimeController.AddCallback(0f, UpdateBottomPanel);
				});
				break;
			case LiveriesListType.MyUploadedLiveries:
				FetchLiveriesFullListData(delegate
				{
					TimeController.AddCallback(0f, UpdateBottomPanel);
				});
				break;
			}
		}

		private void OnClick_PresetsList(BaseListElementData obj)
		{
			if (!obj.selected)
			{
				m_presetsList.Selection(m_presetsList.IndexOf(obj), selected: true);
				UpdateBottomPanel();
			}
		}

		private void OnSelectionChanged_PresetsList(BaseListElementData obj)
		{
			if (obj.selected)
			{
				TimeController.AddCallback(0f, delegate
				{
					m_presetsList.ScrollTo(m_presetsList.IndexOf(obj));
				});
				VinylPresetElementData vinylPresetElementData = obj as VinylPresetElementData;
				if (vinylPresetElementData.preset != null)
				{
					ApplyPreset(vinylPresetElementData.preset);
				}
			}
		}

		private bool SkipSetup(int platform, string playerId)
		{
			IGameOverlay instance = Overlay.instance;
			bool flag = false;
			if (instance.IsCrossPlayRestricted)
			{
				flag = platform != (int)instance.GetPlatform();
			}
			bool flag2 = false;
			PlayerId.TryParse(playerId, out var playerId2);
			if (instance.IsUserGeneratedContentRestricted && instance.IsFriendShowUGCAllowed)
			{
				flag2 = !instance.IsFriend(playerId2);
			}
			bool flag3 = instance.IsBlockedUser(playerId2);
			return flag || flag2 || flag3;
		}

		private void FetchLiveriesShortListData(System.Action onDone)
		{
			LiveriesListType liveryListType = LiveriesListType.GeneralLiveries;
			if (!ShouldRequestData(liveryListType))
			{
				FillLiveriesListFromCacheAndUpdate(liveryListType);
				onDone.SafeCall();
				return;
			}
			UILockInputContext.EnableLock();
			m_sharingService.MakeRequestOfType(m_sharingFilterController.sharingFilter, delegate(SdrRestLiveryLobby response)
			{
				UILockInputContext.DisableLock();
				if (response == null || response.liveries == null)
				{
					onDone.SafeCall();
				}
				else
				{
					m_liverySharingList.Clear();
					foreach (SdrLiveryShort livery in response.liveries)
					{
						if (livery != null && !SkipSetup(livery.crossMask, livery.createPlayerId) && !livery.official)
						{
							LiverySharingListElementData item = new LiverySharingListElementData(livery);
							m_liverySharingList.Add(item);
						}
					}
					m_liverySharingListCache.Clear();
					m_liverySharingListCache.AddRange(m_liverySharingList);
					RemoveReportedLiveries();
					UpdateUILinesFromData();
					onDone.SafeCall();
				}
			});
		}

		private void ShowOfficialLiveries(System.Action onDone)
		{
			LiveriesListType liveryListType = LiveriesListType.OfficialLiveries;
			if (!ShouldRequestData(liveryListType))
			{
				FillLiveriesListFromCacheAndUpdate(liveryListType);
				onDone.SafeCall();
				return;
			}
			UILockInputContext.EnableLock();
			m_sharingService.MakeRequestOfType(m_sharingFilterController.sharingFilter, delegate(SdrRestLiveryLobby response)
			{
				UILockInputContext.DisableLock();
				if (response == null || response.liveries == null)
				{
					onDone.SafeCall();
				}
				else
				{
					m_liverySharingList.Clear();
					foreach (SdrLiveryShort livery in response.liveries)
					{
						if (livery != null && !SkipSetup(livery.crossMask, livery.createPlayerId) && livery.official)
						{
							LiverySharingListElementData item = new LiverySharingListElementData(livery);
							m_liverySharingList.Add(item);
						}
					}
					m_liverySharingListOfficialCache.Clear();
					m_liverySharingListOfficialCache.AddRange(m_liverySharingList);
					RemoveReportedLiveries();
					UpdateUILinesFromData();
					onDone.SafeCall();
				}
			});
		}

		private void FetchLiveriesFullListData(System.Action onDone)
		{
			LiveriesListType liveryListType = LiveriesListType.MyUploadedLiveries;
			if (!ShouldRequestData(liveryListType))
			{
				FillLiveriesListFromCacheAndUpdate(liveryListType);
				onDone.SafeCall();
				return;
			}
			UILockInputContext.EnableLock();
			m_sharingService.MakeRequestOfType(m_carId, delegate(SdrRestLiveryList response)
			{
				UILockInputContext.DisableLock();
				if (response == null || response.liveries == null)
				{
					onDone.SafeCall();
				}
				else
				{
					FillLiveriesSharingList(response.liveries);
					m_liverySharingListMyUploadedCache.Clear();
					m_liverySharingListMyUploadedCache.AddRange(m_liverySharingList);
					onDone.SafeCall();
				}
			});
		}

		private void FillLiveriesSharingList(List<SdrLiveryFull> fullLiveries, string nickName = null)
		{
			m_liverySharingList.Clear();
			foreach (SdrLiveryFull fullLivery in fullLiveries)
			{
				LiverySharingListElementData liverySharingListElementData = new LiverySharingListElementData(fullLivery);
				if (!string.IsNullOrEmpty(nickName))
				{
					liverySharingListElementData.SetNickname(nickName);
				}
				m_liverySharingList.Add(liverySharingListElementData);
			}
			UpdateUILinesFromData();
		}

		private void FillLiveriesListFromCacheAndUpdate(LiveriesListType liveryListType)
		{
			List<LiverySharingListElementData> list = null;
			switch (liveryListType)
			{
			case LiveriesListType.GeneralLiveries:
				list = m_liverySharingListCache;
				break;
			case LiveriesListType.OfficialLiveries:
				list = m_liverySharingListOfficialCache;
				break;
			case LiveriesListType.MyUploadedLiveries:
				list = m_liverySharingListMyUploadedCache;
				break;
			}
			if (list != null)
			{
				m_liverySharingList.Clear();
				m_liverySharingList.AddRange(list);
				RemoveReportedLiveries();
				UpdateUILinesFromData();
			}
		}

		private void RemoveReportedLiveries()
		{
			foreach (string ignoredCode in m_reportedLiveries)
			{
				m_liverySharingList.RemoveAll((LiverySharingListElementData item) => item.code == ignoredCode);
			}
		}

		private bool ShouldRequestData(LiveriesListType liveryListType)
		{
			bool flag = true;
			switch (liveryListType)
			{
			case LiveriesListType.GeneralLiveries:
				flag = IsEmptyCollection(m_liverySharingListCache);
				break;
			case LiveriesListType.OfficialLiveries:
				flag = IsEmptyCollection(m_liverySharingListOfficialCache);
				break;
			case LiveriesListType.MyUploadedLiveries:
				flag = IsEmptyCollection(m_liverySharingListMyUploadedCache);
				break;
			}
			flag |= Time.unscaledTime - m_requestDataTimer > 60f;
			m_requestDataTimer = Time.unscaledTime;
			return flag;
			static bool IsEmptyCollection(List<LiverySharingListElementData> collection)
			{
				return collection.Count == 0;
			}
		}

		private void ResetRequestDataTimer()
		{
			m_requestDataTimer = float.NegativeInfinity;
		}

		private void ClearLiveriesList()
		{
			m_liverySharingList.Clear();
		}

		private void FixedUpdate()
		{
			if (!SkipInputUpdate() && TryGetMoveIndex(out var moveIdx))
			{
				ProcessNavigation(moveIdx);
			}
		}

		private bool TryGetMoveIndex(out int moveIdx)
		{
			moveIdx = 0;
			InputManager inputManager = InputManager.instance;
			if (IsActionActive(ActionType.NavUp))
			{
				moveIdx = (m_isUIListExpanded ? (-3) : (-1));
				return true;
			}
			if (IsActionActive(ActionType.NavDown))
			{
				moveIdx = ((!m_isUIListExpanded) ? 1 : 3);
				return true;
			}
			if (IsActionActive(ActionType.NavRight) && m_isUIListExpanded)
			{
				moveIdx = 1;
				return true;
			}
			if (IsActionActive(ActionType.NavLeft) && m_isUIListExpanded)
			{
				moveIdx = -1;
				return true;
			}
			return false;
			bool IsActionActive(ActionType actionType)
			{
				if (!inputManager.IsActionPressed(actionType))
				{
					return inputManager.IsActionHold(actionType);
				}
				return true;
			}
		}

		private bool SkipInputUpdate()
		{
			if (!m_activeRateContext && !m_activeFilterContext && !m_activeReportContext && !m_activeDialogContext && !m_activeConsoleContext)
			{
				return UILockInputContext.IsActive();
			}
			return true;
		}

		private void ProcessNavigation(int moveIdx)
		{
			if (m_currTabMode == SharingMode.FetchedLiveries)
			{
				MoveFocus(moveIdx);
			}
		}

		[GameHotkey(ActionType.NavDown, false, false)]
		private void NextItem()
		{
			if (!SkipInputUpdate() && m_currTabMode == SharingMode.LocalLiveries)
			{
				m_presetsList.Selection(m_presetsList.GetSelectedIndex() + 1, selected: true);
				UpdateBottomPanel();
			}
		}

		[GameHotkey(ActionType.NavUp, false, false)]
		private void PrevItem()
		{
			if (!SkipInputUpdate() && m_currTabMode == SharingMode.LocalLiveries)
			{
				m_presetsList.Selection(m_presetsList.GetSelectedIndex() - 1, selected: true);
				UpdateBottomPanel();
			}
		}

		private void MoveFocus(int next)
		{
			if (!SkipFocusing())
			{
				m_time = Time.unscaledTime;
				FlushCoroutineIfNeed(ref m_focusCoroutine);
				m_focusCoroutine = TimeController.StartNow(MoveFocusAsync(next));
			}
			IEnumerator MoveFocusAsync(int num2)
			{
				if (m_currentFocusedLivery == null)
				{
					FlushCoroutineIfNeed(ref m_setFirstFocusCoroutine);
					m_setFirstFocusCoroutine = SetFocusToFirstAsync();
					yield return m_setFirstFocusCoroutine;
				}
				else
				{
					int num = m_liverySharingLinesPool.IndexOf(m_currentFocusedLivery);
					if (num != -1)
					{
						num += num2;
						if (num >= 0 && num < m_liverySharingLinesPool.Count)
						{
							LiverySharingListElement liverySharingListElement = m_liverySharingLinesPool[num];
							if (liverySharingListElement != null && liverySharingListElement.gameObject.activeInHierarchy)
							{
								FlushCoroutineIfNeed(ref m_setFocusUICoroutine);
								m_setFocusUICoroutine = SetFocusUI(liverySharingListElement);
								yield return m_setFocusUICoroutine;
							}
						}
					}
				}
			}
		}

		private void UpdateUILinesFromData()
		{
			string currentFocusedLiveryCode = GetCurrentFocusedLiveryCode();
			GOPool.Allocate(m_liverySharingLineTemplate, ref m_liverySharingLinesPool, m_liverySharingList.Count, delegate(LiverySharingListElement uiElement)
			{
				uiElement.SetContext(this);
			}, OnLiveryUIDeactivated);
			for (int num = 0; num < m_liverySharingList.Count; num++)
			{
				UpdateUILiverySharingInfo(m_liverySharingList[num], m_liverySharingLinesPool[num]);
			}
			SetFocus(currentFocusedLiveryCode);
			m_showEmptyListMessage.value = (m_currTabMode == SharingMode.FetchedLiveries && m_liverySharingList.Count == 0) || (m_currTabMode == SharingMode.LocalLiveries && m_presetsList.count == 0);
			m_showPublicationBlock.value = !m_showEmptyListMessage.value && m_currTabMode != SharingMode.LocalLiveries;
		}

		private void UpdateUILiverySharingInfo(LiverySharingListElementData liverySharingData, LiverySharingListElement liveryLine)
		{
			bool localLivery = m_prefs.carSettings.GetVinylsInfoForCar(m_carId).GetPresetsList().Exists((CarVilylsInfo.VinylPreset preset) => preset.liveryId == liverySharingData.liveryId);
			liverySharingData.UpdateLocalCustomLiveryStatus(localLivery);
			liveryLine.UpdateFrom(liverySharingData);
		}

		private void SetFocus(string liveryCode)
		{
			if (string.IsNullOrEmpty(liveryCode))
			{
				SetFocusToFirst();
				return;
			}
			LiverySharingListElementData liverySharingListElementData = FindLiverySharingData(liveryCode);
			if (liverySharingListElementData == null)
			{
				SetFocusToFirst();
				return;
			}
			m_lastFocusedUIDict.TryGetValue(m_currLiveriesListType, out var value);
			LiverySharingListElement liverySharingListElement = FindLiveryUI(value, liverySharingListElementData);
			if (liverySharingListElement == null)
			{
				SetFocusToFirst();
				return;
			}
			FlushCoroutineIfNeed(ref m_focusCoroutine);
			m_focusCoroutine = TimeController.StartNow(SetFocusUI(liverySharingListElement));
		}

		private LiverySharingListElement FindLiveryUI(string code, LiverySharingListElementData item)
		{
			if (item != null)
			{
				int num = m_liverySharingList.FindIndex(0, m_liverySharingList.Count, (LiverySharingListElementData el) => el.code == code);
				if (num >= 0 && num <= m_liverySharingLinesPool.Count)
				{
					return m_liverySharingLinesPool[num];
				}
				foreach (LiverySharingListElement item2 in m_liverySharingLinesPool)
				{
					if (item2.gameObject.activeInHierarchy && item2.code == item.code)
					{
						return item2;
					}
				}
			}
			return null;
		}

		private LiverySharingListElementData FindLiverySharingData(string code)
		{
			return m_liverySharingList.Find((LiverySharingListElementData item) => item.code == code);
		}

		private void SetFocusToFirst()
		{
			if (m_setFirstFocusCoroutine == null)
			{
				FlushCoroutineIfNeed(ref m_focusCoroutine);
				m_focusCoroutine = TimeController.StartNow(SetFocusToFirstAsync());
			}
		}

		private IEnumerator SetFocusToFirstAsync()
		{
			if (m_liverySharingList.Count != 0)
			{
				FlushCoroutineIfNeed(ref m_setFocusUICoroutine);
				m_setFocusUICoroutine = SetFocusUI(m_liverySharingLinesPool[0]);
				yield return m_setFocusUICoroutine;
			}
		}

		private IEnumerator EnsureVisibleCurrentFocus()
		{
			if (m_currentFocusedLivery == null)
			{
				yield break;
			}
			RectTransform focusedRect = m_currentFocusedLivery.rectTransform;
			if (!(focusedRect == null))
			{
				while (!m_liveriesScrollRect.IsScrollItemVerticalyVisible(focusedRect))
				{
					m_liveriesScrollRect.SnapScrollTo(focusedRect);
					yield return null;
				}
			}
		}

		private IEnumerator SetFocusUI(LiverySharingListElement sharingElement, System.Action onDone = null)
		{
			ShowLiveryLoadingSpinner(show: false);
			if (m_currentFocusedLivery != null)
			{
				m_currentFocusedLivery.SetFocus(isFocused: false);
				m_currentFocusedLivery = null;
			}
			if (sharingElement == null)
			{
				CompleteFocusProcess();
				yield break;
			}
			float currTime = Time.time;
			while (sharingElement.preview == null)
			{
				if (Time.time - currTime > 10f)
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.error_server_error.Localized(), 3f);
					m_topPanelContext.switcherActions.SelectionAction(1);
					CompleteFocusProcess();
					yield break;
				}
				yield return null;
			}
			m_currentFocusedLivery = sharingElement;
			FlushCoroutineIfNeed(ref m_ensureVisibleCurrentFocusCoroutine);
			m_ensureVisibleCurrentFocusCoroutine = EnsureVisibleCurrentFocus();
			yield return m_ensureVisibleCurrentFocusCoroutine;
			if (!m_currentFocusedLivery)
			{
				CompleteFocusProcess();
				yield break;
			}
			m_currentFocusedLivery.SetFocus(isFocused: true);
			m_lastFocusedUIDict[m_currLiveriesListType] = m_currentFocusedLivery.code;
			bool requesting = true;
			CarVilylsInfo.VinylPreset preset = null;
			m_loadLiveryDelayObject.waitTime = m_focusDelay;
			yield return m_loadLiveryDelayObject;
			RequestProfileDataIfNeed(delegate(CarVilylsInfo.VinylPreset pr)
			{
				requesting = false;
				preset = pr;
			});
			while (requesting)
			{
				yield return null;
			}
			if (m_currentFocusedLivery == null)
			{
				CompleteFocusProcess();
				yield break;
			}
			m_currentFocusedLivery.SetPreset(preset);
			if (m_currentFocusedLivery.preset == null)
			{
				CompleteFocusProcess();
				yield break;
			}
			ApplyServerPreset(m_currentFocusedLivery.preset);
			RefreshPublicationInfo(sharingElement);
			UpdateBottomPanel();
			CompleteFocusProcess();
			void CompleteFocusProcess()
			{
				UILockInputContext.DisableLock();
				onDone.SafeCall();
			}
		}

		private void RefreshPublicationInfo(LiverySharingListElement item)
		{
			bool isFilterCrossplatformEnabled = m_sharingFilterController.GetFilterValue<LiveriesSharingFilterType, int>(LiveriesSharingFilterType.CrossMask) == 0;
			m_liverySharingPublication.SetData(item, isFilterCrossplatformEnabled);
		}

		public void SwitchTab(bool next)
		{
			if (HaveRequestsInProgress())
			{
				return;
			}
			int currLiveriesListType = (int)m_currLiveriesListType;
			int num = ((!next) ? (--currLiveriesListType) : (++currLiveriesListType));
			Array values = Enum.GetValues(typeof(LiveriesListType));
			int length = values.Length;
			if (num > length - 1)
			{
				num = 0;
			}
			if (num < 0)
			{
				num = length - 1;
			}
			LiveriesListType liveriesListType = (LiveriesListType)values.GetValue(num);
			if (m_currLiveriesListType != liveriesListType)
			{
				m_currLiveriesListType = liveriesListType;
				SetLiveriesTypeVal();
				if (m_filterChanged)
				{
					ResetRequestDataTimer();
					m_filterChanged = false;
				}
				FlushAllCoroutines();
				ProcessLiveriesListTypeView();
			}
		}

		private void SetLiveriesTypeVal()
		{
			m_liveriesType.value = GetLiveriesTypeVal();
			string GetGeneralTabTitle()
			{
				if (m_activeSearchByCode.value)
				{
					return LocalizationKeys.search_result.Localized();
				}
				if (m_activeSearchByAuthor.value)
				{
					return LocalizationKeys.show_author_liveries.Localized();
				}
				return LocalizationKeys.car_catalog_category_all.Localized();
			}
			string GetLiveriesTypeVal()
			{
				return m_currLiveriesListType switch
				{
					LiveriesListType.GeneralLiveries => GetGeneralTabTitle(), 
					LiveriesListType.OfficialLiveries => GetOfficialTabTitle(), 
					LiveriesListType.MyUploadedLiveries => LocalizationKeys.my_uploaded_setups.Localized(), 
					_ => throw new NotImplementedException(), 
				};
			}
			string GetOfficialTabTitle()
			{
				if (m_activeSearchByCode.value)
				{
					return LocalizationKeys.search_result.Localized();
				}
				return LocalizationKeys.official_liveries.Localized();
			}
		}

		private void ShowLiveryLoadingSpinner(bool show)
		{
			if (m_showLiveryLoadingSpinner.value != show)
			{
				m_showLiveryLoadingSpinner.value = show;
			}
		}

		private void RequestProfileDataIfNeed(Action<CarVilylsInfo.VinylPreset> canProceed)
		{
			if (m_currTabMode != SharingMode.FetchedLiveries)
			{
				return;
			}
			if (m_currentFocusedLivery == null)
			{
				canProceed.SafeCall(null);
				return;
			}
			CarVilylsInfo.VinylPreset preset = m_currentFocusedLivery.preset;
			if (preset != null)
			{
				canProceed.SafeCall(preset);
				return;
			}
			string code = m_currentFocusedLivery.code;
			ShowLiveryLoadingSpinner(show: true);
			MakeSingleLiveryRequest(code, delegate(SdrRestLiverySingle response)
			{
				if (response == null)
				{
					canProceed.SafeCall(null);
				}
				else
				{
					LiverySharingListElementData liverySharingListElementData = m_liverySharingList.Find((LiverySharingListElementData el) => el.code == code);
					if (liverySharingListElementData == null)
					{
						canProceed.SafeCall(null);
					}
					else
					{
						liverySharingListElementData.SetElementData(response.livery, delegate(CarVilylsInfo.VinylPreset param)
						{
							canProceed.SafeCall(param);
						});
					}
				}
			});
		}

		private void MakeSingleLiveryRequest(string code, Action<SdrRestLiverySingle> onDone)
		{
			m_sharingService.MakeRequestOfType(code, onDone);
		}

		private void FlushCoroutineIfNeed(ref Coroutine coroutine)
		{
			if (coroutine != null)
			{
				TimeController.StopNow(coroutine);
				coroutine = null;
			}
		}

		private void FlushCoroutineIfNeed(ref IEnumerator coroutine)
		{
			if (coroutine != null)
			{
				TimeController.StopNow(coroutine);
				coroutine = null;
			}
		}

		private void OnLiveryUIDeactivated(LiverySharingListElement uiLine)
		{
			if (!(uiLine == null) && !(uiLine.gameObject == null))
			{
				uiLine.gameObject.SetActive(value: false);
				uiLine = null;
			}
		}

		private string GetCurrentFocusedLiveryCode()
		{
			m_lastFocusedUIDict.TryGetValue(m_currLiveriesListType, out var value);
			return value;
		}

		private bool SkipFocusing()
		{
			return Time.unscaledTime - m_time < 0.2f;
		}

		public void OnClickSharingElement(LiverySharingListElement uiItem)
		{
			if (!SkipFocusing())
			{
				m_time = Time.unscaledTime;
				InputManager.PlayClickSound();
				FlushCoroutineIfNeed(ref m_focusCoroutine);
				m_focusCoroutine = TimeController.StartNow(SetFocusUI(uiItem));
			}
		}
	}
}
