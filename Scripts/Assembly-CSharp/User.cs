using System;
using SimpleJSON;

public class User
{
	[JsonIgnore]
	public DateTime? fuelRestoresTime;

	public int? id { get; set; }

	public string nickname { get; set; }

	public int? elo { get; set; }

	public Season season { get; set; }

	public int? position { get; set; }

	public int? fuel { get; set; }

	public int? maxFuel { get; set; }

	public bool? purchasedMaxFuel4Cash { get; set; }

	public MultiplayerGhost ghost { get; set; }

	public Error.EType qualiNotAvailableReason { get; set; }

	public Error.EType battleNotAvailableReason { get; set; }

	public int qualiAttemptsLeft { get; set; }

	[JsonIgnore]
	public int fuelRestoresIn
	{
		get
		{
			throw new NotImplementedException();
		}
		set
		{
			fuelRestoresTime = GeneralExtensions.MakeTime(value);
		}
	}

	public float? secondsTillFuelRestore
	{
		get
		{
			if (!fuelRestoresTime.HasValue)
			{
				return null;
			}
			return (float)(fuelRestoresTime.Value - DateTime.Now).TotalSeconds;
		}
	}
}
