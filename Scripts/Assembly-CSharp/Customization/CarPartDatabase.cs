using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DB;
using DB.Meta;
using DI;
using GameOverlay;
using Services.UGC.Interfaces;
using Services.UGC.Utils;
using SimpleXML;
using UnityEngine;

namespace Customization
{
	public static class CarPartDatabase
	{
		public class SlotInfo
		{
			public CarPart defaultValue;

			public CarPart value;

			public bool enabled = true;

			public bool isAlive;
		}

		public class SlotScheme
		{
			public HashSet<int> baseList = new HashSet<int>();

			public HashSet<int> blackList = new HashSet<int>();

			public Dictionary<int, SlotInfo> slotMap = new Dictionary<int, SlotInfo>();

			public bool blocked;

			public CarPart hostCore { get; private set; }

			public int hostCarId { get; private set; }

			public SlotScheme(int carId, IEnumerable<int> parts)
			{
				hostCore = null;
				hostCarId = carId;
				List<CarPart> list = ConvertToPartList(parts);
				int carCoreId = GetCarCoreId(hostCarId);
				if (carCoreId != -1)
				{
					hostCore = GetPartById(carCoreId);
					if (!list.Contains(hostCore))
					{
						list.Add(hostCore);
					}
					foreach (int item in hostCore.include)
					{
						baseList.Add(item);
					}
				}
				foreach (CarPart item2 in list)
				{
					foreach (CarPart.SlotPart item3 in item2.enable)
					{
						SetSlotDefault(item3.slotId, GetPartById(item3.partId));
					}
					foreach (int item4 in item2.take)
					{
						ActivateSlot(item4, item2, null);
					}
				}
				foreach (KeyValuePair<int, SlotInfo> item5 in slotMap)
				{
					if (item5.Value.defaultValue == null)
					{
						DeactivateSlot(item5.Key);
					}
				}
				foreach (CarPart item6 in list)
				{
					foreach (int item7 in item6.disable)
					{
						DeactivateSlot(item7);
					}
				}
				CheckLock();
			}

			public void CheckLock()
			{
				blocked = false;
				if (hostCore == null)
				{
					return;
				}
				SlotInfo value = null;
				if (!slotMap.TryGetValue(SLOT_ID_CHASSIS, out value) || value.value == null)
				{
					return;
				}
				int num = hostCore.enable.FindIndex((CarPart.SlotPart slot) => slot.slotId == SLOT_ID_CHASSIS);
				if (num != -1)
				{
					CarPart partById = GetPartById(hostCore.enable[num].partId);
					if (partById != null)
					{
						blocked = partById == value.value;
					}
				}
			}

			private void Equip(CarPart part, bool ignoreForceParts, ref int reqursionGuard)
			{
				if (part == null)
				{
					return;
				}
				if (reqursionGuard-- <= 0)
				{
					Debug.LogWarning($"Equip CarPart {part.nativeid} failed. Recursion detected!");
					return;
				}
				CheckAndReleasePartFromBlackList(part, ref reqursionGuard);
				foreach (int item in part.take)
				{
					SlotInfo slot = GetSlot(item);
					if (slot != null && slot.enabled)
					{
						if (slot.value != null)
						{
							DismountPart(slot.value);
						}
						ActivateSlot(item, part, null);
					}
				}
				FlushStates();
				BuildLiveStates();
				foreach (KeyValuePair<int, SlotInfo> item2 in slotMap)
				{
					if (item2.Value.isAlive && item2.Value.enabled)
					{
						if (item2.Value.value == null || item2.Value.value.disable == null)
						{
							continue;
						}
						foreach (int item3 in item2.Value.value.disable)
						{
							SlotInfo slot2 = GetSlot(item3);
							if (slot2 != null)
							{
								slot2.enabled = false;
							}
						}
					}
					else
					{
						item2.Value.enabled = false;
					}
				}
				if (!ignoreForceParts)
				{
					foreach (int item4 in part.force)
					{
						Equip(item4);
					}
				}
				CleanUpBlackList();
				CheckLock();
			}

			public void Equip(int partId, bool ignoreForceParts = false)
			{
				int reqursionGuard = 10;
				Equip(GetPartById(partId), ignoreForceParts, ref reqursionGuard);
			}

			private void CheckAndReleasePartFromBlackList(CarPart part, ref int reqursionGuard)
			{
				if (blackList.Count <= 0)
				{
					return;
				}
				foreach (KeyValuePair<int, SlotInfo> item in slotMap)
				{
					if (!item.Value.enabled || item.Value.value == null)
					{
						continue;
					}
					CarPart value = item.Value.value;
					if (!item.Value.value.exclude.Contains(part.id))
					{
						continue;
					}
					if (part.include.Count > 0)
					{
						foreach (int item2 in part.include)
						{
							CarPart partById = GetPartById(item2);
							if (partById.type == value.type)
							{
								Equip(partById, ignoreForceParts: false, ref reqursionGuard);
								break;
							}
						}
						continue;
					}
					foreach (int @base in baseList)
					{
						CarPart partById2 = GetPartById(@base);
						if (partById2.type == value.type && !partById2.exclude.Contains(part.id))
						{
							Equip(partById2, ignoreForceParts: false, ref reqursionGuard);
						}
					}
				}
			}

			public void CleanUpBlackList()
			{
				if (blackList.Count <= 0)
				{
					return;
				}
				foreach (KeyValuePair<int, SlotInfo> item in slotMap)
				{
					if (!item.Value.enabled || item.Value.value == null || !blackList.Contains(item.Value.value.id))
					{
						continue;
					}
					if (item.Value.defaultValue != null && !blackList.Contains(item.Value.defaultValue.id))
					{
						Equip(item.Value.defaultValue.id);
						continue;
					}
					foreach (int @base in baseList)
					{
						CarPart partById = GetPartById(@base);
						if (item.Value.value.type == partById.type && !blackList.Contains(@base))
						{
							int reqursionGuard = 10;
							Equip(partById, ignoreForceParts: false, ref reqursionGuard);
							break;
						}
					}
				}
			}

			public void FlushStates()
			{
				foreach (SlotInfo value in slotMap.Values)
				{
					value.isAlive = false;
				}
			}

			public void BuildLiveStates()
			{
				Stack<int> stack = new Stack<int>(GetPartById(GetCarCoreId(hostCarId)).enable.ConvertAll((CarPart.SlotPart e) => e.slotId));
				while (stack.Count > 0)
				{
					int key = stack.Pop();
					if (!slotMap.TryGetValue(key, out var value) || !value.enabled)
					{
						continue;
					}
					value.isAlive = true;
					if (value.value == null)
					{
						continue;
					}
					foreach (CarPart.SlotPart item in value.value.enable)
					{
						stack.Push(item.slotId);
					}
				}
			}

			public HashSet<int> ToPartList()
			{
				HashSet<int> hashSet = new HashSet<int>();
				if (blocked)
				{
					EquipDefaults(hostCarId, hashSet);
					return hashSet;
				}
				foreach (SlotInfo value in slotMap.Values)
				{
					if (value.enabled && value.value != null)
					{
						hashSet.Add(value.value.id);
					}
				}
				return hashSet;
			}

			public void ToPartList(HashSet<int> parts, bool forceAdd = false)
			{
				if (!forceAdd && blocked)
				{
					EquipDefaults(hostCarId, parts);
					return;
				}
				parts.Clear();
				foreach (SlotInfo value in slotMap.Values)
				{
					if (value.enabled && value.value != null)
					{
						parts.Add(value.value.id);
					}
				}
			}

			public void SetSlotDefault(int slotId, CarPart defaultValue)
			{
				GetOrAddSlot(slotId).defaultValue = defaultValue;
			}

			public void ActivateSlot(int slotId, CarPart value, CarPart defaultValue)
			{
				SlotInfo orAddSlot = GetOrAddSlot(slotId);
				if (value != null)
				{
					orAddSlot.value = value;
				}
				if (defaultValue != null)
				{
					orAddSlot.defaultValue = defaultValue;
				}
				if (orAddSlot.value == null)
				{
					orAddSlot.value = orAddSlot.defaultValue;
				}
				if (orAddSlot.value == null)
				{
					orAddSlot.enabled = false;
					return;
				}
				orAddSlot.enabled = true;
				foreach (int item in orAddSlot.value.disable)
				{
					DeactivateSlot(item);
				}
				foreach (CarPart.SlotPart item2 in orAddSlot.value.enable)
				{
					ActivateSlot(item2.slotId, null, GetPartById(item2.partId));
				}
				foreach (int item3 in orAddSlot.value.exclude)
				{
					blackList.Add(item3);
				}
				if (slotId != SLOT_ID_CHASSIS)
				{
					return;
				}
				foreach (int item4 in orAddSlot.value.include)
				{
					baseList.Add(item4);
				}
			}

			public void DeactivateSlot(int slotId)
			{
				SlotInfo orAddSlot = GetOrAddSlot(slotId);
				if (!orAddSlot.enabled)
				{
					return;
				}
				orAddSlot.enabled = false;
				if (orAddSlot.value == null)
				{
					return;
				}
				foreach (int item in orAddSlot.value.disable)
				{
					ActivateSlot(item, null, null);
				}
				foreach (CarPart.SlotPart item2 in orAddSlot.value.enable)
				{
					DeactivateSlot(item2.slotId);
				}
				foreach (int item3 in orAddSlot.value.exclude)
				{
					blackList.Remove(item3);
				}
				if (slotId != SLOT_ID_CHASSIS)
				{
					return;
				}
				foreach (int item4 in orAddSlot.value.include)
				{
					baseList.Remove(item4);
				}
			}

			public void DismountPart(CarPart part)
			{
				if (part == null)
				{
					return;
				}
				foreach (CarPart.SlotPart item in part.enable)
				{
					DeactivateSlot(item.slotId);
				}
				foreach (int item2 in part.disable)
				{
					ActivateSlot(item2, null, null);
				}
				foreach (int item3 in part.exclude)
				{
					blackList.Remove(item3);
				}
				if (!part.take.Contains(SLOT_ID_CHASSIS))
				{
					return;
				}
				foreach (int item4 in part.include)
				{
					baseList.Remove(item4);
				}
			}

			public SlotInfo GetOrAddSlot(int slotId)
			{
				if (slotMap.TryGetValue(slotId, out var value))
				{
					return value;
				}
				value = new SlotInfo();
				slotMap.Add(slotId, value);
				return value;
			}

			public SlotInfo GetSlot(int slotId)
			{
				slotMap.TryGetValue(slotId, out var value);
				return value;
			}
		}

		public const string Wheel = "wheel";

		public const string Tires = "tires";

		public const string Chassis = "chassis";

		public const string ChassisCore = "core";

		public const string LightsFront = "lightsfront";

		public const string LightsRear = "lightsrear";

		public const string BumperFront = "bumperfront";

		public const string BumperRear = "bumperrear";

		public const string Bonnet = "bonnet";

		public const string Wing = "wing";

		public const string Roof = "roof";

		public const string Mirrors = "mirrors";

		public const string RearMirrors = "mirror";

		public const string Doors = "doors";

		public const string Cage = "cage";

		public const string Skirts = "skirts";

		public const string Trunk = "trunk";

		public const string Spoiler = "spoiler";

		public const string Exhaust = "exhaust";

		public const string Handbrake = "handbrake";

		public const string SeatLeft = "seatleft";

		public const string SeatRight = "seatright";

		public const string Shifter = "shifter";

		public const string SteeringWheel = "steeringwheel";

		public const string Window = "window";

		public const string Interior = "interior";

		public const string Path = "DB\\Parts\\";

		public const string PathOverrides = "DB\\PartsOverrides\\";

		public const int PerCarScopeSize = 1000;

		private static readonly HashSet<string> AvailablePartTypes = new HashSet<string>
		{
			"wing", "doors", "exhaust", "trunk", "lightsrear", "bumperfront", "mirrors", "bonnet", "spoiler", "lightsfront",
			"seatleft", "wheel", "seatright", "steeringwheel", "handbrake", "shifter", "interior", "skirts", "bumperrear", "cage",
			"chassis", "core", "roof", "mirror"
		};

		private static List<int> m_badParts = new List<int>();

		public static Dictionary<int, CarPart> database = new Dictionary<int, CarPart>();

		public static Dictionary<ulong, CarPartExternalDatabase> externalDatabases = new Dictionary<ulong, CarPartExternalDatabase>();

		private static Dictionary<string, List<CarPart>> m_typeIndex = new Dictionary<string, List<CarPart>>();

		private static int m_slotIDCounter = 0;

		private static Dictionary<string, int> m_slotMap = new Dictionary<string, int>();

		private static Dictionary<int, int> m_carCores = new Dictionary<int, int>();

		private static GamePrefs m_prefs;

		private static HashSet<int> m_unlockedCarsByDLC = null;

		private static HashSet<int> m_unlockedCarsByEvent = null;

		private static List<CarPart> m_carBodyList = new List<CarPart>();

		private static DLCCode[] m_skipDLCs = new DLCCode[2]
		{
			DLCCode.Unknown,
			DLCCode.SouthAmericaStyle
		};

		private static List<string> m_ignoredNodeNames = new List<string> { "add" };

		private static DLCCode[] m_consoleAllowedDLCParts = new DLCCode[11]
		{
			DLCCode.Police,
			DLCCode.SouthAmericaStyle,
			DLCCode.StreetTuners,
			DLCCode.HitTheWall,
			DLCCode.SayakaS15,
			DLCCode.YoungTimers,
			DLCCode.Sayaka180SX,
			DLCCode.SayakaRX7,
			DLCCode.WildUnion,
			DLCCode.MeowtorMadness,
			DLCCode.FreshX
		};

		private static int SLOT_ID_CORE = -1;

		private static int SLOT_ID_INTERIOR = -1;

		private static int SLOT_ID_CHASSIS = -1;

		private static Dictionary<string, int> m_categoriesOrder = new Dictionary<string, int>
		{
			{ "core", 0 },
			{ "chassis", 1 },
			{ "lightsfront", 2 },
			{ "bumperfront", 3 },
			{ "bonnet", 4 },
			{ "wing", 5 },
			{ "roof", 6 },
			{ "mirrors", 7 },
			{ "doors", 8 },
			{ "cage", 9 },
			{ "skirts", 10 },
			{ "trunk", 11 },
			{ "bumperrear", 12 },
			{ "spoiler", 13 },
			{ "lightsrear", 14 },
			{ "exhaust", 15 },
			{ "seatleft", 16 },
			{ "seatright", 17 },
			{ "steeringwheel", 18 },
			{ "handbrake", 19 },
			{ "shifter", 20 }
		};

		public static bool isReadyForLoadExternals { get; private set; } = false;

		internal static void ImportLocalXMLToAsset(CarPartsAsset carPartsAsset)
		{
			if (Application.isPlaying)
			{
				Debug.LogError("Export to Asset only in non play mode!");
			}
			ClearDatabases();
			LoadInternalAssetXMLs();
			Dictionary<int, CarPart>.ValueCollection values = database.Values;
			carPartsAsset.partsDB.Clear();
			foreach (CarPart item3 in values)
			{
				if (!(item3.type == "chassis"))
				{
					if (!AvailablePartTypes.Contains(item3.type))
					{
						m_badParts.Add(item3.id);
					}
					else
					{
						carPartsAsset.partsDB.Add(item3);
					}
				}
			}
			foreach (CarPart item4 in values)
			{
				if (!(item4.type == "chassis"))
				{
					continue;
				}
				if (m_badParts.Count > 0)
				{
					for (int num = item4.force.Count - 1; num >= 0; num--)
					{
						int item = item4.force[num];
						if (m_badParts.Contains(item))
						{
							item4.force.Remove(item);
						}
					}
					for (int num2 = item4.include.Count - 1; num2 >= 0; num2--)
					{
						int item2 = item4.include[num2];
						if (m_badParts.Contains(item2))
						{
							item4.include.Remove(item2);
						}
					}
					for (int num3 = item4.enable.Count - 1; num3 >= 0; num3--)
					{
						CarPart.SlotPart slotPart = item4.enable[num3];
						if (m_badParts.Contains(slotPart.partId))
						{
							item4.enable.RemoveAt(num3);
						}
					}
					carPartsAsset.partsDB.Add(item4);
				}
				else
				{
					carPartsAsset.partsDB.Add(item4);
				}
			}
			foreach (CarPart item5 in carPartsAsset.partsDB)
			{
				item5.prefab = CheckIgnoreAndToLewer(item5.prefab);
				foreach (CarPart.SubPart submodel in item5.submodels)
				{
					submodel.prefab = CheckIgnoreAndToLewer(submodel.prefab);
				}
				if (item5.cmds == null)
				{
					continue;
				}
				foreach (CarPartCommand cmd in item5.cmds)
				{
					cmd.valueS = cmd.valueS.Replace('\\', '/');
				}
			}
			carPartsAsset.slotIdCounter = m_slotIDCounter;
			carPartsAsset.slotMap.dictionary = m_slotMap;
		}

		private static string CheckIgnoreAndToLewer(string prefabName)
		{
			return prefabName.ToLower();
		}

		public static void ImportExternalData<T>(T ugcItem) where T : UGCItem, IUGCInstallable
		{
			if (!externalDatabases.ContainsKey(ugcItem.id))
			{
				Debug.Log($"Loading UGC car with id: {ugcItem.id} from path: {ugcItem.directory}");
				string directory = ugcItem.directory;
				char directorySeparatorChar = System.IO.Path.DirectorySeparatorChar;
				string data = new StreamReader(directory + directorySeparatorChar + "car.xml").ReadToEnd();
				ulong id = ugcItem.id;
				string directory2 = ugcItem.directory;
				directorySeparatorChar = System.IO.Path.DirectorySeparatorChar;
				AddFromXML(data, id, directory2 + directorySeparatorChar, ugcItem.id, ugcItem.serviceId);
				ParseDisabledCores();
			}
		}

		public static void DeleteExternalData<T>(T ugcItem) where T : UGCItem, IUGCInstallable
		{
			RemoveFromXML(ugcItem.id);
			externalDatabases.Remove(ugcItem.id);
		}

		private static void RemoveFromXML(ulong externalBaseId)
		{
			if (!externalDatabases.TryGetValue(externalBaseId, out var value))
			{
				return;
			}
			foreach (KeyValuePair<int, CarPart> item in value.nativeParts)
			{
				database.Remove(item.Value.id);
				GetTypeIndex(item.Value.type).RemoveAll((CarPart carPart) => carPart.id == item.Value.id);
			}
		}

		public static bool IsCarBodypartsListContainsUGC(int carId)
		{
			CarVisualManager.VisualSettings visualForCar = m_prefs.carSettings.GetVisualForCar(carId);
			List<CarPart> carBodies = GetCarBodies(carId);
			foreach (int partId in visualForCar.parts)
			{
				if (carBodies == null)
				{
					break;
				}
				if (carBodies.Exists((CarPart part) => part.id == partId && part.external))
				{
					return true;
				}
			}
			return false;
		}

		public static void ReplaceExternalID(ulong oldID, ulong newID)
		{
			if (!externalDatabases.TryGetValue(oldID, out var value))
			{
				return;
			}
			externalDatabases.Remove(oldID);
			externalDatabases.Add(newID, value);
			foreach (KeyValuePair<int, CarPart> nativePart in value.nativeParts)
			{
				nativePart.Value.packetid = newID;
			}
			m_prefs.workshopInfo.MoveLocalID(oldID, newID);
		}

		private static void ClearDatabases()
		{
			database.Clear();
			externalDatabases.Clear();
			m_typeIndex.Clear();
			m_slotMap.Clear();
			m_slotIDCounter = 0;
			m_carCores.Clear();
			m_prefs = null;
			m_unlockedCarsByDLC = null;
			m_unlockedCarsByEvent = null;
		}

		public static bool IsCarUnlockedByDLC(int carId)
		{
			if (m_unlockedCarsByDLC == null)
			{
				m_unlockedCarsByDLC = new HashSet<int>();
				foreach (KeyValuePair<int, CarPart> item in database)
				{
					CarPart value = item.Value;
					if (value != null && !SkipAddUnlockedDLC(value.DLCCode) && Overlay.instance.IsDLCInstalled(value.DLCCode))
					{
						m_unlockedCarsByDLC.Add(value.carId);
					}
				}
			}
			return m_unlockedCarsByDLC.Contains(carId);
		}

		private static bool SkipAddUnlockedDLC(DLCCode dlcCode)
		{
			return Array.Exists(m_skipDLCs, (DLCCode skipDLC) => skipDLC == dlcCode);
		}

		public static void Load()
		{
			if (m_prefs == null)
			{
				m_prefs = DependencyInjector.Resolve<GamePrefs>();
			}
			if (database.Count <= 0)
			{
				if (StaticDictionariesHolder.AssetOnlyMode)
				{
					LoadFromAsset();
				}
				else
				{
					LoadInternalAssetXMLs();
				}
				LoadLocalExternals();
				DefineKitWheels();
				ApplyRuntime();
				ParseDisabledCores();
				isReadyForLoadExternals = true;
			}
		}

		private static void LoadFromAsset()
		{
			CarPartsAsset original = ResourcesWrapper.Load<CarPartsAsset>("DB/CarPartsAsset");
			original = UnityEngine.Object.Instantiate(original);
			foreach (CarPart item in original.partsDB)
			{
				database[item.id] = item;
				GetTypeIndex(item.type).Add(item);
			}
			m_slotIDCounter = original.slotIdCounter;
			m_slotMap = original.slotMap.dictionary;
			foreach (KeyValuePair<string, int> item2 in m_slotMap)
			{
				InitStaticSlots(item2.Key, item2.Value);
			}
		}

		private static void InitStaticSlots(string key, int value)
		{
			switch (key.ToLower())
			{
			case "core":
				SLOT_ID_CORE = value;
				break;
			case "interior":
				SLOT_ID_INTERIOR = value;
				break;
			case "chassis":
				SLOT_ID_CHASSIS = value;
				break;
			}
		}

		private static void ApplyRuntime()
		{
			BaseModel baseModel = DependencyInjector.Resolve<BaseModel>();
			foreach (CarPart value in database.Values)
			{
				if (value.type == "core")
				{
					m_carCores[value.carId] = value.id;
				}
				value.ApplyPrice(baseModel);
			}
		}

		private static void LoadInternalAssetXMLs()
		{
			TextAsset[] array = Resources.LoadAll<TextAsset>("DB\\Parts\\");
			for (int i = 0; i < array.Length; i++)
			{
				AddFromXML(array[i].text, 0uL, null, 0uL);
			}
			array = Resources.LoadAll<TextAsset>("DB\\PartsOverrides\\");
			for (int i = 0; i < array.Length; i++)
			{
				AddFromXML(array[i].text, 0uL, null, 0uL);
			}
		}

		private static void LoadLocalExternals()
		{
			if (m_prefs == null)
			{
				m_prefs = DependencyInjector.Resolve<GamePrefs>();
			}
			DirectoryInfo directoryInfo = new DirectoryInfo(Application.dataPath + "/../Cars/");
			if (!directoryInfo.Exists)
			{
				return;
			}
			FileInfo[] files = directoryInfo.GetFiles("car.xml", SearchOption.AllDirectories);
			int num = -1;
			FileInfo[] array = files;
			foreach (FileInfo fileInfo in array)
			{
				StreamReader streamReader = new StreamReader(fileInfo.FullName);
				string obj = fileInfo.Directory?.ToString();
				char directorySeparatorChar = System.IO.Path.DirectorySeparatorChar;
				string path = obj + directorySeparatorChar + "pid.txt";
				ulong result = 0uL;
				if (File.Exists(path))
				{
					ulong.TryParse(File.ReadAllText(path), out result);
				}
				Debug.Log($"Loading local UGC car from: '{fileInfo.Directory}', pid: {result}");
				ulong num2 = (ulong)(--num);
				string data = streamReader.ReadToEnd();
				string obj2 = fileInfo.Directory?.ToString();
				directorySeparatorChar = System.IO.Path.DirectorySeparatorChar;
				AddFromXML(data, num2, obj2 + directorySeparatorChar, result);
				GameEvents.UGCCarReady(num2);
				CarPartExternalLoader.CleanUp();
			}
			foreach (CarPartExternalDatabase value in externalDatabases.Values)
			{
				value.isLocal = true;
			}
		}

		public static void AddFromXML(string data, ulong externalBaseId = 0uL, string externalPath = null, ulong workshopID = 0uL, IUGCManager.ServiceProvider serviceProvider = IUGCManager.ServiceProvider.Void)
		{
			List<CarPart> list = new List<CarPart>();
			XMLDoc xMLDoc = new XMLDoc(data, buildKeyValues: false);
			char[] separator = new char[1] { ' ' };
			Dictionary<int, string[]> dictionary = new Dictionary<int, string[]>();
			int attribDef = xMLDoc.GetAttribDef("carid", -1);
			int num = ((attribDef != -1) ? (attribDef * 1000) : 0);
			externalBaseId = ((workshopID == 0L) ? xMLDoc.GetAttribDef("id", externalBaseId) : workshopID);
			if (externalBaseId != 0L)
			{
				int localID = m_prefs.workshopInfo.GetLocalID(externalBaseId);
				num = (1000 + localID) * 1000;
			}
			foreach (XMLDoc child in xMLDoc.childs)
			{
				if (externalBaseId != 0L)
				{
					foreach (XMLDoc item in child.childs.ToList())
					{
						if (m_ignoredNodeNames.Contains(item.nodeName))
						{
							child.childs.Remove(item);
						}
					}
				}
				CarPart carPart = new CarPart();
				carPart.type = child.nodeName;
				carPart.packetid = externalBaseId;
				carPart.ugcServiceProvider = serviceProvider;
				carPart.carId = child.GetAttribDef("carid", attribDef);
				if (child.attributes.ContainsKey("id"))
				{
					carPart.nativeid = child.GetAttribDef("id", -1);
					carPart.id = carPart.nativeid + num;
				}
				list.Add(carPart);
				carPart.vendor = child.GetAttribDef("vendor", string.Empty).ToLower();
				carPart.description = child.GetAttribDef("description", string.Empty);
				carPart.vinylsNotSupport = child.GetAttribDef("vinylsNotSupport", def: false);
				carPart.price = child.GetAttribDef("price", 0);
				carPart.overrideEmptyBonnetId = child.GetAttribDef("overrideEmptyBonnetId", -1);
				carPart.prefab = child.GetAttribDef("prefab", "");
				if (externalPath != null)
				{
					string attribDef2 = child.GetAttribDef("file", "");
					if (!string.IsNullOrEmpty(attribDef2))
					{
						carPart.external = true;
						carPart.prefab = externalPath + attribDef2;
					}
				}
				carPart.group = child.GetAttribDef("group", "");
				string[] array = child.GetAttribDef("enable", "").Split(separator, StringSplitOptions.RemoveEmptyEntries);
				if (array.Length != 0)
				{
					dictionary[carPart.id] = array;
				}
				ConvertSlots(child.GetAttribDef("disable", "").Split(separator, StringSplitOptions.RemoveEmptyEntries), carPart.disable);
				ConvertSlots(child.GetAttribDef("take", "").Split(separator, StringSplitOptions.RemoveEmptyEntries), carPart.take);
				ConvertParts(child.GetAttribDef("force", "").Split(separator, StringSplitOptions.RemoveEmptyEntries), carPart.force, num);
				ConvertParts(child.GetAttribDef("include", "").Split(separator, StringSplitOptions.RemoveEmptyEntries), carPart.include, num);
				ConvertParts(child.GetAttribDef("exclude", "").Split(separator, StringSplitOptions.RemoveEmptyEntries), carPart.exclude, num);
				carPart.cmds = CarPartCommand.Parse(child);
				carPart.colorOverrides = CarPartColorOverride.Parse(child);
				ReadSubModels(child, carPart.submodels);
				if (database.ContainsKey(carPart.id))
				{
					CarPart carPart2 = database[carPart.id];
					if (carPart2.prefab != carPart.prefab)
					{
						Debug.LogError($"New {carPart.prefab} CarPart id = {carPart.id} already used by CarPart with prefab {carPart2.prefab}. Please make it unique");
					}
					if (carPart?.disable != null && carPart2?.disable != null)
					{
						MergeIntList(ref carPart.disable, ref carPart2.disable);
					}
					if (carPart?.exclude != null && carPart2?.exclude != null)
					{
						MergeIntList(ref carPart.exclude, ref carPart2.exclude);
					}
					if (carPart?.force != null && carPart2?.force != null)
					{
						MergeIntList(ref carPart.force, ref carPart2.force);
					}
					if (carPart?.include != null && carPart2?.include != null)
					{
						MergeIntList(ref carPart.include, ref carPart2.include);
					}
					if (carPart?.enable != null && carPart2?.enable != null)
					{
						MergeSlotParts(ref carPart.enable, ref carPart2.enable);
					}
					if (carPart?.submodels != null)
					{
						carPart2?.submodels?.AddRange(carPart.submodels);
					}
					if (carPart.cmds != null && carPart2?.cmds != null)
					{
						MergeCMD(ref carPart.cmds, ref carPart2.cmds);
					}
				}
				else
				{
					database[carPart.id] = carPart;
					RegisterExternalPart(carPart);
					GetTypeIndex(carPart.type).Add(carPart);
				}
				string attribDef3 = child.GetAttribDef("dlc", "");
				if (string.IsNullOrEmpty(attribDef3))
				{
					continue;
				}
				try
				{
					DLCCode dLCCode = (DLCCode)Enum.Parse(typeof(DLCCode), attribDef3, ignoreCase: true);
					if (dLCCode == DLCCode.RuntimeDLC)
					{
						dLCCode = DLCCode.Unknown;
					}
					carPart.DLCCode = dLCCode;
				}
				catch
				{
				}
			}
			foreach (KeyValuePair<int, string[]> item2 in dictionary)
			{
				CarPart carPart3 = database[item2.Key];
				carPart3.enable.Clear();
				string[] value = item2.Value;
				for (int i = 0; i < value.Length; i++)
				{
					string[] array2 = value[i].Split('#');
					int result = -1;
					int.TryParse(array2[1], out result);
					CarPart carPart4 = INTERNAL_GetPartById(num + result);
					if (carPart4 == null)
					{
						GameLog.Print(AlertLevel.FatalError, "System", "CarPartDatabase: void node " + attribDef + " -> " + result);
					}
					else
					{
						carPart3.enable.Add(new CarPart.SlotPart(GetSlotID(array2[0]), carPart4.id));
					}
				}
			}
		}

		private static void MergeSlotParts(ref List<CarPart.SlotPart> from, ref List<CarPart.SlotPart> to)
		{
			foreach (CarPart.SlotPart itemFrom in from)
			{
				int num = to.FindIndex((CarPart.SlotPart x) => x.slotId == itemFrom.slotId);
				if (num < 0)
				{
					to.Add(itemFrom);
				}
				else
				{
					to[num].partId = itemFrom.partId;
				}
			}
		}

		private static void MergeIntList(ref List<int> from, ref List<int> to)
		{
			foreach (int item in from)
			{
				int num = to.IndexOf(item);
				if (num < 0)
				{
					to.Add(item);
				}
				else
				{
					to[num] = item;
				}
			}
		}

		private static void MergeCMD(ref List<CarPartCommand> from, ref List<CarPartCommand> to)
		{
			foreach (CarPartCommand itemFrom in from)
			{
				int num = to.FindIndex((CarPartCommand x) => x.type == itemFrom.type && x.target == itemFrom.target);
				if (num < 0)
				{
					to.Add(itemFrom);
				}
				else
				{
					to[num] = itemFrom;
				}
			}
		}

		private static void ReadSubModels(XMLDoc node, List<CarPart.SubPart> list)
		{
			int num = 0;
			string attribDef = node.GetAttribDef("model" + num++, "");
			while (!string.IsNullOrEmpty(attribDef))
			{
				string[] array = attribDef.Split('_');
				string prefab;
				string type;
				if (array.Length > 1)
				{
					prefab = attribDef;
					type = array[0].ToLower();
				}
				else
				{
					prefab = attribDef;
					type = "";
				}
				list.Add(new CarPart.SubPart
				{
					prefab = prefab,
					type = type
				});
				attribDef = node.GetAttribDef("model" + num++, "");
			}
		}

		private static void RegisterExternalPart(CarPart part)
		{
			if (part.packetid != 0L)
			{
				if (part.type == "chassis" && !part.take.Contains(SLOT_ID_CHASSIS))
				{
					part.type = "legacy_chassis";
				}
				CarPartExternalDatabase value = null;
				if (!externalDatabases.TryGetValue(part.packetid, out value))
				{
					value = new CarPartExternalDatabase();
					externalDatabases[part.packetid] = value;
				}
				value.nativeParts[part.nativeid] = part;
			}
		}

		private static void DefineKitWheels()
		{
			CarPartCommand.CarPartCommandTarget carPartCommandTarget = CarPartCommand.CarPartCommandTarget.frontWheel;
			CarPartCommand.CarPartCommandTarget carPartCommandTarget2 = CarPartCommand.CarPartCommandTarget.rearWheel;
			foreach (CarPart item in GetTypeIndex("chassis"))
			{
				if (item.DLCCode == DLCCode.Unknown)
				{
					continue;
				}
				foreach (CarPartCommand cmd in item.cmds)
				{
					if ((cmd.target == carPartCommandTarget || cmd.target == carPartCommandTarget2) && database.TryGetValue((int)cmd.valueF, out var value))
					{
						value.DLCCode = item.DLCCode;
					}
				}
			}
		}

		private static void ParseDisabledCores()
		{
			foreach (CarPart value in database.Values)
			{
				if (!value.disable.Contains(SLOT_ID_CORE))
				{
					continue;
				}
				value.disable.Remove(SLOT_ID_CORE);
				CarPart partById = GetPartById(GetCarCoreId(value.carId));
				if (partById != null)
				{
					for (int i = 0; i < partById.enable.Count; i++)
					{
						value.disable.Add(partById.enable[i].slotId);
					}
					value.disable.Remove(SLOT_ID_CHASSIS);
					if (!value.external)
					{
						value.disable.Remove(SLOT_ID_INTERIOR);
					}
				}
			}
		}

		private static void ConvertSlots(string[] source, List<int> dest)
		{
			foreach (string name in source)
			{
				dest.Add(GetSlotID(name));
			}
		}

		private static void ConvertParts(string[] source, List<int> dest, int baseId)
		{
			int result = 0;
			for (int i = 0; i < source.Length; i++)
			{
				if (int.TryParse(source[i], out result))
				{
					dest.Add(result + baseId);
				}
			}
		}

		public static int GetSlotID(string name)
		{
			if (!m_slotMap.TryGetValue(name, out var value))
			{
				value = ++m_slotIDCounter;
				m_slotMap.Add(name, value);
				InitStaticSlots(name, value);
			}
			return value;
		}

		private static bool IsContainsType(string type, HashSet<int> parts)
		{
			return parts.Any((int itr) => GetPartById(itr)?.type == type);
		}

		private static CarPart GetTypeByIndexAndCarId(int carId, string type)
		{
			return GetTypeIndex(type).FirstOrDefault((CarPart x) => x.carId == carId);
		}

		public static void EquipStockInterior(int carId, HashSet<int> parts)
		{
			if (!IsContainsType("seatleft", parts))
			{
				CarPart typeByIndexAndCarId = GetTypeByIndexAndCarId(carId, "seatleft");
				if (typeByIndexAndCarId != null)
				{
					EquipStockPart(typeByIndexAndCarId, parts);
				}
			}
			if (!IsContainsType("seatright", parts))
			{
				CarPart typeByIndexAndCarId2 = GetTypeByIndexAndCarId(carId, "seatright");
				if (typeByIndexAndCarId2 != null)
				{
					EquipStockPart(typeByIndexAndCarId2, parts);
				}
			}
			if (!IsContainsType("handbrake", parts))
			{
				CarPart typeByIndexAndCarId3 = GetTypeByIndexAndCarId(carId, "handbrake");
				if (typeByIndexAndCarId3 != null)
				{
					EquipStockPart(typeByIndexAndCarId3, parts);
				}
			}
			if (!IsContainsType("shifter", parts))
			{
				CarPart typeByIndexAndCarId4 = GetTypeByIndexAndCarId(carId, "shifter");
				if (typeByIndexAndCarId4 != null)
				{
					EquipStockPart(typeByIndexAndCarId4, parts);
				}
			}
			if (!IsContainsType("steeringwheel", parts))
			{
				CarPart typeByIndexAndCarId5 = GetTypeByIndexAndCarId(carId, "steeringwheel");
				if (typeByIndexAndCarId5 != null)
				{
					EquipStockPart(typeByIndexAndCarId5, parts);
				}
			}
		}

		private static void EquipStockPart(CarPart part, HashSet<int> parts)
		{
			Stack<CarPart> stack = new Stack<CarPart>();
			stack.Push(part);
			while (stack.Count > 0)
			{
				CarPart carPart = stack.Pop();
				parts.Add(carPart.id);
				foreach (CarPart.SlotPart item in carPart.enable)
				{
					parts.Add(item.partId);
					stack.Push(GetPartById(item.partId));
				}
			}
		}

		public static void EquipDefaults(int carId, HashSet<int> parts)
		{
			parts.Clear();
			CarPart partById = GetPartById(GetCarCoreId(carId));
			if (partById == null)
			{
				return;
			}
			Stack<CarPart> stack = new Stack<CarPart>();
			stack.Push(partById);
			while (stack.Count > 0)
			{
				CarPart carPart = stack.Pop();
				parts.Add(carPart.id);
				foreach (CarPart.SlotPart item in carPart.enable)
				{
					parts.Add(item.partId);
					stack.Push(GetPartById(item.partId));
				}
			}
		}

		private static int GetCarCoreId(int carid)
		{
			if (!m_carCores.TryGetValue(carid, out var value))
			{
				return -1;
			}
			return value;
		}

		public static bool IsValid(int carId, HashSet<int> parts, bool fixPartsSet = false)
		{
			if (parts.Count == 0)
			{
				return true;
			}
			HashSet<int> hashSet = new HashSet<int>();
			int carCoreId = GetCarCoreId(carId);
			if (carCoreId == -1)
			{
				return true;
			}
			hashSet.Add(carCoreId);
			parts.Add(carCoreId);
			SlotScheme slotScheme = new SlotScheme(carId, parts);
			foreach (KeyValuePair<int, SlotInfo> item in slotScheme.slotMap)
			{
				SlotInfo value = item.Value;
				if (!value.enabled)
				{
					continue;
				}
				if (value.value == null)
				{
					return false;
				}
				foreach (int item2 in value.value.disable)
				{
					if (!slotScheme.slotMap.TryGetValue(item2, out var value2))
					{
						return false;
					}
					if (value2.enabled)
					{
						return false;
					}
				}
			}
			slotScheme.FlushStates();
			slotScheme.BuildLiveStates();
			foreach (KeyValuePair<int, SlotInfo> item3 in slotScheme.slotMap)
			{
				SlotInfo value3 = item3.Value;
				CarPart value4 = value3.value;
				if (!value3.enabled)
				{
					continue;
				}
				hashSet.Add(value4.id);
				if (value4.carId != -1 && value4.carId != carId)
				{
					return false;
				}
				bool flag = false;
				foreach (int item4 in value4.take)
				{
					if (!slotScheme.slotMap.TryGetValue(item4, out var value5))
					{
						return false;
					}
					if (value5.isAlive && value5.value == value4)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					return false;
				}
			}
			if (parts.Count != hashSet.Count)
			{
				if (!fixPartsSet)
				{
					return false;
				}
				if (parts.IsProperSubsetOf(hashSet))
				{
					parts.UnionWith(hashSet);
				}
				else
				{
					if (!hashSet.IsProperSubsetOf(parts))
					{
						return false;
					}
					parts.IntersectWith(hashSet);
				}
			}
			else
			{
				foreach (int item5 in hashSet)
				{
					if (!parts.Contains(item5))
					{
						return false;
					}
				}
			}
			return true;
		}

		public static List<CarPart> ConvertToPartList(IEnumerable<int> parts, bool keepNull = false)
		{
			List<CarPart> list = new List<CarPart>();
			if (parts == null)
			{
				return list;
			}
			foreach (int part in parts)
			{
				CarPart carPart = INTERNAL_GetPartById(part);
				if (keepNull || carPart != null)
				{
					list.Add(carPart);
				}
			}
			return list;
		}

		public static void TryResetIfExistExternalParts(int carId, HashSet<int> parts)
		{
			foreach (int part in parts)
			{
				CarPart carPart = INTERNAL_GetPartById(part);
				if (carPart != null)
				{
					string type = CarVisualManager.CrookedPartType(carPart);
					if (System.IO.Path.IsPathRooted(CarVisualManager.FixPartPath(carPart.prefab, type)))
					{
						EquipDefaults(carId, parts);
						break;
					}
				}
			}
		}

		private static List<CarPart> GetTypeIndex(string type)
		{
			if (!m_typeIndex.TryGetValue(type, out var value))
			{
				value = new List<CarPart>();
				m_typeIndex.Add(type, value);
			}
			return value;
		}

		public static bool IsCarHaveBodies(int carId)
		{
			bool result = false;
			int num = 0;
			if (m_typeIndex.TryGetValue("chassis", out var value))
			{
				foreach (CarPart item in value)
				{
					if (item.DLCCode == DLCCode.Unknown && item.carId == carId)
					{
						num++;
					}
					if (num > 1)
					{
						return true;
					}
				}
			}
			return result;
		}

		public static bool IsCarHaveLockedParts(int carId)
		{
			CarVisualManager.VisualSettings visualForCar = m_prefs.carSettings.GetVisualForCar(carId);
			PlayerCar byId = DependencyInjector.Resolve<BaseModel>().GetById<PlayerCar>(carId);
			foreach (int part in visualForCar.parts)
			{
				if (IsCarPartLocked(carId, GetPartById(part), byId))
				{
					return true;
				}
			}
			return false;
		}

		public static bool IsCarHaveLockedWheels(int carId)
		{
			CarVisualManager.VisualSettings visualForCar = m_prefs.carSettings.GetVisualForCar(carId);
			PlayerCar byId = DependencyInjector.Resolve<BaseModel>().GetById<PlayerCar>(carId);
			if (IsCarPartLocked(carId, GetPartById(visualForCar.frontWheels), byId))
			{
				return true;
			}
			if (IsCarPartLocked(carId, GetPartById(visualForCar.rearWheels), byId))
			{
				return true;
			}
			return false;
		}

		public static bool IsPartsNotSupportLivery(HashSet<int> parts)
		{
			foreach (CarPart item in ConvertToPartList(parts))
			{
				if (item.vinylsNotSupport)
				{
					return true;
				}
			}
			return false;
		}

		public static bool IsCarPartLocked(int carId, CarPart part, PlayerCar playerCar = null)
		{
			if (part == null)
			{
				return false;
			}
			if (playerCar == null)
			{
				playerCar = DependencyInjector.Resolve<BaseModel>().GetById<PlayerCar>(carId);
			}
			bool flag = part.price == 0 || part.id == 0 || playerCar.defaultWheelId == part.id || !m_prefs.locks.GetCarPartLockState(carId, part.id);
			if (part.DLCCode != DLCCode.Unknown)
			{
				bool flag2 = Overlay.instance.IsDLCInstalled(part.DLCCode);
				flag = ((!(part.type == "chassis")) ? (flag || flag2) : flag2);
			}
			return !flag;
		}

		public static List<CarPart> GetCarBodies(int carId)
		{
			m_carBodyList.Clear();
			if (m_typeIndex.TryGetValue("chassis", out var value))
			{
				foreach (CarPart item in value)
				{
					if (item.carId == carId)
					{
						m_carBodyList.Add(item);
					}
				}
			}
			return m_carBodyList;
		}

		private static bool CheckConsoleAllowedPart(DLCCode dLCCode)
		{
			DLCCode[] consoleAllowedDLCParts = m_consoleAllowedDLCParts;
			for (int i = 0; i < consoleAllowedDLCParts.Length; i++)
			{
				if (consoleAllowedDLCParts[i] == dLCCode)
				{
					return true;
				}
			}
			return false;
		}

		private static bool CanPartBeEquiped(SlotScheme scheme, CarPart part)
		{
			if (part.carId != -1 && part.carId != scheme.hostCarId)
			{
				return false;
			}
			if (part.DLCCode != DLCCode.Unknown && string.IsNullOrEmpty(Overlay.instance.GetDLCName(part.DLCCode)) && part.DLCCode != DLCCode.SouthAmericaStyle)
			{
				return false;
			}
			foreach (KeyValuePair<int, SlotInfo> item in scheme.slotMap)
			{
				if (item.Value.enabled && item.Value.value != null && part.exclude.Contains(item.Value.value.id))
				{
					return false;
				}
			}
			if (part.take.Count == 0)
			{
				return true;
			}
			if (!scheme.blocked || part.type == "chassis")
			{
				if (scheme.baseList.Count != 0)
				{
					if (!(part.type == "chassis"))
					{
						return scheme.baseList.Contains(part.id);
					}
					return true;
				}
				foreach (int item2 in part.take)
				{
					if (scheme.slotMap.TryGetValue(item2, out var value) && value.enabled)
					{
						return true;
					}
				}
			}
			return false;
		}

		public static List<string> GetCategories(SlotScheme scheme)
		{
			List<string> list = new List<string>();
			HashSet<string> hashSet = new HashSet<string>();
			foreach (KeyValuePair<string, List<CarPart>> item in m_typeIndex)
			{
				foreach (CarPart item2 in item.Value)
				{
					if (CanPartBeEquiped(scheme, item2))
					{
						if (hashSet.Contains(item.Key))
						{
							list.Add(item.Key);
							break;
						}
						hashSet.Add(item.Key);
					}
				}
			}
			list.Sort(delegate(string lhs, string rhs)
			{
				if (!m_categoriesOrder.TryGetValue(lhs, out var value))
				{
					value = int.MaxValue;
				}
				if (!m_categoriesOrder.TryGetValue(rhs, out var value2))
				{
					value2 = int.MaxValue;
				}
				return (value == value2) ? lhs.CompareTo(rhs) : value.CompareTo(value2);
			});
			return list;
		}

		public static List<string> GetGroups(int carId, string type)
		{
			HashSet<string> hashSet = new HashSet<string>();
			foreach (CarPart item in GetTypeIndex(type))
			{
				if ((item.packetid == 0 || (externalDatabases.TryGetValue(item.packetid, out var value) && (value.isLocal || UGCServiceProvider.instance.IsContentInstalled(item.packetid)))) && (item.carId == -1 || item.carId == carId))
				{
					hashSet.Add(item.group);
				}
			}
			return new List<string>(hashSet);
		}

		public static List<CarPart> GetParts(SlotScheme scheme, string type, string group, bool forceAdd = false)
		{
			List<CarPart> list = new List<CarPart>();
			foreach (CarPart item2 in GetTypeIndex(type))
			{
				if (item2.group == group && (CanPartBeEquiped(scheme, item2) || forceAdd))
				{
					ApplyRuntimeDLC(item2);
					list.Add(item2);
				}
			}
			if (list.Count > 0 && type == "chassis")
			{
				CarPart item = list[0];
				list.Sort(delegate(CarPart lhs, CarPart rhs)
				{
					if (lhs.DLCCode == DLCCode.Unknown && rhs.DLCCode == DLCCode.Unknown)
					{
						return 0;
					}
					if (lhs.DLCCode != DLCCode.Unknown && rhs.DLCCode == DLCCode.Unknown)
					{
						return -1;
					}
					return (lhs.DLCCode == DLCCode.Unknown && rhs.DLCCode != DLCCode.Unknown) ? 1 : lhs.id.CompareTo(rhs.id);
				});
				list.Remove(item);
				list.Insert(0, item);
			}
			return list;
		}

		private static void ApplyRuntimeDLC(CarPart part)
		{
			if ((part.carId == 22 || part.carId == 17) && m_prefs.locks.isOldCarsRestored)
			{
				part.DLCCode = DLCCode.RuntimeDLC;
			}
		}

		public static CarPart GetPartById(int id)
		{
			CarPart carPart = INTERNAL_GetPartById(id);
			if (carPart == null)
			{
				carPart = INTERNAL_GetPartById(0);
			}
			return carPart;
		}

		public static CarPart INTERNAL_GetPartById(int id)
		{
			database.TryGetValue(id, out var value);
			return value;
		}
	}
}
