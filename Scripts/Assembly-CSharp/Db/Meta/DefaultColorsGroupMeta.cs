using System;
using UnityEngine;

namespace DB.Meta
{
	[Serializable]
	public class DefaultColorsGroupMeta
	{
		public DefaultColorsMeta defaultFrontRimColors = new DefaultColorsMeta();

		public DefaultColorsMeta defaultRearRimColors = new DefaultColorsMeta();

		public DefaultColorsMeta defaultFrontSpokeColors = new DefaultColorsMeta();

		public DefaultColorsMeta defaultRearSpokeColors = new DefaultColorsMeta();

		public DefaultColorsGroupMeta()
		{
		}

		public DefaultColorsGroupMeta(int defaultMaterialId, Color mainColor, Color specColor, Color duoColor, Color customSecColor)
		{
			defaultFrontRimColors.defaultMaterial = defaultMaterialId;
			defaultFrontRimColors.defaultColor = mainColor;
			defaultFrontRimColors.defaultSpecularColor = specColor;
			defaultFrontRimColors.defaultDuoColor = duoColor;
			defaultFrontRimColors.defaultCustomSecColor = customSecColor;
			defaultRearRimColors.defaultMaterial = defaultMaterialId;
			defaultRearRimColors.defaultColor = mainColor;
			defaultRearRimColors.defaultSpecularColor = specColor;
			defaultRearRimColors.defaultDuoColor = duoColor;
			defaultRearRimColors.defaultCustomSecColor = customSecColor;
			defaultFrontSpokeColors.defaultMaterial = defaultMaterialId;
			defaultFrontSpokeColors.defaultColor = mainColor;
			defaultFrontSpokeColors.defaultSpecularColor = specColor;
			defaultFrontSpokeColors.defaultDuoColor = duoColor;
			defaultFrontSpokeColors.defaultCustomSecColor = customSecColor;
			defaultRearSpokeColors.defaultMaterial = defaultMaterialId;
			defaultRearSpokeColors.defaultColor = mainColor;
			defaultRearSpokeColors.defaultSpecularColor = specColor;
			defaultRearSpokeColors.defaultDuoColor = duoColor;
			defaultRearSpokeColors.defaultCustomSecColor = customSecColor;
		}
	}
}
