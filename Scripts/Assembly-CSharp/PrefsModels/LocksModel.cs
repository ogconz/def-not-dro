using System;
using System.Collections.Generic;
using AchievementsSyncService;
using Customization;
using DB;
using DB.Meta;
using DI;
using GameOverlay;
using UnityEngine;

namespace PrefsModels
{
	public class LocksModel : BasePrefsModel
	{
		public class CarLocksDescriptor
		{
			public HashSet<int> parts = new HashSet<int>();
		}

		private BaseModel m_model;

		private GamePrefs m_prefs;

		public Dictionary<int, CarLocksDescriptor> carLocks = new Dictionary<int, CarLocksDescriptor>();

		public HashSet<int> availableCars = new HashSet<int>();

		public HashSet<int> availableTracks = new HashSet<int>();

		public HashSet<int> availableProfiles = new HashSet<int>();

		private bool m_isOldCarsRestored;

		private float m_lastFactor;

		private bool m_factorIsActual;

		public bool isOldCarsRestored
		{
			get
			{
				return m_isOldCarsRestored;
			}
			set
			{
				m_isOldCarsRestored = value;
				Save();
			}
		}

		public LocksModel()
		{
			m_isOldCarsRestored = false;
			m_model = DependencyInjector.Resolve<BaseModel>();
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
			carLocks.Clear();
			foreach (PlayerCar item in m_model.QueryAll<PlayerCar>())
			{
				SetLockAndSave(availableCars, item.id, item.defaultLocked);
			}
			foreach (PlayerCarProfile item2 in m_model.QueryAll<PlayerCarProfile>())
			{
				SetLockAndSave(availableProfiles, item2.id, item2.defaultLocked);
			}
			foreach (DB.Meta.Track item3 in m_model.QueryAll<DB.Meta.Track>())
			{
				SetLockAndSave(availableTracks, item3.id, item3.defaultLocked);
			}
		}

		public override void Reset()
		{
			m_factorIsActual = false;
			base.Reset();
		}

		public float GetCarCurrentCoinsFactor()
		{
			if (m_factorIsActual)
			{
				return m_lastFactor;
			}
			m_lastFactor = 1f;
			foreach (PlayerCar item in m_model.QueryAll<PlayerCar>())
			{
				if (item.enabled && item.coinsFactor > m_lastFactor && !GetCarLockState(item.id))
				{
					m_lastFactor = item.coinsFactor;
				}
			}
			m_factorIsActual = true;
			return m_lastFactor;
		}

		public string GetCurrentCarName()
		{
			foreach (PlayerCar item in m_model.QueryAll<PlayerCar>())
			{
				if (item.enabled && item.id == m_prefs.globals.lastCarId)
				{
					return item.name;
				}
			}
			return null;
		}

		public PlayerCar GetCurrentCarMeta()
		{
			foreach (PlayerCar item in m_model.QueryAll<PlayerCar>())
			{
				if (item.enabled && item.id == m_prefs.globals.lastCarId)
				{
					return item;
				}
			}
			return null;
		}

		public override void PostLoad()
		{
			ApplyDefaultLockedFix(isStartup: true);
			GameEvents.OnGameEngineStart.Subscribe(GameEventFlag.OneCall, delegate
			{
				ApplyStylishDriftAchievements();
				AchievementsSyncManager.SyncStatus(StatsManager.EStats.CupsAllGold);
			});
		}

		protected override bool CompareRecovePrefs(BasePrefsModel reserveModel)
		{
			if (!(reserveModel is LocksModel locksModel))
			{
				return true;
			}
			if (availableCars == locksModel.availableCars && availableProfiles == locksModel.availableProfiles && availableTracks == locksModel.availableTracks)
			{
				return carLocks == locksModel.carLocks;
			}
			return false;
		}

		public void ApplyStylishDriftAchievements()
		{
			if (Overlay.IsReady() && Overlay.instance.IsDLCInstalled(DLCCode.FreeContent))
			{
				GetAvailableAndAllCarsCount(out var unlocked, out var _);
				StatsManager.TrySetMaxStat(StatsManager.EStats.StylishDrift, unlocked);
				return;
			}
			int num = 0;
			foreach (CarLocksDescriptor value2 in carLocks.Values)
			{
				foreach (int part in value2.parts)
				{
					if (CarPartDatabase.database.TryGetValue(part, out var value) && value != null && value.type == "chassis")
					{
						num++;
						break;
					}
				}
			}
			StatsManager.TrySetMaxStat(StatsManager.EStats.StylishDrift, num);
		}

		private CarLocksDescriptor GetLocksDescriptor(int carId)
		{
			if (!carLocks.TryGetValue(carId, out var value))
			{
				value = new CarLocksDescriptor();
				carLocks.Add(carId, value);
			}
			return value;
		}

		private void SetLockAndSave(HashSet<int> set, int id, bool state)
		{
			if (state)
			{
				set.Remove(id);
			}
			else
			{
				set.Add(id);
			}
			Save();
		}

		private bool GetLock(HashSet<int> set, int id)
		{
			if (Overlay.IsReady() && Overlay.instance.IsDLCInstalled(DLCCode.FreeContent))
			{
				return false;
			}
			return !set.Contains(id);
		}

		public void SetTrackLockState(int trackID, bool lockState)
		{
			SetLockAndSave(availableTracks, trackID, lockState);
		}

		public bool IsTrackUnlockedByDLC(int trackID)
		{
			DB.Meta.Track byId = m_model.GetById<DB.Meta.Track>(trackID);
			if (byId != null && byId.dlcCode != DLCCode.Unknown)
			{
				return Overlay.instance.IsDLCInstalled(byId.dlcCode);
			}
			return false;
		}

		public bool GetTrackLockState(int trackID)
		{
			if (IsTrackUnlockedByDLC(trackID))
			{
				return false;
			}
			return GetLock(availableTracks, trackID);
		}

		public void SetCarLockState(int carID, bool lockState)
		{
			m_factorIsActual = false;
			SetLockAndSave(availableCars, carID, lockState);
		}

		public bool GetCarLockState(int carID)
		{
			if (CarPartDatabase.IsCarUnlockedByDLC(carID))
			{
				return false;
			}
			PlayerCar byId = m_model.GetById<PlayerCar>(carID);
			if (byId != null)
			{
				if (byId.eventCode != GameEventCode.Unknown)
				{
					return !Singleton<GameplayEvents>.instance.IsContentAvailable(byId.eventCode);
				}
				if (byId.dlcCode != DLCCode.Unknown)
				{
					return !Overlay.instance.IsDLCInstalled(byId.dlcCode);
				}
				if (Mathf.RoundToInt(byId.coinsFactor) == 1 && Overlay.instance.IsDLCInstalled(DLCCode.FreeContent))
				{
					return false;
				}
			}
			return GetLock(availableCars, carID);
		}

		public void SetProfileLockState(int profileID, bool lockState)
		{
			SetLockAndSave(availableProfiles, profileID, lockState);
		}

		public bool GetProfileLockState(int profileID)
		{
			return GetLock(availableProfiles, profileID);
		}

		public void SetCarPartLockState(int carId, int partId, bool lockState)
		{
			SetLockAndSave(GetLocksDescriptor(carId).parts, partId, lockState);
		}

		public bool GetCarPartLockState(int carId, int partId)
		{
			return GetLock(GetLocksDescriptor(carId).parts, partId);
		}

		public int GetUnlockedCarsCount()
		{
			return m_model.QueryAll<PlayerCar>().Reduce(0, (PlayerCar item, int counter) => counter + ((item.defaultLocked && !GetCarLockState(item.id)) ? 1 : 0));
		}

		public void GetAvailableAndAllCarsCount(out int unlocked, out int all)
		{
			List<PlayerCar> collection = m_model.QueryAll<PlayerCar>();
			all = collection.Reduce(0, (PlayerCar item, int counter) => counter + (item.enabled ? 1 : 0));
			unlocked = collection.Reduce(0, (PlayerCar item, int counter) => counter + ((item.enabled && !GetCarLockState(item.id)) ? 1 : 0));
		}

		public int GetUnlockedProfilesCount()
		{
			return m_model.QueryAll<PlayerCarProfile>().Reduce(0, (PlayerCarProfile item, int counter) => counter + ((item.defaultLocked && availableProfiles.Contains(item.id)) ? 1 : 0));
		}

		public int GetUnlockedTracksCount()
		{
			return m_model.QueryAll<DB.Meta.Track>().Reduce(0, (DB.Meta.Track item, int counter) => counter + ((item.defaultLocked && availableTracks.Contains(item.id)) ? 1 : 0));
		}

		public void GetAvailableAndAllTracksCount(out int unlocked, out int all)
		{
			List<DB.Meta.Track> collection = m_model.QueryAll<DB.Meta.Track>();
			all = collection.Reduce(0, (DB.Meta.Track item, int counter) => counter + ((item.enabled && !item.hidden && !item.external) ? 1 : 0));
			unlocked = collection.Reduce(0, (DB.Meta.Track item, int counter) => counter + ((IsTrackCanShow(item) && !item.external) ? 1 : 0));
		}

		private bool IsTrackCanShow(DB.Meta.Track track)
		{
			if (!IsTrackUnlockedByDLC(track.id) && !IsTrackBuyAndAvailable(track))
			{
				return IsTrackDlcDeluxeInstalled(track);
			}
			return true;
		}

		private bool IsTrackBuyAndAvailable(DB.Meta.Track track)
		{
			if (availableTracks.Contains(track.id) && track.enabled && !track.hidden)
			{
				return track.requiredLevel <= m_prefs.playerProfile.level;
			}
			return false;
		}

		private bool IsTrackDlcDeluxeInstalled(DB.Meta.Track track)
		{
			if (Overlay.instance.IsDLCInstalled(DLCCode.FreeContent))
			{
				return track.dlcCode == DLCCode.Unknown;
			}
			return false;
		}

		public void UnlockAll()
		{
			m_factorIsActual = false;
			UnlockAll<PlayerCar>(availableCars);
			UnlockAll<PlayerCarProfile>(availableProfiles);
			UnlockAll<DB.Meta.Track>(availableTracks);
			UnlockAllSwaps();
		}

		public void UnlockAllSwaps()
		{
			IReadOnlyCollection<EngineInfo> swaps = SwapsAsset.LoadAndInitDB().swaps;
			foreach (int availableProfile in availableProfiles)
			{
				foreach (EngineInfo item in swaps)
				{
					PlayerCarProfile byId = m_model.GetById<PlayerCarProfile>(availableProfile);
					string engineIdFromProfile = item.GetEngineIdFromProfile(byId);
					m_prefs.inventory.AddItem(engineIdFromProfile, 1, permanentItem: true, addCountIfExist: false);
				}
			}
		}

		public void UnlockAll<T>(HashSet<int> set) where T : class, IDBMeta, new()
		{
			foreach (T item in m_model.QueryAll<T>())
			{
				set.Add(item.id);
			}
		}

		public void RecordUnlock(TransactionManager.UnlockType type, int unlockedItemId)
		{
			switch (type)
			{
			case TransactionManager.UnlockType.Car:
				SetCarLockState(unlockedItemId, lockState: false);
				break;
			case TransactionManager.UnlockType.Profile:
				SetProfileLockState(unlockedItemId, lockState: false);
				break;
			case TransactionManager.UnlockType.Track:
				SetTrackLockState(unlockedItemId, lockState: false);
				break;
			case TransactionManager.UnlockType.CarPart:
				SetCarPartLockState(m_prefs.globals.lastCarId, unlockedItemId, lockState: false);
				break;
			default:
				throw new Exception("Unsupported unlock type: " + type);
			case TransactionManager.UnlockType.CarColor:
			case TransactionManager.UnlockType.WheelRimColor:
			case TransactionManager.UnlockType.WheelSpokeColor:
			case TransactionManager.UnlockType.SmokeColor:
			case TransactionManager.UnlockType.GlassColor:
				break;
			}
		}

		public void RecordUnlockSpecificCarPart(int carId, int carPart)
		{
			SetCarPartLockState(carId, carPart, lockState: false);
		}

		public void OverwriteLocks(TransactionManager.UnlockType type, IEnumerable<int> unlockedIds)
		{
			m_factorIsActual = false;
			OverwriteLocks(GetByType(type), unlockedIds);
			Save();
		}

		public void MergeCarLocks(Dictionary<int, CarLocksDescriptor> locks)
		{
			CarLocksDescriptor value = null;
			foreach (KeyValuePair<int, CarLocksDescriptor> @lock in locks)
			{
				if (!carLocks.TryGetValue(@lock.Key, out value))
				{
					carLocks.Add(@lock.Key, value);
				}
				value.parts.UnionWith(@lock.Value.parts);
			}
			Save();
		}

		public void MergeLocks(TransactionManager.UnlockType type, IEnumerable<int> unlockedIds)
		{
			m_factorIsActual = false;
			MergeLocks(GetByType(type), unlockedIds);
			Save();
		}

		public void ApplyDefaultLockedFix(bool isStartup)
		{
			int num = 0;
			foreach (PlayerCar item in m_model.QueryAll<PlayerCar>())
			{
				if (!item.defaultLocked && !availableCars.Contains(item.id))
				{
					availableCars.Add(item.id);
					num++;
				}
			}
			int num2 = 0;
			foreach (PlayerCarProfile item2 in m_model.QueryAll<PlayerCarProfile>())
			{
				if (!item2.defaultLocked && !availableProfiles.Contains(item2.id))
				{
					availableProfiles.Add(item2.id);
					num2++;
				}
			}
			int num3 = 0;
			foreach (DB.Meta.Track item3 in m_model.QueryAll<DB.Meta.Track>())
			{
				if (!item3.defaultLocked && !availableTracks.Contains(item3.id))
				{
					availableTracks.Add(item3.id);
					num3++;
				}
			}
			if (num > 0 || num2 > 0 || num3 > 0)
			{
				Save();
				GameLog.Print(AlertLevel.Warning, "Prefs", $"Found default locked Error cars - {num}, profiles - {num2}, tracks - {num3}");
			}
		}

		private HashSet<int> GetByType(TransactionManager.UnlockType type)
		{
			return type switch
			{
				TransactionManager.UnlockType.Car => availableCars, 
				TransactionManager.UnlockType.Profile => availableProfiles, 
				TransactionManager.UnlockType.Track => availableTracks, 
				_ => throw new Exception("Unsupported unlock type: " + type), 
			};
		}

		private int GetUnlockedCount<T>(HashSet<int> set) where T : CarVisualTuningMeta, new()
		{
			return m_model.QueryAll<T>().Reduce(0, (T item, int counter) => counter + ((item.defaultLocked && set.Contains(item.id)) ? 1 : 0));
		}

		private void OverwriteLocks(HashSet<int> set, IEnumerable<int> unlockedIds)
		{
			set.Clear();
			if (unlockedIds != null)
			{
				set.UnionWith(unlockedIds);
			}
		}

		private void MergeLocks(HashSet<int> set, IEnumerable<int> unlockedIds)
		{
			if (unlockedIds != null)
			{
				set.UnionWith(unlockedIds);
			}
		}
	}
}
