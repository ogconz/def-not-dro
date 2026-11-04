using System.Collections.Generic;
using UnityEngine;
using anzu;

namespace Services.Anzu.Obstruction
{
	public class AnzuObstructionItem : MonoBehaviour
	{
		private Obstructor m_obstructor;

		[SerializeField]
		private bool m_updateEveryFrame;

		public Obstructor obstructor => m_obstructor;

		private void OnEnable()
		{
			m_obstructor = new Obstructor
			{
				Owner = base.transform.gameObject,
				ScreenPoints = TryGetShape()
			};
			AnzuObstructionManagerWrapper.instance.AddElement(this);
		}

		private void OnDisable()
		{
			AnzuObstructionManagerWrapper.instance.RemoveElement(this);
		}

		private void Update()
		{
			if (m_updateEveryFrame)
			{
				UpdateObstruction();
			}
		}

		public void UpdateObstruction()
		{
			m_obstructor.ScreenPoints = TryGetShape();
			ObstructionManager.UpdateElement(m_obstructor);
		}

		private Vector2[] TryGetShape()
		{
			Vector2[] result = null;
			if (TryGetComponent<RectTransform>(out var component))
			{
				result = GetRectTransformScreenShape(component);
			}
			else
			{
				Debug.LogWarning("No RectTransform found. Cannot calculate screen shape.");
			}
			return result;
		}

		private Vector2[] GetRectTransformScreenShape(RectTransform rectTransform)
		{
			Vector2[] array = new Vector2[4];
			Vector3[] array2 = new Vector3[4];
			rectTransform.GetWorldCorners(array2);
			for (int i = 0; i < 4; i++)
			{
				array[i] = new Vector2(array2[i].x, array2[i].y);
			}
			return array;
		}

		private Vector2[] GetMeshScreenShape(MeshFilter meshFilter, Camera cam)
		{
			List<Vector2> list = new List<Vector2>();
			Vector3[] vertices = meshFilter.sharedMesh.vertices;
			foreach (Vector3 position in vertices)
			{
				Vector3 position2 = meshFilter.transform.TransformPoint(position);
				Vector3 vector = cam.WorldToScreenPoint(position2);
				if (vector.z > 0f)
				{
					list.Add(new Vector2(vector.x, vector.y));
				}
			}
			return ConvexHull2D(list);
		}

		private Vector2[] GetSpriteScreenShape(SpriteRenderer spriteRenderer, Camera cam)
		{
			Vector3[] array = new Vector3[4];
			Bounds bounds = spriteRenderer.bounds;
			array[0] = new Vector3(bounds.min.x, bounds.min.y, bounds.center.z);
			array[1] = new Vector3(bounds.min.x, bounds.max.y, bounds.center.z);
			array[2] = new Vector3(bounds.max.x, bounds.max.y, bounds.center.z);
			array[3] = new Vector3(bounds.max.x, bounds.min.y, bounds.center.z);
			List<Vector2> list = new List<Vector2>();
			Vector3[] array2 = array;
			foreach (Vector3 position in array2)
			{
				Vector3 vector = cam.WorldToScreenPoint(position);
				if (vector.z > 0f)
				{
					list.Add(new Vector2(vector.x, vector.y));
				}
			}
			return ConvexHull2D(list);
		}

		private static Vector2[] ConvexHull2D(List<Vector2> points)
		{
			if (points != null && points.Count >= 3)
			{
				List<Vector2> list = new List<Vector2>(points);
				list.Sort((Vector2 a, Vector2 b) => (a.x != b.x) ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
				List<Vector2> list2 = new List<Vector2>();
				foreach (Vector2 item in list)
				{
					while (list2.Count >= 2 && Cross(list2[list2.Count - 2], list2[list2.Count - 1], item) <= 0f)
					{
						list2.RemoveAt(list2.Count - 1);
					}
					list2.Add(item);
				}
				List<Vector2> list3 = new List<Vector2>();
				for (int num = list.Count - 1; num >= 0; num--)
				{
					Vector2 vector = list[num];
					while (list3.Count >= 2 && Cross(list3[list3.Count - 2], list3[list3.Count - 1], vector) <= 0f)
					{
						list3.RemoveAt(list3.Count - 1);
					}
					list3.Add(vector);
				}
				if (list2.Count > 0)
				{
					list2.RemoveAt(list2.Count - 1);
				}
				if (list3.Count > 0)
				{
					list3.RemoveAt(list3.Count - 1);
				}
				list2.AddRange(list3);
				return list2.ToArray();
			}
			return (points != null) ? points.ToArray() : new Vector2[0];
		}

		private static float Cross(Vector2 o, Vector2 a, Vector2 b)
		{
			return (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
		}
	}
}
