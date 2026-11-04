using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TrackedScrollRect : ScrollRect
{
	public enum ScrollType
	{
		Other = 0,
		MouseWheel = 1,
		Drag = 2,
		HandleBar = 3
	}

	private ScrollType m_currentScrollType;

	public event Action<ScrollType, Vector2> onScrollEvent;

	public ScrollType GetCurrentScrollType()
	{
		return m_currentScrollType;
	}

	protected override void OnEnable()
	{
		if ((bool)base.horizontalScrollbar)
		{
			base.horizontalScrollbar.onValueChanged.AddListener(OnHandleBarHandler);
		}
		if ((bool)base.verticalScrollbar)
		{
			base.verticalScrollbar.onValueChanged.AddListener(OnHandleBarHandler);
		}
		base.onValueChanged.AddListener(InvokeOnScrollEvent);
		base.OnEnable();
	}

	protected override void OnDisable()
	{
		if ((bool)base.horizontalScrollbar)
		{
			base.horizontalScrollbar.onValueChanged.RemoveListener(OnHandleBarHandler);
		}
		if ((bool)base.verticalScrollbar)
		{
			base.verticalScrollbar.onValueChanged.RemoveListener(OnHandleBarHandler);
		}
		base.onValueChanged.RemoveListener(InvokeOnScrollEvent);
		base.OnDisable();
	}

	private void OnHandleBarHandler(float value)
	{
		m_currentScrollType = ScrollType.HandleBar;
	}

	public override void OnDrag(PointerEventData eventData)
	{
		Vector2 anchoredPosition = base.content.anchoredPosition;
		base.OnDrag(eventData);
		if (HasPositionChanges(anchoredPosition))
		{
			m_currentScrollType = ScrollType.Drag;
		}
	}

	public override void OnScroll(PointerEventData data)
	{
		Vector2 anchoredPosition = base.content.anchoredPosition;
		base.OnScroll(data);
		if (HasPositionChanges(anchoredPosition))
		{
			m_currentScrollType = ScrollType.MouseWheel;
		}
	}

	protected override void SetNormalizedPosition(float value, int axis)
	{
		base.SetNormalizedPosition(value, axis);
		m_currentScrollType = ScrollType.Other;
	}

	private bool HasPositionChanges(Vector2 oldPosition)
	{
		return Vector2.Distance(oldPosition, base.content.anchoredPosition) > 1E-05f;
	}

	private void InvokeOnScrollEvent(Vector2 position)
	{
		this.onScrollEvent.SafeCall(GetCurrentScrollType(), position);
	}
}
