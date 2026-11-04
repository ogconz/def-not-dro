using System;
using UnityEngine;

public class DriftControllerStats : ScriptableSingleton<DriftControllerStats>
{
	[Serializable]
	public struct DpProgressInfo
	{
		public float thresholdDpSpeed;

		public float decreaseTime;
	}

	[SerializeField]
	private float m_drBeginTimeLimit = 1f;

	[SerializeField]
	private float m_drNoTimeLimit = 5f;

	[SerializeField]
	private float m_calcDpCoeff = 0.015f;

	[SerializeField]
	private bool m_useSpeedMultiplier;

	[SerializeField]
	private float m_comboDelta = 0.2f;

	[SerializeField]
	private int m_minCombo = 1;

	[SerializeField]
	private int m_maxCombo = 5;

	[SerializeField]
	private int[] m_sequenceDp = new int[5] { 50, 50, 50, 50, 50 };

	public float angleBackwardLimit = 120f;

	public float angleBackwardMin = 90f;

	[SerializeField]
	private DpProgressInfo[] m_dpProgressInfo = new DpProgressInfo[4]
	{
		new DpProgressInfo
		{
			thresholdDpSpeed = 12f,
			decreaseTime = 4f
		},
		new DpProgressInfo
		{
			thresholdDpSpeed = 12f,
			decreaseTime = 3f
		},
		new DpProgressInfo
		{
			thresholdDpSpeed = 12f,
			decreaseTime = 2f
		},
		new DpProgressInfo
		{
			thresholdDpSpeed = 12f,
			decreaseTime = 2f
		}
	};

	[SerializeField]
	private Vector3 m_qualityDpSpeed = new Vector3(25f, 45f, 60f);

	[SerializeField]
	private Vector2[] m_speedAchieveTable = new Vector2[2]
	{
		new Vector2(95f, 500f),
		new Vector2(120f, 1000f)
	};

	[SerializeField]
	private Vector2[] m_changeAchieveTable = new Vector2[2]
	{
		new Vector2(95f, 100f),
		new Vector2(120f, 300f)
	};

	[SerializeField]
	private Vector2[] m_angleAchieveTable = new Vector2[2]
	{
		new Vector2(50f, 100f),
		new Vector2(60f, 200f)
	};

	[SerializeField]
	private Vector2[] m_accuracyAchieveTable = new Vector2[2]
	{
		new Vector2(3f, 100f),
		new Vector2(4f, 300f)
	};

	[SerializeField]
	private Vector2[] m_qualityAchieveTable = new Vector2[2]
	{
		new Vector2(45f, 100f),
		new Vector2(65f, 200f)
	};

	[SerializeField]
	private Vector2[] m_jumpAchieveTable = new Vector2[1]
	{
		new Vector2(0.2f, 150f)
	};

	[SerializeField]
	private Vector2[] m_backwardAchieveTable = new Vector2[2]
	{
		new Vector2(0.02f, 150f),
		new Vector2(0.2f, 1200f)
	};

	[SerializeField]
	private Vector2[] m_kissAchieveTable = new Vector2[1]
	{
		new Vector2(1f, 150f)
	};

	[SerializeField]
	private Vector2[] m_clippingZoneAchieveTable = new Vector2[3]
	{
		new Vector2(0f, 1000f),
		new Vector2(1f, 1500f),
		new Vector2(2f, 2500f)
	};

	[SerializeField]
	private Vector2[] m_clippingPointAchieveTable = new Vector2[4]
	{
		new Vector2(0f, 1500f),
		new Vector2(1f, 2500f),
		new Vector2(2f, 0f),
		new Vector2(3f, 0f)
	};

	[SerializeField]
	private Vector2[] m_clippingCustomAchieveTable = new Vector2[4]
	{
		new Vector2(0f, 1000f),
		new Vector2(1f, 2000f),
		new Vector2(2f, 0f),
		new Vector2(3f, 0f)
	};

	public float drBeginTimeLimit => m_drBeginTimeLimit;

	public float drNoTimeLimit => m_drNoTimeLimit;

	public float calcDpCoeff => m_calcDpCoeff;

	public bool useSpeedMultiplier => m_useSpeedMultiplier;

	public float comboDelta => m_comboDelta;

	public int minCombo => m_minCombo;

	public int maxCombo => m_maxCombo;

	public int sequenceDpLength => m_sequenceDp.Length;

	public float qualityDpSpeed_1 => m_qualityDpSpeed.x;

	public float qualityDpSpeed_2 => m_qualityDpSpeed.y;

	public float qualityDpSpeed_3 => m_qualityDpSpeed.z;

	public int GetSequenceDp(int index)
	{
		if (index < 0)
		{
			return m_sequenceDp[0];
		}
		if (index >= 0 && index < m_sequenceDp.Length)
		{
			return m_sequenceDp[index];
		}
		return m_sequenceDp[m_sequenceDp.Length - 1];
	}

	public DpProgressInfo GetDpProgressInfo(int index)
	{
		if (index < 0)
		{
			return m_dpProgressInfo[0];
		}
		if (index >= 0 && index < m_dpProgressInfo.Length)
		{
			return m_dpProgressInfo[index];
		}
		return m_dpProgressInfo[m_dpProgressInfo.Length - 1];
	}

	public Vector2[] GetAchievementTable(BaseDriftAchievement.AchievementType achType)
	{
		return achType switch
		{
			BaseDriftAchievement.AchievementType.Speed => m_speedAchieveTable, 
			BaseDriftAchievement.AchievementType.SideChange => m_changeAchieveTable, 
			BaseDriftAchievement.AchievementType.Angle => m_angleAchieveTable, 
			BaseDriftAchievement.AchievementType.Accuracy => m_accuracyAchieveTable, 
			BaseDriftAchievement.AchievementType.Quality => m_qualityAchieveTable, 
			BaseDriftAchievement.AchievementType.Jump => m_jumpAchieveTable, 
			BaseDriftAchievement.AchievementType.Backward => m_backwardAchieveTable, 
			BaseDriftAchievement.AchievementType.KissTheWall => m_kissAchieveTable, 
			BaseDriftAchievement.AchievementType.ClippingZone => m_clippingZoneAchieveTable, 
			BaseDriftAchievement.AchievementType.ClippingPoint => m_clippingPointAchieveTable, 
			BaseDriftAchievement.AchievementType.CustomForEvent => m_clippingCustomAchieveTable, 
			_ => null, 
		};
	}
}
