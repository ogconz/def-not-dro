using UnityEngine;
using UnityEngine.EventSystems;

namespace Binding.Elements
{
	[RequireComponent(typeof(NonDrawingGraphic))]
	public class SelectableListElement : BaseListElement, ISelectHandler, IEventSystemHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
	{
		public virtual void OnSelect(BaseEventData eventData)
		{
			if (base.data is SelectableListElementData selectableListElementData)
			{
				selectableListElementData.OnSelect(eventData);
			}
		}

		public virtual void OnDeselect(BaseEventData eventData)
		{
			if (base.data is SelectableListElementData selectableListElementData)
			{
				selectableListElementData.OnDeselect(eventData);
			}
		}

		public virtual void OnPointerEnter(PointerEventData eventData)
		{
			if (base.data is SelectableListElementData selectableListElementData)
			{
				selectableListElementData.OnPointerEnter(eventData);
			}
		}

		public virtual void OnPointerExit(PointerEventData eventData)
		{
			if (base.data is SelectableListElementData selectableListElementData)
			{
				selectableListElementData.OnPointerExit(eventData);
			}
		}

		public virtual void OnPointerDown(PointerEventData eventData)
		{
			if (base.data is SelectableListElementData selectableListElementData)
			{
				selectableListElementData.OnPointerDown(eventData);
			}
		}

		public virtual void OnPointerUp(PointerEventData eventData)
		{
			if (base.data is SelectableListElementData selectableListElementData)
			{
				selectableListElementData.OnPointerUp(eventData);
			}
		}

		public virtual void OnPointerClick(PointerEventData eventData)
		{
			if (base.data is SelectableListElementData selectableListElementData)
			{
				eventData.selectedObject = base.gameObject;
				selectableListElementData.OnPointerClick(eventData);
			}
		}
	}
}
