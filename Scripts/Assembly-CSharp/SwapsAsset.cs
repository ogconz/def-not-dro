using System.Collections.Generic;
using UnityEngine;

public class SwapsAsset : ScriptableObject
{
	private const string DbPath = "DB/SwapsAsset";

	private static SwapsAsset m_db;

	[SerializeField]
	private List<EngineInfo> m_swaps = new List<EngineInfo>();

	private readonly Dictionary<int, EngineInfo> m_swapsDictionary = new Dictionary<int, EngineInfo>();

	public IReadOnlyCollection<EngineInfo> swaps => m_swaps;

	public static SwapsAsset LoadAndInitDB()
	{
		if (m_db == null)
		{
			m_db = Resources.Load<SwapsAsset>("DB/SwapsAsset");
		}
		if (m_db == null)
		{
			return null;
		}
		foreach (EngineInfo swap in m_db.m_swaps)
		{
			m_db.m_swapsDictionary[swap.id] = swap;
		}
		return m_db;
	}

	public EngineInfo Find(int id)
	{
		if (!m_swapsDictionary.TryGetValue(id, out var value))
		{
			value = m_swaps.Find((EngineInfo x) => x.id == id);
			m_swapsDictionary.Add(id, value);
		}
		return value ?? new EngineInfo
		{
			id = 0
		};
	}
}
