using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class ClickableObject : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
{
	public UnityEvent LeftClick;

	public UnityEvent RightClick;

	public UnityEvent MiddleClick;

	public void OnPointerClick(PointerEventData eventData)
	{
		switch (eventData.button)
		{
		case PointerEventData.InputButton.Left:
			LeftClick.Invoke();
			break;
		case PointerEventData.InputButton.Middle:
			MiddleClick.Invoke();
			break;
		case PointerEventData.InputButton.Right:
			RightClick.Invoke();
			break;
		}
	}
}
