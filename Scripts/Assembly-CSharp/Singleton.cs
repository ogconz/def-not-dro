using System;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
	public static bool isQuitting;

	public static bool isInitialized;

	private static T m_instance;

	public static T instance
	{
		get
		{
			if (isQuitting)
			{
				if (m_instance is IAlwaysAccessibleOnQuit)
				{
					return m_instance;
				}
				return null;
			}
			return m_instance ?? CreateInstance();
		}
	}

	public static bool hasInstance
	{
		get
		{
			if (!isQuitting || m_instance is IAlwaysAccessibleOnQuit)
			{
				return m_instance != null;
			}
			return false;
		}
	}

	protected virtual void Awake()
	{
		if (m_instance == null || m_instance == this)
		{
			m_instance = this as T;
			if (this is IKeepAliveMonoBehaviourSingleton)
			{
				UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
			}
			try
			{
				if (!isInitialized)
				{
					Initialization();
				}
				return;
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				return;
			}
			finally
			{
				isInitialized = true;
			}
		}
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private void OnDestroy()
	{
		try
		{
			if (this == m_instance)
			{
				try
				{
					Finalization();
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
				finally
				{
					isInitialized = false;
				}
			}
		}
		catch (Exception exception2)
		{
			Debug.LogException(exception2);
		}
		if (this == m_instance)
		{
			m_instance = null;
		}
	}

	private void OnApplicationQuit()
	{
		isQuitting = true;
	}

	protected virtual void Initialization()
	{
	}

	protected virtual void Finalization()
	{
	}

	public static T CreateInstance()
	{
		if (m_instance == null)
		{
			try
			{
				m_instance = UnityEngine.Object.FindObjectOfType<T>();
			}
			catch (Exception message)
			{
				Debug.Log(message);
			}
			finally
			{
				if (Application.isPlaying)
				{
					if (!(m_instance == null))
					{
						m_instance.gameObject.SetActive(value: false);
						UnityEngine.Object.Destroy(m_instance.gameObject);
						m_instance = null;
						Debug.LogWarning("Singleton -> Force creating: " + typeof(T).Name);
					}
					GameObject obj = new GameObject(typeof(T).ToString());
					obj.SetActive(value: false);
					m_instance = obj.AddComponent<T>();
					obj.SetActive(value: true);
				}
			}
		}
		return m_instance;
	}
}
