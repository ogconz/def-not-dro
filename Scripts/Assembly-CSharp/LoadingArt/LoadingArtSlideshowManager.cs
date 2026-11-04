using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using Utils.ColorExtensions;

namespace LoadingArt
{
	public class LoadingArtSlideshowManager : IDisposable
	{
		private const int FullOpaqueAlphas = 1;

		private const int FullTransparentAlpha = 0;

		private Image m_imageComponent;

		private Image m_imageToFadeComponent;

		private LoadingArtConfigSO m_loadingArtAsset;

		private List<LoadingArtAssetReference> m_loadingArtSprites = new List<LoadingArtAssetReference>();

		private float m_timeoutChange;

		private float m_timer;

		private bool m_isInitialized;

		private float m_timeToFade;

		private int m_currentIndex;

		private bool m_artSpriteIsLoading;

		public LoadingArtSlideshowManager(Image imageComponent, Image imageToFadeComponent)
		{
			m_imageComponent = imageComponent;
			m_imageToFadeComponent = imageToFadeComponent;
		}

		public void Initialize(LoadingArtConfigSO config)
		{
			m_loadingArtAsset = config;
			m_isInitialized = TryFillArtList();
			ShuffleRefList();
			InitializeTimeouts();
			SetInitialArt();
		}

		public void Update()
		{
			if (!m_isInitialized)
			{
				return;
			}
			if (m_timer >= m_timeoutChange && !m_artSpriteIsLoading)
			{
				m_artSpriteIsLoading = true;
				ChangeArtAsync().ContinueWith(delegate
				{
					m_timer = 0f;
					m_artSpriteIsLoading = false;
				});
			}
			m_timer += Time.deltaTime;
		}

		public void Dispose()
		{
			foreach (LoadingArtAssetReference loadingArtSprite in m_loadingArtSprites)
			{
				if (loadingArtSprite.loaded)
				{
					loadingArtSprite.Unload();
				}
			}
		}

		private void ShuffleRefList()
		{
			for (int num = m_loadingArtSprites.Count - 1; num >= 0; num--)
			{
				int num2 = UnityEngine.Random.Range(0, m_loadingArtSprites.Count);
				List<LoadingArtAssetReference> loadingArtSprites = m_loadingArtSprites;
				int index = num2;
				List<LoadingArtAssetReference> loadingArtSprites2 = m_loadingArtSprites;
				int index2 = num;
				LoadingArtAssetReference loadingArtAssetReference = m_loadingArtSprites[num];
				LoadingArtAssetReference loadingArtAssetReference2 = m_loadingArtSprites[num2];
				LoadingArtAssetReference loadingArtAssetReference3 = (loadingArtSprites[index] = loadingArtAssetReference);
				loadingArtAssetReference3 = (loadingArtSprites2[index2] = loadingArtAssetReference2);
			}
		}

		private bool TryFillArtList()
		{
			foreach (AssetReferenceSprite loadingArtSpritesReference in m_loadingArtAsset.loadingArtSpritesReferences)
			{
				m_loadingArtSprites.Add(new LoadingArtAssetReference(loadingArtSpritesReference));
			}
			return m_loadingArtSprites.Count > 0;
		}

		private void InitializeTimeouts()
		{
			m_timeoutChange = m_loadingArtAsset.timeoutChangePC;
			m_timeToFade = m_loadingArtAsset.timeToFade;
		}

		private void SetInitialArt()
		{
			ChangeArt(isFirstArt: true);
		}

		private async UniTask ChangeArtAsync()
		{
			PrepareFadeImageComponent();
			Sprite sprite = await m_loadingArtSprites[m_currentIndex].LoadAsync();
			if (sprite != null)
			{
				m_imageComponent.sprite = sprite;
			}
			IncCurrIndexArtSelection();
			FadeImage();
		}

		private void ChangeArt(bool isFirstArt)
		{
			if (!isFirstArt)
			{
				PrepareFadeImageComponent();
			}
			Sprite sprite = m_loadingArtSprites[m_currentIndex].Load();
			if (sprite != null)
			{
				m_imageComponent.sprite = sprite;
			}
			IncCurrIndexArtSelection();
			FadeImage();
		}

		private void PrepareFadeImageComponent()
		{
			m_imageToFadeComponent.sprite = m_imageComponent.sprite;
			m_imageToFadeComponent.SetAlpha(1f);
		}

		private void IncCurrIndexArtSelection()
		{
			m_currentIndex++;
			ResetCurrentIndexIfShownAllArt();
		}

		private void ResetCurrentIndexIfShownAllArt()
		{
			if (m_currentIndex >= m_loadingArtSprites.Count)
			{
				m_currentIndex = 0;
			}
		}

		private void FadeImage()
		{
			m_imageToFadeComponent.DOFade(0f, m_timeToFade);
		}
	}
}
