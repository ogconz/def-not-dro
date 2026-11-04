using CarX;
using FMOD.Studio;
using UnityEngine;

public class FMODTyreSound : FMODBaseScript
{
	private class MaterialSound
	{
		public string name;

		public bool lastPlayingState;

		public bool playingState;

		public FMODEventNode node;

		public FMODEventNode.Parameter velocity;

		public FMODEventNode.Parameter pitch;

		public FMODEventNode.Parameter sr;

		public FMODEventNode.Parameter sa;

		public FMODEventNode.Parameter saFront;

		public FMODEventNode.Parameter slipFrontParam;

		public FMODEventNode.Parameter rpmParam;

		public FMODEventNode.Parameter hpParam;

		internal void SendParamsToFMOD(float velocityValue, float rpm, float pitchValue, float srValue, float saValue, float saFrontValue, float slipFront, float hp)
		{
			if (playingState)
			{
				if (!lastPlayingState)
				{
					node.Start();
					lastPlayingState = true;
				}
				velocity.SetValue(velocityValue);
				rpmParam.SetValue(rpm);
				if (pitch != null)
				{
					pitch.SetValue(pitchValue);
				}
				if (sr != null)
				{
					sr.SetValue(srValue);
				}
				if (sa != null)
				{
					sa.SetValue(saValue);
				}
				if (saFront != null)
				{
					saFront.SetValue(saFrontValue);
				}
				if (slipFrontParam != null)
				{
					slipFrontParam.SetValue(slipFront);
				}
				if (hpParam != null)
				{
					hpParam.SetValue(hp);
				}
				playingState = false;
			}
			else if (lastPlayingState)
			{
				node.Stop(STOP_MODE.IMMEDIATE);
				lastPlayingState = false;
			}
		}

		internal void SetCarSpotVolume(float value)
		{
			node.SetCarSpotVolume(value);
		}
	}

	private float m_sTime;

	private MaterialSound[] m_skidSounds;

	private MaterialSound[] m_rollSounds;

	private FMODEventNode m_brakeNode;

	private FMODEventNode.Parameter m_brakeParam;

	private bool m_lastBrakeActive;

	private readonly MathTools.SimpleMovingAvgFilter m_slipFiltered = new MathTools.SimpleMovingAvgFilter(10);

	private readonly MathTools.SimpleMovingAvgFilter m_hpFiltered = new MathTools.SimpleMovingAvgFilter(25);

	private float m_hpDebug;

	private float m_slipDebug;

	protected override bool OnEnableHandler()
	{
		if (!base.raceCar)
		{
			return false;
		}
		int allTemplatesCount = CARXSurfaceManager.allTemplatesCount;
		m_skidSounds = new MaterialSound[allTemplatesCount];
		m_rollSounds = new MaterialSound[allTemplatesCount];
		m_slipFiltered.Clear();
		m_hpFiltered.Clear();
		if (CreateCarNamedNode(ref m_brakeNode, "event:/GameEvents/Cars/Brake"))
		{
			m_brakeParam = m_brakeNode.RegisterParameter("Brake");
		}
		return base.OnEnableHandler();
	}

	protected override void OnDisableHandler()
	{
		m_skidSounds = null;
		m_rollSounds = null;
		m_brakeNode = null;
		base.OnDisableHandler();
	}

	private void FilterSlipValue(Car carX)
	{
		if (!(carX == null))
		{
			Wheel wheel = carX.GetWheel(WheelIndex.FrontLeft);
			float num = Mathf.Sqrt(wheel.sa * wheel.sa + wheel.sr * wheel.sr);
			wheel = carX.GetWheel(WheelIndex.FrontRight);
			num += Mathf.Sqrt(wheel.sa * wheel.sa + wheel.sr * wheel.sr);
			num /= 2f;
			m_slipFiltered.AddValue(num);
		}
	}

	private void FilterHpValue(Car carX)
	{
		if (!(carX == null))
		{
			float newValue = Mathf.Clamp(carX.engineCurPower, 0f, 1500f);
			m_hpFiltered.AddValue(newValue);
		}
	}

	protected override void UpdateSound(float dt)
	{
		m_sTime += dt;
		if (m_sTime > 0.15f)
		{
			m_sTime = 0f;
		}
		Car carX = base.raceCar.carX;
		FilterHpValue(carX);
		FilterSlipValue(carX);
		UpdateBrakeSound(carX);
		float velocityValue = Mathf.Clamp01(carX.speedMPS * 0.05f) * 100f;
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		int num5 = 0;
		float num6 = 0f;
		int num7 = 0;
		float rpm = carX.rpm;
		float hp = (m_hpDebug = m_hpFiltered.value);
		float slipFront = (m_slipDebug = m_slipFiltered.value);
		for (int i = 0; i < 4; i++)
		{
			Wheel wheel = carX.GetWheel((WheelIndex)i);
			if (!wheel.enabled)
			{
				continue;
			}
			switch (i)
			{
			case 2:
			case 3:
				num3 += wheel.sr;
				num4 += Mathf.Abs(wheel.sa);
				num5++;
				break;
			case 0:
			case 1:
				num6 += Mathf.Abs(wheel.sa);
				num7++;
				break;
			}
			if (!wheel.GetUserData(out var userData) || userData < 0)
			{
				continue;
			}
			bool flag = false;
			bool flag2 = false;
			GetSkidParams(m_sTime, wheel, out var volume);
			num = Mathf.Max(num, volume);
			flag = carX.speedKMH > 3f;
			flag2 = num > 0.01f;
			float num8 = Mathf.Abs(wheel.w - wheel.w0);
			if (num8 < 0f)
			{
				num8 = 0f;
			}
			if (num8 > num2)
			{
				num2 = num8;
			}
			if (userData < m_rollSounds.Length)
			{
				if (m_rollSounds[userData] == null)
				{
					m_rollSounds[userData] = CreateSound(userData, isSkid: false);
				}
				if (m_rollSounds[userData] != null)
				{
					m_rollSounds[userData].playingState = flag;
				}
			}
			if (userData < m_skidSounds.Length)
			{
				if (m_skidSounds[userData] == null)
				{
					m_skidSounds[userData] = CreateSound(userData, isSkid: true);
				}
				if (m_skidSounds[userData] != null)
				{
					m_skidSounds[userData].playingState = flag2;
				}
			}
		}
		if (num5 > 0)
		{
			num3 /= (float)num5;
			num4 /= (float)num5;
		}
		if (num7 > 0)
		{
			num6 /= (float)num7;
		}
		float volume2 = GetVolume();
		for (int j = 0; j < m_skidSounds.Length; j++)
		{
			MaterialSound materialSound = m_skidSounds[j];
			if (materialSound != null)
			{
				materialSound.SendParamsToFMOD(num * 100f, rpm, 0.75f + num2 * 0.005f, num3, num4, num6, slipFront, hp);
				materialSound.SetCarSpotVolume(volume2);
			}
			MaterialSound materialSound2 = m_rollSounds[j];
			if (materialSound2 != null)
			{
				materialSound2.SendParamsToFMOD(velocityValue, rpm, 0f, num3, num4, num6, slipFront, hp);
				materialSound2.SetCarSpotVolume(volume2);
			}
		}
		base.UpdateSound(dt);
	}

	private void UpdateBrakeSound(Car carX)
	{
		bool flag = carX.brake > 0.05f;
		if (m_lastBrakeActive != flag)
		{
			if (flag)
			{
				m_brakeNode.Start();
			}
			m_lastBrakeActive = flag;
		}
		m_brakeParam.SetValue(carX.brake);
		m_brakeNode.SetCarSpeedMPHValue(carX.speedMPH);
		m_brakeNode.SetCarSpotVolume(GetVolume());
	}

	private MaterialSound CreateSound(int materialIndex, bool isSkid)
	{
		CARXSurfaceTemplate cARXSurfaceTemplate = CARXSurfaceManager.Get(materialIndex);
		if (cARXSurfaceTemplate == null)
		{
			return null;
		}
		MaterialSound materialSound = new MaterialSound();
		materialSound.name = string.Format("{0}-{1} [{2}]", isSkid ? "Skid" : "Roll", cARXSurfaceTemplate.name, materialIndex);
		if (CreateNode(ref materialSound.node, isSkid ? cARXSurfaceTemplate.wheelSfxData.skidSoundEvent : cARXSurfaceTemplate.wheelSfxData.rollSoundEvent))
		{
			materialSound.velocity = materialSound.node.RegisterParameter("Velocity");
			materialSound.rpmParam = materialSound.node.RegisterParameter("RPM");
			materialSound.sr = materialSound.node.RegisterParameter("SR");
			materialSound.sa = materialSound.node.RegisterParameter("SA");
			materialSound.saFront = materialSound.node.RegisterParameter("SA_Front");
			materialSound.slipFrontParam = materialSound.node.RegisterParameter("SlipFront");
			materialSound.hpParam = materialSound.node.RegisterParameter("HP");
			if (isSkid)
			{
				materialSound.pitch = materialSound.node.RegisterParameter("Pitch");
			}
		}
		return materialSound;
	}

	private static void GetSkidParams(float sTime, Wheel wheel, out float volume)
	{
		if (wheel.saVol < 1.4f && wheel.srVol < 0.6f)
		{
			volume = 0f;
			return;
		}
		float num = Mathf.Clamp01(wheel.saVol - 1.4f) + Mathf.Clamp01(wheel.srVol - 0.6f);
		float num2 = sTime * 0.67f + 0.9f;
		volume = num2 * num * Mathf.Min(wheel.vertForce.y * 0.0003f, 2f);
	}
}
