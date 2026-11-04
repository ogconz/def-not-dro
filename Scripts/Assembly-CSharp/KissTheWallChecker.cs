using System;
using System.Collections.Generic;
using UnityEngine;

public class KissTheWallChecker : MonoBehaviour
{
	private List<IKissTheWallChecker> m_checkers;

	private DriftController m_driftController;

	private KissTheWallCarConfig m_config;

	private IKissTheWallDetector m_distancer;

	public void Init(DriftController driftController, KissTheWallCarConfig carConfig)
	{
		m_driftController = driftController;
		m_config = carConfig;
		m_checkers = new List<IKissTheWallChecker>();
		CreateAllCheckers();
	}

	private void CreateAllCheckers()
	{
		GameObject gameObject = new GameObject("KissTheWallCheckers");
		gameObject.transform.SetParent(base.transform, worldPositionStays: false);
		m_checkers.Add(gameObject.AddComponent<KissTheWallAngleChecker>());
		m_checkers.Add(gameObject.AddComponent<KissTheWallSpeedChecker>());
		m_checkers.Add(gameObject.AddComponent<KissTheWallDistanceChecker>());
		m_distancer = gameObject.GetComponent<IKissTheWallDetector>();
		foreach (IKissTheWallChecker checker in m_checkers)
		{
			checker.InitChecker(m_driftController, m_config);
		}
	}

	public bool CheckAllEntryConditions()
	{
		if (m_checkers == null)
		{
			return false;
		}
		foreach (IKissTheWallChecker checker in m_checkers)
		{
			if (!checker.CheckCondition())
			{
				return false;
			}
		}
		return true;
	}

	public void SubscribeWallDetection(Action<Vector3, Vector3, float> action)
	{
		if (m_distancer != null)
		{
			m_distancer.OnWallDetectedEvent += action;
		}
	}
}
