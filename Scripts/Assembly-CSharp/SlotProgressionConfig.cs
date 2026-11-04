using System;
using Quests;
using UnityEngine;

public class SlotProgressionConfig : ScriptableSingleton<SlotProgressionConfig>
{
	[Serializable]
	private class SlotTasksMeta
	{
		public QuestsAsset.QuestStagePartMeta[] tasks;
	}

	[SerializeField]
	private SlotTasksMeta[] m_tasksForUnlockSlot;

	public static QuestsAsset.QuestStagePartMeta[] GetTaskForSlot(int slotIndex)
	{
		SlotTasksMeta slotTasksMeta = FindMetaForSlot(slotIndex);
		if (slotTasksMeta != null && slotTasksMeta.tasks != null)
		{
			return slotTasksMeta.tasks;
		}
		return new QuestsAsset.QuestStagePartMeta[0];
	}

	private static SlotTasksMeta FindMetaForSlot(int slotIndex)
	{
		SlotTasksMeta[] array = null;
		array = ScriptableSingleton<SlotProgressionConfig>.instance.m_tasksForUnlockSlot;
		if (array == null || array.Length == 0)
		{
			return null;
		}
		if (slotIndex >= 0 && slotIndex < array.Length)
		{
			return array[slotIndex];
		}
		return null;
	}
}
