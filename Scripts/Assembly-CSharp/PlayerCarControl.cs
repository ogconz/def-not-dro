using System.Collections.Generic;
using CarX;
using DI;
using GameInput;
using PrefsModels;
using SyncMultiplayer;
using UnityEngine;

[AddComponentMenu("Scripts/Generic/Input/PlayerCarControl")]
public class PlayerCarControl : MonoBehaviour
{
	private const float ClutchKickMin = 0.95f;

	private const float TimeToFullbrake = 1.5f;

	private const float MinSpeedToAutoPark = 10f;

	private const float MaxPauseAutoparking = 2f;

	private const string WaypointObjectName = "Waypoint";

	[SerializeField]
	private RaceCar m_car;

	[SerializeField]
	private bool m_fastSteering;

	[SerializeField]
	private bool m_autoReverse = true;

	[Dependency]
	private GamePrefs m_gamePrefs;

	private GlobalModel m_globals;

	[Dependency]
	private WheelForceFeedbackSupport m_wheelController;

	private GlobalModel.SteeringAssist? m_oldAssistValue;

	private SteeringMode m_oldsteeringMode = SteeringMode.Axis;

	private SteeringMode m_steeringMode = SteeringMode.Buttons;

	private InputManager m_inputManager;

	private ControlsSetup m_controlsSetup;

	private float m_lastTimeInFrame;

	private int m_lastFrameCount;

	private float m_timerAutoParking = 2f;

	private float m_brakeTimer;

	private float m_brake;

	private bool m_needTryToStartEngine = true;

	private NetGameSyncCarLightsSystem m_lightsSyncSystem;

	private Waypoint m_currentWaypoint;

	private float m_zeroSpeedForceDamp = 1f;

	private float m_lastTimeApplyAssist;

	private bool m_autoNeutral;

	private bool m_waitNeutral;

	public static PlayerCarControl instance { get; private set; }

	public static int kerbIndex { get; private set; } = -1;

	public bool canSpawnCarInMPRace { get; private set; } = true;

	public bool blockControl { get; private set; }

	public (Vector3, Quaternion) currentWayPoint => m_currentWaypoint.minimapItem.waypointScenePosRot;

	public bool fastSteering
	{
		get
		{
			return m_fastSteering;
		}
		set
		{
			m_fastSteering = value;
			ApplyToCar();
		}
	}

	public RaceCar car
	{
		get
		{
			return m_car;
		}
		set
		{
			m_car = value;
			ApplyToCar();
		}
	}

	private void Awake()
	{
		instance = this;
		this.MakeDependenciesInjected();
		m_globals = m_gamePrefs.globals;
		m_inputManager = InputManager.instance;
		m_controlsSetup = ScriptableSingleton<ControlsSetup>.instance;
		if (kerbIndex < 0)
		{
			kerbIndex = new List<CARXSurfaceTemplate>(CARXSurfaceManager.GetAllTemplates()).FindIndex((CARXSurfaceTemplate st) => st.name.Contains("kerb"));
			if (kerbIndex == -1)
			{
				Debug.Log("Not found Kerb surface index");
			}
		}
		m_lastTimeInFrame = Time.realtimeSinceStartup;
		UpdateSteeringMode(forceUpdate: true);
		if (NetworkController.InstanceGame != null)
		{
			m_lightsSyncSystem = NetworkController.InstanceGame.systems.Get<NetGameSyncCarLightsSystem>();
		}
		m_currentWaypoint = InitializeWaypoint();
		RefreshCurrentWaypoint();
		DayNightController dayNightController = DayNightController.instance;
		if (dayNightController != null)
		{
			dayNightController.OnChangeDayNightMode.AddListener(OnDayNightChanged);
		}
	}

	private Waypoint InitializeWaypoint()
	{
		GameObject go = Object.Instantiate(new GameObject("Waypoint"));
		WaypointMinimapItem orAddComponent = go.GetOrAddComponent<WaypointMinimapItem>();
		Waypoint waypoint = (Waypoint)(orAddComponent.host = go.GetOrAddComponent<Waypoint>());
		orAddComponent.typeIndex = 3;
		waypoint.SetMinimapRelyingItem(orAddComponent);
		return waypoint;
	}

	public void RefreshCurrentWaypoint()
	{
		if (m_gamePrefs.globals.waypoints.TryGetValue(m_gamePrefs.globals.lastTrackId, out var value))
		{
			m_currentWaypoint.minimapItem.waypointScenePosRot = value;
		}
		else
		{
			m_currentWaypoint.minimapItem.SetDefaultPosRot();
		}
	}

	public void SetWaypointPosRot(Vector3 position, Quaternion rotation)
	{
		m_currentWaypoint.minimapItem.waypointScenePosRot = (position: position, rotation: rotation);
		m_globals.waypoints[m_globals.lastTrackId] = (position, rotation);
		RefreshCurrentWaypoint();
	}

	private void SetControlGraph(bool flag)
	{
	}

	private void OnDestroy()
	{
		DayNightController dayNightController = DayNightController.instance;
		if (dayNightController != null)
		{
			dayNightController.OnChangeDayNightMode.RemoveListener(OnDayNightChanged);
		}
		if (instance == this)
		{
			instance = null;
		}
	}

	private void OnEnable()
	{
		ApplyToCar();
		GameEvents.OnGameSettingsInputUpdate += new GameEventHandler<GameEvents.EventInfo>(ApplyToCar);
		if (GameConsole.Bool["SetInterpolate"])
		{
			TickManager.RegisterFixedUpdate(UpdateCarControl, 10, "CarControl");
			TickManager.RegisterUpdate(ApplyFFB, 30, "ApplyFFB");
		}
		else
		{
			TickManager.RegisterFixedUpdate(UpdateCarControl, 10, "CarControl");
			TickManager.RegisterFixedUpdate(ApplyFFB, 30, "ApplyFFB");
		}
		SetBlockControl(value: false);
	}

	private void OnDisable()
	{
		SetBlockControl(value: true);
		GameEvents.OnGameSettingsInputUpdate -= new GameEventHandler<GameEvents.EventInfo>(ApplyToCar);
		TickManager.UnregisterFixedUpdate(UpdateCarControl);
		TickManager.UnregisterFixedUpdate(ApplyFFB);
		TickManager.UnregisterUpdate(UpdateCarControl);
		TickManager.UnregisterUpdate(ApplyFFB);
	}

	private void OnChangeControlSettings()
	{
		ApplyToCar();
	}

	public void SetCanSpawnCarInMP(bool canSpawn)
	{
		if (canSpawnCarInMPRace != canSpawn)
		{
			canSpawnCarInMPRace = canSpawn;
		}
	}

	public void SetBlockControl(bool value)
	{
		blockControl = value;
		if ((bool)InputManager.instance)
		{
			if (value)
			{
				InputManager.instance.StopAllInputFX();
			}
			else
			{
				InputManager.instance.EnableAllInputFX();
			}
		}
		RefreshEnabledWheelParams();
	}

	public void PlayImpactEffect(float k)
	{
		if ((bool)m_wheelController && base.enabled && !blockControl)
		{
			m_wheelController.PlayImpactEffect(k);
		}
	}

	private void TryForceAutoPark()
	{
		if (BaseCar.IsNull(m_car))
		{
			return;
		}
		Car carX = m_car.carX;
		carX.accelerate = 0f;
		if (carX.speedKMH > 10f)
		{
			ResetAutoPark();
		}
		else if (!(m_brake >= 1f))
		{
			m_timerAutoParking += Time.deltaTime;
			if (m_timerAutoParking > 2f)
			{
				m_brakeTimer += Time.deltaTime;
				float brake = m_brakeTimer / 1.5f;
				m_brake = brake;
				carX.brake = m_brake;
				carX.clutch = 1f;
				m_autoReverse = false;
			}
		}
	}

	private void UpdateCarControl()
	{
		if (!IsCarCanControlled())
		{
			TryForceAutoPark();
			return;
		}
		InputSettings.Parameters parameters = InputManager.settings.Params;
		InputManager inputManager = InputManager.instance;
		UpdateSurfaceEffects(inputManager);
		float steer = m_car.carX.GetSteer();
		float num = 0f - inputManager.GetActionValue(ActionType.Turn);
		bool lastActionByButton = inputManager.lastActionByButton;
		steer = num;
		float accel = inputManager.GetActionValue(ActionType.Accelerate);
		float brake = inputManager.GetActionValue(ActionType.Brake);
		float num2 = inputManager.GetActionValue(ActionType.Clutch);
		float actionValue = inputManager.GetActionValue(ActionType.HandBrake);
		bool flag = true;
		if (!parameters.autoClutch)
		{
			ResetAutoPark();
			flag = false;
		}
		if (accel > 0.01f)
		{
			ResetAutoPark();
			NetworkController.InstanceGame.stats.SendUserActiveSignal();
			flag = false;
		}
		if (brake > 0.01f || num2 > 0.01f || actionValue > 0.01f)
		{
			ResetAutoPark();
			flag = false;
		}
		if (car.lockState)
		{
			m_timerAutoParking = 2f;
		}
		if (flag || car.lockState)
		{
			m_timerAutoParking += Time.deltaTime;
		}
		if (flag && m_timerAutoParking >= 2f && m_car.carX.speedKMH < 10f)
		{
			m_brakeTimer += Time.deltaTime;
			float t = m_brakeTimer / 1.5f;
			brake = (m_brake = ((!(m_brake >= 1f)) ? Mathf.Lerp(brake, 1f, t) : 1f));
			num2 = 1f;
			m_autoReverse = false;
		}
		else
		{
			m_autoReverse = parameters.automaticGearboxEnabled;
		}
		float num3 = (m_car.lockState ? 1f : num2);
		m_car.carX.clutch = num3;
		m_car.raceCarClutchInputVal = num3;
		m_car.isClutchKick = num3 >= 0.95f;
		if (inputManager.IsActionPressed(ActionType.RespawnCar))
		{
			TryRespawnCar();
		}
		if (inputManager.IsActionPressed(ActionType.RestartRace))
		{
			TryRestartRace();
		}
		UpdateGearbox();
		UpdateEngineControls();
		UpdateHeadlightsControl();
		if (m_globals.autoBrake > 0.01f)
		{
			m_car.ApplyAccelAndBrakeAssistent(ref accel, ref brake, m_globals.autoBrake, m_globals.autoBrake);
		}
		GlobalModel.SteeringAssist steeringAssist = inputManager.CurSteeringDeviceType switch
		{
			GameInput.DeviceType.Wheel => m_gamePrefs.globals.steeringAssistentWheel, 
			GameInput.DeviceType.Gamepad => m_gamePrefs.globals.steeringAssistentGamepad, 
			_ => m_gamePrefs.globals.steeringAssistentKeyboard, 
		};
		if (inputManager.CurSteeringDeviceType != GameInput.DeviceType.Unknown)
		{
			ApplyAssistParams(steeringAssist);
			CarXSetSteer(steer, steeringAssist != GlobalModel.SteeringAssist.None, lastActionByButton);
		}
		if (m_autoReverse && parameters.automaticGearboxEnabled && !NeedDisableRearGearOnStartRace())
		{
			int gear = m_car.carX.gear;
			if (m_car.CheckAutoReverse(ref accel, ref brake, ref gear))
			{
				m_car.carX.gear = gear;
			}
		}
		if (parameters.automaticGearboxEnabled && m_car.carX.gear == 0 && accel > 0f)
		{
			m_car.carX.gear = 1;
		}
		m_car.carX.accelerate = accel;
		m_car.carX.brake = brake;
		if (!m_car.lockState)
		{
			m_car.carX.handbrake = inputManager.GetActionValue(ActionType.HandBrake);
		}
	}

	private void ResetAutoPark()
	{
		m_timerAutoParking = 0f;
		m_brakeTimer = 0f;
		m_brake = 0f;
	}

	private void TryRespawnCar()
	{
		if (BaseCar.IsNull(m_car))
		{
			return;
		}
		BaseRaceState baseRaceState = States.mainCurrent as BaseRaceState;
		if (!m_car.lockState)
		{
			if (baseRaceState == null || !baseRaceState.allowCarRespawnInRace || !canSpawnCarInMPRace)
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.cant_spawn_car.Localized(), 2f);
			}
			else if (m_car.missCheckPoint)
			{
				m_car.ReturnCarToCheckPoint();
			}
			else
			{
				m_car.PlaceOnTrack();
			}
		}
	}

	private void TryRestartRace()
	{
		BaseRaceState baseRaceState = States.mainCurrent as BaseRaceState;
		if (baseRaceState == null)
		{
			return;
		}
		SyncNetFreerideRaceModeState syncNetFreerideRaceModeState = baseRaceState as SyncNetFreerideRaceModeState;
		if ((bool)syncNetFreerideRaceModeState && syncNetFreerideRaceModeState.raceMode == RaceMode.NetworkFreeride)
		{
			if (NetworkController.InstanceGame != null && NetworkController.InstanceGame.systems != null)
			{
				NetworkController.InstanceGame.systems.Get<NetGameSubroomsSystem>()?.RestartSingleRace();
			}
		}
		else if (!(baseRaceState is NetworkRaceState) && !(baseRaceState is DynoRaceModeState))
		{
			BaseRaceModeState baseRaceModeState = baseRaceState as BaseRaceModeState;
			if (baseRaceModeState != null)
			{
				baseRaceModeState.RestartRace();
			}
		}
	}

	private void CarXSetSteer(float steer, bool useAssistent, bool isBtn)
	{
		if (Time.frameCount > m_lastFrameCount)
		{
			m_lastFrameCount = Time.frameCount;
			float realtimeSinceStartup = Time.realtimeSinceStartup;
			if (!useAssistent)
			{
				float num = realtimeSinceStartup - m_lastTimeInFrame;
				if (num < Time.maximumDeltaTime)
				{
					m_car.carX.SetSteer2(steer, useAssistent, isBtn, num);
				}
			}
			m_lastTimeInFrame = realtimeSinceStartup;
		}
		if (useAssistent)
		{
			m_car.carX.SetSteer2(steer, useAssistent, isBtn, Time.deltaTime);
		}
	}

	private bool IsCarCanControlled()
	{
		if (!m_car)
		{
			return false;
		}
		if (Time.deltaTime == 0f)
		{
			return false;
		}
		if (States.current is DynoMenuState)
		{
			return false;
		}
		if (!(States.current is BaseRaceState) || blockControl)
		{
			CarXSetSteer(0f, useAssistent: true, isBtn: true);
			m_car.carX.accelerate = 0f;
			m_car.carX.brake = 1f;
			if (!(States.current is XDSRaceModeState))
			{
				m_car.carX.handbrake = 0f;
			}
			return false;
		}
		if (InputManager.IsInputLocked())
		{
			return false;
		}
		return true;
	}

	private void UpdateSurfaceEffects(InputManager input)
	{
		float num = 0f;
		float num2 = 0f;
		WheelIndex[] activeWheels = m_car.activeWheels;
		for (int i = 0; i < activeWheels.Length; i++)
		{
			Wheel wheel = m_car.carX.GetWheel(activeWheels[i]);
			if (!wheel.HasPatch())
			{
				continue;
			}
			switch (wheel.surfaceType)
			{
			case SurfaceType.Grass:
				num += 0.25f;
				continue;
			case SurfaceType.Earth:
				num2 += 0.25f;
				continue;
			case SurfaceType.Sand:
				num2 += 0.15f;
				continue;
			case SurfaceType.Snow:
				num += 0.1f;
				continue;
			}
			wheel.GetUserData(out var userData);
			if (userData == kerbIndex)
			{
				num2 += 0.3f;
			}
		}
		bool flag = false;
		num *= m_car.carX.velocity.magnitude * 0.025f;
		if (num > 0f)
		{
			flag = true;
			m_wheelController.PlayGrassSurfaceEffect(num);
		}
		num2 *= m_car.carX.velocity.magnitude * 0.025f;
		if (num2 > 0f)
		{
			flag = true;
			m_wheelController.PlayGravelSurfaceEffect(num2);
		}
		if (!flag || m_car.carX.speedKMH < 10f)
		{
			input.PlayVibro(0f, 0f);
		}
	}

	public void ApplyFFB()
	{
		if ((bool)m_car && Time.deltaTime != 0f)
		{
			float steerAlignMom = m_car.carX.steerAlignMom;
			m_wheelController.SetConstantForce(steerAlignMom);
		}
	}

	private GlobalModel.SteeringAssist GetSteeringAssistWithDevice(GameInput.DeviceType deviceType)
	{
		if (deviceType == GameInput.DeviceType.Wheel)
		{
			return GlobalModel.SteeringAssist.None;
		}
		return GlobalModel.SteeringAssist.AssistV2;
	}

	private void ApplyAssistParams(GlobalModel.SteeringAssist steeringAssistent)
	{
		UpdateSteeringMode(m_oldAssistValue != steeringAssistent);
		if (m_oldAssistValue != steeringAssistent)
		{
			GameLog.Print(AlertLevel.Notify, "Assist", $"Change assist from {m_oldAssistValue} to {steeringAssistent}");
			if (m_oldAssistValue.HasValue)
			{
				GUICommonGodVoice.ShowText((steeringAssistent == GlobalModel.SteeringAssist.None) ? (LocalizationKeys.settings_controls_st_assist.Localized() + ": " + LocalizationKeys.control_off.Localized()) : (LocalizationKeys.settings_controls_st_assist.Localized() + ": " + LocalizationKeys.control_on.Localized()), 5f);
			}
			m_oldAssistValue = steeringAssistent;
		}
	}

	private void UpdateSteeringMode(bool forceUpdate)
	{
		m_steeringMode = m_inputManager.GetLastSteeringMode();
		if (!forceUpdate)
		{
			if (m_steeringMode != m_oldsteeringMode)
			{
				GameLog.Print(AlertLevel.Notify, "Assist", "Change steering mode from " + m_oldsteeringMode.ToString() + " to " + m_steeringMode);
				SetSteeringMode(m_steeringMode);
			}
		}
		else
		{
			SetSteeringMode(m_steeringMode);
		}
	}

	private void SetSteeringMode(SteeringMode steeringMode)
	{
		m_oldsteeringMode = steeringMode;
		m_controlsSetup.ApplyParams(steeringMode);
		if (steeringMode == SteeringMode.LogitechWheel)
		{
			if ((bool)m_car)
			{
				m_car.SetEnableSteerRack(enable: false);
			}
		}
		else if ((bool)m_car)
		{
			m_car.SetEnableSteerRack(!blockControl && steeringMode == SteeringMode.Wheel);
		}
		RefreshEnabledWheelParams();
	}

	private void UpdateGearbox()
	{
		InputManager inputManager = InputManager.instance;
		Car carX = m_car.carX;
		InputSettings.Parameters parameters = InputManager.settings.Params;
		if (inputManager.IsActionPressed(ActionType.GearUp))
		{
			if (carX.isShifting)
			{
				return;
			}
			m_autoNeutral = false;
			m_waitNeutral = true;
			carX.GearUp();
		}
		if (inputManager.IsActionPressed(ActionType.GearDown))
		{
			if (carX.isShifting)
			{
				return;
			}
			m_autoNeutral = false;
			m_waitNeutral = true;
			carX.GearDown();
		}
		if (NeedDisableRearGearOnStartRace() && carX.gear == -1)
		{
			carX.gear = 1;
		}
		else
		{
			if (parameters.automaticGearboxEnabled)
			{
				return;
			}
			int num = CheckAllGear();
			if (!m_waitNeutral || num == 0)
			{
				m_waitNeutral = false;
				if (num != 0 || m_autoNeutral)
				{
					carX.gear = num;
				}
			}
		}
	}

	private int CheckAllGear()
	{
		int outgear = 0;
		CheckGear(ActionType.Gear1, 1, ref outgear);
		CheckGear(ActionType.Gear2, 2, ref outgear);
		CheckGear(ActionType.Gear3, 3, ref outgear);
		CheckGear(ActionType.Gear4, 4, ref outgear);
		CheckGear(ActionType.Gear5, 5, ref outgear);
		CheckGear(ActionType.Gear6, 6, ref outgear);
		CheckGear(ActionType.Gear7, 7, ref outgear);
		CheckGear(ActionType.GearR, -1, ref outgear);
		return outgear;
	}

	private void UpdateEngineControls()
	{
		InputManager inputManager = InputManager.instance;
		InputSettings.Parameters parameters = InputManager.settings.Params;
		if (inputManager == null || m_car == null)
		{
			return;
		}
		if (parameters.holdIgnition)
		{
			m_car.engineController.SetIgnition(inputManager.IsActionHold(ActionType.Ignition));
		}
		else if (inputManager.IsActionPressed(ActionType.Ignition))
		{
			m_car.engineController.ToggleIgnition();
			if (m_car.engineController.isIgnitionOn)
			{
				m_needTryToStartEngine = true;
			}
		}
		if (inputManager.IsActionPressed(ActionType.Starter))
		{
			m_car.engineController.StarterPressed();
		}
		else if (inputManager.IsActionReleased(ActionType.Starter))
		{
			m_car.engineController.StarterReleased();
		}
		if (m_car.carX.autoClutch)
		{
			ActionType type = ActionType.Accelerate;
			if (InputManager.settings.Params.automaticGearboxEnabled && m_car.carX.gear < 0)
			{
				type = ActionType.Brake;
			}
			float actionValue = InputManager.instance.GetActionValue(type);
			if (actionValue >= 0.1f && m_needTryToStartEngine)
			{
				m_needTryToStartEngine = false;
				m_car.engineController.StarterPressed();
			}
			else if (actionValue < 0.1f && !m_needTryToStartEngine)
			{
				m_needTryToStartEngine = true;
				m_car.engineController.StarterReleased();
			}
		}
	}

	private void UpdateHeadlightsControl()
	{
		InputManager inputManager = InputManager.instance;
		if (inputManager != null && inputManager.IsActionPressed(ActionType.Headlights) && m_car != null && m_car.engineController.isIgnitionOn)
		{
			UpdateHeadlights(!m_car.headlightsEnabled);
		}
	}

	private void OnDayNightChanged()
	{
		DayNightController dayNightController = DayNightController.instance;
		if (dayNightController != null)
		{
			UpdateHeadlights(!dayNightController.IsDayLight, force: true);
		}
	}

	private void UpdateHeadlights(bool lightsEnabled, bool force = false)
	{
		if (!(m_car == null) || force || m_car.headlightsEnabled != lightsEnabled)
		{
			m_car.SetHeadlightsEnabled(lightsEnabled, ignoreIgnition: false, force: true);
			m_lightsSyncSystem?.SendToAll(m_car.headlightsEnabled);
		}
	}

	private bool NeedDisableRearGearOnStartRace()
	{
		if (Time.unscaledTime < m_car.disableRGearTime)
		{
			return true;
		}
		return false;
	}

	private void CheckGear(ActionType action, int gear, ref int outgear)
	{
		if (InputManager.instance.GetActionValue(action) > 0f)
		{
			m_autoNeutral = InputManager.instance.lastActionDevice != GameInput.DeviceType.Keyboard;
			if (m_waitNeutral && !m_autoNeutral)
			{
				m_waitNeutral = false;
			}
			outgear = gear;
		}
	}

	private static float Linearize(float val, float linearity)
	{
		linearity = Mathf.Clamp01(linearity);
		float num = val * Mathf.Abs(val);
		return val * linearity + num * (1f - linearity);
	}

	private void ApplyToCar(GameEvents.EventInfo info = null)
	{
		if (base.enabled && (bool)m_car)
		{
			InputSettings.Parameters parameters = InputManager.settings.Params;
			m_fastSteering = GameConsole.Bool["cl_fastAssistance"];
			m_car.carX.gearShifting = ((!parameters.automaticGearboxEnabled) ? GearShiftType.Manual : GearShiftType.Auto);
			if ((bool)Singletone<Simulator>.instance)
			{
				Simulator.driftMode = m_fastSteering;
			}
			m_car.carX.autoClutch = parameters.autoClutch;
			RefreshEnabledWheelParams();
		}
	}

	private void RefreshEnabledWheelParams()
	{
		if ((bool)m_car)
		{
			bool flag = !blockControl && InputManager.instance.CurSteeringDeviceType == GameInput.DeviceType.Wheel;
			m_car.wheelsSteerNoInput = flag;
			m_car.SetEnableSteerRack(flag && !InputManager.instance.IsLogitechWheelLastSteeingDevice());
		}
	}
}
