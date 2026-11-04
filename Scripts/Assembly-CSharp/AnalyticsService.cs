using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DB;
using DB.Meta;
using DI;
using GameInput;
using GameOverlay;
using Rewired;
using Services.GameAnalytics;

public static class AnalyticsService
{
	private static BaseModel m_model;

	private static GamePrefs m_prefs;

	private static string s_lastDeviceName = string.Empty;

	private static float s_earnedExpPerSession = 0f;

	private static List<ICommonGameAnalytics> m_analyticsServices;

	private static Dictionary<string, object> m_dictionaryToSend = new Dictionary<string, object>();

	private static EventCache s_eventCache = new EventCache();

	private static bool s_isInitialized = false;

	private static DesignEvent[] m_designEventsToSkip = new DesignEvent[7]
	{
		DesignEvent.PlayerStats,
		DesignEvent.CarProfileUse,
		DesignEvent.DriftPoints,
		DesignEvent.MoneyAdded,
		DesignEvent.RetryRace,
		DesignEvent.UITime,
		DesignEvent.PlayerDevice
	};

	private static ProgressionStatus[] m_progressionEventsToSkip = new ProgressionStatus[3]
	{
		ProgressionStatus.Start,
		ProgressionStatus.Complete,
		ProgressionStatus.Fail
	};

	private static Dictionary<string, object> GetDictionaryToSend()
	{
		m_dictionaryToSend.Clear();
		return m_dictionaryToSend;
	}

	public static async UniTaskVoid InitAsync(string currentUserId, List<ICommonGameAnalytics> commonAnalytics)
	{
		m_model = DependencyInjector.Resolve<BaseModel>();
		m_prefs = DependencyInjector.Resolve<GamePrefs>();
		m_analyticsServices = commonAnalytics;
		if (m_analyticsServices != null)
		{
			foreach (ICommonGameAnalytics analyticsService in m_analyticsServices)
			{
				await analyticsService.InitAsync(currentUserId);
			}
		}
		GameEvents.OnGameEngineStop += new GameEventHandler<GameEvents.EventInfo>(OnEndSession);
		s_isInitialized = true;
		ProcessCache();
	}

	public static void OnBodyPaintClick()
	{
		SendEvent(DesignEvent.SeparatePaint_Body.ToString());
	}

	public static void OnInteriorPaintClick()
	{
		SendEvent(DesignEvent.SeparatePaint_Interior.ToString());
	}

	public static void OnGlassPaintClick()
	{
		SendEvent(DesignEvent.SeparatePaint_Glass.ToString());
	}

	public static void OnSuspensionPaintClick()
	{
		SendEvent(DesignEvent.SeparatePaint_Suspension.ToString());
	}

	public static void OnSmokePaintClick()
	{
		SendEvent(DesignEvent.SeparatePaint_Smoke.ToString());
	}

	private static void OnEndSession(GameEvents.EventInfo e)
	{
		Dictionary<string, object> dictionaryToSend = GetDictionaryToSend();
		dictionaryToSend.Add("SessionExpCount", s_earnedExpPerSession);
		dictionaryToSend.Add("EventType", DesignEvent.ExpPerSession.ToString());
		SendEvent(DesignEvent.PlayerStats.ToString(), dictionaryToSend);
	}

	public static void LogUnlockTrack(string trackName)
	{
		GetDictionaryToSend().Add("TrackName", trackName);
		SendEvent(DesignEvent.UnlockTrack.ToString(), m_dictionaryToSend);
	}

	public static void LogUnlockProfile(string carName, string profileName)
	{
		Dictionary<string, object> dictionaryToSend = GetDictionaryToSend();
		dictionaryToSend.Add("ProfileName", profileName);
		dictionaryToSend.Add("CarName", carName);
		SendEvent(DesignEvent.UnlockProfile.ToString(), m_dictionaryToSend);
	}

	public static void LogUnlockCustomizationItem(string carPartType, int itemId)
	{
		Dictionary<string, object> dictionaryToSend = GetDictionaryToSend();
		dictionaryToSend.Add("PartType", carPartType);
		dictionaryToSend.Add("ItemId", itemId);
		SendEvent(DesignEvent.UnlockCustomizationItem.ToString(), m_dictionaryToSend);
	}

	public static void LogUnlockCar(string carName)
	{
		GetDictionaryToSend().Add("CarName", carName);
		SendEvent(DesignEvent.UnlockCar.ToString(), m_dictionaryToSend);
	}

	public static void MultiplayerRoomSpawned(string mode, string pointName)
	{
		Dictionary<string, object> dictionaryToSend = GetDictionaryToSend();
		dictionaryToSend.Add("MultiplayerMode", mode);
		dictionaryToSend.Add("PointName", pointName);
		SendEvent(DesignEvent.MultiplayerRoomCreated.ToString(), m_dictionaryToSend);
	}

	public static void MultiplayerRoomCanceled()
	{
		GetDictionaryToSend().Add("ProgressionEventType", ProgressionEvent.MultiplayerRoomCreate.ToString());
		SendEvent(ProgressionStatus.Fail.ToString(), m_dictionaryToSend);
	}

	public static void MultiplayerRoomStarted()
	{
		GetDictionaryToSend().Add("ProgressionEventType", ProgressionEvent.MultiplayerRoomCreate.ToString());
		SendEvent(ProgressionStatus.Complete.ToString(), m_dictionaryToSend);
	}

	public static void LogLaunchGame()
	{
		Dictionary<string, object> dictionaryToSend = GetDictionaryToSend();
		dictionaryToSend.Add("GameVersion", GameVersion.versionAsString);
		dictionaryToSend.Add("NetworkVersion", GameVersion.networkVersion);
		SendEvent(DesignEvent.LaunchGame.ToString(), m_dictionaryToSend);
	}

	public static void LogRetryRace(RaceMode raceMode)
	{
		string nameById = m_model.GetNameById<DB.Meta.Track>(m_prefs.globals.lastTrackId);
		Dictionary<string, object> dictionaryToSend = GetDictionaryToSend();
		dictionaryToSend.Add("TrackName", nameById);
		dictionaryToSend.Add("RaceMode", raceMode);
		SendEvent(DesignEvent.RetryRace.ToString(), m_dictionaryToSend);
	}

	public static void LogRestartRaceButton(RaceMode raceMode)
	{
		string nameById = m_model.GetNameById<DB.Meta.Track>(m_prefs.globals.lastTrackId);
		Dictionary<string, object> dictionaryToSend = GetDictionaryToSend();
		dictionaryToSend.Add("TrackName", nameById);
		dictionaryToSend.Add("RaceMode", raceMode);
		SendEvent(DesignEvent.RestartRaceButton.ToString(), m_dictionaryToSend);
	}

	public static void LogEndRace(bool complete, string trackName, RaceMode raceMode)
	{
		Dictionary<string, object> dictionaryToSend = GetDictionaryToSend();
		dictionaryToSend.Add("RaceType", "Race");
		dictionaryToSend.Add("TrackName", trackName);
		dictionaryToSend.Add("RaceMode", raceMode.ToString());
		SendEvent((complete ? ProgressionStatus.Complete : ProgressionStatus.Fail).ToString(), m_dictionaryToSend);
	}

	public static void LogStartRace(string trackName, RaceMode raceMode)
	{
		Dictionary<string, object> dictionaryToSend = GetDictionaryToSend();
		dictionaryToSend.Add("RaceType", "Race");
		dictionaryToSend.Add("TrackName", trackName);
		dictionaryToSend.Add("RaceMode", raceMode.ToString());
		SendEvent(ProgressionStatus.Start.ToString(), m_dictionaryToSend);
	}

	public static void LogSafeModeXDS()
	{
		GetDictionaryToSend();
		SendEvent(DesignEvent.SafeModeXDS.ToString(), m_dictionaryToSend);
	}

	public static void LogTandemXDSinRoom()
	{
		GetDictionaryToSend();
		SendEvent(DesignEvent.TandemXDSInRoom.ToString(), m_dictionaryToSend);
	}

	public static void CarProfileUse(string profileName)
	{
		GetDictionaryToSend().Add("ProfileName", profileName);
		SendEvent(DesignEvent.CarProfileUse.ToString(), m_dictionaryToSend);
	}

	public static void LogDeviceUse()
	{
		Controller lastActiveController = ReInput.controllers.GetLastActiveController();
		if (s_lastDeviceName != lastActiveController.Name())
		{
			Dictionary<string, object> dictionaryToSend = GetDictionaryToSend();
			dictionaryToSend.Add("ControlType", lastActiveController.type);
			dictionaryToSend.Add("ControlName", lastActiveController.name);
			SendEvent(DesignEvent.PlayerDevice.ToString(), m_dictionaryToSend);
			s_lastDeviceName = lastActiveController.Name();
		}
	}

	public static void StartMultiplayerRace(bool isMatchmaking)
	{
		string eventName = (isMatchmaking ? DesignEvent.RatingGame : DesignEvent.PlayerStats).ToString();
		string value = (isMatchmaking ? DesignEvent.PlayerCount : DesignEvent.PlayerDayCountMultiplayer).ToString();
		GetDictionaryToSend().Add("EventType", value);
		SendEvent(eventName, m_dictionaryToSend);
	}

	public static void StartSingleplayerRace()
	{
		GetDictionaryToSend().Add("EventType", DesignEvent.PlayerDayCountSngleplayer.ToString());
		SendEvent(DesignEvent.PlayerStats.ToString(), m_dictionaryToSend);
	}

	public static void PlayerQualificationFinish()
	{
		GetDictionaryToSend().Add("EventType", DesignEvent.PlayerQualificationCount.ToString());
		SendEvent(DesignEvent.RatingGame.ToString(), m_dictionaryToSend);
	}

	public static void DynoWheelAngle(string carName, float value)
	{
		Dictionary<string, object> dictionaryToSend = GetDictionaryToSend();
		dictionaryToSend.Add("WheelAngle", value);
		dictionaryToSend.Add("CarName", carName);
		SendEvent(DesignEvent.DynoWheelAngle.ToString(), m_dictionaryToSend);
	}

	public static void DynoTime(float time)
	{
		Dictionary<string, object> dictionaryToSend = GetDictionaryToSend();
		dictionaryToSend.Add("SpendTime", time);
		dictionaryToSend.Add("Context", "DynoUI");
		SendEvent(DesignEvent.UITime.ToString(), m_dictionaryToSend);
	}

	public static void DynoChangedParams(ICollection<string> paramsName)
	{
		Dictionary<string, object> dictionaryToSend = GetDictionaryToSend();
		int num = 0;
		foreach (string item in paramsName)
		{
			dictionaryToSend.Add(num.ToString(), item);
			num++;
		}
		SendEvent(DesignEvent.DynoState.ToString(), m_dictionaryToSend);
	}

	public static void MultiplayerRaceResult(bool isWin, string carName)
	{
		Dictionary<string, object> dictionaryToSend = GetDictionaryToSend();
		dictionaryToSend.Add("CarName", carName);
		dictionaryToSend.Add("IsWin", isWin);
		SendEvent(DesignEvent.WinRate.ToString(), m_dictionaryToSend);
	}

	public static void EarnDriftPoints(float points)
	{
		GetDictionaryToSend();
		string nameById = m_model.GetNameById<DB.Meta.Track>(m_prefs.globals.lastTrackId);
		Dictionary<string, object> dictionaryToSend = GetDictionaryToSend();
		dictionaryToSend.Add("Count", points);
		dictionaryToSend.Add("Track", nameById);
		SendEvent(DesignEvent.DriftPoints.ToString(), m_dictionaryToSend);
	}

	public static void EarnExp(float exp)
	{
		Dictionary<string, object> dictionaryToSend = GetDictionaryToSend();
		dictionaryToSend.Add("EventType", DesignEvent.Exp.ToString());
		dictionaryToSend.Add("CurrentExp", exp);
		SendEvent(DesignEvent.PlayerStats.ToString(), m_dictionaryToSend);
		s_earnedExpPerSession += exp;
	}

	public static void LogMoneyAdded(int added, ECurrencyType type, TransactionManager.MoneyAddReason reason)
	{
		Dictionary<string, object> dictionaryToSend = GetDictionaryToSend();
		dictionaryToSend.Add("Currency", type.ToString());
		dictionaryToSend.Add("Count", added);
		dictionaryToSend.Add("Reason", reason.ToString());
		SendEvent(DesignEvent.MoneyAdded.ToString(), m_dictionaryToSend);
	}

	public static void SendLiverySharingEnter(int carId)
	{
		GetDictionaryToSend().Add("CarId", carId);
		SendEvent(DesignEvent.LiverySharingEnter.ToString(), m_dictionaryToSend);
	}

	public static void SendLiveryPublish(int carId, string liveryId)
	{
		Dictionary<string, object> dictionaryToSend = GetDictionaryToSend();
		dictionaryToSend.Add("CarId", carId);
		dictionaryToSend.Add("LiveryId", liveryId);
		SendEvent(DesignEvent.LiveryPublish.ToString(), m_dictionaryToSend);
	}

	public static void SendLiverySave(int carId, string liveryId)
	{
		Dictionary<string, object> dictionaryToSend = GetDictionaryToSend();
		dictionaryToSend.Add("CarId", carId);
		dictionaryToSend.Add("LiveryId", liveryId);
		SendEvent(DesignEvent.LiverySave.ToString(), m_dictionaryToSend);
	}

	public static void SendLiveryRate(int carId, string sharingCode)
	{
		Dictionary<string, object> dictionaryToSend = GetDictionaryToSend();
		dictionaryToSend.Add("CarId", carId);
		dictionaryToSend.Add("Code", sharingCode);
		SendEvent(DesignEvent.LiveryRate.ToString(), m_dictionaryToSend);
	}

	public static void SendLiveryShowAuthorList(int carId, string sharingCode)
	{
		Dictionary<string, object> dictionaryToSend = GetDictionaryToSend();
		dictionaryToSend.Add("CarId", carId);
		dictionaryToSend.Add("Code", sharingCode);
		SendEvent(DesignEvent.LiveryShowAuthorList.ToString(), m_dictionaryToSend);
	}

	public static void SendLiveryPublicationCodeSearch(int carId, string sharingCode)
	{
		Dictionary<string, object> dictionaryToSend = GetDictionaryToSend();
		dictionaryToSend.Add("CarId", carId);
		dictionaryToSend.Add("Code", sharingCode);
		SendEvent(DesignEvent.LiveryPublicationCodeSearch.ToString(), m_dictionaryToSend);
	}

	public static void SendLiveryFilterUse(int carId)
	{
		GetDictionaryToSend().Add("CarId", carId);
		SendEvent(DesignEvent.LiveryFilterUse.ToString(), m_dictionaryToSend);
	}

	public static void SendLiveryBuyDLCPressed(DLCCode dlcCode)
	{
		GetDictionaryToSend().Add("DLCCode", dlcCode);
		SendEvent(DesignEvent.LiveryBuyDLCPressed.ToString(), m_dictionaryToSend);
	}

	public static void SendSfxPingUdp(int ping)
	{
		FillRegionPingInfo(ping);
		SendEvent("SfsPingUdp", m_dictionaryToSend);
	}

	public static void SendSfxPingTcp(int ping)
	{
		FillRegionPingInfo(ping);
		SendEvent("SfsPingTcp", m_dictionaryToSend);
	}

	public static void SendLoadPrefsError(Exception error, string prefsFilePath = null, byte[] prefsFileData = null)
	{
		Dictionary<string, object> dictionaryToSend = GetDictionaryToSend();
		dictionaryToSend.Add("error", error);
		dictionaryToSend.Add("prefs_file_path", prefsFilePath);
		dictionaryToSend.Add("prefs_file_data", prefsFileData);
		SendEvent("LoadPrefsError", m_dictionaryToSend);
	}

	public static void SendModMapDownloaded(string mapId)
	{
		GetDictionaryToSend().Add("MapId", mapId);
		SendEvent(DesignEvent.ModMapDownloaded.ToString(), m_dictionaryToSend);
	}

	private static void FillRegionPingInfo(int ping)
	{
		Dictionary<string, object> dictionaryToSend = GetDictionaryToSend();
		dictionaryToSend.Add("ping", ping);
		dictionaryToSend.Add("room_region", m_prefs.globals.currentRegion.ToLower());
	}

	private static void SendEvent(string eventName)
	{
		if (SkipSendEvent(eventName))
		{
			return;
		}
		if (s_isInitialized)
		{
			m_analyticsServices?.ForEach(delegate(ICommonGameAnalytics item)
			{
				item.SendEvent(eventName);
			});
		}
		else
		{
			s_eventCache.Enqueue(eventName, null);
		}
	}

	private static void SendEvent(string eventName, Dictionary<string, object> parameters)
	{
		if (SkipSendEvent(eventName))
		{
			return;
		}
		if (s_isInitialized)
		{
			m_analyticsServices?.ForEach(delegate(ICommonGameAnalytics item)
			{
				item.SendEvent(eventName, parameters);
			});
		}
		else
		{
			s_eventCache.Enqueue(eventName, parameters);
		}
	}

	private static bool SkipSendEvent(string eventName)
	{
		if (!Array.Exists(m_designEventsToSkip, (DesignEvent el) => el.ToString() == eventName))
		{
			return Array.Exists(m_progressionEventsToSkip, (ProgressionStatus el) => el.ToString() == eventName);
		}
		return true;
	}

	private static void ProcessCache()
	{
		if (!s_isInitialized)
		{
			return;
		}
		string eventName;
		Dictionary<string, object> parameters;
		while (s_eventCache.Dequeue(out eventName, out parameters))
		{
			if (parameters == null)
			{
				SendEvent(eventName);
			}
			else
			{
				SendEvent(eventName, parameters);
			}
		}
	}
}
