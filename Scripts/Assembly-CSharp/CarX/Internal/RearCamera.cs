using UnityEngine;

namespace CarX.Internal
{
	public class RearCamera
	{
		private bool m_detectCollision;

		private float m_distance = 4f;

		private float m_minDistance = 2f;

		private float m_maxDistance = 12f;

		private float m_height = 1.2f;

		private float m_minHeight = 1f;

		private float m_maxHeight = 1.5f;

		private float m_heightSpeed = 10f;

		private float m_alignSpeed = 4f;

		private float m_rotationStiffness = 0.1f;

		private float m_accFactor = 0.4f;

		private float m_accSpeed = 2f;

		private LayerMask m_collisionLayers;

		private bool m_skipFirstInterpolation = true;

		private Vector3 m_forward;

		private Vector3 m_right;

		private Vector3 m_up;

		private Vector3 m_oldTargetVel;

		private Vector3 m_oldTargetAccel;

		private Vector3 m_position;

		private Quaternion m_rotation;

		private float m_curHeight;

		private float m_curDist;

		private Vector3 m_shift;

		private Vector3 m_smoothVelocity;

		private RaycastHit m_hit;

		private Vector3 m_focusPoint;

		private Vector3 m_bottomPoint;

		private bool m_autoCalcFocusPoint;

		public float xSpeedControlRotate = 10f;

		public float m_posX;

		public bool detectCollision
		{
			get
			{
				return m_detectCollision;
			}
			set
			{
				m_detectCollision = value;
			}
		}

		public float distance
		{
			get
			{
				return m_distance;
			}
			set
			{
				m_distance = value;
				m_curDist = m_distance;
			}
		}

		public float minDistance
		{
			get
			{
				return m_minDistance;
			}
			set
			{
				m_minDistance = value;
			}
		}

		public float maxDistance
		{
			get
			{
				return m_maxDistance;
			}
			set
			{
				m_maxDistance = value;
			}
		}

		public float height
		{
			get
			{
				return m_height;
			}
			set
			{
				m_height = value;
			}
		}

		public float minHeight
		{
			get
			{
				return m_minHeight;
			}
			set
			{
				m_minHeight = value;
			}
		}

		public float maxHeight
		{
			get
			{
				return m_maxHeight;
			}
			set
			{
				m_maxHeight = value;
			}
		}

		public float heightSpeed
		{
			get
			{
				return m_heightSpeed;
			}
			set
			{
				m_heightSpeed = value;
			}
		}

		public float alignSpeed
		{
			get
			{
				return m_alignSpeed;
			}
			set
			{
				m_alignSpeed = value;
			}
		}

		public float rotationStiffness
		{
			get
			{
				return m_rotationStiffness;
			}
			set
			{
				m_rotationStiffness = value;
			}
		}

		public float accSpeed
		{
			get
			{
				return m_accSpeed;
			}
			set
			{
				m_accSpeed = value;
			}
		}

		public float accFactor
		{
			get
			{
				return m_accFactor;
			}
			set
			{
				m_accFactor = value;
			}
		}

		public Vector3 position => m_position;

		public Quaternion rotation => m_rotation;

		public LayerMask collisionLayers
		{
			get
			{
				return m_collisionLayers;
			}
			set
			{
				m_collisionLayers = value;
			}
		}

		public Vector3 focusPoint
		{
			get
			{
				return m_focusPoint;
			}
			set
			{
				m_focusPoint = value;
			}
		}

		public Vector3 bottomPoint
		{
			get
			{
				return m_bottomPoint;
			}
			set
			{
				m_bottomPoint = value;
			}
		}

		public bool autoCalcFocusPoint
		{
			get
			{
				return m_autoCalcFocusPoint;
			}
			set
			{
				m_autoCalcFocusPoint = value;
			}
		}

		public void SkipFirstInterpolation()
		{
			m_skipFirstInterpolation = true;
		}

		public void Update(Transform target, Vector3 targetOffset, Vector3 velocity, float dt)
		{
			if (dt > 1f)
			{
				dt = 1f;
			}
			float num = ((dt != 0f) ? (1f / dt) : 0f);
			if (m_skipFirstInterpolation)
			{
				m_oldTargetVel = velocity;
				m_oldTargetAccel = MathExt.vZero;
				m_shift = MathExt.vZero;
				m_forward = target.forward;
			}
			float magnitude = velocity.magnitude;
			Vector3 b = (velocity - m_oldTargetVel) * num;
			b = Vector3.Lerp(m_oldTargetAccel, b, 20f * dt);
			m_oldTargetVel = velocity;
			m_oldTargetAccel = b;
			Quaternion obj = (m_autoCalcFocusPoint ? target.root.rotation : target.rotation);
			Vector3 vector = obj * Vector3.forward;
			Vector3 vector2 = obj * Vector3.right;
			Vector3 up = Vector3.up;
			Vector3 vector3 = ((!m_autoCalcFocusPoint) ? (target.position + vector * targetOffset.z + vector2 * targetOffset.x + up * targetOffset.y) : (target.root.TransformPoint(m_focusPoint) + vector * targetOffset.z + vector2 * targetOffset.x + up * targetOffset.y));
			float num2 = Vector3.Dot(vector, b);
			float value = m_distance + num2 * m_accFactor;
			value = MathExt.Clamp(value, m_minDistance, m_maxDistance);
			m_curDist += (value - m_curDist) * m_accSpeed * dt;
			float num3 = m_height;
			if (m_maxDistance > m_distance + 0.01f && m_minDistance < m_distance - 0.01f)
			{
				float num4 = m_distance - m_curDist;
				if (m_curDist > m_distance)
				{
					float num5 = num4 / (m_distance - m_maxDistance);
					num3 = m_height + num5 * (m_minHeight - m_height);
				}
				else
				{
					float num6 = num4 / (m_distance - m_minDistance);
					num3 = m_height + num6 * (m_maxHeight - m_height);
				}
			}
			m_curHeight += (num3 - m_curHeight) * dt * heightSpeed;
			Vector3 direction = velocity;
			float num7 = magnitude;
			if (num7 > 10f)
			{
				direction *= 10f / num7;
			}
			Vector3 direction2 = target.InverseTransformDirection(direction);
			direction2.y = 0f;
			direction = target.TransformDirection(direction2);
			Vector3 v = vector * m_rotationStiffness + direction * (0.01f * num7);
			MathExt.FastNormalizeSafe(ref v);
			if (m_skipFirstInterpolation)
			{
				m_forward = v;
			}
			else
			{
				m_forward = Vector3.Slerp(m_forward, v, m_alignSpeed * dt);
			}
			MathExt.FastNormalize(ref m_forward);
			m_right = Vector3.Cross(MathExt.vUp, m_forward);
			MathExt.FastNormalize(ref m_right);
			m_up = Vector3.Cross(m_forward, m_right);
			Quaternion quaternion = Quaternion.Euler(0f, m_posX, 0f);
			m_position = vector3 - quaternion * (m_forward * m_curDist) + Vector3.up * m_curHeight;
			if (m_detectCollision)
			{
				m_shift = Vector3.SmoothDamp(m_shift, DetectCollision(vector3, magnitude), ref m_smoothVelocity, 0.1f);
				m_position += m_shift;
			}
			Vector3 v2 = vector3 - m_position;
			MathExt.FastNormalize(ref v2);
			m_rotation.SetLookRotation(v2);
			m_skipFirstInterpolation = false;
		}

		private Vector3 DetectCollision(Vector3 targetPosition, float targetSpeed)
		{
			Vector3 v = MathExt.vZero;
			float num = 0f;
			float num2 = Mathf.Clamp(targetSpeed * 0.05f, 0.5f, 0.9f);
			Vector3 vector = m_position - targetPosition;
			Vector3 normalized = vector.normalized;
			if (Physics.SphereCast(targetPosition + normalized * num2, maxDistance: vector.magnitude - num2 * 2f, radius: num2, direction: normalized, hitInfo: out m_hit, layerMask: m_collisionLayers))
			{
				float num3 = num2 - Vector3.Dot(m_position - m_hit.point, m_hit.normal);
				if (num3 > 0f)
				{
					num = num3;
					v = m_hit.normal;
				}
			}
			if (num > 0f)
			{
				MathExt.FastNormalize(ref v);
				v *= num;
				Vector3 vector2 = targetPosition;
				vector2.y = m_position.y - 0.2f;
				Vector3 vector3 = m_position + v - vector2;
				float magnitude = vector3.magnitude;
				if (magnitude < m_minDistance)
				{
					vector3 *= m_minDistance / (magnitude + 0.001f);
					v = vector2 + vector3 - m_position;
				}
			}
			return v;
		}
	}
}
