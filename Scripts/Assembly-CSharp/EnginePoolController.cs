using System.Collections.Generic;
using UnityEngine;

public class EnginePoolController : MonoBehaviour
{
	private List<(int swapId, GameObject swapPrefab)> m_pool = new List<(int, GameObject)>();

	private SwapsAsset m_swapsAsset;

	private List<GameObject> m_toRemove = new List<GameObject>();

	public int count => base.transform.childCount;

	private void Awake()
	{
		Object.DontDestroyOnLoad(this);
	}

	public void InitPool()
	{
		m_swapsAsset = SwapsAsset.LoadAndInitDB();
		foreach (EngineInfo swap in m_swapsAsset.swaps)
		{
			for (int i = 0; i < swap.poolInstantiateCount; i++)
			{
				GameObject gameObject = Object.Instantiate(swap.prefab, base.transform);
				gameObject.SetActive(value: false);
				m_pool.Add((swap.id, gameObject));
			}
		}
	}

	public GameObject GivePrefabFromPool(int swapId)
	{
		GameObject item = m_pool.Find(((int swapId, GameObject swapPrefab) el) => el.swapId == swapId && el.swapPrefab != null && !el.swapPrefab.activeInHierarchy).swapPrefab;
		item.SetActive(value: true);
		return item;
	}

	public void ReturnToPool(int swapId, GameObject gameObject)
	{
		gameObject.SetActive(value: false);
		gameObject.transform.SetParent(base.transform);
		ClearPoolPrefabsIfNeed();
	}

	private void ClearPoolPrefabsIfNeed()
	{
		m_toRemove.Clear();
		for (int i = 0; i < base.transform.childCount; i++)
		{
			Transform child = base.transform.GetChild(i);
			bool flag = false;
			foreach (EngineInfo swap in m_swapsAsset.swaps)
			{
				GameObject gameObject = child.gameObject.RemoveCloneFromName();
				if (swap.prefab.name == gameObject.name)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				m_toRemove.Add(child.gameObject);
			}
		}
		m_toRemove.ForEach(delegate(GameObject go)
		{
			Object.Destroy(go);
		});
	}
}
