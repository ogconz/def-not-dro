using System;
using Binding;
using UnityEngine.EventSystems;

namespace Chat.Overlay.UI.ActionList
{
	public class ChatActionElementData : SelectableListElementData
	{
		private readonly StringProperty m_caption = new StringProperty();

		private readonly Action m_action;

		public ChatActionElementData(string caption, Action action)
		{
			m_caption.value = caption;
			m_action = action;
		}

		public Action GetAction()
		{
			return m_action;
		}

		public override void OnPointerEnter(PointerEventData eventData)
		{
			base.OnPointerEnter(eventData);
			base.selected = true;
		}

		public override void OnPointerExit(PointerEventData eventData)
		{
			base.OnPointerExit(eventData);
			base.selected = false;
		}

		public override void OnPointerClick(PointerEventData eventData)
		{
			base.OnPointerClick(eventData);
			Click();
		}
	}
}
