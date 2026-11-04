using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ResourceLoadHandler<TObject> where TObject : UnityEngine.Object
{
	private Action<TObject> m_onLoad;

	private AsyncOperationHandle<TObject> m_handle;

	public string lastPath { get; private set; }

	public TObject resource
	{
		get
		{
			if (!m_handle.IsValid() || !m_handle.IsDone)
			{
				return null;
			}
			return m_handle.Result;
		}
	}

	~ResourceLoadHandler()
	{
		Unload();
	}

	public void Load(string path, Action<TObject> onLoad)
	{
		if (lastPath == path && m_handle.IsValid())
		{
			if (m_handle.IsDone)
			{
				onLoad?.Invoke(m_handle.Result);
			}
		}
		else
		{
			Unload();
			lastPath = path;
			m_onLoad = onLoad;
			m_handle = Addressables.LoadAssetAsync<TObject>(path);
			m_handle.Completed += OnLoadInternal;
		}
	}

	private void OnLoadInternal(AsyncOperationHandle<TObject> handle)
	{
		if (m_onLoad != null)
		{
			m_handle.Completed -= OnLoadInternal;
			m_onLoad(handle.Result);
			m_onLoad = null;
		}
	}

	public void Unload()
	{
		m_onLoad = null;
		lastPath = string.Empty;
		if (m_handle.IsValid())
		{
			m_handle.Completed -= OnLoadInternal;
			Addressables.Release(m_handle);
		}
		m_handle = default(AsyncOperationHandle<TObject>);
	}
}
