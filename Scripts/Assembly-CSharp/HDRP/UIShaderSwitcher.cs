using UnityEngine;

namespace HDRP
{
	public static class UIShaderSwitcher
	{
		[RuntimeInitializeOnLoadMethod]
		public static void OnLoad()
		{
			Shader shader = Shader.Find("UI/Default (Color Space Fix)");
			if (shader != null)
			{
				Canvas.GetDefaultCanvasMaterial().shader = shader;
			}
		}
	}
}
