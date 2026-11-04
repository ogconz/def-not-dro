using System;
using System.Collections.Generic;
using System.Linq;
using Binding;
using Binding.Base;
using DB;
using DB.Meta;
using DI;
using GameInput;
using GameOverlay;
using GameOverlay.SubscriptionApi;
using PrefsModels;
using UnityEngine;
using UnityEngine.UI;

[GameHotkeyBlocker(true)]
[AudioFXBlocker(true)]
public class UIPlayerProfileContext : BaseBindingBehaviourTarget
{
	[Dependency]
	private GamePrefs m_prefs;

	[Dependency]
	private BaseModel m_model;

	private string m_password = string.Empty;

	private const int MAX_CHARS_CODE = 4;

	public readonly Property<Texture> avatar = new TextureProperty();

	public readonly StringProperty playerName = new StringProperty();

	public readonly IntProperty playerMoney = new IntProperty();

	public readonly StringProperty playerLevel = new StringProperty();

	public readonly StringProperty playerExp = new StringProperty();

	public readonly FloatProperty playerExpBar = new FloatProperty();

	public readonly StringProperty carCount = new StringProperty();

	public readonly StringProperty trackCount = new StringProperty();

	public readonly StringProperty driftRacesCount = new StringProperty();

	public readonly StringProperty timeattackRacesCount = new StringProperty();

	public readonly StringProperty maxPointsPerRace = new StringProperty();

	public readonly StringProperty maxPointsPerDrift = new StringProperty();

	public readonly StringProperty averagePoints = new StringProperty();

	public readonly StringProperty timeRacesCount = new StringProperty();

	public readonly StringProperty mpRacesCount = new StringProperty();

	public readonly StringProperty seasonPassBind = new StringProperty();

	public readonly IntProperty goldCups = new IntProperty();

	public readonly IntProperty silverCups = new IntProperty();

	public readonly IntProperty bronzeCups = new IntProperty();

	public readonly StringProperty cupsCount = new StringProperty();

	public readonly StringProperty timeInGame = new StringProperty();

	public readonly StringProperty traveledTitle = new StringProperty();

	public readonly StringProperty traveledValue = new StringProperty();

	public readonly StringProperty traveledInDriftTitle = new StringProperty();

	public readonly StringProperty traveledInDriftValue = new StringProperty();

	public readonly StringProperty driftScoreAggregate = new StringProperty();

	public readonly StringProperty mpWins = new StringProperty();

	public readonly StringProperty mpFriendWins = new StringProperty();

	public CarUsageStatisticsList m_carStatsController;

	protected override void OnActivate()
	{
		base.OnActivate();
		HideTopPanel();
		if (!base.owner.IsActive<UIBackgroundContext>())
		{
			base.owner.Add<UIBackgroundContext>();
		}
		BringForwardByDepth();
		ShowTopPanel(ETopPanelState.Title, LocalizationKeys.player_profile_wnd.Localized());
		ShowBottomPanel(new ActionHintData[2]
		{
			new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_goto_back.Localized(),
				type = ActionType.NavCancelBtnB,
				action = OnClickClose
			},
			new ActionHintData
			{
				hint = LocalizationKeys.reset_player_progess.Localized(),
				type = ActionType.NavBtnExpand,
				action = OnClickResetStats
			}
		});
		avatar.value = GameManager.GetMainPlayerAvatarNotNull();
		playerName.value = UIUtils.FilterInvalidTags(UIUtils.ConvertNGUIColorToRich(m_prefs.playerProfile.name));
		playerMoney.value = m_prefs.money.coins;
		int all = 0;
		int unlocked = 0;
		m_prefs.locks.GetAvailableAndAllCarsCount(out unlocked, out all);
		carCount.value = unlocked + " / " + all;
		m_prefs.locks.GetAvailableAndAllTracksCount(out unlocked, out all);
		trackCount.value = unlocked + " / " + all;
		goldCups.value = m_prefs.stats.cups1;
		silverCups.value = m_prefs.stats.cups2;
		bronzeCups.value = m_prefs.stats.cups3;
		driftRacesCount.value = UIUtils.DigitsWithSeparate(m_prefs.stats.driftRacesPlayed);
		timeattackRacesCount.value = UIUtils.DigitsWithSeparate(m_prefs.stats.timeAttackRacesPlayed);
		maxPointsPerRace.value = UIUtils.DigitsWithSeparate(Mathf.RoundToInt(m_prefs.stats.maxPointsPerRace));
		maxPointsPerDrift.value = UIUtils.DigitsWithSeparate(Mathf.RoundToInt(m_prefs.stats.maxPointsPerDrift));
		averagePoints.value = UIUtils.DigitsWithSeparate(Mathf.RoundToInt(m_prefs.stats.averagePointsPerRace));
		timeRacesCount.value = UIUtils.DigitsWithSeparate(m_prefs.stats.timeAttackRacesPlayed);
		mpRacesCount.value = UIUtils.DigitsWithSeparate(m_prefs.stats.MPRacesPlayed);
		cupsCount.value = "\ud83e\udd47" + m_prefs.stats.cups1 + "  \ud83e\udd48" + m_prefs.stats.cups2 + "  \ud83e\udd49" + m_prefs.stats.cups3;
		TryUpdateSeasonPassLabelText();
		string arg;
		if (m_prefs.globals.speedKmh)
		{
			arg = LocalizationKeys.player_profile_stats_unit_kmh.Localized();
			traveledTitle.value = LocalizationKeys.player_profile_stats_kilometers.Localized();
			traveledInDriftTitle.value = LocalizationKeys.player_profile_stats_kilometers_in_drift.Localized();
		}
		else
		{
			arg = LocalizationKeys.player_profile_stats_unit_mph.Localized();
			traveledTitle.value = LocalizationKeys.player_profile_stats_mile.Localized();
			traveledInDriftTitle.value = LocalizationKeys.player_profile_stats_mile_in_drift.Localized();
		}
		float num = (m_prefs.globals.speedKmh ? 1f : 1.60934f);
		StatsManager.GetStat(StatsManager.EStats.Travel, out float value);
		traveledValue.value = $"{Mathf.FloorToInt(value / num)} {arg}";
		StatsManager.GetStat(StatsManager.EStats.DriftDistance, out value);
		traveledInDriftValue.value = $"{Mathf.FloorToInt(value / num)} {arg}";
		driftScoreAggregate.value = StatsManager.GetStatAsString(StatsManager.EStats.DriftScore, "N0");
		mpWins.value = StatsManager.GetStatAsString(StatsManager.EStats.MpWins, "N0");
		mpFriendWins.value = StatsManager.GetStatAsString(StatsManager.EStats.MpFriendWins, "N0");
		GameManager.onMainPlayerAvatarChanged = (Action<Texture2D>)Delegate.Combine(GameManager.onMainPlayerAvatarChanged, new Action<Texture2D>(OnAvatarChange));
		PlayerProfileModel playerProfile = m_prefs.playerProfile;
		playerProfile.onPlayerExpChanged = (System.Action)Delegate.Combine(playerProfile.onPlayerExpChanged, new System.Action(OnPlayerExpChange));
		OnPlayerExpChange();
		if (!(m_carStatsController != null))
		{
			return;
		}
		List<PlayerCar> list = m_model.QueryAll<PlayerCar>();
		m_carStatsController.Clear();
		List<CarUsageStatListItem> list2 = new List<CarUsageStatListItem>();
		foreach (PlayerCar item2 in list)
		{
			if (item2.enabled && !item2.forcedDisable && !item2.name.Equals("void", StringComparison.OrdinalIgnoreCase))
			{
				m_prefs.stats.carMileage.TryGetValue(item2.id, out var value2);
				CarUsageStatListItem item = new CarUsageStatListItem
				{
					spriteName = item2.icon,
					name = item2.name,
					percent = 0f,
					kilometers = value2 * 1.60934f
				};
				list2.Add(item);
			}
		}
		float num2 = list2.Max((CarUsageStatListItem x) => x.kilometers);
		foreach (CarUsageStatListItem item3 in list2)
		{
			item3.percent = item3.kilometers / num2;
			m_carStatsController.AddItem(item3);
			item3.hours = SecondsToReadableString(Mathf.RoundToInt(item3.kilometers / 120f * 3600f));
		}
		m_carStatsController.Sort((CarUsageStatListItem x, CarUsageStatListItem y) => (!(x.kilometers > y.kilometers)) ? 1 : (-1));
		m_carStatsController.RecalcList();
	}

	private void TryUpdateSeasonPassLabelText()
	{
		if (Overlay.instance is SteamOverlay steamOverlay)
		{
			string value = string.Empty;
			SubscriptionState<DLCCode> subscription;
			switch (steamOverlay.IsDlcSeasonPassInstalled() ? SubscriptionStatus.Active : ((!SubscriptionOwnershipService.dlcUnlockDispatcher.TryGetSubscription(DLCCode.SeasonPass, out subscription)) ? SubscriptionStatus.Unknown : subscription.status))
			{
			case SubscriptionStatus.Unknown:
				value = "<color=#B3BFCF>" + LocalizationKeys.subscription_no_active_info.Localized() + "</color>";
				break;
			case SubscriptionStatus.Disabled:
				value = "<color=#A655B2>" + LocalizationKeys.subscription_no_active_info.Localized() + "</color>";
				break;
			case SubscriptionStatus.Active:
				value = "<color=#FFCC49>" + LocalizationKeys.subscription_active_info.Localized() + "</color>";
				break;
			}
			seasonPassBind.value = value;
		}
	}

	protected override void OnDeactivate()
	{
		foreach (CarUsageStatListItem item in m_carStatsController.items)
		{
			item.Release();
		}
		base.owner.Hide<UIBackgroundContext>();
		GameManager.onMainPlayerAvatarChanged = (Action<Texture2D>)Delegate.Remove(GameManager.onMainPlayerAvatarChanged, new Action<Texture2D>(OnAvatarChange));
		PlayerProfileModel playerProfile = m_prefs.playerProfile;
		playerProfile.onPlayerExpChanged = (System.Action)Delegate.Remove(playerProfile.onPlayerExpChanged, new System.Action(OnPlayerExpChange));
		base.OnDeactivate();
	}

	protected void Update()
	{
		timeInGame.value = SecondsToReadableString(m_prefs.stats.timeInGame);
	}

	protected static string SecondsToReadableString(long seconds)
	{
		TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
		if (timeSpan.Days <= 0)
		{
			return UIUtils.TimeToHMSString(seconds);
		}
		return timeSpan.Days.ToString("N0", UIUtils.numbersFormatter) + " " + UIUtils.GetDayString(timeSpan.Days) + " ";
	}

	public static void Show(ContextOwner owner)
	{
		if (owner == null)
		{
			UIUtils.LogNullReference("UIPlayerProfileContext.Show", "owner");
		}
		else if (owner.topContext.GetType() == typeof(UIPlayerProfileContext))
		{
			owner.topContext.Hide();
		}
		else
		{
			owner.HideStackAndPush<UIPlayerProfileContext>();
		}
	}

	private void GenerateResetCode()
	{
		m_password = string.Empty;
		m_password = UnityEngine.Random.Range((int)Mathf.Pow(10f, 3f), (int)Mathf.Pow(10f, 4f)).ToString();
	}

	public void OnClickResetStats()
	{
		GenerateResetCode();
		if (GameManager.IsLinuxOsPlatform())
		{
			ShowQuestionToReset();
		}
		else
		{
			ShowInputDialogToReset();
		}
	}

	private void ShowQuestionToReset()
	{
		UIMessageBox.ShowQuestion(LocalizationKeys.player_profile_reset_stats_message.Localized(), null, LocalizationKeys.Cancel.Localized(), delegate
		{
			GhostData.DeleteAllGhostData();
			StatsManager.ClearStats();
			GameConsole.TakeAll();
			IconGeneratorContext.ClearStorage();
			IconStorageSystem.ClearStorage();
			Hide();
			GameManager.GameRestart();
		}, LocalizationKeys.Yes.Localized());
	}

	private void ShowInputDialogToReset()
	{
		UIMessageBox.ShowInputDialog(LocalizationKeys.player_profile_reset_stats_message.Localized() + "\n" + m_password, string.Empty, null, InputField.ContentType.IntegerNumber, null, LocalizationKeys.Cancel.Localized(), OnClickOkWithCode, LocalizationKeys.Yes.Localized(), 4);
	}

	private void OnClickOkWithCode(string text)
	{
		if (m_password != string.Empty && text == m_password)
		{
			GhostData.DeleteAllGhostData();
			StatsManager.ClearStats();
			GameConsole.TakeAll();
			IconGeneratorContext.ClearStorage();
			IconStorageSystem.ClearStorage();
			Hide();
			GameManager.GameRestart();
		}
		Invoke("OnClickResetStats", 0.001f);
	}

	private void OnAvatarChange(Texture2D newAvatar)
	{
		avatar.value = newAvatar;
	}

	private void OnPlayerExpChange()
	{
		playerLevel.value = "lvl " + m_prefs.playerProfile.level;
		playerExp.value = UIUtils.DigitsWithSeparate(m_prefs.playerProfile.exp) + " / " + UIUtils.DigitsWithSeparate(m_prefs.playerProfile.nextLevelExp);
		playerExpBar.value = (float)(m_prefs.playerProfile.exp - m_prefs.playerProfile.prevLevelExp) / (float)(m_prefs.playerProfile.nextLevelExp - m_prefs.playerProfile.prevLevelExp);
	}

	public void OnClickShowRating()
	{
		Hide();
	}

	public void OnClickClose()
	{
		Hide();
	}

	[GameHotkey(ActionType.NavDown, false, false)]
	public void UpSelection()
	{
		if (!(m_carStatsController != null))
		{
			return;
		}
		if (m_carStatsController.GetCount() == 0)
		{
			m_carStatsController.selectedIndex = -1;
			return;
		}
		m_carStatsController.selectedIndex++;
		if (m_carStatsController.selectedIndex >= m_carStatsController.GetCount())
		{
			m_carStatsController.selectedIndex = m_carStatsController.GetCount() - 1;
		}
	}

	[GameHotkey(ActionType.NavUp, false, false)]
	public void DownSelection()
	{
		if (!(m_carStatsController != null))
		{
			return;
		}
		if (m_carStatsController.GetCount() == 0)
		{
			m_carStatsController.selectedIndex = -1;
			return;
		}
		m_carStatsController.selectedIndex--;
		if (m_carStatsController.selectedIndex < 0)
		{
			m_carStatsController.selectedIndex = 0;
		}
	}
}
