using UnityEngine;

public class WheelsParticlesObstacle : MonoBehaviour
{
	public float radius;

	private void Awake()
	{
		SphereCollider orAddComponent = base.gameObject.GetOrAddComponent<SphereCollider>();
		orAddComponent.center = Vector3.zero;
		orAddComponent.radius = radius * 3f;
		base.gameObject.layer = 23;
		Object.Destroy(this);
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(base.transform.position, radius);
	}
}
