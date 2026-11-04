using System;
using System.Collections;
using DI;
using GameOverlay;
using UnityEngine.SceneManagement;

[Obsolete("UserSettings is deprecated")]
public class UserSettings : BaseLoggingMonobeh
{
	private IEnumerator Start()
	{
		yield return null;
		CallMethodWithLogging("start", Apply);
	}

	private void OnApplicationQuit()
	{
		if (Overlay.IsReady())
		{
			DependencyInjector.Resolve<GamePrefs>().globals.Save();
		}
	}

	private void OnEnable()
	{
		SceneManager.sceneLoaded += OnLevelFinishedLoading;
	}

	private void OnDisable()
	{
		SceneManager.sceneLoaded -= OnLevelFinishedLoading;
	}

	private void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
	{
		StartCoroutine(OnLevelFinishedLoading());
	}

	private IEnumerator OnLevelFinishedLoading()
	{
		yield return null;
		Apply();
	}

	public static void Apply()
	{
		if (Overlay.IsReady())
		{
			DependencyInjector.Resolve<GamePrefs>().globals.Apply();
		}
	}
}
