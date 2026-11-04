using System.Collections.Generic;
using UnityEngine;

public abstract class ExternalCarPartPostProcess : IExternalCarPartPostProcess
{
	protected const string bundleFormat = "bundle";

	protected string m_defaultKeyword;

	protected Dictionary<string, string> m_keywordsPerFormat;

	protected abstract void Action(RaceCar raceCar, GameObject gameObject);

	public void Process(RaceCar raceCar, GameObject gameObject, string format)
	{
		string gameObjectName = gameObject.name.ToLower();
		if (IsAvaliableModelFormat(gameObjectName, format) || ContainsDefaultKeyword(gameObjectName))
		{
			Action(raceCar, gameObject);
		}
	}

	private bool IsAvaliableModelFormat(string gameObjectName, string format)
	{
		if (m_keywordsPerFormat.TryGetValue(format, out var value))
		{
			return gameObjectName.Contains(value);
		}
		return false;
	}

	private bool ContainsDefaultKeyword(string gameObjectName)
	{
		return gameObjectName.Contains(m_defaultKeyword);
	}
}
