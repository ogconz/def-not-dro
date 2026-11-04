using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GameOverlay;
using Services.UGC.Interfaces;
using Services.UGC.Utils;
using Steamworks.Ugc;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Services.UGC.Steamworks
{
	public class SteamWorkshopItem : UGCItem, IUGCInstallable, IUGCMeta, IUGCIconSystem, IUGCFavoritable, IUGCSubscridable, IUGCOwnerSystem, IUGCRatable, IUGCDownloadable, IUGCPlayable
	{
		private const int itemIconWidth = 636;

		private const int itemIconHeight = 358;

		private readonly SteamUGCDetails_t m_itemDetails;

		private readonly Overlay.UserInfo m_owner;

		private readonly Item m_item;

		private readonly IUGCManager m_ugcManager;

		private Texture2D m_iconTexture;

		private string m_targetClientVersion;

		private bool m_installFromScratch;

		private IUGCManager.ContentType m_contentType;

		public override ulong id => m_item.Id;

		public override string title => m_item.Title;

		public Item nativeItem => m_item;

		public bool isInstalled => m_ugcManager.IsPackageInstalled(id);

		public bool isDownloaded => m_ugcManager.IsPackageDownloaded(id);

		public bool isPlayable => m_ugcManager.IsPackagePlayable(id);

		public string directory => m_item.Directory;

		public string description => m_item.Description;

		public string[] tags => m_item.Tags;

		public string externalPath => m_item.Url;

		public string localPath
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public string externalIconPath => m_item.PreviewImageUrl;

		public bool needUpdate => m_item.NeedsUpdate;

		public bool isFavorite
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public bool isSubscribed => m_item.IsSubscribed;

		public DateTime timeCreated => m_item.Created;

		public Overlay.UserInfo owner => m_owner;

		public bool isDownloading
		{
			get
			{
				if (!m_item.IsDownloading)
				{
					return m_ugcManager.IsPackageRequested(id);
				}
				return true;
			}
		}

		public ulong subscriptionsCount => m_item.NumUniqueSubscriptions;

		public string ownerName => m_owner.name;

		public float rate => m_itemDetails.Score;

		public ulong sizeBytes => (ulong)m_itemDetails.FileSize;

		public string targetClientVersion => m_targetClientVersion;

		public int iconHeight => 358;

		public int iconWidth => 636;

		public bool supportSetRate => false;

		public bool installFromScratch => m_installFromScratch;

		public override IUGCManager.ServiceProvider serviceId => IUGCManager.ServiceProvider.Steamworkshop;

		public ulong archievedSizeBytes
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public override IUGCManager manager => m_ugcManager;

		public override IUGCManager.ContentType contentType => m_contentType;

		public Texture2D iconTexture
		{
			get
			{
				return m_iconTexture;
			}
			private set
			{
				if (m_iconTexture != value)
				{
					m_iconTexture = value;
				}
			}
		}

		public SteamWorkshopItem(Item steamItem, IUGCManager ugcManager)
		{
			m_item = steamItem;
			m_owner = new Overlay.UserInfo(new PlayerId(UserPlatform.Id.Steam, steamItem.Owner.Id.Value), steamItem.Owner.Name);
			m_itemDetails = UnsafeUtility.As<Item, ParsedSteamWorkshopItem>(ref steamItem).details;
			m_ugcManager = ugcManager;
			m_contentType = m_ugcManager.contentType;
			AcquireTargetItemClientVersion();
		}

		private void AcquireTargetItemClientVersion()
		{
			m_ugcManager.ItemTargetVersionExist(this, out m_targetClientVersion);
		}

		public void DownloadIcon(Action<Texture2D, IUGCIconSystem.DownloadResult> onFinished, Action<float> progress = null, CancellationTokenSource ctsSource = null)
		{
			UIUtils.DownloadTexture(externalIconPath, delegate(Texture2D texture)
			{
				if (ctsSource != null && ctsSource.IsCancellationRequested)
				{
					onFinished.SafeCall(null, IUGCIconSystem.DownloadResult.Cancelled);
				}
				else if (IsValidTexture(texture))
				{
					onFinished.SafeCall(texture, IUGCIconSystem.DownloadResult.Success);
					iconTexture = texture;
				}
				else
				{
					UnityEngine.Object.Destroy(texture);
					texture = null;
					onFinished.SafeCall(null, IUGCIconSystem.DownloadResult.NotValid);
				}
			}, iconWidth, iconHeight, ctsSource);
		}

		public bool IsValidUserName()
		{
			if (ownerName != null)
			{
				return ownerName != "[unknown]";
			}
			return false;
		}

		private UniTask<bool> RequestOwnServicesInfo(IUGCOwnerSystem.RequestInfo request)
		{
			AutoResetUniTaskCompletionSource<bool> tsc = AutoResetUniTaskCompletionSource<bool>.Create();
			if (request == IUGCOwnerSystem.RequestInfo.Name)
			{
				this.RequestNameThrowOwnApi(delegate(string name)
				{
					m_owner.name = name;
					tsc.TrySetResult(IsValidUserName());
				});
			}
			else
			{
				tsc.TrySetResult(result: false);
			}
			return tsc.Task;
		}

		private async UniTask<bool> RequestExternalServicesInfo(IUGCOwnerSystem.RequestInfo request)
		{
			Task task = m_item.Owner.RequestInfoAsync();
			while (!task.IsCompleted)
			{
				await UniTask.Yield();
			}
			if (request == IUGCOwnerSystem.RequestInfo.Name)
			{
				m_owner.name = m_item.Owner.Name;
				return IsValidUserName();
			}
			return true;
		}

		public async UniTask<bool> RequestInfoFromServices(IUGCOwnerSystem.RequestInfo request)
		{
			return await RequestExternalServicesInfo(request) || await RequestOwnServicesInfo(request);
		}

		private bool IsValidTexture(Texture2D texture)
		{
			if (texture != null && texture.width >= 64)
			{
				return texture.height >= 64;
			}
			return false;
		}

		public async UniTask<bool> SetFavorite(bool value)
		{
			if (value)
			{
				return await nativeItem.AddFavorite();
			}
			return await nativeItem.RemoveFavorite();
		}

		public UniTask<bool> SetRateAsync(int value)
		{
			return UniTask.FromResult(value: false);
		}

		public async UniTask<bool> SetSubscribed(bool value)
		{
			m_installFromScratch = true;
			if (value)
			{
				return await nativeItem.Subscribe();
			}
			return await nativeItem.Unsubscribe();
		}

		public void ShowInOverlay()
		{
			SteamWorkshopManager.ShowItemOverlay(id);
		}

		public void DisposeIcon()
		{
			UnityEngine.Object.Destroy(m_iconTexture);
			m_iconTexture = null;
		}
	}
}
