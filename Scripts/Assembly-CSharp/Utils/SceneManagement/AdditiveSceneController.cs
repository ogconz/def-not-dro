using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Utils.SceneManagement
{
	public class AdditiveSceneController
	{
		private AsyncOperationHandle<SceneInstance> m_lastLoadedSceneHandle;

		private Scene m_lastLoadedScene;

		private Scene m_sceneToDelayedUnload;

		public string lastSceneName { get; private set; }

		public Scene lastScene { get; private set; }

		public bool IsReady()
		{
			if (m_lastLoadedScene.IsValid() && m_lastLoadedScene.isLoaded)
			{
				return true;
			}
			if (m_lastLoadedSceneHandle.IsValid() && m_lastLoadedSceneHandle.IsDone)
			{
				return true;
			}
			return false;
		}

		public void Unload()
		{
			if (m_lastLoadedScene.IsValid() && m_lastLoadedScene.isLoaded)
			{
				SceneManager.UnloadSceneAsync(m_lastLoadedScene, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
			}
			if (m_sceneToDelayedUnload.IsValid() && m_sceneToDelayedUnload.isLoaded)
			{
				SceneManager.UnloadSceneAsync(m_sceneToDelayedUnload, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
			}
			if (m_lastLoadedSceneHandle.IsValid())
			{
				Addressables.UnloadSceneAsync(m_lastLoadedSceneHandle);
			}
			lastSceneName = string.Empty;
			lastScene = default(Scene);
		}

		public void Update()
		{
			if (m_sceneToDelayedUnload.IsValid() && m_sceneToDelayedUnload.isLoaded)
			{
				SceneManager.UnloadSceneAsync(m_sceneToDelayedUnload, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
				m_sceneToDelayedUnload = default(Scene);
			}
		}

		public void LoadAdditionalScene(string sceneName)
		{
			if (IsSceneAlreadyLoaded(sceneName))
			{
				return;
			}
			if (m_lastLoadedScene.IsValid())
			{
				if (m_lastLoadedScene.isLoaded)
				{
					SceneManager.UnloadSceneAsync(m_lastLoadedScene, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
					m_lastLoadedScene = default(Scene);
				}
				else
				{
					m_sceneToDelayedUnload = m_lastLoadedScene;
					SetActiveRootGameObjects(m_sceneToDelayedUnload, activity: false);
				}
			}
			lastScene = default(Scene);
			if (!TryLoadSceneFromBuildList(sceneName))
			{
				AsyncOperationHandle<SceneInstance> prevLoadedSceneHandle = m_lastLoadedSceneHandle;
				m_lastLoadedSceneHandle = Addressables.LoadSceneAsync(sceneName + ".unity", LoadSceneMode.Additive);
				m_lastLoadedSceneHandle.Completed += delegate(AsyncOperationHandle<SceneInstance> _)
				{
					lastScene = _.Result.Scene;
					if (prevLoadedSceneHandle.IsValid())
					{
						Addressables.UnloadSceneAsync(prevLoadedSceneHandle);
					}
				};
			}
			lastSceneName = sceneName;
		}

		private void SetActiveRootGameObjects(Scene scene, bool activity)
		{
			GameObject[] rootGameObjects = scene.GetRootGameObjects();
			for (int i = 0; i < rootGameObjects.Length; i++)
			{
				rootGameObjects[i].SetActive(activity);
			}
		}

		private bool IsSceneAlreadyLoaded(string sceneName)
		{
			return lastSceneName == sceneName;
		}

		private bool TryLoadSceneFromBuildList(string sceneName)
		{
			if (SceneLoader.ContainsInBuildList(sceneName))
			{
				if (m_lastLoadedSceneHandle.IsValid())
				{
					Addressables.UnloadSceneAsync(m_lastLoadedSceneHandle);
				}
				m_lastLoadedScene = SceneManager.LoadScene(sceneName, new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.None));
				lastScene = m_lastLoadedScene;
				return true;
			}
			return false;
		}
	}
}
