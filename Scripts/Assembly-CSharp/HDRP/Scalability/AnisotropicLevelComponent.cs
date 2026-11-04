using System;
using UnityEngine.Rendering;

namespace HDRP.Scalability
{
	[Serializable]
	[VolumeComponentMenu("Scalability/Anisotropic Level")]
	public sealed class AnisotropicLevelComponent : VolumeComponent
	{
		public NoInterpClampedIntParameter value = new NoInterpClampedIntParameter(4, 1, 9);
	}
}
