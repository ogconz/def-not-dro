using System;
using System.Collections.Generic;
using DB.XML;
using SimpleXML;
using UnityEngine;

namespace Customization
{
	[Serializable]
	public class CarPartColorOverride
	{
		public enum Target
		{
			Unknown = 0,
			Chassis = 1,
			Ramp = 2,
			RollCage = 3,
			FrontBumper = 4,
			RearBumper = 5,
			Skirts = 6,
			Doors = 7,
			Roof = 8,
			Mirrors = 9,
			Hood = 10,
			Trunk = 11,
			Spoiler = 12,
			Fenders = 13,
			FrontLights = 14,
			RearLights = 15
		}

		public Target target;

		public int indexConfig;

		public int material;

		public Color color;

		public Color customSecColor;

		public Color specularColor;

		public Color duoColor;

		[NonSerialized]
		public CarVisualManager.CarPaintHolder cachedPaint;

		private CarPartColorOverride(Target target, int material, int indexConfig, Color color, Color customSecColor, Color specularColor, Color duoColor)
		{
			this.target = target;
			this.material = material;
			this.indexConfig = indexConfig;
			this.color = color;
			this.specularColor = specularColor;
			this.duoColor = duoColor;
			this.customSecColor = customSecColor;
		}

		public void Apply()
		{
			cachedPaint = new CarVisualManager.CarPaintHolder(TargetToFlag(target), material, indexConfig, color, customSecColor, specularColor, duoColor);
		}

		public static List<CarPartColorOverride> Parse(XMLDoc node)
		{
			List<CarPartColorOverride> list = new List<CarPartColorOverride>();
			foreach (XMLDoc child in node.childs)
			{
				if (!string.Equals(child.nodeName, "overridecolor", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				foreach (XMLDoc child2 in child.childs)
				{
					if (!Enum.TryParse<Target>(child2.nodeName, ignoreCase: true, out var result))
					{
						Debug.LogWarning("Unable to parse target from '" + child2.nodeName + "'");
						continue;
					}
					bool flag = false;
					int result2 = 1;
					int result3 = 0;
					Color white = Color.white;
					Color white2 = Color.white;
					Color white3 = Color.white;
					Color white4 = Color.white;
					foreach (KeyValuePair<string, string> attribute in child2.attributes)
					{
						switch (attribute.Key)
						{
						case "material":
							if (!int.TryParse(attribute.Value, out result2))
							{
								result2 = 1;
							}
							else
							{
								flag = true;
							}
							break;
						case "color":
							ParseColor(attribute.Value, out white);
							flag = true;
							break;
						case "indexconfig":
							if (!int.TryParse(attribute.Value, out result3))
							{
								result3 = 1;
							}
							else
							{
								flag = true;
							}
							break;
						case "specularcolor":
							ParseColor(attribute.Value, out white2);
							flag = true;
							break;
						case "duocolor":
							ParseColor(attribute.Value, out white3);
							flag = true;
							break;
						case "customseccolor":
							ParseColor(attribute.Value, out white4);
							flag = true;
							break;
						}
					}
					if (flag)
					{
						list.Add(new CarPartColorOverride(result, result2, result3, white, white4, white2, white3));
					}
				}
			}
			return list;
		}

		private static bool ParseColor(string src, out Color color)
		{
			Color32? color2 = XmlTools.ParseColor(src);
			if (!color2.HasValue)
			{
				color = Color.white;
				return false;
			}
			color = color2.Value;
			return true;
		}

		public static CarVisualManager.CustomizationFlags TargetToFlag(Target target)
		{
			return target switch
			{
				Target.Chassis => CarVisualManager.CustomizationFlags.ChassisColor, 
				Target.Ramp => CarVisualManager.CustomizationFlags.RampColor, 
				Target.RollCage => CarVisualManager.CustomizationFlags.RollCageColor, 
				Target.FrontBumper => CarVisualManager.CustomizationFlags.FrontBumperColor, 
				Target.RearBumper => CarVisualManager.CustomizationFlags.RearBumperColor, 
				Target.Skirts => CarVisualManager.CustomizationFlags.SkirtsColor, 
				Target.Doors => CarVisualManager.CustomizationFlags.DoorsColor, 
				Target.Roof => CarVisualManager.CustomizationFlags.RoofColor, 
				Target.Mirrors => CarVisualManager.CustomizationFlags.SideMirrorsColor, 
				Target.Hood => CarVisualManager.CustomizationFlags.HoodColor, 
				Target.Trunk => CarVisualManager.CustomizationFlags.TrunkColor, 
				Target.Spoiler => CarVisualManager.CustomizationFlags.SpoilerColor, 
				Target.Fenders => CarVisualManager.CustomizationFlags.FendersColor, 
				Target.FrontLights => CarVisualManager.CustomizationFlags.FrontLightsColor, 
				Target.RearLights => CarVisualManager.CustomizationFlags.RearLightsColor, 
				_ => CarVisualManager.CustomizationFlags.None, 
			};
		}
	}
}
