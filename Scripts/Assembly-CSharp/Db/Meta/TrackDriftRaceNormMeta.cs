using System;
using System.Xml.Serialization;
using UnityEngine;

namespace DB.Meta
{
	[Serializable]
	public class TrackDriftRaceNormMeta : IDBMeta, IRaceNorm
	{
		[XmlAttribute]
		public int trackId;

		[XmlAttribute]
		public int goldPoints;

		[XmlAttribute]
		public int silverPoints;

		[XmlAttribute]
		public int bronzePoints;

		[SerializeField]
		private int m_id;

		[XmlAttribute]
		public int id
		{
			get
			{
				return m_id;
			}
			set
			{
				m_id = value;
			}
		}

		public RaceCupType GetCupByValue(float driftPoints)
		{
			if (driftPoints >= (float)goldPoints)
			{
				return RaceCupType.Gold;
			}
			if (driftPoints >= (float)silverPoints)
			{
				return RaceCupType.Silver;
			}
			if (driftPoints >= (float)bronzePoints)
			{
				return RaceCupType.Bronze;
			}
			return RaceCupType.None;
		}

		public float GetNormByCup(RaceCupType cupType)
		{
			return cupType switch
			{
				RaceCupType.Gold => goldPoints, 
				RaceCupType.Silver => silverPoints, 
				RaceCupType.Bronze => bronzePoints, 
				_ => 0f, 
			};
		}

		public string GetNormAsString(RaceCupType cupType)
		{
			if (cupType != RaceCupType.None)
			{
				return UIUtils.DigitsWithSeparate((int)GetNormByCup(cupType));
			}
			return string.Empty;
		}
	}
}
