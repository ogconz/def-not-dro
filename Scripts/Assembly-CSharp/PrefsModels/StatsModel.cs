using System;
using System.Collections.Generic;
using System.Globalization;
using CodeStage.AntiCheat.ObscuredTypes;
using DB.Meta;
using SimpleJSON;
using UnityEngine;

namespace PrefsModels
{
	public class StatsModel : BasePrefsModel
	{
		public enum XDSStatRace
		{
			First = 0,
			Second = 1,
			Total = 2
		}

		[Serializable]
		public class XDSStats
		{
			public List<float> firstRaceTop = new List<float>();

			public List<float> secondRaceTop = new List<float>();

			public List<float> raceTop = new List<float>();
		}

		private const string DATETIME_FORMAT = "yyyy-MM-dd HH:mm:ss";

		private ObscuredInt m_runApplicationCount;

		[JsonSerialize]
		private Dictionary<string, XDSStats> m_xdsRaceTop = new Dictionary<string, XDSStats>();

		[JsonSerialize]
		private Dictionary<string, int> m_serversStats = new Dictionary<string, int>();

		[JsonSerialize]
		private Dictionary<string, float> m_fourStats = new Dictionary<string, float>();

		[JsonSerialize]
		private string m_firstPlayDate;

		private long m_timeInGameStartPoint;

		private long m_timeInGamePrev;

		[JsonSerialize]
		private int overallRents;

		[JsonSerialize]
		private Dictionary<int, int> perCarRents = new Dictionary<int, int>();

		[JsonSerialize]
		private Dictionary<int, float> m_carMileage = new Dictionary<int, float>();

		[JsonSerialize]
		private Dictionary<int, int> m_raceModePerTrack = new Dictionary<int, int>();

		[JsonSerialize]
		private Dictionary<string, bool> m_mpTopLikedRacesIdsCache = new Dictionary<string, bool>();

		[JsonSerialize]
		private List<RaceResult> m_timeTrialResults = new List<RaceResult>();

		private ObscuredInt m_racesPlayed;

		private ObscuredInt m_driftRacesPlayed;

		private ObscuredInt m_timeAttackRacesPlayed;

		private ObscuredInt m_MPRacesPlayed;

		private ObscuredInt m_purchasesCount;

		private ObscuredInt m_racesPlayedCountOnSessionStart;

		private CommitableVariable<ObscuredFloat> m_maxPointsPerDrift = new CommitableVariable<ObscuredFloat>();

		private CommitableVariable<ObscuredFloat> m_maxPointsPerRace = new CommitableVariable<ObscuredFloat>();

		private ObscuredFloat m_averagePointsPerRace;

		private ObscuredInt m_cups1;

		private ObscuredInt m_cups2;

		private ObscuredInt m_cups3;

		private ObscuredFloat m_curPointPerRace;

		public int runApplicationCount
		{
			get
			{
				return m_runApplicationCount;
			}
			set
			{
				m_runApplicationCount = value;
			}
		}

		public Dictionary<string, XDSStats> xdsRaceTop => m_xdsRaceTop;

		public Dictionary<string, int> serversStats => m_serversStats;

		public Dictionary<string, float> fourStats => m_fourStats;

		public DateTime firstPlayDate { get; private set; }

		public ObscuredBool isFirstPlay { get; private set; }

		public long timeInGame
		{
			get
			{
				return m_timeInGamePrev + (TimeTracker.timeInFrame - m_timeInGameStartPoint);
			}
			set
			{
				m_timeInGamePrev = value;
				m_timeInGameStartPoint = TimeTracker.timeInFrame;
			}
		}

		[JsonIgnore]
		public Dictionary<int, float> carMileage => m_carMileage;

		public int racesPlayed
		{
			get
			{
				return m_racesPlayed;
			}
			set
			{
				m_racesPlayed = value;
				Save();
			}
		}

		public int driftRacesPlayed
		{
			get
			{
				return m_driftRacesPlayed;
			}
			set
			{
				m_driftRacesPlayed = value;
				Save();
			}
		}

		public int timeAttackRacesPlayed
		{
			get
			{
				return m_timeAttackRacesPlayed;
			}
			set
			{
				m_timeAttackRacesPlayed = value;
				Save();
			}
		}

		public int MPRacesPlayed
		{
			get
			{
				return m_MPRacesPlayed;
			}
			set
			{
				m_MPRacesPlayed = value;
				Save();
			}
		}

		public int purchasesCount
		{
			get
			{
				return m_purchasesCount;
			}
			set
			{
				m_purchasesCount = value;
				Save();
			}
		}

		public int racesPlayedCountOnSessionStart => m_racesPlayedCountOnSessionStart;

		public float maxPointsPerDrift
		{
			get
			{
				return m_maxPointsPerDrift.value.Get();
			}
			set
			{
				m_maxPointsPerDrift.curVal.Set(value);
				m_maxPointsPerDrift.Commit();
			}
		}

		public float maxPointsPerRace
		{
			get
			{
				return m_maxPointsPerRace.value.Get();
			}
			set
			{
				m_maxPointsPerRace.curVal.Set(value);
				m_maxPointsPerRace.Commit();
			}
		}

		public float averagePointsPerRace
		{
			get
			{
				return m_averagePointsPerRace.Get();
			}
			set
			{
				m_averagePointsPerRace.Set(value);
				Save();
			}
		}

		public int cups1
		{
			get
			{
				return m_cups1;
			}
			set
			{
				m_cups1 = value;
				Save();
			}
		}

		public int cups2
		{
			get
			{
				return m_cups2;
			}
			set
			{
				m_cups2 = value;
				Save();
			}
		}

		public int cups3
		{
			get
			{
				return m_cups3;
			}
			set
			{
				m_cups3 = value;
				Save();
			}
		}

		public float GetDaysCountSinceFirstLaunch()
		{
			return (float)(DateTime.Now - firstPlayDate).TotalDays;
		}

		public long GetSecondsCountSinceFirstLaunch()
		{
			return (long)(DateTime.Now - firstPlayDate).TotalSeconds;
		}

		public float GetXDSBestResult(string config)
		{
			if (!m_xdsRaceTop.TryGetValue(config, out var value))
			{
				return 0f;
			}
			if (value.raceTop.Count == 0)
			{
				return 0f;
			}
			return value.raceTop[0];
		}

		public void StoreXDSResult(string config, float value, XDSStatRace raceType)
		{
			if (!m_xdsRaceTop.TryGetValue(config, out var value2))
			{
				value2 = new XDSStats();
				m_xdsRaceTop.Add(config, value2);
			}
			List<float> list = null;
			switch (raceType)
			{
			case XDSStatRace.First:
				list = value2.firstRaceTop;
				break;
			case XDSStatRace.Second:
				list = value2.secondRaceTop;
				break;
			case XDSStatRace.Total:
				list = value2.raceTop;
				break;
			}
			foreach (float item in list)
			{
				if (Mathf.Abs(item - value) < 0.01f)
				{
					return;
				}
			}
			list.Add(value);
			list.Sort((float lhs, float rhs) => -lhs.CompareTo(rhs));
			if (list.Count > 5)
			{
				list.RemoveRange(5, list.Count - 5);
			}
		}

		public void IncreaseRent1CarCount(int profileId)
		{
			if (perCarRents.ContainsKey(profileId))
			{
				perCarRents[profileId]++;
			}
			else
			{
				perCarRents.Add(profileId, 1);
			}
			Save();
		}

		public int GetRent1CarCount(int profileId)
		{
			if (!perCarRents.ContainsKey(profileId))
			{
				return 0;
			}
			return perCarRents[profileId];
		}

		public void IncreaseRentOverallCount()
		{
			overallRents++;
			Save();
		}

		public int GetRentOverallCount()
		{
			return overallRents;
		}

		public int GetRacesPlayedCountSinceSessionStart()
		{
			return (int)m_racesPlayed - (int)m_racesPlayedCountOnSessionStart;
		}

		public RaceMode GetLastRaceModeForTrack(int trackId)
		{
			if (!m_raceModePerTrack.ContainsKey(trackId) || !Enum.IsDefined(typeof(RaceMode), m_raceModePerTrack[trackId]))
			{
				return RaceMode.Drift;
			}
			return (RaceMode)m_raceModePerTrack[trackId];
		}

		public void SetLastRaceModeForTrack(int trackId, RaceMode mode)
		{
			if (!m_raceModePerTrack.TryGetValue(trackId, out var value) || value != (int)mode)
			{
				m_raceModePerTrack[trackId] = (int)mode;
				Save();
			}
		}

		public List<RaceResult> GetTimeTrialResults()
		{
			return m_timeTrialResults.Map((RaceResult t) => t.MemberwiseClone() as RaceResult);
		}

		public RaceResult GetRaceResult(int trackId, int carId, int profileId, RaceMode? raceMode = null)
		{
			if (!raceMode.HasValue)
			{
				raceMode = GetLastRaceModeForTrack(trackId);
			}
			return GetTimeTrialResult(trackId, carId, profileId, raceMode);
		}

		public RaceResult GetTimeTrialResult(int trackId, int carId, int profileId, RaceMode? raceMode = null)
		{
			int raceModeId = (int)raceMode.Value;
			RaceResult raceResult = m_timeTrialResults.Find((RaceResult c) => c.trackId == trackId && c.carId == carId && c.profileId == profileId && c.raceModeId == raceModeId);
			if (raceResult != null)
			{
				return raceResult.MemberwiseClone() as RaceResult;
			}
			return null;
		}

		public bool UpdateRaceTrialResult(RaceResult newResult)
		{
			RaceResult raceResult = m_timeTrialResults.Find((RaceResult c) => c.carId == newResult.carId && c.trackId == newResult.trackId && c.profileId == newResult.profileId && c.raceModeId == newResult.raceModeId);
			bool result = false;
			if (raceResult == null)
			{
				RaceResult item = newResult.MemberwiseClone() as RaceResult;
				m_timeTrialResults.Add(item);
				result = true;
			}
			else if (newResult.IsBetter(raceResult))
			{
				raceResult.recordDriftPoints = newResult.recordDriftPoints;
				raceResult.bestLapTime = newResult.bestLapTime;
				result = true;
			}
			return result;
		}

		public bool GetMpTopRaceLiked(string raceId)
		{
			return m_mpTopLikedRacesIdsCache.ContainsKey(raceId);
		}

		public void SetMpTopRaceLiked(string raceId)
		{
			if (!m_mpTopLikedRacesIdsCache.ContainsKey(raceId))
			{
				m_mpTopLikedRacesIdsCache.Add(raceId, value: false);
				Save();
			}
		}

		public StatsModel()
		{
			m_racesPlayed.ApplyNewCryptoKey();
			m_driftRacesPlayed.ApplyNewCryptoKey();
			m_timeAttackRacesPlayed.ApplyNewCryptoKey();
			m_MPRacesPlayed.ApplyNewCryptoKey();
			m_racesPlayedCountOnSessionStart.ApplyNewCryptoKey();
			m_curPointPerRace.ApplyNewCryptoKey();
			m_averagePointsPerRace.ApplyNewCryptoKey();
			m_maxPointsPerDrift.value.ApplyNewCryptoKey();
			m_maxPointsPerRace.value.ApplyNewCryptoKey();
			m_runApplicationCount.ApplyNewCryptoKey();
		}

		public override void PostLoad()
		{
			m_maxPointsPerDrift.Commit();
			m_maxPointsPerRace.Commit();
			isFirstPlay = string.IsNullOrEmpty(m_firstPlayDate);
			if ((bool)isFirstPlay)
			{
				timeInGame = 0L;
				firstPlayDate = DateTime.Now;
				m_firstPlayDate = firstPlayDate.ToString("yyyy-MM-dd HH:mm:ss");
			}
			else
			{
				DateTime.TryParseExact(m_firstPlayDate, "yyyy-MM-dd HH:mm:ss", null, DateTimeStyles.None, out var result);
				firstPlayDate = result;
			}
			m_racesPlayedCountOnSessionStart = m_racesPlayed;
			int num = runApplicationCount + 1;
			runApplicationCount = num;
			Save();
			GameEvents.OnRaceLoaded -= new GameEventHandler<GameEvents.EventInfo>(OnRaceStart);
			GameEvents.OnRaceLoaded += new GameEventHandler<GameEvents.EventInfo>(OnRaceStart);
			GameEvents.OnRaceEnd -= new GameEventHandler<GameEvents.RaceEndInfo>(OnRaceEnd);
			GameEvents.OnRaceEnd += new GameEventHandler<GameEvents.RaceEndInfo>(OnRaceEnd);
			GameEvents.OnRaceCupEarned -= new GameEventHandler<GameEvents.CupInfo>(OnRaceCupEarned);
			GameEvents.OnRaceCupEarned += new GameEventHandler<GameEvents.CupInfo>(OnRaceCupEarned);
			GameEvents.OnDriftPointsEarned -= new GameEventHandler<GameEvents.DriftPointsInfo>(OnDriftPointsEarned);
			GameEvents.OnDriftPointsEarned += new GameEventHandler<GameEvents.DriftPointsInfo>(OnDriftPointsEarned);
		}

		public override void Reset()
		{
			base.Reset();
			timeInGame = 0L;
		}

		private void IncreaseRacePlayedCounters()
		{
			BaseRaceModeState baseRaceModeState = States.mainCurrent as BaseRaceModeState;
			if (!(baseRaceModeState == null))
			{
				int num = racesPlayed + 1;
				racesPlayed = num;
				switch (baseRaceModeState.raceMode)
				{
				case RaceMode.Drift:
					num = driftRacesPlayed + 1;
					driftRacesPlayed = num;
					break;
				case RaceMode.TimeAttack:
					num = timeAttackRacesPlayed + 1;
					timeAttackRacesPlayed = num;
					break;
				case RaceMode.NetworkFreeride:
					num = MPRacesPlayed + 1;
					MPRacesPlayed = num;
					ActivityFeedService.ProcessStory(ActivityFeedService.StoryType.DoneNetworkRace, MPRacesPlayed);
					break;
				case RaceMode.Freeride:
				case RaceMode.Dyno:
					break;
				}
			}
		}

		private void OnRaceStart(GameEvents.EventInfo e)
		{
			m_curPointPerRace.Set(0f);
			GameLog.Print(AlertLevel.Notify, "PlayerProfile", "(stat) race start, points is 0");
		}

		private void OnRaceEnd(GameEvents.RaceEndInfo e)
		{
			if (!e.isCompleted)
			{
				m_maxPointsPerDrift.Discard();
				m_maxPointsPerRace.Discard();
				GameLog.Print(AlertLevel.Notify, "PlayerProfile", "(stat) race canceled, discard race points");
				return;
			}
			isSaveAllowed = false;
			try
			{
				IncreaseRacePlayedCounters();
				averagePointsPerRace = (averagePointsPerRace * (float)(racesPlayed - 1) + m_curPointPerRace.Get()) / (float)racesPlayed;
				m_maxPointsPerDrift.Commit();
				m_maxPointsPerRace.Commit();
				ObscuredFloat curVal = m_maxPointsPerDrift.curVal;
				string text = curVal.ToString();
				curVal = m_maxPointsPerDrift.curVal;
				GameLog.Print(AlertLevel.Notify, "PlayerProfile", "(stat) race finished, commit race points " + text + "/" + curVal.ToString());
			}
			catch
			{
			}
			finally
			{
				isSaveAllowed = true;
			}
			Save();
		}

		private void OnRaceCupEarned(GameEvents.CupInfo e)
		{
			switch (e.cupIndex)
			{
			case 1:
			{
				int num = cups1 + 1;
				cups1 = num;
				break;
			}
			case 2:
			{
				int num = cups2 + 1;
				cups2 = num;
				break;
			}
			case 3:
			{
				int num = cups3 + 1;
				cups3 = num;
				break;
			}
			}
		}

		private void OnDriftPointsEarned(GameEvents.DriftPointsInfo e)
		{
			m_curPointPerRace.Set(e.totalPoints);
			if (e.curPoints > m_maxPointsPerDrift.curVal.Get())
			{
				m_maxPointsPerDrift.curVal.Set(e.curPoints);
			}
			if (e.totalPoints > m_maxPointsPerRace.curVal.Get())
			{
				m_maxPointsPerRace.curVal.Set(e.totalPoints);
			}
		}
	}
}
