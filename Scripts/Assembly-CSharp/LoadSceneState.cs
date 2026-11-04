using System.Collections;
using DI;
using JukeBox;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils.SceneManagement;

public class LoadSceneState : BaseGameState
{
	[SerializeField]
	private bool m_asyncLoad;

	[SerializeField]
	private LoadingProgressBar m_progressBar;

	private BaseState m_currentlyLoadingSceneForState;

	public GameObject prefabScreen;

	private BaseState m_LoadingState;

	private bool m_canLoad;

	private void BeforeLoadStart()
	{
		SoundController.PlayMusic(MusicSignal.Silence);
	}

	private void AfterLoadFinished()
	{
	}

	public void LoadSceneAndSetState(BaseState state)
	{
		if (m_currentlyLoadingSceneForState != null)
		{
			if (States.isLogEnabled())
			{
				GameLog.Print(AlertLevel.Warning, "States", string.Format("[LoadSceneState] Loading scene \"{0}\" for state {1} ({2}) request while already being loading scene for state {3}. {4}", state.stateScene, state, m_asyncLoad ? "async mode" : "sync mode", m_currentlyLoadingSceneForState.name, (m_currentlyLoadingSceneForState == state) ? "Ignoring." : "Stopping previous loading."));
			}
			m_currentlyLoadingSceneForState = state;
		}
		else
		{
			m_currentlyLoadingSceneForState = state;
			if (States.isLogEnabled())
			{
				GameLog.Print(AlertLevel.Notify, "States", string.Format("[LoadSceneState] Loading scene \"{0}\" for state {1} ({2})", state.stateScene, state, m_asyncLoad ? "async mode" : "sync mode"));
			}
			BeforeLoadStart();
			States.Push<LoadSceneState>();
			StartCoroutine((!m_asyncLoad) ? SyncLoadStateScene(state) : AsyncLoadStateScene(state));
		}
	}

	private bool RestartLevelLoadingIfNeeded(BaseState theOneJustLoaded)
	{
		BaseState currentlyLoadingSceneForState = m_currentlyLoadingSceneForState;
		m_currentlyLoadingSceneForState = null;
		if (currentlyLoadingSceneForState != theOneJustLoaded)
		{
			LoadSceneAndSetState(currentlyLoadingSceneForState);
			return true;
		}
		return false;
	}

	private IEnumerator SyncLoadStateScene(BaseState state)
	{
		States.InitiateLevelLoading(state.stateScene);
		yield return null;
		if (!RestartLevelLoadingIfNeeded(state))
		{
			States.Change(state);
			AfterLoadFinished();
		}
	}

	private IEnumerator AsyncLoadStateScene(BaseState state)
	{
		m_LoadingState = state;
		m_canLoad = false;
		yield return new WaitForFixedUpdate();
		InitProgress();
		yield return States.InitiateLevelLoadingAsync("Empty");
		yield return States.ClearMemory();
		yield return States.InitiateLevelLoadingAsync(state.stateScene);
		string[] array = state.additiveScenes;
		foreach (string text in array)
		{
			yield return SceneLoader.LoadSceneAsync(text, LoadSceneMode.Additive);
		}
		UpdateProgress(1f);
		yield return null;
		DoneProgress();
		m_canLoad = true;
	}

	public override void OnUpdate()
	{
		if (m_canLoad)
		{
			m_canLoad = false;
			if (!RestartLevelLoadingIfNeeded(m_LoadingState))
			{
				States.Change(m_LoadingState);
				m_currentlyLoadingSceneForState = null;
				AfterLoadFinished();
			}
		}
	}

	private void InitProgress()
	{
		_ = m_progressBar == null;
	}

	private void UpdateProgress(float progress)
	{
		if ((bool)m_progressBar)
		{
			m_progressBar.progress = progress;
		}
	}

	private void DoneProgress()
	{
		if ((bool)m_progressBar)
		{
			m_progressBar.Done();
		}
	}

	public override void OnInit()
	{
		base.OnInit();
		this.MakeDependenciesInjected();
	}

	public override void OnEnter()
	{
		base.OnEnter();
		TimeController.StartNow(UIStartScreen.ShowStartScreenAsync(prefabScreen));
		UIStartScreen.SetStatusText(LocalizationKeys.Loading.Localized());
	}
}
