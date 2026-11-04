using System;
using UnityEngine.Rendering;

namespace HDRP.Scalability
{
	[Serializable]
	[VolumeComponentMenu("Scalability/Texture Quality")]
	public sealed class TextureQualityComponent : VolumeComponent
	{
		public enum TextureQuality
		{
			FullRes = 0,
			HalfRes = 1,
			QuarterRes = 2,
			EighthRes = 3
		}

		[Serializable]
		public sealed class TextureQualityParameter : VolumeParameter<TextureQuality>
		{
			public TextureQualityParameter(TextureQuality value, bool overrideState = false)
				: base(value, overrideState)
			{
			}
		}

		public TextureQualityParameter value = new TextureQualityParameter(TextureQuality.FullRes);
	}
}
