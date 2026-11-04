using System;
using UnityEngine;

public class ContextPreloader : MonoBehaviour
{
	public BaseContext[] contextsArray;

	public static ContextPreloader instance { get; private set; }

	private void Awake()
	{
		if (instance != null)
		{
			Debug.LogWarning("ContextPreloader mutliple instances!", instance);
		}
		instance = this;
	}

	private void OnDestroy()
	{
		if (instance == this)
		{
			instance = null;
		}
	}

	public static BaseContext GetContext(string name)
	{
		if (instance != null)
		{
			return Array.Find(instance.contextsArray, (BaseContext item) => item != null && item.name == name);
		}
		return null;
	}
}
