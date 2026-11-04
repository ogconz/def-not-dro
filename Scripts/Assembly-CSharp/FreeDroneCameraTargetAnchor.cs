using UnityEngine;

public class FreeDroneCameraTargetAnchor : MonoBehaviour
{
	[SerializeField]
	private LayerMask m_triggerLayer;

	private FreeDroneCameraMovement m_movement;

	private Vector3 m_oldPosition;

	private bool m_stayInCollider;

	private Rigidbody m_rigidbody;

	private void Awake()
	{
		m_movement = GetComponentInParent<FreeDroneCameraMovement>();
		m_rigidbody = GetComponentInParent<Rigidbody>();
	}

	private void OnEnable()
	{
		m_stayInCollider = true;
	}

	private void OnCollisionEnter(Collision other)
	{
		if (!(other.collider != null))
		{
			DisableTrackingMode();
			if (other.contacts.Length != 0)
			{
				m_movement.ReverseMoving(other.contacts[0].normal);
			}
		}
	}

	private void DisableTrackingMode()
	{
		m_movement.DisableTrackingModeByCollision();
	}

	private void LateUpdate()
	{
		if (!m_stayInCollider)
		{
			base.transform.position = m_oldPosition;
			m_rigidbody.velocity = Vector3.zero;
			m_rigidbody.angularVelocity = Vector3.zero;
		}
	}

	private void FixedUpdate()
	{
		m_rigidbody.velocity = Vector3.zero;
		m_rigidbody.angularVelocity = Vector3.zero;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (CheckCollider(other))
		{
			m_stayInCollider = true;
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (CheckCollider(other))
		{
			m_stayInCollider = true;
			m_oldPosition = base.transform.position;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (CheckCollider(other))
		{
			m_stayInCollider = false;
			base.transform.position = m_oldPosition;
			DisableTrackingMode();
		}
	}

	private bool CheckCollider(Collider other)
	{
		if (((1 << other.transform.gameObject.layer) & (int)m_triggerLayer) == 0)
		{
			return false;
		}
		return true;
	}
}
