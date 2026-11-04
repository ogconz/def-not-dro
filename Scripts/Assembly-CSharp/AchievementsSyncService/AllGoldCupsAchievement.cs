using System.Collections.Generic;
using DB;
using DB.Meta;
using DI;

namespace AchievementsSyncService
{
	public sealed class AllGoldCupsAchievement : IAchievementStatusSync
	{
		private BaseModel m_model;

		private GamePrefs m_prefs;

		public AllGoldCupsAchievement()
		{
			m_model = DependencyInjector.Resolve<BaseModel>();
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
		}

		public void UpdateStatus()
		{
			if (IsUnlocked())
			{
				StatsManager.SetStat(StatsManager.EStats.CupsAllGold, 1);
			}
		}

		private bool IsUnlocked()
		{
			List<DB.Meta.Track> list = m_model.QueryAll<DB.Meta.Track>();
			List<PlayerCarProfile> list2 = m_model.QueryAll<PlayerCarProfile>();
			foreach (DB.Meta.Track item in list)
			{
				if (ExcludedTrack(item))
				{
					continue;
				}
				bool flag = false;
				foreach (PlayerCarProfile item2 in list2)
				{
					if (CheckGoldCupForProfile(item, item2))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					return false;
				}
			}
			return true;
		}

		private bool CheckGoldCupForProfile(DB.Meta.Track track, PlayerCarProfile profile)
		{
			if (GetCupForDrift(track, profile) == RaceCupType.Gold)
			{
				return true;
			}
			if (GetCupForTimeAttack(track, profile) == RaceCupType.Gold)
			{
				return true;
			}
			return false;
		}

		private RaceCupType GetCupForDrift(DB.Meta.Track track, PlayerCarProfile profile)
		{
			RaceResult raceResult = m_prefs.stats.GetRaceResult(track.id, profile.carId, profile.id, RaceMode.Drift);
			if (raceResult == null)
			{
				return RaceCupType.None;
			}
			return m_model.Get((TrackDriftRaceNormMeta x) => x.trackId == track.id)?.GetCupByValue(raceResult.recordDriftPoints) ?? RaceCupType.None;
		}

		private RaceCupType GetCupForTimeAttack(DB.Meta.Track track, PlayerCarProfile profile)
		{
			RaceResult raceResult = m_prefs.stats.GetRaceResult(track.id, profile.carId, profile.id, RaceMode.TimeAttack);
			if (raceResult == null)
			{
				return RaceCupType.None;
			}
			return m_model.Get((TrackTimeAttackRaceNormMeta x) => x.trackId == track.id && x.carId == profile.carId)?.GetCupByValue(raceResult.GetBestLapTime()) ?? RaceCupType.None;
		}

		private bool ExcludedTrack(DB.Meta.Track track)
		{
			if (track.enabled && !track.hidden)
			{
				return track.external;
			}
			return true;
		}
	}
}
