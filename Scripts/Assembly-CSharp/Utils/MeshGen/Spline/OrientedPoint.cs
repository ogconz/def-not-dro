using UnityEngine;

namespace Utils.MeshGen.Spline
{
	public struct OrientedPoint
	{
		public float param;

		public Vector3 position;

		public Quaternion rotation;

		public BezierSpline spline;

		public OrientedPoint(float param, Vector3 position, Quaternion rotation, BezierSpline spline)
		{
			this.param = param;
			this.position = position;
			this.rotation = rotation;
			this.spline = spline;
		}

		public Vector3 LocalToWorld(Vector3 point)
		{
			return position + rotation * point;
		}

		public Vector3 LocalToWorld(Transform transform, Vector3 point)
		{
			return transform.InverseTransformPoint(position) + rotation * point;
		}

		public Vector3 LocalToWorldTransformed(Vector3 point, Vector3 up)
		{
			Vector3 vector = Vector3.zero;
			Quaternion quaternion = Quaternion.identity;
			if ((bool)spline)
			{
				vector = spline.GetPoint(param, transformed: true);
				quaternion = spline.GetOrientation(param, up, transformed: true);
			}
			return vector + quaternion * point;
		}

		public Vector3 WorldToLocal(Vector3 point)
		{
			return Quaternion.Inverse(rotation) * (point - position);
		}

		public Vector3 LocalToWorldDirection(Vector3 dir)
		{
			return rotation * dir;
		}
	}
}
