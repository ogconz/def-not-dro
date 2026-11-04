using CarX;
using FMOD.Studio;
using UnityEngine;

public class FMODCarEngine : FMODBaseScript
{
	public const float MAX_HP_PARAM_VALUE = 1500f;

	private const string defaultEngineEvent = "event:/GameEvents/Cars/Default/Engine";

	private const string defaultExhaustEvent = "event:/GameEvents/Cars/Default/Exhaust";

	private const string defaultRevLimiterEvent = "event:/GameEvents/Cars/Default/RevLimiter";

	private const string IGNITION_EVENT = "event:/GameEvents/Ignition";

	private const string STARTER_EVENT = "event:/GameEvents/Starter";

	private const string PUMP_EVENT = "event:/GameEvents/Pump";

	private FMODEventNode m_engineNode;

	private FMODEventNode.Parameter m_rpmParam;

	private FMODEventNode.Parameter m_loadParam;

	private FMODEventNode.Parameter m_wheelRotationParam;

	private FMODEventNode.Parameter m_hpParam;

	private float m_load;

	private FMODEventNode revLimiterNode;

	private bool m_lastRevState;

	private bool m_wastegateAvaliable;

	private FMODEventNode m_ignitionNode;

	private FMODEventNode m_starterNode;

	private FMODEventNode.Parameter m_rpmPumpParam;

	private FMODEventNode m_fuelPumpNode;

	protected override bool OnEnableHandler()
	{
		m_lastRevState = false;
		if (!base.raceCar)
		{
			return false;
		}
		RefreshSound();
		CreateCarNamedNode(ref m_ignitionNode, "event:/GameEvents/Ignition");
		CreateCarNamedNode(ref m_starterNode, "event:/GameEvents/Starter");
		if (CreateCarNamedNode(ref m_fuelPumpNode, "event:/GameEvents/Pump"))
		{
			m_rpmPumpParam = m_fuelPumpNode?.RegisterParameter("RPM");
		}
		CheckWasteGate(base.raceCar);
		base.raceCar.OnSetDescEvent += CheckWasteGate;
		return base.OnEnableHandler();
	}

	protected override void OnDisableHandler()
	{
		base.raceCar.OnSetDescEvent -= CheckWasteGate;
		base.OnDisableHandler();
	}

	public override void RefreshSound()
	{
		int num;
		string sound;
		if (!base.raceCar.swap.isStockEngine)
		{
			num = ((!string.IsNullOrEmpty(base.raceCar.swap.meta.sound)) ? 1 : 0);
			if (num != 0)
			{
				sound = base.raceCar.swap.meta.sound;
				goto IL_005c;
			}
		}
		else
		{
			num = 0;
		}
		sound = base.raceCar.metaInfo.name;
		goto IL_005c;
		IL_005c:
		string metaName = sound;
		string eventRef = ((num != 0) ? "event:/GameEvents/Engines/{0}/{1}" : "event:/GameEvents/Cars/{0}/{1}");
		m_engineNode?.Release();
		if (CreateCustomCarNamedNode(ref m_engineNode, eventRef, metaName, "Engine", "event:/GameEvents/Cars/Default/Engine"))
		{
			m_rpmParam = m_engineNode?.RegisterParameter("RPM");
			m_loadParam = m_engineNode?.RegisterParameter("Load");
			m_wheelRotationParam = m_engineNode?.RegisterParameter("WheelRotation");
			m_hpParam = m_engineNode?.RegisterParameter("HP");
		}
		revLimiterNode?.Release();
		CreateCustomCarNamedNode(ref revLimiterNode, eventRef, metaName, "RevLimiter", "event:/GameEvents/Cars/Default/RevLimiter");
		base.RefreshSound();
	}

	protected override void UpdateSound(float dt)
	{
		base.UpdateSound(dt);
		bool isEngineWorks = base.raceCar.carX.isEngineWorks;
		bool flag = m_engineNode != null && m_engineNode.IsPlaying();
		if (flag && !isEngineWorks)
		{
			m_engineNode?.Stop(STOP_MODE.IMMEDIATE);
		}
		else if (!flag && isEngineWorks)
		{
			m_engineNode?.Start();
		}
		Car carX = base.raceCar.carX;
		float rpm = carX.rpm;
		float num = carX.load;
		float engineRevLimiter = carX.engineRevLimiter;
		float engineRevLimiterStep = carX.engineRevLimiterStep;
		float value = (m_wastegateAvaliable ? Mathf.Clamp(carX.engineCurPower, 0f, 1500f) : (-1f));
		float num2 = 10f * dt;
		float num3 = 5f * dt;
		if (num < 0.1f)
		{
			num = 0.1f;
		}
		if (num > m_load + num2)
		{
			m_load += num2;
		}
		else if (num < m_load - num3)
		{
			m_load -= num3;
		}
		else
		{
			m_load = num;
		}
		if (rpm > engineRevLimiter - (engineRevLimiterStep + 50f))
		{
			m_load = num;
		}
		m_load = Mathf.Clamp01(m_load);
		m_rpmParam.SetValue(rpm);
		m_loadParam.SetValue(m_load);
		m_hpParam.SetValue(value);
		m_rpmPumpParam.SetValue(rpm);
		if (m_lastRevState != base.raceCar.carX.isRevCut)
		{
			m_lastRevState = base.raceCar.carX.isRevCut;
			if (m_lastRevState)
			{
				revLimiterNode.Start();
			}
			else
			{
				revLimiterNode.Stop(STOP_MODE.ALLOWFADEOUT);
			}
		}
		int num4 = 0;
		float num5 = 0f;
		Wheel wheel = carX.GetWheel(WheelIndex.RearLeft);
		if (wheel.enabled)
		{
			num5 += wheel.w;
			num4++;
		}
		wheel = carX.GetWheel(WheelIndex.RearRight);
		if (wheel.enabled)
		{
			num5 += wheel.w;
			num4++;
		}
		if (num4 > 0)
		{
			num5 /= (float)num4;
		}
		m_wheelRotationParam.SetValue(num5);
	}

	private void CheckWasteGate(BaseCar baseCar)
	{
		RaceCar raceCar = baseCar as RaceCar;
		m_wastegateAvaliable = raceCar != null && raceCar.isWasteGateAvailable;
	}

	public void PlayIgnitionSound()
	{
		SetSoundTo(m_ignitionNode, play: true);
	}

	public void StopIgnitionSound()
	{
		SetSoundTo(m_ignitionNode, play: false);
	}

	public void PlayStarterSound()
	{
		SetSoundTo(m_starterNode, play: true);
	}

	public void StopStarterSound()
	{
		SetSoundTo(m_starterNode, play: false);
	}

	public void PlayPumpSound()
	{
		SetSoundTo(m_fuelPumpNode, play: true);
	}

	public void StopPumpSound()
	{
		SetSoundTo(m_fuelPumpNode, play: false);
	}
}
