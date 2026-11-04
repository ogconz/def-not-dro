using System;
using UnityEngine;

namespace TrackLayout.Style
{
	[Serializable]
	public struct RadarSlowZoneStyle
	{
		public GraphicLineStyle styleOutLine;

		public Color topColor;

		public Color backColor;

		public float scale;

		public float step;

		public float angle;

		[NonSerialized]
		public Material currentMaterial;

		private static readonly int COLOR = Shader.PropertyToID("_Color");

		private static readonly int COLORBACK = Shader.PropertyToID("_ColorBack");

		private static readonly int SCALE = Shader.PropertyToID("_Scale");

		private static readonly int STEP = Shader.PropertyToID("_Step");

		private static readonly int ANGLE = Shader.PropertyToID("_Angle");

		private static readonly int RESOLUTION = Shader.PropertyToID("_Res");

		private static readonly int OUTLINESIZE = Shader.PropertyToID("_OutLineSize");

		public static RadarSlowZoneStyle lastSlowZone;

		public void SetMaterialProperties(Material material)
		{
			material.SetColor(COLOR, topColor);
			material.SetColor(COLORBACK, backColor);
			material.SetFloat(SCALE, scale);
			material.SetFloat(STEP, step);
			material.SetFloat(ANGLE, angle);
			material.SetFloat(OUTLINESIZE, 0f);
			material.SetVector(RESOLUTION, new Vector4(Screen.width, Screen.height));
			currentMaterial = material;
		}

		public static Material[] SetMaterialProperties(Material template, RadarSlowZoneStyle[] radarClipZoneStyles)
		{
			Material[] array = new Material[radarClipZoneStyles.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = new Material(template);
				radarClipZoneStyles[i].SetMaterialProperties(array[i]);
			}
			return array;
		}

		public void SetLast()
		{
			lastSlowZone = this;
		}
	}
}
