using System;
using System.Collections;
using System.Collections.Generic;
using Customization;
using DB.Meta;
using PrefsModels;
using SplineLogic;
using SyncMultiplayer;
using TrackLayout.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

public class XDSRaceModeState : DriftRaceModeState
{
	private const string FIRST_RACE_CONFIG = "xds_firstRace";

	private const string SECOND_RACE_CONFIG = "xds_secondRace";

	public bool firstRace = true;

	public float firstResult;

	protected UIXDSInfoContext m_xds;

	protected TrackMetaConfig m_currentConfig;

	private List<PlayerCar> m_carsList;

	private List<PlayerCar> m_cachedCarsList = new List<PlayerCar>();

	private HashSet<int> m_usedWinterCarsId = new HashSet<int>();

	public Action onPreRestart;

	public Action isAboutToDeactivateUserCarAction;

	private Coroutine m_showPrecombatCoroutine;

	public override RaceMode raceMode => RaceMode.XDS;

	public override bool allowCarRespawnInRace => false;

	public DetailedRaceAward raceAward => m_currentAward;

	public XDSGhostController ghostController { get; private set; } = new XDSGhostController();

	public DisqualifyReason disqualifyReason { get; protected set; }

	protected override bool isGhostEnabled => false;

	protected override bool isGhostRecordEnabled => false;

	protected override bool isStoppingOnFinish => true;

	protected override bool isCountdownEnabled => false;

	protected override bool isStartingStopped => true;

	protected override bool destroyCarByFinish => false;

	public override void OnEnter()
	{
		m_currentConfig = m_prefs.globals.lastMetaConfig;
		m_carsList = m_model.QueryAll<PlayerCar>();
		GameConsole.SetCollisions(value: true);
		PreEnterInitialization();
		ApplyWinterCustomize();
		base.OnEnter();
		PreloadOnEnterContext();
	}

	protected virtual void PreEnterInitialization()
	{
		InstantiateConfig(isFirstRaceConfig: true);
	}

	protected virtual void PreloadOnEnterContext()
	{
		base.contextOwner.Preload<UICarSelectorContext>();
		base.contextOwner.Preload<UIXDSPreCombatWheelsContext>();
		base.contextOwner.Preload<UIXDSPostCombatContext>();
		base.contextOwner.Preload<UIXDSPreCombatContext>();
	}

	private void InstantiateConfig(bool isFirstRaceConfig)
	{
		ConfigurationManager.instance.InstantiateCustomConfiguration(isFirstRaceConfig ? "xds_firstRace" : "xds_secondRace");
	}

	public override void OnExit()
	{
		DiscardWinterCustomizeInUsedCars();
		ghostController?.Release();
		DestroyLoadingContext();
	}

	protected override void StartPlay()
	{
		base.StartPlay();
		base.userCar.lockState = true;
		m_xds = UIXDSInfoContext.instance;
		DriftControllerXDS controller = m_xds.controller;
		controller.onRaceDisqualify = (Action<DisqualifyReason>)Delegate.Combine(controller.onRaceDisqualify, new Action<DisqualifyReason>(RaceDisqualifyHandler));
		SortCarsListIfNeed();
		firstRace = true;
		RestartRace();
	}

	protected virtual void SortCarsListIfNeed()
	{
		base.contextOwner.Get<UICarSelectorContext>().carsFilter = delegate
		{
			m_cachedCarsList.Clear();
			foreach (PlayerCar cars in m_carsList)
			{
				if (CarPartDatabase.IsCarUnlockedByDLC(cars.id) || !m_prefs.locks.GetCarLockState(cars.id))
				{
					m_cachedCarsList.Add(cars);
				}
			}
			return m_cachedCarsList;
		};
	}

	private void RaceDisqualifyHandler(DisqualifyReason reason)
	{
		disqualifyReason = reason;
		ForceFinishRace();
		ghostController.StopCars();
	}

	protected override void ShowFinish()
	{
		if (disqualifyReason != DisqualifyReason.Unknown)
		{
			base.contextOwner.Push<UIFinishContext>().PlayCustom(LocalizationKeys.xds_rsn_.AddLocalized(disqualifyReason.ToString().ToLower()));
		}
		else
		{
			base.ShowFinish();
		}
	}

	protected override void Init()
	{
		base.Init();
		m_totalTimeLimitSeconds = 0;
	}

	protected override void OnRaceCompletedHandle()
	{
		base.OnRaceCompletedHandle();
		SetAiControlState(state: false);
		SetPlayerControlState(state: true);
		base.userCar.lockState = true;
		if (firstRace)
		{
			firstResult = UIXDSInfoContext.instance.scores;
		}
	}

	protected override void ShowPostCombat()
	{
		ghostController.SaveGhosts();
		SetPlayerControlState(state: false);
		SetCameraControlState(state: false);
		UIXDSPostCombatContext uIXDSPostCombatContext = base.contextOwner.Get<UIXDSPostCombatContext>();
		if (firstRace)
		{
			uIXDSPostCombatContext.InitRewards();
			if (base.userCar != null && base.userCar.carX != null)
			{
				base.userCar.carX.accelerate = 0f;
			}
		}
		else
		{
			if (base.userCar != null)
			{
				isAboutToDeactivateUserCarAction.SafeCall();
				base.userCar.gameObject.SetActive(value: false);
				UnityEngine.Object.Destroy(base.userCar.gameObject);
			}
			ghostController.PrepareForMode(XDSGhostController.GhostMode.Replay);
			ghostController.PlayBackgroundGhosts();
		}
		if (KeepAliveManager.isUIHidden)
		{
			KeepAliveManager.SetUIVisible(isVisible: true);
		}
		base.contextOwner.Push<UIXDSPostCombatContext>();
		uIXDSPostCombatContext.CountPartialExpByReward(m_currentAward);
		if (disqualifyReason == DisqualifyReason.Unknown)
		{
			string config = m_currentConfig.EncodeName(raceMode, SceneManager.GetActiveScene().name);
			m_prefs.stats.StoreXDSResult(config, m_xds.scores, (!firstRace) ? StatsModel.XDSStatRace.Second : StatsModel.XDSStatRace.First);
			if (!firstRace)
			{
				m_prefs.stats.StoreXDSResult(config, firstResult + m_xds.scores, StatsModel.XDSStatRace.Total);
			}
			m_prefs.stats.Save();
		}
	}

	protected override void Shutdown()
	{
		m_driftDistanceUpdater.SendCurrentToStat();
		OnShutdown.SafeCall();
	}

	protected override void CalculateAward(ref DetailedRaceAward award)
	{
		award.expFinishAward = 0;
		award.cupAward = 0;
		award.finishAward = 0;
		award.leftTimeAward = 0;
		if (!firstRace && disqualifyReason == DisqualifyReason.Unknown)
		{
			RewardNode rewardFor = m_currentConfig.GetRewardFor(raceMode, firstResult + UIXDSInfoContext.instance.scores);
			award.finishAward = rewardFor.coinsReward;
			award.expFinishAward = rewardFor.expReward;
		}
	}

	public override void RestartRace()
	{
		onPreRestart.SafeCall();
		DoRestart(firstRace: true);
	}

	public virtual void DoRestart(bool firstRace)
	{
		KeepAliveManager.SetUIVisible(isVisible: true);
		SoundController.instance.fxAudioPause = false;
		ghostController.Stop();
		StopCountDownAndFinishContext();
		ShowUI(show: false);
		m_xds.DiscardRace();
		this.firstRace = firstRace;
		XDSGhostController.GhostMode mode = ((!firstRace) ? XDSGhostController.GhostMode.SecondRace : XDSGhostController.GhostMode.FirstRace);
		ghostController.PrepareForMode(mode);
		PlayerCarControl.instance.SetBlockControl(value: true);
		SetCameraControlState(state: false);
		RaceCar car = (PlayerCarControl.instance ? PlayerCarControl.instance.car : null);
		if (!firstRace || BaseCar.IsNull(car))
		{
			ManualSetupCar();
		}
		if (m_showPrecombatCoroutine != null)
		{
			TimeController.StopNow(m_showPrecombatCoroutine);
			m_showPrecombatCoroutine = null;
		}
		if (!base.contextOwner.IsActive<UIXDSPreCombatContext>())
		{
			m_showPrecombatCoroutine = TimeController.StartNow(ShowPrecombatCoroutine());
		}
	}

	protected void StopCountDownAndFinishContext()
	{
		UICountDownContext uICountDownContext = base.contextOwner.Get<UICountDownContext>();
		if ((bool)uICountDownContext)
		{
			uICountDownContext.Stop();
		}
		UIFinishContext uIFinishContext = base.contextOwner.Get<UIFinishContext>();
		if ((bool)uIFinishContext)
		{
			uIFinishContext.Stop();
		}
	}

	private IEnumerator ShowPrecombatCoroutine()
	{
		yield return new WaitUntil(() => IsReadyHideLoadingScreen);
		UIXDSPreCombatContext uIXDSPreCombatContext;
		if (base.contextOwner.IsActive<UIXDSPreCombatContext>())
		{
			base.contextOwner.Hide<UIXDSPreCombatContext>();
			uIXDSPreCombatContext = base.contextOwner.Push<UIXDSPreCombatContext>();
		}
		else
		{
			uIXDSPreCombatContext = base.contextOwner.Get<UIXDSPreCombatContext>();
		}
		uIXDSPreCombatContext.gameObject.SetActive(value: true);
		uIXDSPreCombatContext.Activate();
		uIXDSPreCombatContext.catalog.UpdateBottomPanel();
		UIXDSPreCombatContext uIXDSPreCombatContext2 = uIXDSPreCombatContext;
		uIXDSPreCombatContext2.onCarSelected = (Action<int>)Delegate.Remove(uIXDSPreCombatContext2.onCarSelected, new Action<int>(PrepareCarToRace));
		UIXDSPreCombatContext uIXDSPreCombatContext3 = uIXDSPreCombatContext;
		uIXDSPreCombatContext3.onCarSelected = (Action<int>)Delegate.Combine(uIXDSPreCombatContext3.onCarSelected, new Action<int>(PrepareCarToRace));
	}

	protected void ApplyWinterCustomize()
	{
		m_prefs.carSettings.SetWinterTiresForcar(m_prefs.globals.lastCarId, TrackMetaDB.IsWinterTrackWithIce(m_prefs.globals.lastTrackId));
		m_usedWinterCarsId.Add(m_prefs.globals.lastCarId);
	}

	protected void DiscardWinterCustomizeInUsedCars()
	{
		foreach (int item in m_usedWinterCarsId)
		{
			m_prefs.carSettings.SetWinterTiresForcar(item, winterTiresEnabled: false);
		}
	}

	protected virtual void PrepareCarToRace(int carId)
	{
		if (m_globals.lastCarId != carId)
		{
			if ((bool)base.userCar)
			{
				base.userCar.gameObject.SetActive(value: false);
				UnityEngine.Object.Destroy(base.userCar.gameObject);
			}
			m_globals.lastCarId = carId;
			UISpinner.Show();
		}
		ApplyWinterCustomize();
		SpawnUserCar();
		ConfigureDriftController();
		ResetCoins();
		m_xds.controller.SetCar(base.userCar);
		base.userCar.lockState = true;
		UISpinner.HideOnCarLoaded(base.userCar);
		disqualifyReason = DisqualifyReason.Unknown;
		InstantiateConfig(firstRace);
		Reset();
		SetAiControlState(state: false);
		SetPlayerControlState(state: false);
		PlaceCarToStart(firstRace);
		BaseRaceState.SetupCarInScene(base.userCar);
		UIXDSPreCombatWheelsContext uIXDSPreCombatWheelsContext = base.contextOwner.Push<UIXDSPreCombatWheelsContext>();
		uIXDSPreCombatWheelsContext.onComplete = (Action)Delegate.Remove(uIXDSPreCombatWheelsContext.onComplete, new Action(LaunchRace));
		uIXDSPreCombatWheelsContext.onComplete = (Action)Delegate.Combine(uIXDSPreCombatWheelsContext.onComplete, new Action(LaunchRace));
	}

	private void ManualSetupCar()
	{
		ApplyWinterCustomize();
		SpawnUserCar();
		ConfigureDriftController();
		ResetCoins();
		m_xds.controller.SetCar(base.userCar);
		base.userCar.lockState = true;
		disqualifyReason = DisqualifyReason.Unknown;
		InstantiateConfig(firstRace);
		Reset();
		SetAiControlState(state: false);
		SetPlayerControlState(state: false);
		PlaceCarToStart(firstRace);
		BaseRaceState.SetupCarInScene(base.userCar);
	}

	protected virtual void LaunchRace()
	{
		PlayerCarControl.instance.SetBlockControl(value: false);
		base.userCar.gameObject.SetActive(value: true);
		ShowUI(show: true);
		base.userCar.lockState = true;
		base.userCar.carX.gear = 1;
		base.userCar.carX.clutch = 1f;
		SetPlayerControlState(state: true);
		SetCameraControlState(state: true);
		UICountDownContext countdown = base.contextOwner.Get<UICountDownContext>();
		if ((bool)countdown)
		{
			Action action = null;
			action = delegate
			{
				ghostController.RaceStart();
				base.userCar.lockState = false;
				countdown.onFinishedEvent -= action;
			};
			countdown.onFinishedEvent += action;
			countdown.DoStart();
		}
		XDSGhostController.GhostMode mode = ((!firstRace) ? XDSGhostController.GhostMode.SecondRace : XDSGhostController.GhostMode.FirstRace);
		ghostController.ActivateMode(mode, base.userCar, 0f);
		m_xds.PrepareForStart(countdown.GetCountdownTime());
		m_xds.SetupTandemLead(firstRace ? null : ghostController.player.ghostCar);
	}

	protected void PlaceCarToStart(bool firstRace)
	{
		int num = (firstRace ? 1 : 0);
		StartLine startLine = base.checkpointManager.startLine;
		Vector3 pnt = startLine.transform.position;
		ISpline centerLine = Track.instance.centerLine;
		centerLine.ProjectPoint(pnt, out float along, out float _);
		int num2 = num / 2;
		float num3 = 1f;
		if (num % 2 == 0)
		{
			num3 = 5f;
		}
		float num4 = base.userCar.carX.bounds.max.z + 8f * (float)num2 + num3;
		centerLine.SamplePoint(along - num4, out var pnt2, out var vel);
		vel = vel.normalized;
		float num5 = Vector3.Dot(pnt2 - pnt, -vel);
		if (Mathf.Abs(num4 - num5) > 0.1f)
		{
			centerLine.SamplePoint(along, out pnt, out var vel2);
			vel2 = vel2.normalized;
			pnt2 = pnt - vel2 * num4;
			centerLine.ProjectPoint(pnt2, out pnt2, out vel);
			vel = vel.normalized;
		}
		vel = new Vector3(0f - vel.z, vel.y, vel.x) * 1.5f;
		if (num % 2 == 0)
		{
			vel = -vel;
		}
		pnt2 += new Vector3(vel.x, 0f, vel.z);
		Transform getTransform = base.userCar.getTransform;
		getTransform.rotation = startLine.transform.rotation;
		getTransform.position = pnt2;
		base.userCar.AlignCarPositionAtSpawn();
		base.userCar.getRigidbody.velocity = Vector3.zero;
		base.userCar.getRigidbody.angularVelocity = Vector3.zero;
	}
}
