using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using CarModelSystem;
using CarX;
using DB.Meta;
using DI;
using Driver;
using GameInput;
using HUD;
using Minimaps;
using PrefsModels;
using Rewired;
using SyncMultiplayer;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

[AddComponentMenu("Scripts/Car/RaceCar")]
public class RaceCar : BaseCar
{
	private static readonly Vector3 DefaultRearWallCheckPointOffset = new Vector3(0.92f, 0.5f, 1f);

	private static readonly int[] ALLOWED_ANGLES = new int[4] { 180, 360, 540, 900 };

	[NonSerialized]
	private float m_networkQuality = 1f;

	[NonSerialized]
	public GameObject shadowProjectorGO;

	[NonSerialized]
	public CarFlares flares;

	private Transform[] m_wheelCapsParents;

	private Coroutine m_placeOnTrackCoroutine;

	private Coroutine m_placeOnTrackIfFlippedCoroutine;

	private Coroutine m_showReturnCarHelpMessageCoroutine;

	private Coroutine m_waitSpawnCoroutine;

	private Coroutine m_waitFlipCoroutine;

	private WaitForFixedUpdate m_fixedWait = new WaitForFixedUpdate();

	private PlayerCar m_metaInfo;

	private PlayerCarProfile m_profileMetaInfo;

	private PositionOnSpline m_positionOnSpline;

	private CarLightController m_carLightController;

	private bool m_lockState;

	private BaseWheelSmoke[] m_wheelSmokes;

	private Track m_currentTrack;

	private RaceCalculator m_raceCalc = new RaceCalculator();

	private GroundDetector m_groundDetector;

	private CARXWheelSkidmark[] m_skidMarks;

	private CarPlacementModel m_carPlacementModel;

	private CarOptimizationModel m_optimizationModel;

	private const float SMOOTH_STEERING = 100f;

	private const float RESET_TO_SPLINE_DISTANCE = 50f;

	private const int INTEGRATION_STEPS_DEFAULT = 16;

	private const float TIME_CAR_FLIP = 5f;

	private const float SPAWN_POINT_UPDATE_STEP = 5f;

	private const string DRIVER_EMPTY_PARENT_GO = "DriverEmptyParent";

	private const float TeleportCooldown = 5f;

	private float? m_filteredSteerAngle;

	[SerializeField]
	private float m_autoFlipPeriod = 10f;

	private bool m_inGarage;

	private PeriodTimer m_autoFlipPeriodTimer;

	private bool m_lastFlipped;

	private bool m_isGhost;

	[SerializeField]
	private Vector3 m_rearOffset = DefaultRearWallCheckPointOffset;

	private MeshRenderer[] m_meshRendererArray;

	private bool[] m_meshRendererEnabledStateArray;

	private bool m_lightSwitch;

	private bool m_isLogicUpdateEnabled = true;

	private float m_startLoadingVisualTime;

	private CarModel m_carModel;

	private Transform m_driverTransform;

	private DriverSteeringWheel m_steeringWheelObject;

	private bool m_enableDriverModel;

	private DriverAnimController m_driverAnimController;

	private GamePrefs m_prefs;

	private GameObject m_emptyDriverParent;

	private GameObject m_driverInstance;

	private GraphicsQuality m_graphicsQuality;

	private Transform m_modelTransform;

	private Transform m_skeletonTransform;

	private List<Transform> m_windowsRoots;

	private int m_emptyHoodId = -1;

	private Vector3 m_lastOnRoadPosition;

	private Quaternion m_lastOnRoadRotation;

	private Vector3 m_checkPointPosition;

	private Vector3 m_checkPointDirection;

	public bool missCheckPoint;

	private bool m_lastOnSplineRoadDirectionForward;

	private float m_lastOnRoadDistanceTraveled;

	private float m_resetOnSplineDoubleClickTimer;

	public static readonly string SKELETON_TRANSFORM_NAME = "Skeleton";

	private static string[] m_gamepadButtonsArray = new string[8] { "A", "B", "X", "Y", "Circle", "Square", "Triangle", "Cross" };

	private CarEngineController m_engineController = new CarEngineController();

	private List<AsyncOperationHandle<RuntimeAnimatorController>> m_asyncOperationHandles = new List<AsyncOperationHandle<RuntimeAnimatorController>>();

	private float m_currentSteer;

	private float m_currTimeRefreshSpawn;

	public DriftController attachedDriftController;

	public Color networkColor = Color.clear;

	public NetworkPlayer networkPlayer;

	public bool lockedByCamera;

	public float disableRGearTime;

	private float? m_steerSmoothDamp;

	private float m_steerVelSmoothDamp;

	private int m_lastMissCheckPointHash;

	private CARXWheelMorphing[] m_deformWheels;

	private AsyncOperation m_unloadAsyncOp;

	public static bool carDebugInfo = false;

	private PlayerCarFactory m_playerCarFactory;

	public float networkQuality
	{
		get
		{
			return m_networkQuality;
		}
		set
		{
			SetNetworkQuality(value);
		}
	}

	public Transform[] wheelCapsParents => m_wheelCapsParents;

	public int carId { get; set; }

	public bool isPodiumCar { private get; set; }

	public bool IsLoadingVisualInProgress { get; private set; }

	public bool IsVisualLoaded { get; private set; }

	public int LastLoadVisualHash { get; private set; }

	public bool IsLiveryReady => carModel.livery.IsReady;

	public Transform ugcMapSpawnPoint { get; set; }

	public CarLightController carLightController => m_carLightController;

	public CarPlacementModel carPlacementModel => m_carPlacementModel;

	public bool headlightsEnabled { get; set; }

	public bool enableCollisionHitSound { get; set; } = true;

	public Transform steeringWheelRootTransform
	{
		get
		{
			if (!(m_steeringWheelObject != null))
			{
				return null;
			}
			return m_steeringWheelObject.transform.parent.transform;
		}
	}

	public CarCameraSwitchHandler driverCamSwitchHandler { get; private set; }

	public GamePrefs prefs => m_prefs;

	public bool engineModelLoaded => swap.IsEngineModelLoaded();

	public CarSwap swap { get; } = new CarSwap();

	public bool vinylsNotSupport
	{
		get
		{
			return m_carModel.vinylsNotSupport;
		}
		set
		{
			m_carModel.vinylsNotSupport = value;
		}
	}

	public bool onGround => m_groundDetector.onGround;

	public bool offRoad => m_groundDetector.offRoad;

	public bool isNetworkCar
	{
		get
		{
			if (networkPlayer != null)
			{
				return !networkPlayer.IsLocal;
			}
			return false;
		}
	}

	public bool isLocalPlayer
	{
		get
		{
			if (PlayerCarControl.instance != null)
			{
				return PlayerCarControl.instance.car == this;
			}
			return false;
		}
	}

	public bool flipped
	{
		get
		{
			if (!isNetworkCar)
			{
				return m_groundDetector.flipped;
			}
			return false;
		}
	}

	public int wheelsCountOnGround => m_groundDetector.wheelsCountOnGround;

	public int wheelsCountOnTrack => m_groundDetector.WheelsCountOnTrack;

	public bool hasPenalty => offRoad;

	public float distanceOnTrack => m_positionOnSpline.distanceAlongCenterLine;

	public bool wrongWay
	{
		get
		{
			if (m_positionOnSpline.isMovingInReverseDirection && base.carX.gear != -1)
			{
				return Mathf.Abs(Vector3.Dot(base.carX.velocity, base.getTransform.forward)) > 0.2f;
			}
			return false;
		}
	}

	public PlayerCar metaInfo => m_metaInfo;

	public PlayerCarProfile profileMetaInfo
	{
		get
		{
			return m_profileMetaInfo;
		}
		set
		{
			m_profileMetaInfo = value;
		}
	}

	public bool isSequentialTransmissionAvailable
	{
		get
		{
			if (profileMetaInfo.CanSequentialTransmission())
			{
				return base.carX.sequentialTransmission;
			}
			return false;
		}
	}

	public bool isWasteGateAvailable
	{
		get
		{
			if (profileMetaInfo.CanWastegate())
			{
				return base.carX.engineWastegate;
			}
			return false;
		}
	}

	public PositionOnSpline positionOnSpline => m_positionOnSpline;

	public bool lockState
	{
		get
		{
			if (!m_lockState)
			{
				return lockedByCamera;
			}
			return true;
		}
		set
		{
			if (m_lockState != value)
			{
				m_lockState = value;
				InputSettings.Parameters parameters = GameInput.InputManager.settings.Params;
				base.carX.autoClutch = !m_lockState && parameters.autoClutch;
				base.carX.clutch = (m_lockState ? 1f : base.carX.clutch);
				base.carX.handbrake = (m_lockState ? 1 : 0);
			}
		}
	}

	public float raceCarClutchInputVal { get; set; }

	public bool isClutchKick { get; set; }

	public bool isGhost
	{
		get
		{
			return m_isGhost;
		}
		set
		{
			if (m_isGhost != value)
			{
				m_isGhost = value;
				OnIsGhostChanged();
			}
		}
	}

	public CarModel carModel => m_carModel;

	public CarEngineController engineController => m_engineController;

	public float nwMileage { get; set; }

	public float mileageMoreZero
	{
		get
		{
			return Mathf.Max(nwMileage, 0f);
		}
		set
		{
			nwMileage = Mathf.Max(value, 0f);
		}
	}

	public event Action<RaceCar> OnCarVisualLoadStartEvent;

	public event Action<RaceCar> OnCarVisualLoadFinishEvent;

	public event Action<Collision> OnCollisionEnterEvent;

	public event Action<Collision> OnCollisionExitEvent;

	public void ResetToSpawnPointOnUgcMap()
	{
		if (!(ugcMapSpawnPoint == null))
		{
			m_carPlacementModel.ResetToPoint(ugcMapSpawnPoint.position, ugcMapSpawnPoint.rotation);
		}
	}

	public bool TryResetToWaypoint()
	{
		return m_carPlacementModel.TryResetToWaypoint();
	}

	public bool TryResetToPlayer(RaceCar raceCar)
	{
		return m_carPlacementModel.TryResetToPlayer(raceCar);
	}

	public void FlipCar()
	{
		m_carPlacementModel.FlipCar();
	}

	public void SetLogicUpdate(bool isActive)
	{
		m_isLogicUpdateEnabled = isActive;
	}

	public void SetCurrentTrack(Track track)
	{
		if (m_currentTrack != track)
		{
			m_currentTrack = track;
			m_positionOnSpline.track = m_currentTrack;
		}
	}

	public void HoldGaragePosition(bool hold)
	{
		m_carPlacementModel.HoldGaragePosition(hold);
	}

	public void EnableDriverModel(bool enable)
	{
		m_enableDriverModel = enable;
		if (m_driverAnimController != null)
		{
			m_driverInstance.SetActive(m_enableDriverModel);
			m_driverAnimController.enabled = m_enableDriverModel;
		}
	}

	public void EnableCarModel(bool enable)
	{
		if (carModel != null)
		{
			carLightController.UseSourceParent(enable);
			carModel.transform.gameObject.SetActive(enable);
		}
	}

	public void EnableCarXPhysicsForNetworkCar(bool enable)
	{
		if (base.carX != null && isNetworkCar)
		{
			base.carX.SetCarActive(enable);
		}
	}

	public void EnableCarXMultiRaycast(bool enable, int integrationSteps = -1)
	{
		if (!(base.carX == null))
		{
			base.carX.integrationSteps = ((enable && integrationSteps > 0) ? integrationSteps : 16);
			base.carX.useMultiRaycast = enable;
		}
	}

	public float? CalcLapTime()
	{
		return m_raceCalc.GetLapTime(base.carX.speedMPS);
	}

	public int? CalcLapDriftPoints()
	{
		return m_raceCalc.GetLapDriftPoints();
	}

	private void UpdateFilteredSteerAngle(float dt)
	{
		float steer = base.carX.GetSteer();
		if (!m_filteredSteerAngle.HasValue)
		{
			m_filteredSteerAngle = steer;
		}
		float num = Mathf.Max(2f, 100f * dt);
		m_filteredSteerAngle += (steer - m_filteredSteerAngle) / num;
		m_filteredSteerAngle = Mathf.Clamp(m_filteredSteerAngle.Value, -1000000f, 1000000f);
	}

	private void UpdateSteerSmoothDamp(float dt)
	{
		float steer = base.carX.steer;
		if (!m_steerSmoothDamp.HasValue)
		{
			m_steerSmoothDamp = steer;
		}
		else
		{
			m_steerSmoothDamp = Mathf.SmoothDamp(m_steerSmoothDamp.Value, steer, ref m_steerVelSmoothDamp, 0.1f, float.PositiveInfinity, dt);
		}
	}

	public Transform[] GetRootObject(string typeId, string item)
	{
		carModel.skeleton.GetRoots(typeId, out var rootList);
		return rootList[0].FindAllComponent((Transform trans) => trans.name == item);
	}

	public float GetSteerSmoothDamp()
	{
		GameInput.InputManager instance = GameInput.InputManager.instance;
		if ((!(PlayerCarControl.instance != null) || !PlayerCarControl.instance.blockControl) && instance.CurSteeringDeviceType == GameInput.DeviceType.Wheel && !isNetworkCar)
		{
			return 0f - instance.GetActionValue(ActionType.Turn);
		}
		if (!m_steerSmoothDamp.HasValue)
		{
			return base.carX.steer;
		}
		return m_steerSmoothDamp.Value;
	}

	public void SetCheckPointPosition(Vector3 position, Vector3 direction)
	{
		m_checkPointPosition = position;
		m_checkPointDirection = direction;
	}

	public void GetWallCheckPointPosition(out Vector3 rearLeft, out Vector3 rearRight)
	{
		Vector3 extents = GetDesc().carXDesc.weight.bounds.extents;
		float num = extents.x * m_rearOffset.x;
		float num2 = extents.z * m_rearOffset.z;
		rearLeft = new Vector3(0f - num, m_rearOffset.y, 0f - num2);
		rearRight = new Vector3(num, m_rearOffset.y, 0f - num2);
	}

	public void CreateWallCheckPoints(Transform parent, out GameObject rearLeftPoint, out GameObject rearRightPoint)
	{
		GetWallCheckPointPosition(out var rearLeft, out var rearRight);
		rearLeftPoint = CreatWallCheckPoint("RearLeftPoint", parent, rearLeft);
		rearRightPoint = CreatWallCheckPoint("RearRightPoint", parent, rearRight);
	}

	private GameObject CreatWallCheckPoint(string name, Transform parent, Vector3 position)
	{
		GameObject obj = new GameObject(name);
		obj.transform.SetParent(parent, worldPositionStays: false);
		obj.transform.localPosition = position;
		return obj;
	}

	private void UpdateFlipped(float dt)
	{
		if (flipped)
		{
			if (m_lastFlipped)
			{
				m_autoFlipPeriodTimer.Update(dt);
			}
			else
			{
				m_autoFlipPeriodTimer.Reset();
			}
		}
		m_lastFlipped = flipped;
	}

	public float GetSmoothSteer()
	{
		GameInput.InputManager instance = GameInput.InputManager.instance;
		if ((!(PlayerCarControl.instance != null) || !PlayerCarControl.instance.blockControl) && instance.CurSteeringDeviceType == GameInput.DeviceType.Wheel && !isNetworkCar)
		{
			return 0f - instance.GetActionValue(ActionType.Turn);
		}
		return m_filteredSteerAngle ?? base.carX.GetSteer();
	}

	public void ClearSmoke()
	{
		if (m_wheelSmokes == null)
		{
			return;
		}
		BaseWheelSmoke[] wheelSmokes = m_wheelSmokes;
		foreach (BaseWheelSmoke baseWheelSmoke in wheelSmokes)
		{
			if (baseWheelSmoke != null)
			{
				baseWheelSmoke.ClearSmoke();
			}
		}
	}

	public void SetHeadlightsEnabled(bool lightsEnabled, bool ignoreIgnition = false, bool force = false)
	{
		if (force || headlightsEnabled != lightsEnabled)
		{
			headlightsEnabled = lightsEnabled;
			m_lightSwitch = headlightsEnabled;
			if (headlightsEnabled)
			{
				carLightController.ChangeToNight(ignoreIgnition);
			}
			else
			{
				carLightController.ChangeToDay();
			}
		}
	}

	public void OnSkidmarkChangeHandler()
	{
		GraphicSettings graphics = m_prefs.graphics;
		SetSkidmarkLengthScale(graphics.skidmarkPower[graphics.skidmarkLength]);
	}

	private void SetSkidmarkLengthScale(int power)
	{
		InitSkidMarksIfNeed();
		if (m_skidMarks == null)
		{
			return;
		}
		CARXWheelSkidmark[] skidMarks = m_skidMarks;
		foreach (CARXWheelSkidmark cARXWheelSkidmark in skidMarks)
		{
			if (cARXWheelSkidmark == null)
			{
				continue;
			}
			if (cARXWheelSkidmark.MarkMax != 128 * power && cARXWheelSkidmark.enabled)
			{
				cARXWheelSkidmark.enabled = false;
			}
			if (power != 0)
			{
				cARXWheelSkidmark.MarkMax = 128 * power;
				if (!cARXWheelSkidmark.enabled)
				{
					cARXWheelSkidmark.enabled = true;
				}
			}
		}
	}

	public void ClearSkidmark()
	{
		InitSkidMarksIfNeed();
		CARXWheelSkidmark[] skidMarks = m_skidMarks;
		foreach (CARXWheelSkidmark obj in skidMarks)
		{
			obj.enabled = false;
			obj.enabled = true;
		}
	}

	private void InitSkidMarksIfNeed()
	{
		if (m_skidMarks == null)
		{
			m_skidMarks = base.gameObject.GetComponentsInChildren<CARXWheelSkidmark>();
		}
	}

	public bool CheckAutoReverse(ref float accel, ref float brake, ref int gear)
	{
		int num = gear;
		if (Mathf.Abs(Vector3.Dot(base.carX.velocity, base.getTransform.forward)) < 0.1f)
		{
			int num2 = 0;
			float num3 = 0f;
			for (int i = 0; i < base.activeWheels.Length; i++)
			{
				Wheel wheel = base.carX.GetWheel(base.activeWheels[i]);
				num2++;
				num3 += wheel.w;
			}
			num3 = ((num2 <= 0) ? 0f : (num3 / (float)num2));
			if (Mathf.Abs(num3) < 5f)
			{
				if (brake > 0.5f && accel < 0.1f)
				{
					gear = -1;
				}
				else if (accel > 0.5f)
				{
					gear = 1;
				}
			}
		}
		if (gear == -1)
		{
			float num4 = brake;
			float num5 = accel;
			accel = num4;
			brake = num5;
		}
		return num != gear;
	}

	private void OnIsGhostChanged()
	{
		m_autoFlipPeriodTimer = ((m_isGhost && !m_inGarage) ? null : new PeriodTimer(m_autoFlipPeriod, PlaceOnTrackIfFlipped));
	}

	public void ResetBrokenPhysicsInGarage()
	{
		DependencyInjector.Resolve<PlayerCarFactory>().ResetGaragePositionAndProfile(this);
		ResetPhysicsAfterBroke();
	}

	public void ResetVelocity()
	{
		if (base.getRigidbody != null)
		{
			base.getRigidbody.velocity = Vector3.zero;
			base.getRigidbody.angularVelocity = Vector3.zero;
		}
	}

	public void PlaceOnTrack()
	{
		if (m_placeOnTrackCoroutine != null)
		{
			StopCoroutine(m_placeOnTrackCoroutine);
		}
		m_placeOnTrackCoroutine = StartCoroutine((m_currentTrack == null) ? PlaceOnRoad() : PlaceOnTrackCoroutine());
	}

	public void PlaceOnTrackIfFlipped()
	{
		if (m_placeOnTrackIfFlippedCoroutine != null)
		{
			StopCoroutine(m_placeOnTrackIfFlippedCoroutine);
		}
		m_placeOnTrackIfFlippedCoroutine = StartCoroutine(PlaceOnTrackIfFlippedCoroutine());
	}

	private IEnumerator PlaceOnTrackIfFlippedCoroutine()
	{
		if (flipped)
		{
			yield return PlaceOnTrackCoroutine();
		}
	}

	private IEnumerator PlaceOnRoad()
	{
		if (!base.carX.enabled)
		{
			yield break;
		}
		bool flag = false;
		if (!(m_positionOnSpline.track == null) && m_positionOnSpline.track.centerLine != null)
		{
			yield break;
		}
		if (Time.time - m_resetOnSplineDoubleClickTimer > 5f)
		{
			m_showReturnCarHelpMessageCoroutine = TimeController.StartNow(ShowReturnCarHelpMessage());
			m_resetOnSplineDoubleClickTimer = Time.time;
			FlipCar();
			flag = true;
		}
		if (!flag)
		{
			if (m_showReturnCarHelpMessageCoroutine != null)
			{
				TimeController.StopNow(m_showReturnCarHelpMessageCoroutine);
				m_showReturnCarHelpMessageCoroutine = null;
			}
			ResetToSpawnPointOnUgcMap();
		}
	}

	private IEnumerator PlaceOnTrackCoroutine()
	{
		if (!base.carX.enabled || m_positionOnSpline.track == null)
		{
			yield break;
		}
		bool needSetLastOnRoad = false;
		CarDesc desc = null;
		base.carX.GetCarDesc(ref desc);
		int layerMask = 16;
		layerMask |= 0x800;
		layerMask |= 0x40000;
		layerMask |= 0x80000;
		layerMask |= 0x100000;
		layerMask |= 0x200000;
		layerMask |= 0x400000;
		Vector3 splinePosition = base.getTransform.position;
		Vector3 direction = base.getTransform.forward;
		Vector3 upShift = new Vector3(0f, desc.weight.bounds.center.y, 0f);
		bool collision = true;
		float bias = 0f;
		bool networkState = States.mainCurrent is SyncNetFreerideRaceModeState;
		bool freerideState = States.mainCurrent is FreerideRaceModeState;
		bool respawnOnSplinePrefs = m_prefs.globals.respawnCarOnlyOnSpline;
		while (collision)
		{
			m_positionOnSpline.GetNearestCenterPosition(splinePosition, out splinePosition, out direction);
			if (networkState || freerideState)
			{
				float magnitude = (m_lastOnRoadPosition - splinePosition).magnitude;
				if (NetworkController.InstanceGame?.LocalPlayer?.attachedRoom == null)
				{
					if (!respawnOnSplinePrefs && !m_positionOnSpline.isInsideTrack && magnitude > 50f && Time.time - m_resetOnSplineDoubleClickTimer > 5f)
					{
						m_showReturnCarHelpMessageCoroutine = TimeController.StartNow(ShowReturnCarHelpMessage());
						m_resetOnSplineDoubleClickTimer = Time.time;
						needSetLastOnRoad = true;
					}
					if (!needSetLastOnRoad)
					{
						if (m_showReturnCarHelpMessageCoroutine != null)
						{
							TimeController.StopNow(m_showReturnCarHelpMessageCoroutine);
							m_showReturnCarHelpMessageCoroutine = null;
						}
						direction *= (float)(m_lastOnSplineRoadDirectionForward ? 1 : (-1));
					}
				}
			}
			ResetRecordLastPosInfo();
			splinePosition += Vector3.up * bias;
			collision = Physics.CheckBox(splinePosition + upShift, desc.weight.bounds.extents, Quaternion.LookRotation(direction), layerMask);
			bias += 1f;
			yield return null;
		}
		RecordLastOnRoadData();
		base.getTransform.position = (needSetLastOnRoad ? m_lastOnRoadPosition : splinePosition);
		base.getTransform.rotation = (needSetLastOnRoad ? m_lastOnRoadRotation : Quaternion.LookRotation(direction));
		AlignCarPositionAtSpawn();
		Rigidbody rigidbody = base.carX.getRigidbody;
		rigidbody.velocity = Vector3.zero;
		rigidbody.angularVelocity = Vector3.zero;
		base.carX.gear = 0;
		base.carX.ResetTransmission();
		m_positionOnSpline.SetDirty();
		if (!isNetworkCar)
		{
			NetworkController.InstanceGame.OnLocalPlayerPlacedOnTrack();
		}
	}

	public bool TryGetDiscDiamCoeff(out float discDiam, bool checkHeight = true)
	{
		return m_carPlacementModel.TryGetDiscDiamCoeff(out discDiam, checkHeight);
	}

	public void AlignCarPositionAtSpawn(bool adjustRotation = true, bool rotateToForward = false)
	{
		m_carPlacementModel.AlignCarPositionAtSpawn(adjustRotation, rotateToForward);
	}

	public void ReturnCarToCheckPoint()
	{
		TimeController.AddCallback(0f, ReturnCarToLastCheckPoint);
	}

	private void ReturnCarToLastCheckPoint()
	{
		bool num = States.mainCurrent is FreerideRaceModeState;
		bool flag = NetworkController.InstanceGame?.LocalPlayer?.attachedRoom != null;
		if (!num || flag)
		{
			base.getTransform.position = m_checkPointPosition;
			base.getTransform.rotation = Quaternion.LookRotation(m_checkPointDirection);
			Rigidbody rigidbody = base.carX.getRigidbody;
			rigidbody.velocity = Vector3.zero;
			rigidbody.angularVelocity = Vector3.zero;
			base.carX.gear = 1;
			base.carX.ResetTransmission();
			missCheckPoint = false;
			ShowWrongCheckPointMessage();
			m_positionOnSpline.SetDirty();
			if (!isNetworkCar)
			{
				NetworkController.InstanceGame.OnLocalPlayerPlacedOnTrack();
			}
			if (NetworkController.InstanceGame?.LocalPlayer?.attachedRoom != null)
			{
				SetRespawnRuleForCompetition(SubroomMode.Tandem, isAllowToSpawn: false);
			}
		}
	}

	private static string GetOutputActionString(string elementName)
	{
		return "<sprite name=\"" + elementName + "\"/>";
	}

	public void ShowWrongCheckPointMessage()
	{
		if (missCheckPoint)
		{
			string text = string.Empty;
			GameInput.InputManager instance = GameInput.InputManager.instance;
			BindElement respawnBind = instance.GetActionBindOfLastInput(ActionType.RespawnCar, allowFallbackToKeyboard: true);
			if (respawnBind != null)
			{
				if (respawnBind.deviceInfo.rewiredType != ControllerType.Keyboard)
				{
					if (Array.FindIndex(m_gamepadButtonsArray, (string button) => button == respawnBind.elementName) != -1)
					{
						text = GetOutputActionString(respawnBind.elementName);
					}
				}
				else
				{
					text = respawnBind.elementName;
				}
			}
			if (string.IsNullOrEmpty(text))
			{
				text = LocalizationKeys.input_action_.AddLocalized(ActionType.RespawnCar.ToString().ToLower());
			}
			GUICommonGodVoice.ShowText(LocalizationKeys.wrong_checkpoint_missed.Localized() + ". " + LocalizationKeys.return_car_to_last_checkpoint.LocalizedFormat(text));
			m_lastMissCheckPointHash = GUICommonGodVoice.CurrentLabelHash;
		}
		else
		{
			GUICommonGodVoice.HideTextWithHash(m_lastMissCheckPointHash);
		}
		if (NetworkController.InstanceGame?.LocalPlayer?.attachedRoom != null)
		{
			SetRespawnRuleForCompetition(SubroomMode.Tandem, missCheckPoint);
		}
	}

	private void SetRespawnRuleForCompetition(SubroomMode targetSubroomMode, bool isAllowToSpawn)
	{
		if (NetworkController.InstanceGame?.LocalPlayer?.attachedRoom?.subroomMode == targetSubroomMode && PlayerCarControl.instance != null)
		{
			PlayerCarControl.instance.SetCanSpawnCarInMP(isAllowToSpawn);
		}
	}

	private IEnumerator ShowReturnCarHelpMessage()
	{
		yield return new WaitForSeconds(0.5f);
		GameInput.InputManager instance = GameInput.InputManager.instance;
		BindElement respawnBind = instance.GetActionBindOfLastInput(ActionType.RespawnCar, allowFallbackToKeyboard: true);
		if (respawnBind == null || States.mainCurrent is MultiplayerPCState)
		{
			yield break;
		}
		if (respawnBind.deviceInfo.rewiredType != ControllerType.Keyboard)
		{
			if (Array.FindIndex(m_gamepadButtonsArray, (string button) => button == respawnBind.elementName) != -1)
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.reset_car_help_message.LocalizedFormat(GetOutputActionString(respawnBind.elementName)), 3f);
			}
			else
			{
				string text = LocalizationKeys.input_action_.AddLocalized(ActionType.RespawnCar.ToString().ToLower()).ToLower();
				GUICommonGodVoice.ShowText(LocalizationKeys.reset_car_help_message.LocalizedFormat(text), 3f);
			}
		}
		else
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.reset_car_help_message.LocalizedFormat(respawnBind.elementName), 3f);
		}
	}

	public Vector3 GetDirectionForMinimap()
	{
		Vector3 velocity = base.carX.velocity;
		if (!(velocity.sqrMagnitude > 0.1f))
		{
			return base.getTransform.forward;
		}
		return velocity.normalized;
	}

	public float GetETAToDistance(float targetDistance)
	{
		float num = positionOnSpline.distanceAlongCenterLine + base.lengthFront;
		float forwardDistance = positionOnSpline.GetForwardDistance(num, targetDistance);
		float speedMPS = base.carX.speedMPS;
		float longAccel = base.carX.longAccel;
		if (Mathf.Approximately(longAccel, 0f))
		{
			return forwardDistance / speedMPS;
		}
		float num2 = speedMPS * speedMPS + 2f * forwardDistance * longAccel;
		if (num2 < 0f)
		{
			GameLog.Print(AlertLevel.Error, "RaceCar", $"Negative value to root: current distance {num}, target distance {targetDistance}, S {forwardDistance}, V {speedMPS}, a {longAccel}");
			return float.MaxValue;
		}
		return (0f - speedMPS + Mathf.Sqrt(num2)) / longAccel;
	}

	public bool NeedBrake(out float curVel, out float maxVel)
	{
		Vector3 delta;
		float relativeDistance = this.CalcPrediction(0.65f, out delta);
		float targetDistance = m_positionOnSpline.GetTargetDistance(relativeDistance);
		curVel = base.carX.speedMPS;
		maxVel = m_raceCalc.GetMaxSpeedAtDistance(targetDistance);
		float num = 1f;
		float num2 = 0f;
		float speedMPS = base.carX.speedMPS;
		float speedKMH = base.carX.speedKMH;
		if (speedMPS > 1f)
		{
			Vector3 forward = base.getTransform.forward;
			Vector3 velocity = base.carX.velocity;
			float num3 = 0f;
			velocity *= 1f / speedMPS;
			num3 = Vector3.Dot(velocity, forward);
			float f = Mathf.Atan2(Vector3.Dot(Vector3.up, Vector3.Cross(forward, velocity)), num3) * 57.29578f;
			float num4 = Mathf.Clamp01((speedKMH - 30f) / 90f);
			float num5 = 20f * (1f - num4) + 10f * num4;
			bool flag = Mathf.Abs(f) > num5 && num3 > 0f;
			if (Mathf.Abs(base.carX.GetSteer()) > 0.9f && base.carX.accelerate > 0.9f)
			{
				flag = true;
			}
			if (flag)
			{
				num = 1.2f;
				num2 = 3f;
			}
		}
		maxVel *= num;
		maxVel += num2;
		return curVel > maxVel;
	}

	public void ApplyAccelAndBrakeAssistent(ref float accel, ref float brake, float accel_factor, float brake_factor)
	{
		float curVel;
		float maxVel;
		bool num = NeedBrake(out curVel, out maxVel);
		float num2 = 1f;
		float num3 = 0f;
		if (num)
		{
			num3 = (curVel - maxVel) * 0.1f;
			if (num3 > 1f)
			{
				num3 = 1f;
			}
			if (num3 < 0f)
			{
				num3 = 0f;
			}
			num2 = 0f;
		}
		else if (curVel < maxVel)
		{
			num2 = (maxVel - curVel) * 0.3f;
			if (num2 > 1f)
			{
				num2 = 1f;
			}
			if (num2 < 0f)
			{
				num2 = 0f;
			}
		}
		brake = Mathf.Max(num3, brake) * brake_factor + brake * (1f - brake_factor);
		accel = Mathf.Min(num2, accel) * accel_factor + accel * (1f - accel_factor);
	}

	public IEnumerator InstantiateAndInitDriverCoroutine()
	{
		if (!m_driverInstance)
		{
			if (!m_emptyDriverParent)
			{
				m_emptyDriverParent = new GameObject("DriverEmptyParent");
			}
			m_emptyDriverParent.SetActive(value: false);
			Transform transform = m_emptyDriverParent.transform;
			transform.SetParent(base.transform);
			m_driverInstance = UnityEngine.Object.Instantiate(CarResourceProvider.instance.driverPrefab, transform);
			m_driverTransform = m_driverInstance.transform;
			yield return new WaitUntil(() => IsVisualLoaded);
			m_driverTransform.SetParent(base.transform, worldPositionStays: false);
			m_driverAnimController = m_driverInstance.GetExistingComponent<DriverAnimController>();
			driverCamSwitchHandler = m_driverTransform.GetComponent<CarCameraSwitchHandler>();
			if (TryInitializeIK())
			{
				SetupSteerAnimationMaxAngleHandler();
			}
		}
	}

	public void SpawnNickNameUIPrefab(string nameParam)
	{
		GUICommonNickNames.SpawnNickName(this, nameParam);
		GUICommonNickNames.SetVisibleNickName(this, m_prefs.globals.showNickNames);
		MinimapProvider.instance.OnCreateNicknameHandlerInvoke(this, nameParam);
	}

	public void UpdateNickName(string nameParam)
	{
		GUICommonNickNames.UpdateNickName(this, nameParam);
		MinimapProvider.instance.OnUpdateNicknameHandlerInvoke(this, nameParam);
	}

	public void SetVisibleUIName(bool show)
	{
		GUICommonNickNames.SetVisibleNickName(this, show);
	}

	public void UpdateDriverVisibility()
	{
		if (!isNetworkCar)
		{
			bool flag = !m_prefs.globals.hideWheel;
			bool driverVisible = flag && !m_prefs.globals.hideDriver && !GameManager.isInGarage;
			SetDriverSystemVisibility(flag, driverVisible);
		}
	}

	public void SetDriverSystemVisibility(bool steeringWheelVisible = true, bool driverVisible = false)
	{
		if (m_driverTransform != null)
		{
			SkinnedMeshRenderer[] componentsInChildren = m_driverTransform.GetComponentsInChildren<SkinnedMeshRenderer>();
			if (componentsInChildren != null && componentsInChildren.Length != 0)
			{
				SkinnedMeshRenderer[] array = componentsInChildren;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].enabled = driverVisible;
				}
			}
		}
		if (m_steeringWheelObject == null)
		{
			m_steeringWheelObject = GetComponentInChildren<DriverSteeringWheel>(includeInactive: true);
		}
		if (m_steeringWheelObject != null)
		{
			m_steeringWheelObject.gameObject.SetActive(steeringWheelVisible);
		}
		TryInitializeIK();
	}

	public void SetWindowsVisability(bool windowsHide)
	{
		windowsHide = !windowsHide || !m_prefs.globals.hideWindows;
		if (m_windowsRoots != null && m_windowsRoots.Count > 0)
		{
			foreach (Transform windowsRoot in m_windowsRoots)
			{
				if (windowsRoot != null && windowsRoot.gameObject != null)
				{
					windowsRoot.gameObject.SetActive(windowsHide);
				}
			}
		}
		UpdateSayakaWindowStickers(windowsHide);
	}

	public void UpdateWindowsVisability()
	{
		if (!isNetworkCar)
		{
			SetWindowsVisability((bool)CameraSwitch.instance && !CameraSwitch.instance.CheckForOutsideView(base.carX));
		}
	}

	private void UpdateSayakaWindowStickers(bool enable)
	{
		if (!m_carModel.skeleton.GetRoots("chassis", out var rootList) || rootList == null)
		{
			return;
		}
		foreach (Transform item in rootList)
		{
			if (item == null)
			{
				continue;
			}
			MeshRenderer[] componentsInChildren = item.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
			if (componentsInChildren == null)
			{
				continue;
			}
			MeshRenderer[] array = componentsInChildren;
			foreach (MeshRenderer meshRenderer in array)
			{
				UnityEngine.Material[] materials = meshRenderer.materials;
				if (materials == null)
				{
					continue;
				}
				for (int j = 0; j < materials.Length; j++)
				{
					if (materials[j].name.Contains("Sayaka_decals"))
					{
						meshRenderer.gameObject.SetActive(enable);
					}
				}
			}
		}
	}

	private void InitWindowsRoots()
	{
		if (m_windowsRoots == null)
		{
			m_windowsRoots = new List<Transform>();
		}
		m_windowsRoots.Clear();
		string[] array = new string[4] { "chassis", "doors", "roof", "trunk" };
		foreach (string text in array)
		{
			m_carModel.skeleton.GetRoots(text, out var rootList);
			if (rootList == null)
			{
				continue;
			}
			for (int j = 0; j < rootList.Count; j++)
			{
				MeshRenderer[] componentsInChildren = rootList[j].GetComponentsInChildren<MeshRenderer>(includeInactive: true);
				if (componentsInChildren == null)
				{
					continue;
				}
				foreach (MeshRenderer meshRenderer in componentsInChildren)
				{
					if (!(meshRenderer == null) && !(meshRenderer.gameObject == null))
					{
						Transform item = meshRenderer.transform;
						if (meshRenderer.gameObject.name.Contains("window") && !m_windowsRoots.Contains(item))
						{
							m_windowsRoots.Add(item);
						}
					}
				}
			}
		}
	}

	private void SetupWheelSmokes()
	{
		if (m_wheelSmokes == null || m_wheelSmokes.HasAny((BaseWheelSmoke smoke) => smoke == null))
		{
			m_wheelSmokes = base.gameObject.GetComponentsInChildren<BaseWheelSmoke>();
		}
	}

	public void SetSmokeColor(Color color, Action<BaseWheelSmoke> foreachSmokeDo = null)
	{
		SetupWheelSmokes();
		BaseWheelSmoke[] wheelSmokes = m_wheelSmokes;
		for (int i = 0; i < wheelSmokes.Length; i++)
		{
			wheelSmokes[i].SetSmokeColor(color, foreachSmokeDo);
		}
	}

	private void SetNetworkQuality(float value)
	{
		if (m_networkQuality == value)
		{
			return;
		}
		bool flag = value > 0.6f;
		base.carX.simulationMode = ((!flag) ? CarX.SimulationMode.Simple : CarX.SimulationMode.Advanced);
		SetShadowVisibility(flag);
		if (m_deformWheels != null)
		{
			CARXWheelMorphing[] deformWheels = m_deformWheels;
			for (int i = 0; i < deformWheels.Length; i++)
			{
				deformWheels[i].deformation = flag;
			}
		}
		m_networkQuality = value;
	}

	public void Init(PlayerCar metaInfo, int layer, bool isLocalCar, bool inGarage)
	{
		m_inGarage = inGarage;
		m_metaInfo = metaInfo;
		base.gameObject.SetActive(value: true);
		if (m_carModel != null)
		{
			base.carX.OnApplyCarDescEvent -= OnOnApplyCarDescEvent;
			m_carModel.Dispose();
			m_carModel = null;
		}
		m_carModel = ((m_metaInfo != null) ? new CarModel(base.name.Replace("(Clone)", "").ToLower(), base.getTransform, isLocalCar, !inGarage, layer) : null);
		base.carX.OnApplyCarDescEvent += OnOnApplyCarDescEvent;
		m_startLoadingVisualTime = Time.realtimeSinceStartup;
	}

	private void OnUpdateWheelsEvent()
	{
		if (m_isLogicUpdateEnabled && m_carModel != null && !(base.carX == null))
		{
			Wheel wheel = base.carX.GetWheel(WheelIndex.FrontLeft);
			Wheel wheel2 = base.carX.GetWheel(WheelIndex.FrontRight);
			Wheel wheel3 = base.carX.GetWheel(WheelIndex.RearLeft);
			Wheel wheel4 = base.carX.GetWheel(WheelIndex.RearRight);
			bool deform = wheel.GetDeform(out var patch_cnt, out var lat_deform_vec, out var long_deform_vec, out var vert_deform_plane);
			m_carModel.SetupTireDeformation(CarWheelID.FrontLeft, deform, patch_cnt, lat_deform_vec, long_deform_vec, vert_deform_plane);
			deform = wheel2.GetDeform(out patch_cnt, out lat_deform_vec, out long_deform_vec, out vert_deform_plane);
			m_carModel.SetupTireDeformation(CarWheelID.FrontRight, deform, patch_cnt, lat_deform_vec, long_deform_vec, vert_deform_plane);
			deform = wheel3.GetDeform(out patch_cnt, out lat_deform_vec, out long_deform_vec, out vert_deform_plane);
			m_carModel.SetupTireDeformation(CarWheelID.RearLeft, deform, patch_cnt, lat_deform_vec, long_deform_vec, vert_deform_plane);
			deform = wheel4.GetDeform(out patch_cnt, out lat_deform_vec, out long_deform_vec, out vert_deform_plane);
			m_carModel.SetupTireDeformation(CarWheelID.RearRight, deform, patch_cnt, lat_deform_vec, long_deform_vec, vert_deform_plane);
			float fixedUnscaledDeltaTime = Time.fixedUnscaledDeltaTime;
			m_carModel.SetupAngularVelocityAngle(CarWheelID.FrontLeft, wheel.w, fixedUnscaledDeltaTime);
			m_carModel.SetupAngularVelocityAngle(CarWheelID.FrontRight, wheel2.w, fixedUnscaledDeltaTime);
			m_carModel.SetupAngularVelocityAngle(CarWheelID.RearLeft, wheel3.w, fixedUnscaledDeltaTime);
			m_carModel.SetupAngularVelocityAngle(CarWheelID.RearRight, wheel4.w, fixedUnscaledDeltaTime);
		}
	}

	private void OnOnApplyCarDescEvent(Car car)
	{
		m_carModel.ApplyWheelDesc(CarWheelID.FrontWheels, car.frontTyreDiscDiam, car.frontTyreWidth, car.frontTyreProfile, car.frontTyreStretch);
		m_carModel.ApplyWheelDesc(CarWheelID.RearWheels, car.rearTyreDiscDiam, car.rearTyreWidth, car.rearTyreProfile, car.rearTyreStretch);
	}

	private void OnIgnitionChanged(bool ignition)
	{
		if (States.mainCurrent is GarageGUIState)
		{
			return;
		}
		bool flag = ((isLocalPlayer || m_prefs.globals.syncHeadlights) ? (ignition && m_lightSwitch) : (DayNightController.instance != null && ignition && !DayNightController.instance.IsDayLight));
		if (flares != null)
		{
			bool flag2 = ignition && (isLocalPlayer || m_prefs.globals.syncHeadlights);
			flares.SetEnabled(flag2);
		}
		if (headlightsEnabled != flag)
		{
			if (flag)
			{
				carLightController.ChangeToNight();
			}
			else
			{
				carLightController.ChangeToDay();
			}
		}
	}

	public void ResetSteer()
	{
		TimeController.StartNow(ResetSteerCoroutine());
	}

	private IEnumerator ResetSteerCoroutine()
	{
		m_currentSteer = base.carX.GetSteer();
		while (!Mathf.Approximately(m_currentSteer, 0f) && !(base.carX == null))
		{
			float currentVelocity = 0f;
			m_currentSteer = Mathf.SmoothDamp(base.carX.GetSteer(), 0f, ref currentVelocity, 0.05f);
			base.carX.SetSteer(m_currentSteer, useAssistent: false, Time.deltaTime);
			yield return null;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		m_graphicsQuality = DependencyInjector.Resolve<GraphicsQuality>();
		m_prefs = DependencyInjector.Resolve<GamePrefs>();
		m_positionOnSpline = base.gameObject.GetOrAddComponent<PositionOnSpline>();
		m_carLightController = base.gameObject.GetOrAddComponent<CarLightController>();
		m_groundDetector = new GroundDetector(this);
		m_carPlacementModel = new CarPlacementModel(this);
		if (States.mainCurrent is BaseRaceState)
		{
			base.gameObject.GetOrAddComponent<MinimapItem>();
		}
		OnIsGhostChanged();
		MinimapItem component = GetComponent<MinimapItem>();
		if ((bool)component)
		{
			component.host = this;
		}
		OnSkidmarkChangeHandler();
		m_engineController.Init(this);
		CarEngineController carEngineController = m_engineController;
		carEngineController.onIgnitionChanged = (Action<bool>)Delegate.Combine(carEngineController.onIgnitionChanged, new Action<bool>(OnIgnitionChanged));
		GraphicsQuality graphicsQuality = m_graphicsQuality;
		graphicsQuality.onSkidmarkChange = (System.Action)Delegate.Combine(graphicsQuality.onSkidmarkChange, new System.Action(OnSkidmarkChangeHandler));
	}

	private void Start()
	{
		flares = GetComponentInChildren<CarFlares>(includeInactive: true);
		SetupWheelSmokes();
		UpdateDriverVisibility();
		ResetRecordLastPosInfo();
		InitHeadLightState();
	}

	private void InitHeadLightState()
	{
		DayNightController instance = DayNightController.instance;
		SetHeadlightsEnabled(instance != null && !instance.IsDayLight);
	}

	public void SetShadowVisibility(bool value)
	{
		if (!(shadowProjectorGO == null))
		{
			shadowProjectorGO.SetActive(value);
		}
	}

	private IEnumerator WaitOnGround(float time, float timeMax)
	{
		while (Time.time - time < timeMax)
		{
			yield return m_fixedWait;
		}
		if (!m_groundDetector.onGround)
		{
			ResetToSpawnPointOnUgcMap();
		}
		m_waitSpawnCoroutine = null;
	}

	private IEnumerator WaitOnFlip(float time, float timeMax)
	{
		while (Time.time - time < timeMax)
		{
			yield return m_fixedWait;
		}
		if (!m_groundDetector.onGround && m_groundDetector.flipped)
		{
			FlipCar();
		}
		m_waitFlipCoroutine = null;
	}

	private void FixedUpdate()
	{
		if (!m_isLogicUpdateEnabled)
		{
			return;
		}
		if (!m_isGhost)
		{
			m_groundDetector.FixedUpdate(Time.fixedDeltaTime);
			if (ugcMapSpawnPoint != null && m_groundDetector.flipped && !m_groundDetector.onGround)
			{
				if (m_waitSpawnCoroutine == null)
				{
					m_waitSpawnCoroutine = TimeController.StartNow(WaitOnGround(Time.time, 1f));
				}
				if (m_waitFlipCoroutine == null)
				{
					m_waitFlipCoroutine = TimeController.StartNow(WaitOnFlip(Time.time, 1f));
				}
			}
			m_currTimeRefreshSpawn += Time.fixedDeltaTime;
			if (m_currTimeRefreshSpawn >= 5f)
			{
				if ((bool)ugcMapSpawnPoint && wheelsCountOnTrack == 4)
				{
					RefreshSpawnPointOnUGCMaps();
				}
				m_currTimeRefreshSpawn %= 5f;
			}
		}
		m_engineController.FixedUpdate();
	}

	private void RefreshSpawnPointOnUGCMaps()
	{
		if (!(base.getRigidbody.velocity.sqrMagnitude <= 0.5f))
		{
			ugcMapSpawnPoint.SetPositionAndRotation(base.carX.getTransform.position, Quaternion.LookRotation(base.getRigidbody.velocity));
		}
	}

	public void CreateWaypoint()
	{
		PlayerCarControl.instance.SetWaypointPosRot(base.carX.getTransform.position, base.carX.getTransform.rotation);
	}

	private void Update()
	{
		if ((IsVisualLoaded && !m_isLogicUpdateEnabled) || base.gameObject == null || base.carX == null || m_carModel == null)
		{
			return;
		}
		if (!IsVisualLoaded)
		{
			WaitLoading();
		}
		m_carModel.Update();
		if (IsVisualLoaded)
		{
			if (!base.carX.enabled)
			{
				CheckBrokenPhysics();
			}
			UpdateSteeringAndFlipped();
			if (m_driverAnimController != null && m_driverAnimController.isActiveAndEnabled)
			{
				m_driverAnimController.SetupSteer(GetSmoothSteer());
				m_driverAnimController.SetupHandbrake(base.carX.handbrake);
				m_driverAnimController.SetupGear(base.carX.gear);
			}
			m_carLightController.UpdateCarLightsStates();
		}
	}

	private void UpdateSteeringAndFlipped()
	{
		float deltaTime = Time.deltaTime;
		if (deltaTime > 0f && !IsPhysicsBroken())
		{
			UpdateFilteredSteerAngle(deltaTime);
			UpdateSteerSmoothDamp(deltaTime);
			if (!m_isGhost && !m_inGarage)
			{
				UpdateFlipped(deltaTime);
			}
			if (m_positionOnSpline.totalDistanceTraveled - m_lastOnRoadDistanceTraveled > 5f && wheelsCountOnGround == wheelsCountOnTrack && onGround)
			{
				ResetRecordLastPosInfo();
				RecordLastOnRoadData();
			}
		}
	}

	public void RecordLastOnRoadData()
	{
		if (wheelsCountOnGround == wheelsCountOnTrack && onGround)
		{
			RecordLastRoadTransform();
			m_lastOnSplineRoadDirectionForward = !wrongWay;
		}
	}

	public void RecordLastRoadTransform()
	{
		m_lastOnRoadPosition = base.getTransform.position;
		m_lastOnRoadRotation = base.getTransform.rotation;
	}

	private void ResetRecordLastPosInfo()
	{
		m_lastOnRoadDistanceTraveled = m_positionOnSpline.totalDistanceTraveled;
	}

	private void CheckBrokenPhysics()
	{
		if (isNetworkCar)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		GameManager.raceDisabledByPhysExplo_Time = Time.unscaledTime + 1f;
		if (GameManager.isInGarage)
		{
			GUICommonGodVoice.ShowText("FatalError: physic explo. Car setup reset is needed!");
		}
		else
		{
			GUICommonGodVoice.ShowText("FatalError: physic explo. Restart is needed!");
		}
	}

	public void GetGroundPosition(out Vector3 position, out Quaternion rotation)
	{
		position = base.getTransform.position;
		rotation = base.getTransform.rotation;
		if ((wheelsCountOnGround != wheelsCountOnTrack || !onGround) && !(m_lastOnRoadPosition == Vector3.zero) && !((m_lastOnRoadPosition - base.getTransform.position).magnitude > 50f))
		{
			position = m_lastOnRoadPosition;
			rotation = m_lastOnRoadRotation;
		}
	}

	public Bounds GetWorldBoundingBox(ref Transform itemTransform, string itemName)
	{
		if (!carModel.IsLoading())
		{
			Transform transform = carModel.transform;
			if (itemTransform == null)
			{
				itemTransform = base.transform.Find(itemName);
			}
			List<MeshRenderer> list = new List<MeshRenderer>();
			itemTransform.GetComponentsInChildren(list);
			if (list.Count > 0)
			{
				Quaternion rotation = transform.rotation;
				transform.rotation = Quaternion.identity;
				Bounds bounds = list[0].bounds;
				for (int i = 1; i < list.Count; i++)
				{
					bounds.Encapsulate(list[i].bounds);
				}
				transform.rotation = rotation;
				return bounds;
			}
		}
		return new Bounds(Vector3.zero, Vector3.zero);
	}

	public void StartLoadingVisual(int loadHash)
	{
		if (IsLoadingVisualInProgress)
		{
			Debug.LogError($"Start Loading Visual[{loadHash}] for {base.name} again while loading[{LastLoadVisualHash}] in progress!");
		}
		LastLoadVisualHash = loadHash;
		this.OnCarVisualLoadStartEvent.SafeCall(this);
		IsLoadingVisualInProgress = true;
		IsVisualLoaded = false;
	}

	public void EndLoadingVisual()
	{
		IsLoadingVisualInProgress = false;
	}

	public void ResetHandBrakeShiftAnimation()
	{
		if (m_driverAnimController != null)
		{
			m_driverAnimController.ResetHandbrake();
		}
	}

	private void WaitLoading()
	{
		if (!m_carModel.IsLoading() && !IsLoadingVisualInProgress)
		{
			_ = Time.realtimeSinceStartup;
			_ = m_startLoadingVisualTime;
			OnCarLoaded();
		}
	}

	private bool IsWaitResourceUnloading()
	{
		if (m_unloadAsyncOp != null)
		{
			if (m_unloadAsyncOp.isDone)
			{
				m_unloadAsyncOp = null;
				return false;
			}
			return true;
		}
		if (IsNeedUnloadUnusedResources())
		{
			m_unloadAsyncOp = Resources.UnloadUnusedAssets();
			return true;
		}
		return false;
	}

	private int GetTextureMemoryMb()
	{
		return (int)(Texture.currentTextureMemory / 1048576);
	}

	private bool IsNeedUnloadUnusedResources()
	{
		return GetTextureMemoryMb() > 300;
	}

	private void OnCarLoaded()
	{
		IsVisualLoaded = true;
		carModel.OnCarLoaded();
		m_optimizationModel?.Release();
		m_optimizationModel = new CarOptimizationModel(this);
		if (!isNetworkCar && !isGhost)
		{
			CameraSwitch instance = CameraSwitch.instance;
			if ((bool)instance)
			{
				instance.SetCar(this);
			}
		}
		TryInitializeIK();
		InitWindowsRoots();
		RecordLastOnRoadData();
		m_raceCalc.Clear();
		m_carLightController.OnCarLoaded(this);
		if ((bool)m_currentTrack)
		{
			m_raceCalc.Init(this, m_currentTrack);
		}
		InputSettings.Parameters parameters = GameInput.InputManager.settings.Params;
		InputSettings.WheelParams wheelParams = parameters.wheelParams;
		wheelParams.SteeringWheelAngleChanged = (System.Action)Delegate.Remove(wheelParams.SteeringWheelAngleChanged, new System.Action(SetupSteerAnimationMaxAngleHandler));
		InputSettings.WheelParams wheelParams2 = parameters.wheelParams;
		wheelParams2.SteeringWheelAngleChanged = (System.Action)Delegate.Combine(wheelParams2.SteeringWheelAngleChanged, new System.Action(SetupSteerAnimationMaxAngleHandler));
		base.gameObject.ReplaceLayerRecursively(0, base.gameObject.layer);
		m_engineController.OnCarLoaded();
		this.OnCarVisualLoadFinishEvent.SafeCall(this);
		if (flares == null)
		{
			flares = GetComponentInChildren<CarFlares>(includeInactive: true);
		}
	}

	private bool TryInitializeIK()
	{
		if (carModel.skeleton.GetRoots("driver", out var rootList) && rootList.Count > 0 && m_driverTransform != null)
		{
			int index = 0;
			if (carModel.skeleton.GetRoots("steeringwheel", out var rootList2) && rootList2.Count == 1)
			{
				Vector3 position = rootList2[0].position;
				float num = float.MaxValue;
				int i = 0;
				for (int count = rootList.Count; i < count; i++)
				{
					Transform transform = rootList[i];
					if (!(transform == null))
					{
						float sqrMagnitude = (transform.position - position).sqrMagnitude;
						if (sqrMagnitude < num)
						{
							num = sqrMagnitude;
							index = i;
						}
					}
				}
			}
			try
			{
				if ((bool)m_driverAnimController)
				{
					m_driverAnimController.Setup(carModel.transform, rootList[index]);
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning("Driver init error: " + ex.Message);
				return false;
			}
			return true;
		}
		return false;
	}

	private AsyncOperationHandle<RuntimeAnimatorController> LoadRuntimeController(bool isDriverAnimController, int angle)
	{
		string text = (isDriverAnimController ? "pilotcontroller{0}" : "steeringwheel{0}");
		return ResourcesWrapper.LoadAsync<RuntimeAnimatorController>(string.Format("prefabs/driver/animators/" + text, GetAllowedAngleBeforeLoad(angle)));
	}

	private int GetAllowedAngleBeforeLoad(int angle)
	{
		Array.Sort(ALLOWED_ANGLES);
		int num = ALLOWED_ANGLES.Length - 1;
		if (angle >= ALLOWED_ANGLES[num])
		{
			return ALLOWED_ANGLES[num];
		}
		if (angle <= ALLOWED_ANGLES[0])
		{
			return ALLOWED_ANGLES[0];
		}
		for (int i = 1; i < num; i++)
		{
			int num2 = ALLOWED_ANGLES[i];
			int num3 = ALLOWED_ANGLES[i - 1];
			if (angle == num2)
			{
				return angle;
			}
			if (angle > num3 && angle < num2)
			{
				return num2;
			}
		}
		return ALLOWED_ANGLES[num];
	}

	private void ReleaseRuntimeAnimControllers()
	{
		foreach (AsyncOperationHandle<RuntimeAnimatorController> asyncOperationHandle in m_asyncOperationHandles)
		{
			ResourcesWrapper.UnloadAsset(asyncOperationHandle);
		}
		m_asyncOperationHandles.Clear();
	}

	private async void SetupSteerAnimationMaxAngleHandler()
	{
		if (m_driverAnimController == null)
		{
			return;
		}
		InputSettings.Parameters parameters = GameInput.InputManager.settings.Params;
		ESteeringWheelAngle steeringAngle = parameters.wheelParams.steeringWheelAngle;
		int angleAsInt = (int)steeringAngle;
		ReleaseRuntimeAnimControllers();
		m_driverAnimController.InvalidateCurrentMaxSteeringAngle();
		AsyncOperationHandle<RuntimeAnimatorController> driverAnimatorController = LoadRuntimeController(isDriverAnimController: true, angleAsInt);
		AsyncOperationHandle<RuntimeAnimatorController> steeringWheelAnimatorController = LoadRuntimeController(isDriverAnimController: false, angleAsInt);
		m_asyncOperationHandles.Add(driverAnimatorController);
		m_asyncOperationHandles.Add(steeringWheelAnimatorController);
		await driverAnimatorController;
		await steeringWheelAnimatorController;
		if (!m_driverAnimController.TrySetupSteeringAngle(angleAsInt, driverAnimatorController.Result, steeringWheelAnimatorController.Result))
		{
			ESteeringWheelAngle[] angles = (ESteeringWheelAngle[])Enum.GetValues(typeof(ESteeringWheelAngle));
			int i = 0;
			while (i < angles.Length)
			{
				ESteeringWheelAngle eSteeringWheelAngle = angles[i];
				if (eSteeringWheelAngle > steeringAngle)
				{
					angleAsInt = (int)eSteeringWheelAngle;
					AsyncOperationHandle<RuntimeAnimatorController> asyncOperationHandle = LoadRuntimeController(isDriverAnimController: true, angleAsInt);
					AsyncOperationHandle<RuntimeAnimatorController> steeringWheelAnimController = LoadRuntimeController(isDriverAnimController: false, angleAsInt);
					m_asyncOperationHandles.Add(asyncOperationHandle);
					m_asyncOperationHandles.Add(steeringWheelAnimController);
					await asyncOperationHandle;
					await steeringWheelAnimController;
					if (m_driverAnimController.TrySetupSteeringAngle(angleAsInt, driverAnimatorController.Result, steeringWheelAnimatorController.Result))
					{
						break;
					}
				}
				int num = i + 1;
				i = num;
			}
		}
		await Task.Yield();
		m_driverTransform.gameObject.SetActive(value: true);
		EnableDriverModel(m_enableDriverModel);
	}

	private void OnCollisionEnter(Collision collInfo)
	{
		this.OnCollisionEnterEvent.Call(collInfo);
		if (attachedDriftController != null)
		{
			attachedDriftController.RegisterCollision(collInfo);
		}
	}

	private void OnCollisionExit(Collision collInfo)
	{
		this.OnCollisionExitEvent.Call(collInfo);
	}

	private void OnDestroy()
	{
		m_optimizationModel?.Release();
		InputSettings.WheelParams wheelParams = GameInput.InputManager.settings.Params.wheelParams;
		wheelParams.SteeringWheelAngleChanged = (System.Action)Delegate.Remove(wheelParams.SteeringWheelAngleChanged, new System.Action(SetupSteerAnimationMaxAngleHandler));
		if (m_windowsRoots != null)
		{
			m_windowsRoots.Clear();
			m_windowsRoots = null;
		}
		if (m_carModel != null)
		{
			m_carModel.Dispose();
			m_carModel = null;
		}
		UnityEngine.Object.Destroy(m_driverInstance);
		UnityEngine.Object.Destroy(m_emptyDriverParent);
		m_driverInstance = null;
		m_emptyDriverParent = null;
		GUICommonNickNames.DisposeNick(this);
		MinimapProvider.instance.DisposeCar(this);
		ReleaseRuntimeAnimControllers();
		GraphicsQuality graphicsQuality = m_graphicsQuality;
		graphicsQuality.onSkidmarkChange = (System.Action)Delegate.Remove(graphicsQuality.onSkidmarkChange, new System.Action(OnSkidmarkChangeHandler));
		CarEngineController carEngineController = m_engineController;
		carEngineController.onIgnitionChanged = (Action<bool>)Delegate.Remove(carEngineController.onIgnitionChanged, new Action<bool>(OnIgnitionChanged));
		m_engineController.Destroy();
		m_engineController = null;
	}

	public void UnloadResources()
	{
		ResourcesWrapper.UnloadResources("race_state");
	}

	private void OnEnable()
	{
		if ((bool)Singletone<Simulator>.instance)
		{
			Singletone<Simulator>.instance.OnUpdateWheelsEvent += OnUpdateWheelsEvent;
		}
	}

	private void OnDisable()
	{
		if ((bool)Singletone<Simulator>.instance)
		{
			Singletone<Simulator>.instance.OnUpdateWheelsEvent -= OnUpdateWheelsEvent;
		}
	}

	private void OnGUI()
	{
		bool flag = UIWatchPlayerContext.networkPlayer != null;
		if (!carDebugInfo || (m_prefs.globals.lastCarId != carId && !flag) || (!isNetworkCar && flag) || isPodiumCar || isGhost || (isNetworkCar && flag && !networkPlayer.PlayerId.Equals(UIWatchPlayerContext.networkPlayer.PlayerId)))
		{
			return;
		}
		float x = (float)Screen.width - 450f;
		float y = 80f;
		float btnWidth = 300f;
		float btnHeight = 22f;
		if (m_playerCarFactory == null)
		{
			m_playerCarFactory = DependencyInjector.Resolve<PlayerCarFactory>();
		}
		Rect buttonPos = new Rect(new Vector2(x, y), new Vector2(btnWidth, btnHeight));
		CarDesc carXDesc = GetDesc().carXDesc;
		CarDesc.Engine engine = carXDesc.engine;
		CarDesc.GearBox gearBox = carXDesc.gearBox;
		PrefsCarSlotsModel carSettings = m_prefs.carSettings;
		CarVisualManager.VisualSettings visualForCar = carSettings.GetVisualForCar(carId);
		(string, bool, int) haveCustomSetupsPerCar = carSettings.GetHaveCustomSetupsPerCar(carId);
		string arg = (m_prefs.globals.speedKmh ? " км/ч" : " м/ч");
		float num = 3.6f * (m_prefs.globals.speedKmh ? 1f : 0.6213f);
		base.carX.CalcMaxPowerMaxTorque(out var max_hp, out var _, out var max_tq, out var _);
		ShowInfo("<-----------Engine Info----------->");
		ShowInfo($"зацеп пер:: {visualForCar.frontTire.ProtectorGrip}");
		ShowInfo($"зацеп зад:: {visualForCar.rearTire.ProtectorGrip}");
		ShowInfo($"двигатель :: {swap.engineId} | {swap.engineName}");
		ShowInfo($"профиль :: {carSettings.GetProfileIdForCar(carId)} | {profileMetaInfo?.name}");
		ShowInfo("id кастома :: " + haveCustomSetupsPerCar.Item1);
		ShowInfo($"отсечка :: {engine.revLimiter}");
		ShowInfo($"турбо вкл :: {engine.turboCharged}");
		ShowInfo($"turboPressure :: {engine.turboPressure}");
		ShowInfo($"масса :: {carXDesc.weight.mass}");
		ShowInfo($"Удельная мощность :: {MathF.Round(max_hp / carXDesc.weight.mass, 2)}");
		ShowInfo($"Развесовка :: {carXDesc.weight.frontPercent}");
		ShowInfo($"Высота центра тяжести :: {carXDesc.weight.upPercent}");
		ShowInfo($"пул двигателей :: {m_playerCarFactory.enginesPool.count}");
		ShowInfo($"ABS :: {carXDesc.brakes.useABS}");
		ShowInfo($"USED Traction Control :: {carXDesc.engine.useTC}");
		ShowInfo($"HAS Traction Control :: {carXDesc.engine.hasTC}");
		ShowInfo($"Launch Control :: {carXDesc.engine.hasLaunchControl}");
		ShowInfo("Привод :: " + carXDesc.transmission.gearType);
		ShowInfo("<-----------Car Specs----------->");
		ShowInfo($"макс скорость :: {Mathf.RoundToInt(base.carX.CalcTopSpeed() * num)} {arg}");
		ShowInfo($"разгон :: {base.carX.CalcAccelTime(0f, 27.777779f)} с");
		ShowInfo($"мощность :: {Mathf.RoundToInt(max_hp)} ");
		ShowInfo($"обороты :: {Mathf.RoundToInt(max_tq)} ");
		ShowInfo($"Gamepad Assist :: {m_prefs.globals.steeringAssistentGamepad} ");
		ShowInfo($"Keyboard Assist :: {m_prefs.globals.steeringAssistentKeyboard} ");
		ShowInfo($"SteeringWheel Assist :: {m_prefs.globals.steeringAssistentWheel} ");
		ShowInfo("<-----------GearBox----------->");
		ShowInfo($"главная пара :: {gearBox.finalDrive}");
		for (int i = 0; i < gearBox.gears.Length; i++)
		{
			ShowInfo($"передача {i + 1} :: {gearBox.gears[i]}");
		}
		ShowInfo("<-----------Toe/Camber----------->");
		Vector3 eulerAngles = carModel.carWheels[0].hub.parent.localRotation.eulerAngles;
		Vector3 eulerAngles2 = carModel.carWheels[2].hub.parent.localRotation.eulerAngles;
		string text = Mathf.DeltaAngle(0f, eulerAngles.y).ToString("N2");
		string text2 = Mathf.DeltaAngle(0f, eulerAngles2.y).ToString("N2");
		string text3 = Mathf.DeltaAngle(0f, eulerAngles.z).ToString("N2");
		string text4 = Mathf.DeltaAngle(0f, eulerAngles2.z).ToString("N2");
		ShowInfo("схождение перед :: " + text);
		ShowInfo("развал перед :: " + text3);
		ShowInfo("схождение звд :: " + text2);
		ShowInfo("развал зад :: " + text4);
		x = (float)Screen.width - 250f;
		y = 80f;
		List<CarVilylsInfo.VinylPreset> presetsList = m_prefs.carSettings.GetVinylsInfoForCar(carId).GetPresetsList();
		ShowInfo("<-----------Current Visual----------->");
		ShowInfo("Preset ID :: " + visualForCar.vinylPresetLiveryId);
		ShowInfo($"Количество винилов на машине :: {visualForCar.vinylLayers.Count}");
		ShowInfo("<-----------All cars presets----------->");
		for (int j = 0; j < presetsList.Count; j++)
		{
			CarVilylsInfo.VinylPreset vinylPreset = presetsList[j];
			ShowInfo($"<Preset {j + 1}-------------------->");
			ShowInfo("Preset ID :: " + vinylPreset.liveryId);
			ShowInfo($"Количество винилов в пресете :: {vinylPreset.layers.Count}");
			if (vinylPreset.layers.Count <= 0)
			{
				ShowInfo("<Нет винилов в пресете!>");
			}
			ShowInfo("");
		}
		void ShowInfo(string msg)
		{
			y += 18f;
			buttonPos = new Rect(new Vector2(x, y), new Vector2(btnWidth, btnHeight));
			GUI.Label(buttonPos, msg);
		}
	}
}
