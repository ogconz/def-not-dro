using System.Collections.Generic;
using UnityEngine;

namespace TrackLayout.Data
{
	public class TrackMetaConfig : ScriptableObject
	{
		public TrackLayoutData data;

		public float rewardFactor = 1f;

		public string GetLocalizedName()
		{
			return LocalizationKeys.xds_ui_config.LocalizedFormat(base.name);
		}

		public string EncodeName(RaceMode mode, string sceneName)
		{
			return mode.ToString() + "_" + sceneName + "_" + base.name;
		}

		public List<RewardNode> GetRewardListFor(RaceMode mode)
		{
			List<RewardNode> list = TrackMetaDefaultReward.Get(mode)?.rewards;
			if (list == null || list.Count == 0)
			{
				return new List<RewardNode>();
			}
			List<RewardNode> list2 = new List<RewardNode>();
			for (int i = 0; i < list.Count; i++)
			{
				RewardNode rewardNode = list[i];
				list2.Add(new RewardNode
				{
					value = rewardNode.value,
					coinsReward = Mathf.RoundToInt((float)rewardNode.coinsReward * rewardFactor),
					expReward = Mathf.RoundToInt((float)rewardNode.expReward * rewardFactor)
				});
			}
			return list2;
		}

		public RewardNode GetRewardFor(RaceMode mode, float points)
		{
			List<RewardNode> list = TrackMetaDefaultReward.Get(mode)?.rewards;
			if (list == null || list.Count == 0)
			{
				return TrackMetaDefaultReward.GetNullReward();
			}
			foreach (RewardNode item in list)
			{
				if (points >= item.value)
				{
					return new RewardNode
					{
						coinsReward = Mathf.RoundToInt((float)item.coinsReward * rewardFactor),
						expReward = Mathf.RoundToInt((float)item.expReward * rewardFactor)
					};
				}
			}
			return TrackMetaDefaultReward.GetNullReward();
		}
	}
}
