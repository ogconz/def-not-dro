using System;
using System.Collections.Generic;
using UnityEngine;

public class PoolObject : IDisposable
{
	private readonly Queue<GameObject> m_pool = new Queue<GameObject>();

	private Dictionary<int, GameObject> m_elemenents = new Dictionary<int, GameObject>();

	public GameObject Pop(GameObject prefab)
	{
		GameObject gameObject;
		if (m_pool.Count == 0)
		{
			gameObject = UnityEngine.Object.Instantiate(prefab);
			m_elemenents.Add(gameObject.gameObject.GetInstanceID(), gameObject.gameObject);
		}
		else
		{
			gameObject = m_pool.Dequeue();
			gameObject.gameObject.SetActive(value: true);
		}
		return gameObject;
	}

	public T Pop<T>(T prefab) where T : Component
	{
		GameObject gameObject;
		if (m_pool.Count == 0)
		{
			gameObject = UnityEngine.Object.Instantiate(prefab.gameObject);
			m_elemenents.Add(gameObject.gameObject.GetInstanceID(), gameObject.gameObject);
		}
		else
		{
			gameObject = m_pool.Dequeue();
			gameObject.gameObject.SetActive(value: true);
		}
		return gameObject.GetComponent<T>();
	}

	public void Push(GameObject pointData)
	{
		int instanceID = pointData.gameObject.GetInstanceID();
		if (m_elemenents.TryGetValue(instanceID, out var value))
		{
			m_pool.Enqueue(value);
			value.gameObject.SetActive(value: false);
		}
	}

	public void Push<T>(T pointData) where T : Component
	{
		int instanceID = pointData.gameObject.GetInstanceID();
		if (m_elemenents.TryGetValue(instanceID, out var value))
		{
			m_pool.Enqueue(value);
			value.gameObject.SetActive(value: false);
		}
	}

	public void Dispose()
	{
		foreach (KeyValuePair<int, GameObject> elemenent in m_elemenents)
		{
			UnityEngine.Object.Destroy(elemenent.Value.gameObject);
		}
		m_elemenents.Clear();
		m_pool.Clear();
	}
}
