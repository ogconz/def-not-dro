using System.Text;

namespace SimpleJSON
{
	internal class JSONLazyCreator : JSONNode
	{
		private JSONNode m_Node;

		private string m_Key;

		private JSONCache m_cache;

		public override JSONNode this[int aIndex]
		{
			get
			{
				return new JSONLazyCreator(m_cache, this);
			}
			set
			{
				JSONArray jSONArray = m_cache.AllocateJSONArray();
				jSONArray.Add(value);
				Set(jSONArray);
			}
		}

		public override JSONNode this[string aKey]
		{
			get
			{
				return new JSONLazyCreator(m_cache, this, aKey);
			}
			set
			{
				JSONClass jSONClass = m_cache.AllocateJSONClass();
				jSONClass.Add(aKey, value);
				Set(jSONClass);
			}
		}

		public override int AsInt
		{
			get
			{
				JSONData aVal = m_cache.AllocateJSONData(0);
				Set(aVal);
				return 0;
			}
			set
			{
				JSONData aVal = m_cache.AllocateJSONData(value);
				Set(aVal);
			}
		}

		public override float AsFloat
		{
			get
			{
				JSONData aVal = m_cache.AllocateJSONData(0f);
				Set(aVal);
				return 0f;
			}
			set
			{
				JSONData aVal = m_cache.AllocateJSONData(value);
				Set(aVal);
			}
		}

		public override double AsDouble
		{
			get
			{
				JSONData aVal = m_cache.AllocateJSONData(0.0);
				Set(aVal);
				return 0.0;
			}
			set
			{
				JSONData aVal = m_cache.AllocateJSONData(value);
				Set(aVal);
			}
		}

		public override bool AsBool
		{
			get
			{
				JSONData aVal = m_cache.AllocateJSONData(aData: false);
				Set(aVal);
				return false;
			}
			set
			{
				JSONData aVal = m_cache.AllocateJSONData(value);
				Set(aVal);
			}
		}

		public override JSONArray AsArray
		{
			get
			{
				JSONArray jSONArray = m_cache.AllocateJSONArray();
				Set(jSONArray);
				return jSONArray;
			}
		}

		public override JSONClass AsObject
		{
			get
			{
				JSONClass jSONClass = m_cache.AllocateJSONClass();
				Set(jSONClass);
				return jSONClass;
			}
		}

		public JSONLazyCreator(JSONCache cache, JSONNode aNode, string aKey = null)
		{
			m_Node = aNode;
			m_Key = aKey;
			m_cache = cache;
		}

		private void Set(JSONNode aVal)
		{
			if (m_Key == null)
			{
				m_Node.Add(aVal);
			}
			else
			{
				m_Node.Add(m_Key, aVal);
			}
			m_Node = null;
			m_cache = null;
		}

		public override void Add(JSONNode aItem)
		{
			JSONArray jSONArray = m_cache.AllocateJSONArray();
			jSONArray.Add(aItem);
			Set(jSONArray);
		}

		public override void Add(string aKey, JSONNode aItem)
		{
			JSONClass jSONClass = m_cache.AllocateJSONClass();
			jSONClass.Add(aKey, aItem);
			Set(jSONClass);
		}

		public static bool operator ==(JSONLazyCreator a, object b)
		{
			if (b == null)
			{
				return true;
			}
			return (object)a == b;
		}

		public static bool operator !=(JSONLazyCreator a, object b)
		{
			return !(a == b);
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return true;
			}
			return (object)this == obj;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override void ToString(ref StringBuilder sb)
		{
			sb.Append("");
		}

		public override void ToString(ref StringBuilder sb, string aPrefix)
		{
			sb.Append("");
		}
	}
}
