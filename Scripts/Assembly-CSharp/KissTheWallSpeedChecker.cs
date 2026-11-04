using CarX;
using UnityEngine;

public class KissTheWallSpeedChecker : MonoBehaviour, IKissTheWallChecker
{
	private Car m_car;

	private float m_minDriftSpeed;

	public void InitChecker(DriftController driftController, KissTheWallCarConfig config)
	{
		m_car = driftController.raceCar.carX;
		m_minDriftSpeed = config.minDriftSpeed;
	}

	public bool CheckCondition()
	{
		return m_car.speedKMH >= m_minDriftSpeed;
	}
}
