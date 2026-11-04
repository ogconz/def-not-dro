using System;
using System.Collections.Generic;
using System.Threading;
using ArrayExtension;
using Cysharp.Threading.Tasks;
using ModIO;
using Services.UGC.Interfaces;
using Services.UGC.Platforms;
using Unity.Mathematics;

namespace Service.UGC.ModIO
{
	public abstract class ModIOManager : IUGCManager
	{
		private const string ServiceTitle = "Mod.io";

		private CancellationTokenSource m_fetchItemCts;

		private ModIOService m_service;

		protected List<ModIOItem> m_ugcFetchedByModIOItems = new List<ModIOItem>();

		protected List<ModIOItem> m_ugcSubscribedByModIOItems = new List<ModIOItem>();

		protected List<ModIOItem> m_ugcInstalledItems = new List<ModIOItem>();

		protected List<ModIOItem> m_nonValidUgcItems = new List<ModIOItem>();

		private Dictionary<ulong, Action<float>> m_progressRequestedCallbacks = new Dictionary<ulong, Action<float>>();

		private Dictionary<ulong, Action<bool, string>> m_finishedRequestedCallbacks = new Dictionary<ulong, Action<bool, string>>();

		private UniTask? m_installationTask;

		public string serviceTitle => "Mod.io";

		public IUGCService service => m_service;

		public IUGCManager.ServiceProvider serviceProviderType => IUGCManager.ServiceProvider.ModIO;

		public virtual IUGCManager.ContentType contentType => IUGCManager.ContentType.Void;

		public abstract string ugcTagMatch { get; }

		public void DownloadAllSubscribedPackages(Action<float, string> progress = null, Action onFinished = null)
		{
			OnInstalledSubscribedItems();
			onFinished.SafeCall();
		}

		private void FillSubscribedPackages()
		{
			Result result;
			SubscribedMod[] subscribedMods = ModIOUnity.GetSubscribedMods(out result);
			if (result.Succeeded())
			{
				for (int i = 0; i < subscribedMods.Length; i++)
				{
					SubscribedMod subscribedMod = subscribedMods[i];
					if (subscribedMod.modProfile.tags.Contains(ugcTagMatch))
					{
						ModIOItem modIOItem = m_ugcSubscribedByModIOItems.Find((ModIOItem modIOItem2) => modIOItem2.id == math.asulong(subscribedMod.modProfile.id.id));
						if (modIOItem != null)
						{
							modIOItem.subscribedModProfile = subscribedMod;
							continue;
						}
						ModIOItem item = new ModIOItem(subscribedMod, this);
						m_ugcSubscribedByModIOItems.Add(item);
					}
				}
			}
			else
			{
				GameLog.Print(AlertLevel.Warning, "ModMaps", "Failed to get subscribed mods: " + result.message);
			}
		}

		public void Initialize(IUGCService service)
		{
			m_service = service as ModIOService;
			ModIOService modIOService = m_service;
			modIOService.onModOperation = (Action<ModManagementEventType, ModId, Result>)Delegate.Combine(modIOService.onModOperation, new Action<ModManagementEventType, ModId, Result>(OnModOperationHandler));
			IUGCAuthentication authentication = m_service.authentication;
			authentication.onAuthChanged = (Action<bool>)Delegate.Combine(authentication.onAuthChanged, new Action<bool>(OnAuthenticationChangedHandler));
		}

		private void OnAuthenticationChangedHandler(bool isAuthenticated)
		{
			if (!isAuthenticated)
			{
				DeleteAllSubscribtions();
			}
		}

		private void DeleteAllSubscribtions()
		{
			foreach (ModIOItem item in m_ugcSubscribedByModIOItems)
			{
				m_ugcInstalledItems.RemoveAll((ModIOItem installedItem) => installedItem.id == item.id);
				OnUninstallItem(item);
				item.SetSubscribed(value: false).Forget();
			}
			m_ugcSubscribedByModIOItems.Clear();
		}

		private void OnModOperationHandler(ModManagementEventType eventType, ModId modId, Result eventResult)
		{
			switch (eventType)
			{
			case ModManagementEventType.Downloaded:
				GUICommonGodVoice.ShowText(LocalizationKeys.ugc_manager_is_downloaded_prepare_to_install.Localized(), 15f);
				break;
			case ModManagementEventType.InstallFailed:
			case ModManagementEventType.DownloadFailed:
			case ModManagementEventType.UninstallFailed:
			case ModManagementEventType.UpdateFailed:
				FinalizeAndInformHandleItem((ulong)modId.id, installResult: false);
				break;
			}
		}

		public void Update()
		{
			HandleSubscribedItemsStatus();
		}

		private void HandleSubscribedItemsStatus()
		{
			FillSubscribedPackages();
			EnsureSufficientStorageSpaceForRequestedPackages();
			TryProceedSubscribedItems();
		}

		private void TryProceedSubscribedItems()
		{
			if (!IsReadyToHandle())
			{
				return;
			}
			foreach (ModIOItem ugcSubscribedByModIOItem in m_ugcSubscribedByModIOItems)
			{
				if (!IsItemAlreadyHandled(ugcSubscribedByModIOItem))
				{
					m_installationTask = HandleItemAsync(ugcSubscribedByModIOItem);
					m_installationTask.Value.ContinueWith(() => m_installationTask = null).Forget();
				}
			}
		}

		protected virtual bool IsReadyToHandle()
		{
			return true;
		}

		private void EnsureSufficientStorageSpaceForRequestedPackages()
		{
			if (!m_service.storage.IsReady())
			{
				return;
			}
			List<ModIOItem> list = GetRequestedPackagesQueue() as List<ModIOItem>;
			ulong num = GetRequestedQueueItemsTotalSizeBytes(list);
			ulong availableSizeBytes = m_service.storage.GetAvailableSizeBytes();
			while (num >= availableSizeBytes)
			{
				ModIOItem lastInQueue = list.PopLast();
				if (lastInQueue != null)
				{
					num -= lastInQueue.sizeBytes;
					m_ugcSubscribedByModIOItems.Remove(lastInQueue);
					lastInQueue.SetSubscribed(value: false).ContinueWith(delegate
					{
						FinalizeAndInformHandleItem(lastInQueue, installResult: false, string.Empty, string.Empty);
					}).Forget();
					continue;
				}
				break;
			}
		}

		private bool IsItemAlreadyHandled(ModIOItem item)
		{
			if (!m_installationTask.HasValue && !m_ugcInstalledItems.Exists((ModIOItem installedItem) => installedItem.id == item.id))
			{
				return m_nonValidUgcItems.Exists((ModIOItem installedItem) => installedItem.id == item.id);
			}
			return true;
		}

		private async UniTask HandleItemAsync(ModIOItem item)
		{
			SubscribedMod value = item.subscribedModProfile.Value;
			if (value.status == SubscribedModStatus.Installed)
			{
				if (await ObtainInstalledItem(item))
				{
					string godvoiceMessage = (item.installFromScratch ? (item.title + ": " + LocalizationKeys.ugc_item_downloaded.Localized()) : string.Empty);
					m_ugcInstalledItems.Add(item);
					FinalizeAndInformHandleItem(item, installResult: true, godvoiceMessage, string.Empty);
				}
				else
				{
					string text = LocalizationKeys.ugc_track_meta_error.Localized();
					string godvoiceMessage2 = item.title + ": " + text;
					m_nonValidUgcItems.Add(item);
					FinalizeAndInformHandleItem(item, installResult: false, godvoiceMessage2, text);
				}
			}
			else if (value.status == SubscribedModStatus.ProblemOccurred)
			{
				await item.SetSubscribed(value: false);
				FinalizeAndInformHandleItem(item, installResult: false, "ModIO item problem occured with " + item.title, string.Empty);
			}
		}

		private void FinalizeAndInformHandleItem(ModIOItem item, bool installResult, string godvoiceMessage, string errorMessage)
		{
			FinalizeAndInformHandleItem(item.id, installResult, godvoiceMessage, errorMessage);
		}

		private void FinalizeAndInformHandleItem(ulong id, bool installResult, string godvoiceMessage = null, string errorMessage = null)
		{
			m_finishedRequestedCallbacks.TryGetValue(id, out var value);
			value.SafeCall(installResult, errorMessage);
			if (value != null)
			{
				m_finishedRequestedCallbacks.Remove(id);
			}
			if (!string.IsNullOrEmpty(godvoiceMessage))
			{
				GUICommonGodVoice.ShowText(godvoiceMessage);
			}
			m_progressRequestedCallbacks.Remove(id);
		}

		public void HandleProgress(ProgressHandle handle)
		{
			ulong id = (ulong)handle.modId.id;
			if (m_progressRequestedCallbacks.ContainsKey(id))
			{
				float param = handle.OperationType switch
				{
					ModManagementOperationType.Download => 0f, 
					ModManagementOperationType.Install => 0.5f, 
					ModManagementOperationType.Update => 0.5f, 
					_ => 0f, 
				} + handle.Progress * 0.5f;
				m_progressRequestedCallbacks[id].SafeCall(param);
			}
		}

		public abstract void LaunchPackage(ulong id);

		protected abstract void OnInstalledSubscribedItems();

		protected abstract void OnUninstallItem(ModIOItem modIOItem);

		protected abstract UniTask<bool> ObtainInstalledItem(ModIOItem modIOItem);

		public void IsMyPackage(ulong id, Action<bool> isMy)
		{
			isMy.SafeCall(param: false);
		}

		public void RequestPackage(ulong id, Action<float> progress = null, Action<bool, string> onFinished = null, bool needSubscribe = true)
		{
			UniTask<bool> authTask = m_service.authentication.AuthenticateAsync();
			authTask.GetAwaiter().OnCompleted(delegate
			{
				if (authTask.GetAwaiter().GetResult())
				{
					RequestPackageAsync(id, progress, onFinished).Forget();
				}
				else
				{
					onFinished.SafeCall(param1: false, string.Empty);
					if (States.current is BaseGameState { contextOwner: var contextOwner } && contextOwner.IsActive<UIUGCBrowserContext>())
					{
						contextOwner.HideAllExcept<UIMainMenuContext>();
					}
				}
			});
		}

		private async UniTask RequestPackageAsync(ulong id, Action<float> progress, Action<bool, string> onFinished)
		{
			m_progressRequestedCallbacks[id] = progress;
			m_finishedRequestedCallbacks[id] = onFinished;
			ModIOItem foundedMod = await TryFindModAsync(id);
			if (foundedMod == null)
			{
				OnStartRequestResult(foundedMod, result: false, LocalizationKeys.publication_not_exist.Localized());
			}
			else if (m_service.storage.IsReady() && !m_service.storage.IsEnoughSpaceToInstall(foundedMod))
			{
				OnStartRequestResult(foundedMod, result: false, LocalizationKeys.ugc_browser_install_insufficient_storage_space.Localized());
			}
			else
			{
				OnStartRequestResult(foundedMod, result: true);
			}
			void OnStartRequestResult(ModIOItem item, bool result, string errorMessage = null)
			{
				if (result && foundedMod != null)
				{
					item.SetSubscribed(value: true).GetAwaiter().OnCompleted(delegate
					{
						if (!m_ugcSubscribedByModIOItems.Exists((ModIOItem subscribedItem) => subscribedItem.id == item.id))
						{
							m_ugcSubscribedByModIOItems.Add(item);
						}
					});
				}
				else
				{
					FinalizeAndInformHandleItem(id, installResult: false, string.Empty, errorMessage);
				}
			}
		}

		public IEnumerable<IUGCMeta> GetRequestedPackagesQueue()
		{
			return m_ugcSubscribedByModIOItems.FindAll(delegate(ModIOItem package)
			{
				SubscribedModStatus status = package.subscribedModProfile.Value.status;
				return status.IsSubscribed() && status != SubscribedModStatus.Installed;
			});
		}

		public ulong GetRequestedQueueItemsTotalSizeBytes()
		{
			return GetRequestedQueueItemsTotalSizeBytes(GetRequestedPackagesQueue());
		}

		public ulong GetRequestedQueueItemsTotalSizeBytes(IEnumerable<IUGCMeta> items)
		{
			ulong num = 0uL;
			foreach (IUGCMeta item in items)
			{
				num += item.sizeBytes;
			}
			return num;
		}

		private async UniTask<ModIOItem> TryFindModAsync(ulong id)
		{
			ResultAnd<ModProfile> resultAnd = await ModIOUnityAsync.GetMod(new ModId((long)id));
			if (resultAnd == null || !resultAnd.result.Succeeded())
			{
				return null;
			}
			return new ModIOItem(resultAnd.value, this);
		}

		public virtual void RemovePackage(ulong id)
		{
			ModIOItem item = m_ugcFetchedByModIOItems.Find((ModIOItem modIOItem) => modIOItem.id == id);
			if (item != null)
			{
				ProcessUnsubscribeFromItem(item);
				return;
			}
			UniTask<ModIOItem> findTask = TryFindModAsync(id);
			findTask.GetAwaiter().OnCompleted(delegate
			{
				item = findTask.GetAwaiter().GetResult();
				if (item != null)
				{
					ProcessUnsubscribeFromItem(item);
				}
			});
			void ProcessUnsubscribeFromItem(ModIOItem modIOItem)
			{
				UniTask<bool> unsubscribeTask = modIOItem.SetSubscribed(value: false);
				unsubscribeTask.GetAwaiter().OnCompleted(delegate
				{
					if (unsubscribeTask.GetAwaiter().GetResult())
					{
						OnUninstallItem(modIOItem);
						m_ugcSubscribedByModIOItems.RemoveAll((ModIOItem modIOItem2) => modIOItem2.id == id);
						m_ugcInstalledItems.RemoveAll((ModIOItem modIOItem2) => modIOItem2.id == id);
						m_nonValidUgcItems.RemoveAll((ModIOItem modIOItem2) => modIOItem2.id == id);
					}
				});
			}
		}

		public bool IsPackageInstalled(ulong id)
		{
			return m_ugcInstalledItems.Exists((ModIOItem installedItem) => installedItem.id == id);
		}

		public bool IsPackageDownloaded(ulong id)
		{
			ModIOItem modIOItem = m_ugcSubscribedByModIOItems.Find((ModIOItem item) => item.id == id);
			if (modIOItem == null || !modIOItem.subscribedModProfile.HasValue)
			{
				return false;
			}
			_ = modIOItem.subscribedModProfile.Value;
			return modIOItem.subscribedModProfile.Value.status switch
			{
				SubscribedModStatus.Downloading => false, 
				SubscribedModStatus.Installed => true, 
				SubscribedModStatus.Installing => true, 
				SubscribedModStatus.Updating => true, 
				SubscribedModStatus.WaitingToDownload => false, 
				SubscribedModStatus.WaitingToInstall => true, 
				SubscribedModStatus.WaitingToUpdate => false, 
				_ => false, 
			};
		}

		public bool IsPackagePlayable(ulong id)
		{
			bool flag = IsPackageDownloaded(id) || IsPackageInstalled(id);
			if (flag)
			{
				flag = contentType switch
				{
					IUGCManager.ContentType.Void => false, 
					IUGCManager.ContentType.Tracks => true, 
					IUGCManager.ContentType.Cars => false, 
					_ => false, 
				};
			}
			return flag;
		}

		public bool IsPackageRequested(ulong id)
		{
			ModIOItem modIOItem = m_ugcSubscribedByModIOItems.Find((ModIOItem subscribed) => subscribed.id == id);
			if (modIOItem == null)
			{
				return false;
			}
			SubscribedModStatus status = modIOItem.subscribedModProfile.Value.status;
			if (status == SubscribedModStatus.ProblemOccurred)
			{
				return false;
			}
			if (m_nonValidUgcItems.Exists((ModIOItem item) => item.id == id))
			{
				return false;
			}
			if (status == SubscribedModStatus.Installed)
			{
				return !IsPackageInstalled(id);
			}
			return true;
		}

		public void StopLastRequestedPackage()
		{
			throw new NotImplementedException();
		}

		public void TryStopRequestedPackage(ulong id)
		{
			m_ugcSubscribedByModIOItems.Find((ModIOItem subscribed) => subscribed.id == id)?.SetSubscribed(value: false).Forget();
		}

		public int GetFetchedItemsCount()
		{
			return m_ugcFetchedByModIOItems.Count;
		}

		public bool ItemTargetVersionExist(IUGCMeta meta, out string targetVersion)
		{
			targetVersion = string.Empty;
			return false;
		}

		public UGCItem TryGetInstalledItem(ulong id)
		{
			return m_ugcInstalledItems.Find((ModIOItem installedItem) => installedItem.id == id);
		}

		public async UniTask<IEnumerable<UGCItem>> GetFetchedItems(int startIndex, int count, CancellationTokenSource cancellationToken = null)
		{
			await UniTask.WaitUntil(() => IsCompleteRangeFetch(startIndex, count) || (cancellationToken != null && cancellationToken.IsCancellationRequested));
			return m_ugcFetchedByModIOItems.GetRange(Math.Min(m_ugcFetchedByModIOItems.Count, startIndex), Math.Min(m_ugcFetchedByModIOItems.Count - startIndex, count));
		}

		private bool IsCompleteRangeFetch(int index, int count)
		{
			if (m_ugcFetchedByModIOItems.Count - index <= count)
			{
				return m_fetchItemCts == null;
			}
			return true;
		}

		public async UniTaskVoid FetchUGCItems(Action onComplete)
		{
			m_fetchItemCts = new CancellationTokenSource();
			await PagingModsAsync();
			onComplete.SafeCall();
			m_fetchItemCts.Cancel();
			m_fetchItemCts.Dispose();
			m_fetchItemCts = null;
		}

		private async UniTask PagingModsAsync()
		{
			m_ugcFetchedByModIOItems.Clear();
			SearchFilter filter = new SearchFilter();
			filter.SetPageSize(100);
			filter.AddTag(ugcTagMatch);
			int pageIndex = 0;
			while (true)
			{
				filter.SetPageIndex(pageIndex);
				ResultAnd<ModPage> resultAnd = await ModIOUnityAsync.GetMods(filter);
				if (!resultAnd.result.Succeeded())
				{
					GameLog.Print(AlertLevel.Warning, "ModMaps", "Failed to paging mods: " + resultAnd.result.message);
					break;
				}
				if (resultAnd.value.modProfiles.Length != 0)
				{
					for (int i = 0; i < resultAnd.value.modProfiles.Length; i++)
					{
						m_ugcFetchedByModIOItems.Add(new ModIOItem(resultAnd.value.modProfiles[i], this));
					}
					pageIndex++;
					continue;
				}
				break;
			}
		}
	}
}
