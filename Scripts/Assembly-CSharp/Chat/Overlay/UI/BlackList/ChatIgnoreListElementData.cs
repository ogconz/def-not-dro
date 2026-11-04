using Binding;

namespace Chat.Overlay.UI.BlackList
{
	public class ChatIgnoreListElementData : BaseListElementData
	{
		private readonly StringProperty m_username = new StringProperty();

		private readonly PlaceholderFading m_placeholderFading = new PlaceholderFading();

		private readonly IgnoreListElementEventHandler m_eventHandler = new IgnoreListElementEventHandler();

		private readonly UserInfo m_user;

		public ChatIgnoreListElementData(UserInfo user)
		{
			m_user = user;
			m_username.value = m_user.GetUsername();
			m_eventHandler.Setup(m_placeholderFading, this);
		}

		public UserInfo GetUserInfo()
		{
			return m_user;
		}

		public override void OnDestroy()
		{
			m_eventHandler.Release();
			base.OnDestroy();
		}
	}
}
