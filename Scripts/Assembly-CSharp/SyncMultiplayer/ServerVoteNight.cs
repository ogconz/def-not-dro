using System.Collections.Generic;
using Sfs2X.Entities.Data;

namespace SyncMultiplayer
{
	public class ServerVoteNight : IServerVote
	{
		private NetworkGame m_game;

		private int m_nightModeInt;

		private string m_nightKeyString;

		private bool m_isNightMode;

		private string m_lightPresetName = string.Empty;

		public VoteType voteType => VoteType.Night;

		public static void PrepareVoteData(SFSObject data, Dictionary<string, string> parameters)
		{
			data.PutUtfString("vt", "NIGHT");
			data.PutUtfString("id", parameters["id"]);
			data.PutUtfString("h", parameters["h"]);
			data.PutUtfString("cm", parameters["additional"]);
		}

		public static ServerVoteNight VoteStarted(NetworkGame game, ISFSObject parameters)
		{
			ServerVoteNight serverVoteNight = new ServerVoteNight();
			serverVoteNight.m_game = game;
			if (!parameters.ContainsKey("id"))
			{
				GameLog.Print(AlertLevel.Error, "Vote", "ServerVoteNight: target is not specified");
				return null;
			}
			string utfString = parameters.GetUtfString("id");
			if (!int.TryParse(parameters.GetUtfString("id"), out serverVoteNight.m_nightModeInt))
			{
				GameLog.Print(AlertLevel.Error, "Vote", "ServerVoteNight: target parse failed " + utfString);
				return null;
			}
			string utfString2 = parameters.GetUtfString("cm");
			if (string.IsNullOrEmpty(utfString2))
			{
				GameLog.Print(AlertLevel.Error, "Vote", "ServerVoteMap: target parse failed " + utfString2);
				return null;
			}
			serverVoteNight.m_lightPresetName = utfString2;
			serverVoteNight.m_isNightMode = ((serverVoteNight.m_nightModeInt != 0) ? true : false);
			if (game.lastRoomOptions.RoomNightMode == serverVoteNight.m_isNightMode)
			{
				serverVoteNight.m_isNightMode = !serverVoteNight.m_isNightMode;
			}
			return serverVoteNight;
		}

		public void OnVoteFinished(VoteResult result)
		{
			if (result != VoteResult.yes)
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.voteresult_night_no.Localized());
			}
			if (result == VoteResult.yes && m_game.LocalPlayer.IsMasterClient)
			{
				SmartfoxRoomOptions smartfoxRoomOptions = m_game.CurrentRoomOptions.Clone();
				smartfoxRoomOptions.RoomNightMode = m_isNightMode;
				smartfoxRoomOptions.RoomLightPreset = m_lightPresetName;
				m_game.SendChangedRoomOptions(smartfoxRoomOptions);
			}
		}

		public string GetTitle()
		{
			return LocalizationKeys.game_wnd_votenight.Localized();
		}

		public string GetTarget()
		{
			if (!string.IsNullOrEmpty(m_lightPresetName))
			{
				return ScriptableSingleton<TrackLightVariantsBase>.instance.GetVisibleNameForLightPreset(m_lightPresetName);
			}
			if (!string.IsNullOrEmpty(m_nightKeyString))
			{
				return m_nightKeyString;
			}
			m_nightKeyString = LocalizationKeys.game_wnd_votenight.KeyValue;
			return m_nightKeyString;
		}

		public string GetAdditionalTarget()
		{
			return string.Empty;
		}
	}
}
