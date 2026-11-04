using Binding;
using Binding.Base;
using DI;
using PrefsModels;
using UnityEngine;

public class UIBoostGaugeContext : BehaviourBindingTarget
{
	private const string ZERO_VALUE_TEXT = "0";

	private const float LOW_BOOST_MAX = 0.3f;

	[SerializeField]
	private float m_gaugeFillAngle = 270f;

	private readonly BoolProperty m_boostGaugeVisible = new BoolProperty();

	private readonly StringProperty m_zeroValue = new StringProperty();

	private readonly StringProperty m_mid0Value = new StringProperty();

	private readonly StringProperty m_mid1Value = new StringProperty();

	private readonly StringProperty m_maxValue = new StringProperty();

	private readonly FloatProperty m_gaugeFill = new FloatProperty();

	private readonly FloatProperty m_gaugeFillNegative = new FloatProperty();

	private readonly BoolProperty m_ignitionEnabled = new BoolProperty(false);

	private RaceCar m_car;

	private bool m_turboCharged;

	private bool m_forceSetVisible;

	private float m_maxPressure;

	private float m_gaugeFillScale;

	private GlobalModel m_globals;

	private FMODCarTurbo.TurboBoostNegativer m_boostNegativer = new FMODCarTurbo.TurboBoostNegativer();

	private void Awake()
	{
		m_globals = DependencyInjector.Resolve<GamePrefs>().globals;
	}

	private void Start()
	{
		BaseRaceState baseRaceState = States.current as BaseRaceState;
		SetCar((baseRaceState != null) ? baseRaceState.userCar : null);
	}

	private void OnEnable()
	{
		UpdateGaugeVisibility();
	}

	private void Update()
	{
		if (m_boostGaugeVisible.value && !BaseCar.IsNull(m_car))
		{
			m_car.carX.GetEngineTurboParams(out var _, out var curTurboPressure, out var _);
			float num = m_boostNegativer.UpdateValue(curTurboPressure / m_maxPressure, Time.deltaTime);
			if (m_maxPressure != 0f)
			{
				m_gaugeFill.value = Mathf.Clamp(num, 0f, 1f) * m_gaugeFillScale;
				m_gaugeFillNegative.value = ((num > 0f) ? 0f : (0f - num));
			}
			m_ignitionEnabled.value = m_car.engineController.isIgnitionOn;
		}
	}

	public void SetCar(RaceCar car, bool forceVisible = false)
	{
		if (!(car == null))
		{
			m_car = car;
			m_turboCharged = m_car != null && m_car.carX.engineTurboCharged;
			m_forceSetVisible = forceVisible;
			UpdateGaugeVisibility();
			UpdateGaugeValues();
		}
	}

	private void UpdateGaugeValues()
	{
		if (m_turboCharged)
		{
			m_car.carX.GetEngineTurboParams(out m_maxPressure, out var _, out var _);
			m_zeroValue.value = "0";
			if (Mathf.Approximately(m_maxPressure, 0f))
			{
				m_turboCharged = false;
				UpdateGaugeVisibility();
			}
			else if (m_maxPressure > 0.3f)
			{
				m_mid0Value.value = $"{m_maxPressure * 0.33f:F1}";
				m_mid1Value.value = $"{m_maxPressure * 0.66f:F1}";
				m_maxValue.value = $"{m_maxPressure:F1}";
				m_gaugeFillScale = m_gaugeFillAngle / 360f;
			}
			else
			{
				m_mid0Value.value = "0.1";
				m_mid1Value.value = "0.2";
				m_maxValue.value = "0.3";
				m_gaugeFillScale = m_gaugeFillAngle * (m_maxPressure / 0.3f) / 360f;
			}
		}
	}

	private void UpdateGaugeVisibility()
	{
		if (m_globals != null)
		{
			m_boostGaugeVisible.value = (m_globals.displayBoostGauge || m_forceSetVisible) && m_turboCharged;
		}
	}
}
