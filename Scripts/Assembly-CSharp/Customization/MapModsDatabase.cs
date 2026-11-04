using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SimpleJSON;
using SimpleXML;
using SyncMultiplayer;
using TrackLayout.Data;
using TriLib;
using UnityEngine;

namespace Customization
{
	public static class MapModsDatabase
	{
		public class CustomMap
		{
			public ulong packageID;

			public string mapName;

			public string basepath;

			public string[] fileNames;

			public TrackLayoutData defaultConfig;

			public List<TrackLayoutData> configs = new List<TrackLayoutData>();
		}

		private static AssetLoaderAsync m_asyncLoader = new AssetLoaderAsync();

		public static Dictionary<ulong, CustomMap> database = new Dictionary<ulong, CustomMap>();

		public static TrackLayoutData GetDefaultLayout(ulong id)
		{
			if (database.Count == 0)
			{
				return null;
			}
			if (!database.TryGetValue(id, out var value))
			{
				return null;
			}
			return value.defaultConfig;
		}

		public static IEnumerator Instantiate(ulong id)
		{
			if (database.Count == 0 || !database.TryGetValue(id, out var info))
			{
				yield break;
			}
			AssetLoaderOptions options = ScriptableObject.CreateInstance<AssetLoaderOptions>();
			options.UseCutoutMaterials = false;
			GameObject loadPlace = new GameObject("Map");
			string[] fileNames = info.fileNames;
			foreach (string text in fileNames)
			{
				GameObject obj = null;
				Task task = m_asyncLoader.LoadFromFile(info.basepath + text, options, loadPlace, delegate(GameObject x)
				{
					obj = x;
				});
				while (!task.IsCompleted)
				{
					yield return null;
				}
				DoPostInstantiateJob(info, obj);
				LoadRaceState loadState;
				if (States.mainCurrent is BaseRaceModeState)
				{
					DoPostRaceStartJob(info, obj);
				}
				else if (States.mainCurrent is LoadRaceState)
				{
					loadState = States.mainCurrent as LoadRaceState;
					LoadRaceState loadRaceState = loadState;
					loadRaceState.onRaceStateActivatedEvent = (Action<BaseState>)Delegate.Combine(loadRaceState.onRaceStateActivatedEvent, new Action<BaseState>(job));
				}
				void job(BaseState s)
				{
					LoadRaceState loadRaceState2 = loadState;
					loadRaceState2.onRaceStateActivatedEvent = (Action<BaseState>)Delegate.Remove(loadRaceState2.onRaceStateActivatedEvent, new Action<BaseState>(job));
					DoPostRaceStartJob(info, obj);
				}
			}
		}

		private static void DoPostInstantiateJob(CustomMap info, GameObject obj)
		{
			ConfigurationManager.instance.InitializeCustomConfigs(RaceMode.NetworkFreeride, info.configs);
			Renderer[] componentsInChildren = obj.GetComponentsInChildren<Renderer>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				GameObject gameObject = componentsInChildren[i].gameObject;
				gameObject.AddComponent<MeshCollider>();
				if (gameObject.name.ToLower().Contains("road"))
				{
					gameObject.AddComponent<CARXSurface>().template = CARXSurfaceManager.Get("asphalt");
					gameObject.layer = 11;
				}
				if (gameObject.name.ToLower().Contains("curb"))
				{
					gameObject.AddComponent<CARXSurface>().template = CARXSurfaceManager.Get("kerb_racetrack");
					gameObject.layer = 11;
				}
				if (gameObject.name.ToLower().Contains("grass"))
				{
					gameObject.AddComponent<CARXSurface>().template = CARXSurfaceManager.Get("grass");
					gameObject.layer = 11;
				}
			}
		}

		private static void DoPostRaceStartJob(CustomMap info, GameObject obj)
		{
		}

		public static void Load()
		{
			LoadLocalExternals();
			GameConsole.SetCmd("net_start", delegate
			{
				if (GameConsole.cmdArgs.Length != 2)
				{
					GameConsole.Print("Map ID needed!");
				}
				else
				{
					ulong externalId = ulong.Parse(GameConsole.cmdArgs[1]);
					NetworkController.InstanceGame.CreateRoom(new RoomSettings(-1, externalId, "", "-=DEV=-", "mod track", 0, 0, nightMode: false, 0, crossPlay: true, "", 0, 0, 0, 0, 0, 0, 0));
				}
			});
			GameConsole.SetCmd("net_maplist", delegate
			{
				foreach (KeyValuePair<ulong, CustomMap> item in database)
				{
					GameConsole.Print(item.Key + " " + item.Value.mapName);
				}
			});
		}

		private static void LoadLocalExternals()
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(Application.dataPath + "/../Maps/");
			if (!directoryInfo.Exists)
			{
				return;
			}
			FileInfo[] files = directoryInfo.GetFiles("map.xml", SearchOption.AllDirectories);
			int num = 0;
			FileInfo[] array = files;
			foreach (FileInfo fileInfo in array)
			{
				StreamReader streamReader = new StreamReader(fileInfo.FullName);
				string obj = fileInfo.Directory?.ToString();
				char directorySeparatorChar = Path.DirectorySeparatorChar;
				string path = obj + directorySeparatorChar + "pid.txt";
				ulong result = 0uL;
				if (File.Exists(path))
				{
					ulong.TryParse(File.ReadAllText(path), out result);
				}
				AddFromXML(streamReader.ReadToEnd(), (ulong)(++num), fileInfo.Directory, result);
			}
		}

		public static void AddFromXML(string data, ulong externalBaseId = 0uL, DirectoryInfo externalPath = null, ulong workshopID = 0uL)
		{
			XMLDoc xMLDoc = new XMLDoc(data, buildKeyValues: false);
			CustomMap customMap = new CustomMap();
			customMap.mapName = externalPath.Name;
			string obj = externalPath?.ToString();
			char directorySeparatorChar = Path.DirectorySeparatorChar;
			customMap.basepath = obj + directorySeparatorChar;
			customMap.fileNames = xMLDoc.GetAttribDef("files", "map.obj").Split(',');
			customMap.packageID = ((workshopID == 0L) ? xMLDoc.GetAttribDef("id", externalBaseId) : workshopID);
			if (customMap.packageID == 0L)
			{
				customMap.packageID = externalBaseId;
			}
			database[customMap.packageID] = customMap;
			foreach (XMLDoc child in xMLDoc.childs)
			{
				if (!(child.nodeName == "cfg"))
				{
					continue;
				}
				string obj2 = externalPath?.ToString();
				directorySeparatorChar = Path.DirectorySeparatorChar;
				string path = obj2 + directorySeparatorChar + child.GetAttribDef("json", "");
				if (!File.Exists(path))
				{
					continue;
				}
				string aJSON = File.ReadAllText(path);
				TrackLayoutData trackLayoutData = ScriptableObject.CreateInstance<TrackLayoutData>();
				try
				{
					JSON.Parse(aJSON, trackLayoutData);
				}
				catch
				{
				}
				if ((bool)trackLayoutData)
				{
					trackLayoutData.name = child.GetAttribDef("name", "Unknown");
					customMap.configs.Add(trackLayoutData);
					if (trackLayoutData.name.ToLower() == "default")
					{
						customMap.defaultConfig = trackLayoutData;
					}
				}
			}
		}

		private static Vector3 ReadVector3Data(XMLDoc node)
		{
			return new Vector3(node.GetAttribDef("x", 0f), node.GetAttribDef("y", 0f), node.GetAttribDef("z", 0f));
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Initialize()
		{
			GameEvents.OnGameEngineStart.Subscribe(GameEventFlag.OneCall, delegate
			{
				Load();
			});
		}
	}
}
