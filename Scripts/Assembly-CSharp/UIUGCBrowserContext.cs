using System;
using System.Collections.Generic;
using System.Threading;
using Binding;
using Binding.Base;
using Cysharp.Threading.Tasks;
using DB;
using DB.Meta;
using DI;
using GameInput;
using GameOverlay;
using ModIO;
using Rewired;
using Services.UGC.Interfaces;
using SharingTracks;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;

[AudioFXBlocker(true)]
public class UIUGCBrowserContext : BaseBindingBehaviourTarget
{
	private const string FILTER_FORMAT = "<color=yellow>{0}</color>";

	private const int PAGE_ITEM_COUNT = 16;

	private const int COLUMN_COUNT = 4;

	[SerializeField]
	private SpriteAtlas m_setupsAtlas;

	[SerializeField]
	private UIBaseSharingPublication m_baseSharingPublication;

	[SerializeField]
	private CanvasGroup m_trackContainerCanvasGroup;

	[SerializeField]
	private TMP_InputField m_searchInputField;

	private readonly GridListProperty m_itemsGridList = new GridListProperty(4);

	private static readonly List<UGCBrowserListItemData> s_itemsData = new List<UGCBrowserListItemData>();

	private readonly List<UGCItem> m_fetchedItems = new List<UGCItem>();

	private readonly List<ActionHintData> m_actionHints = new List<ActionHintData>();

	private readonly BoolProperty m_entryLoadingVisibility = new BoolProperty();

	private readonly StringProperty m_ugcServiceTitle = new StringProperty();

	private readonly StringProperty m_carName = new StringProperty();

	private readonly SpriteProperty m_setupIcon = new SpriteProperty();

	private readonly BoolProperty m_showEmptyMsg = new BoolProperty();

	private readonly BoolProperty m_isChangingPage = new BoolProperty();

	private readonly BoolProperty m_itemDetailsVisibility = new BoolProperty();

	private readonly BoolProperty m_nextPageSwitcherVisibility = new BoolProperty();

	private readonly BoolProperty m_prevPageSwitcherVisibility = new BoolProperty();

	private readonly BoolProperty m_ugcDetailsVisibility = new BoolProperty(startValue: true);

	private readonly BoolProperty m_isUsedCarTagVisibility = new BoolProperty(false);

	private readonly BoolProperty m_showInputField = new BoolProperty();

	private readonly BoolProperty m_searchInputFieldSelected = new BoolProperty();

	private readonly BoolProperty m_searchByFilterLabelActivity = new BoolProperty();

	private readonly StringProperty m_filterProgressPercent = new StringProperty();

	private readonly StringProperty m_placeholderText = new StringProperty();

	private readonly StringProperty m_emptyListLabel = new StringProperty();

	private readonly IntProperty m_ugcServiceId = new IntProperty();

	private SharingFilterController m_sharingFilterController;

	private List<UGCItem> m_filteredItems = new List<UGCItem>();

	private int m_currPageIndex;

	private int m_maxFetchedPage = -1;

	private bool m_isAllItemsFetched;

	private IUGCManager m_ugcManager;

	private ISharingFilter m_currFilter;

	private CancellationTokenSource m_filterProceedCancellationToken;

	private CancellationTokenSource m_waitFetchingPageCancellationToken;

	private Queue<UGCBrowserListItemData> m_queuedIconsItems = new Queue<UGCBrowserListItemData>();

	private bool m_isAnyIconProceeding;

	private UISharingTracksFilterContext m_filterCtx;

	private bool m_managerAuthenticated;

	[Dependency]
	private BaseModel m_model;

	[Dependency]
	private GamePrefs m_prefs;

	[ContextDependency]
	private GarageCar3dViewContext m_car3dView;

	public void InitUGCManager(IUGCManager ugcManager)
	{
		m_ugcManager = ugcManager;
		m_ugcServiceId.value = (int)ugcManager.serviceProviderType;
		m_ugcServiceTitle.value = ugcManager.serviceTitle;
	}

	private void AssertUGCManager()
	{
		if (m_ugcManager == null)
		{
			GameLog.Print(AlertLevel.Error, "ModMaps", "Init UGC Manager at first!");
		}
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		if (!CheckUGCRestrictionPass())
		{
			TimeController.AddCallback(0f, OnCloseHandler);
			return;
		}
		AssertUGCManager();
		m_isAllItemsFetched = false;
		m_isChangingPage.value = false;
		ContextOwner contextOwner = base.owner;
		contextOwner.onContextActiveChanged = (Action<BaseContext>)Delegate.Combine(contextOwner.onContextActiveChanged, new Action<BaseContext>(OnContextActiveChangedHandler));
		if (!base.owner.IsActive<UIBackgroundContext>())
		{
			base.owner.Add<UIBackgroundContext>();
		}
		ResetSearchInputField();
		BringForwardByDepth();
		InitFilters();
		UpdateTopPanelLabel();
		UpdateLabelsByTabType();
		UpdateDGCDetailsTagVisibility();
		SubscribeListHandlers();
		UpdateCarSpecsDescription();
		InitSearchInputField();
		PrepareAndStartUGCManagerAsync().Forget();
	}

	private void ResetSearchInputField()
	{
		m_searchInputField.text = string.Empty;
	}

	private void UpdateLabelsByTabType()
	{
		m_placeholderText.value = GetInputPlaceholderLocalization();
		m_emptyListLabel.value = GetEmptyListLabelLocalization();
	}

	private string GetInputPlaceholderLocalization()
	{
		return m_ugcManager.contentType switch
		{
			IUGCManager.ContentType.Tracks => LocalizationKeys.input_track_name.Localized(), 
			IUGCManager.ContentType.Cars => LocalizationKeys.input_bodykit_name.Localized(), 
			_ => null, 
		};
	}

	private string GetEmptyListLabelLocalization()
	{
		return m_ugcManager.contentType switch
		{
			IUGCManager.ContentType.Tracks => LocalizationKeys.tracks_empty_list.Localized(), 
			IUGCManager.ContentType.Cars => LocalizationKeys.cars_empty_list.Localized(), 
			_ => null, 
		};
	}

	private void UpdateDGCDetailsTagVisibility()
	{
		m_isUsedCarTagVisibility.value = m_ugcManager.contentType == IUGCManager.ContentType.Cars || m_ugcManager.serviceProviderType == IUGCManager.ServiceProvider.ModIO;
	}

	private void UpdateTopPanelLabel()
	{
		string title = ((m_ugcManager.contentType == IUGCManager.ContentType.Tracks) ? LocalizationKeys.server_sharing_tracks.Localized() : (LocalizationKeys.car_sharing_title.Localized() + " (BETA)"));
		ShowTopPanel(ETopPanelState.Title, title);
	}

	private bool CheckUGCRestrictionPass()
	{
		if (Overlay.instance.IsUserGeneratedContentRestricted)
		{
			Overlay.instance.ShowUGCRestrictionDialog();
			return false;
		}
		return true;
	}

	private async UniTaskVoid PrepareAndStartUGCManagerAsync()
	{
		m_managerAuthenticated = false;
		m_entryLoadingVisibility.value = true;
		UpdateBottomPanel();
		HideBottomPanel();
		IUGCAuthentication ugcAuthentication = m_ugcManager.service.authentication;
		bool flag = !(await ugcAuthentication.IsAuthenticatedAsync());
		if (flag)
		{
			flag = !(await ugcAuthentication.AuthenticateAsync());
		}
		if (flag)
		{
			OnCloseHandler();
			return;
		}
		m_managerAuthenticated = true;
		if (m_ugcManager.serviceProviderType == IUGCManager.ServiceProvider.ModIO)
		{
			await ModIOUnityAsync.FetchUpdates();
		}
		if (base.owner.IsActive<UIUGCBrowserContext>())
		{
			UpdateBottomPanel();
			if (m_sharingFilterController.IsActiveFilter())
			{
				m_ugcManager.FetchUGCItems(OnCompletedFetchHandler);
				ProceedFilter().Forget();
			}
			else
			{
				LoadItemList(byFilter: false).Forget();
			}
		}
	}

	private void InitSearchInputField()
	{
		m_searchInputFieldSelected.OnValueChanged += OnSearchInputFieldSelectedHandler;
		SubscribeTMPInputHandlers();
	}

	private void Update()
	{
		HandleSelectInputField();
		HandleJoystickDeselectInputField();
		HandleItemsIconQueue();
	}

	private void HandleSelectInputField()
	{
		if (m_searchInputField.isFocused && !m_searchInputFieldSelected.value)
		{
			SelectInputField();
		}
	}

	private void HandleJoystickDeselectInputField()
	{
		if (m_searchInputFieldSelected.value)
		{
			GameInput.InputManager instance = GameInput.InputManager.instance;
			if (instance.GetActionValue(ActionType.NavCancelBtnB) > 0f && instance.lastActionDevice != GameInput.DeviceType.Keyboard)
			{
				m_searchInputField.OnDeselect(null);
			}
		}
	}

	private void HandleItemsIconQueue()
	{
		if (m_queuedIconsItems.Count > 0 && !m_isAnyIconProceeding)
		{
			UGCBrowserListItemData uGCBrowserListItemData = m_queuedIconsItems.Dequeue();
			m_isAnyIconProceeding = true;
			uGCBrowserListItemData.LoadIcon(delegate
			{
				m_isAnyIconProceeding = false;
			});
		}
	}

	private void OnSearchInputFieldSelectedHandler()
	{
		UpdateBottomPanel();
	}

	private void OnSearchInputFieldEndEditHandler(string result)
	{
		if (result == LocalizationKeys.placeholder_input_text.Localized() || string.IsNullOrWhiteSpace(result))
		{
			result = string.Empty;
		}
		m_sharingFilterController.SetFilterValue(TracksSharingFilterType.TrackName, result);
		DeferredApplyFilterHandler().Forget();
	}

	private async UniTask DeferredApplyFilterHandler()
	{
		await UniTask.Delay(200);
		OnApplyFilterHandler();
		m_searchInputField.OnDeselect(null);
	}

	private void OnSearchInputFieldDeselectHandler(string _)
	{
		TryReleaseLockOnInputField();
	}

	private void OnSearchInputFieldChangedHandler(string text)
	{
		m_searchInputField.text = UIUtils.FilterText(text);
	}

	private void TryReleaseLockOnInputField()
	{
		if (GameInput.InputManager.GetLockedInputObject() == m_searchInputField)
		{
			GameInput.InputManager.LockInput(null);
		}
	}

	public void OnSearchInputFieldSelectHandler(string _)
	{
		SelectInputField();
	}

	private void SelectInputField()
	{
		m_itemsGridList.SelectionAll(selected: false);
		m_searchInputFieldSelected.value = true;
		TryLockOnInputField();
	}

	private void TryLockOnInputField()
	{
		if (GameInput.InputManager.GetLockedInputObject() != m_searchInputField)
		{
			GameInput.InputManager.LockInput(m_searchInputField);
		}
	}

	protected override void OnDeactivate()
	{
		m_showEmptyMsg.value = false;
		ContextOwner contextOwner = base.owner;
		contextOwner.onContextActiveChanged = (Action<BaseContext>)Delegate.Remove(contextOwner.onContextActiveChanged, new Action<BaseContext>(OnContextActiveChangedHandler));
		m_searchInputFieldSelected.OnValueChanged -= OnSearchInputFieldSelectedHandler;
		m_ugcDetailsVisibility.value = false;
		m_itemDetailsVisibility.value = false;
		m_showInputField.value = false;
		UnsubscribeTMPInputHandlers();
		ReleaseFilterInputDialogHandlers();
		ResetSearchByFilterLabels();
		UnsubscribeListEvents();
		HideTopPanel();
		HideBottomPanel();
		DisposeResources();
		SetEntryLoading(needActivate: false);
		ResetPageSelectionIndexes();
		base.OnDeactivate();
	}

	private void SubscribeTMPInputHandlers()
	{
		m_searchInputField.onSelect.AddListener(OnSearchInputFieldSelectHandler);
		m_searchInputField.onDeselect.AddListener(OnSearchInputFieldDeselectHandler);
		m_searchInputField.onEndEdit.AddListener(OnSearchInputFieldEndEditHandler);
		m_searchInputField.onValueChanged.AddListener(OnSearchInputFieldChangedHandler);
	}

	private void UnsubscribeTMPInputHandlers()
	{
		m_searchInputField.onSelect.RemoveAllListeners();
		m_searchInputField.onDeselect.RemoveAllListeners();
		m_searchInputField.onEndEdit.RemoveAllListeners();
		m_searchInputField.onValueChanged.RemoveAllListeners();
	}

	private void TryInitFilterInputDialogHandlers(UISharingTracksFilterContext filterContext)
	{
		if (!(m_filterCtx != null))
		{
			m_filterCtx = filterContext;
			UISharingTracksFilterContext filterCtx = m_filterCtx;
			filterCtx.onOpenInputDialog = (System.Action)Delegate.Combine(filterCtx.onOpenInputDialog, new System.Action(OnOpenInputDialogHandler));
			UISharingTracksFilterContext filterCtx2 = m_filterCtx;
			filterCtx2.onClosedInputDialog = (System.Action)Delegate.Combine(filterCtx2.onClosedInputDialog, new System.Action(OnCloseInputDialogHandler));
		}
	}

	private void ReleaseFilterInputDialogHandlers()
	{
		if (!(m_filterCtx == null))
		{
			UISharingTracksFilterContext filterCtx = m_filterCtx;
			filterCtx.onOpenInputDialog = (System.Action)Delegate.Remove(filterCtx.onOpenInputDialog, new System.Action(OnOpenInputDialogHandler));
			UISharingTracksFilterContext filterCtx2 = m_filterCtx;
			filterCtx2.onClosedInputDialog = (System.Action)Delegate.Remove(filterCtx2.onClosedInputDialog, new System.Action(OnCloseInputDialogHandler));
			m_filterCtx = null;
		}
	}

	private void TryDisposeCancellationToken(ref CancellationTokenSource cancellationToken)
	{
		if (cancellationToken != null && !cancellationToken.IsCancellationRequested)
		{
			cancellationToken.Cancel();
			cancellationToken.Dispose();
			cancellationToken = null;
		}
	}

	private bool IsTokenSourceDisposed(ref CancellationTokenSource cancellationToken)
	{
		if (cancellationToken != null)
		{
			return cancellationToken.IsCancellationRequested;
		}
		return true;
	}

	private void DisposeResources()
	{
		TryDisposeCancellationToken(ref m_filterProceedCancellationToken);
		TryDisposeCancellationToken(ref m_waitFetchingPageCancellationToken);
		DisposeItemsDataResources();
		m_itemsGridList.Clear();
		m_fetchedItems.Clear();
	}

	private void DisposeItemsDataResources()
	{
		foreach (UGCBrowserListItemData s_itemsDatum in s_itemsData)
		{
			s_itemsDatum.Dispose();
		}
		s_itemsData.Clear();
		m_queuedIconsItems.Clear();
		m_isAnyIconProceeding = false;
	}

	private void UpdateCarSpecsDescription()
	{
		int lastCarId = m_prefs.globals.lastCarId;
		PlayerCar byId = m_model.GetById<PlayerCar>(lastCarId);
		int profileIdForCar = m_prefs.carSettings.GetProfileIdForCar(lastCarId);
		PlayerCarProfile byId2 = m_model.GetById<PlayerCarProfile>(profileIdForCar);
		CarSwap swap = m_car3dView.currentCar.swap;
		m_setupIcon.value = (swap.isStockEngine ? m_setupsAtlas.GetSprite(byId2.GetProfileIconName()) : SwapsAsset.LoadAndInitDB().Find(swap.engineId).logo);
		m_carName.value = byId.name;
	}

	private void OnContextActiveChangedHandler(BaseContext ctx)
	{
		ProcessFilterContext(ctx);
		void ProcessFilterContext(BaseContext baseContext)
		{
			if (baseContext is UISharingTracksFilterContext uISharingTracksFilterContext)
			{
				TryInitFilterInputDialogHandlers(uISharingTracksFilterContext);
				if (uISharingTracksFilterContext.isActive)
				{
					uISharingTracksFilterContext.onApplyFilter = (System.Action)Delegate.Combine(uISharingTracksFilterContext.onApplyFilter, new System.Action(OnApplyFilterHandler));
				}
				else
				{
					uISharingTracksFilterContext.onApplyFilter = (System.Action)Delegate.Remove(uISharingTracksFilterContext.onApplyFilter, new System.Action(OnApplyFilterHandler));
				}
			}
		}
	}

	private void ResetSearchByFilterLabels()
	{
		m_searchByFilterLabelActivity.value = false;
		m_filterProgressPercent.value = string.Empty;
	}

	private void OnApplyFilterHandler()
	{
		ResetSearchByFilterLabels();
		if (!m_sharingFilterController.IsActiveFilter())
		{
			ResetSearchInputField();
		}
		if (m_currFilter.Equals(m_sharingFilterController.sharingFilter))
		{
			if (m_itemsGridList.count > 0)
			{
				if (GameInput.InputManager.instance.lastController.type == ControllerType.Mouse)
				{
					m_searchInputFieldSelected.value = false;
				}
				else
				{
					SelectFirstElementOnLoadedGridList();
				}
			}
		}
		else
		{
			m_currFilter = m_sharingFilterController.sharingFilter.MemberwiseClone() as ISharingFilter;
			ProceedFilter().Forget();
		}
	}

	private void OnOpenInputDialogHandler()
	{
		m_searchInputField.enabled = false;
	}

	private void OnCloseInputDialogHandler()
	{
		m_searchInputField.enabled = true;
	}

	private void SelectFirstElementOnLoadedGridList()
	{
		m_itemsGridList.SelectionAll(selected: false);
		m_itemsGridList.Selection(0, selected: true);
		m_searchInputFieldSelected.value = false;
	}

	private async UniTask ProceedFilter()
	{
		TryDisposeCancellationToken(ref m_filterProceedCancellationToken);
		m_filterProceedCancellationToken = new CancellationTokenSource();
		await LoadItemList(byFilter: true);
		TryDisposeCancellationToken(ref m_filterProceedCancellationToken);
	}

	private void InitFilters()
	{
		if (m_sharingFilterController == null)
		{
			m_sharingFilterController = new SharingFilterController(m_prefs);
		}
		m_sharingFilterController.InitFilter<SharingTracksFilter>();
		m_currFilter = m_sharingFilterController.sharingFilter.MemberwiseClone() as ISharingFilter;
	}

	private void SetEntryLoading(bool needActivate)
	{
		m_entryLoadingVisibility.value = needActivate;
		m_trackContainerCanvasGroup.alpha = (needActivate ? 0f : 1f);
		if (needActivate)
		{
			HideBottomPanel();
		}
	}

	public void NextPage()
	{
		TryChangePage(m_currPageIndex + 1).Forget();
	}

	public void PrevPage()
	{
		TryChangePage(m_currPageIndex - 1).Forget();
	}

	private async UniTaskVoid TryChangePage(int newPageIndex)
	{
		if (newPageIndex != m_currPageIndex && !m_isChangingPage.value)
		{
			m_baseSharingPublication.ResetView();
			m_ugcDetailsVisibility.value = false;
			m_isChangingPage.value = true;
			m_showInputField.value = false;
			UpdatePageSwitchersVisibility();
			UpdateBottomPanel();
			await FillListByPage(newPageIndex);
			m_isChangingPage.value = false;
			m_ugcDetailsVisibility.value = true;
			m_showInputField.value = true;
			if (m_isAllItemsFetched || !IsTokenSourceDisposed(ref m_waitFetchingPageCancellationToken))
			{
				m_currPageIndex = newPageIndex;
				UpdatePageSwitchersVisibility();
				UpdateBottomPanel();
			}
		}
	}

	private void UpdatePageSwitchersVisibility()
	{
		int num = (m_isAllItemsFetched ? m_filteredItems.Count : m_ugcManager.GetFetchedItemsCount());
		m_nextPageSwitcherVisibility.value = (float)(m_currPageIndex + 1) < (float)num / 16f;
		m_prevPageSwitcherVisibility.value = m_currPageIndex - 1 >= 0;
	}

	private async UniTask FillListByPage(int pageIndex)
	{
		int startIndex = pageIndex * 16;
		int endIndex = startIndex + 16 - 1;
		if (pageIndex > m_maxFetchedPage)
		{
			if (!m_isAllItemsFetched)
			{
				m_waitFetchingPageCancellationToken?.Dispose();
				m_waitFetchingPageCancellationToken = new CancellationTokenSource();
				IEnumerable<UGCItem> enumerable = await m_ugcManager.GetFetchedItems(startIndex, 16, m_waitFetchingPageCancellationToken);
				if (IsTokenSourceDisposed(ref m_waitFetchingPageCancellationToken))
				{
					return;
				}
				foreach (UGCItem item in enumerable)
				{
					s_itemsData.Add(CreateItemData(item));
				}
			}
			else
			{
				for (int i = startIndex; i <= Mathf.Clamp(endIndex, 0, m_filteredItems.Count - 1); i++)
				{
					s_itemsData.Add(CreateItemData(m_filteredItems[i]));
				}
			}
			m_maxFetchedPage = pageIndex;
		}
		int num = 0;
		endIndex = Mathf.Clamp(endIndex, startIndex, s_itemsData.Count - 1);
		for (int j = startIndex; j <= endIndex; j++)
		{
			UGCBrowserListItemData uGCBrowserListItemData = s_itemsData[j];
			if (m_itemsGridList[num] != null)
			{
				m_itemsGridList[num] = uGCBrowserListItemData;
			}
			else
			{
				m_itemsGridList.Add(uGCBrowserListItemData);
			}
			num++;
		}
		if (num < 16)
		{
			m_itemsGridList.RemoveRange(num, 16 - num);
		}
		SelectFirstElementOnLoadedGridList();
	}

	private UGCBrowserListItemData CreateItemData(UGCItem ugcItem)
	{
		UGCBrowserListItemData uGCBrowserListItemData = new UGCBrowserListItemData(ugcItem, m_ugcManager, OnFinishedRequestItem);
		m_queuedIconsItems.Enqueue(uGCBrowserListItemData);
		return uGCBrowserListItemData;
	}

	private async UniTask LoadItemList(bool byFilter)
	{
		SetEntryLoading(needActivate: true);
		m_itemDetailsVisibility.value = false;
		m_showInputField.value = false;
		if (!m_isAllItemsFetched)
		{
			UpdateBottomPanel();
		}
		if (byFilter)
		{
			await UniTask.WaitUntil(() => m_isAllItemsFetched || IsTokenSourceDisposed(ref m_filterProceedCancellationToken));
			if (IsTokenSourceDisposed(ref m_filterProceedCancellationToken))
			{
				return;
			}
			DisposeItemsDataResources();
			await FillFilteredFetchedItems();
		}
		else
		{
			m_ugcManager.FetchUGCItems(OnCompletedFetchHandler);
		}
		m_itemsGridList.Clear();
		ResetPageSelectionIndexes();
		await FillListByPage(m_currPageIndex);
		if (m_isAllItemsFetched || !IsTokenSourceDisposed(ref m_waitFetchingPageCancellationToken))
		{
			m_showInputField.value = true;
			m_searchInputFieldSelected.value = m_itemsGridList.count == 0;
			SetEntryLoading(needActivate: false);
			UpdatePageSwitchersVisibility();
			UpdateEmptyMessageVisibility();
			UpdateBottomPanel();
		}
	}

	private void ResetPageSelectionIndexes()
	{
		m_maxFetchedPage = -1;
		m_currPageIndex = 0;
	}

	private void UpdateEmptyMessageVisibility()
	{
		m_showEmptyMsg.value = m_itemsGridList.count == 0;
		m_itemDetailsVisibility.value = !m_showEmptyMsg.value;
		m_ugcDetailsVisibility.value = !m_showEmptyMsg.value;
	}

	private async UniTask<bool> IsPassedThroughFilter(UGCItem item)
	{
		if (!m_sharingFilterController.IsActiveFilter())
		{
			return true;
		}
		if (!(m_sharingFilterController.sharingFilter is SharingTracksFilter sharingTracksFilter))
		{
			return true;
		}
		return await sharingTracksFilter.IsPassedThroughFilter(item);
	}

	private void ProceedSelectedItem(BaseListElementData elementData)
	{
		if (elementData is UGCBrowserListItemData itemData)
		{
			ShowExpandMenuToItem(itemData);
		}
		UpdateBottomPanel();
	}

	private void ShowExpandMenuToItem(UGCBrowserListItemData itemData)
	{
		if (!base.owner.IsActive<UIUGCExpandMenuContext>())
		{
			UIUGCExpandMenuContext.SetAsParamsMenu(isParamsMenu: false);
			base.owner.Push<UIUGCExpandMenuContext>();
		}
	}

	public void UpdateBottomPanel()
	{
		m_actionHints.Clear();
		if (!m_managerAuthenticated)
		{
			ShowBottomPanel(m_actionHints);
			return;
		}
		m_actionHints.Add(new ActionHintData
		{
			hint = LocalizationKeys.nav_hint_goto_back.Localized(),
			type = ActionType.NavCancelBtnB,
			action = OnCloseHandler
		});
		if (m_searchInputFieldSelected.value)
		{
			m_actionHints.Add(new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_select.Localized(),
				type = ActionType.NavSelectBtnA,
				action = OnSelectInputFieldHandler
			});
		}
		if (m_entryLoadingVisibility.value || m_isChangingPage.value)
		{
			ShowBottomPanel(m_actionHints, null, ignoreRepeat: true);
		}
		else if (!(m_itemsGridList.GetSelected() is UGCBrowserListItemData uGCBrowserListItemData))
		{
			AddSharingFilterHint();
			ShowBottomPanel(m_actionHints, null, ignoreRepeat: true);
		}
		else if (m_itemsGridList.count != 0)
		{
			_ = uGCBrowserListItemData.isCorrupted;
			_ = uGCBrowserListItemData.isInstalled;
			if (!uGCBrowserListItemData.isDownloading || m_ugcServiceId.value == 2)
			{
				m_actionHints.Add(new ActionHintData
				{
					hint = LocalizationKeys.nav_hint_select.Localized(),
					type = ActionType.NavSelectBtnA,
					action = OnSubmitHandler
				});
			}
			AddSharingFilterHint();
			if (m_ugcServiceId.value == 2)
			{
				m_actionHints.Add(new ActionHintData
				{
					hint = LocalizationKeys.ugc_manager_params_label.Localized(),
					type = ActionType.NavAdditional1BtnY,
					action = OnShowExpandMenuParams
				});
			}
			ShowBottomPanel(m_actionHints, null, ignoreRepeat: true);
		}
		void AddSharingFilterHint()
		{
			string hint = (m_sharingFilterController.IsActiveFilter() ? $"<color=yellow>{LocalizationKeys.active_filter.Localized()}</color>" : LocalizationKeys.filters.Localized());
			m_actionHints.Add(new ActionHintData
			{
				hint = hint,
				type = ActionType.NavAdditional0BtnX,
				action = OnPressFilterHandler
			});
		}
	}

	private void SubscribeListHandlers()
	{
		m_itemsGridList.OnClick += OnClickHandler;
		m_itemsGridList.OnSelectionChanged += OnSelectionChangedHandler;
	}

	private void UnsubscribeListEvents()
	{
		m_itemsGridList.OnClick -= OnClickHandler;
		m_itemsGridList.OnSelectionChanged -= OnSelectionChangedHandler;
	}

	private void OnCompletedFetchHandler()
	{
		FillFetchedItems().Forget();
	}

	private async UniTaskVoid FillFetchedItems()
	{
		m_fetchedItems.Clear();
		if (base.isActive)
		{
			List<UGCItem> fetchedItems = m_fetchedItems;
			fetchedItems.AddRange(await m_ugcManager.GetFetchedItems(0, m_ugcManager.GetFetchedItemsCount()));
			await FillFilteredFetchedItems();
			m_isAllItemsFetched = true;
		}
	}

	private async UniTask FillFilteredFetchedItems()
	{
		m_searchByFilterLabelActivity.value = m_sharingFilterController.IsActiveFilter();
		m_filteredItems.Clear();
		m_filterProgressPercent.value = "0%";
		int totalFetchedItemsCount = m_fetchedItems.Count;
		for (int i = 0; i < totalFetchedItemsCount; i++)
		{
			UGCItem item = m_fetchedItems[i];
			if (await IsPassedThroughFilter(item))
			{
				m_filteredItems.Add(item);
			}
			m_filterProgressPercent.value = $"{(int)((float)i / (float)totalFetchedItemsCount * 100f)}%";
		}
	}

	private void OnSelectionChangedHandler(BaseListElementData elementData)
	{
		if (elementData.selected)
		{
			SetSharingDetails(elementData);
			UIUGCExpandMenuContext.SetCurrentObservableItem(elementData as UGCBrowserListItemData);
		}
	}

	private void SetSharingDetails(BaseListElementData elementData)
	{
		UGCItem ugcItem = (elementData as UGCBrowserListItemData).ugcItem;
		m_baseSharingPublication.InitView(ugcItem);
		m_baseSharingPublication.SetOwnerStatus(ugcItem as IUGCOwnerSystem);
		m_baseSharingPublication.SetSubscridedCount(ugcItem as IUGCSubscridable);
		m_baseSharingPublication.SetMetaData(ugcItem as IUGCMeta);
		m_baseSharingPublication.SetScore(ugcItem as IUGCRatable);
	}

	private void OnFinishedRequestItem(BaseListElementData elementData, bool success, string errorMessage = null)
	{
		(elementData as UGCBrowserListItemData)?.UpdateProperties();
		if (!success)
		{
			UIMessageBox.ShowWarning(errorMessage, null, LocalizationKeys.Ok.Localized());
		}
		UpdateBottomPanel();
	}

	private void OnClickHandler(BaseListElementData elementData)
	{
		if (elementData.selected)
		{
			ProceedSelectedItem(elementData);
		}
		else
		{
			elementData.selected = true;
		}
		m_searchInputFieldSelected.value = false;
		UpdateBottomPanel();
	}

	private void OnSubmitHandler()
	{
		ProceedSelectedItem(m_itemsGridList.GetSelected());
	}

	private void OnShowExpandMenuParams()
	{
		if (!base.owner.IsActive<UIUGCExpandMenuContext>())
		{
			UIUGCExpandMenuContext.SetAsParamsMenu(isParamsMenu: true);
			base.owner.Push<UIUGCExpandMenuContext>();
		}
	}

	private void OnCloseHandler()
	{
		base.owner.HideAllExcept<UIMainMenuContext>();
	}

	private void OnSelectInputFieldHandler()
	{
		m_searchInputField.ActivateInputField();
		GameInput.InputManager.LockInput(m_searchInputField);
	}

	private void OnPressFilterHandler()
	{
		base.owner.Push<UISharingTracksFilterContext>().InitFilter(m_sharingFilterController);
	}

	[GameHotkey(ActionType.NavUp, false, true)]
	public void NavigationUp()
	{
		if (!m_entryLoadingVisibility.value && !m_searchInputFieldSelected.value)
		{
			SelectionProceedByDirection(GridListProperty.GridNavitation.Up);
		}
	}

	[GameHotkey(ActionType.NavDown, false, true)]
	public void NavigationDown()
	{
		if (!m_entryLoadingVisibility.value && !m_searchInputFieldSelected.value)
		{
			SelectionProceedByDirection(GridListProperty.GridNavitation.Down);
		}
	}

	[GameHotkey(ActionType.NavLeft, false, true)]
	public void NavigateLeft()
	{
		if (!m_entryLoadingVisibility.value)
		{
			int selectedIndex = m_itemsGridList.GetSelectedIndex();
			if (selectedIndex == 0 || selectedIndex % 4 == 0)
			{
				m_itemsGridList.SelectionAll(selected: false);
				m_searchInputFieldSelected.value = true;
			}
			else
			{
				SelectionProceedByDirection(GridListProperty.GridNavitation.Left);
			}
		}
	}

	[GameHotkey(ActionType.NavRight, false, true)]
	public void NavigateRight()
	{
		if (!m_entryLoadingVisibility.value && m_itemsGridList.count != 0)
		{
			if (m_searchInputFieldSelected.value)
			{
				SelectFirstElementOnLoadedGridList();
			}
			else
			{
				SelectionProceedByDirection(GridListProperty.GridNavitation.Right);
			}
		}
	}

	private void SelectionProceedByDirection(GridListProperty.GridNavitation direction)
	{
		if (!m_showEmptyMsg.value && !m_isChangingPage.value)
		{
			m_itemsGridList.Selection(direction);
			UpdateBottomPanel();
		}
	}
}
