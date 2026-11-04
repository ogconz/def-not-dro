using UnityEngine;

public static class RaceModeExtensions
{
	public static BaseRaceModeState GetState(this RaceMode mode)
	{
		switch (mode)
		{
		case RaceMode.Drift:
			return States.State<DriftRaceModeState>();
		case RaceMode.TimeAttack:
			return States.State<TimeAttackRaceModeState>();
		case RaceMode.Freeride:
			return States.State<FreerideRaceModeState>();
		case RaceMode.Dyno:
			return States.State<DynoRaceModeState>();
		case RaceMode.NetworkFreeride:
			return States.State<SyncNetFreerideRaceModeState>();
		case RaceMode.XDS:
			return States.State<XDSRaceModeState>();
		case RaceMode.HalloweenEvent:
			return States.State<HalloweenEventState>();
		default:
			Debug.LogError("Unsupported race mode: " + mode);
			return States.State<TimeAttackRaceModeState>();
		}
	}

	public static bool IsNetworkRace(this RaceMode mode)
	{
		if (mode != RaceMode.NetworkFreeride)
		{
			return mode == RaceMode.Top32NetRace;
		}
		return true;
	}
}
