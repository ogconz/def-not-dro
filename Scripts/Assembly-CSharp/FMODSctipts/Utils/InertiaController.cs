using UnityEngine;

namespace FMODSctipts.Utils
{
	public class InertiaController
	{
		private float m_accelInertia;

		private float m_brakingInertia;

		private readonly float m_defaultAccelInertia;

		private readonly float m_defaultBrakingInertia;

		private float m_currVelocity;

		public float value { get; set; }

		public InertiaController(float brakingInertia, float accelInertia)
		{
			m_accelInertia = accelInertia;
			m_defaultAccelInertia = accelInertia;
			m_brakingInertia = brakingInertia;
			m_defaultBrakingInertia = brakingInertia;
		}

		public float UpdateValueQuadratic(float target, float dt, float factor)
		{
			float mass = ((target > value) ? m_accelInertia : m_brakingInertia);
			return Move(target, dt, mass, factor);
		}

		public void Reset()
		{
			value = 0f;
			m_accelInertia = m_defaultAccelInertia;
			m_brakingInertia = m_defaultBrakingInertia;
		}

		private float Move(float target, float dt, float mass, float factor)
		{
			if (dt == 0f)
			{
				return value;
			}
			if (mass == 0f)
			{
				value = target;
				return value;
			}
			float num = target - value;
			float num2 = Mathf.Sign(num);
			num *= num * num2;
			value += num / mass * dt * factor;
			if ((num2 > 0f && value > target) || (num2 < 0f && value < target))
			{
				value = target;
			}
			return value;
		}
	}
}
