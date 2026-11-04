using System.Collections.Generic;

public class Session
{
	public User user { get; set; }

	public string sessionKey { get; set; }

	public List<Opponent> ratings { get; set; }

	public League league { get; set; }

	public DeprecatedSettings config { get; set; }

	public SeasonChangeReward seasonChangeReward { get; set; }
}
