using UnityEngine;

namespace SplineLogic
{
	public interface ISpline
	{
		float length { get; }

		int pointCount { get; }

		bool closed { get; }

		void SetWorldPoints(Vector3[] points);

		void GetWorldPoints(out Vector3[] points);

		Vector3 GetWorldPoint(int index);

		Vector3 GetWorldVelocity(int index);

		Projection ProjectPoint(Vector3 pnt, out Vector3 pos, out Vector3 dir);

		Projection ProjectPoint(Vector3 pnt, ref int segmentIndex, out Vector3 pos, out Vector3 dir, int tillSegmentIndex = -1);

		Projection ProjectPoint(Vector3 pnt, out float along, out float across);

		Projection ProjectPoint(Vector3 pnt, ref int segmentIndex, out float along, out float across, int tillSegmentIndex = -1);

		void SamplePoint(float distance, out int segmentIndex, out Vector3 pnt, out Vector3 vel, out Vector3 acc);

		void SamplePoint(float distance, out Vector3 pnt, out Vector3 vel, out Vector3 acc);

		void SamplePoint(float distance, out Vector3 pnt, out Vector3 vel);

		void SamplePoint(float distance, out Vector3 pnt);

		void SamplePoint(float distance, ref SampleCache sc, out Vector3 pos, out Vector3 vel, out Vector3 acc);

		void SamplePoint(float distance, ref SampleCache sc, out Vector3 pos);
	}
}
