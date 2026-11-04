using System.Collections;
using CarX;
using UnityEngine;
using UnityEngine.Events;

public class ControlDestroy : MonoBehaviour
{
	public Transform[] destroyedObjects;

	public Transform[] disableObjects;

	public Transform[] enableObjects;

	public Rigidbody[] kinematicDisable;

	public UnityEvent onDestroy;

	private Collider _collider;

	public float force = 10f;

	public string prefab;

	private void Start()
	{
		_collider = GetComponent<Collider>();
	}

	private IEnumerator Respawn()
	{
		yield return new WaitForSeconds(60f);
		if (!string.IsNullOrEmpty(prefab))
		{
			ResourcesWrapper.InstantiateAsync(prefab, base.transform.position, base.transform.rotation, base.transform.parent);
		}
		yield return new WaitForSeconds(1f);
		Object.Destroy(base.gameObject);
	}

	private void OnTriggerEnter(Collider collision)
	{
		if (collision.gameObject.layer != 29 && collision.gameObject.layer != 8)
		{
			return;
		}
		_collider.enabled = false;
		for (int num = destroyedObjects.Length - 1; num >= 0; num--)
		{
			Object.Destroy(destroyedObjects[num]);
		}
		Transform[] array = disableObjects;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].gameObject.SetActive(value: false);
		}
		array = enableObjects;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].gameObject.SetActive(value: true);
		}
		RaceCar component = collision.transform.root.GetComponent<RaceCar>();
		Car car = ((component != null) ? component.carX : null);
		if (car != null)
		{
			for (int j = 0; j < kinematicDisable.Length; j++)
			{
				Rigidbody obj = kinematicDisable[j];
				obj.isKinematic = false;
				Vector3 vector = car.velocity * force;
				obj.AddForce(vector, ForceMode.Impulse);
			}
		}
		StartCoroutine(Respawn());
		onDestroy.Invoke();
	}
}
