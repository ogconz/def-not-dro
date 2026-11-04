using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class MousePointerEvents : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public UnityEvent enterEvent;

	public UnityEvent exitEvent;

	public void OnPointerEnter(PointerEventData eventData)
	{
		enterEvent.Invoke();
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		exitEvent.Invoke();
	}
}
