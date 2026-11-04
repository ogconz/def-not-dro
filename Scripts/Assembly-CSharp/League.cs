using System;
using SimpleJSON;

public class League
{
	public class LeagueClientSettings
	{
		public string mapName;

		public string configurationName;

		public bool IsEmpty()
		{
			if (!string.IsNullOrEmpty(mapName))
			{
				return string.IsNullOrEmpty(configurationName);
			}
			return true;
		}
	}

	public class Group
	{
		public int lessOrEqualThan;

		public Cost reward;

		public SeasonChangeReward.ERatingDirection direction;

		[JsonIgnore]
		public int number;
	}

	public int id;

	public string name;

	public LeagueClientSettings clientSettings;

	public int playerSlotsCount;

	public Group[] groups;

	public Cost refuelCost = new Cost(0, 60);

	public Cost maxFuelIncreaseCost = new Cost(0, 480);

	public Cost additionalQualiCost = new Cost(0, 120);

	public Cost trainToQualiConversionCost = new Cost(0, 360);

	public Cost trainingCost = new Cost(400, 0);

	public const int MaxLeagueId = 5;

	public void Prepare()
	{
		Array.Sort(groups, (Group g1, Group g2) => g1.lessOrEqualThan.CompareTo(g2.lessOrEqualThan));
		int num = 1;
		Group[] array = groups;
		for (int num2 = 0; num2 < array.Length; num2++)
		{
			array[num2].number = num++;
		}
	}

	public int LeagueNumber()
	{
		return 5 - id + 1;
	}
}
