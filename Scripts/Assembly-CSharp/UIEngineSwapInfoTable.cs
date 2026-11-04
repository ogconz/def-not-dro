using Binding;
using Binding.Base;
using DG.Tweening;
using Quests;
using UnityEngine;
using UnityEngine.UI;

public class UIEngineSwapInfoTable : BaseBindingBehaviourTarget
{
	[SerializeField]
	private Slider m_progression;

	private readonly SpriteProperty m_swapIcon = new SpriteProperty();

	private readonly StringProperty m_text = new StringProperty();

	private readonly BoolProperty m_tableVisibility = new BoolProperty();

	private readonly BoolProperty m_progressionVisibility = new BoolProperty();

	private readonly StringProperty m_partsCountProgressionLabel = new StringProperty();

	private SwapsAsset m_swapsAsset;

	private QuestsAsset m_questsAsset;

	private void Awake()
	{
		InitializeDB();
	}

	public void SetVisible(bool value)
	{
		m_tableVisibility.value = value;
	}

	private void InitializeDB()
	{
		m_swapsAsset = SwapsAsset.LoadAndInitDB();
		m_questsAsset = QuestsAsset.LoadDB();
	}

	public string GetCurrentQuestRequirementsString()
	{
		return m_text?.value;
	}

	public void UpdateTable(EngineSwapListItem engineSwapItem)
	{
		if (engineSwapItem == null || engineSwapItem.isStock)
		{
			SetVisible(value: false);
			return;
		}
		SetVisible(value: true);
		m_swapIcon.value = engineSwapItem.info.logo;
		if (engineSwapItem.HasInInventory() || GameManager.SwapsDLCInstalled)
		{
			m_progressionVisibility.value = false;
			UpdateDescriptionTextOf(engineSwapItem);
		}
		else
		{
			m_progressionVisibility.value = true;
			UpdateQuestRequirementsOf(engineSwapItem);
			UpdateUnlockProgressionOf(engineSwapItem);
		}
	}

	private void UpdateQuestRequirementsOf(EngineSwapListItem engineSwapItem)
	{
		string text = "\n\n";
		bool flag = true;
		EngineInfo engineInfo = m_swapsAsset.Find(engineSwapItem.info.id);
		if (engineInfo != null)
		{
			bool flag2 = false;
			foreach (QuestsAsset.QuestMeta quest in m_questsAsset.quests)
			{
				QuestsAsset.QuestRewardMeta[] rewards = quest.rewards;
				for (int i = 0; i < rewards.Length; i++)
				{
					if (!(rewards[i].itemId == engineInfo.partItem))
					{
						continue;
					}
					flag2 = true;
					QuestsAsset.QuestStageMeta[] stages = quest.stages;
					foreach (QuestsAsset.QuestStageMeta obj in stages)
					{
						flag = obj.completeType == QuestsAsset.PartCompleteType.OneOf;
						QuestsAsset.QuestStagePartMeta[] parts = obj.parts;
						foreach (QuestsAsset.QuestStagePartMeta questStagePartMeta in parts)
						{
							string format = LocalizationService.Localize(questStagePartMeta.itemId);
							text = text + string.Format(format, questStagePartMeta.completeValue) + "\n";
						}
					}
				}
				if (flag2)
				{
					break;
				}
			}
			if (text[text.Length - 1] == '\n')
			{
				text = text.Substring(0, text.Length - 1);
			}
		}
		string value = (flag ? LocalizationKeys.swaps_unlock_message_one_of_template : LocalizationKeys.swaps_unlock_message_all_template).LocalizedFormat(text);
		m_text.value = value;
	}

	private void UpdateUnlockProgressionOf(EngineSwapListItem engineSwapItem)
	{
		int collectedPartsCount = engineSwapItem.GetCollectedPartsCount();
		int partsToInstall = engineSwapItem.info.partsToInstall;
		m_partsCountProgressionLabel.value = $"{collectedPartsCount}/{partsToInstall}";
		m_progression.DOValue((float)collectedPartsCount / (float)partsToInstall, 1f);
	}

	private void UpdateDescriptionTextOf(EngineSwapListItem engineSwapItem)
	{
		m_text.value = LocalizationService.Localize(engineSwapItem.info.descriptionLocalizationKey);
	}
}
