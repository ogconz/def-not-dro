using System;
using System.Text.RegularExpressions;

namespace Chat.Socket
{
	public class EventHandler
	{
		private ushort m_lastDisconnectInternalCode;

		public ushort lastDisconnectInternalCode => m_lastDisconnectInternalCode;

		public event Action onConnected;

		public event Action onDisconnected;

		public event Action onError;

		public event Action<string> onMessage;

		public void OnConnectHandler()
		{
			GameLog.Print(AlertLevel.Warning, "Chat", "Socket is connected");
			this.onConnected.SafeCall();
		}

		public void OnDisconnectHandler(ushort code, string message)
		{
			GameLog.Print(AlertLevel.Warning, "Chat", $"Socket is disconnected: {code} {message}");
			m_lastDisconnectInternalCode = GetInternalCodeFromMessage(message);
			this.onDisconnected.SafeCall();
		}

		public void OnErrorHandler(string reason)
		{
			GameLog.Print(AlertLevel.Error, "Chat", reason);
			this.onError.SafeCall();
		}

		public void OnConnectionErrorHandler(string reason)
		{
			GameLog.Print(AlertLevel.Notify, "Chat", reason);
			this.onError.SafeCall();
		}

		public void OnMessageHandler(string message)
		{
			GameLog.Print(AlertLevel.Notify, "Chat", "Receive data: " + message);
			this.onMessage.SafeCall(message);
		}

		private ushort GetInternalCodeFromMessage(string message)
		{
			if (!ushort.TryParse(Regex.Match(message, "\\d+").Value, out var result))
			{
				return 0;
			}
			return result;
		}
	}
}
