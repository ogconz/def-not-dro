using System;
using System.Collections.Generic;
using ArrayExtension;
using UnityEngine;

public class ActivityFeedService
{
	public enum StoryType
	{
		Unknown = -1,
		BuyCar = 0,
		BuyTrack = 1,
		DoneNetworkRace = 2,
		LevelUnlocked = 3,
		MPWonRaceVsFriend = 4,
		GainRacePoints = 5,
		SingleTandemLeaderPoints = 6,
		SingleTandemTotalPoints = 7,
		MPWonTandemDrift = 8,
		MPWonTandemDriftXDS = 9
	}

	public class StoryData
	{
		public Caption[] captions;

		public Caption[] condensedCaptions;

		public string largeImgURL;

		public int subType;
	}

	public struct Caption
	{
		public string captionStr;

		public string languageCode;
	}

	public static readonly string VISIT_WEBSITE_URL = "https://carx-online.com/ps4/";

	private static readonly string BOUGHT_CAR_URL = "/bought_car.jpg";

	private static readonly string BOUGHT_TRACK_URL = "/bought_track.jpg";

	private static readonly string DONE_NETWORK_RACES_URL = "/done_network_races.jpg";

	private static readonly string UNLOCKED_LEVEL_URL = "/unlocked_level.jpg";

	private static readonly string FRIEND_RACES_URL = "/friend_races.jpg";

	private static readonly string GAIN_RACE_POINTS_URL = "/gain_race_points.jpg";

	private static readonly string SINGLE_TANDEM_LEADER_POINTS_URL = "/single_tandem_leader_points.jpg";

	private static readonly string SINGLE_TANDEM_TOTAL_POINTS_URL = "/single_tandem_total_points.jpg";

	private static readonly string MP_WON_TANDEM_URL = "/mp_won_tandem_drift.jpg";

	private static readonly string MP_WON_TANDEM_XDS_URL = "/mp_won_tandem_drift_xds.jpg";

	public static Action<StoryData> postFeedAction;

	protected static ActivityFeedTasksData m_feedTasksData;

	protected static IList<LocalizationLanguageMeta> m_allLanguages;

	protected static GamePrefs m_prefs;

	public static void ProcessStory(StoryType storyType, object data = null)
	{
		DoProcessStory(forcePost: false, storyType, data);
	}

	private static void DoProcessStory(bool forcePost, StoryType storyType, object data = null)
	{
		if (m_feedTasksData == null || string.IsNullOrEmpty(WebConfigManager.CdnRootUrl))
		{
			return;
		}
		StoryData storyData = new StoryData();
		storyData.subType = (int)storyType;
		bool flag = false;
		switch (storyType)
		{
		case StoryType.BuyCar:
		{
			string text2 = string.Empty;
			if (data != null)
			{
				text2 = (string)data;
			}
			storyData.captions = FillCaptions(LocalizationKeys.activity_feed_bought_car.KeyValue, text2);
			storyData.condensedCaptions = FillCaptions(LocalizationKeys.condensed_activity_feed_bought_car.KeyValue);
			storyData.largeImgURL = BOUGHT_CAR_URL;
			flag = true;
			break;
		}
		case StoryType.BuyTrack:
		{
			string text = string.Empty;
			if (data != null)
			{
				text = (string)data;
			}
			storyData.captions = FillCaptions(LocalizationKeys.activity_feed_bought_track.KeyValue, text);
			storyData.condensedCaptions = FillCaptions(LocalizationKeys.condensed_activity_feed_bought_track.KeyValue);
			storyData.largeImgURL = BOUGHT_TRACK_URL;
			flag = true;
			break;
		}
		case StoryType.DoneNetworkRace:
		{
			int racesPlayed = 0;
			if (data != null)
			{
				racesPlayed = (int)data;
			}
			if (forcePost || (m_feedTasksData != null && m_feedTasksData.networkRaces.IsNonEmpty()))
			{
				bool flag2 = Array.Exists(m_feedTasksData.networkRaces, (int needRaces) => needRaces == racesPlayed);
				if (forcePost || flag2)
				{
					storyData.captions = FillCaptions(LocalizationKeys.activity_feed_done_network_races.KeyValue, racesPlayed);
					storyData.condensedCaptions = FillCaptions(LocalizationKeys.condensed_activity_feed_done_network_races.KeyValue);
					storyData.largeImgURL = DONE_NETWORK_RACES_URL;
					flag = true;
				}
			}
			break;
		}
		case StoryType.LevelUnlocked:
		{
			int num4 = 0;
			if (data != null)
			{
				num4 = (int)data;
			}
			if (num4 > 1)
			{
				storyData.captions = FillCaptions(LocalizationKeys.activity_feed_level_unlocked.KeyValue, num4);
				storyData.condensedCaptions = FillCaptions(LocalizationKeys.condensed_activity_feed_level_unlocked.KeyValue);
				storyData.largeImgURL = UNLOCKED_LEVEL_URL;
				flag = true;
			}
			break;
		}
		case StoryType.MPWonRaceVsFriend:
		{
			StatsManager.GetStat(StatsManager.EStats.MpFriendWins, out int friendsMPWinsRacesCount);
			if (forcePost || (m_feedTasksData != null && m_feedTasksData.friendRaces.IsNonEmpty()))
			{
				int[] friendRaces = m_feedTasksData.friendRaces;
				if (forcePost || Array.Exists(friendRaces, (int race) => race == friendsMPWinsRacesCount))
				{
					storyData.captions = FillCaptions(LocalizationKeys.activity_feed_friend_races.KeyValue, friendsMPWinsRacesCount);
					storyData.condensedCaptions = FillCaptions(LocalizationKeys.condensed_activity_feed_friend_races.KeyValue);
					storyData.largeImgURL = FRIEND_RACES_URL;
					flag = true;
				}
			}
			break;
		}
		case StoryType.GainRacePoints:
		{
			int num2 = 0;
			if (data != null)
			{
				num2 = (int)data;
			}
			if (forcePost || num2 >= m_feedTasksData.maxPointsPerRace)
			{
				storyData.captions = FillCaptions(LocalizationKeys.activity_feed_gain_race_points.KeyValue, num2);
				storyData.condensedCaptions = FillCaptions(LocalizationKeys.condensed_activity_feed_gain_race_points.KeyValue);
				storyData.largeImgURL = GAIN_RACE_POINTS_URL;
				flag = true;
			}
			break;
		}
		case StoryType.SingleTandemLeaderPoints:
		{
			float num3 = 0f;
			if (data != null)
			{
				num3 = (float)data;
			}
			if (forcePost || num3 >= (float)m_feedTasksData.leaderPointsSingleTandem)
			{
				storyData.captions = FillCaptions(LocalizationKeys.activity_feed_single_tandem_leader.KeyValue, num3);
				storyData.condensedCaptions = FillCaptions(LocalizationKeys.condensed_activity_feed_single_tandem_leader.KeyValue);
				storyData.largeImgURL = SINGLE_TANDEM_LEADER_POINTS_URL;
				flag = true;
			}
			break;
		}
		case StoryType.SingleTandemTotalPoints:
		{
			float num = 0f;
			if (data != null)
			{
				num = (float)data;
			}
			if (forcePost || num >= (float)m_feedTasksData.totalPointsSingleTandemDrift)
			{
				storyData.captions = FillCaptions(LocalizationKeys.activity_feed_single_tandem_total.KeyValue, num);
				storyData.condensedCaptions = FillCaptions(LocalizationKeys.condensed_activity_feed_single_tandem_total.KeyValue);
				storyData.largeImgURL = SINGLE_TANDEM_TOTAL_POINTS_URL;
				flag = true;
			}
			break;
		}
		case StoryType.MPWonTandemDrift:
			storyData.captions = FillCaptions(LocalizationKeys.activity_feed_win_mp_tandem.KeyValue);
			storyData.condensedCaptions = FillCaptions(LocalizationKeys.condensed_activity_feed_win_mp_tandem.KeyValue);
			storyData.largeImgURL = MP_WON_TANDEM_URL;
			flag = true;
			break;
		case StoryType.MPWonTandemDriftXDS:
			storyData.captions = FillCaptions(LocalizationKeys.activity_feed_win_mp_tandem_xds.KeyValue);
			storyData.condensedCaptions = FillCaptions(LocalizationKeys.condensed_activity_feed_win_mp_tandem_xds.KeyValue);
			storyData.largeImgURL = MP_WON_TANDEM_XDS_URL;
			flag = true;
			break;
		}
	}

	private static Caption[] FillCaptions(string key, params object[] args)
	{
		List<Caption> list = new List<Caption>();
		for (int i = 0; i < m_allLanguages.Count; i++)
		{
			SystemLanguage languageType = m_allLanguages[i].languageType;
			string languageTranslate = LocalizationService.GetLanguageTranslate(languageType, LocalizationKeys.localization_id.KeyValue);
			string languageTranslate2 = LocalizationService.GetLanguageTranslate(languageType, key, args);
			if (!string.IsNullOrEmpty(languageTranslate2) && !string.IsNullOrEmpty(languageTranslate))
			{
				Caption item = new Caption
				{
					languageCode = languageTranslate,
					captionStr = languageTranslate2
				};
				list.Add(item);
			}
		}
		return list.ToArray();
	}
}
