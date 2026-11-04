public static class AchievementTypeExtension
{
	public static StatsManager.EStats ToStat(this BaseDriftAchievement.AchievementType type)
	{
		return type switch
		{
			BaseDriftAchievement.AchievementType.Speed => StatsManager.EStats.ATSpeed, 
			BaseDriftAchievement.AchievementType.SideChange => StatsManager.EStats.ATSideChange, 
			BaseDriftAchievement.AchievementType.Angle => StatsManager.EStats.ATAngle, 
			BaseDriftAchievement.AchievementType.Accuracy => StatsManager.EStats.ATAccuracy, 
			BaseDriftAchievement.AchievementType.Quality => StatsManager.EStats.ATQuality, 
			BaseDriftAchievement.AchievementType.Backward => StatsManager.EStats.ATBackward, 
			BaseDriftAchievement.AchievementType.ClippingZone => StatsManager.EStats.ATClippingZone, 
			BaseDriftAchievement.AchievementType.ClippingPoint => StatsManager.EStats.ATClippingPoint, 
			_ => StatsManager.EStats.None, 
		};
	}
}
