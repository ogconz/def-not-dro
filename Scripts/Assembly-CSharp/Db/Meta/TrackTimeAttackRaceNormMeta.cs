using System;
using System.Xml.Serialization;
using UnityEngine;

namespace DB.Meta
{
	[Serializable]
	public class TrackTimeAttackRaceNormMeta : IDBMeta, IRaceNorm
	{
		[XmlAttribute]
		public int carId;

		[XmlAttribute]
		public int trackId;

		[XmlAttribute]
		public int goldTime;

		[XmlAttribute]
		public int silverTime;

		[XmlAttribute]
		public int bronzeTime;

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

		public RaceCupType GetCupByValue(float time)
		{
			if (time <= ConvertToTime(goldTime))
			{
				return RaceCupType.Gold;
			}
			if (time <= ConvertToTime(silverTime))
			{
				return RaceCupType.Silver;
			}
			if (time <= ConvertToTime(bronzeTime))
			{
				return RaceCupType.Bronze;
			}
			return RaceCupType.None;
		}

		public float GetNormByCup(RaceCupType cupType)
		{
			return cupType switch
			{
				RaceCupType.Gold => ConvertToTime(goldTime), 
				RaceCupType.Silver => ConvertToTime(silverTime), 
				RaceCupType.Bronze => ConvertToTime(bronzeTime), 
				_ => 2.1474836E+09f, 
			};
		}

		public string GetNormAsString(RaceCupType cupType)
		{
			if (cupType != RaceCupType.None)
			{
				return UIUtils.TimeToMSMString(GetNormByCup(cupType));
			}
			return UIUtils.TimeToMSMString(null);
		}

		private float ConvertToTime(float rawTime)
		{
			return rawTime / 10000f;
		}
	}
}
