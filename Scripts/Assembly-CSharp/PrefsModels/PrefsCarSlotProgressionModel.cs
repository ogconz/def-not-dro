using System.Collections.Generic;
using DI;
using Inventory;
using Quests;
using SimpleJSON;

namespace PrefsModels
{
	public class PrefsCarSlotProgressionModel : BasePrefsModel
	{
		[JsonSerialize]
		private Dictionary<int, HashSet<string>> m_completedTasks = new Dictionary<int, HashSet<string>>();

		[JsonIgnore]
		private GamePrefs m_prefs = DependencyInjector.Resolve<GamePrefs>();

		[JsonIgnore]
		private ItemsAsset m_inventory = ItemsAsset.LoadDB();

		[JsonIgnore]
		private int m_sessionCompletedTaskCount;

		public override void Reset()
		{
			base.Reset();
			m_completedTasks.Clear();
			m_sessionCompletedTaskCount = 0;
		}

		public void HandleQuestEvent(int carId, string taskId, int valueCount)
		{
			if (!IsTaskCompleted(carId, taskId))
			{
				int slotOfTask = GetSlotOfTask(taskId);
				if (IsAnyTaskCompleted(carId, slotOfTask - 1) && CheckCompleteTask(taskId, slotOfTask, valueCount))
				{
					CompleteTask(carId, taskId, slotOfTask);
				}
			}
		}

		public List<(string, bool)> ConstructTaskList(int carId, int slotIndex)
		{
			List<(string, bool)> list = new List<(string, bool)>();
			QuestsAsset.QuestStagePartMeta[] taskForSlot = SlotProgressionConfig.GetTaskForSlot(slotIndex);
			for (int i = 0; i < taskForSlot.Length; i++)
			{
				if (i == 0 && slotIndex > 1)
				{
					list.Add(("stage_car_slot_unlock_previous", IsAnyTaskCompleted(carId, slotIndex - 1)));
				}
				list.Add((taskForSlot[i].itemId, IsTaskCompleted(carId, taskForSlot[i].itemId)));
				if (i < taskForSlot.Length - 1)
				{
					list.Add((string.Empty, false));
				}
			}
			return list;
		}

		public QuestsAsset.QuestReward ConstructReward()
		{
			if (m_sessionCompletedTaskCount <= 0 || GameManager.SlotDLCInstalled)
			{
				return null;
			}
			ItemsAsset.ItemMeta itemMeta = m_inventory.Find("car_slot_reward");
			if (itemMeta == null || itemMeta.icon == null)
			{
				return null;
			}
			QuestsAsset.QuestReward result = new QuestsAsset.QuestReward
			{
				id = "car_slot_reward",
				count = m_sessionCompletedTaskCount,
				icon = itemMeta.icon
			};
			m_sessionCompletedTaskCount = 0;
			return result;
		}

		private int GetRequiredProgress(string id, int slotIndex)
		{
			QuestsAsset.QuestStagePartMeta[] taskForSlot = SlotProgressionConfig.GetTaskForSlot(slotIndex);
			foreach (QuestsAsset.QuestStagePartMeta questStagePartMeta in taskForSlot)
			{
				if (questStagePartMeta.itemId == id)
				{
					return questStagePartMeta.completeValue;
				}
			}
			return 0;
		}

		private bool CheckCompleteTask(string id, int slotIndex, int value)
		{
			QuestsAsset.QuestStagePartMeta[] taskForSlot = SlotProgressionConfig.GetTaskForSlot(slotIndex);
			foreach (QuestsAsset.QuestStagePartMeta questStagePartMeta in taskForSlot)
			{
				if (!(questStagePartMeta.itemId != id) && ((questStagePartMeta.completeType == QuestsAsset.CompleteType.MoreOrEqual && value >= questStagePartMeta.completeValue) || (questStagePartMeta.completeType == QuestsAsset.CompleteType.LessOrEqual && value <= questStagePartMeta.completeValue) || (questStagePartMeta.completeType == QuestsAsset.CompleteType.More && value > questStagePartMeta.completeValue) || (questStagePartMeta.completeType == QuestsAsset.CompleteType.Less && value < questStagePartMeta.completeValue) || (questStagePartMeta.completeType == QuestsAsset.CompleteType.Equal && value == questStagePartMeta.completeValue)))
				{
					return true;
				}
			}
			return false;
		}

		private int GetSlotOfTask(string taskId)
		{
			for (int i = 0; i < PrefsCarSlotsModel.GetMaxSlotCount(); i++)
			{
				QuestsAsset.QuestStagePartMeta[] taskForSlot = SlotProgressionConfig.GetTaskForSlot(i);
				for (int j = 0; j < taskForSlot.Length; j++)
				{
					if (taskForSlot[j].itemId == taskId)
					{
						return i;
					}
				}
			}
			return -1;
		}

		private bool IsTaskCompleted(int carId, string taskId)
		{
			if (m_completedTasks.TryGetValue(carId, out var value))
			{
				return value.Contains(taskId);
			}
			return false;
		}

		private bool IsAnyTaskCompleted(int carId, int slotIndex)
		{
			QuestsAsset.QuestStagePartMeta[] taskForSlot = SlotProgressionConfig.GetTaskForSlot(slotIndex);
			if (taskForSlot.Length == 0)
			{
				return true;
			}
			QuestsAsset.QuestStagePartMeta[] array = taskForSlot;
			foreach (QuestsAsset.QuestStagePartMeta questStagePartMeta in array)
			{
				if (IsTaskCompleted(carId, questStagePartMeta.itemId))
				{
					return true;
				}
			}
			return false;
		}

		private void CompleteTask(int carId, string taskId, int slotIndex)
		{
			if (!m_completedTasks.ContainsKey(carId))
			{
				m_completedTasks[carId] = new HashSet<string>();
			}
			int num = slotIndex + 1;
			if (m_prefs.slotLock.GetLimit(carId) < num)
			{
				m_completedTasks[carId].Add(taskId);
				m_prefs.slotLock.SetLimit(carId, num);
				m_sessionCompletedTaskCount++;
				AddQueryToSave(this);
			}
		}

		public string GetLocalizedTask(string id, int slotIndex)
		{
			return GetLocalizationKey(id).LocalizedFormat(GetRequiredProgress(id, slotIndex));
		}

		private LocalizationKeys.Key GetLocalizationKey(string id)
		{
			return id switch
			{
				"stage_car_slot_unlock_previous" => LocalizationKeys.car_slot_unlock_previous, 
				"sp_any_race_win" => LocalizationKeys.car_slot_2_task_1, 
				"sp_xds_total_points" => LocalizationKeys.car_slot_3_task_1, 
				"mp_any_race_finish" => LocalizationKeys.car_slot_3_task_2, 
				"mp_session_drift_points" => LocalizationKeys.car_slot_4_task_1, 
				"mp_session_drift_points2" => LocalizationKeys.car_slot_4_task_1, 
				"mp_xds_points_single_run" => LocalizationKeys.car_slot_5_task_1, 
				"top32_position" => LocalizationKeys.car_slot_5_task_2, 
				_ => LocalizationKeys.car_slot_or, 
			};
		}
	}
}
