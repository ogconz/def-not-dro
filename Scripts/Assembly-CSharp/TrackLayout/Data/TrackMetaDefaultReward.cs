using System.Collections.Generic;
using UnityEngine;

namespace TrackLayout.Data
{
	public class TrackMetaDefaultReward : ScriptableObject
	{
		public List<RewardNode> rewards;

		public static TrackMetaDefaultReward Get(RaceMode mode)
		{
			return ResourcesWrapper.Load<TrackMetaDefaultReward>("DefaultRewards/" + mode, "temp");
		}

		public static RewardNode GetNullReward()
		{
			return new RewardNode
			{
				coinsReward = 0,
				expReward = 0
			};
		}
	}
}
