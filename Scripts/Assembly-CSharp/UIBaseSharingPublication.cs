using System.Collections.Generic;
using System.Threading;
using Binding;
using Binding.Base;
using GameOverlay;
using Services.UGC.Interfaces;
using UnityEngine;

public class UIBaseSharingPublication : BaseBindingBehaviourTarget
{
	private const float RATING_COEFF = 5f;

	private const float BYTES_TO_MEGABYTES = 1048576f;

	private const float MAX_DESCRIPTION_LENGTH = 256f;

	private StringProperty m_creatorNickname = new StringProperty();

	private StringProperty m_ugcItemTags = new StringProperty();

	private StringProperty m_uploadDate = new StringProperty();

	private FloatProperty m_size = new FloatProperty();

	private FloatProperty m_rating = new FloatProperty();

	private BoolProperty m_showDescription = new BoolProperty();

	private BoolProperty m_iconVisibility = new BoolProperty(startValue: true);

	private StringProperty m_description = new StringProperty();

	private StringProperty m_itemName = new StringProperty();

	private FloatProperty m_downloadCount = new FloatProperty();

	private BoolProperty m_isFriendOwner = new BoolProperty();

	private BoolProperty m_isPlayerOwner = new BoolProperty();

	private readonly SpriteProperty m_publicationIcon = new SpriteProperty();

	private readonly Property<bool> m_isLoadingWebIcon = new BoolProperty();

	private readonly Property<bool> m_isEmptyIcon = new BoolProperty();

	private bool m_currIconDisposed;

	private CancellationTokenSource m_iconDownloadCancellationToken;

	private List<IUGCIconSystem> m_ugcWithIcons = new List<IUGCIconSystem>();

	public void SetIcon(IUGCIconSystem ugcItem)
	{
		if (ugcItem != null)
		{
			if (!m_ugcWithIcons.Contains(ugcItem))
			{
				m_ugcWithIcons.Add(ugcItem);
			}
			m_isLoadingWebIcon.value = true;
			m_currIconDisposed = false;
			if (ugcItem.iconTexture == null)
			{
				m_iconDownloadCancellationToken = new CancellationTokenSource();
				ugcItem.DownloadIcon(ApplyIconTexture, null, m_iconDownloadCancellationToken);
			}
			else
			{
				ApplyIconTexture(ugcItem.iconTexture);
			}
		}
		void ApplyIconTexture(Texture2D texture, IUGCIconSystem.DownloadResult _ = IUGCIconSystem.DownloadResult.Success)
		{
			if (!m_currIconDisposed)
			{
				m_isEmptyIcon.value = texture == null;
				m_isLoadingWebIcon.value = false;
				if (texture != null)
				{
					m_publicationIcon.value = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.zero);
				}
			}
		}
	}

	public void SetOwnerStatus(IUGCOwnerSystem ugcItem)
	{
		if (ugcItem == null)
		{
			return;
		}
		IGameOverlay instance = Overlay.instance;
		Overlay.UserInfo userInfo = ugcItem.owner;
		m_isFriendOwner.value = instance.IsFriend(userInfo.userId);
		m_isPlayerOwner.value = instance.player.id == userInfo.userId;
		if (ugcItem.IsValidUserName())
		{
			SetOwnerName(ugcItem.owner.name);
			return;
		}
		ugcItem.RequestInfoFromServices(IUGCOwnerSystem.RequestInfo.Name).GetAwaiter().OnCompleted(delegate
		{
			SetOwnerName(ugcItem.owner.name);
		});
		void SetOwnerName(string name)
		{
			m_creatorNickname.value = name;
		}
	}

	public void SetSubscridedCount(IUGCSubscridable ugcItem)
	{
		if (ugcItem != null)
		{
			m_downloadCount.value = ugcItem.subscriptionsCount;
		}
	}

	public virtual void SetMetaData(IUGCMeta ugcItem)
	{
		if (ugcItem != null)
		{
			m_ugcItemTags.value = string.Join(", ", ugcItem.tags);
			m_uploadDate.value = ugcItem.timeCreated.ToLocalTime().ToString();
			m_size.value = (float)ugcItem.sizeBytes / 1048576f;
			m_description.value = ugcItem.description;
			if ((float)m_description.value.Length >= 256f)
			{
				m_description.value += "...";
			}
			bool flag = string.IsNullOrWhiteSpace(m_description.value);
			m_showDescription.value = !flag;
			m_description.value = (flag ? LocalizationKeys.empty_slot.Localized() : m_description.value);
		}
	}

	public void SetScore(IUGCRatable ugcItem)
	{
		if (ugcItem != null)
		{
			m_rating.value = ugcItem.rate * 5f;
		}
	}

	public virtual void InitView(UGCItem ugcItem)
	{
		ResetView();
		m_itemName.value = ugcItem.title;
	}

	public void SetIconVisibility(bool visible)
	{
		m_iconVisibility.value = visible;
	}

	public void ResetView()
	{
		m_description.value = string.Empty;
		m_uploadDate.value = string.Empty;
		m_creatorNickname.value = string.Empty;
		m_ugcItemTags.value = string.Empty;
		m_itemName.value = string.Empty;
		m_isFriendOwner.value = false;
		m_isPlayerOwner.value = false;
		m_showDescription.value = false;
		m_iconVisibility.value = false;
		m_isEmptyIcon.value = false;
		m_size.value = 0f;
		m_rating.value = 0f;
		m_downloadCount.value = 0f;
	}

	public void DisposeResource()
	{
		Object.Destroy(m_publicationIcon.value);
		m_publicationIcon.value = null;
		TryDisposeDownloadIconCancellationToken();
		m_currIconDisposed = true;
	}

	private void TryDisposeDownloadIconCancellationToken()
	{
		if (m_iconDownloadCancellationToken != null && !m_iconDownloadCancellationToken.IsCancellationRequested)
		{
			m_iconDownloadCancellationToken.Cancel();
			m_iconDownloadCancellationToken.Dispose();
		}
	}

	protected override void OnDeactivate()
	{
		foreach (IUGCIconSystem ugcWithIcon in m_ugcWithIcons)
		{
			ugcWithIcon.DisposeIcon();
		}
		base.OnDeactivate();
	}
}
