using System;
using DI;
using UnityEngine;

public class CarCollideEffects : CARXBaseScript
{
	[Serializable]
	public class SparksParams
	{
		public float minRelativeVelocity = 8f;

		public float maxRelativeVelocity = 30f;
	}

	[Serializable]
	public class SoundsParams
	{
		public float crashVelocityFactor = 0.08f;

		public float heavyCrashThreshold = 0.8f;
	}

	[SerializeField]
	private SparksParams m_sparksParams;

	[SerializeField]
	private SoundsParams m_soundParams;

	[SerializeField]
	private LayerMask m_exclude;

	private ParticleSystem m_sparksInstance;

	private Transform m_sparksTransform;

	private RaceCar m_Host;

	public SoundsParams soundParams => m_soundParams;

	public LayerMask excludeMask => m_exclude;

	private void Awake()
	{
		m_Host = GetComponent<RaceCar>();
		if (GetComponent<FMODCarCollideSound>() == null)
		{
			base.gameObject.AddComponent<FMODCarCollideSound>().GetParamsFrom(this);
		}
	}

	protected override bool OnEnableHandler()
	{
		if (CarResourceProvider.instance == null || CarResourceProvider.instance.collisionSparks == null)
		{
			return false;
		}
		CarResourceProvider instance = CarResourceProvider.instance;
		m_sparksInstance = UnityEngine.Object.Instantiate(instance.collisionSparks);
		m_sparksTransform = m_sparksInstance.transform;
		m_sparksTransform.parent = base.transform;
		m_sparksTransform.localPosition = Vector3.zero;
		m_sparksTransform.localRotation = Quaternion.identity;
		this.MakeDependenciesInjected();
		return true;
	}

	protected override void OnDisableHandler()
	{
		if ((bool)m_sparksInstance)
		{
			UnityEngine.Object.Destroy(m_sparksInstance.gameObject);
			m_sparksInstance = null;
			m_sparksTransform = null;
		}
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
		Vector3 forward = collInfo.relativeVelocity * (1f / magnitude);
		if (magnitude > m_sparksParams.minRelativeVelocity)
		{
			magnitude = Mathf.Clamp(magnitude, m_sparksParams.minRelativeVelocity, m_sparksParams.maxRelativeVelocity);
			float num = (magnitude - m_sparksParams.minRelativeVelocity) / (m_sparksParams.maxRelativeVelocity - m_sparksParams.minRelativeVelocity);
			if (m_sparksTransform != null)
			{
				m_sparksTransform.position = contact.point;
				m_sparksTransform.rotation = Quaternion.LookRotation(forward);
			}
			if (m_sparksInstance != null)
			{
				m_sparksInstance.Emit(Mathf.RoundToInt(m_sparksInstance.emission.rateOverTime.constantMax * num));
			}
		}
	}

	private void OnCollisionEnter(Collision collInfo)
	{
		if (base.isEnabled && ((1 << collInfo.gameObject.layer) & (int)m_exclude) == 0 && collInfo.contactCount != 0)
		{
			ContactPoint contact = collInfo.GetContact(0);
			float magnitude = collInfo.relativeVelocity.magnitude;
			Vector3 lhs = collInfo.relativeVelocity * (1f / magnitude);
			float num = Mathf.Clamp01(magnitude * m_soundParams.crashVelocityFactor * 3f);
			num *= Mathf.Clamp(Mathf.Abs(Vector3.Dot(lhs, contact.normal)), 0.7f, 1f);
			if (num > 0f && m_Host.isLocalPlayer && PlayerCarControl.instance != null)
			{
				PlayerCarControl.instance.PlayImpactEffect(num);
			}
		}
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
