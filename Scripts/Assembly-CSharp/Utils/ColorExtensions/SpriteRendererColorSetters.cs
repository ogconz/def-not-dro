using UnityEngine;

namespace Utils.ColorExtensions
{
	public static class SpriteRendererColorSetters
	{
		public static void SetAlpha(this SpriteRenderer renderer, float alpha)
		{
			Color color = renderer.color;
			color.a = alpha;
			renderer.color = color;
		}

		public static void SetColorOnly(this SpriteRenderer renderer, Color color)
		{
			color.a = renderer.color.a;
			renderer.color = color;
		}
	}
}
