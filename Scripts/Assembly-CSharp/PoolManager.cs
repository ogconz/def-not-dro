using UnityEngine;
using UnityEngine.SceneManagement;

public class PoolManager : Singleton<PoolManager>
{
	public readonly int MaxInstanceCount = 24;

	private Pool<string, ObjectPool> m_pool;

	public Pool<string, ObjectPool> pool => m_pool;

	protected override void Awake()
	{
		base.Awake();
		m_pool = new Pool<string, ObjectPool>(MaxInstanceCount);
		SceneManager.sceneLoaded += delegate
		{
			Clear();
		};
	}

	public virtual bool CanPush()
	{
		return m_pool.CanPush();
	}

	public virtual bool Push(string groupKey, ObjectPool objectPool)
	{
		return m_pool.Push(groupKey, objectPool);
	}

	public virtual T PopOrCreate<T>(T prefab) where T : ObjectPool
	{
		return PopOrCreate(prefab, Vector3.zero, Quaternion.identity);
	}

	public virtual T PopOrCreate<T>(T prefab, Vector3 position, Quaternion rotation) where T : ObjectPool
	{
		T val = m_pool.Pop<T>(prefab.group);
		if (val == null)
		{
			val = CreateObject(prefab, position, rotation);
		}
		else
		{
			val.transform.position = position;
			val.transform.rotation = rotation;
		}
		return val;
	}

	public virtual T PopOrCreate<T>(T prefab, Transform parent) where T : ObjectPool
	{
		T val = m_pool.Pop<T>(prefab.group);
		if (val == null)
		{
			val = CreateObject(prefab, parent);
		}
		else
		{
			val.transform.SetParent(parent);
		}
		return val;
	}

	public virtual ObjectPool Pop(string groupKey)
	{
		return m_pool.Pop<ObjectPool>(groupKey);
	}

	public virtual T Pop<T>() where T : ObjectPool
	{
		return m_pool.Pop<T>();
	}

	public virtual T Pop<T>(Pool<string, ObjectPool>.Compare<T> comparer) where T : ObjectPool
	{
		return m_pool.Pop(comparer);
	}

	public virtual T Pop<T>(string groupKey) where T : ObjectPool
	{
		return m_pool.Pop<T>(groupKey);
	}

	public virtual bool Contains(string groupKey)
	{
		return m_pool.Contains(groupKey);
	}

	public virtual void Clear()
	{
		m_pool.Clear();
	}

	protected virtual T CreateObject<T>(T prefab, Vector3 position, Quaternion rotation) where T : ObjectPool
	{
		T component = Object.Instantiate(prefab.gameObject, position, rotation).GetComponent<T>();
		component.name = prefab.name;
		component.Create();
		return component;
	}

	protected virtual T CreateObject<T>(T prefab, Transform parent) where T : ObjectPool
	{
		T component = Object.Instantiate(prefab.gameObject, parent).GetComponent<T>();
		component.name = prefab.name;
		component.Create();
		return component;
	}
}
