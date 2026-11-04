using System;
using GameInput;
using UnityEngine;

public class RawForceCalculator
{
	private float m_impactEffect;

	private float m_grassEffect;

	private float m_groundEffect;

	private bool m_stopEffectsRequested;

	private float m_constEffect;

	private const int GRASS_PERIOD = 440;

	private const int GROUND_PERIOD = 140;

	private const int IMPACT_PERIOD = 800;

	private int m_timeMS;

	public bool onlyConstEffect;

	private Action<float> m_setRawForce;

	public RawForceCalculator(Action<float> setRawForce)
	{
		m_setRawForce = setRawForce;
	}

	public void SetConstantForce(float k)
	{
		k *= 0.75f;
		InputSettings.Parameters parameters = InputManager.settings.Params;
		m_constEffect = Mathf.Clamp((0f - k) * parameters.wheelParams.forceFeedback, -1f, 1f);
	}

	public void PlayImpactEffect(float k)
	{
		k = InputManager.settings.Params.wheelParams.forceFeedback * k;
		m_impactEffect = Mathf.Max(m_impactEffect, k);
	}

	public void PlayGrassSurfaceEffect(float k)
	{
		k = InputManager.settings.Params.wheelParams.forceFeedback * k;
		m_grassEffect = Mathf.Max(m_grassEffect, k);
	}

	public void PlayGravelSurfaceEffect(float k)
	{
		k = InputManager.settings.Params.wheelParams.forceFeedback * k;
		m_groundEffect = Mathf.Max(m_groundEffect, k);
	}

	public void StopEffects(bool instant = true)
	{
		m_stopEffectsRequested = true;
		if (instant)
		{
			UpdateStoppingEffects();
		}
	}

	private void UpdateStoppingEffects()
	{
		if (m_stopEffectsRequested)
		{
			m_stopEffectsRequested = false;
			m_groundEffect = 0f;
			m_impactEffect = 0f;
			m_grassEffect = 0f;
			if (m_setRawForce != null)
			{
				m_setRawForce(0f);
			}
		}
	}

	public void Update(bool disabled)
	{
		UpdateStoppingEffects();
		if (disabled)
		{
			return;
		}
		float deltaTime = Time.deltaTime;
		m_grassEffect = Mathf.Max(0f, m_grassEffect - deltaTime);
		m_groundEffect = Mathf.Max(0f, m_groundEffect - deltaTime);
		m_impactEffect = Mathf.Max(0f, m_impactEffect - deltaTime * 2f);
		float num = m_constEffect;
		m_timeMS += Mathf.FloorToInt(deltaTime * 10000f) % 10000;
		if (m_grassEffect > 0f)
		{
			num += CalcForce(m_grassEffect, m_timeMS, 440) * 0.3f;
		}
		if (m_groundEffect > 0f)
		{
			num += CalcForce(m_groundEffect, m_timeMS, 140) * 0.3f;
		}
		if (m_impactEffect > 0f)
		{
			num += CalcForce(m_impactEffect, m_timeMS, 800) * 1.5f;
		}
		if (m_setRawForce != null)
		{
			if (onlyConstEffect)
			{
				num = m_constEffect;
			}
			m_setRawForce(Mathf.Clamp(num, -1f, 1f));
		}
		m_constEffect = 0f;
	}

	private static float CalcForce(float force, int time, int period)
	{
		return Mathf.Sin((float)(time % period) / (float)period);
	}
}
