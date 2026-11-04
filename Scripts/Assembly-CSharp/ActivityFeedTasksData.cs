using UnityEngine;

public class ActivityFeedTasksData : ScriptableObject
{
	public int maxPointsPerRace = 50000;

	public int leaderPointsSingleTandem = 95;

	public int totalPointsSingleTandemDrift = 180;

	[Header("Network races need to accomplish")]
	public int[] networkRaces = new int[6] { 10, 25, 50, 100, 150, 200 };

	[Header("Races vs friends need to accomplish")]
	public int[] friendRaces = new int[6] { 5, 10, 15, 20, 25, 30 };
}
