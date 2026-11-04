using SimpleJSON;

public class Opponent
{
	public int id;

	public string nickname;

	public int elo;

	public bool isAttackable;

	[JsonIgnore]
	public int? position;
}
