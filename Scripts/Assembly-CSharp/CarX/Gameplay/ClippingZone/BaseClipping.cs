using System;
using System.Collections;
using UnityEngine;

namespace CarX.Gameplay.ClippingZone
{
	public class BaseClipping : MonoBehaviour
	{
		public enum State
		{
			Idle = 0,
			Activated = 1,
			Failed = 2
		}

		[NonSerialized]
		public int sector;

		protected State m_state;

		protected bool m_cachedController;

		protected DriftController m_driftController;

		protected int m_counterCollisions;

		protected RaceCar m_playerCar;

		public State state => m_state;

		public bool isCarOutside => (float)m_counterCollisions == 0f;

		protected virtual void OnTriggerEnter(Collider other)
		{
			if (!(other == null))
			{
				if (BaseCar.IsNull(m_playerCar))
				{
					m_playerCar = PlayerCarControl.instance.car;
					m_cachedController = false;
				}
				if (!m_cachedController)
				{
					m_driftController = other.transform.GetComponentInParent<DriftController>();
					m_cachedController = m_driftController != null;
				}
				if (IsCar(other))
				{
					m_counterCollisions++;
				}
			}
		}

		protected virtual void OnTriggerStay(Collider other)
		{
		}

		protected virtual void OnTriggerExit(Collider other)
		{
			if (!(other == null) && IsCar(other))
			{
				m_counterCollisions--;
				if (m_state == State.Idle && !(m_driftController == null) && m_counterCollisions == 0)
				{
					m_state = State.Activated;
				}
			}
		}

		public void OnFailGate(Collider other)
		{
			if (IsCar(other) && state == State.Idle && isCarOutside)
			{
				m_state = State.Failed;
			}
		}

		public void FailByRaceEnd()
		{
			if (state == State.Idle)
			{
				m_state = State.Failed;
			}
		}

		public virtual void Reset()
		{
			m_state = State.Idle;
			m_counterCollisions = 0;
		}

		protected IEnumerator ResetActivation()
		{
			yield return new WaitForSeconds(2f);
			Reset();
		}

		public static bool IsCar(Collider collider)
		{
			UIXDSInfoContext instance = UIXDSInfoContext.instance;
			if (instance == null || instance.controller == null)
			{
				return false;
			}
			if (instance.activeXDS && !instance.controller.isSelfTandemLeader)
			{
				return false;
			}
			if (collider.gameObject.layer == 29)
			{
				return !collider.isTrigger;
			}
			return false;
		}

		public static bool IsNetworkCar(Collider collider)
		{
			if (collider.gameObject.layer == 8)
			{
				return !collider.isTrigger;
			}
			return false;
		}

		protected bool CheckCarSelfCollider(Collider collider)
		{
			if (collider.gameObject.layer == 8 && !collider.isTrigger)
			{
				return collider.transform.root.gameObject.GetInstanceID() != m_playerCar.GetInstanceID();
			}
			return false;
		}
	}
}
