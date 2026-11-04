using System;
using System.Collections;
using DG.Tweening;
using DI;
using FMOD.Studio;
using JukeBox;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace UI.PromotionWidget
{
	public class Widget : MonoBehaviour
	{
		private const float EnabledBackgroundAlpha = 0.95f;

		private const float DisabledBackgroundAlpha = 0f;

		private const float EnabledSoundVolume = 1f;

		private const float DisabledSoundVolume = 0f;

		private const float ZoomDuration = 0.8f;

		private const int PromotionAppId = 1826420;

		private const float PointerEnterTimeoutInSec = 0.75f;

		private const float HoverFilledValue = 1f;

		private const float HoverEmptyValue = 0f;

		private readonly Vector3 m_minSize = new Vector3(1127f, 635f);

		private readonly Vector3 m_maxSize = new Vector3(451f, 254f);

		[SerializeField]
		private Image m_backgroundImage;

		[SerializeField]
		private Image m_timeoutFiller;

		[SerializeField]
		private VideoPlayer m_videoContent;

		private EventInstance m_promotionEventInstance;

		private RectTransform m_widgetRect;

		private GamePrefs m_prefs;

		private Func<bool> m_raycastRestrictionCallback;

		private float m_promotionCurrentVolume;

		private bool m_isActivated;

		private Coroutine m_pointerEnterTimeout;

		private void Start()
		{
			m_widgetRect = GetComponent<RectTransform>();
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
			m_timeoutFiller.fillAmount = 0f;
		}

		public void SetRaycastRestrictionCallback(Func<bool> raycastRestrictionCallback)
		{
			m_raycastRestrictionCallback = raycastRestrictionCallback;
		}

		public void StartPlayContent()
		{
			InitializeVideoContent();
			InitializeAudio();
		}

		public void StopPlayContent()
		{
			ReleaseVideoContent();
			ReleaseAudio();
			OnPointerOverUI();
		}

		private void OnVideoContentPlayHandler(VideoPlayer source)
		{
			InitializeAudio();
		}

		private void InitializeVideoContent()
		{
			RenderTexture.active = m_videoContent.targetTexture;
			GL.Clear(clearDepth: true, clearColor: true, Color.black);
			RenderTexture.active = null;
			m_videoContent.started += OnVideoContentPlayHandler;
			m_videoContent.loopPointReached += OnVideoContentPlayHandler;
			m_videoContent.Play();
		}

		private void ReleaseVideoContent()
		{
			m_videoContent.Stop();
			m_videoContent.started -= OnVideoContentPlayHandler;
			m_videoContent.loopPointReached -= OnVideoContentPlayHandler;
			m_videoContent.targetTexture.Release();
		}

		private void InitializeAudio()
		{
			ReleaseAudio();
			m_promotionEventInstance = SoundController.Play2DSound(SoundSample.Promotion);
			m_promotionEventInstance.setVolume(m_promotionCurrentVolume);
		}

		private void ReleaseAudio()
		{
			if (m_promotionEventInstance.isValid())
			{
				m_promotionEventInstance.stop(STOP_MODE.IMMEDIATE);
				m_promotionEventInstance.release();
			}
		}

		public void OnPointerEnterUI()
		{
			if (CanRaycast() && (!m_isActivated || m_pointerEnterTimeout == null))
			{
				m_pointerEnterTimeout = TimeController.StartNow(TimeoutCoroutine(PlayWidgetAnimation));
				m_isActivated = true;
			}
		}

		private IEnumerator TimeoutCoroutine(Action playAnimation)
		{
			float elapsedTime = 0f;
			while (elapsedTime < 0.75f)
			{
				float fillAmount = Mathf.Lerp(0f, 1f, elapsedTime / 0.75f);
				m_timeoutFiller.fillAmount = fillAmount;
				elapsedTime += Time.deltaTime;
				yield return null;
			}
			playAnimation.SafeCall();
		}

		private void PlayWidgetAnimation()
		{
			m_timeoutFiller.fillAmount = 1f;
			CreateBackgroundFadeAnimation(0.95f, 0.8f);
			CreateChangeSizeAnimation(Ease.OutExpo, m_minSize, 0.8f);
			PlayPromotionAudioFade(Ease.InSine, 1f, 0.8f);
			TimeController.StartNow(PlayGameSoundFade(Ease.InOutCirc, enableMusic: false, 0.8f));
		}

		public void OnPointerOverUI()
		{
			if (m_isActivated)
			{
				TimeController.StopNow(m_pointerEnterTimeout);
				m_pointerEnterTimeout = null;
				m_timeoutFiller.fillAmount = 0f;
				m_isActivated = false;
				CreateChangeSizeAnimation(Ease.InOutCirc, m_maxSize, 0.8f);
				CreateBackgroundFadeAnimation(0f, 0.8f);
				PlayPromotionAudioFade(Ease.InOutCirc, 0f, 0.8f);
				TimeController.StartNow(PlayGameSoundFade(Ease.InOutCirc, enableMusic: true, 0.8f));
			}
		}

		public void OnPointerClickUI()
		{
			if (CanRaycast())
			{
				SteamFriends.OpenStoreOverlay(1826420);
				OnPointerOverUI();
			}
		}

		private void PlayPromotionAudioFade(Ease easeFunc, float targetValue, float duration)
		{
			float endValue = targetValue * m_prefs.globals.masterVolume;
			DOTween.To(() => m_promotionCurrentVolume, delegate(float x)
			{
				m_promotionCurrentVolume = x;
				m_promotionEventInstance.setVolume(m_promotionCurrentVolume);
			}, endValue, duration).SetEase(easeFunc);
		}

		private IEnumerator PlayGameSoundFade(Ease easeFunc, bool enableMusic, float duration)
		{
			while (!CanStartAudio())
			{
				yield return null;
			}
			try
			{
				JukeBox.Controller.ManualUpdate(enable: true);
				ChannelNode node = JukeBox.Controller.GetActiveNode();
				float endValue = (enableMusic ? m_prefs.globals.musicVolume : 0f);
				float currentMusicVolume = node.ChannelVolume;
				DOTween.To(() => currentMusicVolume, delegate(float x)
				{
					currentMusicVolume = x;
					node.ChannelVolume = currentMusicVolume;
				}, endValue, duration).SetEase(easeFunc).OnComplete(delegate
				{
					JukeBox.Controller.ManualUpdate(!enableMusic);
				});
				float endValue2 = (enableMusic ? m_prefs.globals.ambienceVolume : 0f);
				SoundController.GetSoundAmbienceVolume(out var currentAmbienceVolume);
				DOTween.To(() => currentAmbienceVolume, delegate(float x)
				{
					currentAmbienceVolume = x;
					SoundController.SetSoundAmbienceVolume(currentAmbienceVolume);
				}, endValue2, duration).SetEase(easeFunc).OnComplete(delegate
				{
					JukeBox.Controller.ManualUpdate(!enableMusic);
				});
			}
			catch (Exception)
			{
				JukeBox.Controller.ManualUpdate(enable: false);
			}
		}

		private void CreateBackgroundFadeAnimation(float tagteAlpha, float duration)
		{
			DOTweenModuleUI.DOColor(endValue: new Color(0f, 0f, 0f, tagteAlpha), target: m_backgroundImage, duration: duration).SetEase(Ease.InOutQuad).SetUpdate(isIndependentUpdate: true)
				.Play();
		}

		private void CreateChangeSizeAnimation(Ease easeFunc, Vector3 targetSize, float duration)
		{
			m_widgetRect.DOKill();
			m_widgetRect.DOSizeDelta(targetSize, duration).SetEase(easeFunc).SetUpdate(isIndependentUpdate: true)
				.Play();
		}

		private bool CanStartAudio()
		{
			return JukeBox.Controller.GetActiveNode() != null;
		}

		private bool CanRaycast()
		{
			if (m_raycastRestrictionCallback != null)
			{
				return !m_raycastRestrictionCallback();
			}
			return false;
		}
	}
}
