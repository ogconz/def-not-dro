using System;
using System.Collections.Generic;
using Chat.Protocol;
using Chat.Socket;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Chat
{
	public class EventDispatcher
	{
		private readonly Dictionary<string, Type> m_eventMap = new Dictionary<string, Type>
		{
			["PLAYER_INFO"] = typeof(PlayerInfoPayload),
			["ROOMS_INFO"] = typeof(RoomsInfoPayload),
			["LEAVE_ROOM"] = typeof(LeaveRoomPayload),
			["MESSAGE"] = typeof(RecvMessagePayload),
			["ROOM_REMOVED"] = typeof(RoomRemovedPayload),
			["BAN"] = typeof(BanInfoPayload)
		};

		private readonly List<string> m_ignoreMessages = new List<string> { "ping", "pong" };

		private readonly Dictionary<Type, List<Delegate>> m_listeners = new Dictionary<Type, List<Delegate>>();

		private Chat.Socket.EventHandler m_eventHandler;

		public void Setup(Chat.Socket.EventHandler eventHandler)
		{
			m_eventHandler = eventHandler;
			m_eventHandler.onMessage += Dispatch;
		}

		public void Release()
		{
			m_eventHandler.onMessage -= Dispatch;
			m_eventHandler = null;
		}

		public void AddListener<T>(Action<T> listener)
		{
			Type typeFromHandle = typeof(T);
			if (!m_listeners.ContainsKey(typeFromHandle))
			{
				m_listeners.Add(typeFromHandle, new List<Delegate>());
			}
			m_listeners[typeFromHandle].Add(listener);
		}

		public void RemoveListener<T>(Action<T> listener)
		{
			Type typeFromHandle = typeof(T);
			if (m_listeners.TryGetValue(typeFromHandle, out var value))
			{
				value.Remove(listener);
				if (value.Count == 0)
				{
					m_listeners.Remove(typeFromHandle);
				}
			}
		}

		private void Dispatch(string message)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(message) || m_ignoreMessages.Contains(message))
				{
					return;
				}
				JToken jsonToken = JObject.Parse(message);
				if (!TryParseEventType(jsonToken, out var eventType) || !TryParsePayload(jsonToken, eventType, out var payload) || !m_listeners.TryGetValue(eventType, out var value))
				{
					return;
				}
				foreach (Delegate item in value)
				{
					item.DynamicInvoke(payload);
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		private bool TryParseEventType(JToken jsonToken, out Type eventType)
		{
			string text = (string?)jsonToken.SelectToken("type");
			if (string.IsNullOrEmpty(text))
			{
				eventType = null;
				return false;
			}
			return m_eventMap.TryGetValue(text, out eventType);
		}

		private bool TryParsePayload(JToken jsonToken, Type eventType, out object payload)
		{
			jsonToken = jsonToken.SelectToken("payload");
			payload = jsonToken?.ToObject(eventType);
			return payload != null;
		}
	}
}
