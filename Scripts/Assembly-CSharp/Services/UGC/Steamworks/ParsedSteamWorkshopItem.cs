using Steamworks;
using Steamworks.Data;
using Steamworks.Ugc;

namespace Services.UGC.Steamworks
{
	public struct ParsedSteamWorkshopItem
	{
		public SteamUGCDetails_t details;

		internal PublishedFileId _id;

		public PublishedFileId Id => _id;

		public string Title { get; internal set; }

		public string Description { get; internal set; }

		public string[] Tags { get; internal set; }

		public AppId CreatorApp => details.CreatorAppID;

		public AppId ConsumerApp => details.ConsumerAppID;

		public Friend Owner => new Friend(details.SteamIDOwner);

		public float Score => details.Score;

		public bool IsPublic => details.Visibility == RemoteStoragePublishedFileVisibility.Public;

		public bool IsFriendsOnly => details.Visibility == RemoteStoragePublishedFileVisibility.FriendsOnly;

		public bool IsPrivate => details.Visibility == RemoteStoragePublishedFileVisibility.Private;

		public bool IsBanned => details.Banned;

		public bool IsAcceptedForUse => details.AcceptedForUse;

		public uint VotesUp => details.VotesUp;

		public uint VotesDown => details.VotesDown;

		public string Url => $"http://steamcommunity.com/sharedfiles/filedetails/?source=Facepunch.Steamworks&id={Id}";

		public string ChangelogUrl => $"http://steamcommunity.com/sharedfiles/filedetails/changelog/{Id}";

		public string CommentsUrl => $"http://steamcommunity.com/sharedfiles/filedetails/comments/{Id}";

		public string DiscussUrl => $"http://steamcommunity.com/sharedfiles/filedetails/discussions/{Id}";

		public string StatsUrl => $"http://steamcommunity.com/sharedfiles/filedetails/stats/{Id}";

		public ulong NumSubscriptions { get; internal set; }

		public ulong NumFavorites { get; internal set; }

		public ulong NumFollowers { get; internal set; }

		public ulong NumUniqueSubscriptions { get; internal set; }

		public ulong NumUniqueFavorites { get; internal set; }

		public ulong NumUniqueFollowers { get; internal set; }

		public ulong NumUniqueWebsiteViews { get; internal set; }

		public ulong ReportScore { get; internal set; }

		public ulong NumSecondsPlayed { get; internal set; }

		public ulong NumPlaytimeSessions { get; internal set; }

		public ulong NumComments { get; internal set; }

		public ulong NumSecondsPlayedDuringTimePeriod { get; internal set; }

		public ulong NumPlaytimeSessionsDuringTimePeriod { get; internal set; }

		public string PreviewImageUrl { get; internal set; }

		public Result Result => details.Result;

		public ParsedSteamWorkshopItem(PublishedFileId id)
		{
			this = default(ParsedSteamWorkshopItem);
			_id = id;
		}

		public bool Download(bool highPriority = false)
		{
			return SteamUGC.Download(Id, highPriority);
		}

		public Editor Edit()
		{
			return new Editor(Id);
		}
	}
}
