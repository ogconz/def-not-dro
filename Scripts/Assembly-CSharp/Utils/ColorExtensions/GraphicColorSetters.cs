using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Utils.ColorExtensions
{
	public static class GraphicColorSetters
	{
		public static void SetAlpha(this Graphic graphic, float alpha)
		{
			Color color = graphic.color;
			color.a = alpha;
			graphic.color = color;
		}

		public static void SetColorOnly(this Graphic graphic, Color color)
		{
			color.a = graphic.color.a;
			graphic.color = color;
		}

		public static void SetCharColor(this TMP_Text textPro, int charIndex, Color32 charColor, int charCount = 1)
		{
			TMP_CharacterInfo tMP_CharacterInfo = textPro.textInfo.characterInfo[charIndex];
			if (tMP_CharacterInfo.character != ' ' && tMP_CharacterInfo.isVisible)
			{
				textPro.ForceMeshUpdate();
				int materialReferenceIndex = tMP_CharacterInfo.materialReferenceIndex;
				int num = tMP_CharacterInfo.vertexIndex;
				Color32[] colors = textPro.textInfo.meshInfo[materialReferenceIndex].colors32;
				for (int i = 0; i < charCount; i++)
				{
					colors[num] = charColor;
					colors[num + 1] = charColor;
					colors[num + 2] = charColor;
					colors[num + 3] = charColor;
					num += 4;
				}
				textPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
			}
		}
	}
}
