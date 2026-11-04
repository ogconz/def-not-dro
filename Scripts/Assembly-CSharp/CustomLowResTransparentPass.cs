using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.RendererUtils;

[Serializable]
public class CustomLowResTransparentPass : CustomPass
{
	public LowResTransparentUpsample upsampleType;

	public SortingCriteria sortingCriteria;

	public PerObjectData perObjectData;

	public Shader downsampleDepthPS;

	public Shader upsampleTransparentPS;

	private Material m_downsampleDepthMaterial;

	private Material m_upsampleTransparency;

	private RTHandle m_lowResTransparentBuffer;

	private RTHandle m_cameraHalfResDepthBuffer;

	private ShaderTagId[] m_shaderTags;

	protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
	{
		base.Setup(renderContext, cmd);
		m_downsampleDepthMaterial = CoreUtils.CreateEngineMaterial(downsampleDepthPS);
		m_upsampleTransparency = CoreUtils.CreateEngineMaterial(upsampleTransparentPS);
		m_shaderTags = new ShaderTagId[4]
		{
			new ShaderTagId("Forward"),
			new ShaderTagId("ForwardOnly"),
			new ShaderTagId("SRPDefaultUnlit"),
			new ShaderTagId("FirstPass")
		};
	}

	protected override void Cleanup()
	{
		base.Cleanup();
		CoreUtils.Destroy(m_downsampleDepthMaterial);
		CoreUtils.Destroy(m_upsampleTransparency);
		if (m_lowResTransparentBuffer != null)
		{
			m_lowResTransparentBuffer.Release();
		}
		if (m_cameraHalfResDepthBuffer != null)
		{
			m_cameraHalfResDepthBuffer.Release();
		}
	}

	private RendererListDesc CreateTransparentRendererListDesc(CullingResults cull, Camera camera, ShaderTagId[] passName, PerObjectData rendererConfiguration, RenderQueueRange? renderQueueRange = null, RenderStateBlock? stateBlock = null, Material overrideMaterial = null, bool excludeObjectMotionVectors = false)
	{
		RendererListDesc result = new RendererListDesc(passName, cull, camera);
		result.rendererConfiguration = rendererConfiguration;
		result.renderQueueRange = (renderQueueRange.HasValue ? renderQueueRange.Value : GetRenderQueueRange(RenderQueueType.AllTransparent));
		result.sortingCriteria = sortingCriteria;
		result.stateBlock = stateBlock;
		result.overrideMaterial = overrideMaterial;
		result.excludeObjectMotionVectors = excludeObjectMotionVectors;
		return result;
	}

	protected override void Execute(CustomPassContext ctx)
	{
		if (m_lowResTransparentBuffer == null)
		{
			m_lowResTransparentBuffer = RTHandles.Alloc(Vector2.one * 0.5f, TextureXR.slices, DepthBits.None, GraphicsFormat.R16G16B16A16_SFloat, FilterMode.Point, TextureWrapMode.Repeat, TextureXR.dimension, enableRandomWrite: true, useMipMap: false, autoGenerateMips: true, isShadowMap: false, 1, 0f, MSAASamples.None, bindTextureMS: false, useDynamicScale: true, RenderTextureMemoryless.None, VRTextureUsage.None, "Low res transparent");
		}
		if (m_cameraHalfResDepthBuffer == null)
		{
			m_cameraHalfResDepthBuffer = RTHandles.Alloc(Vector2.one * 0.5f, TextureXR.slices, DepthBits.Depth32, GraphicsFormat.R8G8B8A8_SRGB, FilterMode.Point, TextureWrapMode.Repeat, TextureXR.dimension, enableRandomWrite: false, useMipMap: false, autoGenerateMips: true, isShadowMap: false, 1, 0f, MSAASamples.None, bindTextureMS: false, useDynamicScale: true, RenderTextureMemoryless.None, VRTextureUsage.None, "LowResDepthBuffer");
		}
		CommandBuffer cmd = ctx.cmd;
		RendererListDesc desc = CreateTransparentRendererListDesc(ctx.cullingResults, ctx.hdCamera.camera, m_shaderTags, perObjectData, GetRenderQueueRange(RenderQueueType.LowTransparent));
		DownsampleDepthForLowResTransparency(ctx.hdCamera, cmd);
		CoreUtils.SetRenderTarget(cmd, m_lowResTransparentBuffer, m_cameraHalfResDepthBuffer, ClearFlag.Color, Color.black);
		cmd.SetGlobalInt("_OffScreenRendering", 1);
		cmd.SetGlobalInt("_OffScreenDownsampleFactor", 2);
		ScriptableRenderContext renderContext = ctx.renderContext;
		CoreUtils.DrawRendererList(renderContext, cmd, renderContext.CreateRendererList(desc));
		cmd.SetGlobalInt("_OffScreenRendering", 0);
		cmd.SetGlobalInt("_OffScreenDownsampleFactor", 1);
		UpsampleTransparent(ctx);
	}

	private void DownsampleDepthForLowResTransparency(HDCamera hdCamera, CommandBuffer cmd)
	{
		CoreUtils.SetRenderTarget(cmd, m_cameraHalfResDepthBuffer);
		cmd.SetViewport(new Rect(0f, 0f, (float)hdCamera.actualWidth * 0.5f, (float)hdCamera.actualHeight * 0.5f));
		cmd.DrawProcedural(Matrix4x4.identity, m_downsampleDepthMaterial, 0, MeshTopology.Triangles, 3, 1, null);
	}

	private void UpsampleTransparent(CustomPassContext ctx)
	{
		CoreUtils.SetRenderTarget(buffer: (targetColorBuffer != TargetBuffer.Camera) ? ctx.customColorBuffer.Value : ctx.cameraColorBuffer, cmd: ctx.cmd);
		if (upsampleType == LowResTransparentUpsample.Bilinear)
		{
			m_upsampleTransparency.EnableKeyword("BILINEAR");
		}
		else if (upsampleType == LowResTransparentUpsample.NearestDepth)
		{
			m_upsampleTransparency.EnableKeyword("NEAREST_DEPTH");
		}
		m_upsampleTransparency.SetTexture("_LowResTransparent", m_lowResTransparentBuffer);
		m_upsampleTransparency.SetTexture("_LowResDepthTexture", m_cameraHalfResDepthBuffer);
		ctx.cmd.DrawProcedural(Matrix4x4.identity, m_upsampleTransparency, 0, MeshTopology.Triangles, 3, 1, null);
	}
}
