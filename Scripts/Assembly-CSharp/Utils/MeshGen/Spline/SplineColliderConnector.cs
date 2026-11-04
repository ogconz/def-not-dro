using UnityEngine;

namespace Utils.MeshGen.Spline
{
	public class SplineColliderConnector : MonoBehaviour
	{
		private SplineColliders m_splineColliders;

		private void Start()
		{
			if (m_splineColliders == null)
			{
				m_splineColliders = base.transform.parent.parent.GetComponent<SplineColliders>();
			}
		}

		private void OnEnable()
		{
			Start();
		}

		private void OnTriggerEnter(Collider other)
		{
			if (m_splineColliders != null)
			{
				m_splineColliders.CallTriggerEnter(other);
			}
		}

		private void OnTriggeStay(Collider other)
		{
			if (m_splineColliders != null)
			{
				m_splineColliders.CallTriggerStay(other);
			}
		}

		private void OnTriggerExit(Collider other)
		{
			if (m_splineColliders != null)
			{
				m_splineColliders.CallTriggerExit(other);
			}
		}
	}
}
