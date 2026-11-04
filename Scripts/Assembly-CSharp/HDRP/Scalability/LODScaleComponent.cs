using System;
using UnityEngine.Rendering;

namespace HDRP.Scalability
{
	[Serializable]
	[VolumeComponentMenu("Scalability/LOD Scale")]
	public sealed class LODScaleComponent : VolumeComponent
	{
		public NoInterpClampedFloatParameter value = new NoInterpClampedFloatParameter(1f, 0.01f, 1f);
	}
}
