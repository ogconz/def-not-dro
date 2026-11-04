using System.Collections.Generic;
using UnityEngine;

public class ExternalPopExhaustSetup : ExternalCarPartPostProcess
{
	public ExternalPopExhaustSetup()
	{
		m_keywordsPerFormat = new Dictionary<string, string> { { "bundle", "exhaust" } };
		m_defaultKeyword = "exhaust_$assimpfbx$_translation";
	}

	protected override void Action(RaceCar raceCar, GameObject gameObject)
	{
		Transform transform = raceCar.transform;
		CarPopExhaust carPopExhaust = transform.GetComponentInChildren<CarPopExhaust>(includeInactive: true);
		if (carPopExhaust == null)
		{
			GameObject gameObject2 = new GameObject("Exhaust");
			gameObject2.SetActive(value: false);
			gameObject2.transform.SetParent(transform, worldPositionStays: false);
			carPopExhaust = gameObject2.AddComponent<CarPopExhaust>();
		}
		carPopExhaust.PreAddExhaustPoint(gameObject.transform);
	}
}
