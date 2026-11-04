using System;
using SimpleJSON;

public class Season
{
	[JsonIgnore]
	public DateTime? seasonEndTime;

	[JsonIgnore]
	public DateTime? seasonStartTime;

	public int seasonNumber { get; set; }

	[JsonIgnore]
	public int seasonStartedIn
	{
		get
		{
			throw new NotImplementedException();
		}
		set
		{
			seasonStartTime = GeneralExtensions.MakeTime(value);
		}
	}

	[JsonIgnore]
	public int seasonEndsIn
	{
		get
		{
			throw new NotImplementedException();
		}
		set
		{
			seasonEndTime = GeneralExtensions.MakeTime(value);
		}
	}

	public int seasonDaysPassed { get; set; }

	public float? secondsTillTheSeasonEnds
	{
		get
		{
			if (!seasonEndTime.HasValue)
			{
				return null;
			}
			return (float)(seasonEndTime.Value - DateTime.Now).TotalSeconds;
		}
	}
}
