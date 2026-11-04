using System;
using System.Collections.Generic;

namespace SyncMultiplayer
{
	public class NetGameNotifySystem : BaseNetGameSystem
	{
		private const string NotifyKey = "key";

		private NetworkGame m_game;

		private Dictionary<string, Action<NetworkPlayer>> m_events = new Dictionary<string, Action<NetworkPlayer>>();

		public override void Attach(NetGameSystemsManager owner)
		{
			base.Attach(owner);
			m_game = owner.host;
			m_game.packetHandler.Subscribe(PacketId.Notify, Handler);
		}

		public void Subscribe(string key, Action<NetworkPlayer> action)
		{
			if (!m_events.ContainsKey(key))
			{
				m_events.Add(key, null);
			}
			Dictionary<string, Action<NetworkPlayer>> events = m_events;
			string key2 = key;
			events[key2] = (Action<NetworkPlayer>)Delegate.Remove(events[key2], action);
			events = m_events;
			key2 = key;
			events[key2] = (Action<NetworkPlayer>)Delegate.Combine(events[key2], action);
		}

		public void Unsubscribe(string key, Action<NetworkPlayer> action)
		{
			if (m_events.ContainsKey(key))
			{
				Dictionary<string, Action<NetworkPlayer>> events = m_events;
				events[key] = (Action<NetworkPlayer>)Delegate.Remove(events[key], action);
			}
		}

		public void Send(string key)
		{
			SmartfoxDataPackage data = new SmartfoxDataPackage(PacketId.Notify);
			data.Add("key", key);
			base.owner.host.SendOthers(data, reliable: true);
		}

		private void Handler(NetworkPlayer sender, SmartfoxDataPackage data)
		{
			if (data.Data.ContainsKey("key"))
			{
				string utfString = data.Data.GetUtfString("key");
				if (!string.IsNullOrWhiteSpace(utfString) && m_events.TryGetValue(utfString, out var value))
				{
					value.SafeCall(sender);
				}
			}
		}

		public void HandleProps(NetworkPlayer sender, HashSet<string> changedProps)
		{
			foreach (KeyValuePair<string, Action<NetworkPlayer>> @event in m_events)
			{
				if (changedProps.Contains(@event.Key))
				{
					@event.Value.SafeCall(sender);
				}
			}
		}
	}
}
