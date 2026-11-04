using System;
using CarX;
using FMOD.Studio;
using UnityEngine;

public class FMODCarTransmission : FMODBaseScript
{
	private const string defaultSeqTransmissionEvent = "event:/GameEvents/Gearbox/Transmission";

	private FMODEventNode m_seqTransmissionNode;

	private FMODEventNode.Parameter m_seqTransmissionRpm;

	private FMODEventNode.Parameter m_seqTransmissionLoad;

	private bool m_isSeqTransmissionAvailable;

	private float m_cachedLastCalculatedVolume = 1f;

	protected override bool OnEnableHandler()
	{
		if (CreateNode(ref m_seqTransmissionNode, "event:/GameEvents/Gearbox/Transmission"))
		{
			m_seqTransmissionLoad = m_seqTransmissionNode.RegisterParameter("Load");
			m_seqTransmissionRpm = m_seqTransmissionNode.RegisterParameter("RPM");
		}
		UpdateSeqTransmissionAvailabilityHandler(base.raceCar);
		base.raceCar.OnSetDescEvent += UpdateSeqTransmissionAvailabilityHandler;
		return base.OnEnableHandler();
	}

	protected override void OnDisableHandler()
	{
		base.raceCar.OnSetDescEvent -= UpdateSeqTransmissionAvailabilityHandler;
		base.OnDisableHandler();
	}

	private void UpdateSeqTransmissionAvailabilityHandler(BaseCar baseCar)
	{
		RaceCar raceCar = baseCar as RaceCar;
		m_isSeqTransmissionAvailable = raceCar != null && raceCar.isSequentialTransmissionAvailable;
	}

	protected override void UpdateSound(float dt)
	{
		if (m_isSeqTransmissionAvailable)
		{
			CameraSwitch instance = CameraSwitch.instance;
			if ((bool)instance && instance.targetRaceCar == base.raceCar)
			{
				UpdateSequentialTransmissionSound();
			}
			else if (m_seqTransmissionNode.IsPlaying())
			{
				m_seqTransmissionNode.Stop(STOP_MODE.IMMEDIATE);
			}
		}
		base.UpdateSound(dt);
	}

	protected override float GetVolume()
	{
		return base.GetVolume() * m_prefs.globals.seqTransmisonVolume * m_cachedLastCalculatedVolume;
	}

	private void UpdateSequentialTransmissionSound()
	{
		if (!(base.raceCar == null))
		{
			Car carX = base.raceCar.carX;
			float load = carX.load;
			int gear = carX.gear;
			float num = 1f;
			float f = (carX.GetWheel(WheelIndex.RearLeft).w + carX.GetWheel(WheelIndex.RearRight).w) * 0.5f;
			if (carX.gearType == GearType.GEAR_FWD)
			{
				f = (carX.GetWheel(WheelIndex.FrontLeft).w + carX.GetWheel(WheelIndex.FrontRight).w) * 0.5f;
			}
			float num2 = carX.finaldrive * Mathf.Abs(f);
			if (gear != 0)
			{
				num2 *= 30f / MathF.PI;
				num2 = num2 * carX.GetGearRatio(gear) / Mathf.Sqrt(carX.GetGearRatio(gear));
			}
			else
			{
				num2 = 0f;
			}
			if (carX.handbrake > 0.1f)
			{
				num = 0f;
			}
			if (num2 < 100f)
			{
				num = 0f;
			}
			else if (num2 < 200f)
			{
				float num3 = (num2 - 100f) / 100f;
				num *= num3;
			}
			load = Mathf.Clamp01(load);
			PlaySequentialTransmissionSound(num2, load, num);
		}
	}

	private void PlaySequentialTransmissionSound(float rpm, float load, float volume)
	{
		m_seqTransmissionLoad.SetValue(load);
		m_seqTransmissionRpm.SetValue(rpm);
		m_cachedLastCalculatedVolume = volume;
		if (!m_seqTransmissionNode.IsPlaying())
		{
			m_seqTransmissionNode.Start();
		}
	}
}
