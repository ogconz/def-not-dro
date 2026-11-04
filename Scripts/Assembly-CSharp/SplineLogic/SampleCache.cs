using UnityEngine;

namespace SplineLogic
{
	public struct SampleCache
	{
		public int instanceID;

		public int segmentIndex;

		public float invSegmentLength;

		public float c0;

		public float c1;

		public float c2;

		public float c3;

		public float c4;

		public float c5;

		public float c6;

		public float c7;

		public float c8;

		public float c9;

		public float c10;

		public float c11;

		public Matrix4x4 mtx1;

		public Matrix4x4 mtx2;

		public Matrix4x4 mtx3;

		public void Reset()
		{
			instanceID = -1;
			segmentIndex = -1;
			mtx1 = (mtx2 = (mtx3 = default(Matrix4x4)));
			invSegmentLength = 0f;
		}
	}
}
