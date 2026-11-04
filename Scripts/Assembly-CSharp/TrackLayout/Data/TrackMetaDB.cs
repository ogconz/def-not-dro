using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using ArrayExtension;
using DB;
using DB.Meta;
using DI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace TrackLayout.Data
{
	public static class TrackMetaDB
	{
		private static RaceMode[] m_modesForScan = new RaceMode[6]
		{
			RaceMode.XDS,
			RaceMode.Drift,
			RaceMode.TimeAttack,
			RaceMode.Freeride,
			RaceMode.HalloweenEvent,
			RaceMode.Top32
		};

		private static Dictionary<string, Dictionary<RaceMode, List<TrackMetaConfig>>> m_db = new Dictionary<string, Dictionary<RaceMode, List<TrackMetaConfig>>>();

		private static readonly int WinterfellId = 23;

		private static readonly int WinterfellReverseId = 24;

		private static readonly int RedRingWinterId = 37;

		private static readonly int RedRingWinterReverseId = 38;

		private static readonly int[] wintersTracksIds = new int[4] { WinterfellId, WinterfellReverseId, RedRingWinterId, RedRingWinterReverseId };

		private static readonly int[] wintersTracksWithIceIds = new int[2] { RedRingWinterId, RedRingWinterReverseId };

		private static Dictionary<string, List<IResourceLocation>> m_resources = new Dictionary<string, List<IResourceLocation>>();

		private static List<IResourceLocation> m_resourceLocations = new List<IResourceLocation>();

		public static bool IsWinterTrack(int idTrack)
		{
			return wintersTracksIds.Contains(idTrack);
		}

		public static bool IsWinterTrackWithIce(int idTrack)
		{
			return wintersTracksWithIceIds.Contains(idTrack);
		}

		private static string GetModePath(string scene, RaceMode mode)
		{
			return $"Assets/Settings/Tracks/{scene}/{mode}/Meta/";
		}

		public static List<TrackMetaConfig> GetConfigList(string scene, RaceMode mode)
		{
			if (!m_db.TryGetValue(scene, out var value))
			{
				return new List<TrackMetaConfig>();
			}
			if (!value.TryGetValue(mode, out var value2))
			{
				return new List<TrackMetaConfig>();
			}
			if (value2.Count == 0)
			{
				LoadConfig(GetModePath(scene, mode), ref value2);
				value2.Sort((TrackMetaConfig x, TrackMetaConfig y) => x.name.CompareTo(y.name));
			}
			return value2;
		}

		public static bool HaveConfig(string scene, RaceMode mode)
		{
			if (!m_db.TryGetValue(scene, out var value))
			{
				return false;
			}
			if (!value.TryGetValue(mode, out var value2))
			{
				return false;
			}
			if (value2.Count > 0)
			{
				return true;
			}
			string modePath = GetModePath(scene, mode);
			if (m_resources.TryGetValue(modePath, out var value3))
			{
				return value3.Count > 0;
			}
			return false;
		}

		public static TrackMetaConfig GetConfig(string scene, string configName, RaceMode mode)
		{
			List<TrackMetaConfig> configList = GetConfigList(scene, mode);
			if (configList.Count == 0)
			{
				return null;
			}
			configName = configName.ToLower();
			foreach (TrackMetaConfig item in configList)
			{
				if (item.data != null && item.data.name.ToLower() == configName)
				{
					return item;
				}
			}
			return null;
		}

		public static IEnumerator Initialize()
		{
			float startTime = Time.realtimeSinceStartup;
			yield return InitializeResources();
			List<DB.Meta.Track> list = DependencyInjector.Resolve<BaseModel>().QueryAll<DB.Meta.Track>();
			Stopwatch stopWatch = new Stopwatch();
			stopWatch.Restart();
			foreach (DB.Meta.Track item in list)
			{
				if (!item.enabled || item.hidden || item.configuration != "Default")
				{
					continue;
				}
				Dictionary<RaceMode, List<TrackMetaConfig>> rmList = new Dictionary<RaceMode, List<TrackMetaConfig>>();
				m_db[item.scene] = rmList;
				RaceMode[] modesForScan = m_modesForScan;
				foreach (RaceMode key in modesForScan)
				{
					rmList[key] = new List<TrackMetaConfig>();
					if (stopWatch.ElapsedMilliseconds > 50)
					{
						UnityEngine.Debug.Log(">>> mis frame");
						yield return null;
						stopWatch.Restart();
					}
				}
			}
			float num = Time.realtimeSinceStartup - startTime;
			GameLog.Print(AlertLevel.Notify, "System", $"Done Parse Track db in {num}  sec.");
		}

		private static IEnumerator InitializeResources()
		{
			AsyncOperationHandle<IList<IResourceLocation>> handle = Addressables.LoadResourceLocationsAsync("TracksMeta");
			yield return handle;
			foreach (IResourceLocation item in handle.Result)
			{
				string internalId = item.InternalId;
				int num = internalId.LastIndexOf('/') + 1;
				internalId = internalId.Remove(num, internalId.Length - num);
				if (!m_resources.TryGetValue(internalId, out var value))
				{
					value = new List<IResourceLocation>();
					m_resources.Add(internalId, value);
				}
				value.Add(item);
			}
		}

		private static bool LoadConfig<T>(string path, ref List<T> result)
		{
			m_resourceLocations.Clear();
			if (m_resources.TryGetValue(path, out var value))
			{
				int? num = value?.Count;
				for (int i = 0; i < num; i++)
				{
					IResourceLocation resourceLocation = value[i];
					if (!(resourceLocation.ResourceType != typeof(T)))
					{
						m_resourceLocations.Add(resourceLocation);
					}
				}
				AsyncOperationHandle<IList<T>> asyncOperationHandle = Addressables.LoadAssetsAsync<T>(m_resourceLocations, null);
				result.AddRange(asyncOperationHandle.WaitForCompletion());
				return true;
			}
			return false;
		}

		public static List<T> LoadConfig<T>(string path)
		{
			List<T> result = new List<T>();
			LoadConfig(path, ref result);
			return result;
		}
	}
}
