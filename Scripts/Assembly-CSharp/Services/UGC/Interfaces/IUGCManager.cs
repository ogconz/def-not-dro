using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Services.UGC.Platforms;

namespace Services.UGC.Interfaces
{
	public interface IUGCManager
	{
		public enum RequestPackageAccess
		{
			OK = 0,
			Disabled = 1,
			AccessDenied = 2,
			PublicationNotFound = 3,
			OutOfMemory = 4,
			FailedToSubscribe = 5,
			FailedToInstall = 6,
			IsStorageSpaceInsufficient = 7,
			NoInternetConnection = 8,
			Unknown = 9
		}

		public enum ServiceProvider
		{
			Void = 0,
			Steamworkshop = 1,
			ModIO = 2
		}

		public enum ContentType
		{
			Void = 0,
			Tracks = 1,
			Cars = 2
		}

		const string VERSION_2_21_0 = "2.21.0";

		const string VERSION_2_22_0 = "2.22.0";

		ServiceProvider serviceProviderType { get; }

		string serviceTitle { get; }

		ContentType contentType { get; }

		IUGCService service { get; }

		void DownloadAllSubscribedPackages(Action<float, string> progress = null, Action onFinished = null);

		void IsMyPackage(ulong id, Action<bool> isMy);

		void RequestPackage(ulong id, Action<float> progress = null, Action<bool, string> onFinished = null, bool needSubscribe = true);

		void LaunchPackage(ulong id);

		void RemovePackage(ulong id);

		bool IsPackageInstalled(ulong id);

		bool IsPackageDownloaded(ulong id);

		bool IsPackagePlayable(ulong id);

		bool IsPackageRequested(ulong id);

		void StopLastRequestedPackage();

		void TryStopRequestedPackage(ulong id);

		void Update();

		int GetFetchedItemsCount();

		UniTaskVoid FetchUGCItems(Action onComplete);

		bool ItemTargetVersionExist(IUGCMeta meta, out string targetVersion);

		UGCItem TryGetInstalledItem(ulong id);

		UniTask<IEnumerable<UGCItem>> GetFetchedItems(int startIndex, int count, CancellationTokenSource cancellationToken = null);

		IEnumerable<IUGCMeta> GetRequestedPackagesQueue();

		ulong GetRequestedQueueItemsTotalSizeBytes();
	}
}
