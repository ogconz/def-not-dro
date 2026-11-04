using System;
using System.Text;
using SimpleJSON;

public class Race
{
	public enum ERaceResult
	{
		none = 0,
		canceled = 1,
		in_progress = 2,
		us = 3,
		them = 4,
		tie = 5
	}

	[JsonIgnore]
	public DateTime? createdTime;

	public int? id { get; set; }

	public int? userId { get; set; }

	public int? opponentId { get; set; }

	public int? driftPoints { get; set; }

	public ERaceResult result { get; set; }

	public string type { get; set; }

	public int? ownEloDelta { get; set; }

	public int? opponentEloDelta { get; set; }

	public MultiplayerGhost userGhost { get; set; }

	public MultiplayerGhost opponentGhost { get; set; }

	public Cost reward { get; set; }

	[JsonIgnore]
	public int createdAt
	{
		get
		{
			throw new NotImplementedException();
		}
		set
		{
			createdTime = GeneralExtensions.MakeTime(value);
		}
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("class Race {\n");
		stringBuilder.Append("  Id: ").Append(id).Append("\n");
		stringBuilder.Append("  UserId: ").Append(userId).Append("\n");
		stringBuilder.Append("  OpponentId: ").Append(opponentId).Append("\n");
		stringBuilder.Append("  DriftPoints: ").Append(driftPoints).Append("\n");
		stringBuilder.Append("  Result: ").Append(result).Append("\n");
		stringBuilder.Append("  Type: ").Append(type).Append("\n");
		stringBuilder.Append("  OwnEloDelta: ").Append(ownEloDelta).Append("\n");
		stringBuilder.Append("  OpponentEloDelta: ").Append(opponentEloDelta).Append("\n");
		stringBuilder.Append("  CreatedAt: ").Append(createdTime).Append("\n");
		stringBuilder.Append("}\n");
		return stringBuilder.ToString();
	}

	public string ToJson()
	{
		return JSON.Serialize(this);
	}
}
