using System;
using FMOD.Studio;
using UnityEngine;

public class FMODCarCollideSound : FMODBaseScript
{
	[Serializable]
	public class SoundsParams
	{
		public float crashVelocityFactor = 0.08f;

		public float heavyCrashThreshold = 0.8f;
	}

	private const float MIN_VELOCITY_TO_SOUND = 4f;

	private const string defaultImpact = "event:/GameEvents/Crash/Impact";

	private const string defaultSlide = "event:/GameEvents/Crash/Slip";

	[SerializeField]
	private SoundsParams m_soundParams;

	[SerializeField]
	private LayerMask m_exclude;

	private FMODEventNode m_collisionHitNode;

	private FMODEventNode m_collisionSlideNode;

	private FMODEventNode.Parameter m_hitVelocityParam;

	private FMODEventNode.Parameter m_hitMassParam;

	private FMODEventNode.Parameter m_slideVelocityParam;

	private int m_lastUpdateFrame;

	private float m_lastUpdateVelocity;

	public void GetParamsFrom(CarCollideEffects carCollideEffects)
	{
		m_soundParams = new SoundsParams();
		m_soundParams.crashVelocityFactor = carCollideEffects.soundParams.crashVelocityFactor;
		m_soundParams.heavyCrashThreshold = carCollideEffects.soundParams.heavyCrashThreshold;
		m_exclude = (int)carCollideEffects.excludeMask & -2049;
	}

	protected override bool OnEnableHandler()
	{
		if (CreateNode(ref m_collisionHitNode, "event:/GameEvents/Crash/Impact"))
		{
			m_hitVelocityParam = m_collisionHitNode.RegisterParameter("Velocity");
			m_hitMassParam = m_collisionHitNode.RegisterParameter("Mass");
		}
		if (CreateNode(ref m_collisionSlideNode, "event:/GameEvents/Crash/Slip"))
		{
			m_slideVelocityParam = m_collisionSlideNode.RegisterParameter("Velocity");
		}
		return base.OnEnableHandler();
	}

	private void OnCollisionStay(Collision collInfo)
	{
		if (!base.isEnabled || ((1 << collInfo.gameObject.layer) & (int)m_exclude) != 0 || collInfo.contactCount == 0)
		{
			return;
		}
		Rigidbody rigidbody = collInfo.rigidbody;
		if (IsSmallObjectCollision(rigidbody))
		{
			return;
		}
		ContactPoint contact = collInfo.GetContact(0);
		float magnitude = collInfo.relativeVelocity.magnitude;
		if (m_lastUpdateFrame != Time.frameCount || !(m_lastUpdateVelocity >= magnitude))
		{
			m_lastUpdateFrame = Time.frameCount;
			m_lastUpdateVelocity = magnitude;
			bool flag = magnitude >= 4f;
			if (flag)
			{
				Vector3 lhs = collInfo.relativeVelocity * (1f / magnitude);
				flag = 1f - Mathf.Clamp01(Mathf.Abs(Vector3.Dot(lhs, contact.normal))) > 0f;
			}
			if (flag || m_collisionSlideNode.IsPlaying())
			{
				m_slideVelocityParam.SetValue(magnitude);
			}
			UpdateSlidePlayState(flag);
		}
	}

	private void UpdateSlidePlayState(bool needPlay)
	{
		bool flag = m_collisionSlideNode.IsPlaying();
		if (needPlay != flag)
		{
			if (needPlay)
			{
				m_collisionSlideNode.Start();
			}
			else
			{
				m_collisionSlideNode.Stop(STOP_MODE.ALLOWFADEOUT);
			}
		}
	}

	private void OnCollisionEnter(Collision collInfo)
	{
		if (!base.isEnabled || ((1 << collInfo.gameObject.layer) & (int)m_exclude) != 0 || collInfo.contactCount == 0)
		{
			return;
		}
		ContactPoint contact = collInfo.GetContact(0);
		float magnitude = collInfo.relativeVelocity.magnitude;
		float num = 0f;
		if (magnitude > 0f)
		{
			num = Mathf.Clamp01(magnitude * m_soundParams.crashVelocityFactor * 3f);
			Vector3 lhs = collInfo.relativeVelocity * (1f / magnitude);
			num *= Mathf.Clamp(Mathf.Abs(Vector3.Dot(lhs, contact.normal)), 0.7f, 1f);
		}
		if (num > 0f)
		{
			m_hitVelocityParam.SetValue(magnitude);
			Rigidbody rigidbody = collInfo.rigidbody;
			m_hitMassParam.SetValue((rigidbody == null) ? 99999f : rigidbody.mass);
			if (!m_collisionHitNode.IsPlaying() && base.raceCar != null && base.raceCar.enableCollisionHitSound)
			{
				m_collisionHitNode.Start();
			}
		}
		else
		{
			m_hitVelocityParam.SetValue(0f);
		}
	}

	private void OnCollisionExit(Collision collInfo)
	{
		UpdateSlidePlayState(needPlay: false);
	}

	private bool IsSmallObjectCollision(Rigidbody rb)
	{
		if (rb != null)
		{
			return rb.mass < 30f;
		}
		return false;
	}
}
