using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class DragPanel : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IDragHandler, IEndDragHandler
{
	private Vector2 m_originalLocalPointerPosition;

	private Vector3 m_originalPanelLocalPosition;

	private RectTransform m_panelRectTransform;

	private RectTransform m_parentRectTransform;

	public UnityEvent onDrag;

	public UnityEvent onDragEnd;

	public bool clampPos;

	private void Awake()
	{
		m_panelRectTransform = base.transform.parent as RectTransform;
		m_parentRectTransform = m_panelRectTransform.parent as RectTransform;
	}

	public void OnPointerDown(PointerEventData data)
	{
		m_originalPanelLocalPosition = m_panelRectTransform.localPosition;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(m_parentRectTransform, data.position, data.pressEventCamera, out m_originalLocalPointerPosition);
	}

	public void OnDrag(PointerEventData data)
	{
		if (!(m_panelRectTransform == null) && !(m_parentRectTransform == null))
		{
			if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_parentRectTransform, data.position, data.pressEventCamera, out var localPoint))
			{
				Vector3 vector = localPoint - m_originalLocalPointerPosition;
				m_panelRectTransform.localPosition = m_originalPanelLocalPosition + vector;
			}
			if (clampPos)
			{
				ClampToWindow();
			}
			OnPointerDown(data);
			onDrag.Invoke();
		}
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		onDragEnd.Invoke();
	}

	private void ClampToWindow()
	{
		Vector3 localPosition = m_panelRectTransform.localPosition;
		Vector3 vector = m_parentRectTransform.rect.min - m_panelRectTransform.rect.min;
		Vector3 vector2 = m_parentRectTransform.rect.max - m_panelRectTransform.rect.max;
		localPosition.x = Mathf.Clamp(m_panelRectTransform.localPosition.x, vector.x, vector2.x);
		localPosition.y = Mathf.Clamp(m_panelRectTransform.localPosition.y, vector.y, vector2.y);
		m_panelRectTransform.localPosition = localPosition;
	}
}
