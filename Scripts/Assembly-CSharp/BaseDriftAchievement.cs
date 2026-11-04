using System;
using UnityEngine;

public abstract class BaseDriftAchievement
{
	public enum AchievementType
	{
		None = 0,
		Speed = 1,
		SideChange = 2,
		Angle = 3,
		Accuracy = 4,
		Quality = 5,
		Jump = 6,
		Backward = 7,
		ClippingZone = 8,
		ClippingPoint = 9,
		Addition = 10,
		CustomForEvent = 11,
		KissTheWall = 12
	}

	public bool isWorkInTandems = true;

	protected DriftController m_driftController;

	protected bool m_isTracking;

	protected Action<AchievementType, int, float> onAchievementReceive;

	public AchievementType AchieveType { get; protected set; }

	public void Subscribe(Action<AchievementType, int, float> action)
	{
		onAchievementReceive = (Action<AchievementType, int, float>)Delegate.Combine(onAchievementReceive, action);
	}

	public void Unsubscribe(Action<AchievementType, int, float> action)
	{
		onAchievementReceive = (Action<AchievementType, int, float>)Delegate.Remove(onAchievementReceive, action);
	}

	public BaseDriftAchievement DeacivateInTandems()
	{
		isWorkInTandems = false;
		return this;
	}

	public virtual void Init(DriftController driftController)
	{
		m_driftController = driftController;
		Reset();
	}

	public virtual void Reset()
	{
		m_isTracking = false;
	}

	public abstract void Update(float dt);

	protected int FindIndex(float param, Vector2[] table)
	{
		return FindIndex(param, table, 0);
	}

	protected int FindIndex(float param, Vector2[] table, int startIndx)
	{
		if (table == null)
		{
			return -1;
		}
		int result = -1;
		for (int i = startIndx; i < table.Length && param >= table[i].x; i++)
		{
			result = i;
		}
		return result;
	}
}
