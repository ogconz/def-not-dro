using Binding;
using Binding.Base;
using Chat.Overlay.UI.State;
using UnityEngine.EventSystems;

namespace Chat.Overlay.UI.Window
{
	public class WindowEventHandler : BaseBindingTarget
	{
		private EventTriggerProperty m_windowEventTrigger = new EventTriggerProperty();

		private StateContext m_state;

		public void Setup(StateContext state)
		{
			m_state = state;
			m_windowEventTrigger.Add(EventTriggerType.PointerEnter, OnPointerEnterHandler);
			m_windowEventTrigger.Add(EventTriggerType.PointerExit, OnPointerExitHandler);
		}

		public void Release()
		{
			m_windowEventTrigger.Remove(EventTriggerType.PointerEnter, OnPointerEnterHandler);
			m_windowEventTrigger.Remove(EventTriggerType.PointerExit, OnPointerExitHandler);
			m_state = null;
		}

		private void OnPointerEnterHandler(BaseEventData data)
		{
			m_state.windowHovered.Set(value: true);
		}

		private void OnPointerExitHandler(BaseEventData data)
		{
			m_state.windowHovered.Set(value: false);
		}
	}
}
