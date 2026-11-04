using System;
using UnityEngine;

namespace TriLib
{
	public static class CameraExtensions
	{
		public static void FitToBounds(this Camera camera, Transform transform, float distance)
		{
			Bounds bounds = transform.EncapsulateBounds();
			float num = bounds.extents.magnitude / (2f * Mathf.Tan(0.5f * camera.fieldOfView * (MathF.PI / 180f))) * distance;
			if (!float.IsNaN(num))
			{
				camera.farClipPlane = num * 2f;
				camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z + num);
				camera.transform.LookAt(bounds.center);
			}
		}
	}
}
