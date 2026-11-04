using GameOverlay;
using Steamworks;

public class SteamStatsWorker : IStatsWorker
{
	public void Init()
	{
		SteamUserStats.RequestCurrentStats();
	}

	public void SetStat(string key, int value)
	{
		if (!Overlay.instance.isShut)
		{
			if (!SteamUserStats.SetStat(key, value))
			{
				GameLog.Print(AlertLevel.Error, "Achievements", "SetState is fail");
			}
			if (!SteamUserStats.StoreStats())
			{
				GameLog.Print(AlertLevel.Error, "Achievements", "Stats not saved");
			}
		}
	}

	public void SetStat(string key, float value)
	{
		if (!Overlay.instance.isShut)
		{
			if (!SteamUserStats.SetStat(key, value))
			{
				GameLog.Print(AlertLevel.Error, "Achievements", "SetState is fail");
			}
			if (!SteamUserStats.StoreStats())
			{
				GameLog.Print(AlertLevel.Error, "Achievements", "Stats not saved");
			}
		}
	}

	public void GetStat(string key, out int value)
	{
		value = SteamUserStats.GetStatInt(key);
	}

	public void GetStat(string key, out float value)
	{
		value = SteamUserStats.GetStatFloat(key);
	}

	public void ClearStats()
	{
		SteamUserStats.ResetAll(includeAchievements: true);
		SteamUserStats.StoreStats();
	}
}
