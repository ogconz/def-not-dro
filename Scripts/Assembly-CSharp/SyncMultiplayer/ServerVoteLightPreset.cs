using System.Collections.Generic;
using Sfs2X.Entities.Data;

namespace SyncMultiplayer
{
	public class ServerVoteLightPreset : IServerVote
	{
		private NetworkGame m_game;

		private string m_lightPresetName = string.Empty;

		public VoteType voteType => VoteType.Light;

		public static void PrepareVoteData(SFSObject data, Dictionary<string, string> parameters)
		{
			data.PutUtfString("vt", "LIGHT");
			data.PutUtfString("h", parameters["h"]);
			data.PutUtfString("cm", parameters["name"]);
		}

		public static ServerVoteLightPreset VoteStarted(NetworkGame game, ISFSObject parameters)
		{
			ServerVoteLightPreset serverVoteLightPreset = new ServerVoteLightPreset();
			serverVoteLightPreset.m_game = game;
			string utfString = parameters.GetUtfString("cm");
			if (string.IsNullOrEmpty(utfString))
			{
				return null;
			}
			serverVoteLightPreset.m_lightPresetName = utfString;
			return serverVoteLightPreset;
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
				smartfoxRoomOptions.RoomLightPreset = m_lightPresetName;
				m_game.SendChangedRoomOptions(smartfoxRoomOptions);
			}
		}

		public string GetTitle()
		{
			return LocalizationKeys.game_wnd_votelight.Localized();
		}

		public string GetTarget()
		{
			if (!string.IsNullOrEmpty(m_lightPresetName))
			{
				return ScriptableSingleton<TrackLightVariantsBase>.instance.GetVisibleNameForLightPreset(m_lightPresetName);
			}
			m_lightPresetName = LocalizationKeys.game_wnd_votelight.KeyValue;
			return m_lightPresetName;
		}

		public string GetAdditionalTarget()
		{
			return string.Empty;
		}
	}
}
