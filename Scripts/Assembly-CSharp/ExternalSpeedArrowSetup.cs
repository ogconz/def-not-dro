using System.Collections.Generic;
using UnityEngine;

public class ExternalSpeedArrowSetup : ExternalCarPartPostProcess
{
	public ExternalSpeedArrowSetup()
	{
		m_keywordsPerFormat = new Dictionary<string, string> { { "bundle", "speed" } };
		m_defaultKeyword = "speed_$assimpfbx$_rotation";
	}

	protected override void Action(RaceCar raceCar, GameObject gameObject)
	{
		gameObject.SetActive(value: false);
		CarInteriorRotatingPart carInteriorRotatingPart = gameObject.AddComponent<CarInteriorRotatingPart>();
		carInteriorRotatingPart.part = CarInteriorRotatingPart.RotatingPart.SpeedArrow;
		carInteriorRotatingPart.maxAngle = 270f;
		carInteriorRotatingPart.maxValue = 300f;
		carInteriorRotatingPart.axis = Vector3.forward;
		gameObject.SetActive(value: true);
	}
}
