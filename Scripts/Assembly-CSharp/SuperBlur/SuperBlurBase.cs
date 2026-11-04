using UnityEngine;

namespace SuperBlur
{
	[ExecuteInEditMode]
	public class SuperBlurBase : MonoBehaviour
	{
		protected static class Uniforms
		{
			public static readonly int _Radius = Shader.PropertyToID("_Radius");

			public static readonly int _BackgroundTexture = Shader.PropertyToID("_SuperBlurTexture");
		}

		public RenderMode renderMode;

		public BlurKernelSize kernelSize;

		[Range(0f, 1f)]
		public float interpolation = 1f;

		[Range(0f, 4f)]
		public int downsample = 1;

		[Range(1f, 8f)]
		public int iterations = 1;

		public bool gammaCorrection = true;

		public Material blurMaterial;

		public Material UIMaterial;

		protected void Blur(RenderTexture source, RenderTexture destination)
		{
			if (gammaCorrection)
			{
				Shader.EnableKeyword("GAMMA_CORRECTION");
			}
			else
			{
				Shader.DisableKeyword("GAMMA_CORRECTION");
			}
			int num = 0;
			switch (kernelSize)
			{
			case BlurKernelSize.Small:
				num = 0;
				break;
			case BlurKernelSize.Medium:
				num = 2;
				break;
			case BlurKernelSize.Big:
				num = 4;
				break;
			}
			RenderTexture temporary = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
			for (int i = 0; i < iterations; i++)
			{
				float value = (float)i * interpolation + interpolation;
				blurMaterial.SetFloat(Uniforms._Radius, value);
				Graphics.Blit(source, temporary, blurMaterial, 1 + num);
				source.DiscardContents();
				if (i == iterations - 1)
				{
					Graphics.Blit(temporary, destination, blurMaterial, 2 + num);
					continue;
				}
				Graphics.Blit(temporary, source, blurMaterial, 2 + num);
				temporary.DiscardContents();
			}
			RenderTexture.ReleaseTemporary(temporary);
		}
	}
}
