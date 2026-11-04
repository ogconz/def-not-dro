public class SeasonChangeReward
{
	public enum ERatingDirection
	{
		up = 0,
		down = 1,
		still = 2
	}

	public int finishedPosition;

	public League oldLeague;

	public League newLeague;

	public ERatingDirection direction;

	public Cost reward;
}
