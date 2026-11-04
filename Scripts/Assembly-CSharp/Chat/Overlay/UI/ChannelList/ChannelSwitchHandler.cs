using Binding;
using Chat.Overlay.UI.State;

namespace Chat.Overlay.UI.ChannelList
{
	public class ChannelSwitchHandler
	{
		private StateContext m_state;

		private ChannelSwitchReader m_channelSwitchReader;

		private ListProperty m_listProperty;

		public void Setup(StateContext state, ChannelSwitchReader channelSwitchReader, ListProperty listProperty)
		{
			m_state = state;
			m_channelSwitchReader = channelSwitchReader;
			m_listProperty = listProperty;
			m_channelSwitchReader.onMoveEvent += SelectByOffset;
		}

		public void Release()
		{
			m_channelSwitchReader.onMoveEvent -= SelectByOffset;
			m_state = null;
			m_channelSwitchReader = null;
			m_listProperty = null;
		}

		private void SelectByOffset(int offset)
		{
			int num = m_listProperty.GetSelectedIndex() + offset;
			SelectInputFieldIfChannelEmpty(num);
			m_listProperty.Selection(num, selected: true);
		}

		private void SelectInputFieldIfChannelEmpty(int channelIndex)
		{
			if (m_listProperty.GetByIndex(channelIndex) is ChatChannelElementData chatChannelElementData && chatChannelElementData.GetChannel().GetMessages().Count == 0 && m_state.controlLayerActive.Get() && !m_state.deviceWithNavigation.Get())
			{
				m_state.inputFieldFocused.Set(value: true);
			}
		}
	}
}
