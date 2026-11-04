using System;
using System.Collections;
using DI;
using FMOD.Studio;
using GameInput;
using GameOverlay;
using LoadingArt;
using PrefsModels;
using Rewired;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using UnityEngine.Video;

public class UIStartScreen : MonoBehaviour
{
	public enum ScreenState
	{
		Intro = 0,
		Loading = 1
	}

	private const string PlaybackTimerExpired = "Playback timer expired";

	private const string AsyncLoadFailed = "Async load operation failed";

	private const float FadeOutTimeLoadingScreen = 1f;

	private const float RemainingPlaybackTime = 25f;

	private const float TransparentAlpha = 0f;

	private const float OpaqueAlpha = 1f;

	private static string s_modsReport = string.Empty;

	private static float s_videoPlayerInitTime;

	private static UIStartScreen s_instance;

	private static bool s_videoStarted;

	private static bool s_introSkipped;

	private static bool s_fading;

	private static bool s_needShowEULA;

	public TextMeshProUGUI labelStatus;

	public TextMeshProUGUI modList;

	[SerializeField]
	private AssetReference m_1080p60fpsClip;

	[SerializeField]
	private AssetReference m_1440p60fpsClip;

	[SerializeField]
	private GameObject m_mainLoadingScreenContainer;

	[SerializeField]
	private RawImage m_introWaitSpinnerRawImage;

	[SerializeField]
	private VideoPlayer m_introVideoPlayer;

	[SerializeField]
	private Image m_loadingScreenImageComponent;

	[SerializeField]
	private Image m_loadingScreenImageToFadeComponent;

	private LoadingArtSlideshowManager m_loadingArtSlideshowManager;

	private LoadingSlideshowConfigLoader m_loadingSlideshowActivator;

	private AsyncOperationHandle<VideoClip> m_videoClipLoadHandle;

	private CanvasGroup m_loadingScreenCanvasGroup;

	private EventInstance m_fmodEventInstance;

	private Coroutine m_fadeCoroutine;

	private RawImage m_rawImageIntro;

	private bool m_errorReceived;

	private bool m_isScreenPrefabInstantiated;

	private bool m_isSlideshowStarted;

	public static bool isIntroComplete { get; private set; }

	public static bool IsShown => s_instance != null;

	public static bool isActive
	{
		get
		{
			if (s_instance != null)
			{
				return s_instance.isActiveAndEnabled;
			}
			return false;
		}
	}

	private bool isPlaybackTimerExpired => Time.time - s_videoPlayerInitTime > 25f;

	public static string modsReport
	{
		set
		{
			s_modsReport = value;
			if (s_instance != null)
			{
				s_instance.SetModListText(s_modsReport);
			}
		}
	}

	private void Awake()
	{
		s_instance = this;
		m_loadingScreenCanvasGroup = GetComponentInChildren<CanvasGroup>(includeInactive: true);
		m_rawImageIntro = m_introVideoPlayer.GetComponent<RawImage>();
		Overlay.OnReady = (System.Action)Delegate.Combine(Overlay.OnReady, new System.Action(OnOverlayReady));
		SetModListText(s_modsReport);
		InitializeLoadingArtSlideshowManager();
	}

	private void Update()
	{
		if (m_isSlideshowStarted)
		{
			m_loadingArtSlideshowManager.Update();
		}
	}

	private void OnOverlayReady()
	{
		GlobalModel globals = DependencyInjector.Resolve<GamePrefs>().globals;
		s_needShowEULA = globals != null && !globals.eulaAccepted;
	}

	public static IEnumerator ShowStartScreenAsync(GameObject prefabScreen, bool needShowIntroVideo = false)
	{
		if (!(s_instance != null) || s_fading)
		{
			yield return InstantiateStartScreenPrefab(prefabScreen);
			if (needShowIntroVideo)
			{
				s_instance.SwitchStartScreenTo(ScreenState.Intro);
				s_instance.LoadVideoClipAsync();
				yield return s_instance.TryInvokeVideoPlayerOnReadyComponents();
			}
			else
			{
				s_instance.SwitchStartScreenTo(ScreenState.Loading);
				s_instance.StartSlideShow();
			}
		}
	}

	private void InitializeLoadingArtSlideshowManager()
	{
		m_loadingArtSlideshowManager = new LoadingArtSlideshowManager(m_loadingScreenImageComponent, m_loadingScreenImageToFadeComponent);
		m_loadingSlideshowActivator = new LoadingSlideshowConfigLoader();
		m_loadingArtSlideshowManager.Initialize(m_loadingSlideshowActivator.LoadSptiresByScreenTarget(LoadingScreenTarget.Start));
	}

	private void LoadVideoClipAsync()
	{
		AssetReference targetVideoAssetReferenceByHardware = GetTargetVideoAssetReferenceByHardware();
		s_videoPlayerInitTime = Time.time;
		m_videoClipLoadHandle = targetVideoAssetReferenceByHardware.LoadAssetAsync<VideoClip>();
		m_videoClipLoadHandle.Completed += delegate(AsyncOperationHandle<VideoClip> clipLoadHandler)
		{
			if (clipLoadHandler.IsValid() && clipLoadHandler.Result != null)
			{
				VideoClip result = clipLoadHandler.Result;
				m_introVideoPlayer.clip = result;
				RenderTexture renderTexture = new RenderTexture((int)result.width, (int)result.height, 0);
				m_rawImageIntro.texture = renderTexture;
				m_introVideoPlayer.targetTexture = renderTexture;
			}
			else
			{
				m_errorReceived = true;
			}
		};
	}

	private AssetReference GetTargetVideoAssetReferenceByHardware()
	{
		if (!GameManager.IsLinuxOsPlatform())
		{
			return m_1440p60fpsClip;
		}
		return m_1080p60fpsClip;
	}

	private IEnumerator TryInvokeVideoPlayerOnReadyComponents()
	{
		while (m_introVideoPlayer.clip == null || m_introVideoPlayer.targetTexture == null || m_rawImageIntro.texture == null)
		{
			if (isPlaybackTimerExpired || m_errorReceived)
			{
				OnErrorReceived(null, isPlaybackTimerExpired ? "Playback timer expired" : "Async load operation failed");
				SwitchStartScreenTo(ScreenState.Loading);
				ResourcesWrapper.UnloadAsset(m_videoClipLoadHandle);
				yield break;
			}
			yield return null;
		}
		yield return ShowIntroVideo();
	}

	private static IEnumerator InstantiateStartScreenPrefab(GameObject prefabScreen)
	{
		while (s_fading)
		{
			yield return null;
		}
		if (s_instance == null || !s_instance.m_isScreenPrefabInstantiated)
		{
			States.onStateActivate -= OnStateAcitvate;
			States.onStateActivate += OnStateAcitvate;
			UnityEngine.Object.Instantiate(prefabScreen, BaseGameState.FindUGUIRoot());
			s_instance.m_isScreenPrefabInstantiated = true;
			while (s_instance == null)
			{
				yield return null;
			}
		}
	}

	private IEnumerator FadeInIntro()
	{
		while (!s_videoStarted)
		{
			yield return null;
		}
		while (m_rawImageIntro.color.a < 1f && !isIntroComplete && !s_introSkipped && !m_errorReceived)
		{
			AddAlphaToRawImage(m_rawImageIntro, Time.deltaTime);
			AddAlphaToRawImage(m_introWaitSpinnerRawImage, 0f - Time.deltaTime);
			yield return null;
		}
	}

	private IEnumerator ShowIntroVideo()
	{
		TimeController.StartNow(FadeInIntro());
		m_introVideoPlayer.loopPointReached += OnLoopPointReachedByPlayer;
		m_introVideoPlayer.errorReceived += OnErrorReceived;
		m_introVideoPlayer.Prepare();
		WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();
		while (!m_introVideoPlayer.isPrepared && !m_errorReceived)
		{
			if (isPlaybackTimerExpired)
			{
				OnErrorReceived(null, "Playback timer expired");
			}
			yield return endOfFrame;
		}
		if (!m_errorReceived)
		{
			yield return ProcessPlayIntro();
		}
		m_introVideoPlayer.errorReceived -= OnErrorReceived;
		m_introVideoPlayer.loopPointReached -= OnLoopPointReachedByPlayer;
		SwitchStartScreenTo(ScreenState.Loading);
		ResourcesWrapper.UnloadAsset(m_videoClipLoadHandle);
	}

	private void OnErrorReceived(VideoPlayer source, string errorMessage)
	{
		m_errorReceived = true;
		GameLog.Print(AlertLevel.Error, "Video", "Error while playing intro: " + errorMessage);
	}

	private IEnumerator ProcessPlayIntro()
	{
		m_introVideoPlayer.Play();
		m_fmodEventInstance = SoundController.Play2DSound(SoundSample.Intro);
		s_videoStarted = true;
		while (!isIntroComplete && !m_errorReceived)
		{
			if (isPlaybackTimerExpired)
			{
				OnErrorReceived(null, "Playback timer expired");
			}
			if (ReInput.controllers.GetAnyButtonChanged())
			{
				s_introSkipped = true;
				yield return StopIntroVideoPlayerAsyncIfNeed(needUseFade: true);
				break;
			}
			yield return null;
		}
	}

	private void OnLoopPointReachedByPlayer(VideoPlayer videoPlayer)
	{
		TimeController.StartNow(StopIntroVideoPlayerAsyncIfNeed(needUseFade: false));
	}

	private IEnumerator StopIntroVideoPlayerAsyncIfNeed(bool needUseFade)
	{
		while (needUseFade && m_rawImageIntro.color.a > 0f)
		{
			AddAlphaToRawImage(m_rawImageIntro, (0f - Time.deltaTime) * 2f);
			AddAlphaToRawImage(m_introWaitSpinnerRawImage, Time.deltaTime * 2f);
			yield return null;
		}
		m_fmodEventInstance.stop(STOP_MODE.ALLOWFADEOUT);
		m_introVideoPlayer.Stop();
		isIntroComplete = true;
		StartSlideShow();
	}

	private void AddAlphaToRawImage(RawImage rawImage, float delta)
	{
		Color color = rawImage.color;
		color.a += delta;
		rawImage.color = color;
	}

	private void SwitchStartScreenTo(ScreenState targetState)
	{
		bool flag = targetState == ScreenState.Intro;
		m_introVideoPlayer.gameObject.SetActive(flag);
		m_mainLoadingScreenContainer.SetActive(!flag);
	}

	private static void OnStateAcitvate(BaseState state)
	{
		bool flag = state is LoadRaceState;
		if (!flag && !(state is GarageGUIState))
		{
			return;
		}
		States.onStateActivate -= OnStateAcitvate;
		if (flag)
		{
			if (s_instance != null)
			{
				UnityEngine.Object.Destroy(s_instance.gameObject);
			}
		}
		else
		{
			s_instance.StopSlideshow();
			Fade();
		}
	}

	private void SetModListText(string text)
	{
		if (!(modList == null))
		{
			modList.text = text;
		}
	}

	private void OnDestroy()
	{
		s_instance = null;
		m_loadingArtSlideshowManager.Dispose();
	}

	public static void SetStatusText(string text)
	{
		if (s_instance != null && s_instance.labelStatus != null)
		{
			s_instance.labelStatus.text = text.ToUpper();
		}
	}

	private static void Fade()
	{
		if (s_instance != null)
		{
			s_instance.StartFade();
		}
		else
		{
			Debug.LogWarning("UIStartScreen >>> m_instance is null");
		}
	}

	private void StartFade()
	{
		if (m_fadeCoroutine != null)
		{
			TimeController.StopNow(m_fadeCoroutine);
		}
		m_fadeCoroutine = TimeController.StartNow(FadeScreen());
	}

	private IEnumerator FadeScreen()
	{
		GameInput.InputManager.LockInput(this);
		s_fading = true;
		yield return null;
		yield return null;
		BaseState current = States.current;
		if (current is GarageGUIState garageState)
		{
			while (!garageState.IsRaceCarLoaded() && States.current is GarageGUIState)
			{
				yield return null;
			}
		}
		if (!StartState.m_saveEnded)
		{
			SetStatusText(LocalizationKeys.Loading.Localized());
			while (!StartState.m_saveEnded)
			{
				yield return null;
			}
		}
		float start = Time.time;
		float elapsed = 0f;
		if (m_loadingScreenCanvasGroup != null && !s_needShowEULA)
		{
			while (elapsed < 1f)
			{
				elapsed = Time.time - start;
				float t = Mathf.Clamp01(elapsed / 1f);
				m_loadingScreenCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
				yield return null;
			}
		}
		GameInput.InputManager.ResetLockInput(this);
		UnityEngine.Object.Destroy(base.gameObject);
		s_fading = false;
		m_fadeCoroutine = null;
	}

	private void StartSlideShow()
	{
		m_isSlideshowStarted = true;
	}

	private void StopSlideshow()
	{
		m_isSlideshowStarted = false;
	}
}
