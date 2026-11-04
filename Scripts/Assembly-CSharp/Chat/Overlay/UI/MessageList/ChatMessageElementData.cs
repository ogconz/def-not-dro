using Binding;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Chat.Overlay.UI.MessageList
{
	public class ChatMessageElementData : SelectableListElementData
	{
		private readonly StringProperty m_content = new StringProperty();

		private readonly StringProperty m_time = new StringProperty();

		private readonly TransformProperty m_transform = new TransformProperty();

		private readonly SyncStatusHandler m_statusHandler = new SyncStatusHandler();

		private readonly TransformProperty m_hintLayoutTransform = new TransformProperty();

		private readonly BoolProperty m_hintVisible = new BoolProperty();

		private readonly Message m_message;

		public ChatMessageElementData(Message message)
		{
			m_message = message;
			m_content.value = m_message.username + ": " + m_message.content;
			m_time.value = m_message.timestamp.ToString("HH:mm");
			m_statusHandler.Setup(m_message);
		}

		~ChatMessageElementData()
		{
			m_statusHandler.Release();
		}

		public Message GetMessage()
		{
			return m_message;
		}

		public Vector2 GetContentPosition()
		{
			return m_transform.value.position;
		}

		public RectTransform GetHintRectTransform()
		{
			return (RectTransform)m_hintLayoutTransform.value;
		}

		public void SetHintVisible(bool visible)
		{
			m_hintVisible.value = visible;
		}

		public override void OnPointerDown(PointerEventData eventData)
		{
			base.OnPointerDown(eventData);
			if (base.selected)
			{
				eventData.selectedObject = null;
			}
		}

		public override void OnPointerClick(PointerEventData eventData)
		{
			base.OnPointerClick(eventData);
			base.selected = true;
			Click();
		}

		public override void OnDeselect(BaseEventData eventData)
		{
			base.OnDeselect(eventData);
			base.selected = false;
		}
	}
}
