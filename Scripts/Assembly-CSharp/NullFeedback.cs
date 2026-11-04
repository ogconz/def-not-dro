using GameInput;
using Rewired;
using UnityEngine;

public class NullFeedback : IForceFeedbackSystem
{
	private float m_grassEffect;

	private float m_groundEffect;

	private bool m_stopEffectsRequested;

	public bool isInitiated { get; private set; }

	public void Initialize()
	{
	}

	public bool IsThisDeviceController(Controller controller)
	{
		return false;
	}

	private void UpdateWheelRange()
	{
	}

	public void SetConstantForce(float k)
	{
	}

	public void PlayImpactEffect(float k)
	{
	}

	public void PlayGrassSurfaceEffect(float k)
	{
		k = GameInput.InputManager.settings.Params.wheelParams.forceFeedback * k;
		m_grassEffect = Mathf.Max(m_grassEffect, k);
	}

	public void PlayGravelSurfaceEffect(float k)
	{
		k = GameInput.InputManager.settings.Params.wheelParams.forceFeedback * k;
		m_groundEffect = Mathf.Max(m_groundEffect, k);
	}

	public void StopEffects(bool instant = true)
	{
		m_stopEffectsRequested = true;
		if (instant)
		{
			DoStopEffects();
		}
	}

	private void DoStopEffects()
	{
		if (m_stopEffectsRequested)
		{
			m_stopEffectsRequested = false;
		}
	}

	public bool IsFFBWheelExist()
	{
		return false;
	}

	public void Enable()
	{
	}

	public void Disable()
	{
	}

	public void Update()
	{
		DoStopEffects();
		m_grassEffect = Mathf.Max(0f, m_grassEffect - Time.deltaTime);
		m_groundEffect = Mathf.Max(0f, m_groundEffect - Time.deltaTime);
	}
}
