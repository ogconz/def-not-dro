using System;
using UnityEngine;

[AddComponentMenu("Scripts/Car/StartPosition")]
public class StartPosition : MonoBehaviour
{
	private const int MATRIX_RANK = 3;

	public static StartPosition CreateFromCarCurrentState(RaceCar car)
	{
		StartPosition startPosition = new GameObject("StartPosition").AddComponent<StartPosition>();
		startPosition.transform.SetPositionAndRotation(car.getTransform.position, car.getTransform.rotation);
		return startPosition;
	}

	private void ApplyPosRot(RaceCar car)
	{
		if (!BaseCar.IsNull(car) && !(car.getTransform == null) && !(base.transform == null))
		{
			if (Physics.Raycast(new Ray(base.transform.position + Vector3.up, Vector3.down), out var hitInfo, 10f, 2048))
			{
				car.getTransform.SetPositionAndRotation(hitInfo.point + new Vector3(0f, 0.5f, 0f), base.transform.rotation);
			}
			else
			{
				car.getTransform.SetPositionAndRotation(base.transform.position + Vector3.up, base.transform.rotation);
			}
		}
	}

	private void FindEmptyPositionSlot(RaceCar car)
	{
		Transform getTransform = car.getTransform;
		Vector3 position = getTransform.position;
		float num = 1.76f;
		float num2 = 6.04f;
		Vector3 vector = getTransform.right * num * 1.7f;
		Vector3 vector2 = getTransform.forward * num2 * 1.2f;
		Vector3[,] array = new Vector3[3, 3]
		{
			{
				position + vector2 - vector,
				position + vector2,
				position + vector2 + vector
			},
			{
				position - vector,
				position,
				position + vector
			},
			{
				position - vector2 - vector,
				position - vector2,
				position - vector2 + vector
			}
		};
		UnityEngine.Random.InitState(DateTime.Now.Millisecond);
		car.getTransform.position = array[UnityEngine.Random.Range(0, 3), UnityEngine.Random.Range(0, 3)];
	}

	public void ApplyStatic(RaceCar car)
	{
		ApplyPosRot(car);
		FindEmptyPositionSlot(car);
		car.AlignCarPositionAtSpawn();
		car.getRigidbody.velocity = Vector3.zero;
		car.getRigidbody.angularVelocity = Vector3.zero;
		car.carX.accelerate = 0f;
		car.carX.brake = 1f;
		car.carX.rpm = 0f;
		car.carX.SetSteer(0f, useAssistent: false, Time.deltaTime);
		car.carX.gear = 1;
	}

	public void Apply(RaceCar car)
	{
		ApplyPosRot(car);
		car.AlignCarPositionAtSpawn();
		car.getRigidbody.velocity = base.transform.rotation * Vector3.forward * 8.333334f;
		car.getRigidbody.angularVelocity = Vector3.zero;
		car.carX.accelerate = 0.3f;
		car.carX.brake = 0f;
		car.carX.rpm = 3000f;
		car.carX.SetSteer(0f, useAssistent: false, Time.deltaTime);
		car.carX.gear = 3;
	}
}
