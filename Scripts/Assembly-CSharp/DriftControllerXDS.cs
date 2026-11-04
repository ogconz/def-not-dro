using System;
using System.Collections.Generic;
using System.Linq;
using CarX;
using CarX.Gameplay.ClippingZone;
using CarX.Points;
using CarX.XDSPoints;
using SyncMultiplayer;
using TrackLayout;
using TrackLayout.Behaviours;
using TrackLayout.Data;
using UnityEngine;

public class DriftControllerXDS : MonoBehaviour
{
	public enum TargetSide
	{
		NotActive = 0,
		Left = 1,
		Right = 2,
		StrictLeft = 3,
		StrictRight = 4,
		Depleted = 5
	}

	[Serializable]
	public class SectorInfo
	{
		public sRaceXDS_DriftSector info;

		public float minSpeed = 30f;

		public int prevSideChanges;

		public List<BaseClipping> clippings = new List<BaseClipping>();

		public DisqualifyReason discardReason;

		private int m_debugValue;

		public bool isDiscarded => discardReason != DisqualifyReason.Unknown;

		public float GetFinalScores()
		{
			if (!isDiscarded)
			{
				return info.mid_score * info.max_scores * TokenZonesPercent() + (float)m_debugValue;
			}
			return 0f;
		}

		public void AddDebugScore(int debugValue)
		{
			m_debugValue += debugValue;
		}

		public float TokenZonesPercent()
		{
			if (clippings.Count == 0)
			{
				return 1f;
			}
			int num = 0;
			foreach (BaseClipping clipping in clippings)
			{
				if (clipping == null || clipping.state != BaseClipping.State.Failed)
				{
					num++;
				}
			}
			return (float)num / (float)clippings.Count;
		}
	}

	public static DriftControllerXDS instance;

	public static bool enableShowdeltaV;

	public const float AfterInitiationNoDriftAllowedTime = 2f;

	public const float TimeForSpeedUp = 4f;

	public const float MaxCollisionSpeed = 4f;

	public const float MinDriftAngle = 20f;

	public const float MaxDriftAngle = 145f;

	public const float MinSpeedDefault = 40f;

	public const float StrictDriftAngle = 20f;

	public const float MaxTandemBonusScores = 0.03f;

	public const float MaxContactImpulse = 7f;

	public const float MaxContactTime = 3f;

	public const float MaxOvertakeDistance = 5f;

	public const float MinOvertakeDistance = 2f;

	public const float OvertakeDistanceTime = 3f;

	private const string DefaultCollisionsConfigAssetName = "CollisionsXDS/Default";

	private const string Top32CollisionsConfigAssetName = "CollisionsXDS/TOP32";

	private float m_initialMinSpeed = 40f;

	private float m_currentMinDist = 100f;

	public const float MaxDist = 100f;

	public const float LeaderDontStartedTimeBound = 3f;

	private const string FIRST_RACE_CONFIG = "xds_firstRace";

	private const string SECOND_RACE_CONFIG = "xds_secondRace";

	private const string SPEED_CONTROL_ZONES_CONFIG = "SpeedControlZonesConfig";

	private float m_score;

	private float m_overtakeTimer;

	private CritBrakingDetector m_critBrakingDetector;

	private bool m_enableStartHandle;

	private bool m_startHandled;

	private bool m_playerCrossedStart;

	private bool m_inRace;

	private byte m_spawnGeneration;

	private float m_safeNoDriftTime;

	public bool isSelfTandemLeader;

	private float m_startCountdownTime = 3f;

	private float m_tandemSlowTimer;

	private float m_preLaunchTime;

	private float m_launchTime;

	private float m_contactTime;

	private Car m_carX;

	private CheckpointManager m_checkpointManager;

	private bool m_driftDetected;

	public bool ignoreTermsOfDisqualification;

	public sRaceXDS_Output output;

	public SectorInfo[] sectors;

	public IList<BaseClipping> clippingObjects = new BaseClipping[0];

	public Action<DisqualifyReason> onSectorDiscard;

	public Action<DisqualifyReason> onRaceDisqualify;

	private HashSet<DisqualifyReason> m_strictBadList = new HashSet<DisqualifyReason>
	{
		DisqualifyReason.StartFailed,
		DisqualifyReason.ReverseDrive,
		DisqualifyReason.LostRoadContact,
		DisqualifyReason.LowSpeed,
		DisqualifyReason.CriticalAngle,
		DisqualifyReason.CarCollide,
		DisqualifyReason.MissedCheckpoint,
		DisqualifyReason.BadConnection,
		DisqualifyReason.ChaserCarCollide,
		DisqualifyReason.LeaderCarCollide
	};

	private HashSet<DisqualifyReason> m_leaderBadList = new HashSet<DisqualifyReason>
	{
		DisqualifyReason.BadClipZone,
		DisqualifyReason.BrakeUse,
		DisqualifyReason.AccelerateUse,
		DisqualifyReason.NoDrift,
		DisqualifyReason.CriticalBraking
	};

	private HashSet<DisqualifyReason> m_chaserBadList = new HashSet<DisqualifyReason> { DisqualifyReason.Overtaking };

	private float? m_testScore;

	private Vector3 m_lastValidOtherVel = Vector3.zero;

	private bool m_velInitiated;

	public TargetSide targetSide { get; private set; }

	public bool targetSoftMode { get; private set; }

	public float score
	{
		get
		{
			return m_testScore ?? m_score;
		}
		private set
		{
			m_score = value;
		}
	}

	public float bonusSign { get; private set; }

	public float bonusFactor { get; private set; }

	public float tandemFactor { get; private set; }

	public float totalTime { get; private set; }

	public float driftAngle { get; private set; }

	public float driftAngleAbs { get; private set; }

	public float accumulatedBrake { get; private set; }

	public float maxAccumulatedBrake { get; private set; }

	public bool isLeaderCritBrakingDetected => m_critBrakingDetector?.isCritBrakingDetected ?? false;

	public bool awaitingStart => m_enableStartHandle;

	public bool inRace => m_inRace;

	public bool resultMode { get; private set; }

	public bool isContactDetected { get; set; }

	public bool isSideChanged { get; set; }

	public bool isTandemMode { get; private set; }

	public RaceCar tandemLeader { get; private set; }

	public RaceCar car { get; private set; }

	public IRaceXDS xds { get; private set; }

	public CollisionsConfigXDS collisionConfig { get; private set; }

	private void Awake()
	{
	}

	private void SetCorrectionGraph(bool flag)
	{
	}

	public void Init()
	{
		m_leaderBadList.UnionWith(m_strictBadList);
		m_chaserBadList.UnionWith(m_strictBadList);
		instance = this;
		m_enableStartHandle = false;
		m_startHandled = false;
		m_inRace = false;
		BaseRaceState baseRaceState = States.mainCurrent as BaseRaceState;
		if ((bool)baseRaceState)
		{
			m_checkpointManager = baseRaceState.checkpointManager;
			m_checkpointManager.OnRaceStart += RaceStartHandler;
			m_checkpointManager.OnRaceEnd += RaceEndHandler;
			m_checkpointManager.OnLapCompleted += LapEndHandler;
			m_checkpointManager.OnCheckpointMissed += CheckpointMissedHandler;
			SetCar(baseRaceState.userCar);
		}
		ignoreTermsOfDisqualification = false;
		xds = new RaceXDS();
		xds.SetCoordSys(Vector3.right, Vector3.forward, Vector3.up);
		States.onStateActivate += GameSessionEndHandler;
		SpeedControlConfig speedControlConfig = Resources.Load<SpeedControlConfig>("SpeedControlZonesConfig");
		m_critBrakingDetector = new CritBrakingDetector(speedControlConfig.deltaDecreaseTimeSec, speedControlConfig.coolDownTime, speedControlConfig.maxBrakeAccel / 3.6f, OnCritBrakingDetected);
		LoadCollisionConfig(States.mainCurrent as BaseRaceState);
	}

	private void LoadCollisionConfig(BaseRaceState state)
	{
		if (state != null)
		{
			collisionConfig = CollisionsConfigXDS.Load("CollisionsXDS/TOP32");
		}
		else
		{
			collisionConfig = CollisionsConfigXDS.Load("CollisionsXDS/Default");
		}
	}

	public void SetCar(RaceCar car)
	{
		if ((bool)car && !(m_carX == car.carX))
		{
			if ((bool)this.car)
			{
				this.car.OnCollisionEnterEvent -= CollisionHandler;
			}
			this.car = car;
			m_carX = car.carX;
			car.OnCollisionEnterEvent += CollisionHandler;
		}
	}

	public void SetTargetSide(TargetSide side)
	{
		if (targetSide == side)
		{
			return;
		}
		if (!isSelfTandemLeader)
		{
			targetSoftMode = false;
			if (side == TargetSide.StrictLeft)
			{
				side = TargetSide.Left;
			}
			if (side == TargetSide.StrictRight)
			{
				side = TargetSide.Right;
			}
		}
		else if (driftAngleAbs < 20f)
		{
			targetSoftMode = targetSide == TargetSide.StrictLeft && side == TargetSide.Right;
			targetSoftMode = targetSoftMode || (targetSide == TargetSide.StrictRight && side == TargetSide.Left);
		}
		else
		{
			targetSoftMode = false;
		}
		targetSide = side;
		UpdateTargetDirector();
	}

	public void SetSpeedControl(bool toggle, bool collisionsOnly, float deltaV)
	{
		m_critBrakingDetector?.SetSpeedControl(toggle, collisionsOnly, raceDisqualifyAllowed: true, deltaV);
	}

	public void DebugAddScore(int addingValue)
	{
		int num = output.drift_cur_sector - 1;
		if (num >= 0 && num < sectors.Length - 1)
		{
			sectors[num].AddDebugScore(addingValue);
		}
	}

	public bool IsInDrift()
	{
		int num = output.drift_cur_sector - 1;
		if (m_inRace && driftAngleAbs > 20f && num >= 0 && num < sectors.Length)
		{
			return !sectors[num].isDiscarded;
		}
		return false;
	}

	private void OnDisable()
	{
		SetCorrectionGraph(flag: false);
	}

	protected void OnDestroy()
	{
		States.onStateActivate -= GameSessionEndHandler;
		if (m_checkpointManager != null)
		{
			m_checkpointManager.OnRaceStart -= RaceStartHandler;
			m_checkpointManager.OnRaceEnd -= RaceEndHandler;
			m_checkpointManager.OnLapCompleted -= LapEndHandler;
			m_checkpointManager.OnCheckpointMissed -= CheckpointMissedHandler;
		}
		if ((bool)car)
		{
			car.OnCollisionEnterEvent -= CollisionHandler;
		}
	}

	public void Update()
	{
		if (m_startHandled)
		{
			m_startHandled = false;
			InitiateRace();
		}
		bonusSign = Mathf.Lerp(bonusSign, 0f, Time.deltaTime * 10f);
		accumulatedBrake = 0f;
		maxAccumulatedBrake = 0f;
		if (m_critBrakingDetector != null)
		{
			accumulatedBrake = m_critBrakingDetector.GetAccumulatedBrake();
			maxAccumulatedBrake = m_critBrakingDetector.GetMaxAccumulatedBrake();
		}
	}

	private void CollisionHandler(Collision col)
	{
		if (!m_inRace)
		{
			return;
		}
		if (col.rigidbody == null)
		{
			float num = ((Time.time - m_contactTime < collisionConfig.blameTimeAfterContact && isSelfTandemLeader) ? collisionConfig.blameMaxWallCollisionImpact : collisionConfig.maxWallCollisionImpact);
			num *= car.getRigidbody.mass;
			if (col.impulse.sqrMagnitude >= num * num)
			{
				int sector = output.drift_cur_sector - 1;
				DiscardSector(sector, DisqualifyReason.CarCollide);
			}
			isContactDetected = true;
			return;
		}
		RaceCar component = col.rigidbody.GetComponent<RaceCar>();
		bool flag = component != null;
		if (flag)
		{
			m_contactTime = Time.time;
		}
		if (!isSelfTandemLeader && flag)
		{
			if (isTandemMode && m_overtakeTimer > 0f)
			{
				Internal_DisqualifyYourself(DisqualifyReason.ChaserCarCollide);
				return;
			}
			float num2 = CalcuateCarCollisonImpactSqr(col, component, car);
			float maxCarCollisionImpact = collisionConfig.maxCarCollisionImpact;
			maxCarCollisionImpact *= maxCarCollisionImpact;
			if (num2 > maxCarCollisionImpact)
			{
				int num3 = output.drift_cur_sector - 1;
				DisqualifyReason reason = ((!isTandemMode) ? DisqualifyReason.CarCollide : (isLeaderCritBrakingDetected ? DisqualifyReason.LeaderCarCollide : DisqualifyReason.ChaserCarCollide));
				if (num3 >= 0 && num3 < sectors.Length - 1)
				{
					DiscardSector(num3, reason);
				}
				else
				{
					Internal_DisqualifyYourself(reason);
				}
				return;
			}
		}
		isContactDetected = true;
	}

	public static float CalcuateCarCollisonImpactSqr(Collision collisionInfo, RaceCar otherCar, RaceCar myCar)
	{
		if (otherCar == null || myCar == null || collisionInfo == null || collisionInfo.contactCount == 0 || instance?.collisionConfig == null || instance.collisionConfig.useOld)
		{
			return collisionInfo?.relativeVelocity.sqrMagnitude ?? 0f;
		}
		Vector3 zero = Vector3.zero;
		for (int i = 0; i < collisionInfo.contactCount; i++)
		{
			zero += collisionInfo.GetContact(i).normal;
		}
		zero /= (float)collisionInfo.contactCount;
		float num = 1f;
		float num2 = myCar.getRigidbody.mass / otherCar.getRigidbody.mass;
		if (instance.collisionConfig.massRatioMode == CollisionsConfigXDS.MassRatioMode.On)
		{
			num = num2;
		}
		else if (instance.collisionConfig.massRatioMode == CollisionsConfigXDS.MassRatioMode.ClampLowest)
		{
			num = Mathf.Max(1f, num2);
		}
		return (Vector3.Dot(zero, collisionInfo.relativeVelocity) * zero * num).sqrMagnitude;
	}

	public void FixedUpdate()
	{
		if (Time.timeScale == 0f || !car)
		{
			return;
		}
		if (!m_inRace)
		{
			if (m_preLaunchTime == 0f || !awaitingStart || !(Time.time > m_preLaunchTime + m_startCountdownTime))
			{
				return;
			}
			if (car.carX.gear < 0 && car.carX.speedKMH > 1f)
			{
				Internal_DisqualifyYourself(DisqualifyReason.ReverseDrive);
				return;
			}
			m_tandemSlowTimer -= (isSelfTandemLeader ? Time.deltaTime : (0.6f * Time.deltaTime));
			if (m_tandemSlowTimer < 0f)
			{
				if (car.carX.speedKMH < 5f)
				{
					m_preLaunchTime = 0f;
					Internal_DisqualifyYourself(DisqualifyReason.StartFailed);
				}
				else if (driftAngleAbs > 20f && driftAngle * car.carX.steer > 0f)
				{
					Internal_DisqualifyYourself(DisqualifyReason.CriticalAngle);
				}
			}
			return;
		}
		if (CalcDriftAngle(car.carX, out var num, out var num2))
		{
			driftAngle = num;
			driftAngleAbs = num2;
		}
		bool flag = false;
		int num3 = output.drift_cur_sector - 1;
		if (car.carX.hasLaunchControl && !car.carX.hasTC)
		{
			car.useTC = num3 < 0;
		}
		if (num3 >= sectors.Length - 1)
		{
			flag = true;
		}
		else
		{
			if (num3 >= 0)
			{
				m_driftDetected = IsInDrift();
				if (sectors[num3].isDiscarded)
				{
					flag = true;
				}
				if (m_carX.speedKMH < GetCurrentMinSpeed())
				{
					DiscardSector(num3, DisqualifyReason.LowSpeed);
					flag = true;
				}
			}
			else if (m_carX.speedKMH < GetCurrentMinSpeed() && Time.time > m_launchTime + 4f)
			{
				DiscardSector(num3, (!m_playerCrossedStart) ? DisqualifyReason.StartFailed : DisqualifyReason.LowSpeed);
				flag = true;
			}
			if (car.positionOnSpline.isMovingInReverseDirection && num * car.carX.GetSteer() > 0f)
			{
				DiscardSector(num3, DisqualifyReason.CriticalAngle);
				flag = true;
			}
			if (car.wheelsCountOnGround >= 4)
			{
				if (car.wheelsCountOnTrack <= 1)
				{
					flag = true;
				}
				if (car.wheelsCountOnTrack == 0)
				{
					DiscardSector(num3, DisqualifyReason.LostRoadContact);
				}
			}
			if (m_spawnGeneration != NetworkController.InstanceGame.LocalPlayer.spawnGeneration)
			{
				m_spawnGeneration = NetworkController.InstanceGame.LocalPlayer.spawnGeneration;
				DiscardSector(num3, DisqualifyReason.LostRoadContact);
				flag = true;
			}
			if (num2 > 145f)
			{
				DiscardSector(num3, (car.carX.gear < 0) ? DisqualifyReason.ReverseDrive : DisqualifyReason.CriticalAngle);
				flag = true;
			}
			if (output.drift_show_info == 20 && !sectors[0].isDiscarded)
			{
				DiscardSector(0, DisqualifyReason.InitiationFailed);
				if (output.drift_init_time == 0f)
				{
					sectors[0].discardReason = (m_driftDetected ? DisqualifyReason.InitiationFailedByWrongSide : DisqualifyReason.InitiationFailedByNoDrift);
				}
				else
				{
					sectors[0].discardReason = DisqualifyReason.InitiationFailedByAngle;
				}
				if (m_safeNoDriftTime == 0f)
				{
					m_safeNoDriftTime = Time.time + 2f;
				}
			}
			if (output.drift_state == 4)
			{
				if (m_safeNoDriftTime == 0f || (m_safeNoDriftTime != 0f && Time.time > m_safeNoDriftTime))
				{
					DiscardSector(num3, DisqualifyReason.NoDrift);
					m_safeNoDriftTime = 0f;
				}
			}
			else
			{
				m_safeNoDriftTime = 0f;
			}
			if (num3 == 1 && !sectors[0].isDiscarded && sectors[0].info.mid_score == 0f)
			{
				DiscardSector(0, DisqualifyReason.InitiationFailed);
				sectors[0].discardReason = (m_driftDetected ? DisqualifyReason.InitiationFailedByWrongSide : DisqualifyReason.InitiationFailedByNoDrift);
			}
		}
		if (tandemLeader != null)
		{
			if (!m_velInitiated)
			{
				m_velInitiated = true;
				m_lastValidOtherVel = tandemLeader.transform.forward;
			}
			else
			{
				_ = car.getRigidbody.velocity;
				Vector3 velocity = tandemLeader.getRigidbody.velocity;
				if (velocity.sqrMagnitude > 1f)
				{
					m_lastValidOtherVel = velocity;
				}
			}
			float distanceBetweenPositions = car.positionOnSpline.GetDistanceBetweenPositions(tandemLeader.positionOnSpline.distanceAlongCenterLine, car.positionOnSpline.distanceAlongCenterLine);
			if (distanceBetweenPositions < 0f)
			{
				tandemFactor = 0f;
				if (!isSelfTandemLeader)
				{
					float num4 = distanceBetweenPositions * -1f;
					if (num4 > 2f && num4 < 5f)
					{
						m_overtakeTimer += Time.deltaTime;
					}
					else
					{
						m_overtakeTimer = 0f;
					}
					if (num4 > 5f || m_overtakeTimer > 3f)
					{
						Internal_DisqualifyYourself(DisqualifyReason.Overtaking);
					}
				}
			}
			else
			{
				m_overtakeTimer = 0f;
				float magnitude = (car.transform.position - tandemLeader.transform.position).magnitude;
				float num5 = car.lengthFront + tandemLeader.lengthRear + 1f;
				if (num3 < 1)
				{
					m_currentMinDist = 2f * num5;
				}
				else
				{
					m_currentMinDist = Mathf.MoveTowards(m_currentMinDist, num5, Time.deltaTime);
				}
				float f = Mathf.Clamp01(1f - (magnitude - m_currentMinDist) / 100f);
				tandemFactor = 1.03f * Mathf.Max(0.1f, Mathf.Pow(f, 5f));
			}
		}
		else
		{
			tandemFactor = 1f;
		}
		xds.SetCarParams(m_carX.speedKMH, num, flag ? 0f : (tandemFactor * bonusFactor), car.transform.TransformPoint(new Vector3(0f, 0.1f, car.lengthFront)));
		if (tandemLeader != null && CalcDriftAngle(tandemLeader.carX, out var driftang, out var _))
		{
			xds.SetLeaderParams(driftang, tandemLeader.transform.TransformPoint(new Vector3(0f, 0.1f, tandemLeader.lengthFront)));
		}
		xds.Update(Time.fixedDeltaTime);
		xds.GetOutput(out output);
		totalTime = 0f;
		score = 0f;
		for (int i = 0; i < output.num_drift_sectors - 1; i++)
		{
			xds.GetOutputDriftSector(i, out sectors[i].info);
			sRaceXDS_DriftSector info = sectors[i].info;
			float num7 = ((i < output.drift_cur_sector) ? sectors[i].GetFinalScores() : (100f * info.max_scores));
			totalTime += info.total_time;
			score += num7;
		}
		if (num3 >= 0 && sectors[num3].info.sideChangeCount != sectors[num3].prevSideChanges)
		{
			isSideChanged = true;
		}
		UpdateTargetDirector();
		m_critBrakingDetector?.Update(Time.deltaTime);
	}

	private void UpdateTargetDirector()
	{
		int num = output.drift_cur_sector - 1;
		if (num < 0 || num >= sectors.Length || sectors == null)
		{
			return;
		}
		SectorInfo sectorInfo = sectors[num];
		switch (targetSide)
		{
		case TargetSide.StrictLeft:
			if (driftAngle < -20f)
			{
				DiscardSector(num, DisqualifyReason.OppositeDrift);
			}
			break;
		case TargetSide.StrictRight:
			if (driftAngle > 20f)
			{
				DiscardSector(num, DisqualifyReason.OppositeDrift);
			}
			break;
		case TargetSide.Left:
		case TargetSide.Right:
		case TargetSide.Depleted:
			if (sectorInfo.info.sideChangeCount != sectorInfo.prevSideChanges)
			{
				sectorInfo.prevSideChanges = sectorInfo.info.sideChangeCount;
				if (targetSide == TargetSide.Depleted || (targetSide == TargetSide.Left && driftAngle < -20f) || (targetSide == TargetSide.Right && driftAngle > 20f))
				{
					if (targetSoftMode)
					{
						targetSoftMode = false;
					}
					else
					{
						DiscardSector(num, DisqualifyReason.SideChange);
					}
				}
				else
				{
					targetSide = TargetSide.Depleted;
					targetSoftMode = false;
				}
			}
			else
			{
				if (targetSoftMode && ((targetSide == TargetSide.Left && driftAngle > 20f) || (targetSide == TargetSide.Right && driftAngle < -20f)))
				{
					targetSoftMode = false;
				}
				if ((targetSide == TargetSide.Left && driftAngle > 20f) || (targetSide == TargetSide.Right && driftAngle < -20f))
				{
					targetSide = TargetSide.Depleted;
				}
			}
			break;
		}
		sectorInfo.prevSideChanges = sectorInfo.info.sideChangeCount;
	}

	public float GetCurrentMinSpeed()
	{
		int num = output.drift_cur_sector - 1;
		if (num >= 0)
		{
			if (!(tandemLeader == null))
			{
				return 0.8f * sectors[num].minSpeed;
			}
			return sectors[num].minSpeed;
		}
		if (!(tandemLeader == null))
		{
			return 0.8f * m_initialMinSpeed;
		}
		return m_initialMinSpeed;
	}

	public void ApplyBonusFactor(float val)
	{
		bonusFactor = val;
		if (bonusFactor < 0.0001f)
		{
			bonusFactor = 0.0001f;
		}
		if (val == 1f)
		{
			bonusSign = 0f;
		}
		else
		{
			bonusSign = ((val > 1f) ? 1f : (-1f));
		}
	}

	public void DisqualifyYourself(DisqualifyReason reason)
	{
		if (isSelfTandemLeader && m_leaderBadList.Contains(reason))
		{
			Internal_DisqualifyYourself(reason);
		}
		else if (!isSelfTandemLeader && tandemLeader != null && m_chaserBadList.Contains(reason))
		{
			Internal_DisqualifyYourself(reason);
		}
	}

	public void SelfDisqualify(DisqualifyReason reason)
	{
		Internal_DisqualifyYourself(reason);
	}

	private void Internal_DisqualifyYourself(DisqualifyReason reason)
	{
		if (!ignoreTermsOfDisqualification && (m_inRace || awaitingStart))
		{
			if (isSelfTandemLeader && isTandemMode && Time.time - m_contactTime < collisionConfig.blameTimeAfterContact)
			{
				reason = DisqualifyReason.ChaserCarCollide;
			}
			DiscardRace();
			int num = output.drift_cur_sector - 1;
			if (num >= 0 && num < sectors.Length - 1)
			{
				DiscardSector(num, reason, raceDisqualifyAllowed: false);
				sectors[num].discardReason = reason;
			}
			resultMode = true;
			onRaceDisqualify.SafeCall(reason);
			NetworkController.InstanceGame.systems.Get<NetGameSubroomsSystem>()?.DisqualifyYourself(reason);
		}
	}

	public void DiscardCurrentSector(DisqualifyReason reason)
	{
		int num = output.drift_cur_sector - 1;
		if (num >= 0 && num < sectors.Length - 1)
		{
			DiscardSector(num, reason, raceDisqualifyAllowed: false);
		}
	}

	private void DiscardSector(int sector, DisqualifyReason reason, bool raceDisqualifyAllowed = true)
	{
		if (ignoreTermsOfDisqualification)
		{
			return;
		}
		if (raceDisqualifyAllowed)
		{
			if (isSelfTandemLeader)
			{
				if (m_leaderBadList.Contains(reason))
				{
					Internal_DisqualifyYourself(reason);
					return;
				}
			}
			else if (tandemLeader != null && m_chaserBadList.Contains(reason))
			{
				Internal_DisqualifyYourself(reason);
				return;
			}
		}
		if (!output.drift_cur_sector_blank && sector >= 0 && sector < sectors.Length && sectors[sector].info.max_scores != 0f && !sectors[sector].isDiscarded)
		{
			sectors[sector].discardReason = reason;
			onSectorDiscard?.SafeCall(reason);
		}
	}

	public bool IsSectorDiscarded(int sector)
	{
		if (sector < 0 || sector >= sectors.Length)
		{
			return false;
		}
		return sectors[sector].isDiscarded;
	}

	public void ReadSectorsData()
	{
		m_spawnGeneration = NetworkController.InstanceGame.LocalPlayer.spawnGeneration;
		bonusFactor = 1f;
		bonusSign = 0f;
		xds.Reset();
		m_velInitiated = false;
		output = default(sRaceXDS_Output);
		if (m_checkpointManager == null)
		{
			sectors = new SectorInfo[0];
			return;
		}
		xds.SetSpawnPoint(m_checkpointManager.startLine.transform.position, m_checkpointManager.startLine.transform.forward);
		if (Singleton<TrackLayoutController>.instance.legacyTrackContainer != null && Singleton<TrackLayoutController>.instance.legacyTrackContainer.sectors.Count > 0)
		{
			m_initialMinSpeed = Singleton<TrackLayoutController>.instance.GetData().initialMinSpeed;
			float[] currentPointsArray = GetCurrentPointsArray();
			List<XDSSector> list = Singleton<TrackLayoutController>.instance.legacyTrackContainer.sectors;
			sectors = new SectorInfo[list.Count];
			for (int i = 0; i < list.Count; i++)
			{
				sectors[i] = new SectorInfo();
			}
			int i2 = 0;
			{
				foreach (XDSSector item in list)
				{
					AddSector(item, (i2 < currentPointsArray.Length) ? currentPointsArray[i2] : 0f, list.Count, item.isBlank || i2 == list.Count - 1, item.sideChangeFactor, ref i2);
				}
				return;
			}
		}
		m_initialMinSpeed = 40f;
		float[] currentPointsArrayOld = GetCurrentPointsArrayOld();
		IList<Checkpoint> checkpoints = m_checkpointManager.checkpoints;
		sectors = new SectorInfo[checkpoints.Count];
		for (int j = 0; j < checkpoints.Count; j++)
		{
			sectors[j] = new SectorInfo();
		}
		int i3 = 0;
		foreach (Checkpoint item2 in checkpoints)
		{
			AddSector(item2, (i3 < currentPointsArrayOld.Length) ? currentPointsArrayOld[i3] : 0f, checkpoints.Count, item2.isBlankXDS || i3 == checkpoints.Count - 1, item2.sideChangeFactorXDS, ref i3);
		}
	}

	public static float[] GetCurrentPointsArray()
	{
		SectorData[] array = Singleton<TrackLayoutController>.instance.GetData().sectors;
		float[] array2 = new float[array.Length];
		int num = 0;
		int num2 = 0;
		float num3 = 0f;
		for (int i = 0; i < array.Length; i++)
		{
			SectorData sectorData = array[i];
			if (sectorData.isBlank)
			{
				num++;
			}
			else if (sectorData.weight == 0f)
			{
				num2++;
			}
			num3 += sectorData.weight;
		}
		int num4 = Mathf.Max(1, array.Length - num);
		float num5 = ((num3 == 0f) ? 1f : (num3 / (float)num4));
		num3 += (float)num2 * num5;
		for (int j = 0; j < array.Length; j++)
		{
			SectorData sectorData2 = array[j];
			if (sectorData2.isBlank)
			{
				array2[j] = 0f;
			}
			else
			{
				array2[j] = ((sectorData2.weight == 0f) ? (num5 / num3) : (sectorData2.weight / num3));
			}
		}
		return array2;
	}

	public float[] GetCurrentPointsArrayOld()
	{
		IList<Checkpoint> checkpoints = m_checkpointManager.checkpoints;
		float[] array = new float[checkpoints.Count];
		int num = 0;
		int num2 = 0;
		float num3 = 0f;
		for (int i = 0; i < checkpoints.Count; i++)
		{
			Checkpoint checkpoint = checkpoints[i];
			if (checkpoint.isBlankXDS)
			{
				num++;
			}
			else if (checkpoint.weightXDS == 0)
			{
				num2++;
			}
			num3 += (float)checkpoint.weightXDS;
		}
		int num4 = Mathf.Max(1, checkpoints.Count - num);
		float num5 = ((num3 == 0f) ? 1f : (num3 / (float)num4));
		num3 += (float)num2 * num5;
		for (int j = 0; j < checkpoints.Count; j++)
		{
			Checkpoint checkpoint2 = checkpoints[j];
			if (checkpoint2.isBlankXDS)
			{
				array[j] = 0f;
			}
			else
			{
				array[j] = ((checkpoint2.weightXDS == 0) ? (num5 / num3) : ((float)checkpoint2.weightXDS / num3));
			}
		}
		return array;
	}

	private void AddSector(XDSSector xdsSector, float max_points, int count, bool isBlank, float sideChangeFactor, ref int i)
	{
		Vector3 leftPoint = xdsSector.leftPoint;
		Vector3 rightPoint = xdsSector.rightPoint;
		xds.WriteDriftSector(i, leftPoint, rightPoint, isBlank, sideChangeFactor, isBlank ? 0f : max_points);
		sectors[i].minSpeed = xdsSector.minSpeed;
		sectors[i].prevSideChanges = 0;
		i++;
	}

	private void AddSector(Checkpoint checkpoint, float max_points, int count, bool isBlank, float sideChangeFactor, ref int i)
	{
		BoxCollider component = checkpoint.GetComponent<BoxCollider>();
		Vector3 vector = component.size / 2f;
		Vector3 position = new Vector3(component.center.x - vector.x, 0f, 0f);
		Vector3 position2 = new Vector3(component.center.x + vector.x, 0f, 0f);
		position = component.transform.TransformPoint(position);
		position2 = component.transform.TransformPoint(position2);
		xds.WriteDriftSector(i, position, position2, isBlank, sideChangeFactor, isBlank ? 0f : max_points);
		sectors[i].minSpeed = checkpoint.minSpeedXDS;
		sectors[i].prevSideChanges = 0;
		i++;
	}

	private bool CalcDriftAngle(Car carx, out float driftAngle, out float driftAngleAbs)
	{
		driftAngle = 0f;
		driftAngleAbs = 0f;
		float magnitude = carx.getRigidbody.velocity.magnitude;
		if (magnitude < 1f)
		{
			return false;
		}
		Vector3 vector = carx.getRigidbody.velocity / magnitude;
		Vector3 forward = carx.getTransform.forward;
		float value = Vector3.Dot(forward, vector);
		float num = AngleDir(forward, vector, carx.getTransform.up);
		float f = Mathf.Acos(Mathf.Clamp(value, -0.999f, 0.999f)) * 57.29578f;
		driftAngleAbs = Mathf.Abs(f);
		driftAngle = num * driftAngleAbs;
		return true;
	}

	public float AngleDir(Vector3 fwd, Vector3 targetDir, Vector3 up)
	{
		if ((double)Vector3.Dot(Vector3.Cross(fwd, targetDir), up) < 0.0)
		{
			return -1f;
		}
		return 1f;
	}

	private void GameSessionEndHandler(BaseState newState)
	{
		if (!(States.mainCurrent is BaseRaceState))
		{
			m_inRace = false;
		}
	}

	private void RaceEndHandler()
	{
		m_inRace = false;
	}

	private void LapEndHandler(RaceCar car, int lap, float lapTime, bool isPersonalBestLap, bool isAbsoluteBestLap)
	{
		FinishRace();
	}

	private void CheckpointMissedHandler(RaceCar car, int lap, int previousCheckpoint, int expectedCheckpoint, int reachedCheckpoint)
	{
		if (previousCheckpoint != reachedCheckpoint)
		{
			Internal_DisqualifyYourself(DisqualifyReason.MissedCheckpoint);
		}
	}

	private void InitiateRace()
	{
		m_preLaunchTime = 0f;
		m_launchTime = Time.time;
		Debug.Log("<color=green>XDS</color>> RaceStart");
		m_inRace = true;
		ReadSectorsData();
		xds.SetCarParams(0f, 0f, 1f, car.transform.TransformPoint(new Vector3(0f, 0.1f, car.lengthFront)));
		if (tandemLeader != null)
		{
			xds.SetLeaderParams(0f, tandemLeader.transform.TransformPoint(new Vector3(0f, 0.1f, tandemLeader.lengthFront)));
		}
		xds.Update(0f);
		xds.GetOutput(out output);
		bool flag = isSelfTandemLeader || tandemLeader == null;
		IList<BaseClipping> list2;
		if (!flag)
		{
			IList<BaseClipping> list = new BaseClipping[0];
			list2 = list;
		}
		else
		{
			list2 = m_checkpointManager.clippingObjects;
		}
		clippingObjects = list2;
		string style = (flag ? "xds_firstRace" : "xds_secondRace");
		clippingObjects = clippingObjects.Where(delegate(BaseClipping itr)
		{
			ClippingZoneSpline clippingZoneSpline = itr as ClippingZoneSpline;
			if ((itr.transform.parent.parent.name == "xds_firstRace" || itr.transform.parent.parent.name == "xds_secondRace") && itr.transform.parent.parent.name != style)
			{
				return false;
			}
			return (!(clippingZoneSpline == null) && clippingZoneSpline.zoneType == ClipZoneType.Default) ? true : false;
		}).ToList();
		foreach (BaseClipping clippingObject in clippingObjects)
		{
			int sector = clippingObject.sector;
			if (sector >= 0 && sector < sectors.Length)
			{
				sectors[sector].clippings.Add(clippingObject);
			}
		}
	}

	private void RaceStartHandler(RaceCar car)
	{
		if (m_enableStartHandle)
		{
			m_startHandled = true;
			m_enableStartHandle = false;
			resultMode = false;
		}
		if (car.isLocalPlayer)
		{
			m_playerCrossedStart = true;
		}
	}

	public void SetupTandemLead(RaceCar leadCar)
	{
		tandemLeader = ((car == leadCar) ? null : leadCar);
		isSelfTandemLeader = leadCar == null || car == leadCar;
		if ((bool)car && (bool)leadCar)
		{
			leadCar.positionOnSpline.track = car.positionOnSpline.track;
		}
		m_critBrakingDetector?.SetTarget(leadCar ? leadCar.carX : car.carX);
		isTandemMode = leadCar != null;
	}

	public void PrepareForStart(float startCountdownTime)
	{
		isTandemMode = false;
		m_overtakeTimer = 0f;
		m_startCountdownTime = ((startCountdownTime < 0.1f) ? 3f : startCountdownTime);
		isSelfTandemLeader = true;
		m_driftDetected = false;
		driftAngle = 0f;
		driftAngleAbs = 0f;
		score = 0f;
		m_safeNoDriftTime = 0f;
		bonusFactor = 1f;
		bonusSign = 0f;
		isContactDetected = false;
		isSideChanged = false;
		tandemLeader = null;
		targetSide = TargetSide.NotActive;
		sectors = Array.Empty<SectorInfo>();
		resultMode = false;
		m_inRace = false;
		m_enableStartHandle = true;
		m_playerCrossedStart = false;
		m_preLaunchTime = Time.time;
		m_tandemSlowTimer = 3f;
		tandemFactor = 1f;
		clippingObjects = Array.Empty<BaseClipping>();
		m_critBrakingDetector?.ResetFull();
		if (car.carX.hasLaunchControl && !car.carX.hasTC)
		{
			car.useTC = true;
		}
	}

	public void DiscardRace(bool discardScore = true)
	{
		if (discardScore)
		{
			score = 0f;
		}
		m_enableStartHandle = false;
		m_inRace = false;
		resultMode = false;
		tandemLeader = null;
		m_overtakeTimer = 0f;
	}

	public void FinishRace()
	{
		m_enableStartHandle = false;
		if (!m_inRace)
		{
			return;
		}
		foreach (BaseClipping clippingObject in clippingObjects)
		{
			if (clippingObject.state == BaseClipping.State.Idle)
			{
				clippingObject.FailByRaceEnd();
			}
		}
		output.drift_cur_sector = output.num_drift_sectors;
		output.drift_cur_scores = 0f;
		driftAngle = 0f;
		m_inRace = false;
		resultMode = true;
		tandemLeader = null;
		m_overtakeTimer = 0f;
	}

	private void OnCritBrakingDetected(bool raceDisqualifyAllowed)
	{
		if (isSelfTandemLeader)
		{
			if (raceDisqualifyAllowed)
			{
				Internal_DisqualifyYourself(DisqualifyReason.CriticalBraking);
			}
			else
			{
				DiscardCurrentSector(DisqualifyReason.CriticalBraking);
			}
		}
	}

	public static int ConvertScoresToInt(float scores)
	{
		return Mathf.RoundToInt(scores * 100f);
	}

	public static string ConvertScoresToResultString(float scores)
	{
		return scores.ToString("0.00");
	}

	public static string ConvertScoresToResultString(int scores)
	{
		return ((float)scores / 100f).ToString("0.00");
	}

	public static string GetSectorName(int sector)
	{
		return ((sector == 0) ? LocalizationKeys.xds_initiation.Localized() : LocalizationKeys.xds_sector.Localized()) + " S" + sector;
	}

	private void OnGUI()
	{
		if (enableShowdeltaV)
		{
			float x = (float)Screen.width / 2.5f;
			float y = (float)Screen.height * 0.9f;
			_ = Screen.width;
			float width = 0f;
			_ = Screen.height;
			GUI.Label(new Rect(x, y, width, 0f), style: new GUIStyle
			{
				fontSize = 28,
				normal = 
				{
					textColor = Color.white
				}
			}, text: "DeltaV : " + accumulatedBrake + " / " + maxAccumulatedBrake);
		}
	}
}
