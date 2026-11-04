using System;
using System.Collections.Generic;
using CarX;
using CarX.Gameplay.ClippingZone;
using CodeStage.AntiCheat.ObscuredTypes;
using Quests;
using UnityEngine;

public class DriftController : MonoBehaviour
{
	public enum DriftState
	{
		No = 0,
		Drift = 1,
		Change = 2
	}

	public enum EDriftQuality
	{
		Min = 0,
		Secondary1 = 1,
		Secondary2 = 2,
		Max = 3
	}

	public enum MovementType
	{
		No = 0,
		Forward = 1,
		Fail = 2,
		Backward = 3
	}

	public enum DriftSide
	{
		No = 0,
		Left = 1,
		Right = 2
	}

	private const float SPEED_RANGE_MIN = 30f;

	private const float SPEED_RANGE_MAX = 120f;

	private const float ANG_RANGE_MIN = 20f;

	private const float ANG_RANGE_MAX = 10f;

	private const float START_SPEED_LIMIT = 30f;

	private const float DISTANCE_TOLERANCE = 2f;

	private DriftControllerStats m_drStats;

	private RaceCar m_car;

	private Car m_carX;

	private CheckpointManager m_checkpointManager;

	private bool m_allowDriftOnOutsideTrack;

	private bool m_allowRepeatDrift;

	private bool m_considerWrongWay;

	private int m_sequenceDpIndex;

	private float m_sequenceDpProgress;

	private MovementType m_movementType;

	private DriftState m_driftState;

	private ObscuredBool m_drift = false;

	private ObscuredFloat m_driftTime;

	private float m_noDriftTime;

	private ObscuredFloat m_expPoints;

	private ObscuredFloat m_driftPoints;

	private StatsCachedUpdater m_driftScoreStatUpdater;

	private ObscuredFloat m_extraDriftPoints;

	private float m_minDriftAngle;

	private ObscuredFloat m_driftAngle;

	private ObscuredFloat m_signAngle;

	private ObscuredFloat m_speed;

	private bool m_firstDriftZoneChecked = true;

	private float m_startDriftZone;

	private float m_endDriftZone;

	private List<BaseDriftAchievement> m_achievementList = new List<BaseDriftAchievement>();

	private bool m_start;

	public RaceCar tandemLeader;

	private ObscuredFloat m_tandemMultiplier = new ObscuredFloat(1f);

	private ObscuredFloat m_comboMultipiler;

	private ObscuredFloat m_comboReadyProgress;

	private ObscuredFloat m_pointsEarnSpeed;

	private bool m_IsInitiated;

	private bool m_IsWinterfallTrackActive;

	private bool m_IsVoodoActive;

	private bool m_IsCamaroActive;

	public bool allowRepeatDrift
	{
		get
		{
			return m_allowRepeatDrift;
		}
		set
		{
			m_allowRepeatDrift = value;
			m_firstDriftZoneChecked = true;
		}
	}

	public MovementType TypeMovement => m_movementType;

	public float signAngle => m_signAngle.Get();

	public RaceCar raceCar => m_car;

	public bool isDrift => m_drift;

	public float tandemMultiplier => m_tandemMultiplier.Get();

	public float comboMultipiler => m_comboMultipiler.Get();

	public float comboReadyProgress => m_comboReadyProgress.Get();

	public int pointsAndExtraPoints => Mathf.FloorToInt(m_driftPoints.Get() + m_extraDriftPoints.Get());

	public float pointsEarnSpeed => m_pointsEarnSpeed.Get();

	public EDriftQuality driftQuality { get; private set; }

	public DriftSide driftSide { get; private set; }

	public float driftAngle => m_driftAngle.Get();

	public float driftSpeed => m_speed.Get();

	public DriftState driftState
	{
		get
		{
			return m_driftState;
		}
		private set
		{
			m_driftState = value;
			DriftStateChanged(m_driftState);
		}
	}

	public Queue<ClippingZoneInfo> queueClippingZones { get; private set; }

	public Queue<ClippingPointInfo> queueClippingPoints { get; private set; }

	public Queue<ClippingPointInfo> queueCustomClippingPoints { get; private set; }

	public event Action<float> multiplierIncEvent;

	public event Action<float> comboPenaltyEvent;

	public event Action driftFailedEvent;

	public event Action<int> earnedPointsEvent;

	public void ResetClippingQueues()
	{
		if (tandemLeader == null)
		{
			queueClippingPoints.Clear();
			queueClippingZones.Clear();
		}
	}

	public ClippingZoneInfo DequeueClippingZoneInfo()
	{
		return queueClippingZones.Dequeue();
	}

	public ClippingPointInfo DequeueClippingPointInfo()
	{
		return queueClippingPoints.Dequeue();
	}

	private void Awake()
	{
		queueClippingZones = new Queue<ClippingZoneInfo>();
		queueClippingPoints = new Queue<ClippingPointInfo>();
		queueCustomClippingPoints = new Queue<ClippingPointInfo>();
		m_drStats = ScriptableSingleton<DriftControllerStats>.instance;
		if (m_drStats == null)
		{
			Debug.LogError("Couldn't find driftcontroller configs");
		}
		m_car = GetComponent<RaceCar>();
		if ((bool)m_car)
		{
			m_car.attachedDriftController = this;
			m_carX = m_car.carX;
		}
		m_driftScoreStatUpdater = new StatsCachedUpdater(StatsManager.EStats.DriftScore, 5000);
		m_achievementList.Add(new DriftFailedAchievement(this, m_drStats.GetAchievementTable(BaseDriftAchievement.AchievementType.None)));
		m_achievementList.Add(new DriftSpeedAchievement(this, m_drStats.GetAchievementTable(BaseDriftAchievement.AchievementType.Speed)).DeacivateInTandems());
		m_achievementList.Add(new DriftChangeAchievement(this, m_drStats.GetAchievementTable(BaseDriftAchievement.AchievementType.SideChange)).DeacivateInTandems());
		m_achievementList.Add(new DriftAngleAchievement(this, m_drStats.GetAchievementTable(BaseDriftAchievement.AchievementType.Angle)).DeacivateInTandems());
		m_achievementList.Add(new DriftAccuracyAchievement(this, m_drStats.GetAchievementTable(BaseDriftAchievement.AchievementType.Accuracy)).DeacivateInTandems());
		m_achievementList.Add(new DriftQualityAchievement(this, m_drStats.GetAchievementTable(BaseDriftAchievement.AchievementType.Quality)).DeacivateInTandems());
		m_achievementList.Add(new DriftJumpAchievement(this, m_drStats.GetAchievementTable(BaseDriftAchievement.AchievementType.Jump)).DeacivateInTandems());
		m_achievementList.Add(new DriftBackwardAchievement(this, m_drStats.GetAchievementTable(BaseDriftAchievement.AchievementType.Backward)).DeacivateInTandems());
		m_achievementList.Add(new DriftKissTheWallAchievement(this, m_drStats.GetAchievementTable(BaseDriftAchievement.AchievementType.KissTheWall)).DeacivateInTandems());
		m_achievementList.Add(new DriftClippingZoneAchievement(this, m_drStats.GetAchievementTable(BaseDriftAchievement.AchievementType.ClippingZone)));
		m_achievementList.Add(new DriftClippingPointAchievement(this, m_drStats.GetAchievementTable(BaseDriftAchievement.AchievementType.ClippingPoint)));
		m_achievementList.Add(new CustomEventAchievement(this, m_drStats.GetAchievementTable(BaseDriftAchievement.AchievementType.CustomForEvent)));
	}

	private void OnEnable()
	{
		m_start = false;
		m_drift = false;
		driftState = DriftState.No;
		ResetParams();
		ClearDp();
		SubscribeAchievements();
	}

	private void OnDisable()
	{
		UnsubscribeAchievements();
		m_driftScoreStatUpdater.SendCurrentToStat();
	}

	private void FixedUpdate()
	{
		if (!m_car || !m_start)
		{
			return;
		}
		if (!CheckAllowedDrift())
		{
			if (driftState != DriftState.No)
			{
				DriftFail();
			}
			return;
		}
		float deltaTime = Time.deltaTime;
		if (m_carX.speedMPS > 0.01f)
		{
			float dotProduct;
			float num = GetDriftAngle(m_carX, out dotProduct);
			m_movementType = GetMovementDriftType(m_car, num, dotProduct);
			m_driftAngle.Set(num);
		}
		else
		{
			m_driftAngle.Set(0f);
			m_movementType = MovementType.No;
		}
		float speedKMH = m_carX.speedKMH;
		m_speed.Set(speedKMH);
		if (speedKMH <= 30f)
		{
			driftState = DriftState.No;
		}
		CalcMinDriftAngle();
		UpdateDriftStateNo();
		UpdateDriftStateDrift(deltaTime);
		UpdateDriftStateChange(deltaTime);
		AnalizeDriftState(deltaTime);
	}

	private void Update()
	{
		if ((bool)m_car && m_start && (tandemLeader == null || tandemLeader == m_car))
		{
			UpdateAchievements(Time.deltaTime);
		}
	}

	public void Copy(DriftController controller)
	{
		m_drift = false;
		m_start = true;
		m_checkpointManager = controller.m_checkpointManager;
		m_allowDriftOnOutsideTrack = controller.m_allowDriftOnOutsideTrack;
		m_allowRepeatDrift = controller.m_allowRepeatDrift;
		m_considerWrongWay = controller.m_considerWrongWay;
		m_driftTime = controller.m_driftTime;
		m_noDriftTime = controller.m_noDriftTime;
		m_expPoints = controller.m_expPoints;
		m_driftPoints = controller.m_driftPoints;
		m_extraDriftPoints = controller.m_extraDriftPoints;
		m_pointsEarnSpeed = controller.m_pointsEarnSpeed;
		queueClippingZones = controller.queueClippingZones;
		queueClippingPoints = controller.queueClippingPoints;
		queueCustomClippingPoints = controller.queueCustomClippingPoints;
		EarnPoints();
	}

	public float AngleDir(Vector3 fwd, Vector3 targetDir, Vector3 up)
	{
		if ((double)Vector3.Dot(Vector3.Cross(fwd, targetDir), up) < 0.0)
		{
			return -1f;
		}
		return 1f;
	}

	public void ForceEarnPoints()
	{
		EarnPoints();
	}

	public void StopDrift()
	{
		ResetAchievements();
		EarnPoints();
		driftState = DriftState.No;
		m_drift = false;
		ResetParams();
	}

	public void Configure(bool allowDriftOnOutsideTrack, bool allowRepeatDrift, bool considerWrongWay, CheckpointManager checkpointManager)
	{
		m_allowDriftOnOutsideTrack = allowDriftOnOutsideTrack;
		m_allowRepeatDrift = allowRepeatDrift;
		m_considerWrongWay = considerWrongWay;
		m_checkpointManager = checkpointManager;
		m_start = true;
	}

	private void SubscribeAchievements()
	{
		for (int i = 0; i < m_achievementList.Count; i++)
		{
			m_achievementList[i].Subscribe(CollectAchievement);
		}
	}

	private void UnsubscribeAchievements()
	{
		for (int i = 0; i < m_achievementList.Count; i++)
		{
			m_achievementList[i].Unsubscribe(CollectAchievement);
		}
	}

	private void UpdateAchievements(float dt)
	{
		int count = m_achievementList.Count;
		for (int i = 0; i < count; i++)
		{
			BaseDriftAchievement baseDriftAchievement = m_achievementList[i];
			if (tandemLeader == null || baseDriftAchievement.isWorkInTandems)
			{
				baseDriftAchievement.Update(dt);
			}
		}
	}

	private void ResetAchievements()
	{
		foreach (BaseDriftAchievement achievement in m_achievementList)
		{
			achievement?.Reset();
		}
	}

	private void CollectAchievement(BaseDriftAchievement.AchievementType type, int order, float reward)
	{
		if (type != BaseDriftAchievement.AchievementType.ClippingZone && type != BaseDriftAchievement.AchievementType.ClippingPoint && type != BaseDriftAchievement.AchievementType.Addition && type != BaseDriftAchievement.AchievementType.KissTheWall)
		{
			reward *= (float)Mathf.FloorToInt(comboMultipiler);
		}
		int num = Mathf.FloorToInt(reward);
		StatsManager.EStats eStats = type.ToStat();
		if (eStats != StatsManager.EStats.None)
		{
			StatsManager.AddToStat(eStats, num);
		}
		m_extraDriftPoints.Add(reward);
		m_driftScoreStatUpdater.AddDelta(reward);
		if (type != BaseDriftAchievement.AchievementType.Addition)
		{
			UIDriftEventsContext.Show(type, order, num);
		}
	}

	public void ClippingZoneReaction(ClippingZoneInfo zone)
	{
		queueClippingZones.Enqueue(zone);
	}

	public void ClippingPointReaction(ClippingPointInfo point)
	{
		queueClippingPoints.Enqueue(point);
	}

	public void CustomClippingPointReaction(ClippingPointInfo point)
	{
		queueCustomClippingPoints.Enqueue(point);
	}

	private void UpdateDriftStateNo()
	{
		if (driftState == DriftState.No)
		{
			if (m_speed.Get() > 30f && m_driftAngle.Get() > m_minDriftAngle && m_movementType == MovementType.Forward && m_car.onGround)
			{
				driftState = DriftState.Drift;
				UIDriftEventsContext.ResetDriftFail();
			}
			ResetParams();
			EarnPoints();
		}
	}

	private void UpdateDriftStateDrift(float dt)
	{
		if (driftState == DriftState.Drift)
		{
			m_driftTime.Add(dt);
			if (m_driftAngle.Get() < m_minDriftAngle && m_movementType == MovementType.Forward)
			{
				driftState = DriftState.Change;
				m_noDriftTime = 0f;
			}
			if (m_movementType == MovementType.Fail)
			{
				DriftFail();
			}
		}
	}

	private void UpdateDriftStateChange(float dt)
	{
		if (driftState == DriftState.Change)
		{
			m_driftTime.Add(dt);
			m_noDriftTime += dt;
			if (m_noDriftTime > m_drStats.drNoTimeLimit)
			{
				driftState = DriftState.No;
			}
			else if (m_driftAngle.Get() > m_minDriftAngle && m_movementType == MovementType.Forward)
			{
				driftState = DriftState.Drift;
			}
		}
	}

	public void RegisterCollision(Collision collInfo)
	{
	}

	private void AnalizeDriftState(float dt)
	{
		float num = 0f;
		if (tandemLeader != null && tandemLeader != m_car)
		{
			m_car.positionOnSpline.track.centerLine.ProjectPoint(m_car.getTransform.position, out float along, out float across);
			m_car.positionOnSpline.track.centerLine.ProjectPoint(tandemLeader.getTransform.position, out float along2, out across);
			float num2 = Mathf.Abs(along2 - along);
			float num3 = Quaternion.Angle(m_car.transform.rotation, tandemLeader.transform.rotation);
			float num4 = ((num2 < 10f) ? 5f : Mathf.Clamp(5f - (num2 - 10f) / 5f, 0f, 5f));
			float num5 = Mathf.Clamp01(1f - num3 / 90f);
			m_tandemMultiplier.Set(num4 * num5);
			m_comboReadyProgress.Set(num4 / 5f);
			m_comboMultipiler.Set(num5);
			if (along > along2)
			{
				m_tandemMultiplier.Set(0f);
			}
		}
		if (driftState == DriftState.Drift && m_driftTime.Get() > m_drStats.drBeginTimeLimit)
		{
			m_drift = true;
			float num6 = Mathf.Max(m_driftAngle.Get(), 10f) - 5f;
			float num7 = driftSpeed - 20f;
			num = num6 * num7 * dt * m_drStats.calcDpCoeff;
			if (m_drStats.useSpeedMultiplier)
			{
				num *= Mathf.Clamp(num7, 0f, 90f) / 65f;
			}
			m_pointsEarnSpeed.Set((dt > 0f) ? (num / dt) : 0f);
			if (tandemLeader != null && tandemLeader != m_car)
			{
				float num8 = 5f * num * m_tandemMultiplier.Get();
				m_driftPoints.Add(num8);
				m_driftScoreStatUpdater.AddDelta(num8);
				m_expPoints.Add(dt * m_tandemMultiplier.Get());
			}
			else
			{
				m_tandemMultiplier.Set(1f);
				float num9 = num * comboMultipiler;
				m_driftPoints.Add(num9);
				m_driftScoreStatUpdater.AddDelta(num9);
				m_expPoints.Add(dt * comboMultipiler);
			}
		}
		else
		{
			m_drift = false;
			m_pointsEarnSpeed.Set(0f);
		}
		ComboUpdate(num, dt);
		UpdateDriftQuality(pointsEarnSpeed);
	}

	private void ComboUpdate(float dp, float dt)
	{
		int sequenceDp = m_drStats.GetSequenceDp(m_sequenceDpIndex);
		DriftControllerStats.DpProgressInfo dpProgressInfo = m_drStats.GetDpProgressInfo(Mathf.FloorToInt(comboMultipiler) - 1);
		if (pointsEarnSpeed > dpProgressInfo.thresholdDpSpeed)
		{
			m_sequenceDpProgress += dp;
			m_sequenceDpProgress = Mathf.Clamp(m_sequenceDpProgress, 0f, sequenceDp);
			float num = ((sequenceDp > 0) ? (m_sequenceDpProgress / (float)sequenceDp) : 0f);
			bool num2 = num >= 0.99f;
			m_comboReadyProgress.Set(Mathf.Clamp01(num));
			if (num2 && comboMultipiler < (float)m_drStats.maxCombo)
			{
				float value = comboMultipiler + m_drStats.comboDelta;
				value = Mathf.Clamp(value, m_drStats.minCombo, m_drStats.maxCombo);
				m_comboMultipiler.Set((float)Mathf.RoundToInt(value * 10f) / 10f + 0.001f);
				this.multiplierIncEvent.Call(m_drStats.comboDelta);
				SequenceIncNextDp();
			}
			return;
		}
		float num3 = ((dpProgressInfo.decreaseTime > 0f) ? ((float)sequenceDp / dpProgressInfo.decreaseTime) : 0f);
		m_sequenceDpProgress -= num3 * dt;
		m_sequenceDpProgress = Mathf.Clamp(m_sequenceDpProgress, 0f, sequenceDp);
		float num4 = ((sequenceDp > 0) ? (m_sequenceDpProgress / (float)sequenceDp) : 0f);
		m_comboReadyProgress.Set(Mathf.Clamp01(num4));
		if (num4 <= 0f)
		{
			if (comboMultipiler > (float)m_drStats.minCombo)
			{
				m_comboMultipiler.Set(Mathf.Clamp(m_comboMultipiler.Get() - m_drStats.comboDelta, m_drStats.minCombo, m_drStats.maxCombo));
				SequenceDecNextDp();
			}
			else
			{
				ClearSequenceProgress();
			}
		}
	}

	private void ComboPenalty(float amount)
	{
		m_comboMultipiler.Set(Mathf.Clamp(m_comboMultipiler.Get() - amount, m_drStats.minCombo, m_drStats.maxCombo));
		ClearSequenceProgress();
		this.comboPenaltyEvent.Call(amount);
	}

	private void DriftStateChanged(DriftState drState)
	{
		switch (drState)
		{
		case DriftState.Drift:
		{
			DriftSide driftSide = this.driftSide;
			this.driftSide = (ClockwiseRotation() ? DriftSide.Left : DriftSide.Right);
			if (driftSide != DriftSide.No)
			{
				_ = this.driftSide;
			}
			break;
		}
		case DriftState.No:
			this.driftSide = DriftSide.No;
			break;
		}
	}

	private bool CheckAllowedDrift()
	{
		float? totalTime = m_checkpointManager.GetTotalTime(m_car);
		if (!totalTime.HasValue || totalTime.Value <= 0.2f)
		{
			return false;
		}
		if (m_considerWrongWay && m_car.wrongWay)
		{
			return false;
		}
		if (m_car.offRoad)
		{
			return false;
		}
		if (!m_allowRepeatDrift)
		{
			float forwardDistanceFromStart = m_checkpointManager.GetForwardDistanceFromStart(m_car);
			if (m_firstDriftZoneChecked)
			{
				if (forwardDistanceFromStart > 1000f)
				{
					return false;
				}
				m_firstDriftZoneChecked = false;
				m_startDriftZone = forwardDistanceFromStart;
				m_endDriftZone = m_checkpointManager.GetTrackConfigurationLength(m_car);
			}
			if (!(m_startDriftZone - 2f < forwardDistanceFromStart) || !(forwardDistanceFromStart < m_endDriftZone + 2f))
			{
				return false;
			}
			if (Mathf.Abs(forwardDistanceFromStart - m_startDriftZone) < Mathf.Abs(forwardDistanceFromStart - m_endDriftZone))
			{
				m_startDriftZone = Mathf.Max(forwardDistanceFromStart, m_startDriftZone);
			}
			else
			{
				m_endDriftZone = Mathf.Min(forwardDistanceFromStart, m_endDriftZone);
			}
		}
		if (!m_allowDriftOnOutsideTrack && !m_car.positionOnSpline.isInsideTrack)
		{
			return false;
		}
		return true;
	}

	private void CalcMinDriftAngle()
	{
		float num = Mathf.Clamp01((m_speed.Get() - 30f) / 90f);
		m_minDriftAngle = 20f * (1f - num) + 10f * num;
	}

	public float CalcMinDriftAngleOtherCar(RaceCar raceCar)
	{
		float num = Mathf.Clamp01((raceCar.carX.speedKMH - 30f) / 90f);
		return 20f * (1f - num) + 10f * num;
	}

	public void CalcDriftAngleAndForwardSide(out float driftAngle, out MovementType movementType, RaceCar raceCar = null)
	{
		RaceCar car = raceCar ?? m_car;
		Car car2 = raceCar?.carX ?? m_carX;
		driftAngle = GetDriftAngle(car2, out var dotProduct);
		movementType = GetMovementDriftType(car, driftAngle, dotProduct);
	}

	public bool isDriftOtherCar(RaceCar raceCar)
	{
		if (raceCar == null)
		{
			return false;
		}
		float num = CalcMinDriftAngleOtherCar(raceCar);
		float dotProduct;
		float num2 = GetDriftAngle(raceCar.carX, out dotProduct);
		MovementType movementDriftType = GetMovementDriftType(raceCar, num2, dotProduct);
		if (raceCar.carX.speedKMH > 30f && num2 > num && movementDriftType == MovementType.Forward)
		{
			return raceCar.onGround;
		}
		return false;
	}

	private float GetDriftAngle(Car car, out float dotProduct)
	{
		dotProduct = Vector3.Dot(car.getTransform.forward, car.getRigidbody.velocity.normalized);
		m_signAngle.Set(AngleDir(car.getTransform.forward, car.getRigidbody.velocity.normalized, car.getTransform.up));
		return Mathf.Acos(Mathf.Clamp(dotProduct, -1f, 1f)) * 57.29578f;
	}

	private MovementType GetMovementDriftType(RaceCar car, float angle, float dotProduct)
	{
		if (!car.flipped && (bool)m_drStats)
		{
			if (dotProduct > 0f || (angle < m_drStats.angleBackwardLimit && (isDrift || car != m_car)))
			{
				if (!(angle < m_drStats.angleBackwardMin))
				{
					return MovementType.Backward;
				}
				return MovementType.Forward;
			}
			if (!(angle > m_drStats.angleBackwardLimit))
			{
				return MovementType.No;
			}
			return MovementType.Fail;
		}
		return MovementType.Fail;
	}

	private bool ClockwiseRotation()
	{
		return Vector3.Dot(Vector3.Cross(m_carX.getTransform.forward, m_carX.velocity.normalized), Vector3.up) > 0f;
	}

	private void EarnPoints()
	{
		bool flag = false;
		if (pointsAndExtraPoints > 0)
		{
			if (!m_IsInitiated)
			{
				m_IsInitiated = true;
				m_IsWinterfallTrackActive = StatsManager.IsWinterfallTrackActive();
				m_IsVoodoActive = StatsManager.IsVoodoActive();
				m_IsCamaroActive = StatsManager.IsCamaroActive();
			}
			int added = pointsAndExtraPoints;
			if (m_IsWinterfallTrackActive)
			{
				StatsManager.AddToStat(StatsManager.EStats.WinterDrift, added);
			}
			if (m_IsVoodoActive)
			{
				StatsManager.AddToStat(StatsManager.EStats.Voodoo, added);
			}
			if (m_IsCamaroActive)
			{
				StatsManager.AddToStat(StatsManager.EStats.DriftGuardian, added);
			}
			this.earnedPointsEvent.Call(pointsAndExtraPoints);
			flag = true;
		}
		if (m_expPoints.Get() > 0f)
		{
			GameEvents.DriftExpEarned(new GameEvents.DriftExpInfo(m_expPoints.Get()));
		}
		ClearDp();
		if (flag && States.mainCurrent is BaseRaceModeState baseRaceModeState)
		{
			GameEvents.DriftPointsEarned(new GameEvents.DriftPointsInfo(m_driftPoints.Get(), baseRaceModeState.currentDriftPoints));
			if (baseRaceModeState.raceMode == RaceMode.NetworkFreeride)
			{
				QuestController.EventCompleted("mp_session_drift_points", baseRaceModeState.currentDriftPoints, null, getRewards: false);
				QuestController.EventCompleted("mp_session_drift_points2", baseRaceModeState.currentDriftPoints, null, getRewards: false);
				QuestController.EventCompleted("mp_session_drift_points3", baseRaceModeState.currentDriftPoints);
			}
		}
	}

	private void DriftFail()
	{
		bool num = pointsAndExtraPoints > 0;
		driftState = DriftState.No;
		m_drift = false;
		ResetParams();
		EarnPoints();
		if (num)
		{
			this.driftFailedEvent.Call();
		}
	}

	private void ClearDp()
	{
		m_driftPoints.Set(0f);
		m_extraDriftPoints.Set(0f);
		m_expPoints.Set(0f);
		driftQuality = EDriftQuality.Min;
	}

	private void ResetParams()
	{
		m_noDriftTime = 0f;
		m_driftTime.Set(0f);
		m_comboMultipiler.Set(m_drStats.minCombo);
		ClearSequenceProgress();
	}

	private void SequenceIncNextDp()
	{
		if (m_sequenceDpIndex < m_drStats.sequenceDpLength - 1)
		{
			m_sequenceDpIndex++;
			m_sequenceDpProgress = 0f;
			m_comboReadyProgress.Set(0f);
		}
		else
		{
			ClearSequenceProgress();
		}
	}

	private void SequenceDecNextDp()
	{
		if (m_sequenceDpIndex > 0)
		{
			m_sequenceDpIndex--;
		}
		else
		{
			m_sequenceDpIndex = m_drStats.sequenceDpLength - 1;
		}
		m_sequenceDpProgress = m_drStats.GetSequenceDp(m_sequenceDpIndex);
		m_comboReadyProgress.Set(1f);
	}

	private void ClearSequenceProgress()
	{
		m_sequenceDpIndex = 0;
		m_sequenceDpProgress = 0f;
		m_comboReadyProgress.Set(0f);
	}

	private void UpdateDriftQuality(float dpSpeed)
	{
		if (dpSpeed < m_drStats.qualityDpSpeed_1)
		{
			driftQuality = EDriftQuality.Min;
		}
		else if (dpSpeed < m_drStats.qualityDpSpeed_2)
		{
			driftQuality = EDriftQuality.Secondary1;
		}
		else if (dpSpeed < m_drStats.qualityDpSpeed_3)
		{
			driftQuality = EDriftQuality.Secondary2;
		}
		else
		{
			driftQuality = EDriftQuality.Max;
		}
	}
}
