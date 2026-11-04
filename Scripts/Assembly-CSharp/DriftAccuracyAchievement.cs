using System;
using UnityEngine;

public class DriftAccuracyAchievement : BaseDriftAchievement
{
	private readonly Vector2[] dataTable;

	private const float ANGLE_DELTA = 10f;

	private float m_prevAngle;

	private float m_timer;

	private int m_startIndx;

	public DriftAccuracyAchievement()
	{
		Reset();
	}

	public DriftAccuracyAchievement(DriftController driftController, Vector2[] data)
	{
		Init(driftController);
		dataTable = data;
		base.AchieveType = AchievementType.Accuracy;
	}

	public override void Update(float dt)
	{
		if (!m_driftController || !m_driftController.isDrift)
		{
			return;
		}
		float driftAngle = m_driftController.driftAngle;
		if (Math.Abs(driftAngle - m_prevAngle) > 10f)
		{
			m_prevAngle = driftAngle;
			Reset();
			m_isTracking = true;
			return;
		}
		m_timer += dt;
		if (m_isTracking)
		{
			if (m_startIndx > dataTable.Length - 1)
			{
				m_isTracking = false;
			}
			int num = FindIndex(m_timer, dataTable, m_startIndx);
			if (num != -1)
			{
				onAchievementReceive(base.AchieveType, num, dataTable[num].y);
				m_startIndx++;
			}
		}
	}

	public override void Reset()
	{
		m_startIndx = 0;
		m_timer = 0f;
		base.Reset();
	}
}
