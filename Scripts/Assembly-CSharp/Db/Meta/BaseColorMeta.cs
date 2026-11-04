using System.Xml.Serialization;
using UnityEngine;

namespace DB.Meta
{
	public class BaseColorMeta : CarVisualTuningMeta
	{
		public CarVisualManager.MaterialParameters materialParams;

		[XmlIgnore]
		public Color32? color
		{
			get
			{
				return materialParams.color;
			}
			set
			{
				materialParams.color = value;
			}
		}
	}
}
