using Binding;
using UnityEngine.EventSystems;

namespace Chat.Overlay.UI.ChannelList
{
	public class ChatChannelElementData : SelectableListElementData
	{
		private readonly StringProperty m_caption = new StringProperty();

		private readonly MessageChannel m_channel;

		public ChatChannelElementData(MessageChannel channel)
		{
			m_channel = channel;
			SetDataFromChannel();
			BindHandlers();
		}

		~ChatChannelElementData()
		{
			ReleaseHandlers();
		}

		public MessageChannel GetChannel()
		{
			return m_channel;
		}

		private void BindHandlers()
		{
			m_channel.onCaptionUpdated += OnCaptionUpdatedHandler;
		}

		private void ReleaseHandlers()
		{
			m_channel.onCaptionUpdated -= OnCaptionUpdatedHandler;
		}

		private void OnCaptionUpdatedHandler(string caption)
		{
			m_caption.value = caption;
		}

		private void SetDataFromChannel()
		{
			OnCaptionUpdatedHandler(m_channel.GetCaption());
		}

		public override void OnPointerClick(PointerEventData eventData)
		{
			base.OnPointerClick(eventData);
			Click();
		}
	}
}
