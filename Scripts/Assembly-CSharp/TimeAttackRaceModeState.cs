using DB.Meta;
using UnityEngine;

public class TimeAttackRaceModeState : BaseRaceModeState
{
	public override float? currentLapTime
	{
		get
		{
			if (!base.currentLapTime.HasValue || !base.currentPenaltyTime.HasValue)
			{
				return null;
			}
			return base.currentLapTime.Value + base.currentPenaltyTime.Value;
		}
	}

	public override RaceMode raceMode => RaceMode.TimeAttack;

	protected override void Init()
	{
		base.Init();
		DB.Meta.Track byId = m_model.GetById<DB.Meta.Track>(m_globals.lastTrackId);
		m_totalTimeLimitSeconds = byId.totalTimeLimit;
	}

	protected override void CalculateAward(ref DetailedRaceAward award)
	{
		int carId = m_globals.lastCarId;
		int trackId = m_globals.lastTrackId;
		TrackTimeAttackRaceNormMeta trackTimeAttackRaceNormMeta = m_model.Get((TrackTimeAttackRaceNormMeta x) => x.trackId == trackId && x.carId == carId);
		TrackTimeAttackRaceRewardMeta trackTimeAttackRaceRewardMeta = m_model.Get((TrackTimeAttackRaceRewardMeta x) => x.trackId == trackId);
		if (trackTimeAttackRaceNormMeta != null && trackTimeAttackRaceRewardMeta != null)
		{
			int num = trackTimeAttackRaceRewardMeta.bronzeCoins * 30 / 100;
			int num2 = trackTimeAttackRaceRewardMeta.bronzeCoins * 30 * award.trackProgressPercent / 10000;
			RaceCupType cupByValue = trackTimeAttackRaceNormMeta.GetCupByValue(award.raceTimeSpent);
			int num3 = BaseRaceModeState.CalculateAward(trackTimeAttackRaceRewardMeta.GetAwardByCup(cupByValue));
			award.cupAward = num3;
			award.finishAward = (award.isOutOfTime ? num2 : num);
			award.leftTimeAward = 0;
			if (!award.isOutOfTime && num3 > 0 && (int)m_totalTimeLimitSeconds > 0)
			{
				float normByCup = trackTimeAttackRaceNormMeta.GetNormByCup(cupByValue);
				float num4 = normByCup / award.raceTimeSpent - 1f;
				award.leftTimeAward = Mathf.Clamp(Mathf.RoundToInt(num4 * (float)num3), 0, int.MaxValue);
				award.bonusTime = Mathf.Clamp(normByCup - award.raceTimeSpent, 0f, float.MaxValue);
			}
		}
		else
		{
			Debug.LogError("Track norms not found !");
		}
	}
}
