using System;
using System.Xml.Serialization;
using UnityEngine;

namespace DB.Meta
{
	[Serializable]
	public class WheelSpokeColorMeta : BaseColorMeta
	{
		[SerializeField]
		private string m_name;

		[SerializeField]
		private int m_carId;

		[XmlAttribute]
		public string name
		{
			get
			{
				return m_name;
			}
			set
			{
				m_name = value;
			}
		}

		[XmlAttribute]
		public int carId
		{
			get
			{
				return m_carId;
			}
			set
			{
				m_carId = value;
			}
		}
	}
}
