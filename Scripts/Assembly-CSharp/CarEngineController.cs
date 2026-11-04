using System;
using System.Collections;
using CarX;
using GameInput;
using UnityEngine;

public class CarEngineController
{
	public const float MIN_ENGINE_START_TIME = 0.3f;

	public const float MAX_ENGINE_START_TIME = 0.75f;

	public const float VOLTAGE_DEFAULT_SCALE = 1f;

	public const float VOLTAGE_DROP_SCALE = 0.5f;

	private const float MIN_THROTTLE_TO_START = 0.1f;

	private const float PUMP_TIME_STOCK = 2f;

	private const int BUMP_RPM_TICKS = 9;

	private const float BUMP_RPM_STEP = 100f;

	private const float BUMP_RPM_THRESHOLD = 2000f;

	private const float RPM_DELTA_TO_PREVENT_SHUTDOWN = 50f;

	public Action<bool> onIgnitionChanged;

	public Action<bool> onStarterChanged;

	private bool m_starterEngaged;

	private float m_starterTargetTime;

	private float m_starterTime;

	private bool m_raceCarSetup;

	private bool m_precombatStarted;

	private bool m_pumpSoundPlaying;

	private bool m_stockPumpStarted;

	private bool m_stockPumpPlayed;

	private float m_stockPumpTime;

	private bool m_autoClutch;

	private bool m_onTrack;

	private Coroutine m_forceStartCoroutine;

	private Coroutine m_setupCarCoroutine;

	private RaceCar m_car;

	private FMODCarEngine m_engineScript;

	public bool isIgnitionOn
	{
		get
		{
			if (currentCarValid)
			{
				return m_car.carX.ignition;
			}
			return false;
		}
		private set
		{
			if (currentCarValid)
			{
				m_car.carX.ignition = value;
				if (!value)
				{
					m_stockPumpPlayed = false;
					m_car.carX.starter = 0f;
					StarterReleased();
				}
				onIgnitionChanged.SafeCall(value);
			}
		}
	}

	public bool isStarterOn
	{
		get
		{
			if (currentCarValid)
			{
				return m_car.carX.starter > 0f;
			}
			return false;
		}
		private set
		{
			if (currentCarValid && isIgnitionOn)
			{
				ChangeLightsBrightness(value ? 0.5f : 1f);
				m_car.carX.starter = (value ? 1f : 0f);
				onStarterChanged.SafeCall(value);
			}
		}
	}

	public bool engineWorking
	{
		get
		{
			if (currentCarValid)
			{
				return m_car.carX.isEngineWorks;
			}
			return false;
		}
	}

	public bool checkEngine
	{
		get
		{
			if (currentCarValid && !m_car.carX.isEngineWorks)
			{
				return isIgnitionOn;
			}
			return false;
		}
	}

	public float engineStartTime { get; private set; } = 0.75f;

	private bool currentCarValid
	{
		get
		{
			if (m_car != null)
			{
				return m_car.carX != null;
			}
			return false;
		}
	}

	private bool engineScriptValid => m_engineScript != null;

	public void Init(RaceCar car)
	{
		m_car = car;
		m_engineScript = car.gameObject.GetComponentInChildren<FMODCarEngine>();
		InputSettings.Parameters parameters = InputManager.settings.Params;
		parameters.autoClutchChanged = (Action<bool>)Delegate.Combine(parameters.autoClutchChanged, new Action<bool>(OnAutoClutchChanged));
	}

	public void Destroy()
	{
		InputSettings.Parameters parameters = InputManager.settings.Params;
		parameters.autoClutchChanged = (Action<bool>)Delegate.Remove(parameters.autoClutchChanged, new Action<bool>(OnAutoClutchChanged));
	}

	public void OnCarLoaded()
	{
		m_onTrack = true;
		if (!(States.mainCurrent is BaseRaceState))
		{
			m_onTrack = false;
			if (States.current is GarageGUIState garageGUIState)
			{
				GarageCar3dViewContext garageCar3dViewContext = garageGUIState.contextOwner.Get<GarageCar3dViewContext>();
				if ((bool)garageCar3dViewContext && garageCar3dViewContext.IsPodiumCar(m_car))
				{
					return;
				}
			}
			isIgnitionOn = true;
			isStarterOn = false;
		}
		ResetStarterTimer();
		m_autoClutch = InputManager.settings.Params.autoClutch;
		UpdateCarSetup();
		if (currentCarValid)
		{
			m_car.carX.gear = 0;
			m_car.carX.ResetTransmission();
		}
		if (m_setupCarCoroutine != null)
		{
			TimeController.StopNow(m_setupCarCoroutine);
			m_setupCarCoroutine = null;
		}
		if (m_setupCarCoroutine == null)
		{
			m_setupCarCoroutine = TimeController.StartNow(SetupCarCoroutine());
		}
	}

	public void FixedUpdate()
	{
		if (BaseCar.IsNull(m_car))
		{
			return;
		}
		Car carX = m_car.carX;
		if (m_starterEngaged)
		{
			m_starterTime += Time.fixedDeltaTime;
			if (m_starterTime >= m_starterTargetTime)
			{
				StarterReleased();
				ForceStartEngine(bumpRpm: true);
			}
		}
		if (!m_raceCarSetup && m_stockPumpStarted)
		{
			m_stockPumpTime += Time.fixedDeltaTime;
			if (m_stockPumpTime >= 2f)
			{
				m_stockPumpStarted = false;
				m_stockPumpPlayed = true;
				SetPumpSound(enabled: false);
			}
		}
		if (engineWorking && (carX.autoClutch || States.current is GarageGUIState))
		{
			float num = carX.engineCutRPM + 50f;
			if (carX.rpm < num)
			{
				carX.rpm = num;
			}
		}
		if (m_car.isLocalPlayer && m_autoClutch && States.mainCurrent is BaseRaceState && !(States.current != States.mainCurrent) && PlayerCarControl.instance != null)
		{
			_ = PlayerCarControl.instance.blockControl;
		}
	}

	public void SetIgnition(bool enabled)
	{
		if (isIgnitionOn != enabled)
		{
			SetIgnitionSound(enabled: true);
		}
		isIgnitionOn = enabled;
		SetPumpSound(isIgnitionOn);
	}

	public void SetNetworkIgnition(bool enabled)
	{
		bool num = isIgnitionOn != enabled;
		isIgnitionOn = enabled;
		if (num)
		{
			SetIgnitionSound(enabled: true);
		}
		if (!isIgnitionOn || m_raceCarSetup || (!m_raceCarSetup && m_stockPumpTime < 2f))
		{
			SetPumpSound(isIgnitionOn);
		}
		if (!isIgnitionOn)
		{
			m_stockPumpStarted = false;
			m_stockPumpTime = 0f;
		}
	}

	public void ToggleIgnition()
	{
		isIgnitionOn = !isIgnitionOn;
		SetIgnitionSound(enabled: true);
		SetPumpSound(isIgnitionOn);
	}

	public void StarterNetwork(bool enabled, float startTime = 0f)
	{
		if (currentCarValid && isIgnitionOn && !engineWorking)
		{
			if (!enabled)
			{
				ResetStarterTimer();
				isStarterOn = false;
				SetStarterSound(enabled: false);
			}
			else if (!m_starterEngaged && !isStarterOn)
			{
				ResetStarterTimer();
				m_starterEngaged = true;
				m_starterTargetTime = ((startTime > 0f) ? startTime : UnityEngine.Random.Range(0.3f, 0.75f));
				isStarterOn = true;
				SetStarterSound(enabled: true);
			}
		}
	}

	public void StarterPressed()
	{
		if (currentCarValid && isIgnitionOn && !engineWorking)
		{
			ResetStarterTimer();
			m_starterEngaged = true;
			m_starterTargetTime = UnityEngine.Random.Range(0.3f, 0.75f);
			engineStartTime = m_starterTargetTime;
			isStarterOn = true;
			SetStarterSound(enabled: true);
		}
	}

	public void StarterReleased()
	{
		if (currentCarValid)
		{
			SetStarterSound(enabled: false);
			ResetStarterTimer();
			isStarterOn = false;
		}
	}

	public void ShutdownEngine()
	{
		if (!currentCarValid)
		{
			Debug.LogError("CarEngineController: Unable to shutdown engine, car is null");
			return;
		}
		isIgnitionOn = false;
		isStarterOn = false;
		SetPumpSound(enabled: false);
		m_car.carX.ShutdownEngine();
	}

	public void ShutdownEngineNetwork()
	{
		if (engineWorking)
		{
			m_car.carX.ShutdownEngine();
		}
	}

	public void ForceStartEngine(bool bumpRpm = false)
	{
		if (!engineWorking && m_forceStartCoroutine == null)
		{
			isIgnitionOn = true;
			isStarterOn = false;
			m_forceStartCoroutine = TimeController.StartNow(ForceStartCoroutine(bumpRpm));
		}
	}

	public void ForceStartEngineGarage()
	{
		if (currentCarValid)
		{
			isIgnitionOn = true;
			m_car.carX.starter = 1f;
		}
	}

	public void StartEnginePrecombat()
	{
		if (!m_precombatStarted && !engineWorking && !isStarterOn)
		{
			m_precombatStarted = true;
			SetIgnition(enabled: true);
			StarterPressed();
		}
	}

	public void PrecombatLeft()
	{
		m_precombatStarted = false;
	}

	private void ChangeLightsBrightness(float voltageDropScale)
	{
		if (!(m_car == null) && !(m_car.carLightController == null))
		{
			m_car.carLightController.SetLightsBrightnessByVoltageScale(voltageDropScale);
		}
	}

	private IEnumerator ForceStartCoroutine(bool bumpRpm)
	{
		WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();
		yield return waitForFixedUpdate;
		if (!IsIterationCarValid())
		{
			yield break;
		}
		Car carx = m_car.carX;
		carx.starter = 1f;
		yield return waitForFixedUpdate;
		if (!IsIterationCarValid())
		{
			yield break;
		}
		carx.starter = 0f;
		if (bumpRpm)
		{
			int i = 0;
			while (i <= 9)
			{
				if (carx.rpm < 2000f)
				{
					carx.rpm += 100f;
					yield return waitForFixedUpdate;
				}
				if (!IsIterationCarValid())
				{
					yield break;
				}
				int num = i + 1;
				i = num;
			}
		}
		m_forceStartCoroutine = null;
		bool IsIterationCarValid()
		{
			if (currentCarValid)
			{
				return true;
			}
			m_forceStartCoroutine = null;
			return false;
		}
	}

	private IEnumerator SetupCarCoroutine()
	{
		yield return null;
		yield return null;
		yield return null;
		if (currentCarValid)
		{
			BaseState mainCurrent = States.mainCurrent;
			ForceStartEngineByState(mainCurrent);
			int gear = ((!(mainCurrent is FreerideRaceModeState) && !(mainCurrent is GarageGUIState)) ? 1 : 0);
			m_car.carX.gear = gear;
			m_car.carX.ResetTransmission();
		}
		UpdatePumpSound();
	}

	private void ForceStartEngineByState(BaseState state)
	{
		if (!(state is GarageGUIState) && !(state is DynoMenuState) && (object)state != null)
		{
			ForceStartEngine(bumpRpm: true);
		}
	}

	private void SetIgnitionSound(bool enabled)
	{
		if (engineScriptValid)
		{
			if (enabled)
			{
				m_engineScript.PlayIgnitionSound();
			}
			else
			{
				m_engineScript.StopIgnitionSound();
			}
		}
	}

	private void SetStarterSound(bool enabled)
	{
		if (engineScriptValid)
		{
			if (enabled)
			{
				m_engineScript.PlayStarterSound();
			}
			else
			{
				m_engineScript.StopStarterSound();
			}
		}
	}

	private void SetPumpSound(bool enabled)
	{
		if (!engineScriptValid)
		{
			return;
		}
		if (enabled)
		{
			if (!m_pumpSoundPlaying && !m_stockPumpPlayed)
			{
				m_engineScript.PlayPumpSound();
				m_pumpSoundPlaying = true;
				if (!m_raceCarSetup)
				{
					m_stockPumpTime = 0f;
					m_stockPumpStarted = true;
				}
			}
		}
		else
		{
			m_engineScript.StopPumpSound();
			m_pumpSoundPlaying = false;
			m_stockPumpStarted = false;
		}
	}

	private void UpdateCarSetup()
	{
		m_pumpSoundPlaying = false;
		if (!(m_car == null) && m_car.profileMetaInfo != null)
		{
			m_raceCarSetup = string.Equals(m_car.profileMetaInfo.name, "racing", StringComparison.OrdinalIgnoreCase) || string.Equals(m_car.profileMetaInfo.name, "ultimate", StringComparison.OrdinalIgnoreCase);
		}
	}

	private void UpdatePumpSound()
	{
		if (!m_onTrack)
		{
			SetPumpSound(enabled: false);
		}
		else if (m_raceCarSetup)
		{
			SetPumpSound(enabled: true);
		}
	}

	private void ResetStarterTimer()
	{
		m_starterEngaged = false;
		m_starterTime = 0f;
		m_starterTargetTime = 0f;
	}

	private void OnAutoClutchChanged(bool autoClutch)
	{
		m_autoClutch = autoClutch;
	}
}
