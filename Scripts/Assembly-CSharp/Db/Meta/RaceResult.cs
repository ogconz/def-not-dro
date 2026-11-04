using System;

namespace DB.Meta
{
	public class RaceResult
	{
		public int carId { get; set; }

		public int profileId { get; set; }

		public int trackId { get; set; }

		public int raceModeId { get; set; }

		public int bestLapTime { get; set; }

		public int recordDriftPoints { get; set; }

		public RaceMode raceMode
		{
			get
			{
				if (!Enum.IsDefined(typeof(RaceMode), raceModeId))
				{
					return RaceMode.TimeAttack;
				}
				return (RaceMode)raceModeId;
			}
		}

		public void SetBestLapTime(float time)
		{
			bestLapTime = (int)Math.Floor((double)time * 10000.0);
		}

		public float GetBestLapTime()
		{
			return (float)((double)bestLapTime / 10000.0);
		}

		public bool IsBetter(RaceResult thanOther)
		{
			switch (raceMode)
			{
			case RaceMode.Drift:
				if (recordDriftPoints > thanOther.recordDriftPoints)
				{
					return true;
				}
				if (recordDriftPoints == thanOther.recordDriftPoints)
				{
					return bestLapTime < thanOther.bestLapTime;
				}
				return false;
			case RaceMode.TimeAttack:
				return bestLapTime < thanOther.bestLapTime;
			case RaceMode.Freeride:
			case RaceMode.NetworkFreeride:
				return recordDriftPoints > thanOther.recordDriftPoints;
			case RaceMode.Top32:
			case RaceMode.Top32NetRace:
			case RaceMode.Top32ViewReplay:
				return false;
			default:
				GameLog.Print(AlertLevel.Error, "Debug", "Unsupported RaceMode in RaceResult");
				return false;
			}
		}
	}
}
