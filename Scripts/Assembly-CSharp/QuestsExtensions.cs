using Quests;

public static class QuestsExtensions
{
	public static string GetPartId(this QuestsAsset.QuestStageMeta stage, string partId)
	{
		if (!stage.permanent)
		{
			return stage.itemId + "_" + partId;
		}
		return partId;
	}

	public static string GetPartId(this QuestsAsset.QuestMeta quest, QuestsAsset.QuestStageMeta stageMeta, QuestsAsset.QuestStagePartMeta partMeta)
	{
		if (!stageMeta.permanent)
		{
			return quest.id + "_" + stageMeta.itemId + "_" + partMeta.itemId;
		}
		return partMeta.itemId;
	}
}
