using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SimpleJSON
{
	internal class JSONClass : JSONNode, IEnumerable
	{
		private JSONCache m_cache;

		private Dictionary<string, JSONNode> m_Dict = new Dictionary<string, JSONNode>(16);

		public Dictionary<string, JSONNode>.KeyCollection Keys => m_Dict.Keys;

		public override JSONNode this[string aKey]
		{
			get
			{
				if (m_Dict.ContainsKey(aKey))
				{
					return m_Dict[aKey];
				}
				return new JSONLazyCreator(m_cache, this, aKey);
			}
			set
			{
				if (m_Dict.ContainsKey(aKey))
				{
					m_Dict[aKey] = value;
				}
				else
				{
					m_Dict.Add(aKey, value);
				}
			}
		}

		public override JSONNode this[int aIndex]
		{
			get
			{
				if (aIndex < 0 || aIndex >= m_Dict.Count)
				{
					return null;
				}
				return m_Dict.ElementAt(aIndex).Value;
			}
			set
			{
				if (aIndex >= 0 && aIndex < m_Dict.Count)
				{
					string key = m_Dict.ElementAt(aIndex).Key;
					m_Dict[key] = value;
				}
			}
		}

		public override int Count => m_Dict.Count;

		public override IEnumerable<JSONNode> Children
		{
			get
			{
				foreach (KeyValuePair<string, JSONNode> item in m_Dict)
				{
					yield return item.Value;
				}
			}
		}

		public void Init(JSONCache cache)
		{
			m_Dict.Clear();
			m_cache = cache;
		}

		public override void Free()
		{
			base.Free();
			m_Dict.Clear();
			m_cache = null;
		}

		public override void Add(string aKey, JSONNode aItem)
		{
			if (!string.IsNullOrEmpty(aKey))
			{
				m_Dict[aKey] = aItem;
			}
			else
			{
				m_Dict.Add(Guid.NewGuid().ToString(), aItem);
			}
		}

		public override JSONNode Remove(string aKey)
		{
			if (!m_Dict.ContainsKey(aKey))
			{
				return null;
			}
			JSONNode result = m_Dict[aKey];
			m_Dict.Remove(aKey);
			return result;
		}

		public override JSONNode Remove(int aIndex)
		{
			if (aIndex < 0 || aIndex >= m_Dict.Count)
			{
				return null;
			}
			KeyValuePair<string, JSONNode> keyValuePair = m_Dict.ElementAt(aIndex);
			m_Dict.Remove(keyValuePair.Key);
			return keyValuePair.Value;
		}

		public override JSONNode Remove(JSONNode aNode)
		{
			try
			{
				KeyValuePair<string, JSONNode> keyValuePair = m_Dict.Where((KeyValuePair<string, JSONNode> k) => k.Value == aNode).First();
				m_Dict.Remove(keyValuePair.Key);
				return aNode;
			}
			catch
			{
				return null;
			}
		}

		public override bool HasKey(string key)
		{
			return m_Dict.ContainsKey(key);
		}

		public IEnumerator GetEnumerator()
		{
			foreach (KeyValuePair<string, JSONNode> item in m_Dict)
			{
				yield return item;
			}
		}

		public override void ToString(ref StringBuilder sb)
		{
			sb.Append("{");
			bool flag = false;
			foreach (KeyValuePair<string, JSONNode> item in m_Dict)
			{
				if (flag)
				{
					sb.Append(", ");
				}
				flag = true;
				sb.Append("\"");
				JSONNode.Escape(ref sb, item.Key);
				sb.Append("\":");
				item.Value.ToString(ref sb);
			}
			sb.Append("}");
		}

		public override void ToString(ref StringBuilder sb, string aPrefix)
		{
			sb.Append("{ ");
			bool flag = false;
			foreach (KeyValuePair<string, JSONNode> item in m_Dict)
			{
				if (flag)
				{
					sb.Append(", ");
				}
				flag = true;
				sb.Append("\n");
				sb.Append(aPrefix);
				sb.Append("   ");
				sb.Append("\"");
				JSONNode.Escape(ref sb, item.Key);
				sb.Append("\" : ");
				item.Value.ToString(ref sb, aPrefix + "   ");
			}
			sb.Append("\n");
			sb.Append(aPrefix);
			sb.Append("}");
		}

		public override void Serialize(BinaryWriter aWriter)
		{
			aWriter.Write((byte)2);
			aWriter.Write(m_Dict.Count);
			foreach (string key in m_Dict.Keys)
			{
				aWriter.Write(key);
				m_Dict[key].Serialize(aWriter);
			}
		}
	}
}
