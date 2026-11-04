using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class UpDownPointerHandler : MonoBehaviour, IPointerUpHandler, IEventSystemHandler, IPointerDownHandler
{
	public UnityEvent onUpHandler;

	public UnityEvent onDownHandler;

	public void OnPointerUp(PointerEventData eventData)
	{
		onUpHandler.Invoke();
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		onDownHandler.Invoke();
	}
}
