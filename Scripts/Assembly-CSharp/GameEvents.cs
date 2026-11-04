using ArrayExtension;
using PrefsModels;

public static class GameEvents
{
	public class EventInfo
	{
		public uint eventID;

		public uint subscriberID;

		public EventInfo()
		{
			eventID = 0u;
		}

		public EventInfo(uint eventID)
		{
			this.eventID = eventID;
		}
	}

	public class UGCReadyInfo : EventInfo
	{
		public ulong packageId;

		public UGCReadyInfo(ulong packageId)
		{
			this.packageId = packageId;
		}
	}

	public class LastCarIdInfo : EventInfo
	{
		public int lastCarId;

		public LastCarIdInfo(int lastCarId)
		{
			this.lastCarId = lastCarId;
		}
	}

	public class DriftPointsInfo : EventInfo
	{
		public float curPoints;

		public float totalPoints;

		public DriftPointsInfo(float curPoints, float totalPoints)
		{
			this.curPoints = curPoints;
			this.totalPoints = totalPoints;
		}
	}

	public class CarTCInfo : EventInfo
	{
		public RaceCar car;

		public bool currentState;

		public CarTCInfo(RaceCar car, bool currentState)
		{
			this.currentState = currentState;
			this.car = car;
		}
	}

	public class DriftExpInfo : EventInfo
	{
		public float expPoints;

		public DriftExpInfo(float expPoints)
		{
			this.expPoints = expPoints;
		}
	}

	public class OverlayStateInfo : EventInfo
	{
		public bool isActivated;

		public OverlayStateInfo(bool isActivated)
		{
			this.isActivated = isActivated;
		}
	}

	public class CupInfo : EventInfo
	{
		public int cupIndex;

		public CupInfo(int cupIndex)
		{
			this.cupIndex = cupIndex;
		}
	}

	public class RaceEndInfo : EventInfo
	{
		public bool isCompleted;

		public BaseRaceModeState.DetailedRaceAward award;

		public RaceEndInfo(bool isCompleted, BaseRaceModeState.DetailedRaceAward award)
		{
			this.isCompleted = isCompleted;
			this.award = award;
		}
	}

	public class RaceCountDown : EventInfo
	{
		public int countDown;

		public RaceCountDown(int countDown)
		{
			this.countDown = countDown;
		}
	}

	public static uint EVENT_ID_COUNTER = 0u;

	public static IGameEvent[] eventsList = new IGameEvent[42];

	public static GameEvent<EventInfo> OnGarageEnter = new GameEvent<EventInfo>(eventRegistration: true);

	public static GameEvent<OverlayStateInfo> OnOverlayStateChanged = new GameEvent<OverlayStateInfo>(eventRegistration: true);

	public static GameEvent<EventInfo> OnGameSettingsInputUpdate = new GameEvent<EventInfo>(eventRegistration: true);

	public static GameEvent<EventInfo> OnGameEngineStop = new GameEvent<EventInfo>(eventRegistration: true);

	public static GameEvent<EventInfo> OnGameEngineStart = new GameEvent<EventInfo>(eventRegistration: true);

	public static GameEvent<CupInfo> OnRaceCupEarned = new GameEvent<CupInfo>(eventRegistration: true);

	public static GameEvent<EventInfo> OnRaceLoaded = new GameEvent<EventInfo>(eventRegistration: true);

	public static GameEvent<RaceEndInfo> OnRaceEnd = new GameEvent<RaceEndInfo>(eventRegistration: true);

	public static GameEvent<RaceCountDown> OnRaceCountDown = new GameEvent<RaceCountDown>(eventRegistration: true);

	public static GameEvent<LastCarIdInfo> OnLastCarIdChanged = new GameEvent<LastCarIdInfo>(eventRegistration: true);

	public static GameEvent<CarTCInfo> OnCarTCChanged = new GameEvent<CarTCInfo>(eventRegistration: true);

	public static GameEvent<DriftPointsInfo> OnDriftPointsEarned = new GameEvent<DriftPointsInfo>(eventRegistration: true);

	public static GameEvent<DriftExpInfo> OnDriftExpEarned = new GameEvent<DriftExpInfo>(eventRegistration: true);

	public static GameEvent<UGCReadyInfo> OnUGCCarReady = new GameEvent<UGCReadyInfo>(eventRegistration: true);

	public static GameEvent<UGCReadyInfo> OnUGCMapReady = new GameEvent<UGCReadyInfo>(eventRegistration: true);

	public static EventInfo ZeroInfo = new EventInfo(0u);

	public static void RegisterEvent(IGameEvent gameEvent)
	{
		uint iD = gameEvent.GetID();
		if (iD >= eventsList.Length)
		{
			eventsList = eventsList.Resize(2 * eventsList.Length);
			GameLog.Print(AlertLevel.Warning, "Debug", "GameEvents rescaled, change basic size");
		}
		eventsList[iD] = gameEvent;
	}

	public static void CleanUp(GameEventFlag LifeTime)
	{
		eventsList.ForEachNotNull(delegate(IGameEvent e)
		{
			e.CleanUp(LifeTime);
		});
	}

	public static void OverlayStateChanged(bool isActivated)
	{
		OnOverlayStateChanged.Invoke(new OverlayStateInfo(isActivated));
	}

	public static void GameEngineStop()
	{
		OnGameEngineStop.Invoke(ZeroInfo);
	}

	public static void GameEngineStart()
	{
		OnGameEngineStart.ActivateInvokeOnSubscribeMode(ZeroInfo);
		OnGameEngineStart.Invoke(ZeroInfo);
	}

	public static void GameSettingsInputUpdate()
	{
		OnGameSettingsInputUpdate.Invoke(ZeroInfo);
	}

	public static void GarageEnter()
	{
		OnGarageEnter.Invoke(ZeroInfo);
	}

	public static void ChangeLastCarId(int carId)
	{
		OnLastCarIdChanged.Invoke(new LastCarIdInfo(carId));
	}

	public static void RaceCupEarned(CupInfo info)
	{
		OnRaceCupEarned.Invoke(info);
	}

	public static void CarTCChanged(CarTCInfo info)
	{
		OnCarTCChanged.Invoke(info);
	}

	public static void DriftExpEarned(DriftExpInfo info)
	{
		OnDriftExpEarned.Invoke(info);
	}

	public static void DriftPointsEarned(DriftPointsInfo info)
	{
		OnDriftPointsEarned.Invoke(info);
	}

	public static void RaceLoaded()
	{
		BasePrefsModel.LockSaves(flag: true);
		OnRaceLoaded.Invoke(ZeroInfo);
	}

	public static void RaceEnd(RaceEndInfo info)
	{
		BasePrefsModel.LockSaves(flag: false);
		OnRaceEnd.Invoke(info);
		CleanUp(GameEventFlag.Race);
	}

	public static void RaceStartCountDown(int countDown)
	{
		OnRaceCountDown.Invoke(new RaceCountDown(countDown));
	}

	public static void UGCCarReady(ulong packageID)
	{
		OnUGCCarReady.Invoke(new UGCReadyInfo(packageID));
	}

	public static void UGCMapReady(ulong packageID)
	{
		OnUGCMapReady.Invoke(new UGCReadyInfo(packageID));
	}
}
