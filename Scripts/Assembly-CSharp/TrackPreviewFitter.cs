using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteInEditMode]
[RequireComponent(typeof(RectTransform))]
[AddComponentMenu("Layout/Extensions/Tile Size Fitter")]
public class TrackPreviewFitter : UIBehaviour, ILayoutSelfController, ILayoutController
{
	[NonSerialized]
	private Vector2 m_referenceSize;

	[NonSerialized]
	private RectTransform m_rectTransform;

	private DrivenRectTransformTracker m_tracker;

	public Vector2 referenceSize
	{
		get
		{
			return m_referenceSize;
		}
		set
		{
			m_referenceSize = value;
			SetDirty();
		}
	}

	private RectTransform rectTransform
	{
		get
		{
			if (m_rectTransform == null)
			{
				m_rectTransform = GetComponent<RectTransform>();
			}
			return m_rectTransform;
		}
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		SetDirty();
	}

	protected override void OnDisable()
	{
		m_tracker.Clear();
		LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
		base.OnDisable();
	}

	protected override void OnRectTransformDimensionsChange()
	{
		UpdateRect();
	}

	protected override void OnCanvasHierarchyChanged()
	{
		UpdateRect();
	}

	protected override void OnTransformParentChanged()
	{
		UpdateRect();
	}

	protected override void OnCanvasGroupChanged()
	{
		UpdateRect();
	}

	private void UpdateRect()
	{
		if (IsActive() && !(rectTransform.parent == null) && rectTransform.childCount != 0)
		{
			m_tracker.Clear();
			Rect rect = rectTransform.parent.GetComponent<RectTransform>().rect;
			if (m_referenceSize.x == 0f || m_referenceSize.y == 0f)
			{
				m_referenceSize = rect.size;
			}
			float num = rect.width / m_referenceSize.x;
			if (m_referenceSize.y * num > rect.height)
			{
				num = rect.height / m_referenceSize.y;
			}
			m_tracker.Add(this, rectTransform, DrivenTransformProperties.SizeDelta);
			rectTransform.sizeDelta = m_referenceSize * num;
			RectTransform component = rectTransform.GetChild(0).GetComponent<RectTransform>();
			m_tracker.Add(this, component, DrivenTransformProperties.Scale);
			component.localScale = new Vector3(num, num, num);
			m_tracker.Add(this, component, DrivenTransformProperties.AnchoredPosition);
			component.anchoredPosition = Vector2.zero;
		}
	}

	public virtual void SetLayoutHorizontal()
	{
	}

	public virtual void SetLayoutVertical()
	{
	}

	protected void SetDirty()
	{
		if (IsActive())
		{
			UpdateRect();
		}
	}
}
