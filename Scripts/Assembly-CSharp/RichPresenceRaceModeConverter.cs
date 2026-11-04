using System.Collections.Generic;

public class RichPresenceRaceModeConverter : IRichPresenceConverter<RaceMode>, IRichPresenceConverter
{
	private Dictionary<RaceMode, string> m_raceModeKvp = new Dictionary<RaceMode, string>
	{
		{
			RaceMode.Drift,
			"Drift"
		},
		{
			RaceMode.TimeAttack,
			"TimeAttack"
		},
		{
			RaceMode.Freeride,
			"Freeride"
		},
		{
			RaceMode.Dyno,
			"Dyno"
		},
		{
			RaceMode.NetworkFreeride,
			"NetworkFreeride"
		},
		{
			RaceMode.XDS,
			"XDS"
		},
		{
			RaceMode.Top32,
			"top32"
		},
		{
			RaceMode.Top32NetRace,
			"top32netrace"
		},
		{
			RaceMode.Top32ViewReplay,
			"top32viewreplay"
		}
	};

	public bool TryConvertFrom(RaceMode raceMode, out string result)
	{
		return m_raceModeKvp.TryGetValue(raceMode, out result);
	}
}
