using System;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("")]
public abstract class BaseGameRankingService : BaseLoggingMonobeh
{
	public enum TimeScope
	{
		Today = 0,
		Week = 1,
		AllTime = 2
	}

	public class UserScore
	{
		public string id;

		public int rank;

		public string alias;

		public float value;
	}

	public class UserData
	{
		public string id;

		public Texture2D photo;
	}

	public event Action<bool> OnPlayerAuthenticated;

	public event Action<bool> OnScoreReported;

	public event Action<UserScore> OnLocalUserScoreRetrieved;

	public event Action<UserData> OnLocalUserDataRetrieved;

	public event Action<List<UserScore>, int> OnScoresRetrieved;

	public event Action<List<UserData>> OnPlayerDataLoaded;

	public event Action<bool> OnLeaderboardDismissed;

	public abstract bool IsUseNativeUI();

	public abstract bool IsPlayerAuthenticated();

	public abstract void AuthenticatePlayer();

	public abstract void ReportScore(int carID, int profileID, int trackID, RaceMode raceMode, float score);

	public abstract void LoadLocalUserScore(int carID, int profileID, int trackID, RaceMode raceMode, TimeScope timeScope, bool friendsOnly);

	public abstract void ShowLeaderboard(int carID, int profileID, int trackID, RaceMode raceMode);

	public abstract void LoadScores(int carID, int profileID, int trackID, RaceMode raceMode, int start, int end, TimeScope timeScope, bool friendsOnly);

	public abstract void LoadPlayerData(string[] playerIDs);

	public abstract void LoadLocalUserPhoto();

	public abstract string GetLogoSpriteName();

	protected void FirePlayerAuthenticated(bool success)
	{
		if (this.OnPlayerAuthenticated != null)
		{
			this.OnPlayerAuthenticated(success);
		}
	}

	protected void FireScoreReported(bool success)
	{
		if (this.OnScoreReported != null)
		{
			this.OnScoreReported(success);
		}
	}

	protected void FireLocalUserScoreRetrieved(UserScore score)
	{
		if (this.OnLocalUserScoreRetrieved != null)
		{
			this.OnLocalUserScoreRetrieved(score);
		}
	}

	protected void FireLocalUserDataRetrieved(UserData data)
	{
		if (this.OnLocalUserDataRetrieved != null)
		{
			this.OnLocalUserDataRetrieved(data);
		}
	}

	protected void FireScoresRetrieved(List<UserScore> scores, int maxRange)
	{
		if (this.OnScoresRetrieved != null)
		{
			this.OnScoresRetrieved(scores, maxRange);
		}
	}

	protected void FirePlayerDataLoaded(List<UserData> data)
	{
		if (this.OnPlayerDataLoaded != null)
		{
			this.OnPlayerDataLoaded(data);
		}
	}

	protected void FireLeaderboardDismissed(bool success)
	{
		if (this.OnLeaderboardDismissed != null)
		{
			this.OnLeaderboardDismissed(success);
		}
	}
}
