using System;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public abstract class BaseVinylItemViewModel<T> : BaseListItem where T : UnityEngine.Object
{
	private T m_icon;

	private AsyncOperationHandle<T> m_handle;

	private bool m_needUnload;

	public bool IsLoaded()
	{
		return m_icon != null;
	}

	protected abstract string GetTexturePath();

	public void LoadTexture(Action<T> onTextureLoaded)
	{
		string texturePath = GetTexturePath();
		if (string.IsNullOrEmpty(texturePath))
		{
			return;
		}
		if (IsLoaded())
		{
			onTextureLoaded.SafeCall(m_icon);
			return;
		}
		m_needUnload = false;
		m_handle = ResourcesWrapper.LoadAsync(texturePath, delegate(T result)
		{
			OnResourceLoad(result, onTextureLoaded);
		}, "vinyl_editor");
	}

	private void OnResourceLoad(T result, Action<T> onTextureLoaded)
	{
		if (m_needUnload || !m_handle.IsValid() || result == null)
		{
			Unload();
			return;
		}
		m_icon = result;
		onTextureLoaded.SafeCall(m_icon);
	}

	public void Unload()
	{
		m_needUnload = true;
		if (IsLoaded())
		{
			m_icon = null;
			m_needUnload = false;
			ResourcesWrapper.UnloadAsset(m_handle);
		}
	}
}
