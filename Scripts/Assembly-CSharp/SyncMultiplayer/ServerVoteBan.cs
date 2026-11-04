using System.Collections.Generic;
using GameOverlay;
using Sfs2X.Entities.Data;

namespace SyncMultiplayer
{
	public class ServerVoteBan : IServerVotePlayer, IServerVote
	{
		private NetworkGame m_game;

		private PlayerId m_banId;

		private string m_bannedPlayerUID;

		public VoteType voteType => VoteType.Ban;

		public PlayerId targetId => m_banId;

		public static void PrepareVoteData(SFSObject data, Dictionary<string, string> parameters)
		{
			data.PutUtfString("vt", "BAN");
			data.PutUtfString("vc", parameters["voteType"]);
			data.PutUtfString("vr", parameters["voteReason"]);
			data.PutUtfString("c", parameters["id"]);
			data.PutUtfString("n", parameters["name"]);
		}

		public static ServerVoteBan VoteStarted(NetworkGame game, ISFSObject parameters)
		{
			ServerVoteBan serverVoteBan = new ServerVoteBan
			{
				m_game = game
			};
			if (!parameters.ContainsKey("c"))
			{
				GameLog.Print(AlertLevel.Error, "Vote", "ServerVoteBan: target is not specified");
				return null;
			}
			string utfString = parameters.GetUtfString("c");
			if (!PlayerId.TryParse(utfString, out serverVoteBan.m_banId))
			{
				GameLog.Print(AlertLevel.Error, "Vote", "ServerVoteBan: target parse failed " + utfString);
				return null;
			}
			return serverVoteBan;
		}

		public void OnVoteFinished(VoteResult result)
		{
			NetworkPlayer networkPlayer = m_game.FindPlayer(m_banId);
			string text = ((networkPlayer != null) ? networkPlayer.FilteredNickNameWithPlatformIcon : string.Empty);
			if (result == VoteResult.yes)
			{
				if (m_game.LocalPlayer.PlayerId != m_banId)
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.voteresult_ban_yes.LocalizedFormat(text));
				}
			}
			else
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.voteresult_ban_no.LocalizedFormat(text));
			}
			if (result == VoteResult.yes && m_game.LocalPlayer.PlayerId == m_banId)
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.voteresult_ban_self.Localized());
				if ((bool)(States.mainCurrent as BaseRaceModeState))
				{
					GarageGUIState.ExitToGarage();
				}
			}
		}

		public string GetTitle()
		{
			return LocalizationKeys.game_wnd_voteban.Localized();
		}

		public string GetTarget()
		{
			if (!string.IsNullOrEmpty(m_bannedPlayerUID))
			{
				return m_bannedPlayerUID;
			}
			if (m_banId.IsInvalid)
			{
				return "";
			}
			NetworkPlayer networkPlayer = m_game.FindPlayer(m_banId);
			if (networkPlayer != null)
			{
				m_bannedPlayerUID = networkPlayer.PlayerId.accountId.ToString();
			}
			return m_bannedPlayerUID;
		}

		public string GetAdditionalTarget()
		{
			return string.Empty;
		}
	}
}
