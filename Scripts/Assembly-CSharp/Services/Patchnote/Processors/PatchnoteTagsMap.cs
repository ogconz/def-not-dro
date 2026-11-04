using System.Collections.Generic;
using GameOverlay;

namespace Services.Patchnote.Processors
{
	public class PatchnoteTagsMap
	{
		public const string Prefix = "patchnote";

		public const string EmptyTags = "empty";

		public const string ImagePostfix = "image";

		public const string SpacePostfix = "space";

		public const string HeaderPostfix = "header";

		public const string TitlePostfix = "title";

		private const string SteamPostfix = "steam";

		private const string MyGamesPostfix = "mygames";

		private const string EpicPostfix = "epicgames";

		private const string XboxPostfix = "xbox";

		private const string PS4Postfix = "ps4";

		private const string SwitchPostfix = "switch";

		private const string ExcludePostfixSwitch = "noswitch";

		private const string ExcludePostfixSteam = "nosteam";

		private const string ExcludePostfixEpic = "noepicgames";

		private const string ExcludePostfixMyGames = "nomygames";

		private const string ExcludePostfixXbox = "noxbox";

		private const string ExcludePostfixPS4 = "nops4";

		public readonly Dictionary<UserPlatform.Id, PlatformRelatedTagMap> patchnoteTagPlatform = new Dictionary<UserPlatform.Id, PlatformRelatedTagMap>
		{
			{
				UserPlatform.Id.Steam,
				new PlatformRelatedTagMap
				{
					exclude = new List<string> { "nosteam", "mygames", "epicgames", "xbox", "ps4", "switch" },
					include = new List<string> { "steam" }
				}
			},
			{
				UserPlatform.Id.PS4,
				new PlatformRelatedTagMap
				{
					exclude = new List<string> { "nops4", "steam", "mygames", "epicgames", "xbox", "switch" },
					include = new List<string> { "ps4" }
				}
			},
			{
				UserPlatform.Id.Xbox,
				new PlatformRelatedTagMap
				{
					exclude = new List<string> { "noxbox", "steam", "mygames", "epicgames", "ps4", "switch" },
					include = new List<string> { "xbox" }
				}
			},
			{
				UserPlatform.Id.Switch,
				new PlatformRelatedTagMap
				{
					exclude = new List<string> { "noswitch", "steam", "mygames", "epicgames", "ps4", "xbox" },
					include = new List<string> { "switch" }
				}
			},
			{
				UserPlatform.Id.MyGames,
				new PlatformRelatedTagMap
				{
					exclude = new List<string> { "nomygames", "steam", "epicgames", "ps4", "xbox", "switch" },
					include = new List<string> { "mygames" }
				}
			},
			{
				UserPlatform.Id.EpicGames,
				new PlatformRelatedTagMap
				{
					exclude = new List<string> { "noepicgames", "steam", "mygames", "ps4", "xbox", "switch" },
					include = new List<string> { "epicgames" }
				}
			}
		};

		public PlatformRelatedTagMap GetTagsMap(UserPlatform.Id platform)
		{
			return patchnoteTagPlatform[platform];
		}
	}
}
