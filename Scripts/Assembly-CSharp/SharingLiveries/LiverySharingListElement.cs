using System;
using System.Collections;
using Binding;
using Binding.Base;
using GameOverlay;
using UnityEngine;

namespace SharingLiveries
{
	public class LiverySharingListElement : BaseBindingBehaviourTarget
	{
		private const int PREVIEW_WIDTH = 220;

		private const int PREVIEW_HEIGHT = 124;

		private static readonly (int width, int height) m_previewSize = (width: 220, height: 124);

		private Coroutine m_setDataCoroutine;

		private LiverySharingListElementData m_elementData;

		[SerializeField]
		private RectTransform m_rectTransform;

		private readonly BoolProperty m_selected = new BoolProperty();

		private readonly BoolProperty m_downloaded = new BoolProperty();

		private readonly BoolProperty m_isOfficial = new BoolProperty();

		private readonly BoolProperty m_showIcons = new BoolProperty();

		private readonly BoolProperty m_isFriend = new BoolProperty();

		private readonly BoolProperty m_myCustom = new BoolProperty();

		private readonly BoolProperty m_isPrivate = new BoolProperty();

		private readonly StringProperty m_liveryName = new StringProperty();

		private readonly StringProperty m_layersCount = new StringProperty();

		private readonly FloatProperty m_ratingElement = new FloatProperty();

		private readonly TextureProperty m_preview = new TextureProperty();

		public UILiveriesSharingContext context { get; private set; }

		public string elementName => m_liveryName.value;

		public string liveryId { get; private set; }

		public string code { get; private set; }

		public string publisherPlayerId { get; private set; }

		public string publisherNickname { get; private set; }

		public DateTime createDate { get; private set; }

		public int downloadCount { get; private set; }

		public float rating { get; private set; }

		public bool isOfficial => m_isOfficial.value;

		public bool isDownloaded => !string.IsNullOrEmpty(code);

		public CarVilylsInfo.VinylPreset preset { get; private set; }

		public Texture2D preview { get; private set; }

		public RectTransform rectTransform => m_rectTransform;

		public void SetContext(UILiveriesSharingContext liveriesSharingContext)
		{
			context = liveriesSharingContext;
		}

		public void StopSetDataCoroutine()
		{
			if (m_setDataCoroutine != null)
			{
				TimeController.StopNow(m_setDataCoroutine);
				m_setDataCoroutine = null;
			}
			m_elementData?.StopFetchCorourine();
		}

		private void SetData(LiverySharingListElementData elementData)
		{
			ResetElement();
			m_setDataCoroutine = TimeController.StartNow(SetDataAsync(elementData));
			IEnumerator SetDataAsync(LiverySharingListElementData liverySharingListElementData)
			{
				if (liverySharingListElementData != null)
				{
					m_elementData = liverySharingListElementData;
					bool previewRequestDone = false;
					byte[] previewData = null;
					yield return liverySharingListElementData.FetchResourceCouroutine(liverySharingListElementData.previewDataUrl, delegate(byte[] data)
					{
						previewData = data;
						previewRequestDone = true;
					});
					yield return new WaitUntil(() => previewRequestDone);
					code = liverySharingListElementData.code;
					publisherPlayerId = liverySharingListElementData.publisherPlayerId;
					publisherNickname = liverySharingListElementData.publisherNickname;
					createDate = liverySharingListElementData.createDate;
					downloadCount = liverySharingListElementData.downloadCount;
					rating = liverySharingListElementData.rating;
					if (preview == null)
					{
						preview = new Texture2D(m_previewSize.width, m_previewSize.height, TextureFormat.RGB24, mipChain: false);
					}
					if (previewData != null)
					{
						preview.LoadRawTextureData(previewData);
						preview.Apply();
					}
					m_preview.value = preview;
					m_layersCount.value = LocalizationKeys.vinyls_layers_count.LocalizedFormat(liverySharingListElementData.layersCount);
					m_liveryName.value = Overlay.filterTextManager.GetFilteredCachedText(TextFilterType.Roomname, UserPlatform.Id.Unknown, liverySharingListElementData.liveryName, delegate(string filtered)
					{
						if (m_liveryName.value != filtered)
						{
							m_liveryName.value = filtered;
						}
					});
					liveryId = liverySharingListElementData.liveryId;
					m_downloaded.value = liverySharingListElementData.isLocalLivery && !string.IsNullOrEmpty(liverySharingListElementData.code);
					m_isOfficial.value = liverySharingListElementData.isOfficial;
					m_isPrivate.value = liverySharingListElementData.isPrivate;
					m_ratingElement.value = liverySharingListElementData.rating;
					m_myCustom.value = PlayerId.TryParse(publisherPlayerId, out var playerId) && playerId.Equals(Overlay.instance.player.id);
					if (m_myCustom.value && m_downloaded.value)
					{
						m_downloaded.value = false;
					}
					m_isFriend.value = Overlay.instance.IsFriend(playerId);
					m_showIcons.value = m_downloaded.value || m_myCustom.value;
				}
			}
		}

		public void SetPreset(CarVilylsInfo.VinylPreset preset)
		{
			this.preset = preset;
		}

		public void SetFocus(bool isFocused)
		{
			m_selected.value = isFocused;
		}

		public void OnClick()
		{
			context.OnClickSharingElement(this);
		}

		public void UpdateFrom(LiverySharingListElementData data, bool onlyData = false)
		{
			if (!onlyData)
			{
				if (base.gameObject != null)
				{
					base.gameObject.SetActive(value: true);
				}
				SetFocus(isFocused: false);
			}
			SetData(data);
		}

		public void UpdateDownloadLabelStatus(bool downloaded)
		{
			m_downloaded.value = !m_myCustom.value && downloaded;
			m_showIcons.value = m_downloaded.value || m_myCustom.value;
		}

		private void ResetElement()
		{
			UnityEngine.Object.Destroy(preview);
			preview = null;
			m_preview.value = null;
			preset = null;
			m_ratingElement.value = 0f;
			m_isFriend.value = false;
			m_showIcons.value = false;
			m_isOfficial.value = false;
			m_liveryName.value = string.Empty;
		}
	}
}
