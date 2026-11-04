using UnityEngine;
using UnityEngine.Rendering;

namespace GpuCulling
{
	public class HizMapGenerator
	{
		private ComputeShader m_copyTextureShader;

		private ComputeShader m_mipGeneratorShader;

		private int m_kiCopy;

		private int m_kiBuildMip;

		public HizMapGenerator(ComputeShader copyTextureShader, ComputeShader mipGeneratorShader)
		{
			m_copyTextureShader = copyTextureShader;
			m_mipGeneratorShader = mipGeneratorShader;
			m_kiCopy = m_copyTextureShader.FindKernel("Copy");
			m_kiBuildMip = m_mipGeneratorShader.FindKernel("BuildMip");
		}

		public RenderTexture CreateMipMapRenderTexture()
		{
			RenderTexture renderTexture = new RenderTexture(1, 1, 0, RenderTextureFormat.RFloat);
			renderTexture.enableRandomWrite = true;
			renderTexture.useMipMap = true;
			renderTexture.autoGenerateMips = false;
			renderTexture.filterMode = FilterMode.Point;
			renderTexture.Create();
			return renderTexture;
		}

		public void Build(CommandBuffer cmd, RenderTexture destination, out int maxMipLevel)
		{
			if (destination == null)
			{
				maxMipLevel = 0;
				return;
			}
			int num = (int)Mathf.Floor(Mathf.Log(Mathf.Max(destination.width, destination.height), 2f));
			maxMipLevel = num - 1;
			for (int i = 1; i < num; i++)
			{
				BuildMip(cmd, destination, i);
			}
		}

		public void CopyDepthTexture(CommandBuffer cmd, RenderTexture source, RenderTexture destination)
		{
			if (!(source == null) && !(destination == null))
			{
				m_copyTextureShader.SetTexture(m_kiCopy, "_Src", source);
				m_copyTextureShader.SetTexture(m_kiCopy, "_Dst", destination, 0);
				cmd.DispatchCompute(m_copyTextureShader, m_kiCopy, Mathf.CeilToInt((float)destination.width / 8f), Mathf.CeilToInt((float)destination.height / 8f), 1);
				cmd.GenerateMips(destination);
			}
		}

		private void BuildMip(CommandBuffer cmd, RenderTexture destination, int mipLevel)
		{
			int num = Mathf.Max(1, destination.width >> mipLevel);
			int num2 = Mathf.Max(1, destination.height >> mipLevel);
			m_mipGeneratorShader.SetTexture(m_kiBuildMip, "_Src", destination, mipLevel - 1);
			m_mipGeneratorShader.SetTexture(m_kiBuildMip, "_Dst", destination, mipLevel);
			cmd.DispatchCompute(m_mipGeneratorShader, m_kiBuildMip, Mathf.CeilToInt((float)num / 8f), Mathf.CeilToInt((float)num2 / 8f), 1);
			cmd.GenerateMips(destination);
		}
	}
}
