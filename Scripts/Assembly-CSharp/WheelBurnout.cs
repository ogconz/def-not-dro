using System;
using CarX;
using DI;
using UnityEngine;

public class WheelBurnout : BaseWheelSmoke
{
	private RaceCar m_car;

	private CARXWheelsBurnoutData m_data;

	private ParticleSystem m_emitter;

	private CARXWheelAccessor m_wheelAccessor;

	private float m_elapsedAccumulation;

	private ParticleSystem.Particle[] m_particles;

	private float m_burnoutProgress;

	private float m_noBurnoutTime;

	private GraphicsQuality m_graphicsQuality;

	private bool m_qualitySubscribed;

	private int m_surfaceIndex = -1;

	private void SubscribeToQualitySettings()
	{
		if (!m_qualitySubscribed)
		{
			GraphicsQuality graphicsQuality = m_graphicsQuality;
			graphicsQuality.OnSmokeLevelChange = (Action<int>)Delegate.Combine(graphicsQuality.OnSmokeLevelChange, new Action<int>(OnSmokeLevelChange));
			m_qualitySubscribed = true;
		}
	}

	public void Init(CARXWheelsBurnoutData data, int surfaceIndex, CARXWheelAccessor wheelAccessor, RaceCar car)
	{
		m_car = car;
		m_data = data;
		m_wheelAccessor = wheelAccessor;
		m_surfaceIndex = surfaceIndex;
		m_graphicsQuality = DependencyInjector.Resolve<GraphicsQuality>();
		SubscribeToQualitySettings();
		base.enabled = false;
		base.enabled = true;
	}

	public override void ClearSmoke()
	{
		if (m_emitter != null && m_emitter.particleCount > 0)
		{
			m_emitter.Clear(withChildren: true);
		}
	}

	private void DestroyEmitter()
	{
		if ((bool)Singletone<Simulator>.instance)
		{
			Singletone<Simulator>.instance.OnUpdateWheelsEvent -= HandleOnUpdateWheelsEvent;
		}
		if (m_emitter != null)
		{
			ParticlesManager.DestroyParticleSystem(ref m_emitter);
			m_particles = null;
		}
	}

	private void CreateEmitter()
	{
		DestroyEmitter();
		if (!(m_wheelAccessor == null) && !(m_data == null))
		{
			m_emitter = ParticlesManager.InstantiateParticleSystem(m_data.particles, needUniqueInstance: true, initCollision: false);
			m_emitter.transform.parent = base.transform;
			Singletone<Simulator>.instance.OnUpdateWheelsEvent += HandleOnUpdateWheelsEvent;
		}
	}

	protected override bool OnEnableHandler()
	{
		if (m_graphicsQuality == null || !m_graphicsQuality.smokeEnabled)
		{
			return false;
		}
		CreateEmitter();
		return m_emitter != null;
	}

	protected override void OnDisableHandler()
	{
		DestroyEmitter();
	}

	private void HandleOnUpdateWheelsEvent()
	{
		UpdateParticles();
	}

	private void UpdateParticles()
	{
		float deltaTime = Time.deltaTime;
		if (deltaTime != 0f)
		{
			EmitPartices(deltaTime);
			MoveParticles(deltaTime);
		}
	}

	private void EmitPartices(float dt)
	{
		if (m_car.networkQuality <= 0.1f)
		{
			return;
		}
		float num = 1f / (m_car.networkQuality * m_data.emissionRate);
		Color color = (m_data.colorCustomization ? (m_data.color * base.smokeColor) : m_data.color);
		Wheel wheel = m_wheelAccessor.wheel;
		Vector3 position = m_wheelAccessor.getTransform.position;
		Vector3 right = m_wheelAccessor.getTransform.right;
		Vector3 left;
		Vector3 right2;
		Vector3 cnt;
		Vector3 n;
		bool patch = wheel.GetPatch(out left, out right2, out cnt, out n);
		int userData = -1;
		if (patch && wheel.GetUserData(out userData) && userData == m_surfaceIndex && HasBurnout())
		{
			m_elapsedAccumulation += dt;
			if (m_elapsedAccumulation >= num)
			{
				int num2 = Mathf.FloorToInt(m_elapsedAccumulation * m_data.emissionRate);
				float num3 = num * (float)num2;
				Vector3 vector = cnt - position;
				float num4 = wheel.width * 0.25f;
				float num5 = ((wheel.w > 0f) ? 1 : (-1));
				m_burnoutProgress += 270f * dt;
				for (int i = 0; i < num2; i++)
				{
					Vector3 vector2 = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, m_burnoutProgress) * num5, right) * vector;
					Vector3 pos = position + vector2 + right * UnityEngine.Random.Range(0f - num4, num4);
					EmitParticle(pos, color, num5);
				}
				m_elapsedAccumulation -= num3;
				m_noBurnoutTime = 0f;
			}
		}
		else
		{
			m_elapsedAccumulation = 0f;
			m_noBurnoutTime += dt;
			if (m_noBurnoutTime > 1f)
			{
				m_noBurnoutTime = 0f;
				m_burnoutProgress = 0f;
			}
		}
	}

	private void EmitParticle(Vector3 pos, Color32 color, float wl_wSign)
	{
		ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
		{
			position = pos,
			startLifetime = UnityEngine.Random.Range(m_data.minLifeTime, m_data.maxLifeTime),
			startSize = UnityEngine.Random.Range(m_data.minSize, m_data.maxSize),
			rotation = UnityEngine.Random.Range(0f, m_data.maxStartRotationAngle),
			angularVelocity = UnityEngine.Random.Range(m_data.minAngularVelocity, m_data.maxAngularVelocity),
			axisOfRotation = Vector3.left * wl_wSign,
			startColor = color,
			randomSeed = 5u
		};
		m_emitter.Emit(emitParams, 1);
	}

	private bool HasBurnout()
	{
		if (m_wheelAccessor.car.speedKMH > 30f)
		{
			return false;
		}
		Wheel wheel = m_wheelAccessor.wheel;
		if (Mathf.Abs(wheel.sr) + Mathf.Abs(wheel.sa) < 100f)
		{
			return false;
		}
		if (Mathf.Abs(wheel.w) < 10f && Mathf.Abs(wheel.w0) < 10f)
		{
			return false;
		}
		if (wheel.saVol + wheel.srVol < 0.1f)
		{
			return false;
		}
		return true;
	}

	private void MoveParticles(float dt)
	{
		if (m_emitter.particleCount != 0)
		{
			if (m_particles == null || m_particles.Length != m_emitter.maxParticles)
			{
				m_particles = new ParticleSystem.Particle[m_emitter.maxParticles];
			}
			float num = 1f / m_wheelAccessor.wheel.radius;
			Vector3 position = m_wheelAccessor.getTransform.position;
			Vector3 right = m_wheelAccessor.getTransform.right;
			Vector3 pointVelocity = m_wheelAccessor.car.getRigidbody.GetPointVelocity(position);
			int particles = m_emitter.GetParticles(m_particles);
			Vector3 vector = default(Vector3);
			Vector3 velocity = default(Vector3);
			for (int i = 0; i < particles; i++)
			{
				Vector3 position2 = m_particles[i].position;
				vector.x = (position2.x - position.x) * num;
				vector.y = (position2.y - position.y) * num;
				vector.z = (position2.z - position.z) * num;
				velocity.x = vector.y * right.z - vector.z * right.y;
				velocity.y = vector.z * right.x - vector.x * right.z;
				velocity.z = vector.x * right.y - vector.y * right.x;
				float x = m_particles[i].axisOfRotation.x;
				velocity.x = velocity.x * x + pointVelocity.x;
				velocity.y = velocity.y * x + pointVelocity.y;
				velocity.z = velocity.z * x + pointVelocity.z;
				m_particles[i].velocity = velocity;
			}
			m_emitter.SetParticles(m_particles, particles);
		}
	}

	private void OnDestroy()
	{
		if (m_graphicsQuality != null)
		{
			GraphicsQuality graphicsQuality = m_graphicsQuality;
			graphicsQuality.OnSmokeLevelChange = (Action<int>)Delegate.Remove(graphicsQuality.OnSmokeLevelChange, new Action<int>(OnSmokeLevelChange));
			m_qualitySubscribed = false;
		}
	}

	private void OnSmokeLevelChange(int qualityLevel)
	{
		if (qualityLevel == 0)
		{
			base.enabled = false;
		}
		else
		{
			base.enabled = true;
		}
	}
}
