using TrackLayout.Data;
using TrackLayout.Style;
using UnityEngine;

namespace TrackLayout.Builders
{
	public static class GeometryBuilder
	{
		public static void BuildTriangleStrip(int pointCount, out int[] triangles)
		{
			triangles = new int[pointCount * 6];
			for (int i = 1; i < pointCount; i++)
			{
				int num = (i - 1) * 6;
				int num2 = i * 2;
				triangles[num] = num2 - 2;
				triangles[num + 1] = num2 - 1;
				triangles[num + 2] = num2;
				triangles[num + 3] = num2 + 1;
				triangles[num + 4] = num2;
				triangles[num + 5] = num2 - 1;
			}
		}

		public static void BuildLine(Vector3[] points, GraphicLineStyle style, Vector3 faceNormal, ref MeshData mesh)
		{
			int num = points.Length;
			if (style.closed)
			{
				num++;
			}
			float num2 = 0f;
			float num3 = 0f;
			Vector3 vector = Vector3.zero;
			Vector3[] array = new Vector3[num * 2];
			Vector2[] array2 = ((style.texture == null) ? null : new Vector2[num * 2]);
			float num4 = 1f / Mathf.Max(float.Epsilon, style.width);
			float num5 = ((style.texture == null) ? 1f : ((float)style.texture.width / (float)style.texture.height));
			for (int i = 0; i < num; i++)
			{
				Vector3 vector2;
				Vector3 vector3;
				if (style.closed)
				{
					if (i == 0)
					{
						vector2 = points[i];
						vector3 = points[i + 1] - vector2;
						vector = Vector3.Cross((vector2 - points[num - 2]).normalized, faceNormal);
					}
					else if (i == num - 1)
					{
						vector2 = points[0];
						vector3 = vector2 - points[num - 2];
						vector = Vector3.Cross((points[1] - vector2).normalized, faceNormal);
					}
					else if (i == num - 2)
					{
						vector2 = points[i];
						vector3 = points[0] - vector2;
					}
					else
					{
						vector2 = points[i];
						vector3 = points[i + 1] - vector2;
					}
				}
				else
				{
					vector2 = points[i];
					vector3 = ((i == num - 1) ? (vector2 - points[i - 1]) : (points[i + 1] - vector2));
				}
				float magnitude = vector3.magnitude;
				if (Mathf.Approximately(magnitude, 0f))
				{
					continue;
				}
				Vector3 lhs = vector3 / magnitude;
				Vector3 vector4 = Vector3.Cross(lhs, faceNormal);
				Vector3 normalized = (vector + vector4).normalized;
				float num6 = Vector3.Dot(lhs, normalized);
				float num7 = Mathf.Sqrt(1f - num6 * num6);
				if (!Mathf.Approximately(num7, 0f))
				{
					normalized /= num7;
					array[i * 2] = vector2 + normalized * (style.width * -0.5f + style.bias);
					array[i * 2 + 1] = vector2 + normalized * (style.width * 0.5f + style.bias);
					num2 += num3;
					num3 = magnitude;
					vector = vector4;
					if (array2 != null)
					{
						float y = num2 * num4 * num5;
						array2[i * 2] = new Vector2(0f, y);
						array2[i * 2 + 1] = new Vector2(1f, y);
					}
				}
			}
			BuildTriangleStrip(num, out mesh.triangles);
			mesh.vertices = array;
			mesh.uv = array2;
		}

		public static void BuildMeshLine(Vector3[] points, MeshLineStyle style, Vector3 faceNormal, ref MeshData mesh)
		{
			int num = points.Length;
			if (style.closed)
			{
				num++;
			}
			float num2 = 0f;
			float num3 = 0f;
			Vector3 vector = Vector3.zero;
			Vector3 vector2 = Vector3.zero;
			Vector3[] array = new Vector3[num * 2];
			Vector3[] array2 = new Vector3[num * 2];
			Vector2[] array3 = ((style.material == null) ? null : new Vector2[num * 2]);
			float num4 = 1f / Mathf.Max(float.Epsilon, style.width);
			float uvScale = style.uvScale;
			float uvBias = style.uvBias;
			float num5 = uvScale;
			for (int i = 0; i < num; i++)
			{
				Vector3 vector3;
				Vector3 vector4;
				if (style.closed)
				{
					if (i == 0)
					{
						vector3 = points[i];
						vector4 = points[i + 1] - vector3;
						vector = Vector3.Cross((vector3 - points[num - 2]).normalized, faceNormal);
					}
					else if (i == num - 1)
					{
						vector3 = points[0];
						vector4 = vector3 - points[num - 2];
						vector = Vector3.Cross((points[1] - vector3).normalized, faceNormal);
					}
					else if (i == num - 2)
					{
						vector3 = points[i];
						vector4 = points[0] - vector3;
					}
					else
					{
						vector3 = points[i];
						vector4 = points[i + 1] - vector3;
					}
				}
				else
				{
					vector3 = points[i];
					vector4 = ((i == num - 1) ? (vector3 - points[i - 1]) : (points[i + 1] - vector3));
				}
				float magnitude = vector4.magnitude;
				Vector3 lhs = vector4 / magnitude;
				Vector3 vector5 = Vector3.Cross(lhs, faceNormal);
				Vector3 vector6 = (vector + vector5).normalized;
				float num6 = Vector3.Dot(lhs, vector6);
				float num7 = Mathf.Sqrt(1f - num6 * num6);
				if (num7 < 0.1f)
				{
					vector6 = vector2;
				}
				else
				{
					vector6 /= num7;
					vector2 = vector6;
				}
				Vector3 vector7 = faceNormal * (style.slopeSize * 0.5f);
				array[i * 2] = vector3 + vector6 * (style.width * -0.5f + style.bias) + vector7 + faceNormal * style.normalBias;
				array[i * 2 + 1] = vector3 + vector6 * (style.width * 0.5f + style.bias) - vector7 + faceNormal * style.normalBias;
				array2[i * 2] = faceNormal;
				array2[i * 2 + 1] = faceNormal;
				num2 += num3;
				num3 = magnitude;
				vector = vector5;
				if (array3 != null)
				{
					float y = num2 * num4 * num5;
					array3[i * 2] = new Vector2(uvBias, y);
					array3[i * 2 + 1] = new Vector2(uvBias + uvScale, y);
				}
			}
			BuildTriangleStrip(num, out mesh.triangles);
			mesh.vertices = array;
			mesh.uv = array3;
			mesh.normals = array2;
		}
	}
}
