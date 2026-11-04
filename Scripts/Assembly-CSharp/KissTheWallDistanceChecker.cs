using System;
using UnityEngine;

public class KissTheWallDistanceChecker : MonoBehaviour, IKissTheWallChecker, IKissTheWallDetector
{
	private KissTheWallTriggerChecker[] m_triggerList;

	private KissTheWallCarConfig m_carConfig;

	private DriftController m_driftController;

	private KissTheWallTriggerChecker m_activeChecker;

	public event Action<Vector3, Vector3, float> OnWallDetectedEvent;

	public void InitChecker(DriftController driftController, KissTheWallCarConfig config)
	{
		m_carConfig = config;
		m_driftController = driftController;
		CreateTriggerPoints(driftController.raceCar);
	}

	private void CreateTriggerPoints(RaceCar car)
	{
		if (!(car == null))
		{
			car.CreateWallCheckPoints(base.transform, out var rearLeftPoint, out var rearRightPoint);
			m_triggerList = new KissTheWallTriggerChecker[2]
			{
				CreatTriggerPoint(rearLeftPoint.gameObject, right: false, m_carConfig),
				CreatTriggerPoint(rearRightPoint.gameObject, right: true, m_carConfig)
			};
		}
	}

	private KissTheWallTriggerChecker CreatTriggerPoint(GameObject pointGO, bool right, KissTheWallCarConfig config)
	{
		KissTheWallTriggerChecker kissTheWallTriggerChecker = pointGO.AddComponent<KissTheWallTriggerChecker>();
		kissTheWallTriggerChecker.Initialize(config, right);
		return kissTheWallTriggerChecker;
	}

	public bool CheckCondition()
	{
		if (m_triggerList == null)
		{
			return false;
		}
		m_activeChecker = GetActivePoint();
		if (m_activeChecker != null && (bool)m_activeChecker.GetNearestWall())
		{
			float magnitude = m_activeChecker.vector.magnitude;
			Vector3 position = m_activeChecker.transform.position;
			this.OnWallDetectedEvent(position, position + m_activeChecker.vector, magnitude);
			if (magnitude <= m_carConfig.detectDistance)
			{
				return true;
			}
		}
		return false;
	}

	private KissTheWallTriggerChecker GetActivePoint()
	{
		return m_driftController.driftSide switch
		{
			DriftController.DriftSide.Right => GetLeftPoint(), 
			DriftController.DriftSide.Left => GetRightPoint(), 
			_ => null, 
		};
	}

	private KissTheWallTriggerChecker GetLeftPoint()
	{
		KissTheWallTriggerChecker[] triggerList = m_triggerList;
		foreach (KissTheWallTriggerChecker kissTheWallTriggerChecker in triggerList)
		{
			if (!kissTheWallTriggerChecker.right)
			{
				return kissTheWallTriggerChecker;
			}
		}
		return null;
	}

	private KissTheWallTriggerChecker GetRightPoint()
	{
		KissTheWallTriggerChecker[] triggerList = m_triggerList;
		foreach (KissTheWallTriggerChecker kissTheWallTriggerChecker in triggerList)
		{
			if (kissTheWallTriggerChecker.right)
			{
				return kissTheWallTriggerChecker;
			}
		}
		return null;
	}
}
