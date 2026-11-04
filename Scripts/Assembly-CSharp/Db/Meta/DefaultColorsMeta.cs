using System;
using System.ComponentModel;
using System.Xml.Serialization;
using DB.XML;
using UnityEngine;

namespace DB.Meta
{
	[Serializable]
	public class DefaultColorsMeta
	{
		[SerializeField]
		private int m_defaultMaterial = 1;

		[SerializeField]
		private int m_defaultIndexConfig;

		[SerializeField]
		private Color m_defaultColor = Color.white;

		[SerializeField]
		private Color m_defaultSpecularColor = Color.white;

		[SerializeField]
		private Color m_defaultDuoColor = Color.white;

		[SerializeField]
		private Color m_defaultCustomSecColor = Color.white;

		[XmlIgnore]
		public int defaultMaterial
		{
			get
			{
				return m_defaultMaterial;
			}
			set
			{
				m_defaultMaterial = value;
			}
		}

		[XmlAttribute("defaultMaterial")]
		[DefaultValue(4)]
		public int xmlDefaultMaterial
		{
			get
			{
				return defaultMaterial;
			}
			set
			{
				defaultMaterial = value;
			}
		}

		[XmlIgnore]
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

		[XmlAttribute("defaultIndexConfig")]
		[DefaultValue(0)]
		public int xmlDefaultIndexConfig
		{
			get
			{
				return defaultIndexConfig;
			}
			set
			{
				defaultIndexConfig = value;
			}
		}

		[XmlIgnore]
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
				defaultColor = XmlTools.ParseColor(value) ?? ((Color32)Color.grey);
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
	}
}
