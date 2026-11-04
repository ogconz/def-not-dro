using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameOverlay;
using ModIO;
using Services.UGC.Interfaces;
using Unity.Mathematics;
using UnityEngine;

namespace Service.UGC.ModIO
{
	public class ModIOItem : UGCItem, IUGCIconSystem, IUGCMeta, IUGCOwnerSystem, IUGCRatable, IUGCSubscridable, IUGCInstallable, IUGCDownloadable, IUGCReportable, IUGCPlayable
	{
		private const float percentageScale = 100f;

		private const int itemIconWidth = 320;

		private const int itemIconHeight = 180;

		private Texture2D m_iconTexture;

		private ModIOManager m_modIOManager;

		private IUGCManager.ContentType m_contentType;

		private bool m_installFromScratch;

		public override ulong id => math.asulong(modProfile.id.id);

		public override string title => modProfile.name;

		public string description => modProfile.description;

		public ulong sizeBytes => (ulong)modProfile.modfile.filesize;

		public ulong archievedSizeBytes => (ulong)modProfile.archiveFileSize;

		public string[] tags => modProfile.tags;

		public DateTime timeCreated => modProfile.dateAdded;

		public string targetClientVersion => modProfile.latestVersion;

		public float rate => (float)modProfile.stats.ratingsPercentagePositive / 100f;

		public ulong subscriptionsCount => (ulong)modProfile.stats.downloadsTotal;

		public Texture2D iconTexture => m_iconTexture;

		public bool supportSetRate => true;

		public int iconHeight => 180;

		public int iconWidth => 320;

		public bool installFromScratch => m_installFromScratch;

		public bool isInstalled => m_modIOManager.IsPackageInstalled(id);

		public bool isDownloading => m_modIOManager.IsPackageRequested(id);

		public bool isDownloaded => m_modIOManager.IsPackageDownloaded(id);

		public bool isPlayable => m_modIOManager.IsPackagePlayable(id);

		public string directory
		{
			get
			{
				if (!subscribedModProfile.HasValue)
				{
					return string.Empty;
				}
				return Path.GetFullPath(subscribedModProfile.Value.directory);
			}
		}

		public string localPath { get; }

		public bool needUpdate { get; }

		public string externalPath { get; }

		public string externalIconPath { get; }

		public bool isSubscribed { get; private set; }

		public bool isReported { get; private set; }

		public Overlay.UserInfo owner { get; private set; }

		public SubscribedMod? subscribedModProfile { get; set; }

		public ModProfile modProfile { get; private set; }

		public override IUGCManager.ServiceProvider serviceId => IUGCManager.ServiceProvider.ModIO;

		public override IUGCManager manager => m_modIOManager;

		public override IUGCManager.ContentType contentType => m_contentType;

		public ModIOItem(SubscribedMod subscribedModProfile, ModIOManager modIOManager)
			: this(subscribedModProfile.modProfile, modIOManager)
		{
			this.subscribedModProfile = subscribedModProfile;
		}

		public ModIOItem(ModProfile modProfile, ModIOManager modIOManager)
		{
			this.modProfile = modProfile;
			m_modIOManager = modIOManager;
			m_contentType = m_modIOManager.contentType;
			owner = new Overlay.UserInfo(new PlayerId(Overlay.GetPlatform(), math.asulong(modProfile.creator.userId)), modProfile.creator.username);
		}

		public void DisposeIcon()
		{
			UnityEngine.Object.Destroy(m_iconTexture);
			m_iconTexture = null;
		}

		public void DownloadIcon(Action<Texture2D, IUGCIconSystem.DownloadResult> onFinished, Action<float> progress = null, CancellationTokenSource ctsSource = null)
		{
			ModIOUnity.DownloadTexture(modProfile.logoImage320x180, delegate(ResultAnd<Texture2D> icon)
			{
				onFinished(icon.value, (!icon.result.Succeeded()) ? IUGCIconSystem.DownloadResult.NotValid : IUGCIconSystem.DownloadResult.Success);
			});
		}

		public UniTask<bool> RequestInfoFromServices(IUGCOwnerSystem.RequestInfo info)
		{
			return UniTask.FromResult(value: true);
		}

		public async UniTask<bool> SetRateAsync(int value)
		{
			ModRating rating = ((value > 0) ? ModRating.Positive : ((value < 0) ? ModRating.Negative : ModRating.None));
			return (await ModIOUnityAsync.RateMod(modProfile.id, rating)).Succeeded();
		}

		public async UniTask<bool> SetSubscribed(bool value)
		{
			m_installFromScratch = true;
			Result result;
			if (value)
			{
				result = await ModIOUnityAsync.SubscribeToMod(modProfile.id);
			}
			else
			{
				result = await ModIOUnityAsync.UnsubscribeFromMod(modProfile.id);
				ModIOUnity.ForceUninstallMod(modProfile.id);
			}
			return result.Succeeded();
		}

		public void ShowInOverlay()
		{
			Application.OpenURL(modProfile.profilePageUrl + GetPlatformUrlPostfixBy(Overlay.GetPlatform()));
		}

		private string GetPlatformReportUrlBy(UserPlatform.Id platformId)
		{
			return $"https://mod.io/report/mods/{modProfile.id.id}/widget{GetPlatformUrlPostfixBy(platformId)}";
		}

		private string GetPlatformUrlPostfixBy(UserPlatform.Id platformId)
		{
			return platformId switch
			{
				UserPlatform.Id.PS4 => "?portal=psn&login=auto", 
				UserPlatform.Id.Xbox => "?portal=xboxlive&login=auto", 
				_ => "", 
			};
		}

		public void Report()
		{
			Application.OpenURL(GetPlatformReportUrlBy(Overlay.GetPlatform()));
			isReported = true;
		}

		public bool IsValidUserName()
		{
			return !owner.userId.IsInvalid;
		}
	}
}
