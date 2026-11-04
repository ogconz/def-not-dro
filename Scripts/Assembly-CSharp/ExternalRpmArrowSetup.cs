using System.Collections.Generic;
using UnityEngine;

public class ExternalRpmArrowSetup : ExternalCarPartPostProcess
{
	public ExternalRpmArrowSetup()
	{
		m_keywordsPerFormat = new Dictionary<string, string> { { "bundle", "rpm" } };
		m_defaultKeyword = "rpm_$assimpfbx$_rotation";
	}

	protected override void Action(RaceCar raceCar, GameObject gameObject)
	{
		gameObject.SetActive(value: false);
		CarInteriorRotatingPart carInteriorRotatingPart = gameObject.AddComponent<CarInteriorRotatingPart>();
		carInteriorRotatingPart.part = CarInteriorRotatingPart.RotatingPart.TahoArrow;
		carInteriorRotatingPart.maxAngle = 270f;
		carInteriorRotatingPart.maxValue = 10000f;
		carInteriorRotatingPart.axis = Vector3.forward;
		gameObject.SetActive(value: true);
	}
}
