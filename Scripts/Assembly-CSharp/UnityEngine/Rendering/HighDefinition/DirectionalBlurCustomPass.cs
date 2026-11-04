using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
	public class DirectionalBlurCustomPass : CustomPass
	{
		private Material m_compositeMaterial;

		private Material m_customPassUtilsMaterial;

		private RTHandle m_downSampleBuffer;

		private RTHandle m_blurBuffer;

		private RTHandle m_maskBuffer;

		private RTHandle m_maskDepthBuffer;

		private RTHandle m_focusMaskBuffer;

		private RTHandle m_focusMaskDepthBuffer;

		[SerializeField]
		[HideInInspector]
		private Shader m_compositeShader;

		[SerializeField]
		[HideInInspector]
		private Shader m_blurShader;

		private MaterialPropertyBlock m_propertyBlock;

		private DirectionBlurComponent m_directionalBlurComponent;

		private int m_blurPassIndex;

		protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
		{
			if (m_compositeShader == null)
			{
				m_compositeShader = Shader.Find("Hidden/Fullscreen/CompositeBlur");
			}
			if (m_blurShader == null)
			{
				m_blurShader = Shader.Find("Hidden/HDRP/PassUtils");
			}
			m_compositeMaterial = CoreUtils.CreateEngineMaterial(m_compositeShader);
			m_customPassUtilsMaterial = CoreUtils.CreateEngineMaterial(m_blurShader);
			m_blurPassIndex = m_customPassUtilsMaterial.FindPass("DirectionBlur");
			m_propertyBlock = new MaterialPropertyBlock();
			m_downSampleBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, DepthBits.None, GraphicsFormat.B10G11R11_UFloatPack32, FilterMode.Point, TextureWrapMode.Repeat, TextureXR.dimension, enableRandomWrite: false, useMipMap: false, autoGenerateMips: true, isShadowMap: false, 1, 0f, MSAASamples.None, bindTextureMS: false, useDynamicScale: true, RenderTextureMemoryless.None, VRTextureUsage.None, "DownSampleBuffer");
			m_blurBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, DepthBits.None, GraphicsFormat.B10G11R11_UFloatPack32, FilterMode.Point, TextureWrapMode.Repeat, TextureXR.dimension, enableRandomWrite: false, useMipMap: false, autoGenerateMips: true, isShadowMap: false, 1, 0f, MSAASamples.None, bindTextureMS: false, useDynamicScale: true, RenderTextureMemoryless.None, VRTextureUsage.None, "BlurBuffer");
		}

		private void AllocateMaskBuffersIfNeeded()
		{
			if (m_maskBuffer?.rt == null || !m_maskBuffer.rt.IsCreated())
			{
				m_maskBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, DepthBits.None, GraphicsFormat.R8_UNorm, FilterMode.Point, TextureWrapMode.Repeat, TextureXR.dimension, enableRandomWrite: false, useMipMap: false, autoGenerateMips: true, isShadowMap: false, 1, 0f, MSAASamples.None, bindTextureMS: false, useDynamicScale: true, RenderTextureMemoryless.None, VRTextureUsage.None, "Blur Mask");
			}
			if (m_maskDepthBuffer?.rt == null || !m_maskDepthBuffer.rt.IsCreated())
			{
				m_maskDepthBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, DepthBits.Depth16, GraphicsFormat.R16_UInt, FilterMode.Point, TextureWrapMode.Repeat, TextureXR.dimension, enableRandomWrite: false, useMipMap: false, autoGenerateMips: true, isShadowMap: false, 1, 0f, MSAASamples.None, bindTextureMS: false, useDynamicScale: true, RenderTextureMemoryless.None, VRTextureUsage.None, "Blur Depth Mask");
			}
			if (m_focusMaskBuffer?.rt == null || !m_focusMaskBuffer.rt.IsCreated())
			{
				m_focusMaskBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, DepthBits.None, GraphicsFormat.R8_UNorm, FilterMode.Point, TextureWrapMode.Repeat, TextureXR.dimension, enableRandomWrite: false, useMipMap: false, autoGenerateMips: true, isShadowMap: false, 1, 0f, MSAASamples.None, bindTextureMS: false, useDynamicScale: true, RenderTextureMemoryless.None, VRTextureUsage.None, "Blur Mask");
			}
			if (m_focusMaskDepthBuffer?.rt == null || !m_focusMaskDepthBuffer.rt.IsCreated())
			{
				m_focusMaskDepthBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, DepthBits.Depth16, GraphicsFormat.R16_UInt, FilterMode.Point, TextureWrapMode.Repeat, TextureXR.dimension, enableRandomWrite: false, useMipMap: false, autoGenerateMips: true, isShadowMap: false, 1, 0f, MSAASamples.None, bindTextureMS: false, useDynamicScale: true, RenderTextureMemoryless.None, VRTextureUsage.None, "Blur Depth Mask");
			}
		}

		protected override void Execute(CustomPassContext ctx)
		{
			m_directionalBlurComponent = ctx.hdCamera.volumeStack.GetComponent<DirectionBlurComponent>();
			if (m_directionalBlurComponent == null || !m_directionalBlurComponent.enabled.value || m_directionalBlurComponent.radiusPowered < 0.01f)
			{
				base.Execute(ctx);
				return;
			}
			AllocateMaskBuffersIfNeeded();
			if (!(m_compositeMaterial == null) && m_directionalBlurComponent.radiusPowered > 0f)
			{
				RTHandle source = ((targetColorBuffer == TargetBuffer.Camera) ? ctx.cameraColorBuffer : ctx.customColorBuffer.Value);
				RenderStateBlock renderStateBlock = new RenderStateBlock(RenderStateMask.Depth);
				renderStateBlock.depthState = new DepthState(writeEnabled: true, CompareFunction.LessEqual);
				RenderStateBlock overrideRenderState = renderStateBlock;
				Vector4 vector = new Vector4(1f, 1f, 0f, 0f);
				int value = m_directionalBlurComponent.sampleCount.value;
				float radiusPowered = m_directionalBlurComponent.radiusPowered;
				Vector3 value2 = m_directionalBlurComponent.direction.value;
				RTHandle downSampleBuffer = m_downSampleBuffer;
				Vector3 value3 = m_directionalBlurComponent.carPos.value;
				Vector3 value4 = m_directionalBlurComponent.carForward.value;
				float value5 = m_directionalBlurComponent.powerFocus.value;
				CoreUtils.SetRenderTarget(ctx.cmd, m_maskBuffer, m_maskDepthBuffer, ClearFlag.All);
				CustomPassUtils.DrawRenderers(in ctx, m_directionalBlurComponent.layerMask.value, RenderQueueType.All, null, 0, overrideRenderState);
				CoreUtils.SetRenderTarget(ctx.cmd, m_focusMaskBuffer, m_focusMaskDepthBuffer, ClearFlag.All);
				CustomPassUtils.DrawRenderers(in ctx, m_directionalBlurComponent.layerMaskFlash.value, RenderQueueType.All, null, 0, overrideRenderState);
				RenderGaussianBlur(in ctx, source, ctx.cameraNormalBuffer, downSampleBuffer, vector, vector, value2, value3, value4, value5, value, radiusPowered);
				DrawGaussianBlurInScreen(ctx, source);
			}
		}

		protected override void AggregateCullingParameters(ref ScriptableCullingParameters cullingParameters, HDCamera hdCamera)
		{
			cullingParameters.cullingMask |= (uint)((!(m_directionalBlurComponent == null)) ? m_directionalBlurComponent.layerMask.value.value : 0);
		}

		private void SetViewPortSize(MaterialPropertyBlock block, RTHandle target)
		{
			Vector2Int scaledSize = target.GetScaledSize(target.rtHandleProperties.currentViewportSize);
			block.SetVector(GaussianBlurPropertiesID.viewPortSize, new Vector4(scaledSize.x, scaledSize.y, 1f / (float)scaledSize.x, 1f / (float)scaledSize.y));
		}

		private void DrawGaussianBlurInScreen(CustomPassContext ctx, RTHandle source)
		{
			using (new ProfilingScope(ctx.cmd, new ProfilingSampler("Compose Mask Blur")))
			{
				MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
				materialPropertyBlock.SetFloat(GaussianBlurPropertiesID.radius, m_directionalBlurComponent.radiusPowered);
				materialPropertyBlock.SetTexture(GaussianBlurPropertiesID.source, m_downSampleBuffer);
				materialPropertyBlock.SetTexture(GaussianBlurPropertiesID.mask, m_maskBuffer);
				materialPropertyBlock.SetTexture(GaussianBlurPropertiesID.maskDepth, m_maskDepthBuffer);
				materialPropertyBlock.SetFloat(GaussianBlurPropertiesID.invertMask, m_directionalBlurComponent.invertMask.value ? 1 : 0);
				SetViewPortSize(materialPropertyBlock, source);
				HDUtils.DrawFullScreen(ctx.cmd, m_compositeMaterial, source, materialPropertyBlock);
			}
		}

		private void RenderGaussianBlur(in CustomPassContext ctx, RTHandle source, RTHandle sourceNormal, RTHandle destination, Vector4 sourceScaleBias, Vector4 destScaleBias, Vector4 direction, Vector4 focusPos, Vector4 focusOffset, float focusPower, int sampleCount = 8, float radius = 5f, int destMip = 0)
		{
			if (source == destination)
			{
				Debug.LogError("Can't blur the buffer. Source has to be different from the destination.");
				return;
			}
			if (source.rt.antiAliasing > 1 || destination.rt.antiAliasing > 1)
			{
				Debug.LogError("GaussianBlur is not supported with MSAA buffers");
				return;
			}
			using (new ProfilingScope(ctx.cmd, new ProfilingSampler("Render Gaussian Blur")))
			{
				SetRenderTargetWithScaleBias(in ctx, m_propertyBlock, destination, destScaleBias, ClearFlag.None, destMip);
				m_propertyBlock.SetTexture(GaussianBlurPropertiesID.source, source);
				m_propertyBlock.SetTexture(GaussianBlurPropertiesID.normal, sourceNormal);
				m_propertyBlock.SetVector(GaussianBlurPropertiesID.sourceScaleBias, sourceScaleBias);
				m_propertyBlock.SetFloat(GaussianBlurPropertiesID.sampleCount, sampleCount);
				m_propertyBlock.SetFloat(GaussianBlurPropertiesID.radius, radius);
				m_propertyBlock.SetVector(GaussianBlurPropertiesID.focusPoint, focusPos);
				m_propertyBlock.SetVector(GaussianBlurPropertiesID.focusOffset, focusOffset);
				m_propertyBlock.SetFloat(GaussianBlurPropertiesID.focusPower, focusPower);
				m_propertyBlock.SetVector(GaussianBlurPropertiesID.direction, direction);
				m_propertyBlock.SetTexture(GaussianBlurPropertiesID.mask, m_focusMaskBuffer);
				m_propertyBlock.SetTexture(GaussianBlurPropertiesID.maskDepth, m_focusMaskDepthBuffer);
				m_propertyBlock.SetVector(GaussianBlurPropertiesID.dirToCamera, ctx.hdCamera.camera.transform.position - m_directionalBlurComponent.carPos.value);
				SetSourceSize(m_propertyBlock, source);
				ctx.cmd.DrawProcedural(Matrix4x4.identity, m_customPassUtilsMaterial, m_blurPassIndex, MeshTopology.Triangles, 3, 1, m_propertyBlock);
			}
		}

		private static void SetRenderTargetWithScaleBias(in CustomPassContext ctx, MaterialPropertyBlock block, RTHandle destination, Vector4 destScaleBias, ClearFlag clearFlag, int miplevel)
		{
			Rect viewport = new Rect
			{
				size = (destination.useScaling ? destination.GetScaledSize(destination.rtHandleProperties.currentViewportSize) : new Vector2Int(destination.rt.width, destination.rt.height))
			};
			Vector2 size = viewport.size;
			viewport.position = new Vector2(viewport.size.x * destScaleBias.z, viewport.size.y * destScaleBias.w);
			viewport.size *= new Vector2(destScaleBias.x, destScaleBias.y);
			CoreUtils.SetRenderTarget(ctx.cmd, destination, clearFlag, Color.black, miplevel);
			ctx.cmd.SetViewport(viewport);
			block.SetVector(GaussianBlurPropertiesID.viewPortSize, new Vector4(size.x, size.y, 1f / size.x, 1f / size.y));
			block.SetVector(GaussianBlurPropertiesID.viewportScaleBias, new Vector4(1f / destScaleBias.x, 1f / destScaleBias.y, destScaleBias.z, destScaleBias.w));
		}

		private static void SetSourceSize(MaterialPropertyBlock block, RTHandle source)
		{
			Vector2 vector = source.GetScaledSize(source.rtHandleProperties.currentViewportSize);
			block.SetVector(GaussianBlurPropertiesID.sourceSize, new Vector4(vector.x, vector.y, 1f / vector.x, 1f / vector.y));
			block.SetVector(GaussianBlurPropertiesID.sourceScaleFactor, new Vector4(source.scaleFactor.x, source.scaleFactor.y, 1f / source.scaleFactor.x, 1f / source.scaleFactor.y));
		}

		protected override void Cleanup()
		{
			CoreUtils.Destroy(m_compositeMaterial);
			CoreUtils.Destroy(m_customPassUtilsMaterial);
			m_blurBuffer?.Release();
			m_downSampleBuffer?.Release();
			m_maskDepthBuffer?.Release();
			m_maskBuffer?.Release();
			m_focusMaskBuffer?.Release();
			m_focusMaskDepthBuffer?.Release();
		}
	}
}
