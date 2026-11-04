using System;
using System.ComponentModel;
using System.Xml.Serialization;
using CodeStage.AntiCheat.ObscuredTypes;
using GameOverlay;
using UnityEngine;

namespace DB.Meta
{
	[Serializable]
	public class Track : IDBMeta, IDBCanCost
	{
		private DLCCode? m_dlcCode;

		[SerializeField]
		private string m_dlc;

		[SerializeField]
		private int m_sortId;

		[SerializeField]
		private string m_name;

		[SerializeField]
		private int m_id;

		[SerializeField]
		private int m_requiredLevel;

		[SerializeField]
		private bool m_enabled;

		[SerializeField]
		private string m_icon;

		[SerializeField]
		private string m_largeIcon;

		[SerializeField]
		private string m_scene;

		[SerializeField]
		private string m_configuration;

		[SerializeField]
		private ObscuredInt m_cost;

		[SerializeField]
		private ObscuredFloat m_coinsFactor;

		[SerializeField]
		private bool m_defaultLocked;

		[SerializeField]
		private int m_totalTimeLimit;

		[SerializeField]
		private ObscuredBool m_hidden;

		[SerializeField]
		private bool m_external;

		[SerializeField]
		private ulong m_externalId;

		[SerializeField]
		private string m_description;

		[SerializeField]
		private byte m_ugcServiceId;

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
		public int requiredLevel
		{
			get
			{
				return m_requiredLevel;
			}
			set
			{
				m_requiredLevel = value;
			}
		}

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

		[XmlAttribute]
		public string icon
		{
			get
			{
				return m_icon;
			}
			set
			{
				m_icon = value;
			}
		}

		[XmlAttribute]
		public string largeIcon
		{
			get
			{
				return m_largeIcon;
			}
			set
			{
				m_largeIcon = value;
			}
		}

		[XmlAttribute]
		public string scene
		{
			get
			{
				return m_scene;
			}
			set
			{
				m_scene = value;
			}
		}

		[XmlAttribute]
		public string configuration
		{
			get
			{
				return m_configuration;
			}
			set
			{
				m_configuration = value;
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
		public float coinsFactor
		{
			get
			{
				return m_coinsFactor.Get();
			}
			set
			{
				m_coinsFactor.Set(value);
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
		public int totalTimeLimit
		{
			get
			{
				return m_totalTimeLimit;
			}
			set
			{
				m_totalTimeLimit = value;
			}
		}

		[XmlAttribute]
		public int sortId
		{
			get
			{
				return m_sortId;
			}
			set
			{
				m_sortId = value;
			}
		}

		[XmlAttribute]
		[DefaultValue("")]
		public string dlc
		{
			get
			{
				return m_dlc;
			}
			set
			{
				m_dlc = value;
			}
		}

		[XmlIgnore]
		public DLCCode dlcCode
		{
			get
			{
				if (m_dlcCode.HasValue)
				{
					return m_dlcCode.Value;
				}
				if (!string.IsNullOrEmpty(dlc))
				{
					try
					{
						m_dlcCode = (DLCCode)Enum.Parse(typeof(DLCCode), dlc, ignoreCase: true);
						return m_dlcCode.Value;
					}
					catch (Exception message)
					{
						Debug.LogError(message);
					}
				}
				m_dlcCode = DLCCode.Unknown;
				return m_dlcCode.Value;
			}
		}

		[XmlAttribute]
		public bool hidden
		{
			get
			{
				return m_hidden;
			}
			set
			{
				m_hidden = value;
			}
		}

		[XmlAttribute]
		public bool external
		{
			get
			{
				return m_external;
			}
			set
			{
				m_external = value;
			}
		}

		[XmlAttribute]
		public ulong externalId
		{
			get
			{
				return m_externalId;
			}
			set
			{
				m_externalId = value;
			}
		}

		[XmlAttribute]
		public string description
		{
			get
			{
				return m_description;
			}
			set
			{
				m_description = value;
			}
		}

		[XmlAttribute]
		public byte ugcServiceId
		{
			get
			{
				return m_ugcServiceId;
			}
			set
			{
				m_ugcServiceId = value;
			}
		}

		public static int Sort(Track lhs, Track rhs)
		{
			int num = lhs.sortId.CompareTo(rhs.sortId);
			if (num != 0)
			{
				return num;
			}
			num = lhs.requiredLevel.CompareTo(rhs.requiredLevel);
			if (num != 0)
			{
				return num;
			}
			return lhs.name.CompareTo(rhs.name);
		}
	}
}
