using System;
using System.Collections.Generic;
using DB;
using DB.Meta;
using DI;
using SimpleJSON;
using UnityEngine;

namespace PrefsModels
{
	public class PrefsCarSlotLockModel : BasePrefsModel
	{
		[JsonSerialize]
		private Dictionary<int, int> m_slotLimitPerCar = new Dictionary<int, int>();

		[JsonIgnore]
		private GamePrefs m_prefs = DependencyInjector.Resolve<GamePrefs>();

		[JsonIgnore]
		private BaseModel m_baseModel = DependencyInjector.Resolve<BaseModel>();

		[JsonIgnore]
		private HashSet<int> m_unlockSlotForStarterCars = new HashSet<int> { 3, 40, 9 };

		public override void Reset()
		{
			base.Reset();
			m_slotLimitPerCar.Clear();
		}

		public int GetLimit(int carId)
		{
			if (!m_slotLimitPerCar.ContainsKey(carId))
			{
				SetLimit(carId, 0);
			}
			return m_slotLimitPerCar[carId];
		}

		public void SetLimit(int carId, int value)
		{
			int min = 1;
			int maxSlotCount = PrefsCarSlotsModel.GetMaxSlotCount();
			m_slotLimitPerCar[carId] = Mathf.Clamp(value, min, maxSlotCount);
			AddQueryToSave(this);
		}

		public bool IsSlotUnlocked(int carId, int slotId)
		{
			if (!GameManager.SlotDLCInstalled && !IsActiveSlot(carId, slotId) && !IsQuestCar(carId) && !IsStarterCar(carId))
			{
				return CanUseSlot(carId, slotId);
			}
			return true;
		}

		public List<int> ConstructSortedSlotList(int carId)
		{
			List<int> list = ConstructNonDeterministicSlotList(carId, PrefsCarSlotsModel.GetMaxSlotCount());
			list.Sort((int first, int second) => CompareSlots(carId, first, second));
			return list;
		}

		public List<int> GetUnlockedAndUsedSlots(int carId)
		{
			return GetFilteredSlotList(carId, (int slotId) => IsSlotUnlocked(carId, slotId) && IsSlotUsed(carId, slotId));
		}

		public List<int> GetUnlockedAndFreeSlots(int carId)
		{
			return GetFilteredSlotList(carId, (int slotId) => IsSlotUnlocked(carId, slotId) && !IsSlotUsed(carId, slotId));
		}

		private bool IsQuestCar(int carId)
		{
			return m_baseModel.GetById<PlayerCar>(carId).questReward;
		}

		private bool IsStarterCar(int carId)
		{
			return m_unlockSlotForStarterCars.Contains(carId);
		}

		private bool IsActiveSlot(int carId, int slotId)
		{
			return m_prefs.carSettings.GetActiveSlotId(carId) == slotId;
		}

		public bool IsSlotUsed(int carId, int slotId)
		{
			return m_prefs.carSettings.GetUsedSlots(carId).Contains(slotId);
		}

		private bool CanUseSlot(int carId, int slotId)
		{
			return ConstructNonDeterministicSlotList(carId, GetLimit(carId)).Contains(slotId);
		}

		private List<int> GetFilteredSlotList(int carId, Predicate<int> rule)
		{
			List<int> list = new List<int>();
			foreach (int item in ConstructNonDeterministicSlotList(carId, PrefsCarSlotsModel.GetMaxSlotCount()))
			{
				if (rule(item))
				{
					list.Add(item);
				}
			}
			return list;
		}

		private List<int> ConstructNonDeterministicSlotList(int carId, int limit)
		{
			List<int> list = new List<int>();
			List<int> usedSlots = m_prefs.carSettings.GetUsedSlots(carId);
			int num = Math.Min(usedSlots.Count, limit);
			for (int i = 0; i < num; i++)
			{
				list.Add(usedSlots[i]);
			}
			num = limit - list.Count;
			for (int j = 0; j < num; j++)
			{
				int availableSlotId = m_prefs.carSettings.GetAvailableSlotId(list);
				if (availableSlotId != -1)
				{
					list.Add(availableSlotId);
				}
			}
			return list;
		}

		private int CompareSlots(int carId, int first, int second)
		{
			bool flag = IsSlotUnlocked(carId, first);
			bool flag2 = IsSlotUnlocked(carId, second);
			bool flag3 = IsSlotUsed(carId, first);
			bool flag4 = IsSlotUsed(carId, second);
			if (!flag && !flag2)
			{
				return 0;
			}
			if (!flag)
			{
				return 1;
			}
			if (!flag2)
			{
				return -1;
			}
			if (flag3 == flag4)
			{
				return 0;
			}
			if (flag3)
			{
				return -1;
			}
			if (flag4)
			{
				return 1;
			}
			return 0;
		}
	}
}
