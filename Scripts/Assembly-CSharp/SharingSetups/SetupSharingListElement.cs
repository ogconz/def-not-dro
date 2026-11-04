using System;
using Binding;
using Binding.Base;
using DB.Meta;
using GameOverlay;
using PrefsModels;
using UnityEngine;
using UnityEngine.U2D;

namespace SharingSetups
{
	public class SetupSharingListElement : BaseBindingBehaviourTarget
	{
		[SerializeField]
		private RectTransform m_rectTransform;

		[SerializeField]
		private SpriteAtlas m_setupsAtlas;

		private readonly BoolProperty m_selected = new BoolProperty();

		private readonly BoolProperty m_downloaded = new BoolProperty();

		private readonly BoolProperty m_isOfficial = new BoolProperty();

		private readonly BoolProperty m_showIcons = new BoolProperty();

		private readonly BoolProperty m_isFriend = new BoolProperty();

		private readonly BoolProperty m_myCustom = new BoolProperty();

		private readonly BoolProperty m_stockEngine = new BoolProperty();

		private readonly StringProperty m_customSetupName = new StringProperty();

		private readonly SpriteProperty m_setupIcon = new SpriteProperty();

		private readonly SpriteProperty m_setupEngineIcon = new SpriteProperty();

		public UISetupsSharingContext context { get; private set; }

		public ProfilesInfo.Profile profileData { get; private set; }

		public string elementName => m_customSetupName.value;

		public string code { get; private set; }

		public string publisherPlayerId { get; private set; }

		public string publisherNickname { get; private set; }

		public DateTime createDate { get; private set; }

		public int downloadCount { get; private set; }

		public float rating { get; private set; }

		public CustomProfile customProfile { get; private set; }

		public string customSetupId { get; private set; }

		public int engineId { get; private set; }

		public int profileId { get; private set; }

		public bool isOfficial => m_isOfficial.value;

		public bool isDownloaded => !string.IsNullOrEmpty(code);

		public int deviceType { get; private set; }

		public RectTransform rectTransform => m_rectTransform;

		public void SetContext(UISetupsSharingContext setupsSharingContext)
		{
			context = setupsSharingContext;
		}

		public void SetData(SetupSharingListElementData elementData)
		{
			if (elementData == null)
			{
				return;
			}
			code = elementData.code;
			publisherPlayerId = elementData.publisherPlayerId;
			publisherNickname = elementData.publisherNickname;
			createDate = elementData.createDate;
			downloadCount = elementData.downloadCount;
			rating = elementData.rating;
			customProfile = elementData.customProfile;
			customSetupId = elementData.customSetupId;
			engineId = elementData.engineId;
			profileId = elementData.profileId;
			deviceType = elementData.deviceType;
			m_customSetupName.value = Overlay.filterTextManager.GetFilteredCachedText(TextFilterType.Roomname, UserPlatform.Id.Unknown, elementData.customSetupName, delegate(string filtered)
			{
				if (m_customSetupName.value != filtered)
				{
					m_customSetupName.value = filtered;
				}
			});
			PlayerCarProfile profileById = PlayerCarProfileMetaExtensions.GetProfileById(profileId);
			string text = ((profileById != null) ? ("ico_" + profileById.name) : ("ico_" + elementData.setup));
			m_setupIcon.value = m_setupsAtlas.GetSprite(text.ToLower());
			m_stockEngine.value = elementData.engineId <= 0;
			if (!m_stockEngine.value)
			{
				m_setupEngineIcon.value = SwapsAsset.LoadAndInitDB().Find(elementData.engineId).logo;
			}
			else
			{
				m_setupEngineIcon.value = null;
			}
			m_downloaded.value = elementData.isLocalCustomSetup && !string.IsNullOrEmpty(elementData.code);
			m_isOfficial.value = elementData.isOfficial;
			m_myCustom.value = PlayerId.TryParse(publisherPlayerId, out var playerId) && playerId.Equals(Overlay.instance.player.id);
			if (m_myCustom.value && m_downloaded.value)
			{
				m_downloaded.value = false;
			}
			m_isFriend.value = Overlay.instance.IsFriend(playerId);
			m_showIcons.value = m_downloaded.value || m_myCustom.value;
			profileData = elementData.profileData;
		}

		public void SetFocus(bool isFocused)
		{
			m_selected.value = isFocused;
		}

		public void OnClick()
		{
			context.OnClickSharingElement(this);
		}

		public void UpdateFrom(SetupSharingListElementData data, bool onlyData = false)
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

		public void UpdateDownloadLabelStatus(bool download)
		{
			m_downloaded.value = download;
			m_showIcons.value = m_downloaded.value || m_myCustom.value;
		}

		public void UpdateDownloadIconStatus(bool status)
		{
			m_showIcons.value = status;
		}
	}
}
