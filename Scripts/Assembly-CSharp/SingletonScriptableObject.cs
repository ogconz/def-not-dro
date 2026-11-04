using System;
using UnityEngine;

public class SingletonScriptableObject<T> : ScriptableObject where T : SingletonScriptableObject<T>
{
	private static T m_instance;

	public static T instance
	{
		get
		{
			if (m_instance == null)
			{
				T val = Resources.Load<T>("DB/" + typeof(T).Name);
				if (val == null)
				{
					throw new Exception("(Singleton) Not find config for resource : " + typeof(T).Name);
				}
				val.OnLoaded();
				m_instance = val;
			}
			return m_instance;
		}
	}

	protected void Save()
	{
	}

	public static void Unload()
	{
		if (m_instance != null)
		{
			m_instance = null;
			Resources.UnloadUnusedAssets();
		}
	}

	protected virtual void OnLoaded()
	{
	}
}
