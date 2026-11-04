using System;
using System.Collections;
using System.Collections.Generic;
using AssetCore;
using CodeStage.AntiCheat.ObscuredTypes;
using Cysharp.Threading.Tasks;
using DB;
using DB.Meta;
using DI;
using GameInput;
using HUD;
using JukeBox;
using Minimaps;
using PrefsModels;
using SplineLogic;
using SyncMultiplayer;
using SyncMultiplayer.NetGameSystems.Collisions;
using TrackLayout.Data;
using Unity.Mathematics;
using UnityEngine;

public abstract class BaseRaceState : BaseGameState
{
	[Tooltip("Skip Continue button press")]
	public bool waitContinuePress;

	private readonly CheckpointManager m_checkpointManager = new CheckpointManager();

	private readonly Type[] m_unpausedContextTypes = new Type[7]
	{
		typeof(UIXDSPreCombatContext),
		typeof(UIXDSPreCombatWheelsContext),
		typeof(UIRaceHintsContext),
		typeof(UIFinishContext),
		typeof(UIWatchPlayerContext),
		typeof(UIPlayersWaitingContext),
		typeof(UIRadialMenuContext)
	};

	protected GhostRecorder m_ghostRecorder = new GhostRecorder();

	protected GhostPlayer m_ghostPlayer = new GhostPlayer();

	private RaceCar m_userCar;

	protected StartPosition[] m_startPositions;

	protected int m_currentStartPositionIndex;

	protected StartLine m_startLine;

	protected CheckpointsData m_checkpoints;

	[Dependency]
	protected GamePrefs m_prefs;

	protected GlobalModel m_globals;

	protected float? m_currentLapTime;

	protected ObscuredInt m_totalTimeLimitSeconds = 0;

	protected bool m_runOutOfTime;

	protected bool m_isRaceStarted;

	protected bool m_isRaceCompleted;

	protected ObscuredBool m_isRaceEnded = false;

	protected GhostData m_recordedGhostData;

	protected float m_eliminationTime;

	protected bool m_isWarned;

	private UILoadingScreenContext m_loadingContext;

	private bool m_needShowUI;

	protected PlayerCarFactory m_carFactory;

	private bool m_startPlayPressed;

	public bool forceIgnoreLoseFocus;

	public static bool NightModeActive;

	private bool? m_requestedPlayerCtrlState;

	private Map m_map;

	private DB.Meta.Track m_trackMeta;

	private CarXDboxManager m_carXDboxManager;

	public System.Action RaceEndedEvent;

	public static Action<RaceCar, RaceCar> onCarSwapped;

	protected StartPosition m_currentStartPosition => m_startPositions[math.clamp(m_currentStartPositionIndex, 0, m_startPositions.Length - 1)];

	protected virtual bool destroyCarByFinish => true;

	public GhostRecorder ghostRecorder => m_ghostRecorder;

	public bool carSwapRunning { get; private set; }

	public bool startPlayPressed => m_startPlayPressed;

	public bool isFreeCamActive => base.contextOwner.IsActive<UIFreeCamContext>();

	private bool loadingScreenActive => m_loadingContext != null;

	public virtual bool allowCarRespawnInRace { get; } = true;

	public DB.Meta.Track trackMeta => m_trackMeta;

	public UIRaceModeContext raceContext { get; private set; }

	public UIHUDMiniMapContext miniMapContext { get; private set; }

	public virtual float timeLeft => (float)(int)m_totalTimeLimitSeconds - (currentLapTime.HasValue ? currentLapTime.Value : 0f);

	public virtual float? currentLapTime => m_currentLapTime;

	public bool isRaceCompleted => m_isRaceCompleted;

	public bool isRaceEnded => m_isRaceEnded;

	public CheckpointManager checkpointManager => m_checkpointManager;

	public RaceCar userCar => m_userCar;

	public bool isExternalTrack
	{
		get
		{
			if (m_trackMeta != null)
			{
				return m_trackMeta.external;
			}
			return false;
		}
	}

	protected virtual bool IsReadyHideLoadingScreen { get; set; } = true;

	public override void OnEnter()
	{
		base.OnEnter();
		m_carXDboxManager = CarXDboxManager.instance;
		m_carXDboxManager?.Start();
		States.onStateActivate += OnPostEnter;
		m_globals = m_prefs.globals;
		raceContext = UIRaceModeContext.GetContext(base.contextOwner, this as BaseRaceModeState);
		miniMapContext = base.contextOwner.Preload<UIHUDMiniMapContext>();
		base.contextOwner.Preload<UIInputOverlayContext>();
		Reset(spawnCar: true);
		SoundController.instance.fxAudioPause = true;
		BaseModel baseModel = DependencyInjector.Resolve<BaseModel>();
		m_trackMeta = baseModel.GetById<DB.Meta.Track>(m_globals.lastTrackId);
		if (!m_trackMeta.external)
		{
			TimeController.StartNow(StartPrepareCache());
		}
		ChangeDayNightMode();
	}

	private void ChangeDayNightMode()
	{
		if ((bool)DayNightController.instance)
		{
			DayNightController.instance.SetDayNightMode(!NightModeActive);
		}
	}

	protected void OnPostEnter(BaseState state)
	{
		States.onStateActivate -= OnPostEnter;
		m_startPlayPressed = false;
		if (state == this)
		{
			if (loadingScreenActive)
			{
				UILoadingScreenContext loadingContext = m_loadingContext;
				loadingContext.ContinueClickEvent = (System.Action)Delegate.Combine(loadingContext.ContinueClickEvent, new System.Action(ContinuePlay));
				m_loadingContext.SetLoadedAndShowContinuePlay(IsWaitLoading);
			}
			else
			{
				StartPlay();
			}
		}
	}

	private bool IsWaitLoading()
	{
		if (CheckUserCarLoaded())
		{
			if (!(DayNightController.instance == null))
			{
				return DayNightController.instance.isReady;
			}
			return true;
		}
		return false;
	}

	private bool CheckUserCarLoaded()
	{
		if (!(m_userCar == null))
		{
			return m_userCar.IsVisualLoaded;
		}
		return true;
	}

	private void ContinuePlay()
	{
		TimeController.StartNow(DestroyLoadingScreenCoroutine());
		StartPlay();
	}

	private IEnumerator DestroyLoadingScreenCoroutine()
	{
		yield return new WaitUntil(() => IsReadyHideLoadingScreen);
		DestroyLoadingContext();
		OnLoadingContextDestroy();
	}

	protected void DestroyLoadingContext()
	{
		if (loadingScreenActive)
		{
			UILoadingScreenContext loadingContext = m_loadingContext;
			loadingContext.ContinueClickEvent = (System.Action)Delegate.Remove(loadingContext.ContinueClickEvent, new System.Action(ContinuePlay));
			m_loadingContext.HideAndDestroy();
			m_loadingContext = null;
		}
	}

	public virtual void IncTimer(int delta)
	{
		m_totalTimeLimitSeconds = (int)m_totalTimeLimitSeconds + delta;
	}

	public void ForceContinuePlay()
	{
		if (!m_startPlayPressed)
		{
			ContinuePlay();
		}
	}

	protected virtual void StartPlay()
	{
		SoundController.instance.fxAudioPause = false;
		PlayMusic();
		m_startPlayPressed = true;
		SoundController.StopFX(FXSample.RaceStartSound);
		InputManager.TryShowBindStarterIgnitionMsg(needShow: true);
	}

	public override void OnPaused()
	{
		if (KeepAliveManager.isUIHidden)
		{
			KeepAliveManager.SetUIVisible(isVisible: true);
		}
	}

	protected virtual void PlayMusic()
	{
		SoundController.PlayMusic(MusicSignal.Race);
	}

	public virtual void RestartRace()
	{
		if (m_trackMeta != null && m_trackMeta.external)
		{
			States.State<LoadRaceState>().LoadLocalRace();
			return;
		}
		ConfigurationManager instance = ConfigurationManager.instance;
		States.State<LoadRaceState>().LoadLocalRace(instance?.selectedTrack);
	}

	public void ResetMap()
	{
		if (m_map == null)
		{
			m_map = UnityEngine.Object.FindObjectOfType<Map>();
		}
		m_map.ResetPlacement();
	}

	private IEnumerator StartPrepareCache()
	{
		do
		{
			m_map = UnityEngine.Object.FindObjectOfType<Map>();
		}
		while (!(m_map != null));
		yield return new WaitForEndOfFrame();
		RestoreManager.instance.Initialize(m_map);
	}

	protected virtual void InitStartPositions(ConfigurationManager configManager)
	{
		StartPointsData dataByType = configManager.GetDataByType<StartPointsData>();
		m_startPositions = dataByType.GetPoints();
		m_currentStartPositionIndex = Mathf.Clamp(m_prefs.globals.lastRoomSpawnPoint, 0, m_startPositions.Length - 1);
	}

	public virtual void Reset(bool spawnCar = false, bool resetStatsEvents = true)
	{
		if (resetStatsEvents)
		{
			UIDriftEventsContext.ResetStatsEvents();
		}
		m_ghostPlayer.ClearAll();
		m_ghostRecorder.ClearRecorderSource();
		m_checkpointManager.Reset();
		m_checkpointManager.laps = 1;
		m_checkpointManager.OnRaceStart -= OnRaceStartHandle;
		m_checkpointManager.OnRaceStart += OnRaceStartHandle;
		m_checkpointManager.OnRaceCompleted -= OnRaceCompleted;
		m_checkpointManager.OnRaceCompleted += OnRaceCompleted;
		ConfigurationManager instance = ConfigurationManager.instance;
		if (instance != null)
		{
			m_checkpoints = instance.GetDataByType<CheckpointsData>();
			InitStartPositions(instance);
		}
		if (m_checkpoints != null)
		{
			m_startLine = m_checkpoints.GetStartLine();
		}
		m_runOutOfTime = false;
		m_isRaceStarted = false;
		m_isRaceCompleted = false;
		m_isRaceEnded = false;
		m_recordedGhostData = null;
		m_isWarned = false;
		if (spawnCar)
		{
			SpawnUserCar();
			PositionSpawnedCar();
		}
		if ((bool)userCar)
		{
			UIHUDMiniMapContext uIHUDMiniMapContext = base.contextOwner.Get<UIHUDMiniMapContext>();
			userCar.positionOnSpline.SetDirty();
			uIHUDMiniMapContext.visualizer.ResetZones();
		}
		PerformForEachCar(delegate(RaceCar car)
		{
			car.SetCurrentTrack(Track.instance);
		});
		PerformForEachCar(delegate(RaceCar car)
		{
			checkpointManager.AddCar(car);
		});
		if ((bool)CameraSwitch.instance)
		{
			CameraSwitch.instance.ActivateRaceCamera();
		}
	}

	public virtual void RegisterUserCar()
	{
		userCar.SetCurrentTrack(Track.instance);
		checkpointManager.AddCar(userCar);
		if ((bool)CameraSwitch.instance)
		{
			CameraSwitch.instance.ActivateRaceCamera();
		}
		raceContext.ReInit();
		if ((bool)UIXDSInfoContext.instance)
		{
			DriftControllerXDS.instance.SetCar(userCar);
		}
		NetworkGame instanceGame = NetworkController.InstanceGame;
		if (States.mainCurrent is SyncNetFreerideRaceModeState)
		{
			instanceGame?.systems.Get<NetGameCollisionManager>().UpdateCollideState(instanceGame.LocalPlayer);
		}
	}

	protected virtual void ShowUI(bool show)
	{
		m_needShowUI = show;
		if (show)
		{
			if (!loadingScreenActive)
			{
				ShowHUDIfNeed();
			}
		}
		else
		{
			base.contextOwner.Hide(raceContext.GetType());
			base.contextOwner.Hide<UIInputOverlayContext>();
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		RestoreManager.instance.Clear(clearMap: true);
		m_startPlayPressed = false;
		KeepAliveManager.SetUIVisible(isVisible: true);
		m_ghostPlayer.ClearAll();
		SetPlayerControlState(state: false, forceUpdateCtrlState: true);
		DestroyUserCar();
		DestroyLoadingContext();
		checkpointManager.OnRaceStart -= OnRaceStartHandle;
		checkpointManager.OnRaceCompleted -= OnRaceCompleted;
		m_checkpointManager.OnDestroy();
		m_carXDboxManager?.Stop();
		MinimapProvider.instance.DisposeResources();
	}

	private void OnRaceStartHandle(RaceCar car)
	{
		m_isRaceStarted = true;
	}

	private void ShowHUDIfNeed()
	{
		if (!raceContext.isActive)
		{
			base.contextOwner.Add(raceContext.GetType());
		}
		if (!base.contextOwner.IsActive(typeof(UIInputOverlayContext)))
		{
			base.contextOwner.Add<UIInputOverlayContext>();
		}
	}

	public abstract void ForceFinishRace();

	public virtual void EliminationWarning()
	{
	}

	protected virtual void OnRaceCompletedHandle()
	{
		m_isRaceCompleted = true;
		m_recordedGhostData = m_ghostRecorder.GetRecordedData();
		m_ghostRecorder.ClearRecorderSource();
		m_ghostRecorder.Stop();
	}

	private void OnRaceCompleted(RaceCar car, float bestLap, float totalTime)
	{
		if (!m_isRaceCompleted && !(car != userCar))
		{
			if (KeepAliveManager.isUIHidden)
			{
				KeepAliveManager.SetUIVisible(isVisible: true);
			}
			OnRaceCompletedHandle();
			RaceEndedEvent.Call();
		}
	}

	private void OnLoadingContextDestroy()
	{
		if (m_needShowUI)
		{
			ShowHUDIfNeed();
		}
	}

	protected virtual void SetupXDSReplay()
	{
		m_ghostPlayer.ClearPlayers();
		AddUserPlayer();
		DestroyUserCar();
		m_ghostPlayer.Play();
	}

	protected virtual void SetupRaceReplay()
	{
		m_ghostPlayer.ClearPlayers();
		AddUserPlayerWithCar();
		ReplaceGhostCar();
		m_ghostPlayer.Play();
	}

	protected virtual void AddUserPlayerWithCar()
	{
		if (m_recordedGhostData != null)
		{
			m_ghostPlayer.AddPlayer(new GhostPlayer.StartArgs
			{
				data = m_recordedGhostData,
				setupCamera = true,
				carForPlay = m_userCar
			});
		}
	}

	protected virtual void AddUserPlayer(List<(GhostData data, string nick, string platformUserId)> overrideGhostDatas = null)
	{
		if (overrideGhostDatas != null)
		{
			foreach (var overrideGhostData in overrideGhostDatas)
			{
				if (overrideGhostData.data != null)
				{
					m_ghostPlayer.AddPlayer(new GhostPlayer.StartArgs
					{
						data = overrideGhostData.data,
						nickName = overrideGhostData.nick,
						platformUserId = overrideGhostData.platformUserId,
						setupCamera = true,
						needCollisions = true,
						forceInitDefaultCar = NeedForceInitDefaultCar(overrideGhostData.data)
					});
				}
			}
			return;
		}
		if (m_recordedGhostData != null)
		{
			m_ghostPlayer.AddPlayer(new GhostPlayer.StartArgs
			{
				data = m_recordedGhostData,
				setupCamera = true
			});
		}
		static bool NeedForceInitDefaultCar(GhostData ghostData)
		{
			if (ghostData == null)
			{
				return false;
			}
			if (ghostData.carId != 1)
			{
				return ghostData.carId == 129;
			}
			return true;
		}
	}

	public override void OnUpdate()
	{
		m_carXDboxManager?.UpdateDboxFrame();
		if (!m_isRaceStarted)
		{
			return;
		}
		UpdateLapTime();
		if (!m_isRaceCompleted && (int)m_totalTimeLimitSeconds > 0)
		{
			if (timeLeft < 0f && !m_runOutOfTime)
			{
				m_runOutOfTime = true;
				ForceFinishRace();
			}
			if (!m_isWarned && timeLeft < m_eliminationTime)
			{
				m_isWarned = true;
				EliminationWarning();
			}
		}
		base.OnUpdate();
	}

	public override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		m_checkpointManager.UpdatePenaltyTime();
		UpdateLapTime();
	}

	public override void OnLateUpdate()
	{
		base.OnLateUpdate();
		UpdateControlState();
	}

	private void UpdateControlState()
	{
		if (m_requestedPlayerCtrlState.HasValue)
		{
			PlayerCarControl instance = PlayerCarControl.instance;
			if (instance != null && instance.enabled != m_requestedPlayerCtrlState.Value)
			{
				instance.enabled = m_requestedPlayerCtrlState.Value;
			}
			m_requestedPlayerCtrlState = null;
		}
	}

	private void UpdateLapTime()
	{
		if (m_userCar != null)
		{
			m_currentLapTime = m_checkpointManager.GetTotalTime(m_userCar, withPenalty: false);
		}
	}

	protected virtual void PositionSpawnedCar()
	{
		if (m_startPositions == null || m_startPositions.Length == 0 || m_currentStartPosition == null)
		{
			return;
		}
		if (Track.instance != null)
		{
			ISpline racingLine = Track.instance.racingLine;
			if (racingLine != null)
			{
				racingLine.ProjectPoint(m_currentStartPosition.transform.position, out float along, out float _);
				racingLine.SamplePoint(along, out var pnt, out var vel);
				m_currentStartPosition.transform.SetPositionAndRotation(pnt, Quaternion.LookRotation(vel));
			}
		}
		m_currentStartPosition.Apply(userCar);
	}

	public static void SetupCarInScene(RaceCar car, bool resetInterpolation = false)
	{
		if ((bool)CameraSwitch.instance)
		{
			CameraSwitch.instance.SetCar(car, resetInterpolation);
		}
	}

	protected virtual int GetProfileId()
	{
		return m_prefs.carSettings.GetProfileIdForCar(m_globals.lastCarId);
	}

	public virtual async UniTask SwapCar(int newCarId)
	{
		carSwapRunning = true;
		RaceCar cachedCar = userCar;
		cachedCar.FlipCar();
		cachedCar.gameObject.ReplaceLayerRecursively(29, 17);
		bool winterTiresEnabled = TrackMetaDB.IsWinterTrackWithIce(m_globals.lastTrackId);
		m_prefs.carSettings.SetWinterTiresForcar(cachedCar.carId, winterTiresEnabled: false);
		m_prefs.carSettings.SetWinterTiresForcar(newCarId, winterTiresEnabled);
		float? startTime = checkpointManager.GetStartTime(cachedCar);
		int prevLastCarId = m_globals.lastCarId;
		m_globals.lastCarId = newCarId;
		await CreateUserCarAsync(newCarId);
		if (!userCar)
		{
			m_globals.lastCarId = prevLastCarId;
		}
		SetCarControl(userCar);
		userCar.OnCarVisualLoadFinishEvent -= OnCarVisualLoadFinishEventHandler;
		userCar.OnCarVisualLoadFinishEvent += OnCarVisualLoadFinishEventHandler;
		void OnCarVisualLoadFinishEventHandler(RaceCar car)
		{
			ReplaceCarAsync(car).Forget();
		}
		async UniTask ReplaceCarAsync(RaceCar car)
		{
			userCar.OnCarVisualLoadFinishEvent -= OnCarVisualLoadFinishEventHandler;
			cachedCar.GetGroundPosition(out var position, out var rotation);
			userCar.getTransform.SetPositionAndRotation(position, rotation);
			userCar.RecordLastRoadTransform();
			userCar.ugcMapSpawnPoint = cachedCar.ugcMapSpawnPoint;
			RegisterUserCar();
			userCar.FlipCar();
			SetupCarInScene(userCar);
			if (!(States.mainCurrent is SyncNetFreerideRaceModeState))
			{
				m_ghostRecorder.AddRecordSource(userCar);
			}
			checkpointManager.RemoveCar(cachedCar);
			checkpointManager.SetupStartTimeIfNeed(userCar, startTime ?? (Time.time - 1f));
			userCar.attachedDriftController.Copy(cachedCar.attachedDriftController);
			userCar.SetHeadlightsEnabled(cachedCar.headlightsEnabled, ignoreIgnition: false, force: true);
			onCarSwapped.SafeCall(cachedCar, car);
			cachedCar.UnloadResources();
			m_prefs.carSettings.TryReleaseCarModelResources(cachedCar.carId);
			UnityEngine.Object.Destroy(cachedCar.gameObject);
			cachedCar = null;
			await Resources.UnloadUnusedAssets();
			await UniTask.NextFrame();
			carSwapRunning = false;
		}
	}

	protected void SetCarControl(RaceCar car)
	{
		if (!(car == null))
		{
			PlayerCarControl instance = PlayerCarControl.instance;
			if (instance != null)
			{
				instance.car = car;
			}
			base.contextOwner.Get<UIHUDMiniMapContext>();
			MinimapProvider.instance.Init(car, checkpointManager);
		}
	}

	private void CreateUserCar(CarVisualManager.VisualSettings overrideVisual = null)
	{
		if (!(m_userCar != null) || m_userCar.carId != m_globals.lastCarId)
		{
			if (m_carFactory == null)
			{
				m_carFactory = DependencyInjector.Resolve<PlayerCarFactory>();
			}
			m_userCar = m_carFactory.CreateRaceCar(m_globals.lastCarId, GetProfileId(), overrideVisual);
			m_carXDboxManager?.SetCar(m_userCar);
			m_carXDboxManager?.UpdateConfig();
		}
	}

	private async UniTask CreateUserCarAsync(int newCarId, CarVisualManager.VisualSettings overrideVisual = null)
	{
		if (!(m_userCar != null) || m_userCar.carId != newCarId)
		{
			if (m_carFactory == null)
			{
				m_carFactory = DependencyInjector.Resolve<PlayerCarFactory>();
			}
			m_userCar = await m_carFactory.CreateRaceCarAsync(newCarId, GetProfileId(), overrideVisual);
			m_carXDboxManager?.SetCar(m_userCar);
			m_carXDboxManager?.UpdateConfig();
		}
	}

	protected void SpawnUserCar()
	{
		CreateUserCar();
		SetCarControl(userCar);
		SetupCarInScene(userCar);
		userCar.engineController.SetIgnition(enabled: true);
		userCar.engineController.ForceStartEngine(bumpRpm: true);
	}

	protected void DestroyUserCar()
	{
		if (destroyCarByFinish)
		{
			SetupCarInScene(null);
			if (m_userCar != null)
			{
				checkpointManager.RemoveCar(m_userCar);
				UnityEngine.Object.Destroy(m_userCar.gameObject);
				m_userCar = null;
			}
		}
		m_ghostRecorder.ClearRecorderSource();
		m_ghostRecorder.Stop();
	}

	protected void ReplaceGhostCar()
	{
		if (destroyCarByFinish && m_userCar != null)
		{
			checkpointManager.RemoveCar(m_userCar);
			UnityEngine.Object.Destroy(m_userCar.GetComponent<DriftController>());
			UnityEngine.Object.Destroy(m_userCar.GetComponent<KissTheWallChecker>());
			UnityEngine.Object.Destroy(m_userCar.GetComponent<CarAI>());
			m_userCar = null;
		}
		m_ghostRecorder.ClearRecorderSource();
		m_ghostRecorder.Stop();
	}

	protected void SetPlayerControlState(bool state, bool forceUpdateCtrlState = false)
	{
		m_requestedPlayerCtrlState = state;
		if (forceUpdateCtrlState)
		{
			UpdateControlState();
		}
	}

	protected void SetAiControlState(bool state)
	{
		if ((bool)userCar)
		{
			userCar.gameObject.GetOrAddComponent<CarAI>().enabled = state;
		}
	}

	protected void SetCameraControlState(bool state)
	{
		if ((bool)CameraSwitch.instance)
		{
			CameraSwitch.instance.SetActiveCameraSwitch(state);
		}
	}

	protected override void OnProcessEscape()
	{
	}

	private void OnApplicationPause(bool pauseState)
	{
		if (pauseState && States.current == this)
		{
			LoseFocus();
		}
	}

	public bool LoseFocus()
	{
		if (forceIgnoreLoseFocus)
		{
			return false;
		}
		bool flag = false;
		List<BaseContext> internalOwnedList = base.contextOwner.GetInternalOwnedList();
		if (internalOwnedList != null)
		{
			foreach (BaseContext item in internalOwnedList)
			{
				if (item.isActive)
				{
					flag = true;
					break;
				}
			}
		}
		if ((!isRaceCompleted && !m_loadingContext && flag) & IsAvailableToPauseContext(base.contextOwner))
		{
			InGameMenuState.Show();
			return true;
		}
		return false;
	}

	private bool IsAvailableToPauseContext(ContextOwner owner)
	{
		List<BaseContext> internalOwnedList = owner.GetInternalOwnedList();
		if (internalOwnedList == null)
		{
			return false;
		}
		foreach (BaseContext item in internalOwnedList)
		{
			if (Array.Exists(m_unpausedContextTypes, (Type el) => el == item.GetType()) && item.isActive)
			{
				return false;
			}
		}
		return true;
	}

	public void TryTakeControlOfLoadingContext(UILoadingScreenContext loadingContext)
	{
		if (loadingScreenActive)
		{
			m_loadingContext.HideAndDestroy();
		}
		m_loadingContext = loadingContext;
		m_loadingContext.autoContinue = !waitContinuePress;
	}

	protected IEnumerable<RaceCar> GetAllCars()
	{
		List<RaceCar> list = new List<RaceCar>(1);
		PerformForEachCar(list.Add);
		return list;
	}

	protected void PerformForEachCar(Action<RaceCar> action)
	{
		if (m_userCar != null)
		{
			action(m_userCar);
		}
	}
}
