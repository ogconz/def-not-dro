using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using CodeStage.AntiCheat.ObscuredTypes;
using DB.XML;
using GameOverlay;
using UnityEngine;

namespace DB.Meta
{
	[Serializable]
	public class PlayerCar : IDBMeta, IDBCanCost
	{
		private const string NONE_ENGINE_PREFAB = "None";

		private DLCCode? m_dlcCode;

		private GameEventCode? m_eventCode;

		[SerializeField]
		private string m_name;

		[SerializeField]
		private int m_id;

		[SerializeField]
		private int m_emptyBonnetId;

		[SerializeField]
		private string m_identifier;

		[SerializeField]
		private string m_dlc;

		[SerializeField]
		private string m_eventCodeStr;

		[SerializeField]
		private Color m_defaultColor;

		[SerializeField]
		private Color m_defaultCustomSecColor;

		[SerializeField]
		private Color m_defaultSpecularColor;

		[SerializeField]
		private Color m_defaultDuoColor;

		[SerializeField]
		private bool m_enabled;

		[SerializeField]
		private string m_icon;

		[SerializeField]
		private string m_prefab;

		[SerializeField]
		private ObscuredInt m_cost;

		[SerializeField]
		private ObscuredFloat m_coinsFactor;

		[SerializeField]
		private bool m_defaultLocked;

		[SerializeField]
		private int m_sort;

		[SerializeField]
		private int m_defaultWheelId;

		[SerializeField]
		private int m_defaultColorId;

		[SerializeField]
		private int m_defaultIndexConfig;

		[SerializeField]
		private int m_defaultSmokeColorId;

		[SerializeField]
		private int m_defaultGlassColorId;

		[SerializeField]
		private bool m_customizationEnabled;

		[SerializeField]
		private int m_bronzeTimeFirstTrackXercent;

		[SerializeField]
		private bool m_forcedDisable;

		[SerializeField]
		private bool m_questReward;

		[SerializeField]
		private string m_midEngine;

		[SerializeField]
		private string m_availableEngines;

		[SerializeField]
		private float m_stockEngineWeight;

		[SerializeField]
		private string m_stockEnginePrefab;

		[SerializeField]
		private EngineInfo.Type m_stockEngineType = EngineInfo.Type.Other;

		[SerializeField]
		private int m_vendorId;

		[SerializeField]
		private int m_vendorSort;

		[SerializeField]
		private DefaultColorsGroupMeta m_defaultWheelsColors = new DefaultColorsGroupMeta(1, new Color(0.64f, 0.64f, 0.64f, 1f), Color.white, Color.white, Color.white);

		[XmlIgnore]
		private List<int> m_cachedEngines;

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

		public int emptyBonnetId
		{
			get
			{
				return m_emptyBonnetId;
			}
			set
			{
				m_emptyBonnetId = value;
			}
		}

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

		public string prefab
		{
			get
			{
				return m_prefab;
			}
			set
			{
				m_prefab = value;
			}
		}

		[XmlAttribute]
		public string identifier
		{
			get
			{
				return m_identifier;
			}
			set
			{
				m_identifier = value;
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

		[XmlAttribute("eventCode")]
		[DefaultValue("")]
		public string eventCodeStr
		{
			get
			{
				return m_eventCodeStr;
			}
			set
			{
				m_eventCodeStr = value;
			}
		}

		public string midEngine
		{
			get
			{
				return m_midEngine;
			}
			set
			{
				m_midEngine = value;
			}
		}

		public string availableEngines
		{
			get
			{
				return m_availableEngines;
			}
			set
			{
				m_availableEngines = value;
			}
		}

		public float stockEngineWeight
		{
			get
			{
				return m_stockEngineWeight;
			}
			set
			{
				m_stockEngineWeight = value;
			}
		}

		public string stockEnginePrefab
		{
			get
			{
				return m_stockEnginePrefab;
			}
			set
			{
				m_stockEnginePrefab = value;
			}
		}

		public EngineInfo.Type stockEngineType
		{
			get
			{
				return m_stockEngineType;
			}
			set
			{
				m_stockEngineType = value;
			}
		}

		public int vendorId
		{
			get
			{
				return m_vendorId;
			}
			set
			{
				m_vendorId = value;
			}
		}

		public int vendorSort
		{
			get
			{
				return m_vendorSort;
			}
			set
			{
				m_vendorSort = value;
			}
		}

		[XmlIgnore]
		public bool enableSwaps => availableSwaps.Count > 0;

		[XmlIgnore]
		public bool isMidEngine => m_midEngine != "None";

		[XmlIgnore]
		public List<int> availableSwaps
		{
			get
			{
				if (m_cachedEngines == null)
				{
					m_cachedEngines = StringToIds(m_availableEngines);
					return m_cachedEngines;
				}
				if (!string.IsNullOrWhiteSpace(m_availableEngines) && m_cachedEngines.Count == 0)
				{
					m_cachedEngines = StringToIds(m_availableEngines);
				}
				return m_cachedEngines;
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
					catch
					{
					}
				}
				m_dlcCode = DLCCode.Unknown;
				return m_dlcCode.Value;
			}
		}

		[XmlIgnore]
		public GameEventCode eventCode
		{
			get
			{
				if (m_eventCode.HasValue)
				{
					return m_eventCode.Value;
				}
				if (!string.IsNullOrEmpty(eventCodeStr))
				{
					try
					{
						m_eventCode = (GameEventCode)Enum.Parse(typeof(GameEventCode), eventCodeStr, ignoreCase: true);
						return m_eventCode.Value;
					}
					catch
					{
					}
				}
				m_eventCode = GameEventCode.Unknown;
				return m_eventCode.Value;
			}
		}

		[XmlIgnore]
		public Color defaultColor
		{
			get
			{
				return m_defaultColor;
			}
			set
			{
				m_defaultColor = value;
			}
		}

		[XmlAttribute("defaultColor")]
		public string xmlDefaultColor
		{
			get
			{
				return XmlTools.ColorToString(defaultColor);
			}
			set
			{
				defaultColor = XmlTools.ParseColor(value) ?? ((Color32)Color.white);
			}
		}

		[XmlIgnore]
		public Color defaultSpecularColor
		{
			get
			{
				return m_defaultSpecularColor;
			}
			set
			{
				m_defaultSpecularColor = value;
			}
		}

		[XmlAttribute("defaultSpecularColor")]
		public string xmlDefaultSpecularColor
		{
			get
			{
				return XmlTools.ColorToString(defaultSpecularColor);
			}
			set
			{
				defaultSpecularColor = XmlTools.ParseColor(value) ?? ((Color32)Color.white);
			}
		}

		[XmlIgnore]
		public Color defaultDuoColor
		{
			get
			{
				return m_defaultDuoColor;
			}
			set
			{
				m_defaultDuoColor = value;
			}
		}

		[XmlAttribute("defaultDuoColor")]
		public string xmlDefaultDuoColor
		{
			get
			{
				return XmlTools.ColorToString(defaultDuoColor);
			}
			set
			{
				defaultDuoColor = XmlTools.ParseColor(value) ?? ((Color32)Color.white);
			}
		}

		public Color defaultCustomSecColor
		{
			get
			{
				return m_defaultCustomSecColor;
			}
			set
			{
				m_defaultCustomSecColor = value;
			}
		}

		[XmlAttribute("defaultCustomSecColor")]
		public string xmlDefaultCustomSecColor
		{
			get
			{
				return XmlTools.ColorToString(defaultCustomSecColor);
			}
			set
			{
				defaultCustomSecColor = XmlTools.ParseColor(value) ?? ((Color32)Color.white);
			}
		}

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

		public int sort
		{
			get
			{
				return m_sort;
			}
			set
			{
				m_sort = value;
			}
		}

		public int defaultWheelId
		{
			get
			{
				return m_defaultWheelId;
			}
			set
			{
				m_defaultWheelId = value;
			}
		}

		public int defaultColorId
		{
			get
			{
				return m_defaultColorId;
			}
			set
			{
				m_defaultColorId = value;
			}
		}

		public int defaultIndexConfig
		{
			get
			{
				return m_defaultIndexConfig;
			}
			set
			{
				m_defaultIndexConfig = value;
			}
		}

		public int defaultSmokeColorId
		{
			get
			{
				return m_defaultSmokeColorId;
			}
			set
			{
				m_defaultSmokeColorId = value;
			}
		}

		public int defaultGlassColorId
		{
			get
			{
				return m_defaultGlassColorId;
			}
			set
			{
				m_defaultGlassColorId = value;
			}
		}

		public DefaultColorsGroupMeta defaultWheelsColors
		{
			get
			{
				return m_defaultWheelsColors;
			}
			set
			{
				m_defaultWheelsColors = value;
			}
		}

		public bool customizationEnabled
		{
			get
			{
				return m_customizationEnabled;
			}
			set
			{
				m_customizationEnabled = value;
			}
		}

		public int bronzeTimeFirstTrackXercent
		{
			get
			{
				return m_bronzeTimeFirstTrackXercent;
			}
			set
			{
				m_bronzeTimeFirstTrackXercent = value;
			}
		}

		public bool forcedDisable
		{
			get
			{
				return m_forcedDisable;
			}
			set
			{
				m_forcedDisable = value;
			}
		}

		public bool questReward
		{
			get
			{
				return m_questReward;
			}
			set
			{
				m_questReward = value;
			}
		}

		public void ModifyBronzeTimeForFirstTrack(ref float time)
		{
			if (bronzeTimeFirstTrackXercent != 0 && bronzeTimeFirstTrackXercent != 10000)
			{
				time = time * (float)bronzeTimeFirstTrackXercent / 10000f;
			}
		}

		public GameGlobalFeature AsGameFeature()
		{
			return Mathf.RoundToInt(coinsFactor) switch
			{
				1 => GameGlobalFeature.Cars1, 
				2 => GameGlobalFeature.Cars2, 
				3 => GameGlobalFeature.Cars3, 
				4 => GameGlobalFeature.Cars4, 
				5 => GameGlobalFeature.Cars5, 
				_ => GameGlobalFeature.Unknown, 
			};
		}

		private List<int> StringToIds(string str)
		{
			List<int> list = new List<int>();
			if (string.IsNullOrWhiteSpace(str))
			{
				return list;
			}
			string[] array = str.Split(' ');
			for (int i = 0; i < array.Length; i++)
			{
				if (int.TryParse(array[i], out var result))
				{
					list.Add(result);
				}
			}
			return list;
		}
	}
}
