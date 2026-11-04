using UnityEngine;

public class RaceCarFromCollderCache
{
	private GameObject m_lastCollObject;

	private RaceCar m_lastRaceCarResult;

	public RaceCar GetCar(Collision collInfo)
	{
		GameObject gameObject = collInfo.gameObject;
		if (gameObject == m_lastCollObject)
		{
			return m_lastRaceCarResult;
		}
		m_lastCollObject = gameObject;
		if (gameObject.layer != 29 && gameObject.layer != 8)
		{
			m_lastRaceCarResult = null;
			return null;
		}
		m_lastRaceCarResult = collInfo.collider.GetComponentInParent<RaceCar>();
		return m_lastRaceCarResult;
	}

	public void Reset()
	{
		m_lastCollObject = null;
		m_lastRaceCarResult = null;
	}
}
