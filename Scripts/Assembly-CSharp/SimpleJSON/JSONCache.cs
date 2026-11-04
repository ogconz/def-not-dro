using System.Text;

namespace SimpleJSON
{
	internal class JSONCache
	{
		private JSONCacheLine<JSONData> m_jsonDataCache = new JSONCacheLine<JSONData>(128, 16);

		private JSONCacheLine<JSONClass> m_jsonClassCache = new JSONCacheLine<JSONClass>(64);

		private JSONCacheLine<JSONArray> m_jsonArrayCache = new JSONCacheLine<JSONArray>(64);

		public bool inUse { get; private set; }

		public StringBuilder stringBuilder { get; private set; } = new StringBuilder(1024);

		public int poolSize => m_jsonDataCache.poolSize + m_jsonClassCache.poolSize + m_jsonArrayCache.poolSize;

		public int usedNow => m_jsonDataCache.usedNow + m_jsonClassCache.usedNow + m_jsonArrayCache.usedNow;

		public void Init()
		{
			inUse = true;
			JSONNode.allocations = 0;
			m_jsonDataCache.Init();
			m_jsonClassCache.Init();
			m_jsonArrayCache.Init();
		}

		public void Free()
		{
			_ = m_jsonArrayCache.allocationSaved;
			_ = m_jsonClassCache.allocationSaved;
			_ = m_jsonDataCache.allocationSaved;
			m_jsonDataCache.Free();
			m_jsonClassCache.Free();
			m_jsonArrayCache.Free();
			stringBuilder.Clear();
			inUse = false;
		}

		public JSONClass AllocateJSONClass()
		{
			JSONClass jSONClass = m_jsonClassCache.New();
			jSONClass.Init(this);
			return jSONClass;
		}

		public JSONArray AllocateJSONArray()
		{
			JSONArray jSONArray = m_jsonArrayCache.New();
			jSONArray.Init(this);
			return jSONArray;
		}

		public JSONData AllocateJSONData(string aData)
		{
			JSONData jSONData = m_jsonDataCache.New();
			jSONData.Set(aData);
			return jSONData;
		}

		public JSONData AllocateJSONData(float aData)
		{
			JSONData jSONData = m_jsonDataCache.New();
			jSONData.AsFloat = aData;
			return jSONData;
		}

		public JSONData AllocateJSONData(double aData)
		{
			JSONData jSONData = m_jsonDataCache.New();
			jSONData.AsDouble = aData;
			return jSONData;
		}

		public JSONData AllocateJSONData(bool aData)
		{
			JSONData jSONData = m_jsonDataCache.New();
			jSONData.AsBool = aData;
			return jSONData;
		}

		public JSONData AllocateJSONData(int aData)
		{
			JSONData jSONData = m_jsonDataCache.New();
			jSONData.AsInt = aData;
			return jSONData;
		}
	}
}
