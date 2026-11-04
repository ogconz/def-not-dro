using System.Collections.Generic;
using UnityEngine;

namespace PoolingSystem.Controller
{
	public class PoolController : MonoBehaviour, IPoolController
	{
		private class Pool
		{
			public readonly LinkedList<GameObject> inUse = new LinkedList<GameObject>();

			public readonly LinkedList<GameObject> noUse = new LinkedList<GameObject>();
		}

		[SerializeField]
		private int m_defaultPoolCount = 1;

		private readonly Dictionary<GameObject, Pool> m_prefabToPool = new Dictionary<GameObject, Pool>();

		private readonly Dictionary<GameObject, Pool> m_objectToPool = new Dictionary<GameObject, Pool>();

		private void Awake()
		{
			Object.DontDestroyOnLoad(this);
		}

		public GameObject CreateFromPool(GameObject prefab, Vector3 pos, Transform parent = null)
		{
			GameObject gameObject = CreateFromPoolInternal(prefab.gameObject, parent);
			if (parent != null)
			{
				gameObject.transform.SetParent(parent, worldPositionStays: false);
			}
			gameObject.transform.position = pos;
			return gameObject;
		}

		public int GetPoolObjectsCount()
		{
			if (!(this != null))
			{
				return 0;
			}
			return base.transform.childCount;
		}

		public Transform GetPoolTransform()
		{
			if (!(this != null))
			{
				return null;
			}
			return base.transform;
		}

		public T CreateFromPool<T>(T prefab, Transform parent) where T : Component
		{
			GameObject obj = CreateFromPoolInternal(prefab.gameObject, parent);
			obj.transform.SetParent(parent, worldPositionStays: false);
			return obj.GetComponent<T>();
		}

		public void ReturnToPool(GameObject obj)
		{
			if (!(obj == null) && m_objectToPool.TryGetValue(obj, out var value) && value != null && !(this == null))
			{
				obj.transform.SetParent(base.transform, worldPositionStays: false);
				obj.transform.ResetPRS();
				obj.SetActive(value: false);
				if (value.inUse.Remove(obj))
				{
					value.noUse.AddLast(obj);
				}
			}
		}

		public void DestroyPoolObject(GameObject obj)
		{
			if (!(obj == null) && m_objectToPool.TryGetValue(obj, out var value))
			{
				value.noUse.Remove(obj);
				Object.Destroy(obj);
				obj = null;
			}
		}

		private Pool GetOrCreatePool(GameObject prefab, int count)
		{
			if (m_prefabToPool.TryGetValue(prefab, out var value))
			{
				return value;
			}
			value = (m_prefabToPool[prefab] = new Pool());
			for (int i = 0; i < count; i++)
			{
				GameObject gameObject = Object.Instantiate(prefab, base.transform);
				gameObject.SetActive(value: false);
				value.noUse.AddLast(gameObject);
				m_objectToPool[gameObject] = value;
			}
			return value;
		}

		private GameObject CreateFromPoolInternal(GameObject prefab, Transform holder)
		{
			if (!prefab)
			{
				return null;
			}
			Pool orCreatePool = GetOrCreatePool(prefab, m_defaultPoolCount);
			GameObject value;
			while (orCreatePool.noUse.Count > 0)
			{
				value = orCreatePool.noUse.Last.Value;
				orCreatePool.noUse.RemoveLast();
				if ((bool)value)
				{
					if (!value.gameObject.activeSelf)
					{
						orCreatePool.inUse.AddLast(value);
						value.SetActive(value: true);
						return value;
					}
					orCreatePool.inUse.AddLast(value);
				}
			}
			value = Object.Instantiate(prefab, holder ? holder : base.transform);
			orCreatePool.inUse.AddLast(value);
			m_objectToPool[value] = orCreatePool;
			return value;
		}
	}
}
