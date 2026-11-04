using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Services.UGC.Interfaces;
using Services.UGC.Platforms;

namespace GameOverlay
{
	public class VoidUGCManager : IUGCManager
	{
		private const string ServiceTitle = "Void";

		public IUGCManager.ServiceProvider serviceProviderType => IUGCManager.ServiceProvider.Void;

		public IUGCManager.ContentType contentType => IUGCManager.ContentType.Void;

		public string serviceTitle => "Void";

		public IUGCService service
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public void IsMyPackage(ulong id, Action<bool> isMy)
		{
			isMy.SafeCall(param: false);
		}

		public bool IsSomethingUploading()
		{
			return false;
		}

		public void RequestPackage(ulong id, Action<float> progress, Action<bool, string> isFinished, bool needSubscribe = true)
		{
		}

		public void RemovePackage(ulong id)
		{
		}

		public void StopLastRequestedPackage()
		{
		}

		public bool IsPackageRequested(ulong id)
		{
			return false;
		}

		public void Flush()
		{
		}

		public void Update()
		{
		}

		public void UpdatePackage(string carName, string path, ulong id)
		{
		}

		public void UploadPackage(string carName, string path, Action<ulong> uploadedID)
		{
			uploadedID.SafeCall(0uL);
		}

		public void DownloadAllSubscribedPackages(Action<float, string> progress = null, Action onFinished = null)
		{
			onFinished.SafeCall();
		}

		public UniTaskVoid FetchUGCItems(Action onComplete)
		{
			throw new NotImplementedException();
		}

		public UniTask<IEnumerable<UGCItem>> GetFetchedItems(int startIndex, int count, CancellationTokenSource cancellationToken)
		{
			throw new NotImplementedException();
		}

		public int GetFetchedItemsCount()
		{
			throw new NotImplementedException();
		}

		public void LaunchPackage(ulong id)
		{
		}

		public bool IsPackageInstalled(ulong id)
		{
			return false;
		}

		public UGCItem GetInstalledPackage(ulong id)
		{
			throw new NotImplementedException();
		}

		public bool IsPackageDownloaded(ulong id)
		{
			return false;
		}

		public bool ItemTargetVersionExist(IUGCMeta meta, out string targetVersion)
		{
			targetVersion = null;
			return false;
		}

		public UGCItem TryGetInstalledItem(ulong id)
		{
			return null;
		}

		public void TryStopRequestedPackage(ulong id)
		{
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
			throw new NotImplementedException();
		}
	}
}
