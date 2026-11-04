using System;
using System.Collections.Generic;
using UnityEngine;

public static class CustomTagManager
{
	private static Dictionary<string, HashSet<GameObject>> m_db = new Dictionary<string, HashSet<GameObject>>();

	private static Dictionary<GameObject, string> m_names = new Dictionary<GameObject, string>();

	public static void Register(string name, GameObject go)
	{
		m_db.GetOrSetDefault(name).Add(go);
		m_names.Add(go, name);
	}

	public static void Unregister(GameObject go)
	{
		if (!m_names.TryGetValue(go, out var value))
		{
			return;
		}
		if (m_db.TryGetValue(value, out var value2))
		{
			value2.Remove(go);
			if (value2.Count == 0)
			{
				m_db.Remove(value);
			}
		}
		m_names.Remove(go);
	}

	public static HashSet<GameObject> Get(string tag)
	{
		if (m_db.TryGetValue(tag, out var value))
		{
			return value;
		}
		return new HashSet<GameObject>();
	}

	private static V GetOrSetDefault<K, V>(this Dictionary<K, V> dictionary, K key)
	{
		if (dictionary.TryGetValue(key, out var value))
		{
			return value;
		}
		value = Activator.CreateInstance<V>();
		dictionary.Add(key, value);
		return value;
	}
}
