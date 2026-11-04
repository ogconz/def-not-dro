using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using DB;
using DB.Meta;
using DI;
using Services.UGC.Interfaces;
using TriLib;
using UnityEngine;

namespace Services.UGC.Utils
{
	public static class UGCSceneBundleManager
	{
		private const string IconPreviewLargeFileName = "IconPreview_Large";

		private const string IconPreviewFileName = "IconPreview";

		private const string MetaBundleFileName = "meta";

		private static readonly GamePrefs s_prefs;

		private static readonly BaseModel s_baseModel;

		private static readonly TransactionManager s_transactionManager;

		private static IUGCMeta s_loadedSceneMeta;

		private static AssetBundle s_loadedSceneAssetBundle;

		public static bool isLoadingSceneBundle { get; private set; }

		public static (IUGCMeta, AssetBundle) loadedSceneBundle => (s_loadedSceneMeta, s_loadedSceneAssetBundle);

		static UGCSceneBundleManager()
		{
			s_baseModel = DependencyInjector.Resolve<BaseModel>();
			s_prefs = DependencyInjector.Resolve<GamePrefs>();
			s_transactionManager = DependencyInjector.Resolve<TransactionManager>();
			States.onStateActivate += ReleaseBundleOnStateActivateHandler;
		}

		public static async UniTask<bool> TryInitTrackMetaAsync<T>(T ugcItem) where T : UGCItem, IUGCInstallable, IUGCMeta
		{
			List<DB.Meta.Track> tracks = s_baseModel.QueryAll<DB.Meta.Track>();
			GameLog.Print(AlertLevel.Notify, "ModMaps", $"Try Init {ugcItem.id} {ugcItem.title}");
			DB.Meta.Track track = TryGetMetaByExternalId(ugcItem.id, tracks);
			if (track != null && ugcItem.needUpdate)
			{
				GameLog.Print(AlertLevel.Notify, "ModMaps", $"Update item {ugcItem.id} {ugcItem.title}");
				DeleteExternalTrackMeta(track.externalId);
				track = null;
			}
			if (track != null)
			{
				bool exists = new FileInfo(track.largeIcon).Exists;
				bool exists2 = new FileInfo(track.icon).Exists;
				if (!exists || !exists2 || !CheckExpectedUgcDirSize(ugcItem))
				{
					DeleteExternalTrackMeta(track.externalId);
					track = null;
					GameLog.Print(AlertLevel.Notify, "ModMaps", $"Delete workshop item folder {ugcItem.id} {ugcItem.title}");
				}
			}
			if (track == null)
			{
				if (!CheckExpectedUgcDirSize(ugcItem))
				{
					GameLog.Print(AlertLevel.Error, "ModMaps", $"Mismatch expected dir size for ID: {ugcItem.id} Title: {ugcItem.title}");
					return false;
				}
				DB.Meta.Track track2 = await CreateTrackMetaByItemAsync(ugcItem, tracks);
				if (track2 != null)
				{
					s_baseModel.Insert(track2);
					PerformUnlockTransaction(track2);
					GameLog.Print(AlertLevel.Notify, "ModMaps", $"Successfully added meta item {ugcItem.id} {ugcItem.title}");
					return true;
				}
				GameLog.Print(AlertLevel.Notify, "ModMaps", $"Failed creating meta {ugcItem.id} {ugcItem.title}");
				return false;
			}
			PerformUnlockTransaction(track);
			return true;
			static void PerformUnlockTransaction(DB.Meta.Track trackMeta)
			{
				s_transactionManager.TryToPerformUnlockTransaction(TransactionManager.UnlockType.Track, trackMeta.id, trackMeta.cost, ECurrencyType.Coins);
			}
		}

		public static DB.Meta.Track TryGetMetaByExternalId(ulong externalId, List<DB.Meta.Track> tracks = null)
		{
			if (tracks == null)
			{
				tracks = s_baseModel.QueryAll<DB.Meta.Track>();
			}
			return tracks.Find((DB.Meta.Track track) => track.externalId == externalId);
		}

		private static async UniTask<DB.Meta.Track> CreateTrackMetaByItemAsync<T>(T ugcItem, List<DB.Meta.Track> tracks) where T : UGCItem, IUGCMeta, IUGCInstallable
		{
			string assetPathFromPackage = GetAssetPathFromPackage("meta", ugcItem);
			string metaCacheDir = GetCachedMetaPathDir(ugcItem);
			DateTime metaBundleLastWriteTime = File.GetLastWriteTimeUtc(assetPathFromPackage);
			if (!(Directory.GetLastWriteTimeUtc(metaCacheDir) < metaBundleLastWriteTime))
			{
				return CreateDefaultTrackMeta(ugcItem, tracks);
			}
			if (ModMapTestTool.IsNotCorrectMetaFileSize(assetPathFromPackage))
			{
				return null;
			}
			AssetBundle metaAssetBundle = await LoadBundleByPackageItemAsync("meta", ugcItem);
			if (metaAssetBundle == null)
			{
				GameLog.Print(AlertLevel.Error, "ModMaps", $"Asset bundle from item {ugcItem.id} not loaded");
				return null;
			}
			MapManagerConfig mapManagerConfig = metaAssetBundle.LoadAsset<MapManagerConfig>("MapManagerConfig");
			if (mapManagerConfig == null)
			{
				metaAssetBundle.Unload(unloadAllLoadedObjects: true);
				return null;
			}
			MapMetaConfig mapMetaConfigValue = mapManagerConfig.mapMetaConfigValue;
			if (mapMetaConfigValue == null || mapMetaConfigValue.mapMetaConfigValue.icon == null || !mapMetaConfigValue.mapMetaConfigValue.icon.isReadable || mapMetaConfigValue.mapMetaConfigValue.largeIcon == null || !mapMetaConfigValue.mapMetaConfigValue.largeIcon.isReadable)
			{
				GameLog.Print(AlertLevel.Error, "ModMaps", "Texture error format");
				metaAssetBundle.Unload(unloadAllLoadedObjects: true);
				return null;
			}
			if (!Directory.Exists(metaCacheDir))
			{
				Directory.CreateDirectory(metaCacheDir);
			}
			else
			{
				Directory.SetLastWriteTimeUtc(metaCacheDir, metaBundleLastWriteTime);
			}
			GetCachedIconsPath(ugcItem, out var iconPath, out var iconLargePath);
			try
			{
				Texture2D largeIconTexture = mapMetaConfigValue.mapMetaConfigValue.largeIcon.DeCompress();
				Texture2D iconTexture = mapMetaConfigValue.mapMetaConfigValue.icon.DeCompress();
				await File.WriteAllBytesAsync(iconLargePath, largeIconTexture.EncodeToPNG());
				await File.WriteAllBytesAsync(iconPath, iconTexture.EncodeToPNG());
				UnityEngine.Object.Destroy(largeIconTexture);
				UnityEngine.Object.Destroy(iconTexture);
			}
			catch (Exception ex)
			{
				GameLog.Print(AlertLevel.Error, "ModMaps", "Texture no load : " + ex);
				metaAssetBundle.Unload(unloadAllLoadedObjects: true);
				return null;
			}
			metaAssetBundle.Unload(unloadAllLoadedObjects: true);
			string sceneBundleName = GetSceneBundleName(ugcItem);
			string assetPathFromPackage2 = GetAssetPathFromPackage(sceneBundleName, ugcItem);
			if (ModMapTestTool.IsNotCorrectMapFileSize(sceneBundleName, assetPathFromPackage2))
			{
				GameLog.Print(AlertLevel.Error, "ModMaps", $"Not Correct Map File Size {ugcItem.id} not loaded");
				return null;
			}
			return CreateDefaultTrackMeta(ugcItem, tracks);
		}

		private static bool CheckExpectedUgcDirSize<T>(T ugcItem) where T : IUGCInstallable, IUGCMeta
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(ugcItem.directory);
			if (directoryInfo.Exists)
			{
				return GetDirectorySize(directoryInfo) == ugcItem.sizeBytes;
			}
			return false;
		}

		private static void GetCachedIconsPath<T>(T ugcItem, out string iconPath, out string iconLargePath) where T : IUGCInstallable
		{
			string cachedMetaPathDir = GetCachedMetaPathDir(ugcItem);
			char directorySeparatorChar = Path.DirectorySeparatorChar;
			iconLargePath = cachedMetaPathDir + directorySeparatorChar + "IconPreview_Large";
			directorySeparatorChar = Path.DirectorySeparatorChar;
			iconPath = cachedMetaPathDir + directorySeparatorChar + "IconPreview";
		}

		public static void DeleteExternalTrackMeta(ulong externalId)
		{
			DB.Meta.Track track = s_baseModel.QueryAll<DB.Meta.Track>().Find((DB.Meta.Track track2) => track2.externalId == externalId);
			if (track != null && track.external)
			{
				s_baseModel.Delete(track);
				s_baseModel.Reload();
				s_prefs.customCameras.RemoveTrackCustomCameras(track.id);
				GameLog.Print(AlertLevel.Notify, "ModMaps", $"Delete db item {track.externalId} {track.name}");
			}
		}

		public static async UniTask<bool> TryLoadSceneBundleAsyncBy(ulong externalId, byte serviceId, Action<float> progressFill)
		{
			ReleaseLoadedAssetBundle();
			if (!(UGCServiceProvider.instance.GetManagerBy(IUGCManager.ContentType.Tracks, (IUGCManager.ServiceProvider)serviceId).TryGetInstalledItem(externalId) is IUGCInstallable ugcItem))
			{
				return false;
			}
			string sceneBundleName = GetSceneBundleName(ugcItem);
			if (string.IsNullOrEmpty(sceneBundleName))
			{
				return false;
			}
			s_loadedSceneAssetBundle = await LoadBundleByPackageItemAsync(sceneBundleName, ugcItem, progressFill);
			if (s_loadedSceneAssetBundle == null)
			{
				return false;
			}
			s_loadedSceneMeta = ugcItem as IUGCMeta;
			return true;
		}

		private static void ReleaseBundleOnStateActivateHandler(BaseState baseState)
		{
			if (baseState is GarageGUIState)
			{
				ReleaseLoadedAssetBundle();
			}
		}

		public static void ReleaseLoadedAssetBundle()
		{
			if (s_loadedSceneAssetBundle != null)
			{
				s_loadedSceneAssetBundle.Unload(unloadAllLoadedObjects: true);
				s_loadedSceneMeta = null;
			}
		}

		private static async UniTask<AssetBundle> LoadBundleByPackageItemAsync<T>(string bundleName, T ugcItem, Action<float> progressFill = null) where T : IUGCInstallable
		{
			string assetPathFromPackage = GetAssetPathFromPackage(bundleName, ugcItem);
			isLoadingSceneBundle = true;
			AssetBundleCreateRequest requestAsset = AssetBundle.LoadFromFileAsync(assetPathFromPackage);
			while (!requestAsset.isDone)
			{
				progressFill.SafeCall(requestAsset.progress);
				await UniTask.Yield();
			}
			isLoadingSceneBundle = false;
			return requestAsset.assetBundle;
		}

		public static void LaunchSceneByItem<T>(T itemWithMeta) where T : UGCItem
		{
			DB.Meta.Track track = TryGetMetaByExternalId(itemWithMeta.id);
			if (track != null)
			{
				s_prefs.stats.SetLastRaceModeForTrack(track.id, RaceMode.Freeride);
				s_prefs.globals.lastTrackId = track.id;
				States.State<LoadRaceState>().LoadLocalRace();
				SoundController.PlayFX(FXSample.RaceStartSound);
			}
		}

		public static void CheckVersionCompatibility<T>(T itemWithMeta) where T : IUGCMeta
		{
			if (itemWithMeta?.targetClientVersion == "2.21.0")
			{
				GUICommonGodVoice.ShowText("Map is created for an old version, there may be issues");
			}
		}

		public static ulong GetDirectorySize(DirectoryInfo directory)
		{
			ulong num = 0uL;
			FileInfo[] files = directory.GetFiles();
			foreach (FileInfo fileInfo in files)
			{
				num += (ulong)fileInfo.Length;
			}
			return num;
		}

		private static string GetAssetPathFromPackage<T>(string assetName, T ugcItem) where T : IUGCInstallable
		{
			string directory = ugcItem.directory;
			char directorySeparatorChar = Path.DirectorySeparatorChar;
			return directory + directorySeparatorChar + assetName;
		}

		private static int GetMaxSortIdFrom(List<DB.Meta.Track> trackList)
		{
			int num = int.MinValue;
			foreach (DB.Meta.Track track in trackList)
			{
				if (track.sortId > num)
				{
					num = track.sortId;
				}
			}
			return num;
		}

		private static string GetCachedMetaPathDir<T>(T packageItem) where T : IUGCInstallable
		{
			string directory = packageItem.directory;
			char directorySeparatorChar = Path.DirectorySeparatorChar;
			return directory + directorySeparatorChar + "metaCached";
		}

		private static string GetSceneBundleName<T>(T ugcItem) where T : IUGCInstallable
		{
			string[] files = Directory.GetFiles(ugcItem.directory, "*.bundle", SearchOption.TopDirectoryOnly);
			if (files.Length == 0)
			{
				return null;
			}
			return Path.GetFileName(files[0]);
		}

		private static DB.Meta.Track CreateDefaultTrackMeta<T>(T ugcItem, List<DB.Meta.Track> tracks) where T : UGCItem, IUGCMeta, IUGCInstallable
		{
			GetCachedIconsPath(ugcItem, out var iconPath, out var iconLargePath);
			return new DB.Meta.Track
			{
				requiredLevel = 0,
				enabled = true,
				name = ugcItem.title,
				configuration = "Default",
				cost = 0,
				coinsFactor = 1f,
				defaultLocked = false,
				totalTimeLimit = 300,
				sortId = GetMaxSortIdFrom(tracks) + 1,
				dlc = string.Empty,
				hidden = false,
				external = true,
				externalId = ugcItem.id,
				description = ugcItem.description,
				ugcServiceId = (byte)ugcItem.serviceId,
				icon = iconPath,
				largeIcon = iconLargePath
			};
		}
	}
}
