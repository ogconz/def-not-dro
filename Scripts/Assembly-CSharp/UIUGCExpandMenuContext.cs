using Binding;
using Cysharp.Threading.Tasks;
using GameInput;
using Services.UGC.Interfaces;
using Services.UGC.Utils;

[RaycastBlocker(true)]
[GameHotkeyBlocker(true)]
public class UIUGCExpandMenuContext : UIExpandMenuContext
{
	private ExpandMenuItemData m_subscribeItem;

	private ExpandMenuItemData m_selectItem;

	private ExpandMenuItemData m_rateItemUp;

	private ExpandMenuItemData m_rateItemDown;

	private ExpandMenuItemData m_showItemInWorkshop;

	private ExpandMenuItemData m_reportItem;

	private ExpandMenuItemData m_unsubscribeItem;

	private ExpandMenuItemData m_logOutItem;

	private ExpandMenuItemData m_exitItem;

	private static UGCBrowserListItemData m_currObservableItem;

	private static bool m_isParamsMenu;

	protected override void OnActivate()
	{
		base.OnActivate();
		FillMenuItemsData();
		BuildMenuItemsList();
		m_menuItems.Selection(0, selected: true);
	}

	protected override void OnDeactivate()
	{
		base.OnDeactivate();
		m_menuItems.SelectionAll(selected: false);
	}

	public static void SetCurrentObservableItem(UGCBrowserListItemData browserListItemData)
	{
		m_currObservableItem = browserListItemData;
	}

	public static void SetAsParamsMenu(bool isParamsMenu)
	{
		m_isParamsMenu = isParamsMenu;
	}

	private void FillMenuItemsData()
	{
		if (m_subscribeItem == null)
		{
			m_subscribeItem = new ExpandMenuItemData(LocalizationKeys.ugc_browser_subscribe.Localized());
			m_subscribeItem.OnClick += OnSubscribeItemClickHandler;
		}
		if (m_selectItem == null)
		{
			m_selectItem = new ExpandMenuItemData(LocalizationKeys.ugc_browser_launch_track.Localized());
			m_selectItem.OnClick += OnSelectItemClickHandler;
		}
		if (m_rateItemUp == null)
		{
			m_rateItemUp = new ExpandMenuVoteItemData(LocalizationKeys.rate.Localized(), ExpandMenuVoteItemData.VoteRate.Up);
			m_rateItemUp.OnClick += OnRateItemClickHandler;
		}
		if (m_rateItemDown == null)
		{
			m_rateItemDown = new ExpandMenuVoteItemData(LocalizationKeys.rate.Localized(), ExpandMenuVoteItemData.VoteRate.Down);
			m_rateItemDown.OnClick += OnRateItemClickHandler;
		}
		if (m_showItemInWorkshop == null)
		{
			m_showItemInWorkshop = new ExpandMenuItemData(LocalizationKeys.ugc_manager_show_item_in_workshop.Localized());
			m_showItemInWorkshop.OnClick += OnShowItemInWorkshopClickHandler;
		}
		if (m_reportItem == null)
		{
			m_reportItem = new ExpandMenuItemData(LocalizationKeys.nav_select_report.Localized());
			m_reportItem.OnClick += OnReportItemClickHandler;
		}
		if (m_unsubscribeItem == null)
		{
			m_unsubscribeItem = new ExpandMenuItemData(LocalizationKeys.ugc_browser_unsubscribe.Localized());
			m_unsubscribeItem.OnClick += OnUnsubscribeItemClickHandler;
		}
		if (m_logOutItem == null)
		{
			m_logOutItem = new ExpandMenuItemData(LocalizationKeys.ugc_manager_log_out_mod_io.Localized());
			m_logOutItem.OnClick += OnLogOutItemClickHandler;
		}
		if (m_exitItem == null)
		{
			m_exitItem = new ExpandMenuItemData(LocalizationKeys.nav_hint_exit.Localized());
			m_exitItem.OnClick += OnExitItemClickHandler;
		}
	}

	private void BuildMenuItemsList()
	{
		m_menuItems.Clear();
		if (!m_isParamsMenu)
		{
			BuildMenuItemList();
		}
		else
		{
			BuildMenuParamList();
		}
		m_menuItems.Add(m_exitItem);
	}

	private void BuildMenuItemList()
	{
		if (!m_currObservableItem.isCorrupted && !m_currObservableItem.isInstalled && !m_currObservableItem.isDownloading)
		{
			m_menuItems.Add(m_subscribeItem);
		}
		if (m_currObservableItem.isPlayable && !m_currObservableItem.isCorrupted)
		{
			m_menuItems.Add(m_selectItem);
		}
		if (m_currObservableItem.ugcItem is IUGCRatable { supportSetRate: not false })
		{
			m_menuItems.Add(m_rateItemUp);
			m_menuItems.Add(m_rateItemDown);
		}
		m_menuItems.Add(m_showItemInWorkshop);
		if (m_currObservableItem.ugcItem is IUGCReportable)
		{
			m_menuItems.Add(m_reportItem);
		}
		if (m_currObservableItem.isCorrupted || (m_currObservableItem.isInstalled && !m_currObservableItem.isDownloading))
		{
			m_menuItems.Add(m_unsubscribeItem);
		}
	}

	private void BuildMenuParamList()
	{
		m_menuItems.Add(m_logOutItem);
	}

	private void UpdateBottomPanelFromMainContext()
	{
		base.owner.Get<UIUGCBrowserContext>().UpdateBottomPanel();
	}

	private void OnSubscribeItemClickHandler(BaseListElementData _)
	{
		m_currObservableItem?.Install();
		base.owner.Hide<UIUGCExpandMenuContext>();
		UpdateBottomPanelFromMainContext();
	}

	private void OnSelectItemClickHandler(BaseListElementData _)
	{
		m_currObservableItem?.Launch();
		base.owner.Hide<UIUGCExpandMenuContext>();
	}

	private void OnRateItemClickHandler(BaseListElementData elementData)
	{
		if (elementData is ExpandMenuVoteItemData expandMenuVoteItemData)
		{
			ProceedRateItem(m_currObservableItem.ugcItem as IUGCRatable, expandMenuVoteItemData.rate).Forget();
		}
		base.owner.Hide<UIUGCExpandMenuContext>();
	}

	private void OnShowItemInWorkshopClickHandler(BaseListElementData _)
	{
		if (m_currObservableItem.ugcItem is IUGCMeta iUGCMeta)
		{
			iUGCMeta.ShowInOverlay();
		}
	}

	private void OnLogOutItemClickHandler(BaseListElementData _)
	{
		UGCServiceProvider.instance.GetServiceBy(m_currObservableItem.ugcItem.serviceId)?.authentication.Deauthenticate();
		base.owner.HideAllExcept<UIMainMenuContext>();
	}

	private async UniTaskVoid ProceedRateItem(IUGCRatable ratableItem, int rate)
	{
		UILockInputContext.EnableLock();
		bool num = await ratableItem.SetRateAsync(rate);
		UILockInputContext.DisableLock();
		GUICommonGodVoice.ShowText(num ? LocalizationKeys.ugc_browser_vote_result_success.Localized() : LocalizationKeys.ugc_browser_vote_result_error.Localized());
	}

	private void OnReportItemClickHandler(BaseListElementData _)
	{
		if (m_currObservableItem.ugcItem is IUGCReportable iUGCReportable)
		{
			iUGCReportable.Report();
		}
	}

	private void OnUnsubscribeItemClickHandler(BaseListElementData _)
	{
		m_currObservableItem?.Uninstall();
		base.owner.Hide<UIUGCExpandMenuContext>();
		UpdateBottomPanelFromMainContext();
	}

	private void OnExitItemClickHandler(BaseListElementData _)
	{
		NavigationCancel();
	}

	[GameHotkey(ActionType.NavUp, false, true)]
	public void NavigationUp()
	{
		m_menuItems.Selection(m_menuItems.GetSelectedIndex() - 1, selected: true);
	}

	[GameHotkey(ActionType.NavDown, false, true)]
	public void NavigationDown()
	{
		m_menuItems.Selection(m_menuItems.GetSelectedIndex() + 1, selected: true);
	}

	[GameHotkey(ActionType.NavSelectBtnA, false, true)]
	public void NavigationSubmit()
	{
		m_menuItems.GetSelected().Click();
	}

	[GameHotkey(ActionType.NavCancelBtnB, false, true)]
	public void NavigationCancel()
	{
		base.owner.Hide<UIUGCExpandMenuContext>();
	}
}
