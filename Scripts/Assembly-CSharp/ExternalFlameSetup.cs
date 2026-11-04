using System.Collections.Generic;
using CarModelSystem;
using UnityEngine;

public class ExternalFlameSetup : ExternalCarPartPostProcess
{
	private TypeID m_exhaustTypeID = new TypeID("Exhaust");

	public ExternalFlameSetup()
	{
		m_keywordsPerFormat = new Dictionary<string, string> { { "bundle", "flame" } };
		m_defaultKeyword = "flame";
	}

	protected override void Action(RaceCar raceCar, GameObject gameObject)
	{
		if ((bool)gameObject.GetComponent<CarComponentRoot>() && raceCar.carModel.onPartChanged.TryGetValue(m_exhaustTypeID, out var value))
		{
			value.SafeCall();
		}
	}
}
