using SimpleJSON;

public class TopReplay
{
	public MultiplayerGhost.ClientData userData;

	public MultiplayerGhost.ClientData oppData;

	[JsonIgnore]
	public MultiplayerGhost multiplayerUserGhost;

	[JsonIgnore]
	public MultiplayerGhost multiplayerOpponentGhost;

	[JsonIgnore]
	public int leagueId;

	public string raceId { get; set; }

	public string userName { get; set; }

	public string userGhost { get; set; }

	public int? userElo { get; set; }

	public string oppName { get; set; }

	public string oppGhost { get; set; }

	public int? oppElo { get; set; }

	public string scoreReason { get; set; }

	public int likeCount { get; set; }

	public int viewCount { get; set; }

	public int calculationPeriod { get; set; }

	public long calculationDate { get; set; }

	public League.LeagueClientSettings mapInfo { get; set; }
}
