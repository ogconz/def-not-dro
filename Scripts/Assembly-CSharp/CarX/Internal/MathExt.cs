using System;
using Unity.Mathematics;
using UnityEngine;

namespace CarX.Internal
{
	internal static class MathExt
	{
		public struct BoundRect
		{
			public Vector3 maxBound;

			public Vector3 minBound;

			public Vector3 maxCamera;

			public Vector3 minCamera;

			public BoundRect(Vector3 maxBound, Vector3 minBound, Vector3 maxCamera, Vector3 minCamera)
			{
				this.maxBound = maxBound;
				this.minBound = minBound;
				this.maxCamera = maxCamera;
				this.minCamera = minCamera;
			}

			public float GetAspect()
			{
				return math.max(maxBound.x, maxBound.y) / math.max(maxCamera.x, maxCamera.y);
			}
		}

		public const float PI = MathF.PI;

		public const float HalfPI = MathF.PI / 2f;

		public const float TwoPI = MathF.PI * 2f;

		public const float Deg2Rad = MathF.PI / 180f;

		public const float Rad2Deg = 180f / MathF.PI;

		public const float Epsilon = float.Epsilon;

		public static readonly Vector3 vForward = Vector3.forward;

		public static readonly Vector3 vUp = Vector3.up;

		public static readonly Vector3 vRight = Vector3.right;

		public static readonly Vector3 vZero = Vector3.zero;

		public static readonly Matrix4x4 mIdentity = Matrix4x4.identity;

		public static readonly Matrix4x4 mZero = Matrix4x4.zero;

		public static float Clamp01(float value)
		{
			if (!(value >= 0f))
			{
				return 0f;
			}
			if (!(value <= 1f))
			{
				return 1f;
			}
			return value;
		}

		public static float Clamp(float value, float min, float max)
		{
			if (!(value >= min))
			{
				return min;
			}
			if (!(value <= max))
			{
				return max;
			}
			return value;
		}

		public static int Clamp(int value, int min, int max)
		{
			if (value < min)
			{
				return min;
			}
			if (value > max)
			{
				return max;
			}
			return value;
		}

		public static float Lerp(float from, float to, float t)
		{
			return from + (to - from) * t;
		}

		internal static void SetMultiply3x3(ref Matrix4x4 res, Matrix4x4 lhs, Matrix4x4 rhs)
		{
			res.m00 = lhs.m00 * rhs.m00 + lhs.m01 * rhs.m10 + lhs.m02 * rhs.m20;
			res.m01 = lhs.m00 * rhs.m01 + lhs.m01 * rhs.m11 + lhs.m02 * rhs.m21;
			res.m02 = lhs.m00 * rhs.m02 + lhs.m01 * rhs.m12 + lhs.m02 * rhs.m22;
			res.m10 = lhs.m10 * rhs.m00 + lhs.m11 * rhs.m10 + lhs.m12 * rhs.m20;
			res.m11 = lhs.m10 * rhs.m01 + lhs.m11 * rhs.m11 + lhs.m12 * rhs.m21;
			res.m12 = lhs.m10 * rhs.m02 + lhs.m11 * rhs.m12 + lhs.m12 * rhs.m22;
			res.m20 = lhs.m20 * rhs.m00 + lhs.m21 * rhs.m10 + lhs.m22 * rhs.m20;
			res.m21 = lhs.m20 * rhs.m01 + lhs.m21 * rhs.m11 + lhs.m22 * rhs.m21;
			res.m22 = lhs.m20 * rhs.m02 + lhs.m21 * rhs.m12 + lhs.m22 * rhs.m22;
		}

		internal static void SetMultiply4x4(ref Matrix4x4 res, Matrix4x4 lhs, Matrix4x4 rhs)
		{
			res.m00 = lhs.m00 * rhs.m00 + lhs.m01 * rhs.m10 + lhs.m02 * rhs.m20 + lhs.m03 * rhs.m30;
			res.m01 = lhs.m00 * rhs.m01 + lhs.m01 * rhs.m11 + lhs.m02 * rhs.m21 + lhs.m03 * rhs.m31;
			res.m02 = lhs.m00 * rhs.m02 + lhs.m01 * rhs.m12 + lhs.m02 * rhs.m22 + lhs.m03 * rhs.m32;
			res.m03 = lhs.m00 * rhs.m03 + lhs.m01 * rhs.m13 + lhs.m02 * rhs.m23 + lhs.m03 * rhs.m33;
			res.m10 = lhs.m10 * rhs.m00 + lhs.m11 * rhs.m10 + lhs.m12 * rhs.m20 + lhs.m13 * rhs.m30;
			res.m11 = lhs.m10 * rhs.m01 + lhs.m11 * rhs.m11 + lhs.m12 * rhs.m21 + lhs.m13 * rhs.m31;
			res.m12 = lhs.m10 * rhs.m02 + lhs.m11 * rhs.m12 + lhs.m12 * rhs.m22 + lhs.m13 * rhs.m32;
			res.m13 = lhs.m10 * rhs.m03 + lhs.m11 * rhs.m13 + lhs.m12 * rhs.m23 + lhs.m13 * rhs.m33;
			res.m20 = lhs.m20 * rhs.m00 + lhs.m21 * rhs.m10 + lhs.m22 * rhs.m20 + lhs.m23 * rhs.m30;
			res.m21 = lhs.m20 * rhs.m01 + lhs.m21 * rhs.m11 + lhs.m22 * rhs.m21 + lhs.m23 * rhs.m31;
			res.m22 = lhs.m20 * rhs.m02 + lhs.m21 * rhs.m12 + lhs.m22 * rhs.m22 + lhs.m23 * rhs.m32;
			res.m23 = lhs.m20 * rhs.m03 + lhs.m21 * rhs.m13 + lhs.m22 * rhs.m23 + lhs.m23 * rhs.m33;
			res.m30 = lhs.m30 * rhs.m00 + lhs.m31 * rhs.m10 + lhs.m32 * rhs.m20 + lhs.m33 * rhs.m30;
			res.m31 = lhs.m30 * rhs.m01 + lhs.m31 * rhs.m11 + lhs.m32 * rhs.m21 + lhs.m33 * rhs.m31;
			res.m32 = lhs.m30 * rhs.m02 + lhs.m31 * rhs.m12 + lhs.m32 * rhs.m22 + lhs.m33 * rhs.m32;
			res.m33 = lhs.m30 * rhs.m03 + lhs.m31 * rhs.m13 + lhs.m32 * rhs.m23 + lhs.m33 * rhs.m33;
		}

		internal static void Add3x3(ref Matrix4x4 a, Matrix4x4 m)
		{
			a.m00 += m.m00;
			a.m10 += m.m10;
			a.m20 += m.m20;
			a.m01 += m.m01;
			a.m11 += m.m11;
			a.m21 += m.m21;
			a.m02 += m.m02;
			a.m12 += m.m12;
			a.m22 += m.m22;
		}

		internal static void Orthonormalize(ref Matrix4x4 m)
		{
			Vector3 v = new Vector3(m.m00, m.m10, m.m20);
			Vector3 v2 = new Vector3(m.m01, m.m11, m.m21);
			Vector3 vector = new Vector3(m.m02, m.m12, m.m22);
			Vector3 origin = new Vector3(m.m03, m.m13, m.m23);
			FastNormalize(ref v);
			v2 -= v * Vector3.Dot(v, v2);
			FastNormalize(ref v2);
			vector = Vector3.Cross(v, v2);
			Transform(ref m, origin, v, v2, vector);
		}

		internal static void SetTranslate(ref Matrix4x4 m, Vector3 v)
		{
			m.m03 = v.x;
			m.m13 = v.y;
			m.m23 = v.z;
		}

		internal static void Translate(ref Matrix4x4 m, Vector3 v)
		{
			Matrix4x4 lhs = mIdentity;
			lhs.m03 = v.x;
			lhs.m13 = v.y;
			lhs.m23 = v.z;
			SetMultiply4x4(ref m, lhs, m);
		}

		internal static void Transform(ref Matrix4x4 m, Vector3 origin, Vector3 ox, Vector3 oy, Vector3 oz)
		{
			m.m00 = ox.x;
			m.m10 = ox.y;
			m.m20 = ox.z;
			m.m30 = 0f;
			m.m01 = oy.x;
			m.m11 = oy.y;
			m.m21 = oy.z;
			m.m31 = 0f;
			m.m02 = oz.x;
			m.m12 = oz.y;
			m.m22 = oz.z;
			m.m32 = 0f;
			m.m03 = origin.x;
			m.m13 = origin.y;
			m.m23 = origin.z;
			m.m33 = 1f;
		}

		internal static void RotateFast(ref Matrix4x4 m, float angle, Vector3 axis)
		{
			float num = (float)Math.Cos(angle);
			float num2 = (float)Math.Sin(angle);
			float num3 = 1f - num;
			float x = axis.x;
			float y = axis.y;
			float z = axis.z;
			float num4 = x * num2;
			float num5 = y * num2;
			float num6 = z * num2;
			float num7 = x * num3;
			float num8 = y * num3;
			float num9 = z * num3;
			float num10 = x * num8;
			float num11 = x * num9;
			float num12 = y * num9;
			SetMultiply4x4(ref m, new Matrix4x4
			{
				m00 = x * num7 + num,
				m10 = num10 + num6,
				m20 = num11 - num5,
				m30 = 0f,
				m01 = num10 - num6,
				m11 = y * num8 + num,
				m21 = num12 + num4,
				m31 = 0f,
				m02 = num11 + num5,
				m12 = num12 - num4,
				m22 = z * num9 + num,
				m32 = 0f,
				m03 = 0f,
				m13 = 0f,
				m23 = 0f,
				m33 = 1f
			}, m);
		}

		internal static void GetInverse(this Matrix4x4 m, ref Matrix4x4 res)
		{
			float m2 = 0f - (m.m00 * m.m03 + m.m10 * m.m13 + m.m20 * m.m23);
			float m3 = 0f - (m.m01 * m.m03 + m.m11 * m.m13 + m.m21 * m.m23);
			float m4 = 0f - (m.m02 * m.m03 + m.m12 * m.m13 + m.m22 * m.m23);
			res.m00 = m.m00;
			res.m01 = m.m10;
			res.m02 = m.m20;
			res.m03 = m2;
			res.m10 = m.m01;
			res.m11 = m.m11;
			res.m12 = m.m21;
			res.m13 = m3;
			res.m20 = m.m02;
			res.m21 = m.m12;
			res.m22 = m.m22;
			res.m23 = m4;
			res.m30 = 0f;
			res.m31 = 0f;
			res.m32 = 0f;
			res.m33 = 1f;
		}

		internal static Matrix4x4 GetCrossProductMatrix(this Vector3 v)
		{
			return new Matrix4x4
			{
				m00 = 0f,
				m10 = 0f - v.z,
				m20 = v.y,
				m30 = 0f,
				m01 = v.z,
				m11 = 0f,
				m21 = 0f - v.x,
				m31 = 0f,
				m02 = 0f - v.y,
				m12 = v.x,
				m22 = 0f,
				m32 = 0f,
				m03 = 0f,
				m13 = 0f,
				m23 = 0f,
				m33 = 0f
			};
		}

		internal static Quaternion GetQuaternion(this Matrix4x4 m)
		{
			Quaternion result = default(Quaternion);
			if (m.m00 + m.m11 + m.m22 > 0f)
			{
				float num = m.m00 + m.m11 + m.m22 + 1f;
				float num2 = (float)Math.Sqrt(num) * 0.5f;
				result.w = num2 * num;
				result.z = (m.m10 - m.m01) * num2;
				result.y = (m.m02 - m.m20) * num2;
				result.x = (m.m21 - m.m12) * num2;
			}
			else if (m.m00 > m.m11 && m.m00 > m.m22)
			{
				float num3 = m.m00 - m.m11 - m.m22 + 1f;
				float num4 = (float)Math.Sqrt(num3) * 0.5f;
				result.x = num4 * num3;
				result.y = (m.m10 + m.m01) * num4;
				result.z = (m.m02 + m.m20) * num4;
				result.w = (m.m21 - m.m12) * num4;
			}
			else if (m.m11 > m.m22)
			{
				float num5 = 0f - m.m00 + m.m11 - m.m22 + 1f;
				float num6 = (float)Math.Sqrt(num5) * 0.5f;
				result.y = num6 * num5;
				result.x = (m.m10 + m.m01) * num6;
				result.w = (m.m02 - m.m20) * num6;
				result.z = (m.m21 + m.m12) * num6;
			}
			else
			{
				float num7 = 0f - m.m00 - m.m11 + m.m22 + 1f;
				float num8 = (float)Math.Sqrt(num7) * 0.5f;
				result.z = num8 * num7;
				result.w = (m.m10 - m.m01) * num8;
				result.x = (m.m02 + m.m20) * num8;
				result.y = (m.m21 + m.m12) * num8;
			}
			return result;
		}

		internal static Vector3 GetUp(this Matrix4x4 m)
		{
			return new Vector3(m.m01, m.m11, m.m21);
		}

		public static BoundRect GetMaxMinCameraRectBound(Camera camera, Vector3 boundPosition, Vector3 cameraPosition, float distanceToCamera, Vector3[] boundCorner)
		{
			Vector3[] array = new Vector3[4];
			camera.CalculateFrustumCorners(new Rect(0f, 0f, 1f, 1f), camera.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, array);
			Vector3 vector = Vector3.positiveInfinity;
			Vector3 vector2 = Vector3.negativeInfinity;
			for (int i = 0; i < boundCorner.Length; i++)
			{
				boundCorner[i] = camera.transform.InverseTransformPoint(math.normalize(boundCorner[i] + boundPosition - cameraPosition) * camera.nearClipPlane + (float3)cameraPosition);
				vector = math.min(boundCorner[i], vector);
				vector2 = math.max(boundCorner[i], vector2);
			}
			Vector2 vector3 = Vector3.one * new Vector2(Screen.height, Screen.height);
			vector2 *= vector3 * distanceToCamera;
			Vector3 minCamera = array[1] * vector3;
			Vector3 maxCamera = array[3] * vector3;
			return new BoundRect(vector2, vector, maxCamera, minCamera);
		}

		internal static Vector3 GetForward(this Matrix4x4 m)
		{
			return new Vector3(m.m02, m.m12, m.m22);
		}

		internal static Vector3 GetRight(this Matrix4x4 m)
		{
			return new Vector3(m.m00, m.m10, m.m20);
		}

		internal static Vector3 GetOrigin(this Matrix4x4 m)
		{
			return new Vector3(m.m03, m.m13, m.m23);
		}

		internal static void FastNormalize(ref Vector3 v)
		{
			float num = (float)Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
			num = 1f / num;
			v.x *= num;
			v.y *= num;
			v.z *= num;
		}

		internal static void FastNormalizeSafe(ref Vector3 v)
		{
			float num = (float)Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
			if (num > 0.0001f)
			{
				num = 1f / num;
				v.x *= num;
				v.y *= num;
				v.z *= num;
			}
		}

		internal static void NormalizeAngles(ref float ang, ref float ang2)
		{
			if (ang > MathF.PI * 20f && ang2 > MathF.PI * 20f)
			{
				ang -= MathF.PI * 20f;
				ang2 -= MathF.PI * 20f;
			}
			if (ang < 0f && ang2 < 0f)
			{
				ang += MathF.PI * 20f;
				ang2 += MathF.PI * 20f;
			}
		}
	}
}
