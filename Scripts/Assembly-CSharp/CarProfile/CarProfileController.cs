using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using UnityEngine;

namespace CarProfile
{
	[Serializable]
	public class CarProfileController
	{
		[SerializeField]
		private string m_name;

		[SerializeField]
		private bool m_enabled = true;

		[SerializeField]
		private bool m_isPercent;

		[SerializeField]
		private List<CarProfileChanger> m_changers;

		[DefaultValue(true)]
		[XmlAttribute]
		public bool enabled
		{
			get
			{
				return m_enabled;
			}
			set
			{
				m_enabled = value;
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

		public int sort
		{
			get
			{
				if (changers == null)
				{
					return 0;
				}
				return changers[0].parameter.sort;
			}
		}

		public bool separated
		{
			get
			{
				if (changers == null)
				{
					return false;
				}
				return changers[0].parameter.separated;
			}
		}

		public CarProfileControllerType type
		{
			get
			{
				if (changers == null)
				{
					return CarProfileControllerType.None;
				}
				return changers[0].parameter.controllerType;
			}
		}

		public List<CarProfileChanger> changers
		{
			get
			{
				return m_changers;
			}
			set
			{
				m_changers = value;
			}
		}

		[XmlAttribute]
		[DefaultValue(false)]
		public bool isPercent
		{
			get
			{
				return m_isPercent;
			}
			set
			{
				m_isPercent = value;
			}
		}

		public bool isGroup
		{
			get
			{
				if (changers != null)
				{
					return changers.Count > 1;
				}
				return false;
			}
		}

		public CarProfileController Copy()
		{
			CarProfileController carProfileController = new CarProfileController
			{
				m_name = m_name,
				m_enabled = m_enabled,
				m_isPercent = m_isPercent
			};
			carProfileController.m_changers = new List<CarProfileChanger>(m_changers?.Count ?? 0);
			if (m_changers != null)
			{
				foreach (CarProfileChanger changer in m_changers)
				{
					carProfileController.m_changers.Add(changer.Copy());
				}
			}
			return carProfileController;
		}
	}
}
