using System;
using System.Xml.Serialization;
using UnityEngine;

namespace DB.Meta
{
	[Serializable]
	public class GlassColorMeta : BaseColorMeta
	{
		[SerializeField]
		private int m_carId;

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
