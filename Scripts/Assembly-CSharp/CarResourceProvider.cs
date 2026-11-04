using UnityEngine;

public class CarResourceProvider : MonoBehaviour
{
	public ParticleSystem collisionSparks;

	public ParticleSystem exhaustFlame;

	public ParticleSystem exhaustSmoke;

	public GameObject driverPrefab;

	public static CarResourceProvider instance { get; private set; }

	private void Awake()
	{
		instance = this;
	}

	private void OnDestroy()
	{
		if (instance == this)
		{
			instance = null;
		}
	}
}
