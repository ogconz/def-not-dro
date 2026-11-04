using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrackLayout.Behaviours
{
	public class CompositeTriggerZone : MonoBehaviour
	{
		private class ObjectState
		{
			public int refCounter;

			public float lockTime;

			public float stayTime;

			public ObjectState(float time)
			{
				lockTime = time;
				stayTime = 0f;
				refCounter = 1;
			}
		}

		public bool onlyRigidbody;

		public Action<Collider> onTriggerEnter;

		public Action<Collider> onTriggerStay;

		public Action<Collider> onTriggerExit;

		private Dictionary<int, ObjectState> m_objects = new Dictionary<int, ObjectState>();

		private UnityEngine.Object GetObject(Collider collider)
		{
			if (!onlyRigidbody)
			{
				return collider;
			}
			return collider.attachedRigidbody;
		}

		private void OnEnable()
		{
			if (m_objects.Count > 0)
			{
				m_objects.Clear();
			}
		}

		public void OnTriggerEnter(Collider collider)
		{
			UnityEngine.Object obj = GetObject(collider);
			if (obj == null)
			{
				return;
			}
			int instanceID = obj.GetInstanceID();
			if (!m_objects.ContainsKey(instanceID))
			{
				m_objects[instanceID] = new ObjectState(Time.fixedTime);
				if (onTriggerEnter != null)
				{
					onTriggerEnter(collider);
				}
				return;
			}
			ObjectState objectState = m_objects[instanceID];
			objectState.refCounter++;
			if (objectState.lockTime != Time.fixedTime)
			{
				objectState.lockTime = Time.fixedTime;
				if (onTriggerStay != null)
				{
					onTriggerStay(collider);
				}
			}
		}

		public void OnTriggerStay(Collider collider)
		{
			UnityEngine.Object obj = GetObject(collider);
			if (obj == null)
			{
				return;
			}
			int instanceID = obj.GetInstanceID();
			if (m_objects.TryGetValue(instanceID, out var value) && value.stayTime != Time.fixedTime)
			{
				value.stayTime = Time.fixedTime;
				if (onTriggerStay != null)
				{
					onTriggerStay(collider);
				}
			}
		}

		public void OnTriggerExit(Collider collider)
		{
			UnityEngine.Object obj = GetObject(collider);
			if (obj == null)
			{
				return;
			}
			int instanceID = obj.GetInstanceID();
			if (!m_objects.TryGetValue(instanceID, out var value))
			{
				return;
			}
			value.refCounter--;
			if (value.refCounter <= 0)
			{
				m_objects.Remove(instanceID);
				if (onTriggerExit != null)
				{
					onTriggerExit(collider);
				}
			}
		}
	}
}
