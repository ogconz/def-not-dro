using Binding;
using Binding.Base;
using PrefsModels;
using UnityEngine;

public class UIHUDDriftAnglesContext : BehaviourBindingTarget
{
	protected GlobalModel m_globals;

	private DriftController m_driftController;

	private IntProperty m_angleNum = new IntProperty(0);

	private FloatProperty m_imgFillVal = new FloatProperty(0f);

	private FloatProperty m_angleCar = new FloatProperty(0f);

	private FloatProperty m_alpha = new FloatProperty(0f);

	private ColorProperty m_arrowColor = new ColorProperty(Color.white);

	private RaceCar m_raceCar;

	private float m_driftAngle;

	private float m_speed;

	private bool m_needForceUpdate = true;

	private const float maxLimit = 120f;

	private const float angleMax = 20f;

	private const float fadeInSpeed = 2f;

	private const float fadeOutSpeed = 5f;

	private const float preLimitValue = 85f;

	private float dtTemp;

	private Color idleColor = Color.white;

	private Color limitColor = Color.red;

	private void OnDisable()
	{
		m_alpha.value = 0f;
		m_angleNum.value = 0;
		m_imgFillVal.value = 0f;
		m_angleCar.value = 0f;
		m_arrowColor.value = idleColor;
	}

	public void Init(GlobalModel globals, DriftController driftController)
	{
		m_globals = globals;
		m_driftController = driftController;
	}

	public void SetCar(RaceCar raceCar, bool ignorePrefsSetting = false)
	{
		m_raceCar = raceCar;
		m_needForceUpdate = !ignorePrefsSetting;
	}

	private void LateUpdate()
	{
		GlobalModel globals = m_globals;
		if (((globals != null && globals.inclinometerMode == GlobalModel.InclinometerMode.Off) & m_needForceUpdate) || m_driftController == null || m_raceCar == null)
		{
			if (m_alpha.value > 0f)
			{
				m_alpha.value = 0f;
			}
		}
		else
		{
			Refresh(Time.deltaTime);
		}
	}

	private void Refresh(float dt)
	{
		m_driftController.CalcDriftAngleAndForwardSide(out var driftAngle, out var movementType, m_raceCar);
		m_driftAngle = Mathf.Clamp(driftAngle, 0f, 120f);
		m_speed = m_raceCar.carX.speedKMH;
		if (m_raceCar.carX.gear == -1 && movementType == DriftController.MovementType.Fail)
		{
			m_driftAngle = Mathf.Clamp(180f - driftAngle, 0f, 120f);
		}
		if (m_speed <= 0.5f || m_raceCar.offRoad)
		{
			m_angleNum.value = Mathf.FloorToInt(0f);
			m_angleCar.value = Mathf.LerpAngle(m_angleCar.value, 0f, dt);
		}
		else
		{
			m_angleNum.value = Mathf.FloorToInt(m_driftAngle);
			m_angleCar.value = m_driftController.signAngle * Mathf.Lerp(0f, 20f, m_driftAngle / 120f);
		}
		m_imgFillVal.value = m_angleNum.value;
		FadeAndFill(dt);
	}

	private void FadeAndFill(float dt)
	{
		if (m_globals.inclinometerMode == GlobalModel.InclinometerMode.On)
		{
			if (m_alpha.value < 1f)
			{
				m_alpha.value = 1f;
			}
		}
		else if (m_globals.inclinometerMode == GlobalModel.InclinometerMode.AutoHide)
		{
			float b = 0f;
			float num = 2f;
			if (m_driftController.isDrift || m_driftController.isDriftOtherCar(m_raceCar))
			{
				b = 1f;
				num = 5f;
			}
			m_alpha.value = Mathf.Lerp(m_alpha.value, b, dt * num);
		}
		if (m_speed <= 0.5f)
		{
			dtTemp = 0f;
		}
		else if (m_driftAngle >= 85f)
		{
			dtTemp = (m_driftAngle - 85f) / 35f;
		}
		else
		{
			dtTemp = Mathf.Lerp(dtTemp, 0f, dt);
		}
		m_arrowColor.value = Color.Lerp(idleColor, limitColor, dtTemp);
	}
}
