using System;
using System.Collections.Generic;
using UnityEngine;

namespace chats
{
	public static class ChatClientEndpointManager
	{
		private class EndPointDescriptor
		{
			public ChatClientEndpoint endpoint;

			public int refCount;

			public float releasedTime;

			public bool requested;
		}

		private static Dictionary<string, EndPointDescriptor> m_endpoints;

		static ChatClientEndpointManager()
		{
			m_endpoints = new Dictionary<string, EndPointDescriptor>();
			TimeController.onUpdate = (Action)Delegate.Combine(TimeController.onUpdate, (Action)delegate
			{
				foreach (EndPointDescriptor value in m_endpoints.Values)
				{
					if (value.refCount <= 0)
					{
						if (Time.unscaledTime > value.releasedTime + 60f)
						{
							value.endpoint.Disconnect();
							m_endpoints.Remove(value.endpoint.uri);
							break;
						}
					}
					else if (value.requested)
					{
						value.requested = false;
						if (!value.endpoint.isConnected)
						{
							value.endpoint.Connect();
						}
					}
				}
			});
		}

		public static ChatClientEndpoint Get(string endpoint)
		{
			if (m_endpoints.TryGetValue(endpoint, out var value))
			{
				value.refCount++;
			}
			else
			{
				value = new EndPointDescriptor();
				value.endpoint = new ChatClientEndpoint(endpoint);
				value.refCount++;
				m_endpoints.Add(endpoint, value);
				value.endpoint.Connect();
			}
			value.requested = true;
			return value.endpoint;
		}

		public static void Release(ref ChatClientEndpoint endpoint)
		{
			if (endpoint != null)
			{
				if (m_endpoints.TryGetValue(endpoint.uri, out var value))
				{
					value.refCount--;
					value.releasedTime = Time.unscaledTime;
				}
				else
				{
					endpoint.Disconnect();
				}
				endpoint = null;
			}
		}
	}
}
