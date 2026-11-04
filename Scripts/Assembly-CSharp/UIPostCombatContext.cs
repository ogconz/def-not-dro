using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using AchievementsSyncService;
using Binding;
using Binding.Base;
using DB;
using DB.Meta;
using DI;
using GameInput;
using GameOverlay;
using Quests;
using SharingSetups;
using SyncMultiplayer;
using UnityEngine;

[AudioFXBlocker(false)]
public class UIPostCombatContext : BaseBindingBehaviourTarget
{
	private RaceMode m_raceMode = RaceMode.Drift;

	private RaceResult m_raceResult;

	private GhostPlayer m_ghostPlayer;

	[Dependency]
	private GamePrefs m_prefs;

	[Dependency]
	private BaseModel m_model;

	[SerializeField]
	private UICapsResultContext m_capsResult;

	[SerializeField]
	private UIPostCombatRewardContext m_rewards;

	[SerializeField]
	private UIPostCombatRewardContext m_rewardsMP;

	[SerializeField]
	private GameObject[] m_panelsToShow;

	public readonly StringProperty caption = new StringProperty();

	public readonly IntProperty modeType = new IntProperty();

	public readonly IntProperty cupIndex = new IntProperty();

	public readonly StringProperty driftPoints = new StringProperty();

	public readonly StringProperty raceTime = new StringProperty();

	public readonly StringProperty coinsReward = new StringProperty();

	public readonly StringProperty expReward = new StringProperty();

	public readonly StringProperty expByCombo = new StringProperty();

	public readonly StringProperty expByCups = new StringProperty();

	public readonly StringProperty expByRace = new StringProperty();

	public readonly StringProperty line1Cost = new StringProperty();

	public readonly StringProperty line1Data = new StringProperty();

	public readonly StringProperty line2Cost = new StringProperty();

	public readonly StringProperty line2Data = new StringProperty();

	public readonly StringProperty line3Cost = new StringProperty();

	public readonly StringProperty line3Data = new StringProperty();

	public readonly StringProperty expFactor = new StringProperty();

	public readonly StringProperty coinsFactor = new StringProperty();

	public readonly BoolProperty hasReward = new BoolProperty();

	public readonly BoolProperty hasCup = new BoolProperty();

	public readonly BoolProperty isTimeattack = new BoolProperty(false);

	public readonly BoolProperty driftEventsVisible = new BoolProperty();

	public readonly BoolProperty raceFail = new BoolProperty(false);

	public readonly BoolProperty finishedOutOfTime = new BoolProperty(false);

	public readonly StringProperty eventSpeedCounter = new StringProperty();

	public readonly StringProperty eventSideChangeCounter = new StringProperty();

	public readonly StringProperty eventAngleCounter = new StringProperty();

	public readonly StringProperty eventAccuracyCounter = new StringProperty();

	public readonly StringProperty eventQualityCounter = new StringProperty();

	public readonly StringProperty eventJumpCounter = new StringProperty();

	public readonly StringProperty eventBackwardCounter = new StringProperty();

	public readonly StringProperty eventKissTheWallCounter = new StringProperty();

	public readonly ListProperty challengesList = new ListProperty();

	public readonly BoolProperty challengesListVisible = new BoolProperty();

	private BaseRaceModeState.DetailedRaceAward m_raceAward;

	private int m_coinsGained;

	private int m_xpGained;

	protected override void OnActivate()
	{
		base.OnActivate();
		ShowTopPanel(ETopPanelState.Title, LocalizationKeys.mp_battle_end_result.Localized());
		ShowBottomPanel(new ActionHintData[2]
		{
			new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_exit.Localized(),
				type = ActionType.NavCancelBtnB,
				action = GotoGarage
			},
			new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_select.Localized(),
				type = ActionType.NavSelectBtnA,
				action = SubmitInput
			}
		});
		if (m_ghostPlayer != null)
		{
			GhostPlayer ghostPlayer = m_ghostPlayer;
			ghostPlayer.onPlayEndEvent = (System.Action)Delegate.Remove(ghostPlayer.onPlayEndEvent, new System.Action(OnPlayEndEvent));
			GhostPlayer ghostPlayer2 = m_ghostPlayer;
			ghostPlayer2.onPlayEndEvent = (System.Action)Delegate.Combine(ghostPlayer2.onPlayEndEvent, new System.Action(OnPlayEndEvent));
			m_ghostPlayer.Play();
		}
		if ((bool)CameraSwitch.instance)
		{
			DB.Meta.Track byId = m_model.GetById<DB.Meta.Track>(m_prefs.globals.lastTrackId);
			if (byId != null && byId.external && XRModeManager.instance.isHMDWearing)
			{
				CameraSwitch.instance.ActivateRaceCamera();
			}
			else
			{
				CameraSwitch.instance.ActivateFollowCamera();
			}
		}
		Overlay.NotifyMPFeature(notify: false);
		NetworkGame.USER_CREATED_ROOM_NAME = null;
		CarXDboxManager.instance?.Stop();
		StartCoroutine(ActivatePanels());
	}

	private IEnumerator ActivatePanels()
	{
		yield return null;
		GameObject[] panelsToShow = m_panelsToShow;
		for (int i = 0; i < panelsToShow.Length; i++)
		{
			panelsToShow[i].SetActive(value: true);
			yield return null;
		}
	}

	protected override void OnDeactivate()
	{
		if (m_ghostPlayer != null)
		{
			GhostPlayer ghostPlayer = m_ghostPlayer;
			ghostPlayer.onPlayEndEvent = (System.Action)Delegate.Remove(ghostPlayer.onPlayEndEvent, new System.Action(OnPlayEndEvent));
		}
		CarXDboxManager.instance?.Start();
		base.OnDeactivate();
	}

	public IEnumerator SetResult(BaseRaceModeState raceModeState, RaceMode raceMode, RaceResult raceResult, BaseRaceModeState.DetailedRaceAward raceAward)
	{
		yield return null;
		m_raceAward = raceAward;
		m_raceMode = raceMode;
		m_raceResult = raceResult;
		modeType.value = (int)m_raceMode;
		isTimeattack.value = m_raceMode == RaceMode.TimeAttack;
		hasReward.value = m_raceMode != RaceMode.Freeride && m_raceMode != RaceMode.Dyno;
		hasCup.value = m_raceMode == RaceMode.Drift || m_raceMode == RaceMode.TimeAttack;
		cupIndex.value = 0;
		finishedOutOfTime.value = raceAward.isOutOfTime;
		int carId = m_prefs.globals.lastCarId;
		int trackId = m_prefs.globals.lastTrackId;
		m_prefs.carSettings.GetEngineIdForCar(carId);
		int profileIdForCar = m_prefs.carSettings.GetProfileIdForCar(carId);
		m_model.GetById<PlayerCarProfile>(profileIdForCar);
		if (m_capsResult != null && hasCup.value)
		{
			IRaceNorm raceNorm = null;
			switch (m_raceMode)
			{
			case RaceMode.Drift:
				raceNorm = m_model.Get((TrackDriftRaceNormMeta x) => x.trackId == trackId);
				break;
			case RaceMode.TimeAttack:
				raceNorm = m_model.Get((TrackTimeAttackRaceNormMeta x) => x.trackId == trackId && x.carId == carId);
				break;
			}
			if (raceNorm == null)
			{
				raceNorm = new EmptyRaceNormMeta();
			}
			if (!raceAward.isOutOfTime)
			{
				float value = ((m_raceMode == RaceMode.Drift) ? ((float)raceAward.raceDriftPoints) : raceAward.raceTimeSpent);
				cupIndex.value = (int)raceNorm.GetCupByValue(value);
			}
			m_capsResult.SetInfo(raceNorm, cupIndex.value);
		}
		yield return null;
		raceFail.value = hasCup.value && cupIndex.value <= 0;
		raceTime.value = UIUtils.TimeToMSMString(raceAward.raceTimeSpent);
		driftPoints.value = UIUtils.DigitsWithSeparate(raceAward.raceDriftPoints);
		SetCaption(m_raceMode, cupIndex.value, raceAward.isOutOfTime);
		InitLists(m_raceMode);
		yield return null;
		if (m_capsResult != null && hasCup.value)
		{
			SendCupsStat(cupIndex.value);
		}
		if (hasReward.value)
		{
			if (m_raceMode == RaceMode.NetworkFreeride)
			{
				SetUpNetworkFreerideModeData();
			}
			else
			{
				SetupTotalAward(m_raceAward);
				ActivityFeedService.ProcessStory(ActivityFeedService.StoryType.GainRacePoints, m_raceAward.raceDriftPoints);
			}
		}
		if (!(this == null))
		{
			ObtainQuestRewardsAndShowUI();
		}
	}

	private void ObtainQuestRewardsAndShowUI()
	{
		UIPostCombatRewardContext uIPostCombatRewardContext = ((modeType.value == 5) ? m_rewardsMP : m_rewards);
		uIPostCombatRewardContext.Init();
		uIPostCombatRewardContext.AddCoinsReward(m_coinsGained, (int)m_prefs.coinsFactor);
		uIPostCombatRewardContext.AddXpReward(m_xpGained, (int)m_prefs.expFactor);
		List<(QuestsAsset.QuestMeta, List<QuestsAsset.QuestReward>)> list = CompleteQuests();
		QuestsAsset.QuestReward questReward = m_prefs.slotProgression.ConstructReward();
		if (questReward != null)
		{
			uIPostCombatRewardContext.AddReward(questReward.icon, questReward.id, questReward.count);
		}
		if (list == null)
		{
			return;
		}
		foreach (var item in list)
		{
			foreach (QuestsAsset.QuestReward item2 in item.Item2)
			{
				uIPostCombatRewardContext.AddReward(item2.icon, item2.id, item2.count);
			}
		}
	}

	private List<(QuestsAsset.QuestMeta, List<QuestsAsset.QuestReward>)> CompleteQuests()
	{
		QuestController.EventCompleted("sp_any_race_win", cupIndex.value, null, getRewards: false);
		return m_raceMode switch
		{
			RaceMode.TimeAttack => QuestController.EventCompleted("sp_time_attack_gold", cupIndex.value), 
			RaceMode.Drift => QuestController.EventCompleted("sp_drift_win", cupIndex.value), 
			_ => null, 
		};
	}

	private string GetFormatterEventCounter(int level, int id)
	{
		return UIUtils.DigitsWithSeparate(UIDriftEventsContext.countersEvents[level][id]);
	}

	private void InitLists(RaceMode raceMode)
	{
		driftEventsVisible.value = raceMode != RaceMode.TimeAttack;
		if (driftEventsVisible.value)
		{
			eventSpeedCounter.value = GetFormatterEventCounter(1, 1);
			eventSideChangeCounter.value = GetFormatterEventCounter(1, 2);
			eventAngleCounter.value = GetFormatterEventCounter(1, 3);
			eventAccuracyCounter.value = GetFormatterEventCounter(1, 4);
			eventQualityCounter.value = GetFormatterEventCounter(1, 5);
			eventJumpCounter.value = GetFormatterEventCounter(1, 6);
			eventBackwardCounter.value = GetFormatterEventCounter(1, 7);
			eventKissTheWallCounter.value = GetFormatterEventCounter(1, 12);
			UIDriftEventsContext.ResetStatsEvents();
		}
		if (raceMode == RaceMode.NetworkFreeride)
		{
			ICollection<NetworkGameStats.SubroomStat> subroomStats = NetworkController.InstanceGame.stats.GetSubroomStats();
			if (subroomStats.Count > 0)
			{
				int num = 0;
				int num2 = 0;
				PostcombatListItem.Data data = new PostcombatListItem.Data
				{
					title = LocalizationKeys.challenges.Localized(),
					value = ""
				};
				challengesList.Add(data);
				foreach (NetworkGameStats.SubroomStat item in subroomStats)
				{
					challengesList.Add(new PostcombatListItem.Data
					{
						title = LocalizationKeys.network_room_mode_.AddLocalized(item.mode.ToString().ToLower()),
						value = $"{item.tryCount}/{item.winCount}"
					});
					num += item.winCount;
					num2 += item.tryCount;
				}
				data.value = $"{num2}/{num}";
			}
		}
		challengesListVisible.value = challengesList.count > 0;
	}

	private void SetUpNetworkFreerideModeData()
	{
		NetworkGameStats stats = NetworkController.InstanceGame.stats;
		raceTime.value = UIUtils.TimeToMSMString(Time.realtimeSinceStartup - stats.gameStart);
		driftPoints.value = UIUtils.DigitsWithSeparate(stats.pointsAll);
		line3Data.value = UIUtils.DigitsWithSeparate(stats.pointsPerDrift);
		int num = (int)m_prefs.coinsFactor;
		coinsFactor.value = ((num > 1) ? $"{num}x" : "");
		int num2 = Mathf.RoundToInt((float)stats.pointsAll / 5f * m_prefs.locks.GetCarCurrentCoinsFactor() * (float)num);
		num2 = Mathf.RoundToInt(num2);
		if (num2 > 10000000)
		{
			coinsReward.value = "0";
		}
		m_prefs.money.coins += num2;
		coinsReward.value = UIUtils.DigitsWithSeparate(num2);
		m_coinsGained = num2;
		GameExpController gameExpController = DependencyInjector.Resolve<GameExpController>();
		int num3 = (int)m_prefs.expFactor;
		expFactor.value = ((num3 > 1) ? $"{num3}x" : "");
		int num4 = gameExpController.raceExp * num3;
		expReward.value = UIUtils.DigitsWithSeparate(num4);
		m_xpGained = num4;
		gameExpController.Apply();
	}

	private void SetupExpAward()
	{
		GameExpController gameExpController = DependencyInjector.Resolve<GameExpController>();
		int num = gameExpController.raceExp * (int)m_prefs.expFactor;
		expReward.value = UIUtils.DigitsWithSeparate(num);
		m_xpGained = num;
		int expBySource = gameExpController.GetExpBySource(GameExpController.ExpSource.Drift);
		expByCombo.value = UIUtils.DigitsWithSeparate(expBySource);
		int expBySource2 = gameExpController.GetExpBySource(GameExpController.ExpSource.Cup);
		expByCups.value = UIUtils.DigitsWithSeparate(expBySource2);
		int expBySource3 = gameExpController.GetExpBySource(GameExpController.ExpSource.Race);
		expByRace.value = UIUtils.DigitsWithSeparate(Mathf.Max(expBySource3, num - expBySource - expBySource2));
		gameExpController.Apply();
		float num2 = m_prefs.expFactor;
		expFactor.value = ((num2 > 1f) ? $"{num2}x" : "");
	}

	private void SetupTotalAward(BaseRaceModeState.DetailedRaceAward raceAward)
	{
		DrawCupAward(raceAward.cupAward);
		DrawDriftAward(raceAward.finishAward);
		line3Cost.value = UIUtils.AddWhiteDollarSignAtStart(UIUtils.DigitsWithSeparate(raceAward.leftTimeAward));
		line3Data.value = UIUtils.TimeToMSMString(raceAward.bonusTime);
		coinsReward.value = UIUtils.DigitsWithSeparate(raceAward.totalAward);
		m_coinsGained = raceAward.totalAward;
		float num = m_prefs.coinsFactor;
		coinsFactor.value = ((num > 1f) ? $"{num}x" : "");
		SetupExpAward();
	}

	private void DrawDriftAward(int raceFinishedCost)
	{
		line1Cost.value = UIUtils.AddWhiteDollarSignAtStart(UIUtils.DigitsWithSeparate(raceFinishedCost));
		if (!finishedOutOfTime.value)
		{
			if (raceFinishedCost > 0)
			{
				int value = raceFinishedCost / (int)(m_prefs.locks.GetCarCurrentCoinsFactor() * m_prefs.coinsFactor);
				line1Data.value = MakeXInfo(value, null, (int)(m_prefs.locks.GetCarCurrentCoinsFactor() * m_prefs.coinsFactor));
			}
			else
			{
				line1Data.value = string.Empty;
			}
		}
		else
		{
			line1Data.value = string.Empty;
		}
	}

	private void DrawCupAward(int cupAwardCost)
	{
		line2Cost.value = UIUtils.AddWhiteDollarSignAtStart(UIUtils.DigitsWithSeparate(cupAwardCost));
		if (cupAwardCost > 0)
		{
			int value = cupAwardCost / (int)(m_prefs.locks.GetCarCurrentCoinsFactor() * m_prefs.coinsFactor);
			line2Data.value = MakeXInfo(value, null, (int)(m_prefs.locks.GetCarCurrentCoinsFactor() * m_prefs.coinsFactor));
		}
		else
		{
			line2Data.value = string.Empty;
		}
	}

	private static string MakeXInfo(int value, string valueInfo, int x)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(UIUtils.DigitsWithSeparate(value));
		if (!string.IsNullOrEmpty(valueInfo))
		{
			stringBuilder.Append(valueInfo);
		}
		if (x > 1)
		{
			stringBuilder.Append(" x ");
			stringBuilder.Append(UIUtils.DigitsWithSeparate(x));
		}
		return stringBuilder.ToString();
	}

	private void SetCaption(RaceMode raceMode, int cupIndex, bool isOutOfTime)
	{
		switch (raceMode)
		{
		case RaceMode.Drift:
		case RaceMode.TimeAttack:
			if (isOutOfTime)
			{
				caption.value = LocalizationKeys.race_result_out_of_time.Localized();
				break;
			}
			switch (cupIndex)
			{
			case 1:
				caption.value = LocalizationKeys.cup_name_gold.Localized();
				break;
			case 2:
				caption.value = LocalizationKeys.cup_name_silver.Localized();
				break;
			case 3:
				caption.value = LocalizationKeys.cup_name_bronze.Localized();
				break;
			default:
				caption.value = LocalizationKeys.window_title_race_finish.Localized();
				break;
			}
			break;
		case RaceMode.Freeride:
		{
			LocalizationKeys.Key key = (UISetupsSharingContext.isSetupsSharingActive ? LocalizationKeys.nav_hint_goto_testdrive : LocalizationKeys.track_mode_training_title);
			caption.value = key.Localized();
			break;
		}
		case RaceMode.Dyno:
		case RaceMode.NetworkFreeride:
			caption.value = LocalizationKeys.window_title_race_finish.Localized();
			break;
		}
	}

	public void SetGhostPlayer(GhostPlayer ghostPlayer)
	{
		if (m_ghostPlayer != null)
		{
			GhostPlayer ghostPlayer2 = m_ghostPlayer;
			ghostPlayer2.onPlayEndEvent = (System.Action)Delegate.Remove(ghostPlayer2.onPlayEndEvent, new System.Action(OnPlayEndEvent));
		}
		m_ghostPlayer = ghostPlayer;
		if (m_ghostPlayer != null)
		{
			GhostPlayer ghostPlayer3 = m_ghostPlayer;
			ghostPlayer3.onPlayEndEvent = (System.Action)Delegate.Combine(ghostPlayer3.onPlayEndEvent, new System.Action(OnPlayEndEvent));
		}
	}

	private void OnPlayEndEvent()
	{
		if (m_ghostPlayer != null)
		{
			m_ghostPlayer.progress = 0f;
		}
	}

	public void SubmitInput()
	{
		NavigationInputModule.instance.Submit();
	}

	public void GotoLobby()
	{
		NetworkController.TryEnterToMultiplayer(base.owner);
	}

	public void GotoGarage()
	{
		m_ghostPlayer?.ClearPlayers();
		GarageGUIState.ExitToGarage();
	}

	protected override bool OnEscape()
	{
		GotoGarage();
		return true;
	}

	public void OnRetryClick()
	{
		BaseRaceModeState baseRaceModeState = States.mainCurrent as BaseRaceModeState;
		if ((bool)baseRaceModeState)
		{
			baseRaceModeState.RestartRace();
			AnalyticsService.LogRetryRace(m_raceMode);
		}
	}

	public void OnReplayClick()
	{
		HideTopPanel();
		HideBottomPanel();
		base.owner.HideStackAndPush<UIViewingReplayContext>().Init(m_ghostPlayer);
	}

	private void SendCupsStat(int cupIndex)
	{
		GameEvents.RaceCupEarned(new GameEvents.CupInfo(cupIndex));
		switch (cupIndex)
		{
		case 1:
			StatsManager.IncStat(StatsManager.EStats.CupsGold);
			break;
		case 2:
			StatsManager.IncStat(StatsManager.EStats.CupsSilver);
			break;
		case 3:
			StatsManager.IncStat(StatsManager.EStats.CupsBronze);
			break;
		}
		AchievementsSyncManager.SyncStatus(StatsManager.EStats.CupsAllGold);
	}
}
