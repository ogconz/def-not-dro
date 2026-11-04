using System.Collections.Generic;
using UnityEngine;

namespace Utils.MeshGen.Spline
{
	public class BezierSpline : MonoBehaviour
	{
		public enum AddPointMode
		{
			first = 0,
			end = 1
		}

		public enum BezierControlPointMode
		{
			Free = 0,
			Aligned = 1,
			Mirrored = 2
		}

		[HideInInspector]
		[SerializeField]
		private List<Vector3> m_points = new List<Vector3>(3);

		private Transform m_cachedTransform;

		[HideInInspector]
		[SerializeField]
		private List<BezierControlPointMode> m_controlModes = new List<BezierControlPointMode>();

		[SerializeField]
		private bool m_loop;

		public List<Vector3> points => m_points;

		public Vector3 this[int index]
		{
			get
			{
				return m_points[index];
			}
			set
			{
				if (index % 3 == 0)
				{
					Vector3 vector = value - m_points[index];
					if (loop)
					{
						if (index == 0)
						{
							m_points[1] += vector;
							m_points[m_points.Count - 2] += vector;
							m_points[m_points.Count - 1] = value;
						}
						else if (index == points.Count - 1)
						{
							m_points[0] = value;
							m_points[1] += vector;
							m_points[index - 1] += vector;
						}
						else
						{
							m_points[index - 1] += vector;
							m_points[index + 1] = vector;
						}
					}
					else
					{
						if (index > 0)
						{
							m_points[index - 1] += vector;
						}
						if (index + 1 < m_points.Count)
						{
							m_points[index + 1] += vector;
						}
					}
				}
				m_points[index] = value;
				EnforceMode(index);
			}
		}

		public int Count => m_points.Count;

		public bool loop
		{
			get
			{
				return m_loop;
			}
			set
			{
				m_loop = value;
				if (value)
				{
					m_controlModes[m_controlModes.Count - 1] = m_controlModes[0];
					this[0] = m_points[0];
				}
			}
		}

		public bool debugNormals { get; set; }

		public bool debugDirections { get; set; }

		public void Reset()
		{
			m_points.Clear();
			m_points.AddRange(new Vector3[4]
			{
				Vector3.right,
				Vector3.right * 2f,
				Vector3.right * 3f,
				Vector3.right * 4f
			});
			m_controlModes.AddRange(new BezierControlPointMode[2]);
		}

		private void Awake()
		{
			m_cachedTransform = base.transform;
		}

		public Vector3 GetPoint(float t, bool transformed = false)
		{
			int num;
			if (t >= 1f)
			{
				t = 1f;
				num = points.Count - 4;
			}
			else
			{
				t = Mathf.Clamp01(t) * (float)GetCurveCount();
				num = (int)t;
				t -= (float)num;
				num *= 3;
			}
			Vector3 point = Bezier.GetPoint(m_points[num], m_points[num + 1], m_points[num + 2], m_points[num + 3], t);
			if (!transformed)
			{
				return point;
			}
			return m_cachedTransform.TransformPoint(point);
		}

		public Vector3 GetTangent(float t, bool transformed = false)
		{
			int num;
			if (t >= 1f)
			{
				t = 1f;
				num = m_points.Count - 4;
			}
			else
			{
				t = Mathf.Clamp01(t) * (float)GetCurveCount();
				num = (int)t;
				t -= (float)num;
				num *= 3;
			}
			Vector3 normalized = Bezier.GetFirstDerivative(m_points[num], m_points[num + 1], m_points[num + 2], m_points[num + 3], t).normalized;
			if (!transformed)
			{
				return normalized;
			}
			return (m_cachedTransform.TransformPoint(normalized) - m_cachedTransform.position).normalized;
		}

		public Vector3 GetNormal(float t, Vector3 up, bool transformed = false)
		{
			Vector3 tangent = GetTangent(t, transformed);
			Vector3 normalized = Vector3.Cross(up, tangent).normalized;
			return Vector3.Cross(tangent, normalized).normalized;
		}

		public Vector3 GetNormal2D(float t, bool transformed)
		{
			Vector3 tangent = GetTangent(t, transformed);
			return new Vector3(0f - tangent.y, tangent.x, 0f).normalized;
		}

		public Vector3 GetBiNormal(float t, Vector3 up, bool transformed = false)
		{
			Vector3 tangent = GetTangent(t, transformed);
			return Vector3.Cross(up, tangent).normalized;
		}

		public Vector3 GetDirection(float t, bool transformed)
		{
			return GetTangent(t, transformed);
		}

		public Quaternion GetOrientation2D(float t, bool transformed = false)
		{
			Vector3 tangent = GetTangent(t, transformed);
			Vector3 normal2D = GetNormal2D(t, transformed);
			return Quaternion.LookRotation(tangent, normal2D);
		}

		public Quaternion GetOrientation(float t, Vector3 up, bool transformed = false)
		{
			Vector3 tangent = GetTangent(t, transformed);
			Vector3 normal = GetNormal(t, up, transformed);
			return Quaternion.LookRotation(tangent, normal);
		}

		public int GetCurveCount()
		{
			return (m_points.Count - 1) / 3;
		}

		public BezierControlPointMode GetControlMode(int index)
		{
			return m_controlModes[(index + 1) / 3];
		}

		public void SetControlMode(int index, BezierControlPointMode mode)
		{
			int num = (index + 1) / 3;
			m_controlModes[num] = mode;
			if (loop)
			{
				if (num == 0)
				{
					m_controlModes[m_controlModes.Count - 1] = mode;
				}
				else if (num == m_controlModes.Count - 1)
				{
					m_controlModes[0] = mode;
				}
			}
			EnforceMode(index);
		}

		private void EnforceMode(int index)
		{
			int num = (index + 1) / 3;
			BezierControlPointMode bezierControlPointMode = m_controlModes[num];
			if (bezierControlPointMode == BezierControlPointMode.Free || (!loop && (num == 0 || num == m_controlModes.Count - 1)))
			{
				return;
			}
			int num2 = num * 3;
			int num3;
			int num4;
			if (index <= num2)
			{
				num3 = num2 - 1;
				if (num3 < 0)
				{
					num3 = m_points.Count - 2;
				}
				num4 = num2 + 1;
				if (num4 >= m_points.Count)
				{
					num4 = 1;
				}
			}
			else
			{
				num3 = num2 + 1;
				if (num3 >= m_points.Count)
				{
					num3 = 1;
				}
				num4 = num2 - 1;
				if (num4 < 0)
				{
					num4 = m_points.Count - 2;
				}
			}
			Vector3 vector = points[num2];
			Vector3 vector2 = vector - m_points[num3];
			if (bezierControlPointMode == BezierControlPointMode.Aligned)
			{
				vector2 = vector2.normalized * Vector3.Distance(vector, m_points[num4]);
			}
			m_points[num4] = vector + vector2;
		}

		public void AddCurve(AddPointMode mode)
		{
			if (mode == AddPointMode.first)
			{
				Vector3 vector = m_points[0];
				m_points.InsertRange(0, new Vector3[3]
				{
					vector - Vector3.right * 3f,
					vector - Vector3.right * 2f,
					vector - Vector3.right
				});
			}
			else
			{
				Vector3 vector = m_points[points.Count - 1];
				m_points.AddRange(new Vector3[3]
				{
					vector + Vector3.right,
					vector + Vector3.right * 2f,
					vector + Vector3.right * 3f
				});
			}
			m_controlModes.Add(m_controlModes[m_controlModes.Count - 1]);
			EnforceMode(m_points.Count - 4);
			if (loop)
			{
				m_points[m_points.Count - 1] = m_points[0];
				m_controlModes[m_controlModes.Count - 1] = m_controlModes[0];
				EnforceMode(0);
			}
		}

		public OrientedPoint[] GetOrietedPoints(int steps, bool transformed = false)
		{
			OrientedPoint[] array = new OrientedPoint[(steps + 1) * GetCurveCount()];
			for (int i = 0; i < array.Length; i++)
			{
				float num = (float)i / (float)steps;
				if ((bool)m_cachedTransform)
				{
					array[i] = new OrientedPoint(num, GetPoint(num, transformed), GetOrientation(num, m_cachedTransform.up, transformed), this);
				}
			}
			return array;
		}
	}
}
