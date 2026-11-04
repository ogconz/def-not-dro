using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SimpleJSON
{
	internal class JSONArray : JSONNode, IEnumerable
	{
		private JSONCache m_cache;

		private List<JSONNode> m_List = new List<JSONNode>(16);

		public override JSONNode this[int aIndex]
		{
			get
			{
				if (aIndex < 0 || aIndex >= m_List.Count)
				{
					return new JSONLazyCreator(m_cache, this);
				}
				return m_List[aIndex];
			}
			set
			{
				if (aIndex < 0 || aIndex >= m_List.Count)
				{
					m_List.Add(value);
				}
				else
				{
					m_List[aIndex] = value;
				}
			}
		}

		public override JSONNode this[string aKey]
		{
			get
			{
				return new JSONLazyCreator(m_cache, this);
			}
			set
			{
				m_List.Add(value);
			}
		}

		public override int Count => m_List.Count;

		public override IEnumerable<JSONNode> Children
		{
			get
			{
				foreach (JSONNode item in m_List)
				{
					yield return item;
				}
			}
		}

		public void Init(JSONCache cache)
		{
			m_List.Clear();
			m_cache = cache;
		}

		public override void Free()
		{
			base.Free();
			m_List.Clear();
			m_cache = null;
		}

		public override void Add(string aKey, JSONNode aItem)
		{
			m_List.Add(aItem);
		}

		public override JSONNode Remove(int aIndex)
		{
			if (aIndex < 0 || aIndex >= m_List.Count)
			{
				return null;
			}
			JSONNode result = m_List[aIndex];
			m_List.RemoveAt(aIndex);
			return result;
		}

		public override JSONNode Remove(string aKey)
		{
			for (int i = 0; i < m_List.Count; i++)
			{
				if (m_List[i].Value == aKey)
				{
					JSONNode result = m_List[i];
					m_List.RemoveAt(i);
					return result;
				}
			}
			return null;
		}

		public override JSONNode Remove(JSONNode aNode)
		{
			m_List.Remove(aNode);
			return aNode;
		}

		public IEnumerator GetEnumerator()
		{
			foreach (JSONNode item in m_List)
			{
				yield return item;
			}
		}

		public override void ToString(ref StringBuilder sb)
		{
			sb.Append("[ ");
			bool flag = false;
			int i = 0;
			for (int count = m_List.Count; i < count; i++)
			{
				if (flag)
				{
					sb.Append(", ");
				}
				flag = true;
				m_List[i].ToString(ref sb);
			}
			sb.Append(" ]");
		}

		public override void ToString(ref StringBuilder sb, string aPrefix)
		{
			sb.Append("[ ");
			bool flag = false;
			int i = 0;
			for (int count = m_List.Count; i < count; i++)
			{
				if (flag)
				{
					sb.Append(", ");
				}
				flag = true;
				sb.Append("\n");
				sb.Append(aPrefix);
				sb.Append("   ");
				m_List[i].ToString(ref sb, aPrefix + "   ");
			}
			sb.Append("\n");
			sb.Append(aPrefix);
			sb.Append("]");
		}

		public override void Serialize(BinaryWriter aWriter)
		{
			aWriter.Write((byte)1);
			aWriter.Write(m_List.Count);
			for (int i = 0; i < m_List.Count; i++)
			{
				m_List[i].Serialize(aWriter);
			}
		}
	}
}
