using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class ResizePanel : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IDragHandler, IEndDragHandler
{
	private RectTransform m_panelRectTransform;

	private Vector2 m_originalLocalPointerPosition;

	private Vector2 m_originalSizeDelta;

	public UnityEvent onResize;

	public UnityEvent onDragEnd;

	private void Awake()
	{
		m_panelRectTransform = base.transform.parent.GetComponent<RectTransform>();
	}

	public void OnPointerDown(PointerEventData data)
	{
		m_originalSizeDelta = m_panelRectTransform.sizeDelta;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(m_panelRectTransform, data.position, data.pressEventCamera, out m_originalLocalPointerPosition);
	}

	public void OnDrag(PointerEventData data)
	{
		if (!(m_panelRectTransform == null))
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle(m_panelRectTransform, data.position, data.pressEventCamera, out var localPoint);
			Vector3 vector = localPoint - m_originalLocalPointerPosition;
			Vector2 vector2 = m_originalSizeDelta + new Vector2(vector.x, 0f - vector.y);
			vector2 = new Vector2(Mathf.Max(vector2.x, 0f), Mathf.Max(vector2.y, 0f));
			m_panelRectTransform.sizeDelta = vector2;
			onResize.Invoke();
		}
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		onDragEnd.Invoke();
	}
}
