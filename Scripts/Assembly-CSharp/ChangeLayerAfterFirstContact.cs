using System.Collections.Generic;
using UnityEngine;

public class ChangeLayerAfterFirstContact : MonoBehaviour
{
	private static int DoNotCarLayerMask = -1;

	private static int LocalCarLayerMask = -1;

	private List<Collider> m_colliders;

	private void Awake()
	{
		if (LocalCarLayerMask == -1)
		{
			LocalCarLayerMask = LayerMask.NameToLayer("LocalCar");
		}
		if (DoNotCarLayerMask == -1)
		{
			DoNotCarLayerMask = LayerMask.NameToLayer("DoNotCar");
		}
	}

	private void OnCollisionEnter(Collision other)
	{
		if (other.rigidbody != null && other.rigidbody.gameObject.layer == LocalCarLayerMask)
		{
			SetLayer(DoNotCarLayerMask);
		}
	}

	public void Init(List<Collider> colliders)
	{
		m_colliders = colliders;
	}

	public void SetLayer(int layer)
	{
		foreach (Collider collider in m_colliders)
		{
			collider.gameObject.layer = layer;
		}
	}
}
