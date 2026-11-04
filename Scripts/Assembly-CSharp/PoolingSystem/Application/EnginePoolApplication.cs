using System;
using System.Collections.Generic;
using PoolingSystem.Controller;
using UnityEngine;

namespace PoolingSystem.Application
{
	public class EnginePoolApplication : PoolApplication
	{
		private readonly List<(int swapId, GameObject swapPrefab)> m_pool = new List<(int, GameObject)>(84);

		private readonly List<(int swapId, GameObject swapPrefab)> m_copyToRemovePool = new List<(int, GameObject)>();

		private readonly List<GameObject> m_extraUniqueEngines = new List<GameObject>(3);

		private readonly HashSet<GameObject> m_uniqueEngines = new HashSet<GameObject>();

		private readonly int m_maxUniqueEnginesPool = 2;

		private SwapsAsset m_swapsAsset;

		public EnginePoolApplication(IPoolController poolController)
			: base(poolController)
		{
		}

		public void PreparePool()
		{
			LoadSwapsDB();
			foreach (EngineInfo swap in m_swapsAsset.swaps)
			{
				for (int i = 0; i < swap.poolInstantiateCount; i++)
				{
					CreatePrefabInPool(swap);
				}
			}
		}

		public int GetPoolCount()
		{
			return m_poolController.GetPoolObjectsCount();
		}

		public GameObject GivePrefabFromPool(int swapId)
		{
			foreach (var item in m_pool)
			{
				if (item.swapId == swapId && (bool)item.swapPrefab && !IsUnpackedEngine(item.swapPrefab))
				{
					item.swapPrefab.SetActive(value: true);
					return item.swapPrefab;
				}
			}
			ClearEmptyEngines();
			return CreatePrefabInPool(m_swapsAsset.Find(swapId), enableOnStart: true);
		}

		public void ReturnToPool(GameObject engine)
		{
			if (m_poolController != null)
			{
				m_poolController.ReturnToPool(engine);
			}
			if (IsUniqueEngine(engine))
			{
				DeterminateUniqueEngines();
				UpdateUniqueEngines();
			}
		}

		private void LoadSwapsDB()
		{
			m_swapsAsset = SwapsAsset.LoadAndInitDB();
		}

		private GameObject CreatePrefabInPool(EngineInfo info, bool enableOnStart = false)
		{
			GameObject gameObjectFromPool = GetGameObjectFromPool(info.prefab, Vector3.zero);
			gameObjectFromPool.RemoveCloneFromName();
			gameObjectFromPool.SetActive(enableOnStart);
			m_pool.Add((info.id, gameObjectFromPool));
			return gameObjectFromPool;
		}

		private bool IsUniqueEngine(GameObject engine)
		{
			if (m_swapsAsset == null)
			{
				LoadSwapsDB();
			}
			foreach (EngineInfo swap in m_swapsAsset.swaps)
			{
				if (string.Equals(swap.prefab.name, engine.RemoveCloneFromName().name, StringComparison.CurrentCultureIgnoreCase))
				{
					return false;
				}
			}
			return true;
		}

		private bool IsUnpackedEngine(GameObject engine)
		{
			return engine.transform.parent != m_poolController.GetPoolTransform();
		}

		private void DeterminateUniqueEngines()
		{
			m_uniqueEngines.Clear();
			if (m_poolController == null)
			{
				return;
			}
			for (int i = 0; i < m_poolController.GetPoolObjectsCount(); i++)
			{
				Transform child = m_poolController.GetPoolTransform().GetChild(i);
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
					m_uniqueEngines.Add(child.gameObject);
				}
			}
		}

		private void UpdateUniqueEngines()
		{
			if (m_uniqueEngines.Count <= m_maxUniqueEnginesPool)
			{
				return;
			}
			m_extraUniqueEngines.Clear();
			foreach (GameObject uniqueEngine in m_uniqueEngines)
			{
				if (m_extraUniqueEngines.Count < m_maxUniqueEnginesPool)
				{
					m_extraUniqueEngines.Add(uniqueEngine);
					continue;
				}
				break;
			}
			m_extraUniqueEngines.ForEach(delegate(GameObject engine)
			{
				m_poolController.DestroyPoolObject(engine);
			});
		}

		private void ClearEmptyEngines()
		{
			m_copyToRemovePool.Clear();
			m_copyToRemovePool.AddRange(m_pool);
			m_pool.Clear();
			foreach (var item in m_copyToRemovePool)
			{
				if ((bool)item.swapPrefab)
				{
					m_pool.Add(item);
				}
			}
		}
	}
}
