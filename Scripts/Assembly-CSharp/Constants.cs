using System;

public static class Constants
{
	public static string LikeUsOnFb = "https://www.facebook.com/carx2";

	public static string LikeUsOnFbMobile = "https://m.facebook.com/carx2";

	public static string Twitter = "https://twitter.com/carx_technology";

	public static string CarxSite = "http://www.carx-tech.com";

	public static string Instagram = "https://instagram.com/carx_tech/";

	public static string Forum = "http://forum.carx-tech.com/";

	public static string PrivacyPolicy = "http://www.carx-tech.com/privacy-car-physics-engine-eng";

	public static string GooglePlayGameUrl = "https://play.google.com/store/apps/details?id=com.CarXTech.CarXDriftRacingFull";

	public static string AppstoreGameUrl = "https://itunes.apple.com/ua/app/carx-drift-racing/id644907661?l=ru&mt=8";

	public static readonly TimeSpan DayInterval = new TimeSpan(24, 0, 0);

	public const string DiscordSiteUrl = "https://discord.gg/bXpFeuU";

	public const string InternetCheckUrl = "https://config.carx-online.com/gcs";

	public static readonly string CarsSettingsJsonName = "CarsSettings";

	public static readonly string InAppsSettingsJsonName = "InAppProducts";

	public static readonly string PushMsgSettingsJsonName = "PushMsgSettings";

	public const float NetworkGhostLeadTime = 1.5f;

	public const float InchToMm = 25.4f;

	public const float MilesToKm = 1.60934f;

	public const float KmToMiles = 0.621371f;

	public const string ScenariosDir = "Scenarios";

	public const string PatchNotesDir = "Patchnotes";

	public static TimeSpan GetDayInterval()
	{
		return DayInterval;
	}
}
