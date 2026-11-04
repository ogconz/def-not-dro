using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DI;
using Services.UGC.Interfaces;
using Services.UGC.Platforms;
using Steamworks;
using Steamworks.Ugc;
using UnityEngine;

namespace Services.UGC.Steamworks
{
	public abstract class SteamWorkshopManager : IUGCManager
	{
		private class ItemComparer : IEqualityComparer<SteamWorkshopItem>
		{
			public bool Equals(SteamWorkshopItem x, SteamWorkshopItem y)
			{
				return x.id == y.id;
			}

			public int GetHashCode(SteamWorkshopItem product)
			{
				return product.id.GetHashCode();
			}
		}

		private const string ServiceTitle = "Steamworkshop";

		protected readonly Queue<SteamWorkshopItem> m_ugcPackagesToHandleQueue = new Queue<SteamWorkshopItem>();

		protected readonly Queue<ulong> m_ugcRequestedPackages = new Queue<ulong>();

		protected readonly Queue<ulong> m_ugcQueueToUnsubscribe = new Queue<ulong>();

		protected readonly List<SteamWorkshopItem> m_ugcDownloadedPackages = new List<SteamWorkshopItem>();

		protected readonly List<SteamWorkshopItem> m_ugcSubscribedPackages = new List<SteamWorkshopItem>();

		protected readonly List<SteamWorkshopItem> m_ugcFavouritePackages = new List<SteamWorkshopItem>();

		protected GamePrefs m_prefs;

		private static CancellationTokenSource s_cancellationDownloadTokenSource;

		private IUGCService m_service;

		private List<SteamWorkshopItem> m_tempFetchResultList = new List<SteamWorkshopItem>();

		private List<SteamWorkshopItem> m_fetchResultListItems = new List<SteamWorkshopItem>();

		private Dictionary<ulong, Action<bool, string>> m_requestedPackagesCallbacks = new Dictionary<ulong, Action<bool, string>>();

		private bool m_isAnyPackageUnsubscribing;

		private bool m_isAnyPackageRequested;

		private bool m_fetchDistinct;

		private bool m_fetchComplete;

		protected bool m_isAllDownloadedOnStart;

		private bool m_downloadAllProgressWithInstall = true;

		public IUGCManager.ServiceProvider serviceProviderType => IUGCManager.ServiceProvider.Steamworkshop;

		public virtual IUGCManager.ContentType contentType => IUGCManager.ContentType.Void;

		public string serviceTitle => "Steamworkshop";

		public IUGCService service => m_service;

		public void Initialize(IUGCService service)
		{
			m_service = service;
		}

		public async void DownloadAllSubscribedPackages(Action<float, string> progress = null, Action onFinished = null)
		{
			TimeController.StartNow(DownloadAllSubscribedItems(progress));
			while (!m_isAllDownloadedOnStart)
			{
				await Task.Yield();
			}
			if (!m_downloadAllProgressWithInstall)
			{
				onFinished.SafeCall();
				return;
			}
			int itemsCountToHandle = m_ugcPackagesToHandleQueue.Count;
			while (m_ugcPackagesToHandleQueue.Count > 0)
			{
				progress.SafeCall(0.5f + (1f - (float)m_ugcPackagesToHandleQueue.Count / (float)itemsCountToHandle) / 2f, null);
				await Task.Yield();
			}
			onFinished.SafeCall();
		}

		protected SteamWorkshopItem PopPendingPackage()
		{
			if (m_ugcPackagesToHandleQueue.Count <= 0)
			{
				return null;
			}
			return m_ugcPackagesToHandleQueue.Dequeue();
		}

		protected SteamWorkshopItem PeekPendingPackage()
		{
			if (m_ugcPackagesToHandleQueue.Count <= 0)
			{
				return null;
			}
			return m_ugcPackagesToHandleQueue.Peek();
		}

		public virtual void Update()
		{
		}

		public void IsMyPackage(ulong id, Action<bool> isMy)
		{
			TimeController.StartNow(IsMyItemCoroutine(id, isMy));
		}

		public async void RequestPackage(ulong id, Action<float> progress, Action<bool, string> onFinished, bool needSubscribe = true)
		{
			await RequestPackageProcess(id, progress, onFinished, needSubscribe);
		}

		public async void StopLastRequestedPackage()
		{
			await StopRequestedPackageProcess();
		}

		public void TryStopRequestedPackage(ulong id)
		{
			StopLastRequestedPackage();
		}

		public bool IsPackageSubscribed(ulong id)
		{
			return m_ugcSubscribedPackages.Exists((SteamWorkshopItem item) => item.id == id);
		}

		public virtual bool IsPackageInstalled(ulong id)
		{
			return m_ugcDownloadedPackages.Exists((SteamWorkshopItem item) => item.id == id);
		}

		public bool IsPackageRequested(ulong id)
		{
			return m_ugcRequestedPackages.Contains(id);
		}

		public virtual async void RemovePackage(ulong id)
		{
			if (!m_ugcQueueToUnsubscribe.Contains(id))
			{
				m_ugcQueueToUnsubscribe.Enqueue(id);
				while (m_isAnyPackageUnsubscribing)
				{
					await Task.Yield();
				}
				m_isAnyPackageUnsubscribing = true;
				ulong targetId = m_ugcQueueToUnsubscribe.Peek();
				SteamWorkshopItem steamWorkshopItem = m_ugcDownloadedPackages.Find((SteamWorkshopItem item) => item.id == targetId);
				if (steamWorkshopItem != null)
				{
					m_ugcDownloadedPackages.Remove(steamWorkshopItem);
				}
				SteamWorkshopItem packageToUnsubscribe = m_ugcSubscribedPackages.Find((SteamWorkshopItem item) => item.id == targetId);
				if (packageToUnsubscribe != null)
				{
					await packageToUnsubscribe.SetSubscribed(value: false);
					m_ugcSubscribedPackages.Remove(packageToUnsubscribe);
				}
				m_isAnyPackageUnsubscribing = false;
				m_ugcQueueToUnsubscribe.Dequeue();
			}
		}

		public SteamWorkshopItem GetInstalledPackage(ulong id)
		{
			return m_ugcDownloadedPackages.Find((SteamWorkshopItem item) => item.id == id);
		}

		protected virtual void OnDownloadAllSubscribed()
		{
			m_isAllDownloadedOnStart = true;
		}

		protected virtual Query CreateQueryList()
		{
			return Query.Items.MatchAnyTag();
		}

		protected virtual Query CreateQueryFavoriteList()
		{
			return Query.All.WhereUserFavorited();
		}

		protected virtual void OnFavouriteItemsAquired()
		{
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
		}

		public async UniTask QueryFavoriteItems()
		{
			m_ugcFavouritePackages.Clear();
			await PaggingQuery(CreateQueryFavoriteList(), m_ugcFavouritePackages);
			OnFavouriteItemsAquired();
		}

		private IEnumerator DownloadAllSubscribedItems(Action<float, string> progress = null)
		{
			m_ugcPackagesToHandleQueue.Clear();
			m_fetchResultListItems.Clear();
			Query query = Query.All.WhereUserSubscribed();
			yield return PaggingQuery(query, m_fetchResultListItems).AsIEnumerator();
			int itemsCount = m_fetchResultListItems.Count;
			if (itemsCount == 0)
			{
				GameLog.Print(AlertLevel.Notify, "System", "No subscribed UGC detected");
				OnDownloadAllSubscribed();
				yield break;
			}
			GameLog.Print(AlertLevel.Notify, "System", $"Subscribed items: {m_fetchResultListItems.Count} ");
			for (int i = 0; i < itemsCount; i++)
			{
				SteamWorkshopItem item = m_fetchResultListItems[i];
				progress.SafeCall((float)i / (float)itemsCount / (m_downloadAllProgressWithInstall ? 2f : 1f), item.title);
				yield return ProcessItem(item, null, delegate(bool success)
				{
					if (success)
					{
						TryAddToSubscribedList(item);
					}
				}).AsIEnumerator();
			}
			OnDownloadAllSubscribed();
		}

		private async Task RequestPackageProcess(ulong id, Action<float> progress, Action<bool, string> onFinished, bool needSubcribe)
		{
			if (m_ugcRequestedPackages.Contains(id))
			{
				return;
			}
			m_ugcRequestedPackages.Enqueue(id);
			m_requestedPackagesCallbacks[id] = onFinished;
			while (m_isAnyPackageRequested)
			{
				await Task.Yield();
			}
			m_isAnyPackageRequested = true;
			ulong targetId = m_ugcRequestedPackages.Peek();
			Item? item = await SteamUGC.QueryFileAsync(targetId);
			if (!item.HasValue)
			{
				GameLog.Print(AlertLevel.Notify, "System", $"Unable to fetch UGC item with id {id}");
				OnFinishedRequest(success: false, IUGCManager.RequestPackageAccess.Unknown);
				return;
			}
			Item value = item.Value;
			switch (value.Result)
			{
			case Result.AccessDenied:
				OnFinishedRequest(success: false, IUGCManager.RequestPackageAccess.AccessDenied);
				return;
			case Result.FileNotFound:
				OnFinishedRequest(success: false, IUGCManager.RequestPackageAccess.PublicationNotFound);
				return;
			}
			bool itemInstalled = false;
			SteamWorkshopItem steamItem = new SteamWorkshopItem(value, this);
			if (!(await ProcessItem(steamItem, progress, delegate(bool installed)
			{
				itemInstalled = installed;
			})))
			{
				if (itemInstalled)
				{
					OnFinishedRequest(success: false, IUGCManager.RequestPackageAccess.Disabled);
				}
				else
				{
					OnFinishedRequest(success: false, IUGCManager.RequestPackageAccess.Unknown);
				}
				return;
			}
			bool flag = needSubcribe;
			if (flag)
			{
				flag = await steamItem.SetSubscribed(value: true);
			}
			if (flag)
			{
				TryAddToSubscribedList(steamItem);
			}
			while (m_ugcPackagesToHandleQueue.Contains(steamItem))
			{
				await Task.Yield();
			}
			OnFinishedRequest(success: true, IUGCManager.RequestPackageAccess.OK);
			void OnFinishedRequest(bool success, IUGCManager.RequestPackageAccess accessResult)
			{
				m_isAnyPackageRequested = false;
				m_ugcRequestedPackages.Dequeue();
				string param = string.Empty;
				switch (accessResult)
				{
				case IUGCManager.RequestPackageAccess.Disabled:
					param = LocalizationKeys.ugc_track_meta_error.Localized();
					break;
				case IUGCManager.RequestPackageAccess.AccessDenied:
					param = LocalizationKeys.external_track_access_denied.Localized();
					break;
				case IUGCManager.RequestPackageAccess.PublicationNotFound:
					param = LocalizationKeys.publication_not_exist.Localized();
					break;
				default:
					param = LocalizationKeys.unknown_error.Localized();
					break;
				case IUGCManager.RequestPackageAccess.OK:
					break;
				}
				m_requestedPackagesCallbacks[targetId].SafeCall(success, param);
				m_requestedPackagesCallbacks.Remove(targetId);
			}
		}

		public static async void ShowItemOverlay(ulong id)
		{
			Item? item = await SteamUGC.QueryFileAsync(id);
			if (!item.HasValue)
			{
				GameLog.Print(AlertLevel.Notify, "System", $"Unable to find UGC item with id {id}");
			}
			else
			{
				SteamFriends.OpenWebOverlay(item.Value.Url);
			}
		}

		public async Task StopRequestedPackageProcess()
		{
			if (m_ugcRequestedPackages.Count == 0)
			{
				return;
			}
			while (s_cancellationDownloadTokenSource == null)
			{
				if (m_ugcRequestedPackages.Count == 0)
				{
					return;
				}
				await Task.Delay(100);
			}
			s_cancellationDownloadTokenSource?.Cancel();
			s_cancellationDownloadTokenSource?.Dispose();
			s_cancellationDownloadTokenSource = null;
		}

		private void TryAddToSubscribedList(SteamWorkshopItem item)
		{
			if (!m_ugcSubscribedPackages.Contains(item))
			{
				m_ugcSubscribedPackages.Add(item);
			}
		}

		private bool IsTagMatching(Item item)
		{
			string[] tags = item.Tags;
			foreach (string tag in tags)
			{
				if (EqualsTag(tag))
				{
					return true;
				}
			}
			return false;
		}

		public async Task<bool> SetFavourite(ulong id, bool isFavourite)
		{
			SteamWorkshopItem steamWorkshopItem = m_ugcFavouritePackages.Find((SteamWorkshopItem steamWorkshopItem2) => steamWorkshopItem2.id == id);
			if (steamWorkshopItem == null)
			{
				Item? item = await SteamUGC.QueryFileAsync(id);
				if (!item.HasValue)
				{
					return false;
				}
				steamWorkshopItem = new SteamWorkshopItem(item.Value, this);
			}
			return await steamWorkshopItem.SetFavorite(isFavourite);
		}

		protected virtual bool EqualsTag(string tag)
		{
			return true;
		}

		private async Task<bool> ProcessItem(SteamWorkshopItem steamItem, Action<float> progress = null, Action<bool> onDone = null)
		{
			if (!IsTagMatching(steamItem.nativeItem))
			{
				onDone.SafeCall(param: false);
				return false;
			}
			bool flag = Directory.Exists(steamItem.directory) || steamItem.nativeItem.IsInstalled;
			if (!flag)
			{
				GameLog.Print(AlertLevel.Notify, "System", "Item not installed, downloading '" + steamItem.title + "'");
				flag = await DownloadWorkshopItem(steamItem.nativeItem, progress);
				if (flag)
				{
					SendItemDownloadedAnalytics(steamItem.id);
				}
			}
			bool available = flag && steamItem.nativeItem.IsInstalled;
			if (available && steamItem.needUpdate)
			{
				GameLog.Print(AlertLevel.Notify, "System", "Updating installed item '" + steamItem.title + "'");
				flag = await DownloadWorkshopItem(steamItem.nativeItem, progress);
			}
			if (available)
			{
				GameLog.Print(AlertLevel.Notify, "System", "Added UGC item '" + steamItem.title + "'");
				if (!m_ugcDownloadedPackages.Contains(steamItem))
				{
					m_ugcDownloadedPackages.Add(steamItem);
				}
				if (!m_ugcPackagesToHandleQueue.Contains(steamItem))
				{
					m_ugcPackagesToHandleQueue.Enqueue(steamItem);
				}
			}
			onDone.SafeCall(flag);
			return available;
		}

		protected virtual void SendItemDownloadedAnalytics(ulong id)
		{
		}

		private static async Task<bool> DownloadWorkshopItem(Item item, Action<float> progress)
		{
			Item? item2 = await SteamUGC.QueryFileAsync(item.Id);
			if (!item2.HasValue)
			{
				return false;
			}
			item = item2.Value;
			if (item.Result != Result.OK)
			{
				return false;
			}
			s_cancellationDownloadTokenSource = new CancellationTokenSource();
			if (!(await item.DownloadAsync(progress, 60, s_cancellationDownloadTokenSource.Token)))
			{
				if (s_cancellationDownloadTokenSource == null || s_cancellationDownloadTokenSource.IsCancellationRequested)
				{
					Debug.Log($"Cancel download: {item.Id.Value}");
				}
				else
				{
					Debug.Log($"Download file with id: {item.Id.Value} doesn't exist");
				}
				return false;
			}
			bool isDownloadedCanceled = s_cancellationDownloadTokenSource == null || s_cancellationDownloadTokenSource.IsCancellationRequested;
			while (item.NeedsUpdate && !isDownloadedCanceled)
			{
				await Task.Delay(1000);
				Debug.Log($"Downloading Update... ({item.DownloadAmount:0.000}) [{item.DownloadBytesDownloaded}/{item.DownloadBytesTotal}]");
			}
			while (!item.IsInstalled && !isDownloadedCanceled)
			{
				await Task.Delay(1000);
				Debug.Log("Installing... " + item.Title);
			}
			return !isDownloadedCanceled;
		}

		private IEnumerator IsMyItemCoroutine(ulong id, Action<bool> isMy)
		{
			Task<Item?> itemTask = Item.GetAsync(id);
			yield return itemTask.AsIEnumerator();
			Item? result = itemTask.Result;
			if (!result.HasValue)
			{
				isMy.SafeCall(param: false);
			}
			else
			{
				isMy.SafeCall(result.Value.Owner.IsMe);
			}
		}

		public async UniTaskVoid FetchUGCItems(Action onComplete)
		{
			m_fetchComplete = false;
			m_fetchResultListItems.Clear();
			await PaggingQuery(CreateQueryList().WhereUserSubscribed().SortBySubscriptionDate(), m_fetchResultListItems);
			await PaggingQuery(CreateQueryList().WhereUserPublished().SortByVoteScore(), m_fetchResultListItems);
			await PaggingQuery(CreateQueryList().CreatedByFriends().SortByVoteScore(), m_fetchResultListItems);
			await PaggingQuery(CreateQueryList().SortByVoteScore(), m_fetchResultListItems);
			DistinctFetch(force: true);
			m_fetchComplete = true;
			onComplete.SafeCall();
		}

		public void DistinctFetch(bool force = false)
		{
			if (force || m_fetchDistinct)
			{
				m_fetchResultListItems = m_fetchResultListItems.Distinct(new ItemComparer()).ToList();
				m_fetchDistinct = force;
			}
		}

		public bool IsCompleteRangeFetch(int index, int count)
		{
			if (m_fetchResultListItems == null)
			{
				return true;
			}
			if (m_fetchResultListItems.Count - index <= count)
			{
				return m_fetchComplete;
			}
			return true;
		}

		private async Task PaggingQuery(Query query, List<SteamWorkshopItem> resultList)
		{
			int estimateCount = int.MaxValue;
			int page = 1;
			byte errorCounter = 0;
			while (estimateCount > 0 && errorCounter < 5)
			{
				ResultPage? resultPage = await query.GetPageAsync(page);
				if (resultPage.HasValue)
				{
					ResultPage value = resultPage.Value;
					AddToResultList(value.Entries, resultList);
					estimateCount = value.TotalCount - value.ResultCount;
					value.Dispose();
					if (value.ResultCount == 0)
					{
						errorCounter++;
					}
					else
					{
						page++;
						errorCounter = 0;
					}
				}
				else
				{
					errorCounter++;
				}
				if (estimateCount > 0)
				{
					await Task.Delay(500);
				}
			}
			void AddToResultList(IEnumerable<Item> queryList, List<SteamWorkshopItem> list)
			{
				m_tempFetchResultList.Clear();
				foreach (Item item in queryList)
				{
					if (item.Result == Result.OK && list.FindIndex((SteamWorkshopItem x) => x.id == item.Id) < 0 && IsTagMatching(item))
					{
						SteamWorkshopItem item2 = new SteamWorkshopItem(item, this);
						m_tempFetchResultList.Add(item2);
					}
				}
				list.AddRange(m_tempFetchResultList);
			}
		}

		public async UniTask<IEnumerable<UGCItem>> GetFetchedItems(int startIndex, int count, CancellationTokenSource cancellationToken = null)
		{
			await UniTask.WaitUntil(() => IsCompleteRangeFetch(startIndex, count) || (cancellationToken != null && cancellationToken.IsCancellationRequested));
			DistinctFetch();
			int num = startIndex + count;
			int fetchedItemsCount = GetFetchedItemsCount();
			return m_fetchResultListItems.GetRange(startIndex, (num > fetchedItemsCount) ? (GetFetchedItemsCount() - startIndex) : (num - startIndex));
		}

		public int GetFetchedItemsCount()
		{
			return m_fetchResultListItems.Count;
		}

		public virtual void LaunchPackage(ulong id)
		{
		}

		public bool IsPackageDownloaded(ulong id)
		{
			return m_ugcDownloadedPackages.FindIndex((SteamWorkshopItem item) => item.id == id) >= 0;
		}

		public virtual bool ItemTargetVersionExist(IUGCMeta itemMeta, out string targetVersion)
		{
			targetVersion = null;
			return false;
		}

		public virtual UGCItem TryGetInstalledItem(ulong id)
		{
			return m_ugcSubscribedPackages.Find((SteamWorkshopItem item) => item.id == id);
		}

		public IEnumerable<IUGCMeta> GetRequestedPackagesQueue()
		{
			throw new NotImplementedException();
		}

		public ulong GetRequestedQueueItemsTotalSizeBytes()
		{
			throw new NotImplementedException();
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
	}
}
