using System.Collections.Generic;
using UnityEngine;

public class ExternalDrivingWheelSetup : ExternalCarPartPostProcess
{
	public ExternalDrivingWheelSetup()
	{
		m_keywordsPerFormat = new Dictionary<string, string> { { "bundle", "steering_wheel" } };
		m_defaultKeyword = "steering_wheel_$assimpfbx$_rotation";
	}

	protected override void Action(RaceCar raceCar, GameObject gameObject)
	{
		gameObject.SetActive(value: false);
		CarInteriorRotatingPart carInteriorRotatingPart = gameObject.AddComponent<CarInteriorRotatingPart>();
		carInteriorRotatingPart.part = CarInteriorRotatingPart.RotatingPart.DrivingWheel;
		carInteriorRotatingPart.maxAngle = 270f;
		carInteriorRotatingPart.axis = Vector3.forward;
		gameObject.SetActive(value: true);
	}
}
