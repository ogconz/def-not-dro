using System;
using System.Collections.Generic;
using Chat.Protocol;

namespace Chat
{
	public class ChannelProvider
	{
		private Dictionary<Guid, MessageChannel> m_channelsMap = new Dictionary<Guid, MessageChannel>();

		public event Action<MessageChannel> onChannelAdded;

		public event Action<MessageChannel> onChannelRemoved;

		public void Release()
		{
			RemoveAll();
		}

		public void AddOrUpdate(TargetType type, Guid id, string caption)
		{
			bool num = !m_channelsMap.ContainsKey(id);
			if (num)
			{
				TargetInfo target = new TargetInfo
				{
					type = type,
					id = id
				};
				m_channelsMap[id] = new MessageChannel(target);
			}
			m_channelsMap[id].SetCaption(caption);
			if (num)
			{
				this.onChannelAdded.SafeCall(m_channelsMap[id]);
			}
		}

		public void Remove(Guid id)
		{
			if (m_channelsMap.Remove(id, out var value))
			{
				this.onChannelRemoved.SafeCall(value);
			}
		}

		public void RemoveAll()
		{
			Guid[] array = new Guid[m_channelsMap.Count];
			m_channelsMap.Keys.CopyTo(array, 0);
			Guid[] array2 = array;
			foreach (Guid id in array2)
			{
				Remove(id);
			}
		}

		public MessageChannel Get(Guid id)
		{
			return m_channelsMap.GetValueOrDefault(id);
		}

		public IEnumerable<MessageChannel> GetAllByType(TargetType type)
		{
			foreach (MessageChannel value in m_channelsMap.Values)
			{
				if (value.GetTargetInfo().type == type)
				{
					yield return value;
				}
			}
		}

		public IEnumerator<MessageChannel> GetEnumerator()
		{
			foreach (MessageChannel value in m_channelsMap.Values)
			{
				yield return value;
			}
		}
	}
}
