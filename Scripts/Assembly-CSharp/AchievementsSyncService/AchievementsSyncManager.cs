using System;
using System.Collections.Generic;

namespace AchievementsSyncService
{
	public static class AchievementsSyncManager
	{
		private static Dictionary<StatsManager.EStats, IAchievementStatusSync> m_achievements = new Dictionary<StatsManager.EStats, IAchievementStatusSync> { [StatsManager.EStats.CupsAllGold] = new AllGoldCupsAchievement() };

		public static void SyncStatus(StatsManager.EStats achievementType)
		{
			try
			{
				m_achievements[achievementType].UpdateStatus();
			}
			catch (Exception ex)
			{
				GameLog.LogError("Achievement validation or acceprion failed");
				GameLog.LogError(ex.Message);
			}
		}
	}
}
