using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Scripts/Services/GameRankingService/EditorGameRankingService")]
public class EditorGameRankingService : BaseGameRankingService
{
	public override bool IsUseNativeUI()
	{
		return false;
	}

	public override bool IsPlayerAuthenticated()
	{
		return true;
	}

	public override void AuthenticatePlayer()
	{
		FirePlayerAuthenticated(success: true);
	}

	public override void ReportScore(int carID, int profileID, int trackID, RaceMode raceMode, float score)
	{
		FireScoreReported(success: true);
	}

	public override void LoadScores(int carId, int profileId, int trackId, RaceMode raceMode, int start, int end, TimeScope timeScope, bool friendsOnly)
	{
		List<UserScore> list = new List<UserScore>();
		for (int i = 1; i < 100; i++)
		{
			list.Add(new UserScore
			{
				id = i.ToString(),
				rank = i + 1,
				alias = $"User {i}",
				value = i
			});
		}
		FireScoresRetrieved(list, 100);
	}

	public override void LoadPlayerData(string[] playerIDs)
	{
	}

	public override void LoadLocalUserScore(int carId, int profileId, int trackId, RaceMode raceMode, TimeScope timeScope, bool friendsOnly)
	{
		FireLocalUserScoreRetrieved(new UserScore
		{
			id = "0",
			rank = 1,
			alias = "LocalUser",
			value = 0f
		});
	}

	public override void LoadLocalUserPhoto()
	{
		FireLocalUserDataRetrieved(new UserData
		{
			id = "0",
			photo = null
		});
	}

	public override void ShowLeaderboard(int carID, int profileID, int trackID, RaceMode raceMode)
	{
	}

	public override string GetLogoSpriteName()
	{
		return "GameCenter";
	}
}
