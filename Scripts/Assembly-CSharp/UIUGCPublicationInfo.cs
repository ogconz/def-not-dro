using Binding;
using Services.UGC.Interfaces;
using UnityEngine;

public class UIUGCPublicationInfo : UIBaseSharingPublication
{
	private readonly BoolProperty m_detailedStorageInfoVisibility = new BoolProperty();

	private readonly BoolProperty m_storageMemoryExceeded = new BoolProperty();

	private readonly StringProperty m_availableStorageSize = new StringProperty();

	private readonly FloatProperty m_usedStorageMemoryNormalized = new FloatProperty();

	private readonly FloatProperty m_usedStorageMemoryWithItemNormalized = new FloatProperty();

	private readonly FloatProperty m_usedStorageMemoryWithRequestedQueueNormalized = new FloatProperty();

	private static float? s_plsSizeMB;

	private IUGCMeta m_currUgcItem;

	private IUGCManager m_currItemManager;

	private string m_availableStorageSizeLabel;

	private bool m_initialized;

	private float m_lastTimeStorageAccess;

	private IUGCInstallable m_currUgcInstallable => m_currUgcItem as IUGCInstallable;

	private IUGCDownloadable m_currUgcDownloadable => m_currUgcItem as IUGCDownloadable;

	private bool IsInitialized()
	{
		return m_initialized;
	}

	public override void SetMetaData(IUGCMeta ugcItem)
	{
		base.SetMetaData(ugcItem);
		m_currUgcItem = ugcItem;
		m_currItemManager = (ugcItem as UGCItem).manager;
		LocalizeLabels();
		TryInitialize();
	}

	private void LocalizeLabels()
	{
		if (m_availableStorageSizeLabel == null)
		{
			m_availableStorageSizeLabel = LocalizationKeys.ugc_browser_avaiable_storage_space.Localized();
		}
	}

	private void Update()
	{
		TryInitialize();
		if (IsInitialized() && IsNeedUpdateStorageInfo())
		{
			UpdateDetailedStorageInfoPanel();
			m_lastTimeStorageAccess = Time.time;
		}
	}

	private bool IsNeedUpdateStorageInfo()
	{
		return Time.time - m_lastTimeStorageAccess > GetDelaySecStorageCheckRelyOnPlatform();
	}

	private void TryInitialize()
	{
		m_initialized = IsSupportedPlatform() && m_currItemManager.service.storage.IsReady() && m_currUgcInstallable != null && !m_currUgcInstallable.isInstalled && m_currUgcDownloadable != null && !m_currUgcDownloadable.isDownloading;
		m_detailedStorageInfoVisibility.value = IsInitialized();
	}

	private bool IsSupportedPlatform()
	{
		return m_currItemManager.serviceProviderType == IUGCManager.ServiceProvider.ModIO;
	}

	private float GetDelaySecStorageCheckRelyOnPlatform()
	{
		return 1f;
	}

	private void UpdateDetailedStorageInfoPanel()
	{
		AcquireUGCStorageSizeInfo();
		float num = FromBytesToMBytes(m_currItemManager.service.storage.GetAvailableSizeBytes());
		float num2 = Mathf.Clamp(s_plsSizeMB.Value - num, 0f, 2.1474836E+09f);
		float num3 = num2 + FromBytesToMBytes(m_currItemManager.GetRequestedQueueItemsTotalSizeBytes());
		float num4 = num3 + FromBytesToMBytes(m_currUgcItem.sizeBytes);
		m_usedStorageMemoryNormalized.value = num2 / s_plsSizeMB.Value;
		m_usedStorageMemoryWithRequestedQueueNormalized.value = num3 / s_plsSizeMB.Value;
		m_usedStorageMemoryWithItemNormalized.value = num4 / s_plsSizeMB.Value;
		m_storageMemoryExceeded.value = !m_currItemManager.service.storage.IsEnoughSpaceToInstall(m_currUgcItem);
		m_availableStorageSize.value = $"{num:0.0} MB ({m_availableStorageSizeLabel})";
	}

	private void AcquireUGCStorageSizeInfo()
	{
		float valueOrDefault = s_plsSizeMB.GetValueOrDefault();
		if (!s_plsSizeMB.HasValue)
		{
			valueOrDefault = FromBytesToMBytes(m_currItemManager.service.storage.GetSizeBytes());
			s_plsSizeMB = valueOrDefault;
		}
	}

	private float FromBytesToMBytes(ulong bytes)
	{
		return (float)bytes / 1024f / 1024f;
	}
}
