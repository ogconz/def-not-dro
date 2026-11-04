using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameOverlay;
using HUD;
using JukeBox;
using SyncMultiplayer;
using UnityEngine;

public class SyncNetFreerideRaceModeState : FreerideRaceModeState
{
	private const float LOADING_SCREEN_HIDE_TIMEOUT = 60f;

	private UIStartContext m_starter;

	private SmartfoxRoomClient m_sfxClient;

	public override RaceMode raceMode => RaceMode.NetworkFreeride;

	public bool IsLoadingScreenHided => IsReadyHideLoadingScreen;

	protected override bool isGhostRecordEnabled => false;

	public virtual bool doNotAllowOMT => false;

	public override void OnEnter()
	{
		base.OnEnter();
		PreloadOnEnterContext();
		m_starter = base.contextOwner.Get<UIStartContext>();
		AFKController.instance.SetActive(active: true, clearCallbacks: true);
		AFKController instance = AFKController.instance;
		instance.onAfkKick = (Action)Delegate.Combine(instance.onAfkKick, new Action(MultiplayerPCState.ReturnToLobby));
		UpdateNetworkRichPresenceVariables(NetworkController.InstanceGame.lastRoomOptions);
		SubscribeOnSmartfoxChanges();
	}

	public void SubscribeOnSmartfoxChanges()
	{
		m_sfxClient = NetworkController.InstanceGame.Client;
		if (m_sfxClient != null)
		{
			m_sfxClient.RoomOptionsChanged += OnRoomOptionsChangedHandler;
		}
	}

	private void UnsubscribeFromSmartfoxChanges()
	{
		if (m_sfxClient != null)
		{
			m_sfxClient.RoomOptionsChanged -= OnRoomOptionsChangedHandler;
		}
	}

	private void OnRoomOptionsChangedHandler(SmartfoxRoomClient _, SmartfoxRoomOptions options, HashSet<string> __)
	{
		UpdateNetworkRichPresenceVariables(options);
	}

	private void UpdateNetworkRichPresenceVariables(SmartfoxRoomOptions options)
	{
		if (options != null)
		{
			IRichPresence richPresence = Overlay.instance.richPresence;
			richPresence.SetVariable(RichPresenceVariables.PlayersCountInRoom, options.PlayerCount.ToString());
			richPresence.SetVariable(RichPresenceVariables.MaxPlayersCountInRoom, options.MaxPlayers.ToString());
		}
	}

	protected virtual void WaitLoadingScreenIfNeed()
	{
		TimeController.StartNow(HideLoadingScreenAsync());
	}

	private IEnumerator HideLoadingScreenAsync()
	{
		IsReadyHideLoadingScreen = false;
		float time = Time.unscaledTime;
		while (true)
		{
			List<NetworkPlayer> players = NetworkController.InstanceGame.Players;
			int count = players.Count;
			int num = 0;
			for (int i = 0; i < count; i++)
			{
				NetworkPlayer networkPlayer = players[i];
				if (networkPlayer != null && !networkPlayer.IsLocal && !(networkPlayer.userCar == null) && networkPlayer.userCar.IsVisualLoaded)
				{
					num++;
				}
			}
			bool num2 = num >= count / 2;
			bool flag = Time.unscaledTime - time > 60f;
			if (num2 || flag)
			{
				break;
			}
			yield return null;
		}
		IsReadyHideLoadingScreen = true;
		Overlay.NotifyMPFeature(NetworkController.InstanceGame.Players.Count > 1);
	}

	protected virtual void PreloadOnEnterContext()
	{
		base.contextOwner.Preload<UICreateCompetitionContext>();
		base.contextOwner.Preload<UIPlayersWaitingContext>();
		base.contextOwner.Preload<UIJoinCompetitionContext>();
		base.contextOwner.Preload<UIVoteContext>();
		base.contextOwner.Preload<UIRaceStatsContext>();
		base.contextOwner.Preload<UIStartContext>();
		base.contextOwner.Preload<UIReportContext>();
	}

	protected override void StartPlay()
	{
		base.StartPlay();
		Overlay.instance.CheckOnlinePlayAbility();
	}

	protected override void PlayMusic()
	{
		SoundController.PlayMusic(MusicSignal.MPIdle);
	}

	public void ResetPoints()
	{
		m_driftController.StopDrift();
		m_driftCoinsStartValue = UnityEngine.Random.Range(0, 10000);
		m_driftCoinsEarned = m_driftCoinsStartValue;
	}

	public void StopDrift()
	{
		m_driftController.StopDrift();
	}

	public void SetupTandemLead(RaceCar leadCar)
	{
		bool flag = false;
		if ((bool)UIXDSInfoContext.instance)
		{
			UIXDSInfoContext.instance.SetupTandemLead(leadCar);
			flag = UIXDSInfoContext.instance.awaitingStart;
		}
		m_driftController.tandemLeader = (flag ? null : leadCar);
	}

	public void SetupTimeLimit(int seconds)
	{
		m_totalTimeLimitSeconds = seconds;
		m_prevPenaltyTime = 0f;
		if (seconds == 0)
		{
			m_driftController.tandemLeader = null;
			m_driftController.allowRepeatDrift = true;
			base.checkpointManager.ClearTimers();
		}
	}

	public override void Reset(bool spawnCar = false, bool resetStatsEvents = true)
	{
		base.Reset(spawnCar, resetStatsEvents);
		UIFinishContext uIFinishContext = base.contextOwner.Get<UIFinishContext>();
		if ((bool)uIFinishContext)
		{
			uIFinishContext.Play(timeIsOut: false);
			uIFinishContext.onFinishedEvent -= base.OnFinished;
		}
		if (spawnCar)
		{
			base.checkpointManager.RegisterStartRace(base.userCar, Time.time - 1f);
		}
	}

	protected override void ShowUI(bool show)
	{
		if (!show)
		{
			BaseContext topContext = base.contextOwner.topContext;
			if (topContext is UIReportContext)
			{
				topContext.Hide();
				topContext = base.contextOwner.topContext;
			}
			if (topContext is UIRaceStatsContext)
			{
				topContext.Hide();
			}
		}
		base.ShowUI(show);
	}

	public virtual void ShowRaceFinish(string customMsg = "", bool timeIsOut = false)
	{
		HideStarter();
		base.contextOwner.Hide<UIFinishContext>();
		UIFinishContext uIFinishContext = base.contextOwner.Push<UIFinishContext>();
		if (!string.IsNullOrEmpty(customMsg))
		{
			bool fail = timeIsOut;
			uIFinishContext.PlayCustom(customMsg, fail);
		}
		else
		{
			uIFinishContext.Play(timeIsOut);
		}
		uIFinishContext.onFinishedEvent -= base.OnFinished;
	}

	public void HideStarter()
	{
		if ((bool)m_starter)
		{
			m_starter.Hide();
		}
	}

	public void ShowCountDown(int seconds)
	{
		if (States.current != this || seconds == -1)
		{
			HideStarter();
			return;
		}
		if (!m_starter.gameObject.activeSelf)
		{
			base.contextOwner.Add<UIStartContext>();
		}
		m_starter.Play(seconds);
	}

	protected override void Shutdown()
	{
		NetworkController.LobbyExit();
		base.Shutdown();
	}

	public override void OnExit()
	{
		base.OnExit();
		AFKController.instance.SetActive(active: false, clearCallbacks: true);
		AFKController instance = AFKController.instance;
		instance.onAfkKick = (Action)Delegate.Remove(instance.onAfkKick, new Action(MultiplayerPCState.ReturnToLobby));
		UIRaceStatsContext uIRaceStatsContext = base.contextOwner.Get<UIRaceStatsContext>();
		if ((bool)uIRaceStatsContext)
		{
			uIRaceStatsContext.ClearContainerData();
		}
		NetworkController.LobbyExit();
		Overlay.NotifyMPFeature(notify: false);
		UnsubscribeFromSmartfoxChanges();
	}

	public override async UniTask SwapCar(int id)
	{
		NetworkGame instanceGame = NetworkController.InstanceGame;
		NetGameCarSwapSystem swapSystem;
		if (instanceGame?.CurrentRoom != null)
		{
			swapSystem = instanceGame.systems.Get<NetGameCarSwapSystem>();
			swapSystem.SEND_CarSwapStart();
			await base.SwapCar(id);
			base.userCar.OnCarVisualLoadFinishEvent -= OnCarLoad;
			base.userCar.OnCarVisualLoadFinishEvent += OnCarLoad;
		}
		async void OnCarLoad(RaceCar car)
		{
			car.OnCarVisualLoadFinishEvent -= OnCarLoad;
			await swapSystem.CarSwapReady();
		}
	}

	public UniTask BaseSwapCar(int id)
	{
		return base.SwapCar(id);
	}

	protected override void PositionSpawnedCar()
	{
		if (m_startPositions != null && m_startPositions.Length != 0 && base.m_currentStartPosition != null)
		{
			base.m_currentStartPosition.ApplyStatic(base.userCar);
		}
		else
		{
			base.PositionSpawnedCar();
		}
	}
}
