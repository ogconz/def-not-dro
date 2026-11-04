using System;
using System.ComponentModel;
using System.Xml.Serialization;
using CarProfile;
using DB.Meta;
using UnityEngine;

namespace Db.Meta.CarProfile
{
	[Serializable]
	public class CarProfileParameterMeta : IDBMeta
	{
		[XmlAttribute]
		public CarProfileParameterType type;

		[XmlAttribute]
		[DefaultValue(false)]
		public bool isBool;

		[XmlAttribute]
		[DefaultValue("none")]
		public string units = "none";

		[XmlAttribute]
		[DefaultValue("N0")]
		public string format = "N0";

		[XmlAttribute]
		public int sort;

		[XmlAttribute]
		[DefaultValue(false)]
		[Obsolete("No more used")]
		public bool separated;

		[XmlAttribute]
		public CarProfileControllerType controllerType;

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

		public CarProfileParameterMeta(CarProfileParameterType type, bool isBool, int id)
		{
			this.type = type;
			this.isBool = isBool;
			this.id = id;
		}

		public CarProfileParameterMeta()
		{
			type = CarProfileParameterType.None;
		}

		public CarProfileParameterMeta Copy()
		{
			return new CarProfileParameterMeta
			{
				m_id = m_id,
				isBool = isBool,
				units = units,
				format = format,
				sort = sort,
				separated = separated,
				controllerType = controllerType
			};
		}
	}
}
