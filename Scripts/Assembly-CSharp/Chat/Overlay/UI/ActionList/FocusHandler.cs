using System.Collections.Generic;
using Binding;
using Binding.Base;
using Chat.Overlay.UI.State;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Chat.Overlay.UI.ActionList
{
	public class FocusHandler : BaseBindingTarget
	{
		private EventTriggerProperty m_actionPanelEventTrigger = new EventTriggerProperty();

		private StateContext m_state;

		private ChatActionListProperty m_actions;

		private List<RaycastResult> m_raycastResultCache;

		public void Setup(StateContext state, ChatActionListProperty actions)
		{
			m_state = state;
			m_actions = actions;
			m_raycastResultCache = new List<RaycastResult>();
			m_actionPanelEventTrigger.Add(EventTriggerType.PointerEnter, OnPointerEnterHandler);
			m_actionPanelEventTrigger.Add(EventTriggerType.PointerExit, OnPointerExitHandler);
			m_actionPanelEventTrigger.Add(EventTriggerType.Select, OnSelectHandler);
			m_actionPanelEventTrigger.Add(EventTriggerType.Deselect, OnDeselectHandler);
			m_actions.onCalledAction += OnCalledActionHandler;
			m_state.actionPanelActive.onChanged += OnActionPanelActiveHandler;
		}

		public void Release()
		{
			m_actionPanelEventTrigger.Remove(EventTriggerType.PointerEnter, OnPointerEnterHandler);
			m_actionPanelEventTrigger.Remove(EventTriggerType.PointerExit, OnPointerExitHandler);
			m_actionPanelEventTrigger.Remove(EventTriggerType.Select, OnSelectHandler);
			m_actionPanelEventTrigger.Remove(EventTriggerType.Deselect, OnDeselectHandler);
			m_actions.onCalledAction -= OnCalledActionHandler;
			m_state.actionPanelActive.onChanged -= OnActionPanelActiveHandler;
			m_state = null;
			m_actions = null;
			m_raycastResultCache.Clear();
			m_raycastResultCache = null;
		}

		private void OnPointerEnterHandler(BaseEventData data)
		{
			m_state.actionPanelHovered.Set(value: true);
		}

		private void OnPointerExitHandler(BaseEventData data)
		{
			m_state.actionPanelHovered.Set(value: false);
		}

		private void OnSelectHandler(BaseEventData data)
		{
			m_state.actionPanelFocused.Set(value: true);
		}

		private void OnDeselectHandler(BaseEventData data)
		{
			if (CanDeselect())
			{
				m_state.actionPanelFocused.Set(value: false);
			}
		}

		private void OnCalledActionHandler()
		{
			m_state.actionPanelFocused.Set(value: false);
		}

		private void OnActionPanelActiveHandler(bool isActive)
		{
			EventSystem current = EventSystem.current;
			if (!current.alreadySelecting)
			{
				GameObject rootOfTrigger = GetRootOfTrigger();
				bool flag = current.currentSelectedGameObject == rootOfTrigger;
				if (isActive && !flag)
				{
					current.SetSelectedGameObject(rootOfTrigger);
				}
				else if (!isActive && flag)
				{
					current.SetSelectedGameObject(null);
				}
			}
		}

		private bool CanDeselect()
		{
			bool result = true;
			PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
			pointerEventData.position = Input.mousePosition;
			EventSystem.current.RaycastAll(pointerEventData, m_raycastResultCache);
			for (int i = 0; i < m_raycastResultCache.Count; i++)
			{
				if (m_raycastResultCache[i].gameObject == GetRootOfTrigger())
				{
					result = false;
					break;
				}
			}
			m_raycastResultCache.Clear();
			return result;
		}

		private GameObject GetRootOfTrigger()
		{
			return m_actionPanelEventTrigger.value.gameObject;
		}
	}
}
