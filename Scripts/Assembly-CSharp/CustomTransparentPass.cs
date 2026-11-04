using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.RendererUtils;

[Serializable]
public class CustomTransparentPass : CustomPass
{
	internal const PerObjectData k_RendererConfigurationBakedLighting = PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume | PerObjectData.Lightmaps;

	internal const PerObjectData k_RendererConfigurationBakedLightingWithShadowMask = PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume | PerObjectData.Lightmaps | PerObjectData.OcclusionProbe | PerObjectData.OcclusionProbeProxyVolume | PerObjectData.ShadowMask;

	public SortingCriteria sortingCriteria = SortingCriteria.CommonTransparent;

	public bool overrideDepthState;

	public CompareFunction depthCompareFunction = CompareFunction.LessEqual;

	public bool depthWrite;

	private ShaderTagId[] m_shaderTags;

	protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
	{
		m_shaderTags = new ShaderTagId[4]
		{
			new ShaderTagId("Forward"),
			new ShaderTagId("ForwardOnly"),
			new ShaderTagId("SRPDefaultUnlit"),
			new ShaderTagId("FirstPass")
		};
	}

	protected override void Execute(CustomPassContext ctx)
	{
		RenderStateMask renderStateMask = (overrideDepthState ? RenderStateMask.Depth : RenderStateMask.Nothing);
		renderStateMask = (RenderStateMask)((int)renderStateMask | ((overrideDepthState && !depthWrite) ? 8 : 0));
		RenderStateBlock renderStateBlock = new RenderStateBlock(renderStateMask);
		renderStateBlock.depthState = new DepthState(depthWrite, depthCompareFunction);
		renderStateBlock.stencilState = new StencilState(enabled: false);
		RenderStateBlock value = renderStateBlock;
		PerObjectData rendererConfiguration = (ctx.hdCamera.frameSettings.IsEnabled(FrameSettingsField.Shadowmask) ? (PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume | PerObjectData.Lightmaps | PerObjectData.OcclusionProbe | PerObjectData.OcclusionProbeProxyVolume | PerObjectData.ShadowMask) : (PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume | PerObjectData.Lightmaps));
		RendererListDesc rendererListDesc = new RendererListDesc(m_shaderTags, ctx.cullingResults, ctx.hdCamera.camera);
		rendererListDesc.rendererConfiguration = rendererConfiguration;
		rendererListDesc.renderQueueRange = GetRenderQueueRange(RenderQueueType.Transparent);
		rendererListDesc.sortingCriteria = sortingCriteria;
		rendererListDesc.excludeObjectMotionVectors = false;
		rendererListDesc.overrideMaterialPassIndex = 0;
		rendererListDesc.stateBlock = value;
		RendererListDesc desc = rendererListDesc;
		ScriptableRenderContext renderContext = ctx.renderContext;
		CoreUtils.DrawRendererList(renderContext, ctx.cmd, renderContext.CreateRendererList(desc));
	}
}
