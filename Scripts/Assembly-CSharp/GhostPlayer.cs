using System;
using System.Collections.Generic;
using CarX;
using Customization;
using DI;
using PrefsModels;
using SyncMultiplayer;
using UnityEngine;

public class GhostPlayer
{
	public class StartArgs
	{
		public bool isUser = true;

		public bool needCollisions;

		public GhostData data;

		public CarVisualManager.VisualSettings visualSettings;

		public float timeOffset;

		public bool setupCamera;

		public string nickName;

		public string platformUserId;

		public ProfilesInfo.Profile profileData;

		public RaceCar carForPlay;

		public bool forceInitDefaultCar;

		public bool visibleOnlyInProgress;
	}

	public class Player
	{
		public enum PlayerType
		{
			User = 0,
			Opponent = 1
		}

		public float timeOffset;

		public bool isPlay;

		public GlobalModel globals;

		private RaceCarFromCollderCache m_raceCarCollderCache = new RaceCarFromCollderCache();

		private PlayerType m_type;

		private GameObject m_carGo;

		private bool m_isCameraSetted;

		private bool m_isCustomCar;

		private string m_nickName;

		private bool m_visibleOnlyInProgress;

		public bool visible = true;

		public Action<int> OnDPChangedEvent;

		public RaceCar ghostCar { get; private set; }

		public GhostData ghostData { get; private set; }

		public PlayerType Type => m_type;

		public string NickName => m_nickName;

		private void DestroyCar()
		{
			if (m_isCameraSetted)
			{
				BaseRaceState.SetupCarInScene(null);
			}
			if (ghostCar != null)
			{
				ghostCar = null;
				if (!m_isCustomCar)
				{
					UnityEngine.Object.Destroy(m_carGo);
				}
				m_carGo = null;
			}
		}

		private void SetupCamera(bool followCar, bool resetInpterpolation = false)
		{
			if (ghostCar != null && ghostCar.IsVisualLoaded && !m_isCameraSetted)
			{
				ghostCar.SendMessage("SetTarget", ghostCar, SendMessageOptions.DontRequireReceiver);
				m_isCameraSetted = true;
				BaseRaceState.SetupCarInScene(ghostCar, resetInpterpolation);
				if (followCar && CameraSwitch.instance != null)
				{
					CameraSwitch.instance.ActivateFollowCamera();
				}
			}
		}

		public void HandleCollision(Collision collInfo)
		{
			DriftControllerXDS instance = DriftControllerXDS.instance;
			if (!isPlay || instance == null)
			{
				return;
			}
			CollisionsConfigXDS collisionConfig = instance.collisionConfig;
			if (collisionConfig == null)
			{
				return;
			}
			float? num = collisionConfig.maxCarCollisionImpact * collisionConfig.maxCarCollisionImpact;
			RaceCar car = m_raceCarCollderCache.GetCar(collInfo);
			if (car == null)
			{
				return;
			}
			float num2 = DriftControllerXDS.CalcuateCarCollisonImpactSqr(collInfo, ghostCar, car);
			if (!num.HasValue || num2 < num || !(UIXDSInfoContext.instance.controller.tandemLeader == ghostCar))
			{
				return;
			}
			bool isLeaderCritBrakingDetected = UIXDSInfoContext.instance.controller.isLeaderCritBrakingDetected;
			DisqualifyReason reason = (isLeaderCritBrakingDetected ? DisqualifyReason.LeaderCarCollide : DisqualifyReason.ChaserCarCollide);
			NetGameSubroomsSystem netGameSubroomsSystem = NetworkController.InstanceGame.systems.Get<NetGameSubroomsSystem>();
			if (netGameSubroomsSystem != null && netGameSubroomsSystem.myRoom != null)
			{
				netGameSubroomsSystem.DisqualifyYourself(reason);
				return;
			}
			reason = (isLeaderCritBrakingDetected ? DisqualifyReason.CarCollide : DisqualifyReason.ChaserCarCollide);
			UIXDSInfoContext instance2 = UIXDSInfoContext.instance;
			if (instance2 != null && instance2.controller != null)
			{
				instance2.controller.DisqualifyYourself(reason);
			}
		}

		public void Init(StartArgs args, bool setCarActive)
		{
			globals = DependencyInjector.Resolve<GamePrefs>().globals;
			if (args == null)
			{
				return;
			}
			m_type = ((!args.isUser) ? PlayerType.Opponent : PlayerType.User);
			ghostData = args.data;
			timeOffset = args.timeOffset;
			isPlay = false;
			DestroyCar();
			CarVisualManager.VisualSettings visualSettings = (args.visualSettings ?? args.data.visual).Copy();
			if (visualSettings != null && !CarPartDatabase.IsValid(ghostData.carId, visualSettings.parts))
			{
				visualSettings.parts.Clear();
			}
			ProfilesInfo.Profile profile = args.profileData ?? args.data.profile;
			PlayerCarFactory playerCarFactory = DependencyInjector.Resolve<PlayerCarFactory>();
			m_isCustomCar = args.carForPlay != null;
			ghostCar = args.carForPlay;
			if (ghostCar == null)
			{
				ghostCar = playerCarFactory.CreateGhostCarRealistic(ghostData.carId, ghostData.profileId, ghostData.swapId, visualSettings, profile, args.needCollisions, args.platformUserId);
				if ((bool)ghostCar && args.needCollisions)
				{
					ghostCar.OnCollisionEnterEvent += HandleCollision;
				}
				if (ghostCar == null || args.forceInitDefaultCar)
				{
					GameLog.Print(AlertLevel.Notify, "Ghost", "GhostPlayer::Init(): fallback to default car");
					CarVisualManager.VisualSettings visualForCar = DependencyInjector.Resolve<GamePrefs>().carSettings.GetVisualForCar(1);
					ghostCar = playerCarFactory.CreateGhostCarRealistic(1, 1, ghostData.swapId, visualSettings ?? visualForCar, profile, args.needCollisions, args.platformUserId);
				}
			}
			ghostCar.carX.integrationSteps = 16;
			ghostCar.carX.gearShifting = GearShiftType.Manual;
			ghostCar.carX.autoClutch = true;
			m_carGo = ghostCar.gameObject;
			m_carGo.SetActive(setCarActive);
			if (args.setupCamera)
			{
				SetupCamera(followCar: true, setCarActive);
			}
			m_nickName = args.nickName;
			m_visibleOnlyInProgress = args.visibleOnlyInProgress;
		}

		public void StopCar()
		{
			if ((bool)ghostCar)
			{
				Car carX = ghostCar.carX;
				carX.accelerate = 0f;
				carX.brake = 1f;
				carX.handbrake = 1f;
			}
		}

		public void Play(float time, bool networkMode)
		{
			if (ghostData == null)
			{
				return;
			}
			OnDPChangedEvent.SafeCall(ghostData.PlaybackDriftPoints(time));
			if (ghostCar == null)
			{
				return;
			}
			if (m_visibleOnlyInProgress)
			{
				GetTimeInterval(out var begTime, out var endTime);
				if (time < begTime || time > endTime)
				{
					if (m_carGo.activeSelf)
					{
						m_carGo.SetActive(value: false);
						ClearEffects();
						m_isCameraSetted = false;
					}
					return;
				}
			}
			if (ghostData.PlaybackRaceCar(time, ghostCar))
			{
				m_carGo.SetActive(visible);
				if (m_visibleOnlyInProgress)
				{
					SetupCamera(followCar: false);
				}
			}
		}

		public void GetTimeInterval(out float begTime, out float endTime)
		{
			begTime = float.MaxValue;
			endTime = float.MinValue;
			if (ghostData != null)
			{
				ghostData.GetTimeInterval(out begTime, out endTime);
			}
		}

		public void ClearEffects()
		{
			if (!(ghostCar == null))
			{
				ghostCar.ClearSmoke();
				ghostCar.ClearSkidmark();
			}
		}

		public void ClearSmoke()
		{
			if (!(ghostCar == null))
			{
				ghostCar.ClearSmoke();
			}
		}

		public void Destroy()
		{
			ghostData = null;
			ClearEffects();
			DestroyCar();
		}
	}

	private float m_begTime;

	private float m_endTime;

	private float m_curTime;

	private float m_rewindTime;

	private bool m_isUpdateSubscribed;

	public bool holdLastFrame;

	public bool networkMode;

	public bool unscaledTimer;

	public bool isReverse;

	public Action onPlayEndEvent;

	public List<Player> players { get; private set; } = new List<Player>();

	public bool isRewind => m_rewindTime != 0f;

	public float length => m_endTime - m_begTime;

	public float progress
	{
		get
		{
			if (length == 0f)
			{
				return 0f;
			}
			return (m_curTime - m_begTime) / length;
		}
		set
		{
			m_curTime = m_begTime + Mathf.Clamp01(value) * length;
		}
	}

	public bool isPlaying
	{
		get
		{
			if (m_isUpdateSubscribed)
			{
				return !isPaused;
			}
			return false;
		}
	}

	public bool isPaused { get; private set; }

	public RaceCar ghostCar
	{
		get
		{
			using (List<Player>.Enumerator enumerator = players.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					return enumerator.Current.ghostCar;
				}
			}
			return null;
		}
	}

	public GhostData ghostData
	{
		get
		{
			using (List<Player>.Enumerator enumerator = players.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					return enumerator.Current.ghostData;
				}
			}
			return null;
		}
	}

	public GhostPlayer(bool networkMode = false)
	{
		this.networkMode = networkMode;
	}

	private void PlayersUpdateTimeInterval()
	{
		m_begTime = float.MaxValue;
		m_endTime = float.MinValue;
		foreach (Player player in players)
		{
			player.GetTimeInterval(out var begTime, out var endTime);
			m_begTime = Mathf.Min(m_begTime, begTime);
			m_endTime = Mathf.Max(m_endTime, endTime);
		}
		if (m_endTime < m_begTime)
		{
			m_begTime = 0f;
			m_endTime = 0f;
		}
	}

	private void SubscribeUpdate()
	{
		if (!m_isUpdateSubscribed)
		{
			TimeController.onAfterFixedUpdate = (Action)Delegate.Combine(TimeController.onAfterFixedUpdate, new Action(OnUpdate));
			m_isUpdateSubscribed = true;
		}
	}

	private void UnsubscribeUpdate()
	{
		if (m_isUpdateSubscribed)
		{
			TimeController.onAfterFixedUpdate = (Action)Delegate.Remove(TimeController.onAfterFixedUpdate, new Action(OnUpdate));
			m_isUpdateSubscribed = false;
		}
	}

	public Player AddPlayer(StartArgs startArgs, bool setCarActiveAtStart = false)
	{
		Player player = new Player();
		player.Init(startArgs, setCarActiveAtStart);
		players.Add(player);
		PlayersUpdateTimeInterval();
		SubscribeUpdate();
		return player;
	}

	public void ClearPlayers()
	{
		foreach (Player player in players)
		{
			player.Destroy();
		}
		players.Clear();
		PlayersUpdateTimeInterval();
		m_curTime = 0f;
		UnsubscribeUpdate();
	}

	public void ClearEffects()
	{
		foreach (Player player in players)
		{
			player.ClearEffects();
		}
	}

	public void ClearSmokes()
	{
		foreach (Player player in players)
		{
			player.ClearSmoke();
		}
	}

	public void CurTimeOverride(float time)
	{
		m_curTime = time;
	}

	public void Rewind(float time)
	{
		holdLastFrame = false;
		m_rewindTime = time;
	}

	public void Stop()
	{
		UnsubscribeUpdate();
		progress = 0f;
	}

	public void Pause()
	{
		isPaused = true;
	}

	public void IgnoreCollisions()
	{
		for (int i = 0; i < players.Count; i++)
		{
			Player player = players[i];
			player.ghostCar.OnCollisionEnterEvent -= player.HandleCollision;
		}
	}

	public void Play()
	{
		holdLastFrame = false;
		for (int i = 0; i < players.Count; i++)
		{
			players[i].isPlay = true;
		}
		isPaused = false;
	}

	public void StopCars()
	{
		Stop();
		for (int i = 0; i < players.Count; i++)
		{
			players[i].StopCar();
		}
	}

	private void PlayProgress(Player player)
	{
		if (player.isPlay)
		{
			float time = m_curTime;
			if (player.timeOffset != 0f)
			{
				time = Mathf.Clamp(m_curTime + player.timeOffset, m_begTime, m_endTime);
			}
			if (m_rewindTime < 0f)
			{
				player.ClearSmoke();
			}
			player.Play(time, networkMode);
		}
	}

	private void PlayProgress()
	{
		if (!(length > 0f))
		{
			return;
		}
		float num = (unscaledTimer ? Time.unscaledDeltaTime : Time.deltaTime);
		if (isPaused)
		{
			num = 0f;
		}
		if (m_rewindTime != 0f)
		{
			m_curTime += m_rewindTime;
		}
		else
		{
			m_curTime += (isReverse ? (0f - num) : num);
		}
		m_curTime = Mathf.Clamp(m_curTime, m_begTime, m_endTime);
		if (m_curTime == m_endTime && !holdLastFrame)
		{
			if (networkMode)
			{
				StopCars();
			}
			onPlayEndEvent.Call();
			return;
		}
		foreach (Player player in players)
		{
			PlayProgress(player);
		}
	}

	public void ClearAll()
	{
		ClearPlayers();
	}

	public void SubscribeUsersDPChanged(Action<int> action, bool subscribe = true)
	{
		foreach (Player player in players)
		{
			if (player.Type == Player.PlayerType.User)
			{
				if (subscribe)
				{
					player.OnDPChangedEvent = (Action<int>)Delegate.Combine(player.OnDPChangedEvent, action);
				}
				else
				{
					player.OnDPChangedEvent = (Action<int>)Delegate.Remove(player.OnDPChangedEvent, action);
				}
			}
		}
	}

	public void SubscribeOpponentsDPChanged(Action<int> action, bool subscribe = true)
	{
		foreach (Player player in players)
		{
			if (player.Type == Player.PlayerType.Opponent)
			{
				if (subscribe)
				{
					player.OnDPChangedEvent = (Action<int>)Delegate.Combine(player.OnDPChangedEvent, action);
				}
				else
				{
					player.OnDPChangedEvent = (Action<int>)Delegate.Remove(player.OnDPChangedEvent, action);
				}
			}
		}
	}

	public string GetUserNickName()
	{
		foreach (Player player in players)
		{
			if (player.Type == Player.PlayerType.User)
			{
				return player.NickName;
			}
		}
		return string.Empty;
	}

	public string GetOpponentNickName()
	{
		foreach (Player player in players)
		{
			if (player.Type == Player.PlayerType.Opponent)
			{
				return player.NickName;
			}
		}
		return string.Empty;
	}

	private void OnUpdate()
	{
		PlayProgress();
	}
}
