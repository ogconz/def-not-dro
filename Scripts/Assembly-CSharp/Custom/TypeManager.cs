using System;
using System.Collections.Generic;
using SimpleJSON;

namespace Custom
{
	public class TypeManager
	{
		private readonly Dictionary<string, Type> m_types = new Dictionary<string, Type>();

		public void AddType<T>()
		{
			Type typeFromHandle = typeof(T);
			m_types.Add(typeFromHandle.Name, typeFromHandle);
		}

		public bool Get(string name, out Type result)
		{
			return m_types.TryGetValue(name, out result);
		}

		public string[] GetAllNames()
		{
			string[] array = new string[m_types.Count];
			int num = 0;
			foreach (string key in m_types.Keys)
			{
				array[num] = key;
				num++;
			}
			return array;
		}

		public object Create(string name)
		{
			if (!Get(name, out var result))
			{
				throw new Exception($"Type with name - {name} not found. All types - {JSON.Serialize(GetAllNames())}");
			}
			return Activator.CreateInstance(result);
		}
	}
}
