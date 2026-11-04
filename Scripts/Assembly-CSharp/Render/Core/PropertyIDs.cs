using UnityEngine;

namespace Render.Core
{
	public static class PropertyIDs
	{
		public static readonly int color = Shader.PropertyToID("u_vColor");

		public static readonly int secondaryColor = Shader.PropertyToID("u_vSecondaryColor");

		public static readonly int texture = Shader.PropertyToID("u_sTexture");

		public static readonly int texture1 = Shader.PropertyToID("u_sTexture1");

		public static readonly int texture2 = Shader.PropertyToID("u_sTexture2");

		public static readonly int texture3 = Shader.PropertyToID("u_sTexture3");

		public static readonly int parameters = Shader.PropertyToID("u_fParams");

		public static readonly int normalMap = Shader.PropertyToID("u_sNormalMap");

		public static readonly int normalMap1 = Shader.PropertyToID("u_sNormalMap1");

		public static readonly int normalMap2 = Shader.PropertyToID("u_sNormalMap2");

		public static readonly int packedMap = Shader.PropertyToID("u_sPackedMap");

		public static readonly int tiling = Shader.PropertyToID("u_fTiling");

		public static readonly int face = Shader.PropertyToID("u_iFace");

		public static readonly int mip = Shader.PropertyToID("u_fMip");

		public static readonly int samples = Shader.PropertyToID("u_vSamples");

		public static readonly int sampleCount = Shader.PropertyToID("u_iSampleCount");

		public static readonly int invTotalWeight = Shader.PropertyToID("u_fInvTotalWeight");

		public static readonly int reprojtionMatrix = Shader.PropertyToID("u_mReprojectionMatrix");

		public static readonly int colorBufferAttachment = Shader.PropertyToID("g_sColorBufferAttachment");

		public static readonly int depthBufferAttachment = Shader.PropertyToID("g_sDepthBufferAttachment");

		public static readonly int depthBuffer = Shader.PropertyToID("g_sDepthBuffer");

		public static readonly int blurColorBuffer = Shader.PropertyToID("g_sBlurColorBuffer");

		public static readonly int offscreenColorBuffer = Shader.PropertyToID("g_sOffscreenColorBuffer");

		public static readonly int offscreenDepthBuffer = Shader.PropertyToID("g_sOffscreenDepthBuffer");

		public static readonly int ggxLUT = Shader.PropertyToID("g_sGgxLUT");

		public static readonly int colorGradingLUT = Shader.PropertyToID("g_sColorGradingLUT");

		public static readonly int postExposure = Shader.PropertyToID("g_fPostExposure");

		public static readonly int sunlightDir = Shader.PropertyToID("g_vSunlightDir");

		public static readonly int sunlightColor = Shader.PropertyToID("g_vSunlightColor");

		public static readonly int viewMatrix = Shader.PropertyToID("g_mView");

		public static readonly int projMatrix = Shader.PropertyToID("g_mProj");

		public static readonly int viewProjMatrix = Shader.PropertyToID("g_mViewProj");

		public static readonly int prevViewProjMatrix = Shader.PropertyToID("g_mPrevViewProj");

		public static readonly int invViewMatrix = Shader.PropertyToID("g_mInvView");

		public static readonly int invProjMatrix = Shader.PropertyToID("g_mInvProj");

		public static readonly int invViewProjMatrix = Shader.PropertyToID("g_mInvViewProj");

		public static readonly int invPrevViewProjMatrix = Shader.PropertyToID("g_mInvPrevViewProj");

		public static readonly int cameraWorldPosition = Shader.PropertyToID("g_vCameraPosition");

		public static readonly int cameraVelocity = Shader.PropertyToID("g_vCameraVelocity");

		public static readonly int projParams = Shader.PropertyToID("g_fProjParams");

		public static readonly int projJitterOffset = Shader.PropertyToID("g_vProjJitterOffset");

		public static readonly int screenParams = Shader.PropertyToID("g_fScreenParams");

		public static readonly int zBufferParams = Shader.PropertyToID("g_fZBufferParams");

		public static readonly int time = Shader.PropertyToID("g_fTime");

		public static readonly int normalizedTime = Shader.PropertyToID("g_fNormalizedTime");

		public static readonly int fogParamsArray = Shader.PropertyToID("g_fFogParamsArray");

		public static readonly int envMap = Shader.PropertyToID("g_sEnvMap");

		public static readonly int irradianceSHAr = Shader.PropertyToID("g_vIrradianceSHAr");

		public static readonly int irradianceSHAg = Shader.PropertyToID("g_vIrradianceSHAg");

		public static readonly int irradianceSHAb = Shader.PropertyToID("g_vIrradianceSHAb");

		public static readonly int irradianceSHBr = Shader.PropertyToID("g_vIrradianceSHBr");

		public static readonly int irradianceSHBg = Shader.PropertyToID("g_vIrradianceSHBg");

		public static readonly int irradianceSHBb = Shader.PropertyToID("g_vIrradianceSHBb");

		public static readonly int irradianceSHC = Shader.PropertyToID("g_vIrradianceSHC");

		public static readonly int shadowMap = Shader.PropertyToID("g_sShadowMap");

		public static readonly int shadowDither2x2 = Shader.PropertyToID("g_sShadowDither2x2");

		public static readonly int shadowLightDir = Shader.PropertyToID("g_vShadowLightDir");

		public static readonly int shadowBiasParams = Shader.PropertyToID("g_fShadowBiasParams");

		public static readonly int shadowSplitSphere = Shader.PropertyToID("g_vShadowSplitSphere");

		public static readonly int shadowSplitSpheres = Shader.PropertyToID("g_vShadowSplitSpheres");

		public static readonly int shadowPCFKernel = Shader.PropertyToID("g_fShadowPCFKernel");

		public static readonly int shadowSplitMatrices = Shader.PropertyToID("g_mShadow");

		public static readonly int shadowData = Shader.PropertyToID("g_fShadowData");

		public static readonly int globalLightmap = Shader.PropertyToID("g_sGlobalLightmap");

		public static readonly int globalLightmapInvSize = Shader.PropertyToID("g_vGlobalLightmapInvSize");

		public static readonly int globalLightmapOffsetByInvSize = Shader.PropertyToID("g_vGlobalLightmapOffsetByInvSize");

		public static readonly int temporaryRT = Shader.PropertyToID("g_sTemporary");
	}
}
