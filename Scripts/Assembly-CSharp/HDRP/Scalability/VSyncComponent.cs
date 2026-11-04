using System;
using UnityEngine.Rendering;

namespace HDRP.Scalability
{
	[Serializable]
	[VolumeComponentMenu("Scalability/VSync")]
	public sealed class VSyncComponent : VolumeComponent
	{
		[Serializable]
		public enum VSyncMode
		{
			DontSync = 0,
			EveryVBlank = 1,
			EverySecondVBlank = 2
		}

		[Serializable]
		public sealed class VSyncModeParameter : VolumeParameter<VSyncMode>
		{
			public VSyncModeParameter(VSyncMode value, bool overrideState = false)
				: base(value, overrideState)
			{
			}
		}

		public VSyncModeParameter value = new VSyncModeParameter(VSyncMode.EveryVBlank, overrideState: true);

		public VSyncComponent()
		{
		}

		public VSyncComponent(int VSyncCount)
		{
			value.value = (VSyncMode)VSyncCount;
		}
	}
}
