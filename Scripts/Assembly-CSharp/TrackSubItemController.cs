using Binding;
using Binding.Base;
using UnityEngine;
using UnityEngine.UI;

public class TrackSubItemController : BaseBindingBehaviourTarget
{
	private const float MAX_TEXTS_GAP = 120f;

	private const float TEXT_MOVE_SPEED = 70f;

	private const float ICON_ALPHA_FADE = 0.25f;

	private const int MAX_TRACK_NAME_LENGTH = 24;

	[SerializeField]
	private CanvasGroup m_iconCanvasGroup;

	[SerializeField]
	private Image m_loadingContentProgress;

	[SerializeField]
	private RectTransform m_movingTextsParent;

	public TextureProperty iconMap = new TextureProperty();

	public Property<string> nameMap = new StringProperty();

	public Property<string> dlcName = new StringProperty();

	public Property<Sprite> cupSprite = new SpriteProperty();

	public Property<string> goldLimit = new StringProperty();

	public Property<string> silverLimit = new StringProperty();

	public Property<string> bronzeLimit = new StringProperty();

	public Property<string> bestResult = new StringProperty();

	public Property<int> coinsValue = new IntProperty();

	public Property<bool> locked = new BoolProperty();

	public Property<bool> lockedByDLC = new BoolProperty();

	public Property<bool> lockedByLevel = new BoolProperty();

	public Property<string> text = new StringProperty();

	public Property<int> cupIndex = new IntProperty();

	public Property<bool> visibleElement = new BoolProperty();

	public Property<bool> isReverse = new BoolProperty();

	public Property<bool> isLoadingWebIcon = new BoolProperty();

	public Property<bool> isExternal = new BoolProperty();

	public Property<bool> isDownloaded = new BoolProperty();

	public Property<bool> needShowDownloadLabel = new BoolProperty();

	public Property<string> downloadLabel = new StringProperty();

	public Property<bool> isCompleteEvent = new BoolProperty();

	public Property<bool> isDownloadingContent = new BoolProperty();

	public Property<bool> isFavorite = new BoolProperty();

	public Property<bool> isFriendOwner = new BoolProperty();

	public Property<bool> isPlayerOwner = new BoolProperty();

	public Property<bool> selected = new BoolProperty();

	public Property<bool> corrupted = new BoolProperty();

	public Property<bool> emptyIcon = new BoolProperty(startValue: true);

	public StringProperty emptyLabel = new StringProperty();

	public IntProperty ugcServiceId = new IntProperty();

	private BoolProperty m_versionVisibility = new BoolProperty();

	private StringProperty m_version = new StringProperty();

	private TrackSubItem m_subItem;

	private Vector3 m_vectorOffset;

	private bool m_widthAdjusted;

	private bool m_canMoveTexts;

	private float m_movingTextsGap;

	private RectTransform[] m_movingContainers;

	private void ScrollTrackNameIfNeed()
	{
		if (!m_canMoveTexts || Time.frameCount % 3 != 0)
		{
			return;
		}
		for (int i = 0; i < m_movingContainers.Length; i++)
		{
			RectTransform rectTransform = m_movingContainers[i];
			float num = Mathf.Max(m_movingTextsParent.GetWidth(), rectTransform.GetWidth());
			if (rectTransform.anchoredPosition.x > 0f - (num + m_movingTextsGap))
			{
				m_vectorOffset.x = rectTransform.anchoredPosition.x - Time.deltaTime * 70f;
			}
			else
			{
				m_vectorOffset.x = num + m_movingTextsGap;
			}
			rectTransform.anchoredPosition = m_vectorOffset;
		}
	}

	public void OnClick()
	{
		m_subItem?.OnClick();
	}

	public void OnDoubleClick()
	{
		m_subItem?.OnDoubleClick();
	}

	public void SetItem(TrackSubItem listItem)
	{
		if (m_subItem != null)
		{
			m_subItem.trackSubItemController = null;
		}
		m_subItem = listItem;
		if (m_subItem != null)
		{
			m_subItem.trackSubItemController = this;
		}
		visibleElement.value = listItem != null;
		UpdateItem();
	}

	public void UpdateItem()
	{
		if (m_subItem == null)
		{
			return;
		}
		isCompleteEvent.value = m_subItem.isCompleteEvent;
		dlcName.value = m_subItem.dlcName;
		goldLimit.value = m_subItem.goldLimit;
		silverLimit.value = m_subItem.silverLimit;
		bronzeLimit.value = m_subItem.bronzeLimit;
		bestResult.value = m_subItem.bestResult;
		lockedByLevel.value = m_subItem.isLockedByLevel;
		lockedByDLC.value = m_subItem.isLockedByDLC;
		locked.value = m_subItem.locked;
		coinsValue.value = m_subItem.coinsValue;
		this.text.value = m_subItem.text;
		cupIndex.value = m_subItem.cupIndex;
		isReverse.value = m_subItem.isReverse;
		isExternal.value = m_subItem.isExternal;
		isDownloadingContent.value = m_subItem.isDownloading;
		isFavorite.value = m_subItem.isFavorite;
		ugcServiceId.value = m_subItem.ugcServiceId;
		isFriendOwner.value = m_subItem.isFriendOwner;
		isPlayerOwner.value = m_subItem.isPlayerOwner;
		needShowDownloadLabel.value = isExternal.value && m_subItem.isDownloaded;
		selected.value = m_subItem.isSelected;
		corrupted.value = m_subItem.isMetaCorrupted;
		m_versionVisibility.value = m_subItem.targetClientVersion == "2.21.0";
		m_version.value = m_subItem.targetClientVersion;
		string text = m_subItem.name;
		nameMap.value = ((text.Length > 24) ? (text.Substring(0, 24) + "...") : text);
		emptyIcon.value = m_subItem.spriteAsset == null && m_subItem.iconPreview == null;
		iconMap.value = ((m_subItem.spriteAsset == null) ? m_subItem.iconPreview : m_subItem.spriteAsset.texture);
		if (corrupted.value)
		{
			m_iconCanvasGroup.alpha = 0.25f;
			isLoadingWebIcon.value = false;
			return;
		}
		if (needShowDownloadLabel.value)
		{
			downloadLabel.value = LocalizationKeys.ugc_item_downloaded.Localized();
		}
		if (isDownloadingContent.value)
		{
			emptyIcon.value = false;
			m_iconCanvasGroup.alpha = 0.25f;
			isLoadingWebIcon.value = true;
			needShowDownloadLabel.value = true;
			downloadLabel.value = LocalizationKeys.downloading_ugc_item.Localized();
		}
		else
		{
			isLoadingWebIcon.value = m_subItem.isLoadingWebIcon;
		}
		if (!isDownloadingContent.value)
		{
			isLoadingWebIcon.value = false;
			m_iconCanvasGroup.alpha = 1f;
		}
		emptyLabel.value = (emptyIcon.value ? LocalizationKeys.empty_slot.Localized() : string.Empty);
	}

	private void AdjustMovingTextsWidth()
	{
		if (m_movingContainers == null)
		{
			int childCount = m_movingTextsParent.childCount;
			m_movingContainers = new RectTransform[childCount];
			for (int i = 0; i < childCount; i++)
			{
				m_movingContainers[i] = m_movingTextsParent.GetChild(i).GetComponent<RectTransform>();
			}
		}
		if (m_widthAdjusted)
		{
			return;
		}
		for (int j = 0; j < m_movingContainers.Length; j++)
		{
			float width = m_movingContainers[j].GetChild(0).GetComponent<RectTransform>().GetWidth();
			float width2 = m_movingTextsParent.GetWidth();
			if (width > width2)
			{
				m_canMoveTexts = true;
			}
			float size = Mathf.Max(width, width2);
			m_movingContainers[j].SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
			if (j > 0)
			{
				m_movingTextsGap = 120f;
				m_vectorOffset.x = m_movingContainers[j].rect.width + m_movingTextsGap;
				m_movingContainers[j].anchoredPosition = m_vectorOffset;
			}
		}
		m_widthAdjusted = true;
	}

	public void FillDownloadProgress(float value)
	{
		if (isLoadingWebIcon.value && value > 0f)
		{
			isLoadingWebIcon.value = false;
			m_iconCanvasGroup.alpha = 0.5f;
		}
		m_loadingContentProgress.fillAmount = value;
	}
}
