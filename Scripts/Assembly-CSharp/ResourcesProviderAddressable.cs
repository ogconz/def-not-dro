using CarModelSystem;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

[CreateAssetMenu(fileName = "ResourcesProviderAddressable", menuName = "Car Model/ResourcesProviderAddressable")]
public class ResourcesProviderAddressable : ResourcesProvider
{
	private class ResourceRequest<T> : IResourceRequest<T> where T : Object
	{
		private AsyncOperationHandle<T> m_handle;

		public bool isDone => m_handle.IsDone;

		public int priority { get; set; }

		public T asset => m_handle.Result;

		public ResourceRequest(AsyncOperationHandle<T> handle)
		{
			m_handle = handle;
		}
	}

	public override T Load<T>(string path)
	{
		return ResourcesWrapper.Load<T>(path);
	}

	public override IResourceRequest<T> LoadAsync<T>(string path)
	{
		return new ResourceRequest<T>(ResourcesWrapper.LoadAsync<T>(path));
	}

	public override void UnloadAsset(Object asset)
	{
		ResourcesWrapper.UnloadAsset(asset);
	}
}
