using System;
using SyncMultiplayer.DynamicCollisions.Structs;
using Unity.Mathematics;

namespace SyncMultiplayer.DynamicCollisions.Tools
{
	public static class ObbIntersectionTool
	{
		public static bool IsIntersects(in Obb a, in Obb b)
		{
			Span<float3> axes = stackalloc float3[3];
			Span<float3> axes2 = stackalloc float3[3];
			DeconstructAxes(in axes, in a);
			DeconstructAxes(in axes2, in b);
			Span<float3> span = stackalloc float3[15];
			for (int i = 0; i < 3; i++)
			{
				span[i] = axes[i];
				span[i + 3] = axes2[i];
			}
			for (int j = 0; j < 3; j++)
			{
				for (int k = 0; k < 3; k++)
				{
					float3 x = math.cross(axes[j], axes2[k]);
					span[6 + j * 3 + k] = (math.any(x) ? math.normalizesafe(x) : float3.zero);
				}
			}
			DeconstructAxesWithExtents(in axes, in a);
			DeconstructAxesWithExtents(in axes2, in b);
			Span<float3> span2 = span;
			for (int l = 0; l < span2.Length; l++)
			{
				float3 projectionAxis = span2[l];
				if (math.any(projectionAxis))
				{
					GetProjectionInterval(in projectionAxis, in axes, in a, out var min, out var max);
					GetProjectionInterval(in projectionAxis, in axes2, in b, out var min2, out var max2);
					if (max < min2 || max2 < min)
					{
						return false;
					}
				}
			}
			return true;
		}

		private static void DeconstructAxes(in Span<float3> axes, in Obb obb)
		{
			axes[0] = math.mul(obb.rotation, math.right());
			axes[1] = math.mul(obb.rotation, math.up());
			axes[2] = math.mul(obb.rotation, math.forward());
		}

		private static void DeconstructAxesWithExtents(in Span<float3> axes, in Obb obb)
		{
			axes[0] = math.mul(obb.rotation, math.right() * obb.extents.x);
			axes[1] = math.mul(obb.rotation, math.up() * obb.extents.y);
			axes[2] = math.mul(obb.rotation, math.forward() * obb.extents.z);
		}

		private static void GetProjectionInterval(in float3 projectionAxis, in Span<float3> axesWithExtents, in Obb obb, out float min, out float max)
		{
			float num = math.dot(obb.center, projectionAxis);
			float num2 = 0f;
			for (int i = 0; i < axesWithExtents.Length; i++)
			{
				num2 += math.abs(math.dot(axesWithExtents[i], projectionAxis));
			}
			min = num - num2;
			max = num + num2;
		}
	}
}
