using UnityEngine;

public abstract class ScriptableSingleton<T> : ScriptableObject where T : ScriptableSingleton<T>
{
	private static T m_cachedInstance;

	private static string fileName => typeof(T).Name;

	private static string resourcePath => fileName;

	public static T instance
	{
		get
		{
			if (m_cachedInstance == null)
			{
				m_cachedInstance = ResourcesWrapper.Load<T>(resourcePath);
				if (m_cachedInstance != null)
				{
					m_cachedInstance.OnInitialize();
				}
			}
			if (m_cachedInstance == null)
			{
				Debug.LogWarning("No instance of " + fileName + " found, using default values");
				m_cachedInstance = ScriptableObject.CreateInstance<T>();
				m_cachedInstance.hideFlags = HideFlags.HideAndDontSave;
				m_cachedInstance.OnInitialize();
			}
			return m_cachedInstance;
		}
	}

	protected virtual void OnInitialize()
	{
	}
}
