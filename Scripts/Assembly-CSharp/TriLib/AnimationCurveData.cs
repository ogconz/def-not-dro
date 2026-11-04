using UnityEngine;

namespace TriLib
{
	public class AnimationCurveData
	{
		public readonly Keyframe[] Keyframes;

		private uint _index;

		public AnimationCurve AnimationCurve;

		public AnimationCurveData(uint numKeys)
		{
			Keyframes = new Keyframe[numKeys];
		}

		public void AddKey(float time, float value)
		{
			Keyframes[_index++] = new Keyframe(time, value);
		}
	}
}
