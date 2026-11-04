using System.Collections.Generic;
using Binding;
using Binding.Base;
using UnityEngine;

public class UIPostCombatRewardContext : BaseBindingBehaviourTarget
{
	private const string ID_XP = "xp";

	private const string ID_COINS = "coins";

	private readonly BoolProperty m_hasRewards = new BoolProperty(false);

	private readonly StringProperty m_rewardText = new StringProperty();

	[SerializeField]
	private Sprite m_coinsSprite;

	[SerializeField]
	private Sprite m_xpSprite;

	[SerializeField]
	private GameObject m_rewardItem;

	[SerializeField]
	private RectTransform m_rewardsContainer;

	[SerializeField]
	private Sprite m_questIconSprite;

	private readonly List<UIRewardItem> m_rewards = new List<UIRewardItem>();

	public void Init(string customText = "")
	{
		m_rewards.Clear();
		if (m_rewardsContainer != null)
		{
			m_rewardsContainer.DestroyChildren();
		}
		m_hasRewards.value = false;
		m_rewardText.value = (string.IsNullOrWhiteSpace(customText) ? LocalizationKeys.reward.Localized() : customText);
	}

	public void AddCoinsReward(int count, int multiplier = 1)
	{
		if (count != 0)
		{
			AddReward(m_coinsSprite, "coins", count, multiplier);
		}
	}

	public void AddXpReward(int count, int multiplier = 1)
	{
		if (count != 0)
		{
			AddReward(m_xpSprite, "xp", count, multiplier);
		}
	}

	public void AddReward(Sprite sprite, string itemId, int count, int multiplier = 1)
	{
		if (!GameManager.SwapsDLCInstalled || !itemId.StartsWith("engine_part_"))
		{
			UIRewardItem component = Object.Instantiate(m_rewardItem, m_rewardsContainer).GetComponent<UIRewardItem>();
			if (component != null)
			{
				component.SetItemData(sprite, UIUtils.DigitsWithSeparate(count), multiplier);
				m_rewards.Add(component);
			}
			m_hasRewards.value = m_rewards.Count > 0;
		}
	}
}
