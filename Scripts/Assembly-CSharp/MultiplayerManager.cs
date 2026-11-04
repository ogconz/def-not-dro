using System;
using System.Collections.Generic;
using System.IO;
using CodeStage.AntiCheat.ObscuredTypes;
using DB;
using DB.Meta;
using DI;
using SimpleJSON;
using UnityEngine;

public class MultiplayerManager
{
	public enum EMultiplayerState
	{
		Idle = 0,
		LoggingIn = 1,
		Operational = 2,
		StartingRace = 3,
		TrainingRace = 4,
		QualificationRace = 5,
		BattleRace = 6,
		Replay = 7,
		TopReplay = 8
	}

	public abstract class BaseHighlight
	{
	}

	public class SeasonEndHighlight : BaseHighlight
	{
	}

	public class ErrorHighlight : BaseHighlight
	{
		public enum Type
		{
			None = 0,
			Login = 1,
			RaceCreation = 2
		}

		public Type type;

		public string errorMessage;

		public Error error;

		public ErrorHighlight(string errorMessage, Error error, Type type = Type.None)
		{
			this.errorMessage = errorMessage;
			this.type = type;
			this.error = error;
		}
	}

	public class LoginHighlight : BaseHighlight
	{
		public SeasonChangeReward seasonChangeReward;
	}

	public abstract class RaceHighlight : BaseHighlight
	{
		public int dpResult { get; private set; }

		protected RaceHighlight(int dpResult)
		{
			this.dpResult = dpResult;
		}
	}

	public class TrainingRaceHighlight : RaceHighlight
	{
		public TrainingRaceHighlight(int dpResult)
			: base(dpResult)
		{
		}
	}

	public class QualificationRaceHighlight : RaceHighlight
	{
		public int previousDpResult { get; private set; }

		public QualificationRaceHighlight(int dpResult, int previousDpResult)
			: base(dpResult)
		{
			this.previousDpResult = previousDpResult;
		}
	}

	public class BattleRaceHighlight : RaceHighlight
	{
		public bool isVictory { get; private set; }

		public string opponentNickname { get; private set; }

		public int? opponentId { get; private set; }

		public MultiplayerGhost opponentGhost { get; private set; }

		public BattleRaceHighlight(bool isVictory, int dpResult, string opponentNickname, int? opponentId, MultiplayerGhost opponentGhost)
			: base(dpResult)
		{
			this.isVictory = isVictory;
			this.opponentNickname = opponentNickname;
			this.opponentId = opponentId;
			this.opponentGhost = opponentGhost;
		}
	}

	private const int AttackablePositionsDelta = 30;

	private const int UpdateUserEverySeconds = 120;

	private const byte SeasonEndingMin = 10;

	public Action<EMultiplayerState> onStateChange;

	public Action<User> onUserLoggedIn;

	public Action<User> onCurrentUserChanged;

	public Action onGhostUploadStart;

	public Action<Race> onRaceResultReceived;

	[Dependency]
	private GamePrefs m_prefs;

	[Dependency]
	private BaseModel m_model;

	[Dependency]
	private TransactionManager m_transactionManager;

	private Protocol m_protocol;

	private Session m_session;

	private ActionHolder m_seasonEndCallback;

	private ActionHolder m_updateUserCallback;

	private ActionHolder m_ghostOutdateCallback;

	private ActionHolder m_fuelRestoresCallback;

	private Opponent m_currentOpponent;

	private Race m_currentRace;

	private Race m_lastFailedToSendQuali;

	private List<League> m_allLeagues;

	private TopReplay m_currentTopReplay;

	private BaseHighlight m_latestHighlight;

	private MultiplayerTops m_multiplayerTops = new MultiplayerTops();

	private string m_loadedRaceLevel = "";

	private ObscuredInt m_increasedMaxFuel = 12;

	public readonly KeyValuePair<int, string>[] magicTokens = new KeyValuePair<int, string>[12]
	{
		new KeyValuePair<int, string>(0, "test_me_plenty_lol_1"),
		new KeyValuePair<int, string>(1, "test_me_plenty_lol_2"),
		new KeyValuePair<int, string>(2, "test_me_plenty_bug_42"),
		new KeyValuePair<int, string>(3, "test_me_plenty_MickeyMouse"),
		new KeyValuePair<int, string>(4, "test_me_plenty_LeninGrib"),
		new KeyValuePair<int, string>(5, "test_me_plenty_Bug_A"),
		new KeyValuePair<int, string>(6, "test_me_plenty_Bug_B"),
		new KeyValuePair<int, string>(7, "test_me_plenty_Bug_C"),
		new KeyValuePair<int, string>(8, "test_me_plenty_Bug_D"),
		new KeyValuePair<int, string>(9, "test_me_plenty_Bug_E"),
		new KeyValuePair<int, string>(9, "test_me_plenty_Ferz"),
		new KeyValuePair<int, string>(9, "test_me_plenty_Ferz2")
	};

	public Action<bool> isBusyEvent;

	public Action<Error> needReloginEvent;

	public Action<int> topReceived;

	public Action<bool> topReplayViewed;

	public EMultiplayerState currentState { get; private set; }

	public List<Opponent> divisionRating
	{
		get
		{
			if (m_session != null)
			{
				return m_session.ratings;
			}
			return null;
		}
	}

	public Season currentSeason
	{
		get
		{
			if (currentUser != null)
			{
				return currentUser.season;
			}
			return null;
		}
	}

	public User currentUser
	{
		get
		{
			if (m_session != null)
			{
				return m_session.user;
			}
			return null;
		}
	}

	public League currentLeague
	{
		get
		{
			if (m_session != null)
			{
				return m_session.league;
			}
			return null;
		}
	}

	public DeprecatedSettings currentSettings
	{
		get
		{
			if (m_session != null)
			{
				return m_session.config;
			}
			return null;
		}
	}

	public Opponent currentOpponent => m_currentOpponent;

	public MultiplayerGhost currentUserGhost
	{
		get
		{
			if (currentUser != null)
			{
				return currentUser.ghost;
			}
			return null;
		}
	}

	public bool isInNetworkRace
	{
		get
		{
			if (currentState != EMultiplayerState.BattleRace && currentState != EMultiplayerState.QualificationRace && currentState != EMultiplayerState.TrainingRace && currentState != EMultiplayerState.Replay)
			{
				return currentState == EMultiplayerState.TopReplay;
			}
			return true;
		}
	}

	public Race LastRace { get; private set; }

	public int IncreasedMaxFuel
	{
		get
		{
			return m_increasedMaxFuel;
		}
		set
		{
			m_increasedMaxFuel = value;
		}
	}

	public MultiplayerManager()
	{
		DependencyInjector.InjectObjectsInto(this);
	}

	public void Initialize(string serverUrl)
	{
		m_protocol = new Protocol(serverUrl, OnRequestTotalFailure);
		m_protocol.ApiClient.needReloginEvent = delegate(Error error)
		{
			needReloginEvent.Call(error);
		};
		SetState(EMultiplayerState.Idle);
		States.onAboutToLoadLevel += delegate
		{
			m_loadedRaceLevel = "";
		};
		onCurrentUserChanged = (Action<User>)Delegate.Combine(onCurrentUserChanged, (Action<User>)delegate(User user)
		{
			Season season = user.season;
			if (season != null)
			{
				TimeController.RemoveCallback(m_seasonEndCallback);
				m_seasonEndCallback = TimeController.AddCallback(season.secondsTillTheSeasonEnds.GetValueOrDefault(), OnSeasonEnd);
			}
			TimeController.RemoveCallback(m_ghostOutdateCallback);
			if (user.ghost != null && user.ghost.secondsTillOutdate.HasValue && user.ghost.secondsTillOutdate.Value > 0f)
			{
				m_ghostOutdateCallback = TimeController.AddCallback(user.ghost.secondsTillOutdate.Value, UpdateCurrentUser);
			}
			TimeController.RemoveCallback(m_fuelRestoresCallback);
			if (user.secondsTillFuelRestore.HasValue && user.secondsTillFuelRestore.Value > 0f)
			{
				m_fuelRestoresCallback = TimeController.AddCallback(user.secondsTillFuelRestore.Value, UpdateCurrentUser);
			}
			TimeController.RemoveCallback(m_updateUserCallback);
			m_updateUserCallback = TimeController.AddCallback(120f, UpdateCurrentUser);
			RefreshUserPosition();
		});
	}

	private void OnRequestTotalFailure(Error details)
	{
		if (currentState != EMultiplayerState.Idle)
		{
			ErrorHighlight reason = null;
			if (details.guiMessage != null)
			{
				reason = new ErrorHighlight(details.guiMessage, details);
			}
			Debug.LogError("Total request failure: " + details);
			Exit(reason);
		}
	}

	private void SetHighlight(BaseHighlight newHighlight)
	{
		m_latestHighlight = newHighlight;
	}

	public BaseHighlight GetLatestHighlight()
	{
		return m_latestHighlight;
	}

	public void RemoveLatestHighlight()
	{
		m_latestHighlight = null;
	}

	public void GetUserRating(int id, Action<UserElo, Error> callback)
	{
		m_protocol.UnauthorizedGetRating(id, callback);
	}

	public void Ping(Action<bool> callback)
	{
		Action<Protocol.Success, Error> callback2 = delegate(Protocol.Success result, Error error)
		{
			callback.SafeCall(result != null);
		};
		m_protocol.Ping(callback2);
	}

	public void Login(string token = null)
	{
		if (currentState != EMultiplayerState.Idle)
		{
			Debug.LogError("Trying to log in while in the state " + currentState);
			return;
		}
		SetState(EMultiplayerState.LoggingIn);
		LoginStandalone(token ?? magicTokens[0].Value);
	}

	private void LoginStandalone(string token)
	{
		m_protocol.AuthenticateGoogle(token, LoginEnding);
	}

	private void LoginEnding(Session session, Error error)
	{
		if (error.type != Error.EType.None)
		{
			Debug.LogError("Could not login: " + error);
			Exit(new ErrorHighlight(error.guiMessage, error, ErrorHighlight.Type.Login));
			return;
		}
		ErrorHighlight reason = new ErrorHighlight("Corrupted data received. Try to reconnect and try again.", error, ErrorHighlight.Type.Login);
		if (session.user == null || session.league == null || session.ratings == null || session.user.season == null || session.league.clientSettings == null || session.league.clientSettings.IsEmpty())
		{
			Debug.LogError("Partial session received from server. Exiting.");
			Exit(reason);
			return;
		}
		session.league.Prepare();
		if (session.seasonChangeReward != null)
		{
			m_transactionManager.OnMultiplayerRewardAcquired(session.seasonChangeReward.reward);
			SeasonChangeReward seasonChangeReward = null;
			if (session.seasonChangeReward.reward.coins > 0 || session.seasonChangeReward.reward.cash > 0)
			{
				seasonChangeReward = session.seasonChangeReward;
			}
			SetHighlight(new LoginHighlight
			{
				seasonChangeReward = seasonChangeReward
			});
		}
		m_session = session;
		SetRating(m_session.ratings);
		onCurrentUserChanged.SafeCall(m_session.user);
		Debug.Log("Logged in as user " + m_session.user.id);
		SetState(EMultiplayerState.Operational);
		onUserLoggedIn.SafeCall(currentUser);
	}

	public void Exit(BaseHighlight reason = null)
	{
		SetHighlight(reason);
		SetState(EMultiplayerState.Idle);
		TimeController.RemoveCallback(m_seasonEndCallback);
		TimeController.RemoveCallback(m_ghostOutdateCallback);
		TimeController.RemoveCallback(m_fuelRestoresCallback);
		TimeController.RemoveCallback(m_updateUserCallback);
		m_seasonEndCallback = null;
		m_ghostOutdateCallback = null;
		m_fuelRestoresCallback = null;
		m_updateUserCallback = null;
		m_session = null;
		m_currentOpponent = null;
		m_currentRace = null;
		m_multiplayerTops.Clear();
	}

	private void OnSeasonEnd()
	{
		Debug.Log("Multiplayer season has ended. Exiting MM.");
		Exit(new SeasonEndHighlight());
	}

	public string GetUserNickname(int? userId)
	{
		if (divisionRating == null || !userId.HasValue)
		{
			return "Unknown user";
		}
		Opponent opponent = divisionRating.Find((Opponent u) => u.id == userId);
		if (opponent == null)
		{
			return "Unknown user";
		}
		return opponent.nickname;
	}

	public bool IsSeasonEnding()
	{
		if (currentSeason != null)
		{
			int num = (int)currentSeason.secondsTillTheSeasonEnds.GetValueOrDefault();
			if (num < 0)
			{
				num = 0;
			}
			return num < 600;
		}
		return true;
	}

	public bool IsSeasonEnded()
	{
		BaseHighlight latestHighlight = GetLatestHighlight();
		if (latestHighlight == null)
		{
			return false;
		}
		return latestHighlight is SeasonEndHighlight;
	}

	public bool IsRaceAvailableByTime()
	{
		if (currentSeason == null || currentSettings == null)
		{
			return false;
		}
		if ((int)currentSeason.secondsTillTheSeasonEnds.GetValueOrDefault() <= currentSettings.seasonEndNoRacesTimeIn)
		{
			return false;
		}
		return true;
	}

	public bool IsTrainingAvailable()
	{
		return m_transactionManager.IsMoneyEnoughToAfford(currentLeague.trainingCost);
	}

	public bool IsQualificationAvailable()
	{
		if (currentUser != null)
		{
			return currentUser.qualiNotAvailableReason == Error.EType.None;
		}
		return false;
	}

	public Error.EType GetBattleUnavailabilityReason(int? userId)
	{
		if (currentUser == null)
		{
			return Error.EType.Unknown;
		}
		Opponent opponent = ((divisionRating == null) ? null : divisionRating.Find((Opponent u) => u.id == userId));
		if (!userId.HasValue || opponent == null || !opponent.isAttackable)
		{
			return Error.EType.Unknown;
		}
		if (!opponent.position.HasValue || !currentUser.position.HasValue)
		{
			return Error.EType.Unknown;
		}
		if (currentUser.position.Value - opponent.position.Value > currentSettings.attackablePositionsAhead)
		{
			return Error.EType.Unknown;
		}
		if (opponent.position.Value - currentUser.position.Value > currentSettings.attackablePositionsBehind)
		{
			return Error.EType.Unknown;
		}
		return currentUser.battleNotAvailableReason;
	}

	public void UpdateRatings(Action<List<Opponent>> callback)
	{
		if (currentState != EMultiplayerState.Operational)
		{
			Debug.LogError("Trying to get ratings while in the state " + currentState);
			callback.SafeCall(null);
			return;
		}
		Action<List<Opponent>, Error> callback2 = delegate(List<Opponent> list, Error error)
		{
			if (currentState != EMultiplayerState.Operational)
			{
				ClearBusy();
			}
			else
			{
				if (list != null)
				{
					SetRating(list);
					callback.SafeCall(m_session.ratings);
				}
				else
				{
					callback.SafeCall(null);
				}
				ClearBusy();
			}
		};
		SetBusy();
		m_protocol.GetRating(m_session.sessionKey, callback2);
	}

	private void SetRating(List<Opponent> list)
	{
		int num = 0;
		foreach (Opponent item in list)
		{
			item.position = (num += 1);
		}
		m_session.ratings = list;
		RefreshUserPosition();
	}

	private void RefreshUserPosition()
	{
		foreach (Opponent rating in m_session.ratings)
		{
			if (m_session.user.id == rating.id)
			{
				m_session.user.position = rating.position;
				break;
			}
		}
	}

	public void GetRaceLog(Action<List<Race>> callback)
	{
		if (currentState != EMultiplayerState.Operational)
		{
			Debug.LogError("Trying to get race log while in the state " + currentState);
			callback.SafeCall(null);
			return;
		}
		Action<List<Race>, Error> callback2 = delegate(List<Race> list, Error error)
		{
			if (currentState == EMultiplayerState.Operational)
			{
				callback.SafeCall(list);
			}
		};
		m_protocol.GetRaceLog(m_session.sessionKey, callback2);
	}

	public void GetLeagues(Action<List<League>> callback)
	{
		if (m_allLeagues != null && m_allLeagues.Count != 0)
		{
			callback.SafeCall(m_allLeagues);
			return;
		}
		Action<List<League>, Error> callback2 = delegate(List<League> list, Error error)
		{
			foreach (League item in list)
			{
				item.Prepare();
			}
			m_allLeagues = list;
			callback.SafeCall(m_allLeagues);
		};
		m_protocol.GetLeagues(m_session.sessionKey, callback2);
	}

	private void LoadLevelOrRestart<T>(Action<T> onStateActivated = null) where T : BaseState
	{
		if (currentLeague == null)
		{
			Debug.LogError("Null current league. Exiting.");
			Exit();
			return;
		}
		League.LeagueClientSettings clientSettings = currentLeague.clientSettings;
		if (clientSettings == null)
		{
			Debug.LogError("Null client settings for the current league. Exiting.");
			Exit();
			return;
		}
		string trackName = clientSettings.mapName;
		if (m_loadedRaceLevel == trackName)
		{
			if (!States.TryRestart<T>())
			{
				LoadRaceState.StartArgs startArgs = new LoadRaceState.StartArgs();
				startArgs.loadingType = LoadRaceState.StartArgs.LoadingType.NormalEmulation;
				startArgs.nextStateType = typeof(T);
				startArgs.onNextStateActivated = delegate(BaseState state)
				{
					onStateActivated.Call((T)state);
				};
				States.State<LoadRaceState>().LoadRaceCommon(startArgs);
			}
		}
		else
		{
			LoadRaceState.StartArgs startArgs2 = new LoadRaceState.StartArgs();
			startArgs2.trackName = trackName;
			startArgs2.trackConfiguration = clientSettings.configurationName;
			startArgs2.nextStateType = typeof(T);
			startArgs2.onNextStateActivated = delegate(BaseState state)
			{
				m_loadedRaceLevel = trackName;
				onStateActivated.Call((T)state);
			};
			States.State<LoadRaceState>().LoadRaceCommon(startArgs2);
		}
	}

	private void LoadLevelOrRestart<T>(League.LeagueClientSettings clientSettings, Action<T> onStateActivated = null) where T : BaseState
	{
		if (clientSettings == null)
		{
			Debug.LogError("Null client settings for the current league. Exiting.");
			Exit();
			return;
		}
		if (m_loadedRaceLevel == clientSettings.mapName)
		{
			if (!States.TryRestart<T>())
			{
				States.Change<T>();
				onStateActivated.Call((T)States.current);
			}
			return;
		}
		LoadRaceState.StartArgs startArgs = new LoadRaceState.StartArgs();
		startArgs.trackName = clientSettings.mapName;
		startArgs.trackConfiguration = clientSettings.configurationName;
		startArgs.nextStateType = typeof(T);
		startArgs.onNextStateActivated = delegate(BaseState state)
		{
			m_loadedRaceLevel = clientSettings.mapName;
			onStateActivated.Call((T)state);
		};
		States.State<LoadRaceState>().LoadRaceCommon(startArgs);
	}

	public void RetryQualiResultSending()
	{
		SendQualiResult(m_lastFailedToSendQuali);
	}

	private bool StartAnyRace(EMultiplayerState targetState)
	{
		if (currentState != EMultiplayerState.StartingRace)
		{
			Debug.LogError("Trying to start " + targetState.ToString() + " while in the state " + currentState);
			return false;
		}
		if (currentLeague.clientSettings == null)
		{
			Debug.LogError("Null client settings for the current league. Exiting.");
			Exit();
			return false;
		}
		SetState(targetState);
		LoadLevelOrRestart<NetworkRaceState>();
		return true;
	}

	private void OnFailToCreateRace(Error error)
	{
		if (error.guiMessage != null)
		{
			SetHighlight(new ErrorHighlight(error.guiMessage, error, ErrorHighlight.Type.RaceCreation));
		}
		SetState(EMultiplayerState.Operational);
		Debug.LogError("Could not create race: " + error.debugMessage);
	}

	private void OnRaceCreationResponse(Race race, Error error, EMultiplayerState targetState)
	{
		if (currentState == EMultiplayerState.StartingRace)
		{
			if (error.type != Error.EType.None || race == null)
			{
				OnFailToCreateRace(error);
				return;
			}
			m_currentRace = race;
			StartAnyRace(targetState);
		}
	}

	public MultiplayerGhost GetOpponentGhost()
	{
		if (m_currentRace != null)
		{
			return m_currentRace.opponentGhost;
		}
		return null;
	}

	public TopReplay GetCurrentTopReplay()
	{
		return m_currentTopReplay;
	}

	public bool StartTrainingRace()
	{
		if (!IsTrainingAvailable() || IsSeasonEnding())
		{
			return false;
		}
		m_currentRace = new Race
		{
			type = "training",
			opponentId = currentUser.id,
			userId = currentUser.id
		};
		SetState(EMultiplayerState.StartingRace);
		return StartAnyRace(EMultiplayerState.TrainingRace);
	}

	public bool StartQualificationRace()
	{
		if (!IsQualificationAvailable())
		{
			return false;
		}
		SetState(EMultiplayerState.StartingRace);
		m_protocol.CreateRace(isQuali: true, currentUser.id.GetValueOrDefault(), GetMaxMultiplierAmongUnlockedCars(), m_session.sessionKey, delegate(Race race, Error error)
		{
			OnRaceCreationResponse(race, error, EMultiplayerState.QualificationRace);
		});
		return true;
	}

	private void SendQualiResult(Race qualiRace)
	{
		if (qualiRace == null)
		{
			Debug.LogError("QualiRace is null");
			return;
		}
		if (m_session == null || m_session.sessionKey == null)
		{
			Debug.LogError("Session or SessionKey is null");
			return;
		}
		LastRace = null;
		m_protocol.SaveQuali(qualiRace, m_session.sessionKey, delegate(Race race, Error error)
		{
			LastRace = race;
			if (race != null)
			{
				m_transactionManager.OnMultiplayerRewardAcquired(race.reward);
				m_lastFailedToSendQuali = null;
			}
			else
			{
				m_lastFailedToSendQuali = qualiRace;
			}
			if (currentState == EMultiplayerState.Operational)
			{
				Debug.Log("Sent user ghost successfully: " + (race != null));
				onRaceResultReceived.SafeCall(race);
				UpdateCurrentUser();
			}
		});
	}

	public void ReportRaceResult(int dpResult, MemoryStream ghostStream)
	{
		MultiplayerGhost.ClientData clientData = GetClientData();
		m_currentRace.driftPoints = dpResult;
		m_currentRace.userGhost = new MultiplayerGhost(ghostStream.ReadFully(), dpResult, clientData);
		switch (currentState)
		{
		case EMultiplayerState.QualificationRace:
			onGhostUploadStart.SafeCall();
			SendQualiResult(m_currentRace);
			SetHighlight(new QualificationRaceHighlight(dpResult, (currentUser.ghost == null) ? (-1) : currentUser.ghost.driftPoints));
			Debug.Log("Reported qualification result: " + dpResult);
			break;
		case EMultiplayerState.TrainingRace:
			Debug.Log("Reported training result: " + dpResult);
			SetHighlight(new TrainingRaceHighlight(dpResult));
			break;
		case EMultiplayerState.BattleRace:
		{
			Debug.Log("Reported attack result: " + dpResult);
			MultiplayerGhost opponentGhost = m_currentRace.opponentGhost;
			bool flag = dpResult > opponentGhost.driftPoints;
			SetHighlight(new BattleRaceHighlight(flag, dpResult, m_currentOpponent.nickname, m_currentOpponent.id, opponentGhost));
			Race.ERaceResult result = (flag ? Race.ERaceResult.us : Race.ERaceResult.them);
			m_currentRace.opponentId = m_currentOpponent.id;
			m_currentRace.result = result;
			Race currentRaceCopy = m_currentRace;
			LastRace = null;
			m_protocol.SaveBattleResult(m_currentRace, m_session.sessionKey, delegate(Race race, Error error)
			{
				LastRace = race;
				if (race != null)
				{
					m_transactionManager.OnMultiplayerRewardAcquired(race.reward);
				}
				m_protocol.SaveBattleGhost(currentRaceCopy, m_session.sessionKey, null);
				if (currentState == EMultiplayerState.Operational)
				{
					onRaceResultReceived.SafeCall(race);
					UpdateCurrentUser();
				}
			});
			break;
		}
		default:
			Debug.LogError("Report result in the wrong state: " + currentState);
			break;
		}
		m_currentRace = null;
		SetState(EMultiplayerState.Operational);
	}

	private void SetState(EMultiplayerState newState)
	{
		bool num = currentState != newState;
		currentState = newState;
		if (num)
		{
			onStateChange.SafeCall(currentState);
		}
	}

	public void UpdateCurrentUser()
	{
		if (currentState == EMultiplayerState.Operational)
		{
			m_protocol.GetUser(currentUser.id, m_session.sessionKey, delegate(User user, Error error)
			{
				ChangeUser(user);
			});
		}
	}

	private void ChangeUser(User user)
	{
		if (user == null || currentState != EMultiplayerState.Operational)
		{
			Debug.LogError("Could not update current user with id " + currentUser.id);
			return;
		}
		m_session.user = user;
		onCurrentUserChanged.SafeCall(user);
	}

	private int GetMaxMultiplierAmongUnlockedCars()
	{
		return Mathf.RoundToInt(m_model.QueryAll<PlayerCar>().Reduce(0f, (PlayerCar car, float acc) => Mathf.Max(acc, car.coinsFactor)));
	}

	private void SetBusy()
	{
		isBusyEvent.SafeCall(param: true);
	}

	private void ClearBusy()
	{
		isBusyEvent.SafeCall(param: false);
	}

	public void UpdateTops(int leagueId)
	{
		if (m_multiplayerTops.UpdateNeeded(leagueId))
		{
			m_multiplayerTops.RemoveTop(leagueId);
			m_protocol.GetTopReplays(m_session.sessionKey, leagueId, m_multiplayerTops.GetNextPageAccessor(leagueId), TopReceived);
		}
		else
		{
			topReceived.SafeCall(leagueId);
		}
	}

	public bool GetNextTop(int leagueId)
	{
		int? nextPageAccessor = m_multiplayerTops.GetNextPageAccessor(leagueId);
		if (nextPageAccessor.HasValue && nextPageAccessor != 0)
		{
			m_protocol.GetTopReplays(m_session.sessionKey, leagueId, nextPageAccessor, TopNextReceived);
			return true;
		}
		return false;
	}

	public MultiplayerTop GetTop(int leagueId)
	{
		return m_multiplayerTops.GetTop(leagueId);
	}

	private void TopReceived(Top top, Error error)
	{
		if (top == null)
		{
			Debug.LogError("MultiplayerManager::TopReceived(): top is null.");
			topReceived.SafeCall(-1);
		}
		else
		{
			m_multiplayerTops.AddTop(top);
			topReceived.SafeCall(top.league);
		}
	}

	private void TopNextReceived(Top top, Error error)
	{
		if (top == null)
		{
			Debug.LogError("MultiplayerManager::TopNextReceived(): top is null.");
			topReceived.SafeCall(-1);
		}
		else
		{
			m_multiplayerTops.AddTop(top, next: true);
			topReceived.SafeCall(top.league);
		}
	}

	public void ClearTops()
	{
		m_multiplayerTops.Clear();
	}

	public MultiplayerGhost.ClientData GetClientData()
	{
		int lastCarId = m_prefs.globals.lastCarId;
		int profileIdForCar = m_prefs.carSettings.GetProfileIdForCar(lastCarId);
		return new MultiplayerGhost.ClientData
		{
			carId = lastCarId,
			profileId = profileIdForCar,
			visualSettings = JSON.Serialize(m_prefs.carSettings.GetVisualForCar(lastCarId)),
			profileData = JSON.Serialize(m_prefs.carSettings.GetProfileInfo(profileIdForCar, lastCarId))
		};
	}

	public int GetSecondsBeforeNextTopUpdate(int leagueId)
	{
		return m_multiplayerTops.GetSecondsBeforeNextTopUpdate(leagueId);
	}

	public void SendTopReplayLike(string raceId, Action<Error> callback)
	{
		m_protocol.SendTopReplayLike(m_session.sessionKey, raceId, callback);
	}

	public void SendTopReplayView(string raceId, Action<TopView, Error> callback)
	{
		m_protocol.SendTopReplayView(m_session.sessionKey, raceId, callback);
	}

	public void RaceResultClosed()
	{
		m_currentOpponent = null;
	}
}
