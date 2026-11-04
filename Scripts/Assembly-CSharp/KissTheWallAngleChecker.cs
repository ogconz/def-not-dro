using UnityEngine;

public class KissTheWallAngleChecker : MonoBehaviour, IKissTheWallChecker
{
	private DriftController m_driftController;

	private float m_minDriftAngle;

	public void InitChecker(DriftController driftController, KissTheWallCarConfig config)
	{
		m_driftController = driftController;
		m_minDriftAngle = config.minDriftAngle;
	}

	public bool CheckCondition()
	{
		return m_driftController.driftAngle >= m_minDriftAngle;
	}
}
