using UnityEngine;

namespace PoolingSystem.Application
{
	public interface IPoolApplication
	{
		GameObject GetGameObjectFromPool(GameObject prefab, Vector3 pos, Transform parent = null);

		T GetComponentFromPool<T>(T prefab, Transform parent = null) where T : Component;

		void Return(GameObject createdObject);
	}
}
