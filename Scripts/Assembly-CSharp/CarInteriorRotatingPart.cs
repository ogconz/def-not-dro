using System;
using DI;
using GameInput;
using UnityEngine;

[AddComponentMenu("Scripts/Car/CarInteriorRotatingPart")]
public class CarInteriorRotatingPart : MonoBehaviour
{
	public enum RotatingPart
	{
		DrivingWheel = 0,
		SpeedArrow = 1,
		TahoArrow = 2
	}

	public RotatingPart part;

	public float maxAngle;

	public float maxValue;

	public Vector3 axis;

	private RaceCar m_raceCar;

	private Transform m_cachedTransform;

	private Quaternion m_baseRotation;

	private float m_invMaxValue;

	private int m_maxDrivingWheelAngle;

	[Dependency]
	private GamePrefs m_prefs;

	private void SetupDrivingWheelAngle()
	{
		if (m_prefs != null)
		{
			m_maxDrivingWheelAngle = (int)InputManager.settings.Params.wheelParams.steeringWheelAngle;
		}
	}

	private void Awake()
	{
		this.MakeDependenciesInjected();
		m_cachedTransform = base.transform;
		m_baseRotation = m_cachedTransform.localRotation;
		m_raceCar = GetComponentInParent<RaceCar>();
		if (!m_raceCar)
		{
			base.enabled = false;
		}
		else if (Mathf.Abs(maxValue) > 0.0001f)
		{
			m_invMaxValue = 1f / maxValue;
		}
	}

	private void OnEnable()
	{
		if (part == RotatingPart.DrivingWheel)
		{
			SetupDrivingWheelAngle();
			InputSettings.WheelParams wheelParams = InputManager.settings.Params.wheelParams;
			wheelParams.SteeringWheelAngleChanged = (System.Action)Delegate.Combine(wheelParams.SteeringWheelAngleChanged, new System.Action(SetupDrivingWheelAngle));
		}
	}

	private void OnDisable()
	{
		InputSettings.WheelParams wheelParams = InputManager.settings.Params.wheelParams;
		wheelParams.SteeringWheelAngleChanged = (System.Action)Delegate.Remove(wheelParams.SteeringWheelAngleChanged, new System.Action(SetupDrivingWheelAngle));
	}

	private void Update()
	{
		float angle = 0f;
		switch (part)
		{
		case RotatingPart.DrivingWheel:
			angle = (0f - m_raceCar.GetSmoothSteer()) * (float)m_maxDrivingWheelAngle * 0.5f;
			break;
		case RotatingPart.SpeedArrow:
			angle = ((!m_prefs.globals.speedKmh) ? (maxAngle * Mathf.Clamp01(m_raceCar.carX.speedMPH * m_invMaxValue)) : (maxAngle * Mathf.Clamp01(m_raceCar.carX.speedKMH * m_invMaxValue)));
			break;
		case RotatingPart.TahoArrow:
			angle = maxAngle * Mathf.Clamp01(m_raceCar.carX.rpm * m_invMaxValue);
			break;
		}
		Quaternion quaternion = m_baseRotation * Quaternion.AngleAxis(angle, axis);
		if (!quaternion.IsRotationHaveNANs())
		{
			m_cachedTransform.localRotation = quaternion;
		}
	}
}
