using UnityEngine;

namespace CarX.Gameplay.ClippingZone
{
	public class EventClippingPumpkin : BaseClippingPoint
	{
		[SerializeField]
		private Collider m_triggerDetector;

		private static ClippingZoneSettings m_settings;

		private bool m_reposition;

		private Transform m_transform;

		private Vector3? m_localScale;

		private void Awake()
		{
			m_settings = m_settings ?? ResourcesWrapper.Load<ClippingZoneSettings>("ClippingZoneSettings");
		}

		public void SetLocalScale(Vector3 scale)
		{
			m_localScale = scale;
		}

		public void Init()
		{
			if (m_triggerDetector == null)
			{
				m_triggerDetector = GetComponent<Collider>();
			}
			if (m_triggerDetector != null && m_triggerDetector.isTrigger)
			{
				m_triggerDetector.isTrigger = true;
			}
			PostInit();
			ClippingPointObject componentInChildren = GetComponentInChildren<ClippingPointObject>();
			if (componentInChildren != null)
			{
				componentInChildren.clippingPoint = this;
			}
			else
			{
				Debug.LogWarning("Clipping zone is not have dynamic object: " + base.gameObject.name);
			}
		}

		private void PostInit()
		{
			m_state = State.Idle;
			m_info = null;
			m_reacted = false;
		}

		protected override void OnTriggerEnter(Collider other)
		{
			if (!m_reacted && !(other == null))
			{
				if (!m_cachedController)
				{
					m_driftController = other.transform.GetComponentInParent<DriftController>();
					m_cachedController = m_driftController != null;
				}
				if (m_playerCar == null)
				{
					m_playerCar = PlayerCarControl.instance.car;
				}
				m_counterCollisions++;
				if (!(m_driftController == null) && m_info == null)
				{
					m_info = new ClippingPointInfo
					{
						minDistanceFront = 1000f,
						minDistanceRear = 1000f,
						timeEnter = Time.time,
						touched = m_driftController.isDrift,
						timeDriftPoint = 0f,
						minSpeed = m_driftController.driftSpeed
					};
					m_reacted = true;
					m_driftController.CustomClippingPointReaction(m_info);
				}
			}
		}

		protected override void OnTriggerStay(Collider other)
		{
			if (m_reacted)
			{
				return;
			}
			base.OnTriggerStay(other);
			if (m_driftController == null || m_state != State.Idle || m_info == null || m_playerCar == null)
			{
				return;
			}
			for (int i = 0; i < m_playerCar.wheelCapsParents.Length; i++)
			{
				float b = Vector3.Distance(base.transform.position, m_playerCar.wheelCapsParents[i].position);
				if (i < 2)
				{
					m_info.minDistanceFront = Mathf.Min(m_info.minDistanceFront, b);
				}
				else
				{
					m_info.minDistanceRear = Mathf.Min(m_info.minDistanceRear, b);
				}
			}
			m_info.minSpeed = Mathf.Min(m_info.minSpeed, m_driftController.driftSpeed);
		}

		protected override void OnTriggerExit(Collider other)
		{
			if (m_reacted || other == null)
			{
				return;
			}
			m_counterCollisions--;
			if (m_state == State.Idle && !(m_driftController == null))
			{
				if (m_counterCollisions == 0)
				{
					m_state = State.Activated;
				}
				if (!(m_driftController == null) && m_info != null && !CheckCarSelfCollider(other) && m_counterCollisions == 0 && m_state == State.Activated && !m_info.touched)
				{
					m_info.minSpeed = Mathf.Min(m_info.minSpeed, m_driftController.driftSpeed);
					m_info.timeDriftPoint = Time.time - m_info.timeEnter;
					m_reacted = true;
				}
			}
		}

		public override void Reset()
		{
			m_info = null;
			m_reacted = false;
			base.Reset();
			PostInit();
		}

		private void DrawGizmos(bool selected)
		{
			if (!(m_triggerDetector == null))
			{
				Color color = new Color(0.5f, 1f, 1f, 0.7f);
				Color color2 = new Color(0.5f, 1f, 1f, 0.3f);
				if (selected)
				{
					color.a = 1f;
					color2.a = 0.7f;
				}
				Matrix4x4 matrix = Gizmos.matrix;
				Matrix4x4 localToWorldMatrix = base.transform.localToWorldMatrix;
				Gizmos.matrix *= localToWorldMatrix;
				Gizmos.color = color;
				Gizmos.DrawWireCube(m_triggerDetector.bounds.center, m_triggerDetector.bounds.size);
				Gizmos.color = color2;
				Gizmos.DrawCube(m_triggerDetector.bounds.center, m_triggerDetector.bounds.size);
				Gizmos.matrix = matrix;
			}
		}

		private void OnDrawGizmos()
		{
			DrawGizmos(selected: false);
		}

		private void OnDrawGizmosSelected()
		{
			DrawGizmos(selected: true);
		}
	}
}
