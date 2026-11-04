using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CodeStage.AntiCheat.Detectors
{
	[AddComponentMenu("")]
	public abstract class ActDetectorBase : MonoBehaviour
	{
		protected const string CONTAINER_NAME = "Anti-Cheat Toolkit Detectors";

		protected const string MENU_PATH = "GameObject/Create Other/Code Stage/Anti-Cheat Toolkit/";

		[Tooltip("Automatically dispose Detector after firing callback.")]
		public bool autoDispose = true;

		[Tooltip("Detector will survive new level (scene) load if checked.")]
		public bool keepAlive = true;

		protected static GameObject detectorsContainer;

		protected Action onDetection;

		private bool inited;

		private void Start()
		{
			inited = true;
		}

		protected virtual bool Init(ActDetectorBase instance, string detectorName)
		{
			if (instance != null && instance != this && instance.keepAlive)
			{
				UnityEngine.Object.Destroy(this);
				return false;
			}
			UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
			return true;
		}

		private void OnDisable()
		{
			GameEvents.OnGameEngineStop -= new GameEventHandler<GameEvents.EventInfo>(OnGameEngineStop);
			SceneManager.sceneLoaded -= OnLevelFinishedLoading;
			if (inited)
			{
				PauseDetector();
			}
		}

		private void OnEnable()
		{
			GameEvents.OnGameEngineStop += new GameEventHandler<GameEvents.EventInfo>(OnGameEngineStop);
			SceneManager.sceneLoaded += OnLevelFinishedLoading;
			if (inited && onDetection != null)
			{
				ResumeDetector();
			}
		}

		private void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
		{
			if (inited && !keepAlive)
			{
				DisposeInternal();
			}
		}

		private void OnGameEngineStop(GameEvents.EventInfo e)
		{
			DisposeInternal();
		}

		protected abstract void StopDetectionInternal();

		protected abstract void PauseDetector();

		protected abstract void ResumeDetector();

		protected virtual void DisposeInternal()
		{
			StopDetectionInternal();
			UnityEngine.Object.Destroy(this);
		}

		protected virtual void OnDestroy()
		{
			if (base.transform.childCount == 0 && GetComponentsInChildren<Component>().Length <= 2)
			{
				UnityEngine.Object.Destroy(base.gameObject);
			}
			else if (base.name == "Anti-Cheat Toolkit Detectors" && GetComponentsInChildren<ActDetectorBase>().Length <= 1)
			{
				UnityEngine.Object.Destroy(base.gameObject);
			}
		}
	}
}
