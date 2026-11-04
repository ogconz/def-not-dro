using UnityEngine;

namespace SuperBlur
{
	[RequireComponent(typeof(Camera))]
	[AddComponentMenu("Effects/Super Blur Fast", -1)]
	public class SuperBlurFast : SuperBlurBase
	{
		private Camera m_Camera;

		private RenderTexture rt;

		private void OnEnable()
		{
			m_Camera = GetComponent<Camera>();
		}

		private void OnPreCull()
		{
			if (!(blurMaterial == null) && !(UIMaterial == null))
			{
				int width = Screen.width >> downsample;
				int height = Screen.height >> downsample;
				rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.Default);
				m_Camera.targetTexture = rt;
			}
		}

		private void OnPostRender()
		{
			if (!(blurMaterial == null) && !(UIMaterial == null))
			{
				m_Camera.targetTexture = null;
				if (renderMode == RenderMode.Screen)
				{
					Blur(rt, null);
				}
				else if (renderMode == RenderMode.UI)
				{
					Blur(rt, rt);
					UIMaterial.SetTexture(Uniforms._BackgroundTexture, rt);
					Graphics.Blit(null, rt, blurMaterial, 0);
				}
				else if (renderMode == RenderMode.OnlyUI)
				{
					Blur(rt, rt);
					UIMaterial.SetTexture(Uniforms._BackgroundTexture, rt);
				}
				RenderTexture.ReleaseTemporary(rt);
			}
		}
	}
}
