using UnityEngine;

namespace SuperBlur
{
	[RequireComponent(typeof(Camera))]
	[AddComponentMenu("Effects/Super Blur", -1)]
	public class SuperBlur : SuperBlurBase
	{
		private void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			if (!(blurMaterial == null) && !(UIMaterial == null))
			{
				int width = source.width >> downsample;
				int height = source.height >> downsample;
				RenderTexture temporary = RenderTexture.GetTemporary(width, height, 0, source.format);
				Graphics.Blit(source, temporary);
				if (renderMode == RenderMode.Screen)
				{
					Blur(temporary, destination);
				}
				else if (renderMode == RenderMode.UI)
				{
					Blur(temporary, temporary);
					UIMaterial.SetTexture(Uniforms._BackgroundTexture, temporary);
					Graphics.Blit(source, destination);
				}
				else if (renderMode == RenderMode.OnlyUI)
				{
					Blur(temporary, temporary);
					UIMaterial.SetTexture(Uniforms._BackgroundTexture, temporary);
				}
				RenderTexture.ReleaseTemporary(temporary);
			}
		}
	}
}
