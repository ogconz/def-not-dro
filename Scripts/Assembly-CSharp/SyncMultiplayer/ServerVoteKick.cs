using System.Collections.Generic;
using GameOverlay;
using Sfs2X.Entities.Data;

namespace SyncMultiplayer
{
	public class ServerVoteKick : IServerVotePlayer, IServerVote
	{
		private NetworkGame m_game;

		private PlayerId m_banId;

		private string m_bannedPlayerUID;

		public VoteType voteType => VoteType.Kick;

		public PlayerId targetId => m_banId;

		public static void PrepareVoteData(SFSObject data, Dictionary<string, string> parameters)
		{
			data.PutUtfString("vt", "KICK");
			data.PutUtfString("vr", parameters["voteReason"]);
			data.PutUtfString("c", parameters["id"]);
			data.PutUtfString("n", parameters["name"]);
			data.PutUtfString("h", parameters["h"]);
		}

		public static ServerVoteKick VoteStarted(NetworkGame game, ISFSObject parameters)
		{
			ServerVoteKick serverVoteKick = new ServerVoteKick();
			serverVoteKick.m_game = game;
			if (!parameters.ContainsKey("c"))
			{
				GameLog.Print(AlertLevel.Error, "Vote", "ServerVoteKick: target is not specified");
				return null;
			}
			string utfString = parameters.GetUtfString("c");
			if (!PlayerId.TryParse(utfString, out serverVoteKick.m_banId))
			{
				GameLog.Print(AlertLevel.Error, "Vote", "ServerVoteKick: target parse failed " + utfString);
				return null;
			}
			return serverVoteKick;
		}

		public void OnVoteFinished(VoteResult result)
		{
			NetworkPlayer networkPlayer = m_game.FindPlayer(m_banId);
			string text = ((networkPlayer != null) ? networkPlayer.FilteredNickNameWithPlatformIcon : string.Empty);
			if (result == VoteResult.yes)
			{
				if (m_game.LocalPlayer.PlayerId != m_banId)
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.voteresult_kick_yes.LocalizedFormat(text));
				}
			}
			else
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.voteresult_ban_no.LocalizedFormat(text));
			}
			if (result == VoteResult.yes && m_game.LocalPlayer.PlayerId == m_banId)
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.voteresult_kick_self.Localized());
				if ((bool)(States.mainCurrent as BaseRaceModeState))
				{
					GarageGUIState.ExitToGarage();
				}
			}
		}

		public string GetTitle()
		{
			return LocalizationKeys.game_wnd_votekick.Localized();
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
