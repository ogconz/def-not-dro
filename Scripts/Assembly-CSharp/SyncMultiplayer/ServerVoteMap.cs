using System.Collections.Generic;
using DB;
using DB.Meta;
using DI;
using Sfs2X.Entities.Data;

namespace SyncMultiplayer
{
	public class ServerVoteMap : IServerVote
	{
		private NetworkGame m_game;

		private int m_trackID;

		private ulong m_externalTrackId;

		private byte m_ugcServiceId;

		private string m_trackName;

		private string m_lightPresetName = string.Empty;

		public VoteType voteType => VoteType.Map;

		public static void PrepareVoteData(SFSObject data, Dictionary<string, string> parameters)
		{
			data.PutUtfString("vt", "GENERAL");
			data.PutUtfString("id", parameters["id"]);
			data.PutUtfString("h", parameters["h"]);
			data.PutUtfString("cm", parameters["additional"]);
			data.PutUtfString("ex", parameters["externalId"]);
			data.PutUtfString("usid", parameters["ugcServiceId"]);
			data.PutUtfString("nm", NetworkController.InstanceGame.CurrentRoomOptions.RoomNightMode.ToString());
			data.PutUtfString("lm", parameters["additional"]);
		}

		public static ServerVoteMap VoteStarted(NetworkGame game, ISFSObject parameters)
		{
			ServerVoteMap serverVoteMap = new ServerVoteMap();
			serverVoteMap.m_game = game;
			if (!parameters.ContainsKey("id"))
			{
				GameLog.Print(AlertLevel.Error, "Vote", "ServerVoteMap: target is not specified");
				return null;
			}
			string utfString = parameters.GetUtfString("id");
			if (!int.TryParse(parameters.GetUtfString("id"), out serverVoteMap.m_trackID))
			{
				GameLog.Print(AlertLevel.Error, "Vote", "ServerVoteMap: target parse failed " + utfString);
				return null;
			}
			string utfString2 = parameters.GetUtfString("cm");
			if (string.IsNullOrEmpty(utfString2))
			{
				GameLog.Print(AlertLevel.Error, "Vote", "ServerVoteMap: target parse failed " + utfString2);
				return null;
			}
			serverVoteMap.m_lightPresetName = utfString2;
			string utfString3 = parameters.GetUtfString("ex");
			if (string.IsNullOrEmpty(utfString3))
			{
				GameLog.Print(AlertLevel.Error, "Vote", "ServerVoteMap: target parse failed " + utfString3);
				return null;
			}
			serverVoteMap.m_externalTrackId = ulong.Parse(utfString3);
			string utfString4 = parameters.GetUtfString("usid");
			if (string.IsNullOrEmpty(utfString3))
			{
				GameLog.Print(AlertLevel.Error, "Vote", "ServerVoteMap: target parse failed " + utfString4);
				return null;
			}
			serverVoteMap.m_ugcServiceId = byte.Parse(utfString4);
			return serverVoteMap;
		}

		public void OnVoteFinished(VoteResult result)
		{
			if (result != VoteResult.yes)
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.voteresult_map_no.Localized());
			}
			if (result == VoteResult.yes && m_game.LocalPlayer.IsMasterClient)
			{
				SmartfoxRoomOptions smartfoxRoomOptions = m_game.CurrentRoomOptions.Clone();
				smartfoxRoomOptions.TrackID = m_trackID;
				smartfoxRoomOptions.ExternalTrackId = m_externalTrackId;
				smartfoxRoomOptions.ugcServiceId = m_ugcServiceId;
				smartfoxRoomOptions.RoomLightPreset = m_lightPresetName;
				smartfoxRoomOptions.TrackName = GetTarget();
				smartfoxRoomOptions.SpawnPointIndex = 0;
				m_game.SendChangedRoomOptions(smartfoxRoomOptions);
			}
		}

		public string GetTitle()
		{
			return LocalizationKeys.game_wnd_votemap.Localized();
		}

		public string GetTarget()
		{
			if (!string.IsNullOrEmpty(m_trackName))
			{
				return m_trackName;
			}
			if (m_trackID == 0)
			{
				return "";
			}
			DB.Meta.Track byId = DependencyInjector.Resolve<BaseModel>().GetById<DB.Meta.Track>(m_trackID);
			if (byId != null)
			{
				m_trackName = byId.name;
			}
			return m_trackName;
		}

		public string GetAdditionalTarget()
		{
			return ScriptableSingleton<TrackLightVariantsBase>.instance.GetVisibleNameForLightPreset(m_lightPresetName);
		}
	}
}
