using System.Collections.Generic;
using Binding;
using Binding.Base;
using DI;
using GameOverlay;
using GameOverlay.SubscriptionApi;
using SharingLiveries;
using SharingSetups;
using UnityEngine;

public class UITopPanelContext : BaseBindingBehaviourTarget
{
	[Dependency]
	private GamePrefs m_prefs;

	private ETopPanelState m_statePanel;

	public readonly Property<bool> visibleCategories = new BoolProperty(false);

	public readonly Property<bool> visiblePanel = new BoolProperty(startValue: true);

	public readonly Property<bool> visibleTitle = new BoolProperty(false);

	public readonly Property<bool> visibleLogo = new BoolProperty(false);

	public readonly Property<bool> subscriptionLabelVisibility = new BoolProperty(false);

	public readonly Property<int> subscriptionStatusType = new IntProperty(0);

	public readonly Property<string> subscriptionTimer = new StringProperty();

	public readonly Property<string> title = new StringProperty();

	public readonly StringCachedProperty<int> levelPlayer = new StringCachedProperty<int>();

	public readonly StringCachedProperty<int> driftTracksComplete = new StringCachedProperty<int>();

	public readonly StringCachedProperty<int> timeattackTracksComplete = new StringCachedProperty<int>();

	public readonly Property<string> ratingPlayer = new StringProperty();

	public readonly StringCachedProperty<int> moneyPlayer = new StringCachedProperty<int>();

	public readonly Property<Texture> avatar = new TextureProperty();

	public readonly Property<Sprite> rankSprite = new SpriteProperty(null);

	public readonly Property<bool> syncErrorIco = new BoolProperty();

	public readonly Property<bool> syncErrorTooltip = new BoolProperty();

	public readonly Property<string> syncErrorText = new StringProperty();

	public readonly Property<bool> syncErrorMPAllowed = new BoolProperty();

	public readonly BoolProperty showAgeErrorIcon = new BoolProperty();

	public readonly BoolProperty m_profileButton = new BoolProperty();

	private readonly BoolProperty m_hintActionVisibility = new BoolProperty(startValue: true);

	[SerializeField]
	private ElementNavigate m_userProfileNavigate;

	public static bool m_firstEnter = true;

	private int m_lastErrorFlags;

	private SubscriptionVisibilityState m_subscriptionVisibilityState;

	public const string m_syncErrorVar = "#h_syncerror_warn";

	public SwitcherActions switcherActions = new SwitcherActions();

	private static bool m_subscriptionEndShown;

	private static bool m_subscriptionVerifyFailedShown;

	public ETopPanelState currentState => m_statePanel;

	protected override void OnActivate()
	{
		base.OnActivate();
		SubscriptionOwnershipService.Start();
		SubscriptionOwnershipService.dlcUnlockDispatcher.onStatusUpdated += OnReceivedSubscriptionStatusChangedHandler;
		SubscriptionState<DLCCode> subscription;
		if (IsDlcSeasonPassSteamInstalled())
		{
			SetSeasonPassVisibilityState(SubscriptionVisibilityState.ActiveDuePlatformDlc);
		}
		else if (SubscriptionOwnershipService.dlcUnlockDispatcher.TryGetSubscription(DLCCode.SeasonPass, out subscription))
		{
			if (subscription.status != SubscriptionStatus.NotYetInited)
			{
				UpdateSubscriptionStatusVisibility(subscription.status);
			}
			else
			{
				SetSeasonPassVisibilityState(SubscriptionVisibilityState.Unverified);
			}
		}
		avatar.value = GameManager.GetMainPlayerAvatarNotNull();
		m_hintActionVisibility.value = true;
		m_lastErrorFlags = 0;
		RefreshAvatar();
		CheckConnection();
		UpdateValues();
		UpdateProfileButton();
	}

	protected override void OnDeactivate()
	{
		SubscriptionOwnershipService.dlcUnlockDispatcher.onStatusUpdated -= OnReceivedSubscriptionStatusChangedHandler;
		UpdateProfileButton();
		base.OnDeactivate();
	}

	public void UpdateProfileButton()
	{
		if (IsRestrictedContextActive())
		{
			m_profileButton.value = false;
			m_userProfileNavigate.enabled = false;
		}
		else if (!m_profileButton.value)
		{
			m_profileButton.value = true;
			m_userProfileNavigate.enabled = true;
		}
		UpdateSubscriptionLabelVisibility();
	}

	private void Update()
	{
		if (CheckIsConnectionStateChanged())
		{
			UpdateErrorMessage();
		}
		UpdateValues();
	}

	private void SetSeasonPassVisibilityState(SubscriptionVisibilityState state)
	{
		m_subscriptionVisibilityState = state;
		subscriptionStatusType.value = (int)state;
	}

	private void UpdateSubscriptionLabelVisibility()
	{
		SystemLanguage languageType = LocalizationService.languageMeta.languageType;
		subscriptionLabelVisibility.value = base.owner.IsActive<UIMainMenuContext>() && languageType != SystemLanguage.Polish && languageType != SystemLanguage.Japanese;
	}

	public void ShowDisabledSubscriptionDialog()
	{
		UIMessageBox.ShowSubscriptionDialogToBuy();
	}

	public void ShowOwnedSeasonPassDialog()
	{
		UIMessageBox.ShowMessageWithTitle(LocalizationKeys.subscription_active_info.Localized(), LocalizationKeys.subscription_dlc_owned_info.Localized());
	}

	public void ShowEnabledSubscriptionDialog()
	{
		UIMessageBox.ShowMessageWithTitle(LocalizationKeys.subscription_active_info.Localized(), LocalizationKeys.subscription_days_expiring.LocalizedFormat(TryGetRemainingDaysFromSubscription()));
	}

	public void ShowExpiringSubscriptionDialog()
	{
		UIMessageBox.ShowMessageWithTitle(LocalizationKeys.subscription_expiring.Localized(), LocalizationKeys.subscription_days_expiring.LocalizedFormat(TryGetRemainingDaysFromSubscription()));
	}

	public void ShowSubscriptionEndDialog()
	{
		UIMessageBox.ShowMessageWithTitle(LocalizationKeys.subscription_no_active_info.Localized(), LocalizationKeys.subscription_end_date.Localized());
		m_subscriptionEndShown = true;
	}

	private void OnConfigExpiredHandler()
	{
		UIMessageBox.ShowMessageWithTitle(LocalizationKeys.subscription_expiring.Localized(), LocalizationKeys.subscription_days_expiring.LocalizedFormat(TryGetRemainingDaysFromSubscription()));
	}

	public void ShowSubscriptionVerifyFailedDialog()
	{
		UIMessageBox.ShowMessageWithTitle(LocalizationKeys.subscription_verify_failed_title.Localized(), LocalizationKeys.subscription_check_connect.Localized());
		m_subscriptionVerifyFailedShown = true;
	}

	private int TryGetRemainingDaysFromSubscription()
	{
		if (SubscriptionOwnershipService.dlcUnlockDispatcher.TryGetSubscription(DLCCode.SeasonPass, out var subscription) && subscription.TryGetRemainingDays(out var daysCount))
		{
			return daysCount;
		}
		return 0;
	}

	private void OnReceivedSubscriptionStatusChangedHandler(IEnumerable<SubscriptionState<DLCCode>> enumerable)
	{
		foreach (SubscriptionState<DLCCode> item in enumerable)
		{
			if (item.subject == DLCCode.SeasonPass)
			{
				UpdateSubscriptionStatusVisibility(item.status);
			}
		}
	}

	private bool IsRestrictedContextActive()
	{
		if (!base.owner.IsActive<UIDynostandContext>() && !base.owner.IsActive<UIPaintCustomizationContext>() && !base.owner.IsActive<UIVinylsContext>() && !base.owner.IsActive<UIVinylsEditingContext>() && !base.owner.IsActive<UIVinylsEditingLayersContext>() && !base.owner.IsActive<UISetupsSharingContext>() && !base.owner.IsActive<UILiveriesSharingContext>() && !base.owner.IsActive<UIBodyCustomizationContext>() && !base.owner.IsActive<UISetupsContext>() && !base.owner.IsActive<UISelectorTrackContext>() && !base.owner.IsActive<UIUGCBrowserContext>())
		{
			return base.owner.IsActive<UICarSlotsContext>();
		}
		return true;
	}

	public override bool IsAllowNavigation()
	{
		return false;
	}

	private bool CheckIsConnectionStateChanged()
	{
		if (!Overlay.IsReady())
		{
			return false;
		}
		int connectionErrorFlags = GameSync.GetConnectionErrorFlags();
		bool result = connectionErrorFlags != m_lastErrorFlags;
		m_lastErrorFlags = connectionErrorFlags;
		return result;
	}

	public void SetActiveActionHintsVisibility(bool needActivate)
	{
		m_hintActionVisibility.value = needActivate;
	}

	private void UpdateErrorMessage()
	{
		bool flag = !GameSync.isAuthorized;
		syncErrorIco.value = flag || GameSync.isAuthorizationInProgress;
		if (!GameSync.isAuthorizationInProgress)
		{
			syncErrorMPAllowed.value = flag;
			showAgeErrorIcon.value = CheckOverlayAgeEligibility();
			syncErrorText.value = GameSync.GetCurrentSyncStateMessage();
			if (Overlay.instance.NeedUpdatePatch)
			{
				syncErrorMPAllowed.value = true;
				showAgeErrorIcon.value = false;
			}
		}
		else
		{
			syncErrorMPAllowed.value = false;
			showAgeErrorIcon.value = false;
			syncErrorText.value = string.Empty;
		}
	}

	public override bool IsHotkeysEnabled()
	{
		return switcherActions.listActions.Count > 0;
	}

	public void OnSwitchNextPage()
	{
		switcherActions.OnNextPage();
	}

	public void OnSwitchPrevPage()
	{
		switcherActions.OnPrevPage();
	}

	public void GoToProfile()
	{
		UIPlayerProfileContext.Show(base.owner);
	}

	public void ShowPanel(ETopPanelState newState, string title = null, HeaderActionData[] actions = null)
	{
		switcherActions.ClearActions();
		visiblePanel.value = true;
		switch (newState)
		{
		case ETopPanelState.Title:
			visibleTitle.value = !string.IsNullOrEmpty(title);
			visibleCategories.value = false;
			break;
		case ETopPanelState.Actions:
			visibleTitle.value = false;
			visibleCategories.value = true;
			switcherActions.AddActions(actions);
			switcherActions.Select(0);
			break;
		}
		this.title.value = title;
		BringForwardByDepth();
		UpdateProfileButton();
	}

	private bool IsDlcSeasonPassSteamInstalled()
	{
		if (Overlay.instance is SteamOverlay steamOverlay)
		{
			return steamOverlay.IsDlcSeasonPassInstalled();
		}
		return false;
	}

	private void UpdateSubscriptionStatusVisibility(SubscriptionStatus currSubcriptionStatus)
	{
		if (IsDlcSeasonPassSteamInstalled())
		{
			SetSeasonPassVisibilityState(SubscriptionVisibilityState.ActiveDuePlatformDlc);
			return;
		}
		switch (currSubcriptionStatus)
		{
		case SubscriptionStatus.Unknown:
		{
			if (!SubscriptionOwnershipService.dlcUnlockDispatcher.TryGetSubscriptionRecord(DLCCode.SeasonPass, out var subscriptionRecord2))
			{
				break;
			}
			if (subscriptionRecord2.status == SubscriptionStatus.Disabled)
			{
				SetSeasonPassVisibilityState(SubscriptionVisibilityState.Disabled);
				return;
			}
			if (subscriptionRecord2.status == SubscriptionStatus.Active)
			{
				SetSeasonPassVisibilityState(SubscriptionVisibilityState.Unverified);
				if (!m_subscriptionVerifyFailedShown)
				{
					ShowSubscriptionVerifyFailedDialog();
				}
				return;
			}
			break;
		}
		case SubscriptionStatus.Disabled:
		{
			if (SubscriptionOwnershipService.dlcUnlockDispatcher.TryGetSubscriptionRecord(DLCCode.SeasonPass, out var subscriptionRecord) && subscriptionRecord.status == SubscriptionStatus.Active)
			{
				SetSeasonPassVisibilityState(SubscriptionVisibilityState.Disabled);
				if (!m_subscriptionEndShown)
				{
					ShowSubscriptionEndDialog();
				}
				return;
			}
			break;
		}
		}
		SubscriptionVisibilityState subscriptionVisibilityState = default(SubscriptionVisibilityState);
		switch (currSubcriptionStatus)
		{
		case SubscriptionStatus.Unknown:
			subscriptionVisibilityState = SubscriptionVisibilityState.Unverified;
			break;
		case SubscriptionStatus.Active:
			subscriptionVisibilityState = SubscriptionVisibilityState.Active;
			break;
		case SubscriptionStatus.Disabled:
			subscriptionVisibilityState = SubscriptionVisibilityState.Disabled;
			break;
		default:
			global::_003CPrivateImplementationDetails_003E.ThrowSwitchExpressionException(currSubcriptionStatus);
			break;
		}
		SubscriptionVisibilityState subscriptionVisibilityState2 = subscriptionVisibilityState;
		SetSeasonPassVisibilityState(subscriptionVisibilityState2);
		if (subscriptionVisibilityState2 == SubscriptionVisibilityState.Active && TryGetSubscriptionEstimatedTimeString(out var timerValue))
		{
			subscriptionTimer.value = timerValue;
		}
	}

	private bool TryGetSubscriptionEstimatedTimeString(out string timerValue)
	{
		timerValue = string.Empty;
		if (SubscriptionOwnershipService.dlcUnlockDispatcher.TryGetSubscription(DLCCode.SeasonPass, out var subscription) && subscription.ownerships == null)
		{
			return false;
		}
		timerValue = LocalizationKeys.subscription_days_expiring.LocalizedFormat(subscription.TryGetRemainingDays(out var daysCount) ? daysCount : 0) ?? "";
		return true;
	}

	public static UITopPanelContext Show(ContextOwner owner)
	{
		if (owner == null)
		{
			UIUtils.LogNullReference("UITopPanelContext.Show", "owner");
			return null;
		}
		UITopPanelContext uITopPanelContext = (owner.IsActive<UITopPanelContext>() ? owner.Get<UITopPanelContext>() : owner.Add<UITopPanelContext>());
		if (uITopPanelContext != null)
		{
			uITopPanelContext.ShowPanel(ETopPanelState.Title);
		}
		return uITopPanelContext;
	}

	public void HidePanel()
	{
		visiblePanel.value = false;
	}

	private void CheckConnection()
	{
		CheckIsConnectionStateChanged();
		UpdateErrorMessage();
		if (!GameSync.isAuthorized && !GameSync.isAuthorizationInProgress)
		{
			if (m_firstEnter)
			{
				UIMessageBox.ShowHint(LocalizationKeys.sync_not_connect_extended.Localized(), LocalizationKeys.error.Localized(), "#h_syncerror_warn");
			}
			m_firstEnter = false;
		}
	}

	private void RefreshAvatar()
	{
		Overlay.OnPlayerAvatarDecoded.Subscribe(GameEventFlag.Global, delegate(Overlay.AvatarInfo e)
		{
			if (Overlay.instance.player.id == e.userId)
			{
				avatar.value = e.avatar;
			}
		});
	}

	private static bool CheckOverlayAgeEligibility()
	{
		if (Overlay.IsReady() && Overlay.instance.ConnectedToInternet())
		{
			return !Overlay.instance.IsValidAge;
		}
		return false;
	}

	public void UpdateValues()
	{
		if (m_prefs == null)
		{
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
		}
		driftTracksComplete.SetCached(m_prefs.stats.driftRacesPlayed);
		timeattackTracksComplete.SetCached(m_prefs.stats.timeAttackRacesPlayed);
		if (moneyPlayer.UpdateCache(m_prefs.money.coins))
		{
			moneyPlayer.value = UIUtils.AddWhiteDollarSignAtStart(UIUtils.DigitsWithSeparate(m_prefs.money.coins));
		}
		if (levelPlayer.UpdateCache(m_prefs.playerProfile.level))
		{
			levelPlayer.value = $"lvl {m_prefs.playerProfile.level}";
		}
	}
}
