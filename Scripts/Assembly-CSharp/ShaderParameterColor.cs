using System.Xml.Serialization;
using DB.XML;
using UnityEngine;

public class ShaderParameterColor : ShaderParameter
{
	[XmlIgnore]
	public Color32 value;

	[XmlAttribute("value")]
	public string xmlColor
	{
		get
		{
			return XmlTools.ColorToString(value);
		}
		set
		{
			this.value = XmlTools.ParseColor(value).GetValueOrDefault();
		}
	}
}
