using System;
using UnityEngine.Rendering;

namespace HDRP.Scalability
{
	[Serializable]
	[VolumeComponentMenu("Scalability/NightSettings")]
	public sealed class NightSettingsComponent : VolumeComponent
	{
		[Serializable]
		public sealed class NightSettingsParameter : VolumeParameter<NightSettings>
		{
			public NightSettingsParameter(NightSettings value, bool overrideState = false)
				: base(value, overrideState)
			{
			}
		}

		public NightSettingsParameter value = new NightSettingsParameter(new NightSettings(), overrideState: true);
	}
}
