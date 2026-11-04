using System;
using Binding;
using Chat.Features;
using Chat.Overlay.UI.ChannelList;
using Chat.Protocol;

namespace Chat.Overlay.UI
{
	public class ChannelSelectionHolder
	{
		private ChannelProvider m_channelProvider;

		private ChannelHolder m_channelHolder;

		private ListProperty m_listProperty;

		private bool m_isAutoSelect;

		private bool m_isSavedChannelFound;

		public void Setup(ChannelProvider channelProvider, ChannelHolder channelHolder, ListProperty listProperty)
		{
			m_channelProvider = channelProvider;
			m_channelHolder = channelHolder;
			m_listProperty = listProperty;
			m_listProperty.OnElementInserted += OnElementInsertedHandler;
			m_listProperty.OnElementRemoved += OnElementRemovedHandler;
			m_listProperty.OnSelectionChanged += OnSelectionChangedHandler;
		}

		public void Release()
		{
			m_listProperty.OnElementInserted -= OnElementInsertedHandler;
			m_listProperty.OnElementRemoved -= OnElementRemovedHandler;
			m_listProperty.OnSelectionChanged -= OnSelectionChangedHandler;
			m_channelProvider = null;
			m_channelHolder = null;
			m_listProperty = null;
		}

		private void OnElementInsertedHandler(int index, BaseListElementData item)
		{
			if (TryCastToChannel(item, out var channel))
			{
				TrySelectSpecialChannelIfNeed(channel);
				if (IsSavedChannel(channel))
				{
					TrySelectSavedChannelIfNeed();
				}
				if (!IsChannelSelected())
				{
					TrySelectFirstChannel();
				}
			}
		}

		private void OnElementRemovedHandler(int index, BaseListElementData item)
		{
			if (TryCastToChannel(item, out var channel))
			{
				if (IsSavedChannel(channel))
				{
					m_isSavedChannelFound = false;
					m_channelHolder.ResetSelectedChannel();
				}
				if (!IsChannelSelected())
				{
					m_isSavedChannelFound = false;
					TrySelectSavedChannelIfNeed();
				}
				if (!IsChannelSelected())
				{
					TrySelectFirstChannel();
				}
			}
		}

		private void OnSelectionChangedHandler(BaseListElementData item)
		{
			if (item.selected && (!m_isAutoSelect || !m_channelHolder.HasSelectedChannel()) && item is ChatChannelElementData chatChannelElementData && CanSave(chatChannelElementData.GetChannel()))
			{
				Guid id = chatChannelElementData.GetChannel().GetTargetInfo().id;
				m_channelHolder.SetSelectedChannel(id);
				m_isSavedChannelFound = true;
			}
		}

		private bool IsChannelSelected()
		{
			return m_listProperty.GetSelectedIndex() != -1;
		}

		private int ElementIndexOfChannel(MessageChannel channel)
		{
			return m_listProperty.IndexOf((BaseListElementData item) => item is ChatChannelElementData chatChannelElementData && object.Equals(chatChannelElementData.GetChannel(), channel));
		}

		private bool IsSavedChannel(MessageChannel channel)
		{
			return channel.GetTargetInfo().id == m_channelHolder.GetSelectedChannel();
		}

		private void TrySelectSavedChannelIfNeed()
		{
			if (m_channelHolder.HasSelectedChannel() && !m_isSavedChannelFound)
			{
				MessageChannel messageChannel = m_channelProvider.Get(m_channelHolder.GetSelectedChannel());
				if (messageChannel != null)
				{
					int index = ElementIndexOfChannel(messageChannel);
					m_listProperty.Selection(index, selected: true);
				}
			}
		}

		private void TrySelectFirstChannel()
		{
			SetAutoSelectedIndex(0);
		}

		private void TrySelectSpecialChannelIfNeed(MessageChannel channel)
		{
			if (IsForceFocusChannel(channel))
			{
				int autoSelectedIndex = ElementIndexOfChannel(channel);
				SetAutoSelectedIndex(autoSelectedIndex);
			}
		}

		private void SetAutoSelectedIndex(int index)
		{
			m_isAutoSelect = true;
			m_listProperty.Selection(index, selected: true);
			m_isAutoSelect = false;
		}

		private bool CanSave(MessageChannel channel)
		{
			return channel.GetTargetInfo().type == TargetType.Global;
		}

		private bool IsForceFocusChannel(MessageChannel channel)
		{
			return channel.GetTargetInfo().type == TargetType.Mp_Room;
		}

		private bool TryCastToChannel(BaseListElementData item, out MessageChannel channel)
		{
			if (item is ChatChannelElementData chatChannelElementData)
			{
				channel = chatChannelElementData.GetChannel();
				return channel != null;
			}
			channel = null;
			return false;
		}
	}
}
