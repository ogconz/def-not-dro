using System;
using CarX;
using UnityEngine;

public class CritBrakingDetector
{
	private Car m_carX;

	private Action<bool> m_isCritBrakingInvoked;

	private bool m_callbackFlag;

	private SpeedControlConfig m_config;

	private bool m_toggled;

	private bool m_collisionsOnly = true;

	private bool m_raceDisqualifyAllowed = true;

	private float m_maxDeltaV_Mps;

	private float m_maxDeltaV;

	private float m_accumulatedBrakeDelta;

	private float m_deltaDecreaseMultiplier;

	private float m_brakeDelayTimer;

	private float m_maxBrakeDelayTimer;

	private readonly float m_maxBrakeAccel;

	public bool isCritBrakingDetected
	{
		get
		{
			if ((bool)m_carX && m_toggled)
			{
				if (!(m_brakeDelayTimer > 0f))
				{
					return m_accumulatedBrakeDelta >= m_maxDeltaV_Mps;
				}
				return true;
			}
			return false;
		}
	}

	public CritBrakingDetector(float deltaDecreseMultiplier, float maxBrakeTimerSec, float maxBrakeAccel, Action<bool> criticalBrakingCallback)
		: this(null, deltaDecreseMultiplier, maxBrakeTimerSec, maxBrakeAccel, criticalBrakingCallback)
	{
	}

	public CritBrakingDetector(Car carX, float deltaDecreaseMultiplier, float maxBrakeDelayTimerSec, float maxBrakeAccelMPS2, Action<bool> criticalBrakingCallback)
	{
		m_carX = carX;
		m_isCritBrakingInvoked = criticalBrakingCallback;
		m_maxBrakeDelayTimer = maxBrakeDelayTimerSec;
		m_deltaDecreaseMultiplier = deltaDecreaseMultiplier;
		m_maxBrakeAccel = maxBrakeAccelMPS2;
	}

	public void SetTarget(Car carX)
	{
		m_carX = carX;
	}

	public void Update(float dt)
	{
		if (!m_carX || !m_toggled || m_maxDeltaV_Mps == 0f)
		{
			return;
		}
		Vector3 vector = new Vector3(m_carX.sideAccel, m_carX.vertAccel, m_carX.longAccel);
		float num = Vector3.Dot(m_carX.transform.rotation * vector, m_carX.velocity.normalized);
		if (num < 0f && 0f - num > m_maxBrakeAccel)
		{
			m_accumulatedBrakeDelta += (0f - num) * dt;
			if (m_accumulatedBrakeDelta >= m_maxDeltaV_Mps)
			{
				m_accumulatedBrakeDelta = m_maxDeltaV_Mps;
				if (m_brakeDelayTimer == 0f)
				{
					m_brakeDelayTimer = m_maxBrakeDelayTimer;
					if (m_isCritBrakingInvoked != null && !m_callbackFlag && !m_collisionsOnly)
					{
						m_isCritBrakingInvoked.SafeCall(m_raceDisqualifyAllowed);
						m_callbackFlag = true;
					}
				}
			}
		}
		else if (m_accumulatedBrakeDelta > 0f)
		{
			m_accumulatedBrakeDelta -= m_deltaDecreaseMultiplier * m_carX.longAccel * dt;
			if (m_accumulatedBrakeDelta < 0f)
			{
				Reset();
			}
		}
		if (m_brakeDelayTimer > 0f)
		{
			m_brakeDelayTimer -= dt;
			if (m_brakeDelayTimer < 0f)
			{
				Reset();
			}
		}
	}

	private void Reset()
	{
		m_brakeDelayTimer = 0f;
		m_accumulatedBrakeDelta = 0f;
		m_callbackFlag = false;
	}

	public void ResetFull()
	{
		Reset();
		m_maxDeltaV = 0f;
	}

	public void SetSpeedControl(bool toggle, bool collisionOnly, bool raceDisqualifyAllowed, float maxDeltaV)
	{
		m_toggled = toggle;
		m_maxDeltaV_Mps = maxDeltaV / 3.6f;
		m_maxDeltaV = maxDeltaV;
		m_collisionsOnly = collisionOnly;
		m_raceDisqualifyAllowed = raceDisqualifyAllowed;
		if (!m_toggled || maxDeltaV <= 0f)
		{
			m_accumulatedBrakeDelta = 0f;
		}
	}

	public float GetAccumulatedBrake()
	{
		return m_accumulatedBrakeDelta * 3.6f;
	}

	public float GetMaxAccumulatedBrake()
	{
		return m_maxDeltaV;
	}
}
