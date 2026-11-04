using CodeStage.AntiCheat.ObscuredTypes;
using DB.Meta;
using TrackLayout.Data;
using UnityEngine;

public class HalloweenEventState : BaseRaceModeState
{
	private ObscuredInt m_pumpkinCount = 0;

	protected ObscuredInt m_driftCoinsEarned;

	protected int m_driftCoinsStartValue;

	protected DriftController m_driftController;

	private bool m_showedEliminationWarning;

	public EventSettings eventSettings { get; private set; }

	public override int currentDriftPoints => (int)m_driftCoinsEarned + ((!(m_driftController == null)) ? m_driftController.pointsAndExtraPoints : 0) - m_driftCoinsStartValue;

	public override RaceMode raceMode => RaceMode.HalloweenEvent;

	public override float timeLeft => (float)(int)m_totalTimeLimitSeconds - (currentLapTime.HasValue ? currentLapTime.Value : 0f);

	protected virtual bool allowDriftOnOutsideTrack => true;

	protected virtual bool allowRepeatDrift => false;

	public ObscuredInt pumpkinCount
	{
		get
		{
			return m_pumpkinCount;
		}
		set
		{
			m_pumpkinCount = value;
		}
	}

	public DriftController driftController => m_driftController;

	public override void OnEnter()
	{
		base.OnEnter();
		pumpkinCount = 0;
	}

	protected override void Init()
	{
		base.Init();
		eventSettings = ResourcesWrapper.Load<EventSettings>("HalloweenEventSettings");
		DB.Meta.Track byId = m_model.GetById<DB.Meta.Track>(m_globals.lastTrackId);
		if (byId != null)
		{
			TrackMetaConfig config = TrackMetaDB.GetConfig(byId.scene, byId.configuration, RaceMode.HalloweenEvent);
			m_totalTimeLimitSeconds = (int)config.data.networkSubraceTime;
		}
		ConfigureDriftController();
	}

	protected void ConfigureDriftController()
	{
		m_driftController = base.userCar.gameObject.GetOrAddComponent<DriftController>();
		m_driftController.Configure(allowDriftOnOutsideTrack, allowRepeatDrift, !allowDriftOnOutsideTrack, base.checkpointManager);
		m_driftController.earnedPointsEvent -= OnEarnedPointsEventHandle;
		m_driftController.earnedPointsEvent += OnEarnedPointsEventHandle;
		m_driftCoinsStartValue = Random.Range(0, 10000);
		m_driftCoinsEarned = m_driftCoinsStartValue;
	}

	protected override void Shutdown()
	{
		base.Shutdown();
		m_driftController.earnedPointsEvent -= OnEarnedPointsEventHandle;
		HideEliminationWarning();
	}

	public override void EliminationWarning()
	{
		if (!m_showedEliminationWarning)
		{
			m_showedEliminationWarning = true;
			base.contextOwner.Add<UIEliminationCountdownContext>().Play();
		}
	}

	public void HideEliminationWarning()
	{
		if (m_showedEliminationWarning)
		{
			m_showedEliminationWarning = false;
			UIEliminationCountdownContext uIEliminationCountdownContext = base.contextOwner?.Get<UIEliminationCountdownContext>();
			if (uIEliminationCountdownContext != null)
			{
				uIEliminationCountdownContext.Stop();
				uIEliminationCountdownContext.Hide();
			}
		}
	}

	private void OnEarnedPointsEventHandle(int points)
	{
		m_driftCoinsEarned = (int)m_driftCoinsEarned + points;
	}

	protected override void ShowPostCombat()
	{
		UIPostCombatCustomEventContext uIPostCombatCustomEventContext = base.contextOwner.Push<UIPostCombatCustomEventContext>();
		uIPostCombatCustomEventContext.SetGhostPlayer(m_ghostPlayer);
		uIPostCombatCustomEventContext.SetResult(raceMode, m_previousBest, m_currentAward);
	}

	public override void IncTimer(int delta)
	{
		base.IncTimer(delta);
		m_isWarned = false;
		HideEliminationWarning();
	}

	public override void OnUpdate()
	{
		base.OnUpdate();
	}

	public override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
	}

	protected override void OnRaceCompletedHandle()
	{
		base.OnRaceCompletedHandle();
	}

	protected override void CalculateAward(ref DetailedRaceAward award)
	{
		if (award.isOutOfTime)
		{
			return;
		}
		int num = 0;
		foreach (DB.Meta.Track item in m_model.QueryAll<DB.Meta.Track>())
		{
			if (!(item.configuration != "Default") && item.enabled && !item.hidden && (raceMode != RaceMode.HalloweenEvent || TrackMetaDB.HaveConfig(item.scene, raceMode)))
			{
				num++;
			}
		}
		if (!m_prefs.eventsData.halloweenLocksTracks.ContainsKey(m_prefs.globals.lastTrackId))
		{
			m_prefs.eventsData.halloweenLocksTracks.Add(m_prefs.globals.lastTrackId, pumpkinCount);
			m_prefs.eventsData.pumpkinsCount += pumpkinCount;
			StatsManager.AddToStat(StatsManager.EStats.HalloweenCountPumpkins2019, pumpkinCount);
		}
		else if (m_prefs.eventsData.halloweenLocksTracks[m_prefs.globals.lastTrackId] < (int)pumpkinCount)
		{
			int num2 = (int)pumpkinCount - m_prefs.eventsData.halloweenLocksTracks[m_prefs.globals.lastTrackId];
			m_prefs.eventsData.pumpkinsCount += num2;
			StatsManager.AddToStat(StatsManager.EStats.HalloweenCountPumpkins2019, num2);
		}
		if (m_prefs.eventsData.pumpkinsCount >= eventSettings.maxCountPumpkins && m_prefs.eventsData.halloweenLocksTracks.Count == num)
		{
			m_prefs.eventsData.halloweenComplete = true;
			StatsManager.IncStat(StatsManager.EStats.HalloweenWin);
		}
		m_prefs.eventsData.Save();
	}
}
