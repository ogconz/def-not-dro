using System;
using System.Collections.Generic;
using CarX.Gameplay.ClippingZone;
using SyncMultiplayer;
using UnityEngine;

public class CheckpointManager
{
	private class RaceInfo : IComparable<RaceInfo>
	{
		private readonly CheckpointManager m_checkpointManager;

		public RaceCar car;

		public float? startTime;

		public List<List<float>> laps = new List<List<float>>();

		public List<List<float>> penalty = new List<List<float>>();

		public float penaltyTime;

		public int personalBestLap = -1;

		public RaceInfo(CheckpointManager cm, RaceCar car)
		{
			m_checkpointManager = cm;
			this.car = car;
		}

		public int CompareTo(RaceInfo other)
		{
			if (other == null)
			{
				return 1;
			}
			int lastCompletedLap = m_checkpointManager.GetLastCompletedLap(car);
			int lastCompletedLap2 = m_checkpointManager.GetLastCompletedLap(other.car);
			if (lastCompletedLap < lastCompletedLap2)
			{
				return 1;
			}
			if (lastCompletedLap > lastCompletedLap2)
			{
				return -1;
			}
			int lastCompletedCheckpoint = m_checkpointManager.GetLastCompletedCheckpoint(car);
			int lastCompletedCheckpoint2 = m_checkpointManager.GetLastCompletedCheckpoint(other.car);
			if (lastCompletedCheckpoint < lastCompletedCheckpoint2)
			{
				return 1;
			}
			if (lastCompletedCheckpoint > lastCompletedCheckpoint2)
			{
				return -1;
			}
			float distanceOnTrack = car.distanceOnTrack;
			float distanceOnTrack2 = other.car.distanceOnTrack;
			if (distanceOnTrack < distanceOnTrack2)
			{
				return 1;
			}
			if (distanceOnTrack > distanceOnTrack2)
			{
				return -1;
			}
			return 0;
		}
	}

	public delegate void OnRaceStartEvent(RaceCar car);

	public delegate void OnRaceEndEvent();

	public delegate void OnRaceCompletedEvent(RaceCar car, float bestLap, float totalTime);

	public delegate void OnCheckpointReachedEvent(RaceCar car, int lap, int checkpoint);

	public delegate void OnCheckpointMissedEvent(RaceCar car, int lap, int previousCheckpoint, int expectedCheckpoint, int reachedCheckpoint);

	public delegate void OnLapCompletedEvent(RaceCar car, int lap, float lapTime, bool isPersonalBestLap, bool isAbsoluteBestLap);

	private const float DEFAULT_STARTLINE_TRIGGER_DEPTH = 0.5f;

	[SerializeField]
	private int m_laps;

	private StartLine m_startLine;

	private float m_startLineTriggerDepth = 0.5f;

	private Checkpoint[] m_checkpoints = new Checkpoint[0];

	private BaseClipping[] m_clippingObjects = new BaseClipping[0];

	private RaceCar m_absoluteBestLapVehicle;

	private readonly List<RaceCar> m_cars = new List<RaceCar>();

	private readonly Dictionary<RaceCar, RaceInfo> m_info = new Dictionary<RaceCar, RaceInfo>();

	private float? m_trackConfigurationLength;

	private int m_lastCheckpointIndexWrong = -1;

	public int laps
	{
		get
		{
			return m_laps;
		}
		set
		{
			m_laps = value;
		}
	}

	public StartLine startLine => m_startLine;

	public IList<Checkpoint> checkpoints => m_checkpoints;

	public IList<BaseClipping> clippingObjects => m_clippingObjects;

	public event OnRaceStartEvent OnRaceStart;

	public event OnRaceEndEvent OnRaceEnd;

	public event OnRaceCompletedEvent OnRaceCompleted;

	public event OnCheckpointReachedEvent OnCheckpointReached;

	public event OnCheckpointMissedEvent OnCheckpointMissed;

	public event OnLapCompletedEvent OnLapCompleted;

	public void ClearCars()
	{
		m_cars.Clear();
		m_info.Clear();
	}

	public void AddCar(RaceCar car)
	{
		m_cars.Add(car);
		m_info[car] = new RaceInfo(this, car);
	}

	public void RemoveCar(RaceCar car)
	{
		m_cars.Remove(car);
		m_info.Remove(car);
	}

	public Checkpoint GetLastCheckpoint()
	{
		if (m_checkpoints == null)
		{
			return null;
		}
		return m_checkpoints[m_checkpoints.Length - 1];
	}

	public Checkpoint GetCheckpoint(int index)
	{
		if (index < 0 || index >= m_checkpoints.Length)
		{
			return null;
		}
		return m_checkpoints[index];
	}

	public float? GetAbsoluteBestLapTime(bool withPenalty = true)
	{
		if (m_absoluteBestLapVehicle != null)
		{
			return GetLapTime(m_absoluteBestLapVehicle, GetPersonalBestLap(m_absoluteBestLapVehicle), withPenalty);
		}
		return null;
	}

	public RaceCar GetAbsoluteBestLapVehicle()
	{
		return m_absoluteBestLapVehicle;
	}

	public int GetLastCompletedLap(RaceCar car)
	{
		if (m_info.TryGetValue(car, out var value) && value.startTime.HasValue)
		{
			int num = value.laps.Count - 1;
			if (num >= 0 && value.laps[num].Count < m_checkpoints.Length)
			{
				num--;
			}
			return num;
		}
		return -1;
	}

	public void ClearVisitedCheckpoints()
	{
		if (m_checkpoints != null)
		{
			Checkpoint[] array = m_checkpoints;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].isVisited = false;
			}
		}
	}

	public int GetLastCompletedCheckpoint(RaceCar car)
	{
		if (m_info.TryGetValue(car, out var value) && value.startTime.HasValue)
		{
			int num = value.laps.Count - 1;
			if (num >= 0)
			{
				return value.laps[num].Count - 1;
			}
		}
		return -1;
	}

	public int GetPersonalBestLap(RaceCar car)
	{
		if (m_info.TryGetValue(car, out var value) && value.startTime.HasValue)
		{
			return value.personalBestLap;
		}
		return -1;
	}

	public float? GetPersonalBestLapTime(RaceCar car, bool withPenalty = true)
	{
		return GetLapTime(car, GetPersonalBestLap(car), withPenalty);
	}

	public int GetCurrentLap(RaceCar car)
	{
		return GetLastCompletedLap(car) + 1;
	}

	public float? GetCurrentLapTime(RaceCar car)
	{
		return GetLapTime(car, GetCurrentLap(car), withPenalty: false);
	}

	public float? GetLapStartTime(RaceCar car, int lap)
	{
		if (m_info.TryGetValue(car, out var value) && value.startTime.HasValue && lap >= 0 && lap <= GetCurrentLap(car))
		{
			int index = m_checkpoints.Length - 1;
			if (lap == 0)
			{
				return value.startTime;
			}
			return value.laps[lap - 1][index];
		}
		return null;
	}

	public float? GetStartTime(RaceCar car)
	{
		if (m_info.TryGetValue(car, out var value))
		{
			return value.startTime;
		}
		return null;
	}

	public float? GetTotalTime(RaceCar car, bool withPenalty = true)
	{
		return GetTotalTime(car, laps - 1, withPenalty);
	}

	public float? GetTotalTime(RaceCar car, int lap, bool withPenalty = true)
	{
		return GetTotalTime(car, lap, m_checkpoints.Length - 1, withPenalty);
	}

	public float? GetTotalTime(RaceCar car, int lap, int checkpoint, bool withPenalty = true)
	{
		if (m_info.TryGetValue(car, out var value) && value.startTime.HasValue)
		{
			float num = ((lap < 0 || (lap >= value.laps.Count - 1 && (lap != value.laps.Count - 1 || checkpoint < 0 || checkpoint >= value.laps[lap].Count))) ? Time.time : value.laps[lap][checkpoint]);
			float value2 = value.startTime.Value;
			float num2 = num - value2;
			if (withPenalty)
			{
				float? totalPenaltyTime = GetTotalPenaltyTime(car, lap, checkpoint);
				if (totalPenaltyTime.HasValue)
				{
					num2 += totalPenaltyTime.Value;
				}
			}
			return num2;
		}
		return null;
	}

	public float? GetLapTime(RaceCar car, int lap, bool withPenalty = true)
	{
		if (m_info.TryGetValue(car, out var value) && value.startTime.HasValue)
		{
			int currentLap = GetCurrentLap(car);
			if (lap >= 0 && lap <= currentLap)
			{
				float num = ((lap < currentLap) ? value.laps[lap][m_checkpoints.Length - 1] : Time.time);
				float num2 = ((lap != 0) ? value.laps[lap - 1][m_checkpoints.Length - 1] : value.startTime.Value);
				float num3 = num - num2;
				if (withPenalty)
				{
					float? penaltyTime = GetPenaltyTime(car, lap);
					if (penaltyTime.HasValue)
					{
						num3 += penaltyTime.Value;
					}
				}
				return num3;
			}
		}
		return null;
	}

	public float? GetPenaltyTime(RaceCar car, int lap)
	{
		if (m_info.TryGetValue(car, out var value) && value.startTime.HasValue)
		{
			int currentLap = GetCurrentLap(car);
			if (lap >= 0 && lap <= currentLap)
			{
				float num = ((lap < currentLap) ? value.penalty[lap][m_checkpoints.Length - 1] : value.penaltyTime);
				float num2 = ((lap != 0) ? value.penalty[lap - 1][m_checkpoints.Length - 1] : 0f);
				return num - num2;
			}
		}
		return null;
	}

	public float? GetCurrentPenaltyTime(RaceCar car)
	{
		return GetPenaltyTime(car, GetCurrentLap(car));
	}

	public float? GetTotalPenaltyTime(RaceCar car, int lap, int checkpoint)
	{
		if (m_info.TryGetValue(car, out var value) && value.startTime.HasValue)
		{
			return (lap < 0 || (lap >= value.laps.Count - 1 && (lap != value.laps.Count - 1 || checkpoint < 0 || checkpoint >= value.laps[lap].Count))) ? value.penaltyTime : value.penalty[lap][checkpoint];
		}
		return null;
	}

	public RaceCar[] GetClasification()
	{
		List<RaceInfo> list = new List<RaceInfo>(m_info.Values);
		list.Sort();
		RaceCar[] array = new RaceCar[list.Count];
		int num = 0;
		foreach (RaceInfo item in list)
		{
			array[num] = item.car;
			num++;
		}
		return array;
	}

	public int GetCheckpointsPassedPercent(RaceCar car)
	{
		if (!m_info.TryGetValue(car, out var value))
		{
			return 0;
		}
		if (value.laps.Count == 0 || value.laps[0].Count == 0)
		{
			return 0;
		}
		int num = value.laps.Count - 1;
		int count = value.laps[value.laps.Count - 1].Count;
		int num2 = m_checkpoints.Length;
		return (num * num2 + count) * 100 / (num2 * laps);
	}

	public void ClearTimers()
	{
		foreach (RaceInfo value in m_info.Values)
		{
			value.startTime = Time.time;
			value.penaltyTime = 0f;
			value.laps.Clear();
			value.penalty.Clear();
		}
	}

	public bool SetupStartTimeIfNeed(RaceCar car, float time)
	{
		if (car == null || !m_cars.Contains(car))
		{
			return false;
		}
		if (m_info.TryGetValue(car, out var value) && !value.startTime.HasValue)
		{
			value.startTime = time;
			value.penaltyTime = 0f;
			return true;
		}
		return false;
	}

	public void RegisterStartRace(RaceCar car, float time)
	{
		if (SetupStartTimeIfNeed(car, time))
		{
			if (m_checkpoints.Length != 0)
			{
				car.positionOnSpline.SetDirty();
				car.positionOnSpline.SetNextCheckpointIndex(GetLastSegmentIndex(0));
			}
			this.OnRaceStart?.Invoke(car);
			WriteLastActiveCheckPointPosition(car);
		}
	}

	public void RegisterStartRaceMode(RaceCar car, float time)
	{
		RaceInfo value;
		if (!m_info.ContainsKey(car))
		{
			value = new RaceInfo(this, car);
			m_info.Add(car, value);
		}
		if (car != null && m_cars.Contains(car) && m_info.TryGetValue(car, out value) && !value.startTime.HasValue)
		{
			if (m_checkpoints.Length != 0)
			{
				car.positionOnSpline.SetDirty();
				car.positionOnSpline.SetNextCheckpointIndex(GetLastSegmentIndex(0));
			}
			value.startTime = time;
			value.penaltyTime = 0f;
			if (this.OnRaceStart != null)
			{
				this.OnRaceStart(car);
			}
		}
	}

	private int GetLastSegmentIndex(int index)
	{
		if (index >= 0 && index < m_checkpoints.Length)
		{
			return m_checkpoints[index].segmentIndex;
		}
		return -1;
	}

	private void SetRaceEndByFirstFinishedCar(RaceCar car)
	{
		if (this.OnRaceCompleted != null)
		{
			this.OnRaceCompleted(car, GetPersonalBestLapTime(car).Value, GetTotalTime(car).Value);
		}
		if (m_cars.TrueForAll(delegate(RaceCar x)
		{
			int num = m_checkpoints.Length - 1;
			int lastCompletedCheckpoint = GetLastCompletedCheckpoint(x);
			return GetCurrentLap(x) >= laps - 1 && (num <= 0 || lastCompletedCheckpoint >= num);
		}))
		{
			this.OnRaceEnd?.Invoke();
		}
	}

	public void RegisterCheckpoint(RaceCar car, Checkpoint checkpoint)
	{
		if (!(car != null) || !(checkpoint != null) || !m_cars.Contains(car) || !m_info.TryGetValue(car, out var value) || !value.startTime.HasValue)
		{
			return;
		}
		int currentLap = GetCurrentLap(car);
		if (currentLap >= laps)
		{
			return;
		}
		int num = Array.IndexOf(m_checkpoints, checkpoint);
		if (num < 0)
		{
			return;
		}
		int lastCompletedCheckpoint = GetLastCompletedCheckpoint(car);
		int num2 = m_checkpoints.Length - 1;
		if (num2 == 0)
		{
			return;
		}
		if (((currentLap == 0 && lastCompletedCheckpoint < 0) || (currentLap > 0 && currentLap < laps && lastCompletedCheckpoint == num2)) && num == 0)
		{
			WriteLastActiveCheckPointPosition(car);
			m_lastCheckpointIndexWrong = -1;
			ClearVisitedCheckpoints();
			checkpoint.isVisited = true;
			value.laps.Add(new List<float>());
			value.laps[currentLap].Add(Time.time);
			value.penalty.Add(new List<float>());
			value.penalty[currentLap].Add(value.penaltyTime);
			if (this.OnCheckpointReached != null)
			{
				this.OnCheckpointReached(car, currentLap, num);
			}
			car.positionOnSpline.SetNextCheckpointIndex(GetLastSegmentIndex(num + 1));
			return;
		}
		if (num == lastCompletedCheckpoint + 1)
		{
			WriteLastActiveCheckPointPosition(car);
			m_lastCheckpointIndexWrong = -1;
			checkpoint.isVisited = true;
			value.laps[currentLap].Add(Time.time);
			value.penalty[currentLap].Add(value.penaltyTime);
			if (this.OnCheckpointReached != null)
			{
				this.OnCheckpointReached(car, currentLap, num);
			}
			if (num == num2)
			{
				car.positionOnSpline.SetNextCheckpointIndex(-1);
				float? personalBestLapTime = GetPersonalBestLapTime(car);
				float? lapTime = GetLapTime(car, currentLap);
				bool isPersonalBestLap = false;
				bool isAbsoluteBestLap = false;
				if (lapTime.HasValue)
				{
					if (!personalBestLapTime.HasValue || lapTime.Value < personalBestLapTime.Value)
					{
						value.personalBestLap = currentLap;
						isPersonalBestLap = true;
					}
					float? absoluteBestLapTime = GetAbsoluteBestLapTime();
					if (!absoluteBestLapTime.HasValue || lapTime.Value < absoluteBestLapTime.Value)
					{
						m_absoluteBestLapVehicle = car;
						isAbsoluteBestLap = true;
					}
				}
				if (this.OnLapCompleted != null && !car.isGhost)
				{
					this.OnLapCompleted(car, currentLap, lapTime.Value, isPersonalBestLap, isAbsoluteBestLap);
				}
				if (currentLap == laps - 1)
				{
					SetRaceEndByFirstFinishedCar(car);
				}
			}
			else
			{
				car.positionOnSpline.SetNextCheckpointIndex(GetLastSegmentIndex(num + 1));
			}
			return;
		}
		if (lastCompletedCheckpoint >= 0 || num != num2)
		{
			if (num > lastCompletedCheckpoint + 1 && (!car.missCheckPoint || m_lastCheckpointIndexWrong != num))
			{
				m_lastCheckpointIndexWrong = num;
				if (CanShowWrong())
				{
					car.missCheckPoint = true;
					car.ShowWrongCheckPointMessage();
				}
			}
			if (this.OnCheckpointMissed != null)
			{
				this.OnCheckpointMissed(car, currentLap, lastCompletedCheckpoint, (lastCompletedCheckpoint + 1) % m_checkpoints.Length, num);
			}
			return;
		}
		int checkpointsPassedPercent = GetCheckpointsPassedPercent(car);
		float completedTrackPercent = GetCompletedTrackPercent(car);
		if ((checkpointsPassedPercent == 0 && completedTrackPercent > 99.5f) || lastCompletedCheckpoint != -1 || num != num2 || checkpointsPassedPercent != 0 || !(completedTrackPercent > 1f))
		{
			return;
		}
		if (num > lastCompletedCheckpoint + 1 && (!car.missCheckPoint || m_lastCheckpointIndexWrong != num))
		{
			m_lastCheckpointIndexWrong = num;
			if (CanShowWrong())
			{
				car.missCheckPoint = true;
				car.ShowWrongCheckPointMessage();
			}
		}
		if (this.OnCheckpointMissed != null)
		{
			this.OnCheckpointMissed(car, currentLap, lastCompletedCheckpoint, (lastCompletedCheckpoint + 1) % m_checkpoints.Length, num);
		}
	}

	private void WriteLastActiveCheckPointPosition(RaceCar car)
	{
		Vector3 position = car.transform.position;
		Vector3 direction = car.transform.forward;
		if (car.positionOnSpline.track != null)
		{
			car.positionOnSpline.GetNearestCenterPosition(position, out position, out direction);
		}
		car.SetCheckPointPosition(position, direction);
		car.missCheckPoint = false;
		car.ShowWrongCheckPointMessage();
	}

	private bool CanShowWrong()
	{
		switch ((States.mainCurrent as BaseRaceModeState).raceMode)
		{
		case RaceMode.NetworkFreeride:
			if (NetworkController.InstanceGame != null)
			{
				NetGameSubroomsSystem netGameSubroomsSystem = NetworkController.InstanceGame.systems.Get<NetGameSubroomsSystem>();
				SubroomMode? subroomMode = netGameSubroomsSystem?.myRoom?.subroomMode;
				if (netGameSubroomsSystem == null || netGameSubroomsSystem.myRoom?.state != SubroomState.Race)
				{
					return false;
				}
				if (subroomMode == SubroomMode.XDS || subroomMode == SubroomMode.TandemXDS)
				{
					return false;
				}
			}
			break;
		case RaceMode.Freeride:
		case RaceMode.Dyno:
		case RaceMode.XDS:
		case RaceMode.Top32:
		case RaceMode.Top32NetRace:
			return false;
		}
		return true;
	}

	public void Reset()
	{
		Detach();
		ConfigurationManager instance = ConfigurationManager.instance;
		if (instance == null)
		{
			return;
		}
		CheckpointsData dataByType = instance.GetDataByType<CheckpointsData>();
		laps = dataByType.laps;
		m_startLine = dataByType.GetStartLine();
		if (m_startLine != null)
		{
			StartLine obj = m_startLine;
			obj.OnCarEnterEvent = (Action<RaceCar>)Delegate.Combine(obj.OnCarEnterEvent, new Action<RaceCar>(OnStartLineCarEnterEvent));
			BoxCollider component = m_startLine.GetComponent<BoxCollider>();
			if (component != null)
			{
				m_startLineTriggerDepth = component.size.z;
			}
		}
		m_checkpoints = dataByType.GetPoints();
		m_clippingObjects = instance.GetAndResetClippingObjects();
		ClearVisitedCheckpoints();
		if (m_checkpoints != null)
		{
			Checkpoint[] array = m_checkpoints;
			foreach (Checkpoint obj2 in array)
			{
				obj2.OnCarEnterEvent = (Action<Checkpoint, RaceCar>)Delegate.Combine(obj2.OnCarEnterEvent, new Action<Checkpoint, RaceCar>(OnCheckpointCarEnterEvent));
			}
		}
		m_trackConfigurationLength = null;
	}

	private void Detach()
	{
		ClearCars();
		if (m_startLine != null)
		{
			StartLine obj = m_startLine;
			obj.OnCarEnterEvent = (Action<RaceCar>)Delegate.Remove(obj.OnCarEnterEvent, new Action<RaceCar>(OnStartLineCarEnterEvent));
		}
		if (m_checkpoints != null)
		{
			Checkpoint[] array = m_checkpoints;
			foreach (Checkpoint obj2 in array)
			{
				obj2.OnCarEnterEvent = (Action<Checkpoint, RaceCar>)Delegate.Remove(obj2.OnCarEnterEvent, new Action<Checkpoint, RaceCar>(OnCheckpointCarEnterEvent));
			}
		}
	}

	public float GetForwardDistanceFromStart(RaceCar car)
	{
		float? totalTime = GetTotalTime(car);
		if (!totalTime.HasValue || totalTime.Value <= 0f)
		{
			return 0f;
		}
		float shift = car.carX.bounds.max.z * 1.1f + m_startLineTriggerDepth;
		float result = 0f;
		if (!BaseCar.IsNull(car) && car.positionOnSpline != null && m_startLine != null)
		{
			result = car.positionOnSpline.GetForwardDistance(m_startLine.distance, car.positionOnSpline.GetShiftedDistanceAlongCenterLine(shift));
		}
		return result;
	}

	public float GetStartLineDistance()
	{
		return m_startLine.distance;
	}

	public float GetCompletedTrackPercent(RaceCar car)
	{
		float? totalTime = GetTotalTime(car);
		if (!totalTime.HasValue || totalTime.Value <= 0f)
		{
			return 0f;
		}
		float num = 100f * GetForwardDistanceFromStart(car) / GetTrackConfigurationLength(car);
		num = ((num > 101f) ? 0f : num);
		return Mathf.Clamp(num, 0f, 100f);
	}

	public float GetTrackConfigurationLength(RaceCar car)
	{
		if (!m_trackConfigurationLength.HasValue)
		{
			m_trackConfigurationLength = car.positionOnSpline.GetForwardDistance(m_startLine.distance, GetLastCheckpoint().distance);
		}
		return m_trackConfigurationLength.Value;
	}

	public void OnDestroy()
	{
		if (m_startLine != null)
		{
			StartLine obj = m_startLine;
			obj.OnCarEnterEvent = (Action<RaceCar>)Delegate.Remove(obj.OnCarEnterEvent, new Action<RaceCar>(OnStartLineCarEnterEvent));
		}
		if (m_checkpoints != null)
		{
			Checkpoint[] array = m_checkpoints;
			foreach (Checkpoint obj2 in array)
			{
				obj2.OnCarEnterEvent = (Action<Checkpoint, RaceCar>)Delegate.Remove(obj2.OnCarEnterEvent, new Action<Checkpoint, RaceCar>(OnCheckpointCarEnterEvent));
			}
		}
	}

	private void OnStartLineCarEnterEvent(RaceCar raceCar)
	{
		if (raceCar != null)
		{
			RegisterStartRace(raceCar, Time.time);
		}
	}

	private void OnCheckpointCarEnterEvent(Checkpoint checkpoint, RaceCar raceCar)
	{
		if (raceCar != null)
		{
			RegisterCheckpoint(raceCar, checkpoint);
		}
	}

	public void UpdatePenaltyTime()
	{
		if (m_cars.Count == 0)
		{
			return;
		}
		float deltaTime = Time.deltaTime;
		int i = 0;
		for (int count = m_cars.Count; i < count; i++)
		{
			RaceCar raceCar = m_cars[i];
			if (raceCar.hasPenalty && m_info.TryGetValue(raceCar, out var value) && value.startTime.HasValue)
			{
				value.penaltyTime += deltaTime;
			}
		}
	}
}
