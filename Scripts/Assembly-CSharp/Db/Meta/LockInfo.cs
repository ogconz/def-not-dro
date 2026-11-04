using System;
using System.Xml.Serialization;
using UnityEngine;

namespace DB.Meta
{
	[Serializable]
	public class LockInfo
	{
		[SerializeField]
		private int m_level;

		[SerializeField]
		private GameGlobalFeature m_feature;

		[XmlAttribute]
		public int level
		{
			get
			{
				return m_level;
			}
			set
			{
				m_level = value;
			}
		}

		[XmlAttribute]
		public GameGlobalFeature feature
		{
			get
			{
				return m_feature;
			}
			set
			{
				m_feature = value;
			}
		}
	}
}
