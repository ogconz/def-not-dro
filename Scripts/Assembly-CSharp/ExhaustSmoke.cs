using CarX;
using UnityEngine;

[AddComponentMenu("Car/ExhaustSmoke")]
public class ExhaustSmoke : CARXBaseScript
{
	[SerializeField]
	private Transform[] m_exhaustPoints;

	public Color lowPowerSmokeColor = Color.gray;

	public Color highPowerSmokeColor = Color.black;

	public float lowPowerEmissionRate = 10f;

	public float highPowerEmissionRate = 100f;

	public float lowPowerStartSpeed = 1f;

	public float highPowerStartSpeed = 2f;

	private ParticleSystem[] m_particles;

	private Car m_car;

	private float m_maxPowerInv;

	private float m_currentPower;

	protected override bool OnEnableHandler()
	{
		if (!m_car)
		{
			m_car = GetComponentInParent<Car>();
			if (!m_car)
			{
				return false;
			}
		}
		if ((bool)CarResourceProvider.instance)
		{
			CarResourceProvider instance = CarResourceProvider.instance;
			if (!instance.exhaustSmoke)
			{
				return false;
			}
			m_particles = new ParticleSystem[m_exhaustPoints.Length];
			for (int i = 0; i < m_exhaustPoints.Length; i++)
			{
				m_particles[i] = CreateParticles(instance.exhaustSmoke, m_exhaustPoints[i]);
				m_particles[i].Play();
			}
		}
		m_car.CalcMaxPowerMaxTorque(out var max_hp, out var _, out var _, out var _);
		m_maxPowerInv = 1f / max_hp;
		return true;
	}

	protected override void OnDisableHandler()
	{
		if (m_particles != null)
		{
			for (int i = 0; i < m_particles.Length; i++)
			{
				DestroyParticles(ref m_particles[i]);
			}
		}
		m_particles = null;
	}

	protected override void UpdateHandler(float dt)
	{
		float engineCurPower = m_car.engineCurPower;
		m_currentPower = Mathf.MoveTowards(m_currentPower, engineCurPower, dt * 1000f);
		float t = Mathf.Clamp01(m_currentPower * m_maxPowerInv);
		Color startColor = Color.Lerp(lowPowerSmokeColor, highPowerSmokeColor, t);
		float emissionRate = Mathf.Lerp(lowPowerEmissionRate, highPowerEmissionRate, t);
		float startSpeed = Mathf.Lerp(lowPowerStartSpeed, highPowerStartSpeed, t);
		int i = 0;
		for (int num = m_particles.Length; i < num; i++)
		{
			ParticleSystem obj = m_particles[i];
			obj.startColor = startColor;
			obj.emissionRate = emissionRate;
			obj.startSpeed = startSpeed;
		}
	}

	private static ParticleSystem CreateParticles(ParticleSystem prefab, Transform parent)
	{
		ParticleSystem particleSystem = Object.Instantiate(prefab);
		particleSystem.transform.parent = parent;
		particleSystem.transform.localPosition = Vector3.zero;
		particleSystem.transform.localRotation = Quaternion.identity;
		return particleSystem;
	}

	private static void DestroyParticles(ref ParticleSystem ps)
	{
		if ((bool)ps)
		{
			Object.Destroy(ps.gameObject);
			ps = null;
		}
	}
}
