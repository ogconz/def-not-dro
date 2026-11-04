using System;
using UnityEngine;

public class PS4TrophyData : ScriptableObject
{
	[Serializable]
	public class Item
	{
		public StatsManager.EStats type;

		public float targetValue;
	}

	public Item[] values;
}
