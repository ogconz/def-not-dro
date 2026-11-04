using System.Collections;
using CarX;
using FMOD.Studio;
using FMODSctipts.Utils;
using UnityEngine;

public class FMODCarTurbo : FMODBaseScript
{
	public class TurboBoostNegativer
	{
		private const float LOWER_LIMIT = -0.25f;

		private const float DROP_SPEED = 0.5f;

		private float m_lastPositive;

		public float value { get; set; }

		public float UpdateValue(float targetValue, float dt)
		{
			if (Mathf.Approximately(targetValue, 0f))
			{
				value = Mathf.Clamp(value - 0.5f * dt, -0.25f, value);
			}
			else if (targetValue > value)
			{
				float num = targetValue - m_lastPositive;
				float num2 = ((num > 0f) ? 2f : 0.5f);
				value += num2 * num;
				value = Mathf.Clamp(value, value, targetValue);
			}
			else if (targetValue < value)
			{
				value = targetValue;
			}
			m_lastPositive = targetValue;
			return value;
		}

		public void Reset()
		{
			value = 0f;
			m_lastPositive = 0f;
		}
	}

	private static readonly string TurboSoundSettingsPath = "TurboSoundSettings";

	private static readonly string DefaultTurboEvent = "event:/GameEvents/Cars/Default/Turbine";

	private static readonly string DefaultTurboBlowOffEvent = "event:/GameEvents/Cars/Default/Blow-off";

	private FMODEventNode m_turboNode;

	private FMODEventNode.Parameter m_turboNodeRPMParam;

	private FMODEventNode m_blowOffNode;

	private FMODEventNode.Parameter m_blowOffNodeParam;

	private InertiaController m_inertiaCtrl;

	private BlowoffController m_blowOffCtrl;

	private TurboBoostNegativer m_boostNegativer = new TurboBoostNegativer();

	private Coroutine m_delayedBlowOffCoroutine;

	private float m_carMaxPowerHP = -1f;

	private bool m_allowedToPlayNodes = true;

	private static TurboSoundSettings s_settings;

	protected override bool OnEnableHandler()
	{
		if (!base.raceCar)
		{
			return false;
		}
		if (s_settings == null)
		{
			s_settings = InitSettings(TurboSoundSettingsPath);
		}
		m_inertiaCtrl = new InertiaController(s_settings.brakingInertia, s_settings.acceleratingInertia);
		m_blowOffCtrl = new BlowoffController();
		RefreshSound();
		if (base.raceCar.IsVisualLoaded)
		{
			InitMaxPowerHP(base.raceCar);
		}
		else
		{
			base.raceCar.OnCarVisualLoadFinishEvent += InitMaxPowerHP;
		}
		return base.OnEnableHandler();
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
		m_turboNode?.Release();
		if (CreateCustomCarNamedNode(ref m_turboNode, eventRef, metaName, "Turbine", DefaultTurboEvent))
		{
			m_turboNodeRPMParam = m_turboNode.RegisterParameter("TurboRPM");
		}
		m_blowOffNode?.Release();
		if (CreateCustomCarNamedNode(ref m_blowOffNode, eventRef, metaName, "Blow-off", DefaultTurboBlowOffEvent))
		{
			m_blowOffNodeParam = m_blowOffNode.RegisterParameter("BlowOffPower");
		}
		base.RefreshSound();
	}

	protected override void UpdateSound(float dt)
	{
		base.UpdateSound(dt);
		if (!(base.raceCar == null))
		{
			Car carX = base.raceCar.carX;
			if (carX != null && carX.engineTurboCharged && m_allowedToPlayNodes)
			{
				carX.GetEngineTurboParams(out var _, out var curTurboPressure, out var _);
				float targetValue = curTurboPressure / 2f;
				targetValue = m_boostNegativer.UpdateValue(targetValue, dt);
				float value = ((m_carMaxPowerHP > 0f) ? (carX.engineCurPower / m_carMaxPowerHP) : 0f);
				float accelerate = carX.accelerate;
				PlayTurboSound(Mathf.Clamp01(value), Mathf.Clamp01(targetValue), dt, out var turboRPM);
				ToggleBlowOff(accelerate, targetValue, turboRPM, 2f, dt);
			}
			else
			{
				StopAll();
			}
		}
	}

	public void SetTurboSoundsEnable(bool enable)
	{
		m_allowedToPlayNodes = enable;
		if (!m_allowedToPlayNodes)
		{
			StopAll();
		}
		ResetControllers();
	}

	protected override STOP_MODE GetStopMode()
	{
		return STOP_MODE.IMMEDIATE;
	}

	private void ResetControllers()
	{
		m_inertiaCtrl?.Reset();
		m_blowOffCtrl?.Reset();
		m_boostNegativer?.Reset();
	}

	protected override void OnDisableHandler()
	{
		StopAll();
		base.raceCar.OnCarVisualLoadFinishEvent -= InitMaxPowerHP;
		base.OnDisableHandler();
	}

	private void InitMaxPowerHP(RaceCar raceCar)
	{
		Car car = (raceCar ? raceCar.carX : null);
		if (car == null)
		{
			GameLog.Print(AlertLevel.FatalError, "Sound", "Car is NULL! Cannot calculate max car engine power");
		}
		else
		{
			car.CalcMaxPowerMaxTorque(out m_carMaxPowerHP, out var _, out var _, out var _);
		}
	}

	private void StopAll()
	{
		SetSoundTo(m_turboNode, play: false);
		SetSoundTo(m_blowOffNode, play: false);
		if (m_delayedBlowOffCoroutine != null)
		{
			TimeController.StopNow(m_delayedBlowOffCoroutine);
			m_delayedBlowOffCoroutine = null;
		}
	}

	private static TurboSoundSettings InitSettings(string settingsPath)
	{
		TurboSoundSettings turboSoundSettings = ResourcesWrapper.Load<TurboSoundSettings>(settingsPath);
		if (turboSoundSettings == null)
		{
			turboSoundSettings = TurboSoundSettings.Default;
			GameLog.Print(AlertLevel.Warning, "Sound", "Unable to load saved TurboSoundSettings instance! Default was created.");
		}
		return turboSoundSettings;
	}

	private void PlayTurboSound(float powerNorm, float boostNormalized, float dt, out float turboRPM)
	{
		if (m_turboNode == null || m_turboNodeRPMParam == null)
		{
			GameLog.Print(AlertLevel.Error, "Sound", "Turbo node has not been initialized and you are trying to play it!");
			turboRPM = 0f;
			return;
		}
		if (!m_turboNode.IsPlaying())
		{
			m_turboNode.Start();
		}
		float target = s_settings.CalculateTurboRPM(powerNorm, boostNormalized);
		turboRPM = Mathf.Clamp01(m_inertiaCtrl.UpdateValueQuadratic(target, dt, 5f));
		m_turboNodeRPMParam.SetValue(turboRPM);
	}

	private IEnumerator DelayedBlowOff(float delaySec, float initialTurboRPM, float initialBoost, float maxBoost)
	{
		if (initialBoost < s_settings.blowOffMinValue)
		{
			m_delayedBlowOffCoroutine = null;
			yield break;
		}
		float startTime = Time.time;
		while (Time.time - startTime < delaySec)
		{
			yield return null;
		}
		if (m_blowOffNode == null || m_blowOffNodeParam == null)
		{
			GameLog.Print(AlertLevel.FatalError, "Sound", "Blow-off sound node or parameter is NULL! Cannot playback effect!");
			m_delayedBlowOffCoroutine = null;
			yield break;
		}
		float value = initialTurboRPM * maxBoost;
		if (!m_blowOffNode.IsPlaying())
		{
			m_blowOffNodeParam.SetValue(value);
			m_blowOffNode.Start();
		}
		m_delayedBlowOffCoroutine = null;
	}

	private void ToggleBlowOff(float throttleNormalized, float boostNormalized, float turboRPM, float maxBoost, float dt)
	{
		if (m_blowOffNode == null || m_blowOffNodeParam == null)
		{
			GameLog.Print(AlertLevel.Error, "Sound", "Turbo Blow-of sound node has not been initialized and you are trying to toggle it!");
			return;
		}
		m_blowOffCtrl.Update(m_blowOffNode.IsPlaying() || m_delayedBlowOffCoroutine != null, throttleNormalized, boostNormalized, dt, delegate(bool shouldPlay)
		{
			if (shouldPlay)
			{
				if (m_delayedBlowOffCoroutine == null && !m_blowOffNode.IsPlaying())
				{
					m_delayedBlowOffCoroutine = TimeController.StartNow(DelayedBlowOff(s_settings.blowOffLagSec, turboRPM, boostNormalized, maxBoost));
				}
			}
			else
			{
				if (m_blowOffNode.IsPlaying())
				{
					m_blowOffNode.Stop(STOP_MODE.ALLOWFADEOUT);
				}
				if (m_delayedBlowOffCoroutine != null)
				{
					TimeController.StopNow(m_delayedBlowOffCoroutine);
					m_delayedBlowOffCoroutine = null;
				}
			}
		});
	}
}
