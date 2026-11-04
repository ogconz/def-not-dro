using System.Collections.Generic;
using UnityEngine;

public class ExternalDriverTransformSetup : ExternalCarPartPostProcess
{
	public ExternalDriverTransformSetup()
	{
		m_keywordsPerFormat = new Dictionary<string, string> { { "bundle", "fpc" } };
		m_defaultKeyword = "fpc";
	}

	protected override void Action(RaceCar raceCar, GameObject gameObject)
	{
		Transform transform = raceCar.transform.FindChildWithTag("DriverAssPoint");
		if ((bool)transform)
		{
			transform.SetPositionAndRotation(gameObject.transform.position - Vector3.up * 0.675f, gameObject.transform.rotation);
		}
	}
}
