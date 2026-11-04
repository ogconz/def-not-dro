using System.Collections.Generic;
using CarModelSystem;
using UnityEngine;
using Utils.MeshGen.Spline;

namespace CarX.Gameplay.ClippingZone
{
	public class ClippingPoint : BaseClippingPoint
	{
		[SerializeField]
		private BoxCollider m_triggerDetector;

		private List<float> m_meshLengthsList = new List<float>();

		private static ClippingZoneSettings m_settings;

		private bool m_reposition;

		private Transform m_transform;

		private Vector3? m_localScale;

		private int m_localCarLayer;

		private int m_roadLayer;

		private void Awake()
		{
			m_settings = m_settings ?? ResourcesWrapper.Load<ClippingZoneSettings>("ClippingZoneSettings");
			m_localCarLayer = LayerMask.NameToLayer("LocalCar");
			m_roadLayer = LayerMask.NameToLayer("Road");
		}

		public void SetLocalScale(Vector3 scale)
		{
			m_localScale = scale;
		}

		public void Init()
		{
			m_transform = Object.Instantiate(m_settings.DefaultPoint, base.transform);
			m_transform.localPosition = Vector3.zero;
			m_transform.localRotation = Quaternion.identity;
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
			if (!m_transform)
			{
				m_transform = Object.Instantiate(m_settings.DefaultPoint, base.transform);
				m_transform.localPosition = Vector3.zero;
				m_transform.localRotation = Quaternion.identity;
			}
			m_state = State.Idle;
			m_info = null;
			m_reacted = false;
			if (m_reposition)
			{
				return;
			}
			m_reposition = true;
			GateLaserScript[] componentsInChildren = GetComponentsInChildren<GateLaserScript>();
			RaycastHit hitInfo;
			foreach (GateLaserScript gateLaserScript in componentsInChildren)
			{
				if (Physics.Raycast(gateLaserScript.startPoint.position + Vector3.up, Vector3.down, out hitInfo, 10f, -1073741825))
				{
					gateLaserScript.startPoint.position = hitInfo.point + Vector3.up * 0.01f;
				}
				if (Physics.Raycast(gateLaserScript.endPoint.position + Vector3.up, Vector3.down, out hitInfo, 10f, -1073741825))
				{
					gateLaserScript.endPoint.position = hitInfo.point + Vector3.up * 0.01f;
				}
			}
			Transform transform = base.gameObject.FindChildWithName("clippingPointLime_joint").transform;
			if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hitInfo, 10f, -1073741825))
			{
				transform.position = hitInfo.point;
			}
			if (m_localScale.HasValue)
			{
				base.transform.localScale = m_localScale.Value;
			}
			MeshGenSpline[] componentsInChildren2 = GetComponentsInChildren<MeshGenSpline>();
			Vector3 position = transform.position;
			MeshGenSpline[] array = componentsInChildren2;
			foreach (MeshGenSpline obj in array)
			{
				obj.Extrude();
				Vector3 closestPoint = obj.GetClosestPoint(position);
				float sqrMagnitude = (position - closestPoint).sqrMagnitude;
				m_meshLengthsList.Add(sqrMagnitude);
			}
			m_meshLengthsList.Remove(Mathf.Min(m_meshLengthsList.ToArray()));
			m_meshLengthsList.Sort();
		}

		protected override void OnTriggerEnter(Collider other)
		{
			if (!m_reacted)
			{
				base.OnTriggerEnter(other);
				if (!(m_driftController == null) && m_info == null)
				{
					Vector2 coloredStripesLengths = ((m_meshLengthsList.Count > 1) ? new Vector2(m_meshLengthsList[0], m_meshLengthsList[1]) : Vector2.zero);
					m_info = new ClippingPointInfo
					{
						minDistanceFront = 1000f,
						minDistanceRear = 1000f,
						timeEnter = Time.time,
						touched = false,
						timeDriftPoint = 0f,
						minSpeed = m_driftController.driftSpeed,
						coloredStripesLengths = coloredStripesLengths
					};
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
			if (m_driftController == null || m_state != State.Idle || m_info == null || m_playerCar == null || other == null)
			{
				return;
			}
			if (other.gameObject.layer == m_localCarLayer)
			{
				CarWheelModel[] carWheels = m_playerCar.carModel.carWheels;
				for (int i = 0; i < carWheels.Length; i++)
				{
					Vector3 position = carWheels[i].hub.position;
					float y = position.y;
					if (Physics.Raycast(new Ray(position, Vector3.down), out var hitInfo, m_roadLayer))
					{
						y = hitInfo.point.y;
					}
					float sqrMagnitude = (new Vector3(position.x, y, position.z) - base.transform.position).sqrMagnitude;
					if (i < 2)
					{
						m_info.minDistanceFront = Mathf.Min(m_info.minDistanceFront, sqrMagnitude);
					}
					else
					{
						m_info.minDistanceRear = Mathf.Min(m_info.minDistanceRear, sqrMagnitude);
					}
				}
			}
			m_info.minSpeed = Mathf.Min(m_info.minSpeed, m_driftController.driftSpeed);
		}

		protected override void OnTriggerExit(Collider other)
		{
			if (!m_reacted)
			{
				base.OnTriggerExit(other);
				if (!(m_driftController == null) && m_info != null && !CheckCarSelfCollider(other) && m_counterCollisions == 0 && m_state == State.Activated && !m_info.touched)
				{
					m_info.minSpeed = Mathf.Min(m_info.minSpeed, m_driftController.driftSpeed);
					m_info.timeDriftPoint = Time.time - m_info.timeEnter;
					m_reacted = true;
					m_driftController.ClippingPointReaction(m_info);
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
				Gizmos.DrawWireCube(m_triggerDetector.center, m_triggerDetector.size);
				Gizmos.color = color2;
				Gizmos.DrawCube(m_triggerDetector.center, m_triggerDetector.size);
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
