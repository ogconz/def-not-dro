using System;
using Binding;
using Binding.Base;
using UnityEngine.EventSystems;

namespace Chat.Overlay.UI.BlackList
{
	public class IgnoreListElementEventHandler : BaseBindingTarget
	{
		private EventTriggerProperty m_ignoreListElementEventTrigger = new EventTriggerProperty();

		[NonSerialized]
		private PlaceholderFading m_placeholderFading;

		[NonSerialized]
		private BaseListElementData m_element;

		public void Setup(PlaceholderFading placeholderFading, BaseListElementData element)
		{
			m_placeholderFading = placeholderFading;
			m_element = element;
			m_element.OnSelectionChanged += OnSelectionChangedHandler;
			m_ignoreListElementEventTrigger.Add(EventTriggerType.PointerEnter, OnPointerEnterHandler);
			m_ignoreListElementEventTrigger.Add(EventTriggerType.PointerExit, OnPointerExitHandler);
			m_ignoreListElementEventTrigger.Add(EventTriggerType.PointerDown, OnPointerDownHandler);
			m_ignoreListElementEventTrigger.Add(EventTriggerType.PointerUp, OnPointerUpHandler);
		}

		public void Release()
		{
			m_element.OnSelectionChanged -= OnSelectionChangedHandler;
			m_ignoreListElementEventTrigger.Remove(EventTriggerType.PointerEnter, OnPointerEnterHandler);
			m_ignoreListElementEventTrigger.Remove(EventTriggerType.PointerExit, OnPointerExitHandler);
			m_ignoreListElementEventTrigger.Remove(EventTriggerType.PointerDown, OnPointerDownHandler);
			m_ignoreListElementEventTrigger.Remove(EventTriggerType.PointerUp, OnPointerUpHandler);
			m_placeholderFading = null;
			m_element = null;
		}

		private void OnSelectionChangedHandler(BaseListElementData item)
		{
			m_placeholderFading.SetHover(item.selected);
		}

		private void OnPointerEnterHandler(BaseEventData data)
		{
			m_element.selected = true;
		}

		private void OnPointerExitHandler(BaseEventData data)
		{
			m_element.selected = false;
		}

		private void OnPointerDownHandler(BaseEventData data)
		{
			m_placeholderFading.SetDown(value: true);
		}

		private void OnPointerUpHandler(BaseEventData data)
		{
			m_placeholderFading.SetDown(value: false);
		}
	}
}
