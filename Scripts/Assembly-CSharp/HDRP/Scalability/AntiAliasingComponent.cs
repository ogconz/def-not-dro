using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace HDRP.Scalability
{
	[Serializable]
	[VolumeComponentMenu("Scalability/Anti-Aliasing")]
	public sealed class AntiAliasingComponent : VolumeComponent
	{
		[Serializable]
		public sealed class AntialiasingModeParameter : VolumeParameter<HDAdditionalCameraData.AntialiasingMode>
		{
			public AntialiasingModeParameter(HDAdditionalCameraData.AntialiasingMode value, bool overrideState = false)
				: base(value, overrideState)
			{
			}
		}

		[Serializable]
		public sealed class SMAAQualityLevelParameter : VolumeParameter<HDAdditionalCameraData.SMAAQualityLevel>
		{
			public SMAAQualityLevelParameter(HDAdditionalCameraData.SMAAQualityLevel value, bool overrideState = false)
				: base(value, overrideState)
			{
			}
		}

		[Serializable]
		public sealed class TAAQualityLevelParameter : VolumeParameter<HDAdditionalCameraData.TAAQualityLevel>
		{
			public TAAQualityLevelParameter(HDAdditionalCameraData.TAAQualityLevel value, bool overrideState = false)
				: base(value, overrideState)
			{
			}
		}

		public AntialiasingModeParameter mode = new AntialiasingModeParameter(HDAdditionalCameraData.AntialiasingMode.None);

		public SMAAQualityLevelParameter smaaQuality = new SMAAQualityLevelParameter(HDAdditionalCameraData.SMAAQualityLevel.High);

		public TAAQualityLevelParameter taaQuality = new TAAQualityLevelParameter(HDAdditionalCameraData.TAAQualityLevel.High);

		public ClampedFloatParameter taaSharpenStrength = new ClampedFloatParameter(0.5f, 0f, 2f);

		public ClampedFloatParameter taaHistorySharpening = new ClampedFloatParameter(0.35f, 0f, 1f);

		public ClampedFloatParameter taaAntiFlicker = new ClampedFloatParameter(0.5f, 0f, 1f);

		public ClampedFloatParameter taaMotionVectorRejection = new ClampedFloatParameter(0.5f, 0f, 1f);

		public BoolParameter taaAntiHistoryRinging = new BoolParameter(value: false);
	}
}
