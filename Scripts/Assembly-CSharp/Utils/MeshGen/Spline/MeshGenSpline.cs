using System.Collections.Generic;
using UnityEngine;

namespace Utils.MeshGen.Spline
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(BezierSpline))]
	[RequireComponent(typeof(Shape2D))]
	public class MeshGenSpline : MonoBehaviour
	{
		public MeshFilter meshFilter;

		private BezierSpline m_spline;

		private Shape2D m_shape;

		private MeshRenderer m_childRenderer;

		public int detalization = 100;

		public float uCoordScale = 1f;

		public bool refreshWithRaytrace;

		private List<Vector3> m_drawingRays = new List<Vector3>(64);

		public bool showDebugRaycast;

		private Mesh m_mesh;

		private RaycastHit[] m_hitsCache = new RaycastHit[32];

		private void Awake()
		{
			m_spline = GetComponent<BezierSpline>();
			m_shape = GetComponent<Shape2D>();
			m_childRenderer = GetComponentInChildren<MeshRenderer>();
		}

		private void OnEnable()
		{
			if (m_mesh == null)
			{
				Extrude();
			}
		}

		private float[] CalcLengthTableInto(int steps)
		{
			float[] array = new float[steps];
			array[0] = 0f;
			float num = 0f;
			Vector3 vector = m_spline[0];
			for (int i = 1; i < steps; i++)
			{
				float t = (float)i / (float)(steps - 1);
				Vector3 point = m_spline.GetPoint(t);
				float magnitude = (vector - point).magnitude;
				num = (array[i] = num + magnitude);
				vector = point;
			}
			return array;
		}

		public Vector3 GetClosestPoint(Vector3 point)
		{
			if ((bool)m_childRenderer)
			{
				return m_childRenderer.bounds.ClosestPoint(point);
			}
			return Vector3.zero;
		}

		public void Extrude()
		{
			OrientedPoint[] orietedPoints = m_spline.GetOrietedPoints(detalization);
			float[] fArr = CalcLengthTableInto(detalization);
			int num = m_shape.data.Length;
			int num2 = orietedPoints.Length - 1;
			int num3 = orietedPoints.Length;
			int num4 = num * num3;
			int[] array = new int[m_shape.lines.Length * num3 * 3];
			Vector3[] array2 = new Vector3[num4];
			Vector3[] array3 = new Vector3[num4];
			Vector2[] array4 = new Vector2[num4];
			m_drawingRays.Clear();
			int num5 = 0;
			for (int i = 0; i < orietedPoints.Length; i++)
			{
				num5 = i * num;
				for (int j = 0; j < num; j++)
				{
					int num6 = num5 + j;
					if (refreshWithRaytrace)
					{
						Vector3 globalPoint = orietedPoints[i].LocalToWorldTransformed(m_shape[j].vertex, base.transform.up);
						Vector3 vector = orietedPoints[i].LocalToWorld(m_shape[j].vertex);
						array2[num6] = new Vector3(vector.x, base.transform.InverseTransformPoint(GetMeshPoint(globalPoint)).y, vector.z);
					}
					else
					{
						array2[num6] = orietedPoints[i].LocalToWorld(m_shape[j].vertex);
					}
					array3[num6] = orietedPoints[i].LocalToWorldDirection(m_shape[j].normal);
					array4[num6] = new Vector2((float)i / (float)num3 * fArr.Sample(i) * uCoordScale, m_shape[j].uCoord);
				}
			}
			int num7 = 0;
			for (int k = 0; k < num2; k++)
			{
				num5 = k * num;
				for (int l = 0; l < m_shape.lines.Length; l += 2)
				{
					int num8 = num5 + m_shape.lines[l] + num;
					int num9 = num5 + m_shape.lines[l];
					int num10 = num5 + m_shape.lines[l + 1];
					int num11 = num5 + m_shape.lines[l + 1] + num;
					array[num7] = num8;
					num7++;
					array[num7] = num9;
					num7++;
					array[num7] = num10;
					num7++;
					array[num7] = num10;
					num7++;
					array[num7] = num11;
					num7++;
					array[num7] = num8;
					num7++;
				}
			}
			if (m_mesh == null)
			{
				m_mesh = new Mesh();
			}
			m_mesh.Clear();
			m_mesh.vertices = array2;
			m_mesh.triangles = array;
			m_mesh.normals = array3;
			m_mesh.uv = array4;
			meshFilter.sharedMesh = m_mesh;
		}

		private Vector3 GetMeshPoint(Vector3 globalPoint)
		{
			m_drawingRays.Add(globalPoint + Vector3.up * 2f);
			int num = Physics.RaycastNonAlloc(globalPoint + Vector3.up * 2f, Vector3.down, m_hitsCache, 10f, -1073741825);
			Vector3 vector = globalPoint;
			if (num > 0)
			{
				vector = m_hitsCache[0].point;
			}
			m_drawingRays.Add(vector);
			return vector + base.transform.up * 0.005f;
		}

		private void OnDrawGizmosSelected()
		{
			if (showDebugRaycast)
			{
				for (int i = 0; i < m_drawingRays.Count; i += 2)
				{
					Gizmos.DrawLine(m_drawingRays[i], m_drawingRays[i + 1] + Vector3.up);
				}
			}
		}
	}
}
