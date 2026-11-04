namespace UnityEngine.Rendering.HighDefinition
{
	public static class GaussianBlurPropertiesID
	{
		public static readonly int radius = Shader.PropertyToID("_Radius");

		public static readonly int dirToCamera = Shader.PropertyToID("_DirToCamera");

		public static readonly int direction = Shader.PropertyToID("_Direction");

		public static readonly int focusPoint = Shader.PropertyToID("_FocusPoint");

		public static readonly int focusPower = Shader.PropertyToID("_FocusPower");

		public static readonly int focusOffset = Shader.PropertyToID("_FocusOffset");

		public static readonly int source = Shader.PropertyToID("_Source");

		public static readonly int normal = Shader.PropertyToID("_Normal");

		public static readonly int mask = Shader.PropertyToID("_Mask");

		public static readonly int maskDepth = Shader.PropertyToID("_MaskDepth");

		public static readonly int invertMask = Shader.PropertyToID("_InvertMask");

		public static readonly int viewPortSize = Shader.PropertyToID("_ViewPortSize");

		public static readonly int viewportScaleBias = Shader.PropertyToID("_ViewportScaleBias");

		public static readonly int sourceSize = Shader.PropertyToID("_SourceSize");

		public static readonly int sourceScaleFactor = Shader.PropertyToID("_SourceScaleFactor");

		public static readonly int sourceScaleBias = Shader.PropertyToID("_SourceScaleBias");

		public static readonly int gaussianWeights = Shader.PropertyToID("_GaussianWeights");

		public static readonly int sampleCount = Shader.PropertyToID("_SampleCount");
	}
}
