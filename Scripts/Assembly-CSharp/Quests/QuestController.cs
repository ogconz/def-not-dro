using System;
using System.Collections.Generic;
using DI;
using Inventory;
using UnityEngine;

namespace Quests
{
	public static class QuestController
	{
		public const string ENGINE_PART_ITEM_BEGIN = "engine_part_";

		public static Action<QuestsAsset.QuestMeta, List<QuestsAsset.QuestReward>> onQuestCompleted;

		private static QuestsAsset m_quests;

		private static ItemsAsset m_inventory;

		private static GamePrefs m_prefs;

		public const string stage_sp_time_attack_gold = "sp_time_attack_gold";

		public const string stage_mp_tandem_dp_win = "mp_tandem_dp_win";

		public const string stage_sp_drift_win = "sp_drift_win";

		public const string stage_mp_duel_win = "mp_duel_win";

		public const string stage_sp_xds_total_points = "sp_xds_total_points";

		public const string stage_top32_position = "top32_position";

		public const string stage_mp_drift_win = "mp_drift_win";

		public const string stage_mp_xds_points_single_run = "mp_xds_points_single_run";

		public const string stage_mp_tandem_xds_points_single_run = "mp_tandem_xds_points_single_run";

		public const string stage_mp_tandem_xds_win = "mp_tandem_xds_win";

		public const string stage_mp_time_attack_win = "mp_time_attack_win";

		public const string stage_mp_session_drift_points3 = "mp_session_drift_points3";

		public const string stage_mp_tandem_xds_points_total = "mp_tandem_xds_points_total";

		public const string stage_mp_session_drift_points = "mp_session_drift_points";

		public const string stage_mp_session_drift_points2 = "mp_session_drift_points2";

		public const string stage_mp_any_race_finish = "mp_any_race_finish";

		public const string stage_sp_any_race_win = "sp_any_race_win";

		public const string stage_car_slot_unlock_previous = "stage_car_slot_unlock_previous";

		public static void Initialize()
		{
			m_quests = QuestsAsset.LoadDB();
			m_inventory = ItemsAsset.LoadDB();
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
		}

		public static List<(QuestsAsset.QuestMeta, List<QuestsAsset.QuestReward>)> EventCompleted(string eventId, int valueCount, Func<string, bool> filter = null, bool getRewards = true)
		{
			GameLog.Print(AlertLevel.Notify, "ServerQuests", $"'{eventId}' completed, value: {valueCount}");
			int lastCarId = m_prefs.globals.lastCarId;
			m_prefs.slotProgression.HandleQuestEvent(lastCarId, eventId, valueCount);
			if (!getRewards)
			{
				return new List<(QuestsAsset.QuestMeta, List<QuestsAsset.QuestReward>)>();
			}
			List<QuestsAsset.QuestMeta> list = new List<QuestsAsset.QuestMeta>();
			foreach (QuestsAsset.QuestMeta quest in m_quests.quests)
			{
				if (quest == null)
				{
					continue;
				}
				QuestsAsset.QuestStageMeta[] stages = quest.stages;
				foreach (QuestsAsset.QuestStageMeta questStageMeta in stages)
				{
					bool flag = false;
					QuestsAsset.QuestStagePartMeta[] parts = questStageMeta.parts;
					foreach (QuestsAsset.QuestStagePartMeta questStagePartMeta in parts)
					{
						if (questStagePartMeta.itemId == eventId)
						{
							if ((questStagePartMeta.completeType == QuestsAsset.CompleteType.MoreOrEqual && valueCount >= questStagePartMeta.completeValue) || (questStagePartMeta.completeType == QuestsAsset.CompleteType.LessOrEqual && valueCount <= questStagePartMeta.completeValue) || (questStagePartMeta.completeType == QuestsAsset.CompleteType.More && valueCount > questStagePartMeta.completeValue) || (questStagePartMeta.completeType == QuestsAsset.CompleteType.Less && valueCount < questStagePartMeta.completeValue) || (questStagePartMeta.completeType == QuestsAsset.CompleteType.Equal && valueCount == questStagePartMeta.completeValue))
							{
								AddQuestItem(quest, questStageMeta, questStagePartMeta);
								flag = true;
							}
							break;
						}
					}
					if (!flag)
					{
						continue;
					}
					if (questStageMeta.completeType == QuestsAsset.PartCompleteType.OneOf)
					{
						list.Add(quest);
						continue;
					}
					bool flag2 = false;
					parts = questStageMeta.parts;
					foreach (QuestsAsset.QuestStagePartMeta questStagePartMeta2 in parts)
					{
						if (m_inventory.Find(questStageMeta.GetPartId(questStagePartMeta2.itemId)) == null)
						{
							flag2 = true;
							break;
						}
					}
					if (!flag2)
					{
						list.Add(quest);
					}
				}
			}
			List<(QuestsAsset.QuestMeta, List<QuestsAsset.QuestReward>)> list2 = new List<(QuestsAsset.QuestMeta, List<QuestsAsset.QuestReward>)>();
			QuestsAsset.QuestReward questReward = m_prefs.slotProgression.ConstructReward();
			if (questReward != null)
			{
				list2.Add((new QuestsAsset.QuestMeta(), new List<QuestsAsset.QuestReward> { questReward }));
			}
			foreach (QuestsAsset.QuestMeta item in list)
			{
				if (!IsQuestCompleted(item))
				{
					continue;
				}
				RemoveQuestItems(item);
				List<QuestsAsset.QuestReward> rewards = GetRewards(item);
				List<QuestsAsset.QuestReward> list3 = new List<QuestsAsset.QuestReward>();
				foreach (QuestsAsset.QuestReward item2 in rewards)
				{
					bool flag3 = true;
					if (filter != null)
					{
						flag3 = filter(item2.id);
					}
					if (flag3)
					{
						list3.Add(item2);
						m_prefs.inventory.AddItem(item2.id, item2.count, permanentItem: false);
					}
				}
				if (list3.Count > 0)
				{
					list2.Add((item, list3));
				}
			}
			foreach (var item3 in list2)
			{
				onQuestCompleted.SafeCall(item3.Item1, item3.Item2);
			}
			return list2;
		}

		public static void AddEnginePart(int id, int count)
		{
			List<QuestsAsset.QuestMeta> quests = m_quests.quests;
			if (id >= 0 && id < quests.Count)
			{
				QuestsAsset.QuestMeta questMeta = quests[id];
				m_prefs.inventory.AddItem(questMeta.rewards[0].itemId, count, permanentItem: false);
			}
		}

		private static void AddQuestItem(QuestsAsset.QuestMeta quest, QuestsAsset.QuestStageMeta stageMeta, QuestsAsset.QuestStagePartMeta partMeta)
		{
			string partId = quest.GetPartId(stageMeta, partMeta);
			m_prefs.inventory.AddItem(partId, 1, stageMeta.permanent, addCountIfExist: false);
		}

		private static void RemoveQuestItems(QuestsAsset.QuestMeta quest)
		{
			QuestsAsset.QuestStageMeta[] stages = quest.stages;
			foreach (QuestsAsset.QuestStageMeta questStageMeta in stages)
			{
				QuestsAsset.QuestStagePartMeta[] parts = questStageMeta.parts;
				foreach (QuestsAsset.QuestStagePartMeta partMeta in parts)
				{
					string partId = quest.GetPartId(questStageMeta, partMeta);
					m_prefs.inventory.RemoveItem(partId, 1);
				}
			}
		}

		private static List<QuestsAsset.QuestReward> GetRewards(QuestsAsset.QuestMeta quest)
		{
			List<QuestsAsset.QuestReward> list = new List<QuestsAsset.QuestReward>();
			QuestsAsset.QuestRewardMeta[] rewards = quest.rewards;
			foreach (QuestsAsset.QuestRewardMeta questRewardMeta in rewards)
			{
				ItemsAsset.ItemMeta itemMeta = m_inventory.Find(questRewardMeta.itemId);
				if (itemMeta != null)
				{
					int count = UnityEngine.Random.Range(questRewardMeta.minDrop, questRewardMeta.maxDrop);
					list.Add(new QuestsAsset.QuestReward
					{
						id = itemMeta.id,
						count = count,
						icon = itemMeta.icon
					});
				}
			}
			rewards = quest.additionalRewards;
			foreach (QuestsAsset.QuestRewardMeta questRewardMeta2 in rewards)
			{
				if (UnityEngine.Random.Range(0f, 1f) <= questRewardMeta2.dropRate)
				{
					ItemsAsset.ItemMeta itemMeta2 = m_inventory.Find(questRewardMeta2.itemId);
					if (itemMeta2 != null)
					{
						int count2 = UnityEngine.Random.Range(questRewardMeta2.minDrop, questRewardMeta2.maxDrop);
						list.Add(new QuestsAsset.QuestReward
						{
							id = itemMeta2.id,
							count = count2,
							icon = itemMeta2.icon
						});
					}
				}
			}
			return list;
		}

		private static bool IsQuestCompleted(QuestsAsset.QuestMeta quest)
		{
			QuestsAsset.QuestStageMeta[] stages = quest.stages;
			foreach (QuestsAsset.QuestStageMeta questStageMeta in stages)
			{
				bool flag = questStageMeta.completeType == QuestsAsset.PartCompleteType.OneOf;
				QuestsAsset.QuestStagePartMeta[] parts = questStageMeta.parts;
				foreach (QuestsAsset.QuestStagePartMeta partMeta in parts)
				{
					string partId = quest.GetPartId(questStageMeta, partMeta);
					InventoryItem item = m_prefs.inventory.GetItem(partId);
					if (!flag && item == null)
					{
						return false;
					}
					if (item != null && flag)
					{
						return true;
					}
				}
			}
			return true;
		}

		public static void Update()
		{
		}
	}
}
