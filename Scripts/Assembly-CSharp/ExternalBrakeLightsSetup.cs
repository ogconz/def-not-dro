using System.Collections.Generic;
using UnityEngine;

public class ExternalBrakeLightsSetup : ExternalCarPartPostProcess
{
	public ExternalBrakeLightsSetup()
	{
		m_keywordsPerFormat = new Dictionary<string, string> { { "bundle", "brakelight" } };
		m_defaultKeyword = "brakelight";
	}

	protected override void Action(RaceCar raceCar, GameObject gameObject)
	{
		gameObject.SetActive(value: false);
		gameObject.AddComponent<StopLightFx>();
		gameObject.SetActive(value: true);
	}
}
