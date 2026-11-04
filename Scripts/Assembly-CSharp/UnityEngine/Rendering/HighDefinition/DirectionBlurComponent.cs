using System;

namespace UnityEngine.Rendering.HighDefinition
{
	[Serializable]
	[VolumeComponentMenu("CarX/Direction Blur")]
	public sealed class DirectionBlurComponent : VolumeComponent
	{
		public BoolParameter enabled = new BoolParameter(value: false);

		public BoolParameter invertMask = new BoolParameter(value: false);

		public LayerMaskParameter layerMask = new LayerMaskParameter(0);

		public LayerMaskParameter layerMaskFlash = new LayerMaskParameter(0);

		public Vector3Parameter direction = new Vector3Parameter(Vector3.zero);

		public Vector3Parameter carPos = new Vector3Parameter(Vector3.zero);

		public Vector3Parameter carForward = new Vector3Parameter(Vector3.zero);

		public FloatParameter radius = new FloatParameter(16f);

		public FloatParameter powerFocus = new FloatParameter(1f);

		public FloatParameter powerRadius = new FloatParameter(1f);

		public IntParameter sampleCount = new IntParameter(64);

		public float radiusPowered => radius.value * powerRadius.value;
	}
}
