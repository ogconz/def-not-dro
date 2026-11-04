using UnityEngine;

namespace TriLib
{
	public static class TransformExtensions
	{
		public static void LoadMatrix(this Transform transform, Matrix4x4 matrix, bool local = true)
		{
			if (local)
			{
				transform.localScale = matrix.ExtractScale();
				transform.localRotation = matrix.ExtractRotation();
				transform.localPosition = matrix.ExtractPosition();
			}
			else
			{
				transform.rotation = matrix.ExtractRotation();
				transform.position = matrix.ExtractPosition();
			}
		}

		public static Bounds EncapsulateBounds(this Transform transform)
		{
			Bounds result = default(Bounds);
			Renderer[] componentsInChildren = transform.GetComponentsInChildren<Renderer>();
			if (componentsInChildren != null)
			{
				Renderer[] array = componentsInChildren;
				foreach (Renderer renderer in array)
				{
					result.Encapsulate(renderer.bounds);
				}
			}
			return result;
		}

		public static Transform FindDeepChild(this Transform transform, string name, bool endsWith = false)
		{
			if (endsWith ? (transform.name == name) : transform.name.EndsWith(name))
			{
				return transform;
			}
			foreach (Transform item in transform)
			{
				Transform transform2 = item.FindDeepChild(name);
				if (transform2 != null)
				{
					return transform2;
				}
			}
			return null;
		}

		public static void DestroyChildren(this Transform transform, bool destroyImmediate = false)
		{
			for (int num = transform.childCount - 1; num >= 0; num--)
			{
				Transform child = transform.GetChild(num);
				if (destroyImmediate)
				{
					Object.DestroyImmediate(child.gameObject);
				}
				else
				{
					Object.Destroy(child.gameObject);
				}
			}
		}
	}
}
