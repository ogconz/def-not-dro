using System;
using CodeStage.AntiCheat.ObscuredTypes;
using DB;
using DB.Meta;
using DI;
using GameOverlay;
using SyncMultiplayer;
using UnityEngine;

public abstract class BaseRaceModeState : BaseRaceState
{
	public class DetailedRaceAward
	{
		public float raceTimeSpent;

		public float raceTimeLeft;

		public int raceDriftPoints;

		public int trackProgressPercent;

		public bool isOutOfTime;

		public ObscuredInt expFinishAward;

		public float bonusTime;

		public ObscuredInt finishAward;

		public ObscuredInt cupAward;

		public ObscuredInt leftTimeAward;

		public int totalAward => (int)finishAward + (int)cupAward + (int)leftTimeAward;

		public override string ToString()
		{
			return $"[DetailedRaceAward: totalAward={totalAward}, timeSpent={raceTimeSpent}, timeLeft={raceTimeLeft}, driftPoints={raceDriftPoints}, trackProgressPercent={trackProgressPercent}, " + $"outOfTime={isOutOfTime}, finishAward={finishAward}, cupAward={cupAward}, leftTimeAward={leftTimeAward}, expFinishAward={expFinishAward}]";
		}
	}

	private const string RACE_DURATION_TIMED_EVENT_NAME = "Race_Duration_Timed_Event";

	[Dependency]
	protected BaseModel m_model;

	[Dependency]
	private TransactionManager m_transactionManager;

	protected float? m_lapTime;

	protected RaceResult m_previousBest;

	private bool m_inited;

	protected float? m_prevPenaltyTime;

	protected bool m_penaltyGrowing;

	protected DetailedRaceAward m_currentAward;

	protected float m_lastFrameDistance;

	private StatsCachedUpdater m_distanceStatsUpdater;

	private UIPostCombatContext m_postCombatContext;

	private bool m_previousStateOfSwitchCamera;

	private NetworkGame m_game;

	public Action OnShutdown;

	private GhostPlayer.Player m_ghostPlayerCar;

	public Action<BaseRaceModeState, DetailedRaceAward> onFinishedEvent;

	public virtual int currentDriftPoints => 0;

	public float? currentPenaltyTime => m_prevPenaltyTime;

	public bool isPenaltyGrowing => m_penaltyGrowing;

	public abstract RaceMode raceMode { get; }

	protected virtual bool isGhostEnabled => true;

	protected virtual bool isGhostRecordEnabled => true;

	protected virtual bool isStoppingOnFinish => true;

	protected virtual bool isCountdownEnabled => true;

	protected virtual bool isStartingStopped => false;

	public bool IsPostcombatNowActive
	{
		get
		{
			if (m_postCombatContext != null)
			{
				return m_postCombatContext.isActive;
			}
			return false;
		}
	}

	public override void OnEnter()
	{
		base.OnEnter();
		m_game = NetworkController.InstanceGame;
		m_penaltyGrowing = false;
		m_prevPenaltyTime = 0f;
		m_eliminationTime = 3f;
		m_distanceStatsUpdater = new StatsCachedUpdater(StatsManager.EStats.Travel, 500);
		base.contextOwner.Preload<UICountDownContext>();
		base.contextOwner.Preload<UIFinishContext>();
		base.contextOwner.Preload<UIEliminationCountdownContext>();
		base.contextOwner.Preload<UIFreeCamContext>();
		base.contextOwner.Preload<UISettingsContext>();
		base.contextOwner.Preload<UIGeneralSettingsContext>();
		base.contextOwner.Preload<UIInputSettingsContext>();
		base.contextOwner.Preload<UIInputBindsSettingsContext>();
		base.contextOwner.Preload<UIPollingInputDialogContext>();
		if (raceMode != RaceMode.XDS)
		{
			m_postCombatContext = base.contextOwner.Preload<UIPostCombatContext>();
		}
		Init();
		SetPlayerControlState(state: false);
		SetCameraControlState(state: false);
		if ((bool)m_startLine && isCountdownEnabled)
		{
			base.contextOwner.Get<UICountDownContext>().DoStart(base.userCar, m_startLine);
		}
		if ((bool)UIXDSInfoContext.instance)
		{
			UIXDSInfoContext.instance.Init(base.gameObject.GetOrAddComponent<DriftControllerXDS>());
		}
		UIFreeCamContext uIFreeCamContext = base.contextOwner.Get<UIFreeCamContext>();
		uIFreeCamContext.SubscribeContextUI(base.raceContext);
		uIFreeCamContext.SubscribeContextUI(base.contextOwner.Get<UICountDownContext>());
		uIFreeCamContext.SubscribeContextUI(base.contextOwner.Get<UIHUDMiniMapContext>());
		uIFreeCamContext.SubscribeContextUI(base.contextOwner.Get<UIInputOverlayContext>());
		if (States.current is FreerideRaceModeState)
		{
			uIFreeCamContext.LoadCameraPoints();
		}
		UpdateRichPresence();
		base.userCar.lockState = true;
	}

	protected override void StartPlay()
	{
		if (base.userCar == null)
		{
			return;
		}
		base.StartPlay();
		base.userCar.lockState = false;
		SetAiControlState(!isStartingStopped);
		SetCameraControlState(!isStartingStopped);
		ShowUI(show: true);
		if (isStartingStopped)
		{
			OnRaceStartHandle(base.userCar);
		}
		int lastCarId = m_prefs.globals.lastCarId;
		int profileIdForCar = m_prefs.carSettings.GetProfileIdForCar(lastCarId);
		AnalyticsService.LogStartRace(m_model.GetNameById<DB.Meta.Track>(m_prefs.globals.lastTrackId), raceMode);
		if (raceMode != RaceMode.Dyno)
		{
			AnalyticsService.CarProfileUse(m_model.GetNameById<PlayerCarProfile>(profileIdForCar));
			AnalyticsService.LogDeviceUse();
			if (this is SyncNetFreerideRaceModeState)
			{
				AnalyticsService.StartMultiplayerRace(isMatchmaking: false);
			}
			else
			{
				AnalyticsService.StartSingleplayerRace();
			}
		}
	}

	public override void OnExit()
	{
		Shutdown();
		ShowUI(show: false);
		base.OnExit();
	}

	public override void OnPaused()
	{
		base.OnPaused();
		base.contextOwner.Get<UICountDownContext>().Pause();
		ShowUI(show: false);
		if (base.contextOwner.IsActive<UIEliminationCountdownContext>())
		{
			base.contextOwner.Hide<UIEliminationCountdownContext>();
		}
	}

	public override void OnResume()
	{
		base.OnResume();
		base.contextOwner.Get<UICountDownContext>().Unpause();
		ShowUI(show: true);
		if ((bool)CameraSwitch.instance)
		{
			CameraSwitch.instance.ActivateRaceCamera();
		}
		UpdateRichPresence();
	}

	public void UpdateRichPresence()
	{
		IRichPresence richPresence = Overlay.instance.richPresence;
		RichPresenceStatus richPresenceStatus = RichPresenceStatus.MainMenu;
		switch (raceMode)
		{
		case RaceMode.Drift:
		case RaceMode.TimeAttack:
		case RaceMode.Freeride:
		case RaceMode.XDS:
			richPresenceStatus = RichPresenceStatus.SinglePlayer;
			richPresence.SetVariable(RichPresenceVariables.SinglePlayerMode, raceMode);
			break;
		case RaceMode.NetworkFreeride:
			richPresenceStatus = RichPresenceStatus.MultiplayerFreeride;
			break;
		case RaceMode.Top32:
		case RaceMode.Top32NetRace:
		case RaceMode.Top32ViewReplay:
			richPresenceStatus = RichPresenceStatus.Championship;
			break;
		case RaceMode.Dyno:
			return;
		}
		richPresence.SetVariable(RichPresenceVariables.TrackName, LocalizationService.Localize(base.trackMeta.name));
		richPresence.Show(richPresenceStatus);
	}

	public override void OnFixedUpdate()
	{
		m_lastFrameDistance = 0f;
		base.OnFixedUpdate();
		if (m_isRaceCompleted || base.userCar == null || base.userCar.carX == null)
		{
			return;
		}
		float num = base.userCar.carX.speedMPS;
		if (num < 0.01f)
		{
			num = 0f;
		}
		if (num > 400f)
		{
			num = 0f;
		}
		m_lastFrameDistance = num * Time.deltaTime;
		m_distanceStatsUpdater.AddDelta(m_lastFrameDistance / 1000f);
		if (timeLeft < m_eliminationTime)
		{
			return;
		}
		float? prevPenaltyTime = base.checkpointManager.GetCurrentPenaltyTime(base.userCar);
		m_penaltyGrowing = false;
		if (prevPenaltyTime.HasValue)
		{
			if (m_prevPenaltyTime.HasValue)
			{
				m_penaltyGrowing = prevPenaltyTime.Value > m_prevPenaltyTime.Value;
			}
			m_prevPenaltyTime = prevPenaltyTime;
		}
	}

	protected override void PositionSpawnedCar()
	{
		if (isStartingStopped && m_startLine != null)
		{
			Transform getTransform = base.userCar.getTransform;
			getTransform.rotation = m_startLine.transform.rotation;
			getTransform.position = m_startLine.transform.position - getTransform.forward * base.userCar.lengthFront;
			base.userCar.AlignCarPositionAtSpawn();
			base.userCar.getRigidbody.velocity = Vector3.zero;
			base.userCar.getRigidbody.angularVelocity = Vector3.zero;
		}
		else
		{
			base.PositionSpawnedCar();
		}
	}

	protected override void ShowUI(bool show)
	{
		if (show)
		{
			base.contextOwner.Add<UICountDownContext>();
			base.contextOwner.Add<UIHUDMiniMapContext>();
		}
		else
		{
			UIFreeCamContext uIFreeCamContext = base.contextOwner.Get<UIFreeCamContext>();
			if ((bool)uIFreeCamContext)
			{
				uIFreeCamContext.HideAndAttach();
			}
			base.contextOwner.Hide<UICountDownContext>();
			base.contextOwner.Hide<UIHUDMiniMapContext>();
		}
		base.ShowUI(show);
	}

	private void QueryPreviousBestResult()
	{
		m_lapTime = null;
		m_previousBest = null;
		int lastTrackId = m_globals.lastTrackId;
		RaceResult raceResult = m_prefs.stats.GetRaceResult(lastTrackId, m_globals.lastCarId, m_prefs.carSettings.GetProfileIdForCar(m_globals.lastCarId));
		m_previousBest = raceResult;
	}

	private void OnRaceStartHandle(RaceCar car)
	{
		if (car == base.userCar)
		{
			SetAiControlState(state: false);
			SetPlayerControlState(state: true);
			SetCameraControlState(state: true);
			if (isGhostRecordEnabled)
			{
				m_ghostRecorder.SetRecordSource(car);
				m_ghostRecorder.Record();
			}
			m_ghostPlayer.progress = 0f;
			m_ghostPlayer.Play();
		}
	}

	protected override void OnRaceCompletedHandle()
	{
		base.OnRaceCompletedHandle();
		SetPlayerControlState(state: false);
		SetAiControlState(state: true);
		SetCameraControlState(state: false);
		m_lapTime = currentLapTime;
	}

	protected abstract void CalculateAward(ref DetailedRaceAward award);

	protected virtual void OnRaceEnd()
	{
		if (!m_isRaceEnded)
		{
			m_isRaceEnded = true;
			m_isRaceCompleted = true;
			m_currentAward = new DetailedRaceAward
			{
				raceTimeSpent = (currentLapTime.HasValue ? currentLapTime.Value : 0f),
				raceTimeLeft = ((m_runOutOfTime || timeLeft < 0f) ? 0f : timeLeft),
				raceDriftPoints = currentDriftPoints,
				isOutOfTime = m_runOutOfTime,
				trackProgressPercent = base.checkpointManager.GetCheckpointsPassedPercent(base.userCar)
			};
			CalculateAward(ref m_currentAward);
			Debug.Log("Race ended. Award: " + m_currentAward);
			ShowUI(show: false);
			StocktakingRace();
			if (raceMode != RaceMode.XDS)
			{
				StoreBestResult();
				TimeController.StartNow(m_postCombatContext.SetResult(this, raceMode, m_previousBest, m_currentAward));
			}
			if (raceMode == RaceMode.Freeride || raceMode.IsNetworkRace())
			{
				OnFinished();
			}
			else
			{
				ShowFinish();
			}
		}
	}

	protected virtual void ShowFinish()
	{
		if (!m_runOutOfTime)
		{
			base.contextOwner.Get<UIEliminationCountdownContext>().Stop();
		}
		base.contextOwner.Push<UIFinishContext>().Play(m_runOutOfTime);
	}

	public override void ForceFinishRace()
	{
		if (KeepAliveManager.isUIHidden)
		{
			KeepAliveManager.SetUIVisible(isVisible: true);
		}
		OnRaceCompletedHandle();
		OnRaceEnd();
	}

	public void ForceExitRace()
	{
		Shutdown();
	}

	public override void EliminationWarning()
	{
		if (raceMode != RaceMode.Freeride && raceMode != RaceMode.NetworkFreeride)
		{
			base.contextOwner.Add<UIEliminationCountdownContext>().Play();
		}
	}

	public void SuspendGhostRecorder(bool isSuspend)
	{
		if (isGhostRecordEnabled)
		{
			m_ghostRecorder.SetPauseRecord(isSuspend);
		}
	}

	public void UpdateCurrentGhost()
	{
		if (isGhostRecordEnabled)
		{
			m_ghostRecorder.RiseSuspended(isSuspend: true);
			m_ghostRecorder.AddRecordSource(base.userCar);
		}
	}

	private void OnShowGhostChanged(bool isShow)
	{
		if (isGhostEnabled && m_ghostPlayerCar != null)
		{
			m_ghostPlayerCar.visible = isShow;
		}
	}

	private void LoadLastGhostData()
	{
		if (isGhostEnabled)
		{
			GhostData ghostData = new GhostData();
			int lastCarId = m_globals.lastCarId;
			int profileIdForCar = m_prefs.carSettings.GetProfileIdForCar(m_globals.lastCarId);
			if (ghostData.Load(lastCarId, profileIdForCar, m_globals.lastTrackId, raceMode))
			{
				m_ghostPlayerCar = m_ghostPlayer.AddPlayer(new GhostPlayer.StartArgs
				{
					data = ghostData
				});
				m_ghostPlayerCar.visible = m_prefs.globals.showGhost;
			}
		}
	}

	public override void Reset(bool spawnCar = false, bool resetStatsEvents = true)
	{
		base.Reset(spawnCar, resetStatsEvents);
		base.checkpointManager.OnRaceStart -= OnRaceStartHandle;
		base.checkpointManager.OnRaceStart += OnRaceStartHandle;
		if (isStoppingOnFinish)
		{
			base.checkpointManager.OnRaceEnd -= OnRaceEnd;
			base.checkpointManager.OnRaceEnd += OnRaceEnd;
		}
		UIFinishContext uIFinishContext = base.contextOwner.Get<UIFinishContext>();
		if ((bool)uIFinishContext)
		{
			uIFinishContext.onFinishedEvent -= OnFinished;
			uIFinishContext.onFinishedEvent += OnFinished;
		}
	}

	protected virtual void Init()
	{
		if (!m_inited)
		{
			m_prefs.globals.ShowGhostChanged += OnShowGhostChanged;
			m_ghostPlayerCar = null;
			QueryPreviousBestResult();
			LoadLastGhostData();
			base.checkpointManager.OnRaceStart -= OnRaceStartHandle;
			base.checkpointManager.OnRaceStart += OnRaceStartHandle;
			if (isStoppingOnFinish)
			{
				base.checkpointManager.OnRaceEnd -= OnRaceEnd;
				base.checkpointManager.OnRaceEnd += OnRaceEnd;
			}
			UIFinishContext uIFinishContext = base.contextOwner.Get<UIFinishContext>();
			if ((bool)uIFinishContext)
			{
				uIFinishContext.onFinishedEvent -= OnFinished;
				uIFinishContext.onFinishedEvent += OnFinished;
			}
			m_currentAward = null;
			m_inited = true;
		}
	}

	protected virtual void Shutdown()
	{
		if (m_inited)
		{
			if (KeepAliveManager.isUIHidden)
			{
				KeepAliveManager.ToggleUIInRaceMode();
			}
			m_prefs.globals.ShowGhostChanged -= OnShowGhostChanged;
			m_ghostPlayerCar = null;
			m_distanceStatsUpdater.SendCurrentToStat();
			UICountDownContext uICountDownContext = base.contextOwner.Get<UICountDownContext>();
			if (uICountDownContext != null)
			{
				uICountDownContext.Stop();
			}
			base.checkpointManager.OnRaceStart -= OnRaceStartHandle;
			base.checkpointManager.OnRaceEnd -= OnRaceEnd;
			base.checkpointManager.ClearCars();
			UIFinishContext uIFinishContext = base.contextOwner.Get<UIFinishContext>();
			if (uIFinishContext != null)
			{
				uIFinishContext.StopAllEvents();
				uIFinishContext.onFinishedEvent -= OnFinished;
			}
			OnShutdown.SafeCall();
			m_inited = false;
		}
	}

	protected void OnFinished()
	{
		Shutdown();
		if (raceMode == RaceMode.XDS)
		{
			SetupXDSReplay();
		}
		else
		{
			SetupRaceReplay();
		}
		ShowPostCombat();
		onFinishedEvent.SafeCall(this, m_currentAward);
	}

	protected virtual void ShowPostCombat()
	{
		m_postCombatContext = base.contextOwner.Push<UIPostCombatContext>();
		m_postCombatContext.SetGhostPlayer(m_ghostPlayer);
	}

	private void StoreBestResult()
	{
		if (!m_lapTime.HasValue || m_globals.lastTrackId == -1 || m_globals.lastCarId == -1 || m_runOutOfTime || (base.userCar != null && base.userCar.IsPhysicsBroken()))
		{
			return;
		}
		RaceResult raceResult = GetRaceResult(m_lapTime.Value);
		if (m_prefs.stats.UpdateRaceTrialResult(raceResult))
		{
			m_previousBest = raceResult;
			if (m_recordedGhostData != null)
			{
				m_recordedGhostData.Save(m_globals.lastTrackId, raceMode);
			}
		}
	}

	private RaceResult GetRaceResult(float lapTime)
	{
		int profileIdForCar = m_prefs.carSettings.GetProfileIdForCar(m_globals.lastCarId);
		int lastTrackId = m_globals.lastTrackId;
		int recordDriftPoints = ((m_currentAward == null) ? currentDriftPoints : m_currentAward.raceDriftPoints);
		RaceResult raceResult = new RaceResult();
		raceResult.trackId = lastTrackId;
		raceResult.carId = m_globals.lastCarId;
		raceResult.profileId = profileIdForCar;
		raceResult.raceModeId = (int)raceMode;
		raceResult.recordDriftPoints = recordDriftPoints;
		raceResult.SetBestLapTime(lapTime);
		return raceResult;
	}

	private void StocktakingRace()
	{
		if (m_isRaceCompleted)
		{
			int totalAward = m_currentAward.totalAward;
			m_transactionManager.OnRaceEndAward(totalAward);
		}
		GameEvents.RaceEnd(new GameEvents.RaceEndInfo(m_isRaceCompleted, m_currentAward));
		AnalyticsService.LogEndRace(m_isRaceEnded, m_model.GetNameById<DB.Meta.Track>(m_prefs.globals.lastTrackId), raceMode);
	}

	public static int CalculateAward(int rawAward)
	{
		GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
		BaseModel baseModel = DependencyInjector.Resolve<BaseModel>();
		int lastTrackId = gamePrefs.globals.lastTrackId;
		DB.Meta.Track byId = baseModel.GetById<DB.Meta.Track>(lastTrackId);
		float num = gamePrefs.coinsFactor * gamePrefs.locks.GetCarCurrentCoinsFactor() * byId.coinsFactor;
		return Mathf.RoundToInt((float)rawAward * num);
	}
}
