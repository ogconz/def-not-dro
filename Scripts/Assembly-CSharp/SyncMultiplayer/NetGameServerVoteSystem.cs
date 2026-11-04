using System;
using System.Collections;
using System.Collections.Generic;
using DB.Meta;
using PrefsModels;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;
using Sfs2X.Requests;
using SimpleJSON;
using UnityEngine;

namespace SyncMultiplayer
{
	public class NetGameServerVoteSystem : BaseNetGameSystem
	{
		private struct TokenParamData
		{
			public string token;

			public string authority;
		}

		public const float DEFAULT_VOTE_TIME = 60f;

		public const string START_VOTE_CMD = "sv";

		public const string VOTE_FINISHED_CMD = "vf";

		public const string VOTE_SERVER_TYPE = "vt";

		public const string VOTE_REASON = "vr";

		public const string VOTE_CLIENT_TYPE = "vc";

		public const string VOTE_RESULT = "vr";

		private const string STEAM_AUTHORITY = "STEAM";

		public string voteText = "";

		public float voteStarted;

		public IServerVote currentVote;

		public string reasonVote = string.Empty;

		public bool forceUpdateNightMode;

		public bool forceUpdateLightPreset;

		private bool m_needWaitToLoadRace;

		private bool m_placeLock;

		private float m_checkLockTime;

		private string m_serializedTokenParamData;

		private string m_cachedToken;

		private NetworkGame m_game;

		private NetGameNotifySystem m_notify;

		public event Action onVoteFinished;

		public event Action<VoteType, string> onVoteStart;

		public override void Attach(NetGameSystemsManager owner)
		{
			base.Attach(owner);
			m_game = owner.host;
			m_game.SubscribeOnServerCommand("sv", OnVoteStart);
			m_game.SubscribeOnServerCommand("vf", OnVoteFinished);
			m_notify = owner.Get<NetGameNotifySystem>();
			m_notify.Subscribe("v", DoPlayerLeave);
		}

		public override void OnGameConnect()
		{
			currentVote = null;
			reasonVote = string.Empty;
			OnReconnect();
			ClearVoteState();
		}

		public override void OnReconnect()
		{
			if (m_game.LocalPlayer.IsMasterClient && m_placeLock)
			{
				m_placeLock = false;
				SmartfoxRoomOptions smartfoxRoomOptions = m_game.CurrentRoomOptions.Clone();
				smartfoxRoomOptions.VoteMapLocked = true;
				m_game.SendChangedRoomOptions(smartfoxRoomOptions);
				m_checkLockTime = Time.unscaledTime + 10f;
			}
			if (currentVote != null)
			{
				ValidateCurrentVote();
			}
		}

		public override void DoPlayerLeave(NetworkPlayer player)
		{
			UpdateVoteText();
		}

		public override void ChangeMasterState(bool isMaster)
		{
			if (isMaster && m_checkLockTime == 0f && m_game.CurrentRoomOptions.VoteMapLocked)
			{
				m_checkLockTime = Time.unscaledTime + 2f;
			}
		}

		private void CancelVoteLock()
		{
			if (m_game.CurrentRoomOptions.VoteMapLocked)
			{
				SmartfoxRoomOptions smartfoxRoomOptions = m_game.CurrentRoomOptions.Clone();
				smartfoxRoomOptions.VoteMapLocked = false;
				m_game.SendChangedRoomOptions(smartfoxRoomOptions);
			}
		}

		public override void Update()
		{
			if (m_game.CurrentRoom != null)
			{
				if (m_game.LocalPlayer.IsMasterClient && m_checkLockTime != 0f && Time.unscaledTime > m_checkLockTime)
				{
					m_checkLockTime = 0f;
					CancelVoteLock();
				}
				if (m_needWaitToLoadRace && !NetworkController.CheckIsLoadingState())
				{
					NetworkController.ForceContinuePlayIfNeed();
					m_game.SetRaceMode(RaceMode.NetworkFreeride);
					m_game.LoadRace();
					m_needWaitToLoadRace = false;
				}
			}
		}

		private void ValidateCurrentVote()
		{
			if (Time.unscaledTime - voteStarted > 60f)
			{
				DestroyCurrentVote();
			}
		}

		public void StartVote(VoteType voteType, VoteReason voteReason, Dictionary<string, string> parameters)
		{
			if (m_game.Client == null)
			{
				Debug.Log("Vote disabled. Currently in offline.");
				return;
			}
			byte b = (byte)voteType;
			parameters["voteType"] = b.ToString();
			parameters["voteReason"] = voteReason.ToString();
			parameters["h"] = m_game.LocalPlayer.PlayerId.accountId.ToString();
			InitSFSData(out var data, parameters);
			switch (voteType)
			{
			case VoteType.Ban:
				ServerVoteBan.PrepareVoteData(data, parameters);
				break;
			case VoteType.Kick:
				ServerVoteKick.PrepareVoteData(data, parameters);
				break;
			case VoteType.Map:
				ServerVoteMap.PrepareVoteData(data, parameters);
				break;
			case VoteType.Night:
				ServerVoteNight.PrepareVoteData(data, parameters);
				break;
			case VoteType.Light:
				ServerVoteLightPreset.PrepareVoteData(data, parameters);
				break;
			}
			m_game.SendServerCommand("sv", data);
		}

		private void InitSFSData(out SFSObject data, Dictionary<string, string> parameters)
		{
			data = new SFSObject();
			data.PutUtfString("vc", parameters["voteType"]);
			data.PutUtfString("h", parameters["h"]);
			data.PutUtfString("vtk", PrepareTokenParamData());
		}

		public void OnVoteStart(ISFSObject parameters)
		{
			voteStarted = Time.unscaledTime;
			if (parameters == null)
			{
				GameLog.Print(AlertLevel.Error, "Vote", "OnVoteStart: void params");
				return;
			}
			if (!parameters.ContainsKey("vc"))
			{
				GameLog.Print(AlertLevel.Error, "Vote", "OnVoteStart: unknown vote type");
				return;
			}
			string s = string.Empty;
			if (parameters.ContainsKey("vc"))
			{
				s = parameters.GetUtfString("vc");
			}
			if (parameters.ContainsKey("vr"))
			{
				reasonVote = parameters.GetUtfString("vr");
			}
			if (!byte.TryParse(s, out var result))
			{
				GameLog.Print(AlertLevel.Error, "Vote", "OnVoteStart: error vote type");
				return;
			}
			string text = (parameters.ContainsKey("h") ? parameters.GetUtfString("h") : string.Empty);
			VoteType voteType = (VoteType)result;
			try
			{
				switch (voteType)
				{
				case VoteType.Ban:
					currentVote = ServerVoteBan.VoteStarted(m_game, parameters);
					break;
				case VoteType.Map:
					currentVote = ServerVoteMap.VoteStarted(m_game, parameters);
					break;
				case VoteType.Kick:
					currentVote = ServerVoteKick.VoteStarted(m_game, parameters);
					break;
				case VoteType.Night:
					currentVote = ServerVoteNight.VoteStarted(m_game, parameters);
					break;
				case VoteType.Light:
					currentVote = ServerVoteLightPreset.VoteStarted(m_game, parameters);
					break;
				}
				NetworkPlayer networkPlayer = m_game.FindPlayerByUID(text);
				if (networkPlayer != null)
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.vote_started.LocalizedFormat(networkPlayer.FilteredNickNameWithPlatformIcon));
					if (m_game.LocalPlayer == networkPlayer)
					{
						SetVote(value: true);
					}
				}
			}
			finally
			{
				ClearVoteState();
			}
			this.onVoteStart.Call(voteType, text);
		}

		public void OnVoteFinished(ISFSObject parameters)
		{
			try
			{
				string utfString = parameters.GetUtfString("vr");
				VoteResult result = VoteResult.no;
				if (!(utfString == "yes"))
				{
					if (utfString == "expired")
					{
						GUICommonGodVoice.ShowText(LocalizationKeys.voteresult_expired.Localized());
						return;
					}
				}
				else
				{
					result = VoteResult.yes;
				}
				if (currentVote != null)
				{
					currentVote.OnVoteFinished(result);
				}
			}
			finally
			{
				DestroyCurrentVote();
			}
		}

		private void DestroyCurrentVote()
		{
			this.onVoteFinished.Call();
			currentVote = null;
			reasonVote = string.Empty;
			ClearVoteState();
		}

		private string PrepareTokenParamData()
		{
			if (GameSync.token.Equals(m_cachedToken))
			{
				return m_serializedTokenParamData;
			}
			TokenParamData tokenParamData = new TokenParamData
			{
				token = GameSync.token,
				authority = "STEAM"
			};
			m_cachedToken = GameSync.token;
			m_serializedTokenParamData = JSON.Serialize(tokenParamData);
			return m_serializedTokenParamData;
		}

		public void SetVote(bool value)
		{
			SFSUserVariable sFSUserVariable = new SFSUserVariable("v", value);
			sFSUserVariable.IsPrivate = false;
			SFSUserVariable item = new SFSUserVariable("vtk", PrepareTokenParamData());
			m_game.SendRAW(new SetUserVariablesRequest(new List<UserVariable> { sFSUserVariable, item }));
		}

		public void ClearVoteState()
		{
			voteText = "(0 : 0)";
			foreach (NetworkPlayer player in m_game.Players)
			{
				player.PlayerProperties.Vote = null;
			}
		}

		public void OnRoomPropsChanged(SmartfoxRoomOptions props, HashSet<string> changed)
		{
			if (props != null)
			{
				if (changed.Contains("nm") || forceUpdateNightMode)
				{
					UpdateNightMode(props.RoomNightMode);
				}
				if (changed.Contains("lm") || forceUpdateLightPreset)
				{
					UpdateLightPreset(props, changed);
				}
				if (changed.Contains("2"))
				{
					UpdateCurrentMap();
				}
				if (changed.Contains("c"))
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.rejoin_to_room.Localized(), 2f);
					TimeController.StartNow(UpdateRoomCaption());
				}
				bool flag = changed.Contains("o");
				bool flag2 = changed.Contains("bu");
				if (!changed.Contains("spawns") && !flag2 && !flag && currentVote != null)
				{
					DestroyCurrentVote();
				}
			}
		}

		private void UpdateNightMode(bool isNight)
		{
			BaseRaceState.NightModeActive = isNight;
			m_game.lastRoomOptions.RoomNightMode = isNight;
			if (forceUpdateNightMode && States.current == States.State<LoadRaceState>())
			{
				forceUpdateNightMode = false;
				return;
			}
			if ((bool)DayNightController.instance)
			{
				if (isNight)
				{
					DayNightController.instance.SetDayNightMode(isDay: false);
				}
				else
				{
					DayNightController.instance.SetDayNightMode(isDay: true);
				}
				NetworkGame instanceGame = NetworkController.InstanceGame;
				if (instanceGame != null)
				{
					foreach (NetworkPlayer player in instanceGame.Players)
					{
						if (player != null && !(player.userCar == null) && !player.IsLocal)
						{
							player.userCar.SetHeadlightsEnabled(isNight, ignoreIgnition: false, force: true);
						}
					}
				}
			}
			forceUpdateNightMode = false;
		}

		private void UpdateLightPreset(SmartfoxRoomOptions props, HashSet<string> changed)
		{
			if (States.mainCurrent is LoadRaceState || props.RoomLightPreset == "null")
			{
				return;
			}
			forceUpdateLightPreset = false;
			GlobalModel globals = NetworkGame.prefs.globals;
			TrackLightVariantsBase instance = ScriptableSingleton<TrackLightVariantsBase>.instance;
			DB.Meta.Track byId = m_game.baseModel.GetById<DB.Meta.Track>(props.TrackID);
			if (byId == null)
			{
				return;
			}
			string sceneName = instance.GetSceneName(byId.scene, props.RoomNightMode);
			if (!changed.Contains("2") && !changed.Contains("nm"))
			{
				if (props.RoomLightPreset != globals.lightPresetName)
				{
					globals.sceneName = sceneName;
					LightVariantController.instance.StartChangeLightCoroutine(props.RoomLightPreset);
				}
			}
			else if (changed.Contains("2"))
			{
				globals.sceneName = sceneName;
				globals.lightPresetName = props.RoomLightPreset;
			}
			LightVariantController.instance.skipLightChange = true;
		}

		private IEnumerator UpdateRoomCaption()
		{
			yield return new WaitForSeconds(1f);
			BaseRaceState baseRaceState = States.mainCurrent as BaseRaceState;
			if (!(baseRaceState == null))
			{
				NetworkPlayer localPlayer = m_game.LocalPlayer;
				m_placeLock = !m_game.CurrentRoom.Name.StartsWith("@");
				NetworkController.IsMapChanging = true;
				if ((bool)baseRaceState)
				{
					baseRaceState.ForceFinishRace();
					localPlayer.points.Clear(allData: true);
				}
				if (NetworkController.CheckIsLoadingState())
				{
					m_needWaitToLoadRace = true;
					yield break;
				}
				m_game.SetRaceMode(RaceMode.NetworkFreeride);
				m_game.LoadRace(isRoomNameChanged: true);
			}
		}

		private void UpdateCurrentMap()
		{
			BaseRaceState baseRaceState = States.mainCurrent as BaseRaceState;
			NetworkPlayer localPlayer = m_game.LocalPlayer;
			int num = (GameConsole.Bool["sv_crazyNetworkTracks"] ? 999 : m_game.CurrentRoomOptions.TrackID);
			if (!NetworkGame.IsTrackIDAllowed(num))
			{
				if ((bool)baseRaceState)
				{
					baseRaceState.ForceFinishRace();
					localPlayer.points.Clear(allData: true);
				}
				NetworkController.TryEnterToMultiplayer(baseRaceState ? baseRaceState.contextOwner : null);
				return;
			}
			m_game.lastRoomOptions.TrackID = num;
			int spawnPointIndex = m_game.CurrentRoomOptions.SpawnPointIndex;
			m_game.lastRoomOptions.SpawnPointIndex = spawnPointIndex;
			NetworkGame.prefs.globals.lastRoomSpawnPoint = spawnPointIndex;
			m_placeLock = m_game.CurrentRoom?.Name.StartsWith("@") ?? m_game.lastRoomOptions.Name.ToString().StartsWith("@");
			NetworkController.IsMapChanging = true;
			if ((bool)baseRaceState)
			{
				if (States.current != null && States.current.isPopupState)
				{
					States.Pop();
				}
				baseRaceState.ForceFinishRace();
				localPlayer.points.Clear(allData: true);
			}
			if (NetworkController.CheckIsLoadingState())
			{
				m_needWaitToLoadRace = true;
				return;
			}
			m_game.SetRaceMode(RaceMode.NetworkFreeride);
			m_game.LoadRace();
		}

		public void GetVoteStats(out int pos, out int neg, out int empty, out bool? self)
		{
			pos = 0;
			neg = 0;
			empty = 0;
			foreach (NetworkPlayer player in m_game.Players)
			{
				bool? vote = player.PlayerProperties.Vote;
				if (vote.HasValue)
				{
					if (vote != true)
					{
						neg++;
					}
					else
					{
						pos++;
					}
				}
				else
				{
					empty++;
				}
			}
			self = m_game.LocalPlayer.PlayerProperties.Vote;
		}

		private void UpdateVoteText()
		{
			GetVoteStats(out var pos, out var neg, out var _, out var self);
			string text = ((self.HasValue && self.Value) ? ("[00FF00]" + pos + "[-]") : pos.ToString());
			string text2 = ((self.HasValue && !self.Value) ? ("[FF0000]" + neg + "[-]") : neg.ToString());
			voteText = "(" + text + " : " + text2 + ")";
		}
	}
}
