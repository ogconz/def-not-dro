using UnityEngine;

namespace Utils.ColorExtensions
{
	public static class ColorExtensions
	{
		public static Color ToColor(HSBColor hsbColor)
		{
			float value = hsbColor.b;
			float value2 = hsbColor.b;
			float value3 = hsbColor.b;
			if (hsbColor.s != 0f)
			{
				float b = hsbColor.b;
				float num = hsbColor.b * hsbColor.s;
				float num2 = hsbColor.b - num;
				float num3 = hsbColor.h * 360f;
				if (num3 < 60f)
				{
					value = b;
					value2 = num3 * num / 60f + num2;
					value3 = num2;
				}
				else if (num3 < 120f)
				{
					value = (0f - (num3 - 120f)) * num / 60f + num2;
					value2 = b;
					value3 = num2;
				}
				else if (num3 < 180f)
				{
					value = num2;
					value2 = b;
					value3 = (num3 - 120f) * num / 60f + num2;
				}
				else if (num3 < 240f)
				{
					value = num2;
					value2 = (0f - (num3 - 240f)) * num / 60f + num2;
					value3 = b;
				}
				else if (num3 < 300f)
				{
					value = (num3 - 240f) * num / 60f + num2;
					value2 = num2;
					value3 = b;
				}
				else if (num3 <= 360f)
				{
					value = b;
					value2 = num2;
					value3 = (0f - (num3 - 360f)) * num / 60f + num2;
				}
				else
				{
					value = 0f;
					value2 = 0f;
					value3 = 0f;
				}
			}
			return new Color(Mathf.Clamp01(value), Mathf.Clamp01(value2), Mathf.Clamp01(value3), hsbColor.a);
		}

		public static HSBColor FromColor(Color color)
		{
			HSBColor result = new HSBColor(0f, 0f, 0f, color.a);
			float r = color.r;
			float g = color.g;
			float b = color.b;
			float num = Mathf.Max(r, Mathf.Max(g, b));
			if (num <= 0f)
			{
				return result;
			}
			float num2 = Mathf.Min(r, Mathf.Min(g, b));
			float num3 = num - num2;
			if (num > num2)
			{
				if (g == num)
				{
					result.h = (b - r) / num3 * 60f + 120f;
				}
				else if (b == num)
				{
					result.h = (r - g) / num3 * 60f + 240f;
				}
				else if (b > g)
				{
					result.h = (g - b) / num3 * 60f + 360f;
				}
				else
				{
					result.h = (g - b) / num3 * 60f;
				}
				if (result.h < 0f)
				{
					result.h += 360f;
				}
			}
			else
			{
				result.h = 0f;
			}
			result.h *= 0.0027777778f;
			result.s = num3 / num * 1f;
			result.b = num;
			return result;
		}

		public static Color GetOldSecondaryChameleonColor(Color color)
		{
			Color color2 = new Color(0.5f, 0f, 0.5f, 0f);
			float num = 0.5f;
			float num2 = 0.7f;
			float num3 = 0.4f;
			float num4 = 0.06f;
			float num5 = 0f;
			float num6 = 0f;
			HSBColor hSBColor = FromColor(color);
			Color color3 = default(Color);
			color3.r = Mathf.Clamp01(color.r + (color2.r * 2f - 1f));
			color3.g = Mathf.Clamp01(color.g + (color2.g * 2f - 1f));
			color3.b = Mathf.Clamp01(color.b + (color2.b * 2f - 1f));
			color3.a = 1f;
			HSBColor hSBColor2 = FromColor(color3);
			if (hSBColor2.h == 0f && hSBColor.h != 0f)
			{
				hSBColor2.h = hSBColor.h;
			}
			if (hSBColor2.s == 0f && hSBColor.s != 0f)
			{
				hSBColor2.s = hSBColor.s;
			}
			return ToColor(new HSBColor
			{
				h = Mathf.Repeat(hSBColor2.h + num4, 1f),
				s = hSBColor2.s * num2 + num,
				b = hSBColor2.b * num2 + num3,
				a = (color.r + color.g + color.b) * 0.3333f * num5 + num6
			});
		}

		public static Color ChangeAlpha(this Color color, float alpha)
		{
			color.a = alpha;
			return color;
		}

		public static bool EqualsAllChannels(this Color color, Color other)
		{
			if (color.r == other.r && color.g == other.g && color.b == other.b)
			{
				return color.a == other.a;
			}
			return false;
		}

		public static string ToHexString(this Color color, bool withGrid = true)
		{
			Color32 color2 = color;
			string text = color2.r.ToString("X2") + color2.g.ToString("X2") + color2.b.ToString("X2") + color2.a.ToString("X2");
			if (!withGrid)
			{
				return text;
			}
			return "#" + text;
		}

		public static bool IsColorHexMatch(this Color? colorLeft, Color? colorRight)
		{
			if (!colorLeft.HasValue || !colorRight.HasValue)
			{
				return false;
			}
			Color valueOrDefault = colorLeft.GetValueOrDefault();
			Color valueOrDefault2 = colorRight.GetValueOrDefault();
			string text = valueOrDefault.ToHexString();
			string value = valueOrDefault2.ToHexString();
			return text.Equals(value);
		}

		public static bool IsColorHexMatch(this Color colorLeft, Color colorRight)
		{
			string text = colorLeft.ToHexString();
			string value = colorRight.ToHexString();
			return text.Equals(value);
		}
	}
}
