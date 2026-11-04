using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using Utils.SceneManagement;

namespace Services.Anzu
{
	public class AnzuAdditiveSceneController
	{
		private AdditiveSceneController m_additiveSceneLoadController = new AdditiveSceneController();

		private string m_additiveSceneName;

		public bool isSceneActive => m_additiveSceneLoadController.IsReady();

		public AnzuAdditiveSceneController(string additiveSceneName)
		{
			m_additiveSceneName = additiveSceneName;
		}

		public async UniTask SetActiveSceneAsync(bool activity)
		{
			if (isSceneActive == activity)
			{
				return;
			}
			if (activity)
			{
				TryLoadAdditiveScene();
				await UniTask.WaitUntil(() => isSceneActive);
			}
			else
			{
				UnloadCurrScene();
			}
		}

		public bool TryGetActiveScene(out Scene scene)
		{
			if (isSceneActive)
			{
				scene = m_additiveSceneLoadController.lastScene;
				return true;
			}
			scene = default(Scene);
			return false;
		}

		public void Update()
		{
			m_additiveSceneLoadController.Update();
		}

		public void Dispose()
		{
			UnloadCurrScene();
		}

		private void UnloadCurrScene()
		{
			m_additiveSceneLoadController.Unload();
		}

		private bool TryLoadAdditiveScene()
		{
			if (string.IsNullOrEmpty(m_additiveSceneName))
			{
				return false;
			}
			m_additiveSceneLoadController.LoadAdditionalScene(m_additiveSceneName);
			return true;
		}
	}
}
