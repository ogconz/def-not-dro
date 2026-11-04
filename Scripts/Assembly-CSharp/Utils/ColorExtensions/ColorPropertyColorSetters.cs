using Binding;
using UnityEngine;

namespace Utils.ColorExtensions
{
	public static class ColorPropertyColorSetters
	{
		public static void SetAlpha(this ColorProperty property, float alpha)
		{
			Color value = property.value;
			value.a = alpha;
			property.value = value;
		}

		public static void SetColorOnly(this ColorProperty property, Color color)
		{
			color.a = property.value.a;
			property.value = color;
		}

		public static void SetOpaqueColor(this ColorProperty property, Color color)
		{
			color.a = 1f;
			property.value = color;
		}
	}
}
