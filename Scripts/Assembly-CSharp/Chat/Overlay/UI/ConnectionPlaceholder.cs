using Binding;
using Binding.Base;
using Cysharp.Threading.Tasks;

namespace Chat.Overlay.UI
{
	public class ConnectionPlaceholder : BaseBindingTarget
	{
		private BoolProperty m_showConnectAnimation = new BoolProperty();

		private ChatClient m_client;

		public void Setup(ChatClient client)
		{
			m_client = client;
			m_client.eventHandler.onConnected += OnConnectHandler;
			m_client.eventHandler.onDisconnected += OnDisconnectHandler;
			m_client.eventHandler.onError += OnDisconnectHandler;
			ShowConnectAnimation(!m_client.IsConnected());
		}

		public void Release()
		{
			m_client.eventHandler.onConnected -= OnConnectHandler;
			m_client.eventHandler.onDisconnected -= OnDisconnectHandler;
			m_client.eventHandler.onError -= OnDisconnectHandler;
			m_client = null;
		}

		private void OnConnectHandler()
		{
			ShowConnectAnimation(value: false);
		}

		private void OnDisconnectHandler()
		{
			ShowConnectAnimation(value: true);
		}

		private void ShowConnectAnimation(bool value)
		{
			UniTask.Post(delegate
			{
				m_showConnectAnimation.value = value;
			});
		}
	}
}
