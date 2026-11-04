using UnityEngine;

public class SkyDome : MonoBehaviour
{
	private Transform m_transform;

	private void Awake()
	{
		m_transform = base.transform;
	}

	private void LateUpdate()
	{
		m_transform.rotation = Quaternion.identity;
	}
}
