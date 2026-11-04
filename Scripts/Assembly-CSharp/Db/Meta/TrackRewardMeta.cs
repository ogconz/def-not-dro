using System;
using System.Xml.Serialization;
using UnityEngine;

namespace DB.Meta
{
	[Serializable]
	public class TrackRewardMeta : IDBMeta, IRaceAward
	{
		[XmlAttribute]
		public int trackId;

		[XmlAttribute]
		public int goldCoins;

		[XmlAttribute]
		public int silverCoins;

		[XmlAttribute]
		public int bronzeCoins;

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

		public int GetAwardByCup(RaceCupType cupType)
		{
			return cupType switch
			{
				RaceCupType.Gold => goldCoins, 
				RaceCupType.Silver => silverCoins, 
				RaceCupType.Bronze => bronzeCoins, 
				_ => 0, 
			};
		}
	}
}
