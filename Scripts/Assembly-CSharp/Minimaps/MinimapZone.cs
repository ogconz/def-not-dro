using UnityEngine;

namespace Minimaps
{
	public class MinimapZone
	{
		private readonly float m_fromDist;

		private readonly float m_toDist;

		public int zoneId { get; private set; }

		public int? fromZoneId { get; private set; }

		public MinimapZone(float fromDist, float toDist, int stageId, int? fromStageId = null)
		{
			m_fromDist = fromDist;
			m_toDist = toDist;
			zoneId = stageId;
			fromZoneId = fromStageId;
		}

		public bool Exists(float dist)
		{
			if (dist >= m_fromDist)
			{
				return dist <= m_toDist;
			}
			return false;
		}

		public float GetProgress(float dist)
		{
			if (m_toDist != m_fromDist)
			{
				return Mathf.Clamp01((dist - m_fromDist) / (m_toDist - m_fromDist));
			}
			return 0f;
		}
	}
}
