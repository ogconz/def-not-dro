using UnityEngine;

namespace CarX.Gameplay.ClippingZone
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(BoxCollider))]
	public class ClippingZone : BaseClipping
	{
		protected BoxCollider m_collider;

		private ClippingZoneInfo m_info;

		private void Start()
		{
			OnEnable();
		}

		private void OnEnable()
		{
			m_state = State.Idle;
			if (m_collider == null)
			{
				m_collider = GetComponent<BoxCollider>();
			}
			if (m_collider != null)
			{
				m_collider.center = Vector3.up * 0.5f;
				m_collider.size = Vector3.one + Vector3.up * 2f;
				m_collider.isTrigger = true;
			}
		}

		protected override void OnTriggerEnter(Collider other)
		{
			base.OnTriggerEnter(other);
			if (!(m_driftController == null))
			{
				m_info = new ClippingZoneInfo
				{
					minSpeed = m_driftController.driftSpeed
				};
			}
		}

		protected override void OnTriggerStay(Collider other)
		{
			base.OnTriggerStay(other);
			if (!(m_driftController == null) && m_state == State.Idle)
			{
				m_info.minSpeed = Mathf.Min(m_info.minSpeed, m_driftController.driftSpeed);
			}
		}

		protected override void OnTriggerExit(Collider other)
		{
			base.OnTriggerExit(other);
			if (!(m_driftController == null) && !CheckCarSelfCollider(other) && m_state == State.Activated)
			{
				m_driftController.ClippingZoneReaction(m_info);
			}
		}

		private void DrawGizmos(bool selected)
		{
			if (!(m_collider == null))
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
				Gizmos.DrawWireCube(m_collider.center, m_collider.size);
				Gizmos.color = color2;
				Gizmos.DrawCube(m_collider.center, m_collider.size);
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
