using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;

namespace SyncMultiplayer
{
	public class NetworkGameStats
	{
		public class SubroomStat
		{
			public SubroomMode mode;

			public int tryCount;

			public int winCount;
		}

		public const float USER_ACTIVITY_IDLE_TIME = 10f;

		public float gameStart;

		public float badTime;

		private float m_LastUserActivity;

		private ObscuredInt m_pointsAll;

		private ObscuredInt m_pointsPerDrift;

		private ObscuredInt m_xp;

		private ObscuredInt m_challengesCount;

		private Dictionary<SubroomMode, SubroomStat> m_subrumsStats = new Dictionary<SubroomMode, SubroomStat>();

		public int pointsAll
		{
			get
			{
				return m_pointsAll;
			}
			set
			{
				m_pointsAll = value;
			}
		}

		public int pointsPerDrift
		{
			get
			{
				return m_pointsPerDrift;
			}
			set
			{
				m_pointsPerDrift = value;
			}
		}

		public int xp
		{
			get
			{
				return m_xp;
			}
			set
			{
				m_xp = value;
			}
		}

		public int challengesCount
		{
			get
			{
				return m_challengesCount;
			}
			private set
			{
				m_challengesCount = value;
			}
		}

		public NetworkGameStats()
		{
			Clear();
		}

		public void Clear()
		{
			gameStart = Time.unscaledTime;
			m_LastUserActivity = gameStart;
			badTime = 0f;
			pointsAll = 0;
			pointsPerDrift = 0;
			xp = 0;
			challengesCount = 0;
			m_subrumsStats.Clear();
		}

		public ICollection<SubroomStat> GetSubroomStats()
		{
			return m_subrumsStats.Values;
		}

		public void IncSubroomStat(SubroomMode mode, bool win)
		{
			if (!m_subrumsStats.TryGetValue(mode, out var value))
			{
				value = new SubroomStat
				{
					mode = mode
				};
				m_subrumsStats.Add(mode, value);
			}
			value.tryCount++;
			if (win)
			{
				value.winCount++;
			}
			challengesCount++;
		}

		public void SendUserActiveSignal()
		{
			if (Time.unscaledTime - m_LastUserActivity > 10f)
			{
				badTime += Time.unscaledTime - m_LastUserActivity - 10f;
			}
			m_LastUserActivity = Time.unscaledTime;
		}
	}
}
