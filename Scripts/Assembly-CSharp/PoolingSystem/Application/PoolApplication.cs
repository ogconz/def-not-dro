using System;
using PoolingSystem.Controller;
using UnityEngine;

namespace PoolingSystem.Application
{
	public class PoolApplication : IPoolApplication
	{
		protected readonly IPoolController m_poolController;

		public PoolApplication(IPoolController poolController)
		{
			m_poolController = poolController ?? throw new NullReferenceException("IPoolController");
		}

		public GameObject GetGameObjectFromPool(GameObject prefab, Vector3 pos, Transform parent = null)
		{
			return m_poolController.CreateFromPool(prefab, pos, parent);
		}

		public T GetComponentFromPool<T>(T prefab, Transform parent = null) where T : Component
		{
			return m_poolController.CreateFromPool(prefab, parent);
		}

		public void Return(GameObject createdObject)
		{
			m_poolController.ReturnToPool(createdObject);
		}
	}
}
