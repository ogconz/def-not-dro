using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StockEngineTypesSO", menuName = "StockEngineTypesSO", order = 51)]
public class StockEngineTypesSO : ScriptableObject
{
	[Serializable]
	public class EngineEntry
	{
		public string name;

		public EngineInfo.Type type;
	}

	public List<EngineEntry> engineEntries = new List<EngineEntry>();

	public Dictionary<string, EngineInfo.Type> ToDictionary()
	{
		Dictionary<string, EngineInfo.Type> dictionary = new Dictionary<string, EngineInfo.Type>();
		foreach (EngineEntry engineEntry in engineEntries)
		{
			if (!dictionary.ContainsKey(engineEntry.name))
			{
				dictionary.Add(engineEntry.name, engineEntry.type);
			}
		}
		return dictionary;
	}
}
