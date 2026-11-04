using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DB;
using DB.Meta;
using DI;
using Services.UGC.Interfaces;
using Services.UGC.Utils;
using Steamworks;
using Steamworks.Ugc;

namespace Services.UGC.Steamworks
{
	public class SteamWorkshopMapManager : SteamWorkshopManager
	{
		private const string MAP_TAG_1_0 = "map";

		private const string MAP_TAG_2_0 = "map_2.0";

		private bool m_isHandlePendingPackage;

		private BaseModel m_baseModel;

		private Dictionary<string, string> m_tagToVersionCorrepondence = new Dictionary<string, string>
		{
			{ "map", "2.21.0" },
			{ "map_2.0", "2.22.0" }
		};

		public override IUGCManager.ContentType contentType => IUGCManager.ContentType.Tracks;

		public override void Update()
		{
			base.Update();
			if (!m_isHandlePendingPackage && m_isAllDownloadedOnStart)
			{
				SteamWorkshopItem steamWorkshopItem = PeekPendingPackage();
				if (steamWorkshopItem != null && steamWorkshopItem.nativeItem.IsInstalled && steamWorkshopItem.nativeItem.Result == Result.OK)
				{
					TryObtainPendingPackage(steamWorkshopItem).Forget();
				}
			}
		}

		private async UniTask TryObtainPendingPackage(SteamWorkshopItem item)
		{
			m_isHandlePendingPackage = true;
			await UGCSceneBundleManager.TryInitTrackMetaAsync(item);
			PopPendingPackage();
			m_isHandlePendingPackage = false;
		}

		protected override Query CreateQueryList()
		{
			return Query.Items.WithTag("map").WithTag("map_2.0").MatchAnyTag();
		}

		protected override void SendItemDownloadedAnalytics(ulong id)
		{
			base.SendItemDownloadedAnalytics(id);
			AnalyticsService.SendModMapDownloaded(id.ToString());
		}

		public override bool IsPackageInstalled(ulong id)
		{
			return UGCSceneBundleManager.TryGetMetaByExternalId(id) != null;
		}

		protected override void OnFavouriteItemsAquired()
		{
			base.OnFavouriteItemsAquired();
			InitBaseModelIfNeed();
			List<DB.Meta.Track> list = m_baseModel.QueryAll<DB.Meta.Track>();
			IReadOnlyList<ulong> favoriteTracks = m_prefs.favoriteTrack.favoriteTracks;
			List<ulong> listToRemoveFromFavorite = new List<ulong>();
			foreach (ulong item in favoriteTracks)
			{
				listToRemoveFromFavorite.Add(item);
			}
			list.ForEach(delegate(DB.Meta.Track track)
			{
				listToRemoveFromFavorite.Remove((ulong)track.id);
			});
			foreach (ulong id in listToRemoveFromFavorite)
			{
				if (!m_ugcFavouritePackages.Exists((SteamWorkshopItem i) => i.id == id))
				{
					m_prefs.favoriteTrack.RemoveTrack(id);
				}
			}
			m_ugcFavouritePackages.ForEach(delegate(SteamWorkshopItem item)
			{
				m_prefs.favoriteTrack.AddTrack(item.id);
			});
		}

		private void InitBaseModelIfNeed()
		{
			if (m_baseModel == null)
			{
				m_baseModel = DependencyInjector.Resolve<BaseModel>();
			}
		}

		protected override void OnDownloadAllSubscribed()
		{
			InitBaseModelIfNeed();
			foreach (DB.Meta.Track track in m_baseModel.QueryAll<DB.Meta.Track>())
			{
				if (track.external && track.ugcServiceId == (byte)base.serviceProviderType && m_ugcDownloadedPackages.FindIndex((SteamWorkshopItem i) => i.id == track.externalId) == -1)
				{
					UGCSceneBundleManager.DeleteExternalTrackMeta(track.externalId);
				}
			}
			m_baseModel.Reload();
			base.OnDownloadAllSubscribed();
		}

		public override void RemovePackage(ulong id)
		{
			base.RemovePackage(id);
			UGCSceneBundleManager.DeleteExternalTrackMeta(id);
		}

		protected override bool EqualsTag(string tag)
		{
			if (string.IsNullOrEmpty(tag))
			{
				return false;
			}
			if (!tag.Equals("map_2.0", StringComparison.OrdinalIgnoreCase))
			{
				return tag.Equals("map", StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}

		public override void LaunchPackage(ulong id)
		{
			SteamWorkshopItem installedPackage = GetInstalledPackage(id);
			UGCSceneBundleManager.CheckVersionCompatibility(installedPackage);
			UGCSceneBundleManager.LaunchSceneByItem(installedPackage);
		}

		public override bool ItemTargetVersionExist(IUGCMeta itemMeta, out string targetVersion)
		{
			targetVersion = null;
			string[] tags = itemMeta.tags;
			for (int i = 0; i < tags.Length; i++)
			{
				if (m_tagToVersionCorrepondence.TryGetValue(tags[i], out targetVersion))
				{
					return true;
				}
			}
			return false;
		}
	}
}
