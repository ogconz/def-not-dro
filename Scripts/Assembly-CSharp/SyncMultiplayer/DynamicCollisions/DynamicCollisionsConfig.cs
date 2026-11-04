using Unity.Collections;
using UnityEngine;

namespace SyncMultiplayer.DynamicCollisions
{
	public class DynamicCollisionsConfig : ScriptableSingleton<DynamicCollisionsConfig>
	{
		public int maxCars;

		public int maxCollidersPerCar;

		public int velocitySweptSegmentCount;

		[Space]
		public AnimationCurve distanceLimitBySpeedCurve;

		public float boundsPredictionMultiplier;

		public float boundsScale;

		public float maxDeltaVelocity;

		public float unstableMovementDiffThreshold;

		public float collisionDisableTime;

		public float velocityDiffThreshold;

		public float angleOfApproach;

		private NativeCurve m_distanceLimitBySpeedCurve;

		protected override void OnInitialize()
		{
			base.OnInitialize();
			m_distanceLimitBySpeedCurve = new NativeCurve(distanceLimitBySpeedCurve);
		}

		~DynamicCollisionsConfig()
		{
			m_distanceLimitBySpeedCurve.Dispose();
		}

		public float GetConvertedMaxDeltaVelocity()
		{
			return maxDeltaVelocity / 3.6f;
		}

		public NativeCurve GetNativeDistanceLimitBySpeedCurve()
		{
			return m_distanceLimitBySpeedCurve;
		}
	}
}
