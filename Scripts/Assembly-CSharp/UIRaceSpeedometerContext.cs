using System;
using Binding;
using Binding.Base;
using CarX;
using DI;
using PrefsModels;
using TMPro;
using UnityEngine;

public class UIRaceSpeedometerContext : BehaviourBindingTarget
{
	private const string KM_H = "km/h";

	private const string MP_H = "mp/h";

	private const string D7_FORMAT = "D7";

	private const string R = "R";

	private const string N = "N";

	public float colorAlphaOn = 1f;

	public float colorAlphaOff = 0.4f;

	public float colorAlphaDimmed = 0.5f;

	public Color GEAR_R = Color.red;

	public Color GEAR_N = Color.grey;

	public Color GEAR_D = Color.white;

	public Color32 zeroGrayColorOn = Color.gray;

	public Color32 zeroGrayColorOff = Color.gray;

	public Color32 zeroGrayColorDimmed = Color.gray;

	public Color redZoneBgActive = new Color32(byte.MaxValue, 1, 37, 80);

	public Color redZoneBgDisabled = new Color32(120, 0, 17, 80);

	public Color redZoneBgDimmed = new Color32(179, 0, 26, 80);

	public Color redZoneFillActive = new Color32(byte.MaxValue, 0, 10, byte.MaxValue);

	public Color redZoneFillDisabled = new Color32(144, 0, 6, 80);

	public Color redZoneFillDimmed = new Color32(183, 0, 8, 80);

	public Color whiteZoneFillActive = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);

	public Color whiteZoneFillDisabled = new Color32(188, 188, 188, 128);

	public Color whiteZoneFillDimmed = new Color32(219, 219, 219, 128);

	public Color checkActive = new Color32(242, 192, 65, byte.MaxValue);

	public Color checkDisabled = new Color32(188, 188, 188, 128);

	public Color checkDimmed = new Color32(188, 150, 54, 128);

	private FloatProperty m_currentAngle = new FloatProperty();

	private FloatProperty m_speedFill = new FloatProperty();

	private FloatProperty m_speedFillMask = new FloatProperty();

	private FloatProperty m_overheadFill = new FloatProperty();

	private FloatProperty m_speedOverheadFill = new FloatProperty();

	private BoolProperty m_handbrake = new BoolProperty(false);

	private BoolProperty m_isDrift = new BoolProperty(false);

	private StringProperty m_speedValue = new StringProperty();

	private StringProperty m_speedUnit = new StringProperty();

	public TMP_Text speedUnit;

	private int m_prevShowSpeed = -1;

	public TMP_Text distanceUnit;

	private int m_prevShowDist = -1;

	private StringProperty m_gearStr = new StringProperty();

	private ColorProperty m_gearColor = new ColorProperty(Color.white);

	private BoolProperty m_gearLimitIndication = new BoolProperty(false);

	private BoolProperty m_showMileage = new BoolProperty(startValue: true);

	private ColorProperty m_redZoneBgColor = new ColorProperty(Color.red);

	private ColorProperty m_redZoneFillColor = new ColorProperty(Color.red);

	private ColorProperty m_whiteZoneFillColor = new ColorProperty(Color.white);

	private ColorProperty m_checkEngineColor = new ColorProperty(Color.yellow);

	private bool m_prevIgnition;

	private bool m_ignition;

	private bool m_starter;

	[SerializeField]
	private float m_currentRPM;

	[SerializeField]
	private float m_currentSpeed;

	[SerializeField]
	private int m_currentGear;

	[SerializeField]
	private float m_minRPM;

	[SerializeField]
	private float m_maxRPM;

	[SerializeField]
	private float m_angleZMin;

	[SerializeField]
	private float m_minFill = 1f;

	[SerializeField]
	private float m_maxFill = 1f;

	[SerializeField]
	private float m_angleZMax;

	[SerializeField]
	private RaceCar m_car;

	private float m_rangeRPM;

	private float m_invRangeRPM;

	private int? m_prevGear;

	private bool? m_prevSpeedUnitKMH;

	private GlobalModel m_globals;

	private DriftController m_driftController;

	private GamePrefs m_gamePrefs;

	private float m_counterMiles;

	private bool m_mileageCounterInited;

	private int m_carId;

	private float revLimiter;

	public void SetCar(RaceCar car, bool showMileage = true)
	{
		InitializeIfNeed(car);
		m_car = car;
		if ((bool)m_car)
		{
			revLimiter = Mathf.Floor(m_car.carX.engineRevLimiter / 1000f) * 1000f;
			if (m_car.carX.engineRevLimiter - revLimiter < 500f)
			{
				revLimiter -= 1000f;
			}
			m_showMileage.value = showMileage;
		}
		m_prevShowDist = -1;
	}

	private void InitializeIfNeed(RaceCar car)
	{
		if (!(m_car == null) && !(car == null) && m_car.carId != car.carId && !car.isNetworkCar)
		{
			m_mileageCounterInited = false;
		}
	}

	private void Start()
	{
		m_prevGear = null;
		m_prevSpeedUnitKMH = null;
		m_gamePrefs = DependencyInjector.Resolve<GamePrefs>();
		m_globals = m_gamePrefs.globals;
		if ((bool)m_car)
		{
			m_driftController = m_car.gameObject.GetComponent<DriftController>();
		}
	}

	private void OnEnable()
	{
		if (speedUnit != null)
		{
			Color32 color = (m_starter ? zeroGrayColorDimmed : (m_ignition ? zeroGrayColorOn : zeroGrayColorOff));
			SetLeadZerosColor(speedUnit, color);
		}
	}

	private void Update()
	{
		if (BaseCar.IsNull(m_car) || m_car.IsPhysicsBroken() || m_globals == null)
		{
			return;
		}
		Color32 color = (m_starter ? zeroGrayColorDimmed : (m_ignition ? zeroGrayColorOn : zeroGrayColorOff));
		bool speedKmh = m_globals.speedKmh;
		Car carX = m_car.carX;
		m_currentRPM = carX.rpm;
		m_currentSpeed = (speedKmh ? carX.speedKMH : carX.speedMPH);
		m_currentGear = carX.gear;
		PlayerCarControl instance = PlayerCarControl.instance;
		if (instance == null)
		{
			return;
		}
		RaceCar car = instance.car;
		if (!m_mileageCounterInited)
		{
			m_mileageCounterInited = true;
			if (car.metaInfo != null)
			{
				m_carId = car.metaInfo.id;
				if (!m_gamePrefs.stats.carMileage.TryGetValue(m_carId, out var value))
				{
					m_gamePrefs.stats.carMileage.Add(m_carId, 0f);
				}
				m_counterMiles = Mathf.Max(value, 0f);
			}
		}
		m_counterMiles += car.carX.speedMPH * Time.deltaTime / 3600f;
		m_gamePrefs.stats.carMileage[m_carId] = m_counterMiles;
		float num = m_maxRPM - m_minRPM;
		if (m_rangeRPM != num)
		{
			m_rangeRPM = num;
			if (num > 0f)
			{
				m_invRangeRPM = 1f / num;
			}
		}
		if (Mathf.Abs(m_rangeRPM) > 0.001f)
		{
			float num2 = (Math.Min(m_currentRPM, revLimiter) - m_minRPM) * m_invRangeRPM;
			float value2 = m_minFill * (1f - num2) + m_maxFill * num2;
			m_speedFill.value = value2;
			float num3 = (m_currentRPM - m_minRPM) * m_invRangeRPM;
			float value3 = m_minFill * (1f - num3) + m_maxFill * num3;
			m_speedFillMask.value = value3;
			float num4 = (revLimiter - m_minRPM) * m_invRangeRPM;
			m_currentAngle.value = m_angleZMin * (1f - num4) + m_angleZMax * num4;
			float num5 = m_maxFill * (m_maxRPM - revLimiter) * m_invRangeRPM;
			m_overheadFill.value = num5;
			m_speedOverheadFill.value = Mathf.Clamp(m_maxFill * (m_currentRPM - revLimiter) * m_invRangeRPM, 0f, num5);
		}
		int num6 = Mathf.FloorToInt(m_currentSpeed);
		if (num6 != m_prevShowSpeed)
		{
			speedUnit.text = UIUtils.NumToTreeSignString(num6);
			if (num6 < 100)
			{
				SetLeadZerosColor(speedUnit, color);
			}
			m_prevShowSpeed = num6;
		}
		if (!m_prevSpeedUnitKMH.HasValue || m_prevSpeedUnitKMH.Value != speedKmh)
		{
			m_speedUnit.value = (speedKmh ? "km/h" : "mp/h");
			m_prevSpeedUnitKMH = speedKmh;
		}
		m_handbrake.value = m_car.carX.handbrake > 0f;
		if ((bool)m_driftController)
		{
			m_isDrift.value = m_driftController.isDrift;
		}
		m_prevIgnition = m_ignition;
		bool starter = m_starter;
		m_starter = m_car.engineController.isStarterOn;
		m_ignition = m_car.engineController.isIgnitionOn && !m_starter;
		bool checkEngine = m_car.engineController.checkEngine;
		bool flag = starter != m_starter;
		m_redZoneBgColor.value = (m_starter ? redZoneBgDimmed : (m_ignition ? redZoneBgActive : redZoneBgDisabled));
		m_redZoneFillColor.value = (m_starter ? redZoneFillDimmed : (m_ignition ? redZoneFillActive : redZoneFillDisabled));
		m_whiteZoneFillColor.value = (m_starter ? whiteZoneFillDimmed : (m_ignition ? whiteZoneFillActive : whiteZoneFillDisabled));
		m_checkEngineColor.value = ((checkEngine && m_starter) ? checkDimmed : (checkEngine ? checkActive : checkDisabled));
		if (m_prevIgnition != m_ignition)
		{
			if (speedUnit != null)
			{
				SetLeadZerosColor(speedUnit, color);
			}
			if (distanceUnit != null)
			{
				SetLeadZerosColor(distanceUnit, color);
			}
			m_prevGear = null;
		}
		m_gearLimitIndication.value = m_currentRPM >= revLimiter;
		float num7 = (m_car.isNetworkCar ? m_car.mileageMoreZero : m_counterMiles);
		int num8 = Mathf.FloorToInt(speedKmh ? (num7 * 1.60934f) : num7);
		if (m_prevShowDist != num8)
		{
			m_prevShowDist = num8;
			distanceUnit.text = num8.ToString("D7");
			SetLeadZerosColor(distanceUnit, color);
		}
		if (!m_prevGear.HasValue || m_prevGear.Value != m_currentGear || flag)
		{
			float a = (m_starter ? colorAlphaDimmed : (m_ignition ? colorAlphaOn : colorAlphaOff));
			if (m_currentGear > 0)
			{
				m_gearStr.value = m_currentGear.ToString();
				m_gearColor.value = new Color(GEAR_D.r, GEAR_D.g, GEAR_D.b, a);
			}
			else if (m_currentGear < 0)
			{
				m_gearStr.value = "R";
				m_gearColor.value = new Color(GEAR_R.r, GEAR_R.g, GEAR_R.b, a);
			}
			else
			{
				m_gearStr.value = "N";
				m_gearColor.value = new Color(GEAR_N.r, GEAR_N.g, GEAR_N.b, a);
			}
			m_prevGear = m_currentGear;
		}
	}

	private static void SetLeadZerosColor(TMP_Text textPro, Color32 color)
	{
		if (textPro == null)
		{
			return;
		}
		textPro.ForceMeshUpdate();
		TMP_CharacterInfo tMP_CharacterInfo = textPro.textInfo.characterInfo[0];
		int materialReferenceIndex = tMP_CharacterInfo.materialReferenceIndex;
		int num = tMP_CharacterInfo.vertexIndex;
		Color32[] colors = textPro.textInfo.meshInfo[materialReferenceIndex].colors32;
		string text = textPro.text;
		for (int i = 0; i < text.Length - 1 && text[i] == '0'; i++)
		{
			if (colors == null)
			{
				break;
			}
			colors[num] = color;
			colors[num + 1] = color;
			colors[num + 2] = color;
			colors[num + 3] = color;
			num += 4;
		}
		textPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
	}
}
