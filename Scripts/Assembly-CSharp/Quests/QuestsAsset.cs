using System;
using System.Collections.Generic;
using UnityEngine;

namespace Quests
{
	public class QuestsAsset : ScriptableObject
	{
		public class QuestReward
		{
			public string id;

			public int count;

			public Sprite icon;
		}

		public enum CompleteType
		{
			More = 0,
			MoreOrEqual = 1,
			Less = 2,
			LessOrEqual = 3,
			Equal = 4
		}

		public enum PartCompleteType
		{
			All = 0,
			OneOf = 1
		}

		[Serializable]
		public class QuestStagePartMeta
		{
			public string itemId;

			public int completeValue;

			public CompleteType completeType = CompleteType.MoreOrEqual;
		}

		[Serializable]
		public class QuestStageMeta
		{
			public string itemId;

			public bool permanent;

			public PartCompleteType completeType;

			public QuestStagePartMeta[] parts;
		}

		[Serializable]
		public class QuestRewardMeta
		{
			public string itemId;

			public float dropRate;

			public int minDrop;

			public int maxDrop;
		}

		[Serializable]
		public class QuestMeta
		{
			public string id;

			public QuestStageMeta[] stages;

			public QuestRewardMeta[] rewards;

			public QuestRewardMeta[] additionalRewards;
		}

		[SerializeField]
		private List<QuestMeta> m_quests = new List<QuestMeta>();

		private readonly Dictionary<string, QuestMeta> m_questsDictionary = new Dictionary<string, QuestMeta>();

		public List<QuestMeta> quests => m_quests;

		public static QuestsAsset LoadDB()
		{
			QuestsAsset questsAsset = ResourcesWrapper.Load<QuestsAsset>("DB/QuestsAsset");
			if (questsAsset == null)
			{
				return questsAsset;
			}
			foreach (QuestMeta quest in questsAsset.m_quests)
			{
				questsAsset.m_questsDictionary[quest.id] = quest;
			}
			return questsAsset;
		}

		public QuestMeta Find(string id)
		{
			if (!m_questsDictionary.TryGetValue(id, out var value))
			{
				value = m_quests.Find((QuestMeta x) => x.id == id);
				m_questsDictionary.Add(id, value);
			}
			return value;
		}
	}
}
