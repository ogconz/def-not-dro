using System.Collections.Generic;
using ArrayExtension;
using CarModelSystem;
using CarX;
using UnityEngine;

[AddComponentMenu("Car/PopExhaust")]
public class CarPopExhaust : CARXBaseScript
{
	private List<Transform> m_exhaustPoints = new List<Transform>();

	private List<ParticleSystem> m_exhaustFlameInstances = new List<ParticleSystem>();

	private Car m_carX;

	private RaceCar m_car;

	private float m_popTimer;

	private FMODPopExhaust m_popSound;

	private static readonly TypeID s_exhaustTypeID = new TypeID("Exhaust");

	private bool isCarAttached => m_car != null;

	private void Start()
	{
		if (isCarAttached)
		{
			m_car.carModel.Subscribe(s_exhaustTypeID, RecreateEffects);
		}
		RecreateEffects();
	}

	private void OnDestroy()
	{
		if (isCarAttached && m_car.carModel != null)
		{
			m_car.carModel.Unsubscribe(s_exhaustTypeID, RecreateEffects);
		}
	}

	private void InitEffects()
	{
		if (!isCarAttached)
		{
			return;
		}
		Transform[] collection = m_car.GetComponentsInChildren<CarComponentRoot>(includeInactive: true).Filter((CarComponentRoot e) => e.typeID == "flame").Convert((CarComponentRoot e) => e.transform);
		m_exhaustPoints.AddRange(collection);
		int num = 0;
		for (int num2 = 0; num2 < m_exhaustPoints.Count; num2++)
		{
			if (m_exhaustPoints[num2] != null)
			{
				num++;
			}
		}
		for (int num3 = 0; num3 < m_exhaustPoints.Count; num3++)
		{
			if (m_exhaustPoints[num3] != null)
			{
				m_exhaustFlameInstances.Add(InstantiateParticleSystem(CarResourceProvider.instance.exhaustFlame, m_exhaustPoints[num3]));
			}
		}
		m_popSound = base.gameObject.GetOrAddComponent<FMODPopExhaust>();
	}

	private void RecreateEffects()
	{
		CleanUpEffects();
		InitEffects();
	}

	public void PreAddExhaustPoint(Transform point)
	{
		m_exhaustPoints.Add(point);
	}

	public void CleanUpEffects()
	{
		foreach (ParticleSystem exhaustFlameInstance in m_exhaustFlameInstances)
		{
			if (exhaustFlameInstance != null)
			{
				Object.Destroy(exhaustFlameInstance.gameObject);
			}
		}
		m_exhaustFlameInstances.Clear();
		m_exhaustPoints.Clear();
	}

	protected override bool OnEnableHandler()
	{
		if (!isCarAttached)
		{
			m_car = GetComponentInParent<RaceCar>();
			if (!m_car)
			{
				return false;
			}
		}
		m_carX = m_car.carX;
		if (!CarResourceProvider.instance)
		{
			return false;
		}
		return true;
	}

	protected override void OnDisableHandler()
	{
		for (int i = 0; i < m_exhaustFlameInstances.Count; i++)
		{
			ParticleSystem particleSystem = m_exhaustFlameInstances[i];
			if (particleSystem != null)
			{
				particleSystem.Stop();
			}
		}
	}

	protected override void UpdateHandler(float dt)
	{
		if (!base.isEnabled || !isCarAttached)
		{
			return;
		}
		float rpm = m_carX.rpm;
		float load = m_carX.load;
		float engineRevLimiter = m_carX.engineRevLimiter;
		if (!(rpm > 3000f))
		{
			return;
		}
		if (load < 0.7f)
		{
			if (m_popTimer > 0f)
			{
				m_popTimer -= dt;
				if (m_popTimer > 2f)
				{
					m_popTimer = 0f;
				}
			}
			if (m_popTimer < 0f)
			{
				if (rpm < engineRevLimiter * 0.98f)
				{
					foreach (ParticleSystem exhaustFlameInstance in m_exhaustFlameInstances)
					{
						if ((bool)exhaustFlameInstance)
						{
							exhaustFlameInstance.Stop(withChildren: true);
							exhaustFlameInstance.Play(withChildren: true);
							m_popSound.Play();
						}
					}
				}
				m_popTimer = 0f;
			}
		}
		if (load > 0.7f && m_popTimer == 0f)
		{
			m_popTimer = 0.05f * engineRevLimiter / rpm;
			if (rpm < engineRevLimiter * 0.98f && rpm < Random.Range(0f, engineRevLimiter) * 2f)
			{
				m_popTimer = 10f;
			}
		}
	}

	private ParticleSystem InstantiateParticleSystem(ParticleSystem prefab, Transform parent)
	{
		if (prefab == null)
		{
			return null;
		}
		ParticleSystem particleSystem = Object.Instantiate(prefab);
		parent.AddChildResetPRS(particleSystem);
		return particleSystem;
	}
}
