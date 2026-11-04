using GameOverlay;

namespace Chat.Protocol
{
	public static class PlatformMapping
	{
		public static PlatformAuthType PlatformToAuth(UserPlatform.Id platform)
		{
			return platform switch
			{
				UserPlatform.Id.Steam => PlatformAuthType.Steam, 
				UserPlatform.Id.PS4 => PlatformAuthType.Psn, 
				UserPlatform.Id.Xbox => PlatformAuthType.Xbox, 
				UserPlatform.Id.Switch => PlatformAuthType.Nintendo, 
				UserPlatform.Id.MyGames => PlatformAuthType.My_Games, 
				UserPlatform.Id.EpicGames => PlatformAuthType.Epic_Games, 
				_ => PlatformAuthType.Steam, 
			};
		}

		public static UserPlatform.Id AuthToPlatform(PlatformAuthType authType)
		{
			return authType switch
			{
				PlatformAuthType.Steam => UserPlatform.Id.Steam, 
				PlatformAuthType.Psn => UserPlatform.Id.PS4, 
				PlatformAuthType.Xbox => UserPlatform.Id.Xbox, 
				PlatformAuthType.Nintendo => UserPlatform.Id.Switch, 
				PlatformAuthType.My_Games => UserPlatform.Id.MyGames, 
				PlatformAuthType.Epic_Games => UserPlatform.Id.EpicGames, 
				_ => UserPlatform.Id.Unknown, 
			};
		}
	}
}
