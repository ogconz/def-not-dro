using CodeStage.AntiCheat.ObscuredTypes;
using DB.Meta;
using UnityEngine;

public class DriftRaceModeState : BaseRaceModeState
{
	protected ObscuredInt m_driftCoinsEarned;

	protected int m_driftCoinsStartValue;

	protected DriftController m_driftController;

	protected StatsCachedUpdater m_driftDistanceUpdater;

	public override int currentDriftPoints => (int)m_driftCoinsEarned + ((!(m_driftController == null)) ? m_driftController.pointsAndExtraPoints : 0) - m_driftCoinsStartValue;

	public override RaceMode raceMode => RaceMode.Drift;

	protected virtual bool allowDriftOnOutsideTrack => true;

	protected virtual bool allowRepeatDrift => false;

	public DriftController driftController => m_driftController;

	protected override void Init()
	{
		m_driftDistanceUpdater = new StatsCachedUpdater(StatsManager.EStats.DriftDistance, 50);
		base.Init();
		DB.Meta.Track byId = m_model.GetById<DB.Meta.Track>(m_globals.lastTrackId);
		if (byId != null)
		{
			m_totalTimeLimitSeconds = byId.totalTimeLimit;
		}
		ConfigureDriftController();
		ResetCoins();
	}

	public override void RegisterUserCar()
	{
		ConfigureDriftController();
		base.RegisterUserCar();
	}

	protected void ConfigureDriftController()
	{
		m_driftController = base.userCar.gameObject.GetOrAddComponent<DriftController>();
		m_driftController.Configure(allowDriftOnOutsideTrack, allowRepeatDrift, !allowDriftOnOutsideTrack, base.checkpointManager);
		m_driftController.earnedPointsEvent -= OnEarnedPointsEventHandle;
		m_driftController.earnedPointsEvent += OnEarnedPointsEventHandle;
	}

	protected void ResetCoins()
	{
		m_driftCoinsStartValue = Random.Range(0, 10000);
		m_driftCoinsEarned = m_driftCoinsStartValue;
	}

	protected override void Shutdown()
	{
		base.Shutdown();
		m_driftController.earnedPointsEvent -= OnEarnedPointsEventHandle;
		m_driftDistanceUpdater.SendCurrentToStat();
	}

	private void OnEarnedPointsEventHandle(int points)
	{
		m_driftCoinsEarned = (int)m_driftCoinsEarned + points;
	}

	public override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		if (m_driftController != null && m_driftController.isDrift)
		{
			m_driftDistanceUpdater.AddDelta(m_lastFrameDistance / 1000f);
		}
	}

	protected override void OnRaceCompletedHandle()
	{
		base.OnRaceCompletedHandle();
		if ((bool)m_driftController)
		{
			m_driftController.ForceEarnPoints();
			AnalyticsService.EarnDriftPoints(currentDriftPoints);
		}
	}

	protected override void CalculateAward(ref DetailedRaceAward award)
	{
		int trackId = m_globals.lastTrackId;
		TrackDriftRaceNormMeta trackDriftRaceNormMeta = m_model.Get((TrackDriftRaceNormMeta x) => x.trackId == trackId);
		TrackDriftRaceRewardMeta trackDriftRaceRewardMeta = m_model.Get((TrackDriftRaceRewardMeta x) => x.trackId == trackId);
		if (trackDriftRaceNormMeta != null && trackDriftRaceRewardMeta != null)
		{
			award.bonusTime = award.raceTimeLeft;
			if (award.isOutOfTime)
			{
				award.cupAward = 0;
				int num = trackDriftRaceRewardMeta.bronzeCoins * 30 / 100;
				float num2 = Mathf.Clamp01((float)award.raceDriftPoints / (float)trackDriftRaceNormMeta.bronzePoints);
				num = Mathf.RoundToInt((float)num * num2);
				award.finishAward = num;
			}
			else
			{
				award.finishAward = (int)((float)award.raceDriftPoints * m_prefs.locks.GetCarCurrentCoinsFactor() * m_prefs.coinsFactor);
				RaceCupType cupByValue = trackDriftRaceNormMeta.GetCupByValue(award.raceDriftPoints);
				award.cupAward = BaseRaceModeState.CalculateAward(trackDriftRaceRewardMeta.GetAwardByCup(cupByValue));
			}
			award.leftTimeAward = 0;
			if (!award.isOutOfTime && trackDriftRaceNormMeta.GetCupByValue(award.raceDriftPoints) != RaceCupType.None && (int)m_totalTimeLimitSeconds > 0)
			{
				float num3 = 0.4f * award.raceTimeLeft / (float)(int)m_totalTimeLimitSeconds;
				award.leftTimeAward = Mathf.Clamp(Mathf.RoundToInt(num3 * (float)award.totalAward), 0, int.MaxValue);
			}
		}
		else
		{
			GameLog.Print(AlertLevel.Warning, "Debug", "Track norms not found !");
		}
	}
}
