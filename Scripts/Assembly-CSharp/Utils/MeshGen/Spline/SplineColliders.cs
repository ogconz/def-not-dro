using System;
using System.Collections.Generic;
using CarX.Gameplay.ClippingZone;
using UnityEngine;

namespace Utils.MeshGen.Spline
{
	[ExecuteInEditMode]
	public class SplineColliders : MonoBehaviour
	{
		private class ObjectDescriptor
		{
			public int refCounter;

			public int lockFrame;

			public int stayFrame;

			public ObjectDescriptor(int lockFrame)
			{
				this.lockFrame = lockFrame;
				stayFrame = 0;
				refCounter = 1;
			}
		}

		private BezierSpline m_spline;

		[SerializeField]
		private bool m_inverseFailZone;

		[SerializeField]
		private float m_failZoneOffset = 10f;

		[SerializeField]
		private float m_failZoneWidth = 40f;

		[SerializeField]
		private float m_lengthExtends;

		[SerializeField]
		private float m_width = 1f;

		[SerializeField]
		private float m_height = 1f;

		[SerializeField]
		private float m_heightOffset;

		[SerializeField]
		private bool m_isTrigger;

		[SerializeField]
		private PhysicMaterial m_physicsMaterial;

		private List<BoxCollider> m_colliders = new List<BoxCollider>();

		public int countColliders;

		public bool refreshWithRaycast;

		public Action<Collider> onTriggerEnter;

		public Action<Collider> onTriggerStay;

		public Action<Collider> onTriggerExit;

		private Dictionary<int, ObjectDescriptor> m_objectsStayTrigger = new Dictionary<int, ObjectDescriptor>();

		[HideInInspector]
		[SerializeField]
		private GameObject m_collisionsCollector;

		private int fixedFrame;

		private void Start()
		{
			m_spline = GetComponent<BezierSpline>();
			if (m_collisionsCollector == null)
			{
				Transform transform = base.transform.Find("[CollisionsSpline]");
				if (transform == null)
				{
					m_collisionsCollector = new GameObject("[CollisionsSpline]");
					m_collisionsCollector.transform.parent = base.transform;
				}
				else
				{
					m_collisionsCollector = transform.gameObject;
				}
			}
		}

		private void OnEnable()
		{
			Start();
			m_objectsStayTrigger.Clear();
		}

		public void RefreshColliders()
		{
			OrientedPoint[] orietedPoints = m_spline.GetOrietedPoints(countColliders);
			if (m_collisionsCollector.transform.childCount > 0)
			{
				for (int num = m_collisionsCollector.transform.childCount - 1; num >= 0; num--)
				{
					UnityEngine.Object.DestroyImmediate(m_collisionsCollector.transform.GetChild(num).gameObject);
				}
			}
			BoxCollider[] components = m_collisionsCollector.GetComponents<BoxCollider>();
			for (int i = 0; i < components.Length; i++)
			{
				UnityEngine.Object.Destroy(components[i].gameObject);
			}
			m_colliders.Clear();
			for (int j = 1; j < orietedPoints.Length; j++)
			{
				GameObject obj = new GameObject("[Collider_" + base.gameObject.name + "_" + j + "]");
				obj.transform.parent = m_collisionsCollector.transform;
				obj.layer = m_collisionsCollector.gameObject.layer;
				BoxCollider collider = obj.AddComponent<BoxCollider>();
				obj.AddComponent<SplineColliderConnector>();
				Vector3 vector;
				Vector3 to;
				if (refreshWithRaycast)
				{
					vector = GetRaycastPoint(base.transform.TransformPoint(orietedPoints[j - 1].position));
					to = GetRaycastPoint(base.transform.TransformPoint(orietedPoints[j].position));
				}
				else
				{
					vector = base.transform.TransformPoint(orietedPoints[j - 1].position);
					to = base.transform.TransformPoint(orietedPoints[j].position);
				}
				SetupCollider(collider, vector, to);
			}
			if (m_colliders.Count > 0 && m_failZoneWidth != 0f)
			{
				Transform transform = (m_inverseFailZone ? m_colliders.Last().transform : m_colliders[0].transform);
				Vector3 vector2 = (m_inverseFailZone ? transform.forward : (-transform.forward));
				GameObject obj2 = new GameObject("[FailZone]");
				obj2.transform.parent = m_collisionsCollector.transform;
				obj2.layer = m_collisionsCollector.gameObject.layer;
				BoxCollider collider2 = obj2.AddComponent<BoxCollider>();
				obj2.AddComponent<ClippingFailGate>().Capture();
				Vector3 vector3;
				Vector3 to2;
				if (refreshWithRaycast)
				{
					vector3 = GetRaycastPoint(transform.position + 2f * vector2);
					to2 = GetRaycastPoint(transform.position + 2.1f * vector2);
				}
				else
				{
					vector3 = transform.position + 2f * vector2;
					to2 = transform.position + 2.1f * vector2;
				}
				SetupCollider(collider2, vector3, to2, failzone: true);
			}
		}

		private Vector3 GetRaycastPoint(Vector3 globalPoint)
		{
			RaycastHit[] array = Physics.RaycastAll(globalPoint + Vector3.up * 2f, Vector3.down, 10f, -1073741825);
			Vector3 vector = globalPoint;
			if (array.Length != 0)
			{
				vector = array[0].point;
			}
			return vector + base.transform.up * 0.001f;
		}

		private void SetupCollider(BoxCollider collider, Vector3 from, Vector3 to, bool failzone = false)
		{
			float num = (m_inverseFailZone ? m_failZoneOffset : (0f - m_failZoneOffset));
			Vector3 forward = to - from;
			collider.transform.rotation = Quaternion.LookRotation(forward);
			collider.transform.position = from;
			float num2 = (failzone ? 0.1f : (forward.magnitude + m_lengthExtends));
			collider.center = new Vector3(failzone ? num : 0f, m_height * 0.5f + m_heightOffset, num2 * 0.5f - m_lengthExtends * 0.5f);
			collider.size = new Vector3(failzone ? m_failZoneWidth : m_width, m_height, num2);
			collider.isTrigger = m_isTrigger;
			collider.material = m_physicsMaterial;
			if (!failzone)
			{
				m_colliders.Add(collider);
			}
		}

		private void FixedUpdate()
		{
			fixedFrame++;
		}

		public void CallTriggerEnter(Collider collider)
		{
			Rigidbody attachedRigidbody = collider.attachedRigidbody;
			if (attachedRigidbody == null)
			{
				return;
			}
			int instanceID = attachedRigidbody.GetInstanceID();
			if (!m_objectsStayTrigger.ContainsKey(instanceID))
			{
				m_objectsStayTrigger[instanceID] = new ObjectDescriptor(fixedFrame);
				if (onTriggerEnter != null)
				{
					onTriggerEnter(collider);
				}
				return;
			}
			ObjectDescriptor objectDescriptor = m_objectsStayTrigger[instanceID];
			objectDescriptor.refCounter++;
			if (objectDescriptor.lockFrame != fixedFrame)
			{
				objectDescriptor.lockFrame = fixedFrame;
				if (onTriggerStay != null)
				{
					onTriggerStay(collider);
				}
			}
		}

		public void CallTriggerStay(Collider collider)
		{
			Rigidbody attachedRigidbody = collider.attachedRigidbody;
			if (attachedRigidbody == null)
			{
				return;
			}
			int instanceID = attachedRigidbody.GetInstanceID();
			if (m_objectsStayTrigger.TryGetValue(instanceID, out var value) && value.stayFrame != fixedFrame)
			{
				value.stayFrame = fixedFrame;
				if (onTriggerStay != null)
				{
					onTriggerStay(collider);
				}
			}
		}

		public void CallTriggerExit(Collider collider)
		{
			Rigidbody attachedRigidbody = collider.attachedRigidbody;
			if (attachedRigidbody == null)
			{
				return;
			}
			int instanceID = attachedRigidbody.GetInstanceID();
			if (!m_objectsStayTrigger.TryGetValue(instanceID, out var value))
			{
				return;
			}
			value.refCounter--;
			if (value.refCounter <= 0)
			{
				m_objectsStayTrigger.Remove(instanceID);
				if (onTriggerExit != null)
				{
					onTriggerExit(collider);
				}
			}
		}

		public bool Contains(Vector3 point)
		{
			for (int i = 0; i < countColliders; i++)
			{
				if (m_colliders[i].bounds.Contains(point))
				{
					return true;
				}
			}
			return false;
		}
	}
}
