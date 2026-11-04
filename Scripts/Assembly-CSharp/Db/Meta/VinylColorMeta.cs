using System;
using System.Xml.Serialization;
using UnityEngine;

namespace DB.Meta
{
	[Serializable]
	public class VinylColorMeta : BaseColorMeta
	{
		[SerializeField]
		private int m_carId;

		[SerializeField]
		private string m_name;

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
	}
}
