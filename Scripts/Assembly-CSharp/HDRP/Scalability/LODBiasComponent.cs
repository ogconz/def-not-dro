using System;
using UnityEngine.Rendering;

namespace HDRP.Scalability
{
	[Serializable]
	[VolumeComponentMenu("Scalability/LOD Bias")]
	public sealed class LODBiasComponent : VolumeComponent
	{
		public NoInterpClampedFloatParameter value = new NoInterpClampedFloatParameter(3f, 0.01f, 10f);
	}
}
