using System;
using System.Xml.Serialization;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;

namespace DB.Meta
{
	[Serializable]
	public abstract class CarVisualTuningMeta : IDBMeta, IDBCanCost
	{
		[SerializeField]
		private int m_id;

		[SerializeField]
		private ObscuredInt m_cost;

		[SerializeField]
		private bool m_defaultLocked;

		[SerializeField]
		private int m_guiIndex;

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

		[XmlAttribute]
		public int cost
		{
			get
			{
				return m_cost;
			}
			set
			{
				m_cost = value;
			}
		}

		[XmlAttribute]
		public bool defaultLocked
		{
			get
			{
				return m_defaultLocked;
			}
			set
			{
				m_defaultLocked = value;
			}
		}

		[XmlAttribute]
		public int guiIndex
		{
			get
			{
				return m_guiIndex;
			}
			set
			{
				m_guiIndex = value;
			}
		}
	}
}
