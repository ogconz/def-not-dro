using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RenderCore.UI
{
	[AddComponentMenu("UI/Custom Mask", 13)]
	[ExecuteInEditMode]
	[RequireComponent(typeof(RectTransform))]
	[DisallowMultipleComponent]
	public class CustomMask : RectMask2D
	{
		[Serializable]
		public enum ClipShape
		{
			Rect = 0,
			Circle = 1,
			RectRounded = 2
		}

		[SerializeField]
		private ClipShape m_clipShape = ClipShape.Circle;

		[SerializeField]
		private float m_cornerRadius = 1f;

		[SerializeField]
		private float m_softnessSize = 1f;

		private bool m_needCollectMaskables = true;

		private readonly List<CustomMaskable> m_maskables = new List<CustomMaskable>();

		private static readonly FieldInfo m_clipRectFieldInfo = typeof(RectMask2D).GetField("m_LastClipRectCanvasSpace", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

		private Vector4 m_clipParams0;

		private Vector4 m_clipParams1;

		private void ReregisterAllInChilds()
		{
			CustomMaskable[] componentsInChildren = GetComponentsInChildren<CustomMaskable>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].Reregister();
			}
		}

		private void ReregisterOwn()
		{
			CustomMaskable[] componentsInChildren = GetComponentsInChildren<CustomMaskable>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].Reregister();
			}
		}

		private Rect GetClipRect()
		{
			return (Rect)m_clipRectFieldInfo.GetValue(this);
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			ReregisterAllInChilds();
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			ReregisterOwn();
		}

		public override void PerformClipping()
		{
			base.PerformClipping();
			if (m_needCollectMaskables)
			{
				ReregisterAllInChilds();
				m_needCollectMaskables = false;
			}
			m_clipParams0 = Vector4.zero;
			m_clipParams1 = Vector4.zero;
			if (m_clipShape == ClipShape.Circle)
			{
				Rect clipRect = GetClipRect();
				float xMin = clipRect.xMin;
				float yMin = clipRect.yMin;
				float num = clipRect.width * 0.5f;
				float num2 = clipRect.height * 0.5f;
				float num3 = Mathf.Max(1f, Mathf.Min(num, num2));
				float num4 = Mathf.Max(0f, num3 - m_softnessSize);
				m_clipParams0.x = xMin + num;
				m_clipParams0.y = yMin + num2;
				m_clipParams0.z = num3 * num3;
				m_clipParams0.w = 1f / Mathf.Max(1f, num3 * num3 - num4 * num4);
			}
			else if (m_clipShape == ClipShape.RectRounded)
			{
				Rect clipRect2 = GetClipRect();
				float xMin2 = clipRect2.xMin;
				float yMin2 = clipRect2.yMin;
				float num5 = clipRect2.width * 0.5f;
				float num6 = clipRect2.height * 0.5f;
				float num7 = Mathf.Max(1f, Mathf.Min(m_cornerRadius, Mathf.Min(num5, num6)));
				float num8 = Mathf.Max(0f, num7 - m_softnessSize);
				m_clipParams0.x = xMin2 + num5;
				m_clipParams0.y = yMin2 + num6;
				m_clipParams0.z = num5 - num7;
				m_clipParams0.w = num6 - num7;
				m_clipParams1.x = num7 * num7;
				m_clipParams1.y = 1f / Mathf.Max(1f, num7 * num7 - num8 * num8);
			}
			for (int i = 0; i < m_maskables.Count; i++)
			{
				m_maskables[i].SetClipParams(m_clipParams0, m_clipParams1, m_clipShape);
			}
		}

		protected override void OnTransformParentChanged()
		{
			base.OnTransformParentChanged();
			m_needCollectMaskables = true;
		}

		protected override void OnCanvasHierarchyChanged()
		{
			base.OnCanvasHierarchyChanged();
			m_needCollectMaskables = true;
		}

		public void Register(CustomMaskable maskable)
		{
			if (!m_maskables.Contains(maskable))
			{
				m_maskables.Add(maskable);
				maskable.SetClipParams(m_clipParams0, m_clipParams1, m_clipShape);
			}
		}

		public void Unregister(CustomMaskable maskable)
		{
			if (m_maskables.Contains(maskable))
			{
				m_maskables.Remove(maskable);
				maskable.SetClipParams(Vector4.zero, Vector4.zero, ClipShape.Rect);
			}
		}
	}
}
