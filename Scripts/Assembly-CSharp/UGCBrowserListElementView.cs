using System;
using Binding;
using Binding.Elements;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UGCBrowserListElementView : BaseListElement
{
	[SerializeField]
	private CanvasGroup m_iconCanvas;

	[SerializeField]
	private Image m_downloadProgressBar;

	private readonly BoolProperty m_selected = new BoolProperty();

	private readonly StringProperty m_name = new StringProperty();

	private readonly TextureProperty m_icon = new TextureProperty();

	private readonly BoolProperty m_isFavorite = new BoolProperty();

	private readonly BoolProperty m_isInstalled = new BoolProperty();

	private readonly BoolProperty m_isCorrupted = new BoolProperty();

	private readonly BoolProperty m_isPlayerOwner = new BoolProperty();

	private readonly BoolProperty m_isDownloading = new BoolProperty();

	private readonly BoolProperty m_isPlayable = new BoolProperty();

	private readonly BoolProperty m_isFriendOwner = new BoolProperty();

	private readonly StringProperty m_downloadLabel = new StringProperty();

	private readonly BoolProperty m_downloadStatusLabelVisibility = new BoolProperty();

	private readonly BoolProperty m_downloadSpinnerVisibility = new BoolProperty();

	private readonly BoolProperty m_installingImageVisibility = new BoolProperty();

	private readonly BoolProperty m_useDefaultIcon = new BoolProperty();

	private readonly BoolProperty m_useEmptyLabel = new BoolProperty();

	private readonly BoolProperty m_versionVisibility = new BoolProperty();

	private readonly StringProperty m_version = new StringProperty();

	protected override void OnDataChange(BaseListElementData newData)
	{
		base.OnDataChange(newData);
		OnDataUpdated();
	}

	public override void OnDataUpdated()
	{
		base.OnDataUpdated();
		if (base.data is UGCBrowserListItemData itemData)
		{
			UpdateProperties(itemData);
		}
	}

	private void UpdateProperties(UGCBrowserListItemData itemData)
	{
		m_icon.value = itemData.iconTexture;
		m_selected.value = itemData.selected;
		m_isCorrupted.value = itemData.isCorrupted;
		m_isPlayerOwner.value = itemData.isPlayerOwner;
		m_isFriendOwner.value = itemData.isFriendOwner;
		m_isFavorite.value = itemData.isFavorite;
		m_versionVisibility.value = itemData.needShowClientVersionLabel;
		m_version.value = itemData.clientVersionLabel;
		m_name.value = itemData.name;
		UpdateDownloadVisibilityProps(itemData);
	}

	private void UpdateDownloadVisibilityProps(UGCBrowserListItemData itemData)
	{
		m_isInstalled.value = itemData.isInstalled;
		m_isDownloading.value = itemData.isDownloading;
		m_isPlayable.value = itemData.isPlayable;
		m_downloadSpinnerVisibility.value = itemData.isDownloading || itemData.isDownloadingIcon;
		m_downloadStatusLabelVisibility.value = itemData.isDownloading || itemData.isInstalled;
		m_useDefaultIcon.value = itemData.isDownloadingIcon || m_icon.value == null;
		m_useEmptyLabel.value = m_icon.value == null && !itemData.isDownloadingIcon;
		itemData.installationProgressCallback = (Action<float>)Delegate.Remove(itemData.installationProgressCallback, new Action<float>(UpdateProgressBar));
		itemData.installationProgressCallback = (Action<float>)Delegate.Combine(itemData.installationProgressCallback, new Action<float>(UpdateProgressBar));
		if (itemData.isDownloading)
		{
			m_downloadLabel.value = LocalizationKeys.downloading_ugc_item.Localized();
			TweenCanvasFade(0.7f);
			m_downloadProgressBar.fillAmount = 1f;
		}
		else if (itemData.isInstalled)
		{
			m_downloadLabel.value = LocalizationKeys.ugc_item_downloaded.Localized();
			TweenCanvasFade(1f);
			m_downloadProgressBar.fillAmount = 0f;
		}
		else if (itemData.isCorrupted)
		{
			m_downloadLabel.value = LocalizationKeys.error.Localized();
			TweenCanvasFade(0.7f);
			m_downloadProgressBar.fillAmount = 0f;
		}
		else
		{
			m_downloadLabel.value = string.Empty;
			TweenCanvasFade(1f);
			m_downloadProgressBar.fillAmount = 0f;
		}
		void TweenCanvasFade(float alpha)
		{
			DOTween.To(() => m_iconCanvas.alpha, delegate(float x)
			{
				m_iconCanvas.alpha = x;
			}, alpha, 0.5f);
		}
	}

	private void UpdateProgressBar(float value)
	{
		if (!(this == null))
		{
			m_downloadProgressBar.fillAmount = 1f - value;
		}
	}
}
