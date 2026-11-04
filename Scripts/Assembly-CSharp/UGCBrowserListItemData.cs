using System;
using System.Threading;
using Binding;
using GameOverlay;
using Services.UGC.Interfaces;
using UnityEngine;

public class UGCBrowserListItemData : BaseListElementData
{
	public Action<float> installationProgressCallback;

	private bool m_disposed;

	private readonly IUGCManager m_manager;

	private readonly UGCItem m_ugcItem;

	private CancellationTokenSource m_iconDownloadCancellationToken;

	private Action<UGCBrowserListItemData, bool, string> m_onFinishedInstall;

	public Texture2D iconTexture { get; private set; }

	public string name { get; private set; }

	public bool isInstalled { get; private set; }

	public bool isDownloading { get; private set; }

	public bool isFriendOwner { get; private set; }

	public bool isPlayerOwner { get; private set; }

	public ulong externalId { get; private set; }

	public bool isCorrupted { get; private set; }

	public bool isDownloadingIcon { get; private set; }

	public bool isFavorite { get; private set; }

	public bool isPlayable { get; private set; }

	public bool needShowClientVersionLabel { get; private set; }

	public string clientVersionLabel { get; private set; }

	public PlayerId ownerId { get; private set; }

	public UGCItem ugcItem => m_ugcItem;

	public IUGCMeta ugcItemMeta => ugcItem as IUGCMeta;

	public UGCBrowserListItemData(UGCItem ugcItem, IUGCManager manager, Action<UGCBrowserListItemData, bool, string> onFinished)
	{
		m_ugcItem = ugcItem;
		m_manager = manager;
		name = ugcItem.title;
		externalId = ugcItem.id;
		m_onFinishedInstall = onFinished;
		if (m_manager.IsPackageRequested(externalId))
		{
			m_manager.RequestPackage(externalId, ManagerInstallationProgressCallback, OnFinishedInstall);
		}
		ProceedItemThroughData(ugcItem);
		isDownloadingIcon = true;
	}

	private void ManagerInstallationProgressCallback(float value)
	{
		installationProgressCallback.SafeCall(value);
	}

	private void ProceedItemThroughData(UGCItem ugcItem)
	{
		if (ugcItem is IUGCInstallable installedStatus)
		{
			SetInstalledStatus(installedStatus);
		}
		if (ugcItem is IUGCDownloadable downloadingStatus)
		{
			SetDownloadingStatus(downloadingStatus);
		}
		if (ugcItem is IUGCPlayable playableStatus)
		{
			SetPlayableStatus(playableStatus);
		}
		if (ugcItem is IUGCOwnerSystem ownerStatus)
		{
			SetOwnerStatus(ownerStatus);
		}
		if (ugcItem is IUGCMeta clientVersion)
		{
			SetClientVersion(clientVersion);
		}
	}

	public void LoadIcon(Action onFinished)
	{
		if (!(m_ugcItem is IUGCIconSystem iUGCIconSystem))
		{
			return;
		}
		if (iUGCIconSystem.iconTexture == null)
		{
			m_iconDownloadCancellationToken = new CancellationTokenSource();
			iUGCIconSystem.DownloadIcon(delegate(Texture2D texture, IUGCIconSystem.DownloadResult result)
			{
				ApplyIconTexture(texture, result);
				onFinished.SafeCall();
			}, null, m_iconDownloadCancellationToken);
		}
		else
		{
			ApplyIconTexture(iUGCIconSystem.iconTexture);
			onFinished.SafeCall();
		}
	}

	private void ApplyIconTexture(Texture2D texture, IUGCIconSystem.DownloadResult _ = IUGCIconSystem.DownloadResult.Success)
	{
		if (!m_disposed)
		{
			iconTexture = texture;
			isDownloadingIcon = false;
			UpdateData();
		}
	}

	private void SetDownloadingStatus(IUGCDownloadable ugcItem)
	{
		isDownloading = ugcItem.isDownloading;
		isCorrupted = !isInstalled && ugcItem.isDownloaded;
	}

	private void SetInstalledStatus(IUGCInstallable ugcItem)
	{
		isInstalled = ugcItem.isInstalled;
	}

	private void SetPlayableStatus(IUGCPlayable ugcItem)
	{
		isPlayable = ugcItem.isPlayable;
	}

	private void SetOwnerStatus(IUGCOwnerSystem ugcItem)
	{
		IGameOverlay instance = Overlay.instance;
		ownerId = ugcItem.owner.userId;
		isPlayerOwner = ownerId == instance.player.id;
		isFriendOwner = instance.IsFriend(ownerId);
	}

	private void SetClientVersion(IUGCMeta ugcItem)
	{
		needShowClientVersionLabel = ugcItem.targetClientVersion == "2.21.0";
		clientVersionLabel = ugcItem.targetClientVersion;
	}

	public void UpdateProperties()
	{
		ProceedItemThroughData(ugcItem);
		UpdateData();
	}

	public void Install()
	{
		isDownloading = true;
		m_manager.RequestPackage(externalId, installationProgressCallback, OnFinishedInstall);
		UpdateData();
	}

	public void Launch()
	{
		m_manager.LaunchPackage(externalId);
	}

	private void OnFinishedInstall(bool success, string errorMessage)
	{
		m_onFinishedInstall?.Invoke(this, success, errorMessage);
	}

	public void Uninstall()
	{
		isInstalled = false;
		isCorrupted = false;
		m_manager.RemovePackage(externalId);
		UpdateData();
	}

	public override void Dispose()
	{
		base.Dispose();
		UnityEngine.Object.Destroy(iconTexture);
		iconTexture = null;
		TryDisposeDownloadCancellationToken();
		TryDisposeItemIcon();
		installationProgressCallback = null;
		m_onFinishedInstall = null;
		m_disposed = true;
	}

	private void TryDisposeItemIcon()
	{
		if (m_ugcItem is IUGCIconSystem iUGCIconSystem)
		{
			iUGCIconSystem.DisposeIcon();
		}
	}

	private void TryDisposeDownloadCancellationToken()
	{
		if (m_iconDownloadCancellationToken != null && !m_iconDownloadCancellationToken.IsCancellationRequested)
		{
			m_iconDownloadCancellationToken.Cancel();
			m_iconDownloadCancellationToken.Dispose();
		}
	}
}
