using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Utils.SceneManagement
{
	public static class SceneLoader
	{
		private static Dictionary<string, Action<Scene>> m_includedScenes;

		static SceneLoader()
		{
			m_includedScenes = new Dictionary<string, Action<Scene>>();
			for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
			{
				string scenePathByBuildIndex = SceneUtility.GetScenePathByBuildIndex(i);
				AddScene(scenePathByBuildIndex.Substring(0, scenePathByBuildIndex.Length - 6).Substring(scenePathByBuildIndex.LastIndexOf('/') + 1), null);
			}
		}

		public static void AddScene(string sceneName, Action<Scene> callBack)
		{
			m_includedScenes[sceneName] = callBack;
		}

		public static bool ContainsInBuildList(string sceneName)
		{
			if (string.IsNullOrEmpty(sceneName))
			{
				return false;
			}
			return m_includedScenes.ContainsKey(sceneName);
		}

		public static bool HasScene(string sceneName)
		{
			bool flag = ContainsInBuildList(sceneName);
			if (!flag)
			{
				flag = ResourcesWrapper.ContainsAsset(sceneName);
			}
			return flag;
		}

		public static void LoadScene(string name, LoadSceneMode loadMode = LoadSceneMode.Single)
		{
			if (ContainsInBuildList(name))
			{
				SceneManager.LoadScene(name, loadMode);
			}
			else
			{
				ResourcesWrapper.LoadScene(name, loadMode);
			}
			if (m_includedScenes.TryGetValue(name, out var value))
			{
				value.SafeCall(SceneManager.GetActiveScene());
			}
		}

		public static IEnumerator LoadSceneAsync(string name, LoadSceneMode loadMode = LoadSceneMode.Single)
		{
			if (ContainsInBuildList(name))
			{
				yield return SceneManager.LoadSceneAsync(name, loadMode);
			}
			else
			{
				yield return ResourcesWrapper.LoadSceneAsync(name, loadMode);
			}
			if (m_includedScenes.TryGetValue(name, out var value))
			{
				value.SafeCall(SceneManager.GetActiveScene());
			}
		}
	}
}
