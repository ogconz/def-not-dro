using System;
using System.Collections.Generic;

namespace GameOverlay
{
	public static class UserPlatform
	{
		public enum Id
		{
			Unknown = 0,
			Steam = 1,
			PS4 = 2,
			Xbox = 3,
			Switch = 4,
			MyGames = 5,
			EpicGames = 6,
			Void = 7,
			_Count = 8
		}

		public static int AllMask => 254;

		public static int ToMask(Id id)
		{
			return 1 << (int)id;
		}

		public static IEnumerable<Id> GetPlatformsFromMask(int platformMask)
		{
			foreach (Id value in Enum.GetValues(typeof(Id)))
			{
				switch (value)
				{
				case Id.Unknown:
					continue;
				case Id._Count:
					yield break;
				}
				if ((platformMask & ToMask(value)) != 0)
				{
					yield return value;
				}
			}
		}

		public static bool IsNeedShowLocalPlatformIcon(Id platformId)
		{
			return Overlay.instance.GetPlatform() switch
			{
				Id.PS4 => platformId != Id.PS4, 
				Id.Xbox => platformId != Id.Xbox, 
				Id.Switch => platformId != Id.Switch, 
				Id.Steam => platformId != Id.Steam, 
				Id.MyGames => platformId != Id.MyGames, 
				Id.EpicGames => platformId != Id.EpicGames, 
				Id.Void => false, 
				_ => true, 
			};
		}
	}
}
