using System;
using System.Collections.Generic;
using GameOverlay;
using SimpleJSON;
using UnityEngine;

namespace SyncMultiplayer
{
	public class SubroomInfo
	{
		public const float ROOM_LIVE_TIME = 60f;

		public const float ROOM_RACE_IGNITION_TIME = 5f;

		public const float ROOM_TANDEM_IGNITION_TIME = 7f;

		public const float ROOM_WAIT_RESULT_TIME = 5f;

		public string id;

		public PlayerId creatorId;

		public float createTime;

		public float launchTime;

		public SubroomState state;

		public SubroomSettings settings;

		public DisqualifyReason disqualifyReason;

		public TandemState tandemState;

		public string version = "1";

		public Dictionary<PlayerId, string> players = new Dictionary<PlayerId, string>();

		public HashSet<PlayerId> readyToStartPlayers = new HashSet<PlayerId>();

		[JsonIgnore]
		public float raceTime = 60f;

		[JsonIgnore]
		public Dictionary<PlayerId, SubroomInfoRaceResult> roomResults = new Dictionary<PlayerId, SubroomInfoRaceResult>();

		[JsonIgnore]
		public List<ValidatedRoomResult> validatedResults = new List<ValidatedRoomResult>();

		[JsonIgnore]
		public int validatedTimedOut;

		[JsonIgnore]
		public HashSet<NetworkPlayer> runtimePlayers = new HashSet<NetworkPlayer>();

		[JsonIgnore]
		public List<PlayerId> positions = new List<PlayerId>();

		[JsonIgnore]
		public PlayerId tandemLeadId;

		[JsonIgnore]
		public IReadOnlyCollection<PlayerId> playersIds => players.Keys;

		[JsonIgnore]
		public IReadOnlyCollection<string> playersNames => players.Values;

		[JsonIgnore]
		public int playersCount => players.Count;

		[JsonIgnore]
		public int resultsCount => roomResults.Count;

		[JsonIgnore]
		public SubroomMode subroomMode => settings.subroomMode;

		[JsonIgnore]
		public SubroomFlags subroomFlags => settings.xdsFlags;

		[JsonIgnore]
		public string pointName => settings.pointName;

		[JsonIgnore]
		public int settingsPlayersCount => settings.playersCount;

		public bool HasAllPlayersResult()
		{
			return resultsCount == playersCount;
		}

		public bool IsFullRoom()
		{
			return playersCount == settingsPlayersCount;
		}

		public bool IsAllPlayersReady()
		{
			return readyToStartPlayers.Count == playersCount;
		}

		public SubroomInfo()
		{
			createTime = Time.unscaledTime;
			settings = new SubroomSettings();
		}

		public SubroomInfo(SubroomSettings settings)
		{
			NetworkPlayer localPlayer = NetworkController.InstanceGame.LocalPlayer;
			createTime = Time.unscaledTime;
			this.settings = settings;
			id = Guid.NewGuid().ToString();
			creatorId = localPlayer.PlayerId;
			AddPlayer(localPlayer);
			AddPlayerIsReady(creatorId);
		}

		public NetworkPlayer GetRuntimePlayer(PlayerId playerId)
		{
			foreach (NetworkPlayer runtimePlayer in runtimePlayers)
			{
				if (runtimePlayer.PlayerId == playerId)
				{
					return runtimePlayer;
				}
			}
			return null;
		}

		public void AddPlayer(NetworkPlayer player)
		{
			player.attachedRoom = this;
			UpdatePlayerNickName(player);
		}

		public void AddPlayerIsReady(PlayerId playerId)
		{
			readyToStartPlayers.Add(playerId);
		}

		public void UpdateRuntimePlayersNickName()
		{
			foreach (NetworkPlayer runtimePlayer in runtimePlayers)
			{
				UpdatePlayerNickName(runtimePlayer);
			}
		}

		public void UpdatePlayerNickName(NetworkPlayer player)
		{
			players[player.PlayerId] = player.FilteredNickNameWithPlatformIcon;
		}

		public void RemovePlayer(NetworkPlayer player)
		{
			if (player.attachedRoom == this)
			{
				player.attachedRoom = null;
			}
			PlayerId playerId = player.PlayerId;
			players.Remove(playerId);
			readyToStartPlayers.Remove(playerId);
		}

		public bool AllPlayersLoaded()
		{
			foreach (NetworkPlayer runtimePlayer in runtimePlayers)
			{
				if (runtimePlayer.userCar == null || !runtimePlayer.userCar.IsVisualLoaded)
				{
					return false;
				}
			}
			return true;
		}

		public bool GetNextCreatorPlayer(out PlayerId result, PlayerId ignored)
		{
			result = PlayerId.Invalid;
			foreach (PlayerId playersId in playersIds)
			{
				if (playersId != ignored && playersId.CompareTo(result) != 0)
				{
					result = playersId;
				}
			}
			return result != PlayerId.Invalid;
		}

		public bool ContainsPlayer(NetworkPlayer player)
		{
			if (player == null)
			{
				return false;
			}
			return players.ContainsKey(player.PlayerId);
		}

		public void AddResult(NetworkPlayer player, SubroomInfoRaceResult result)
		{
			AddResult(player.PlayerId, result);
		}

		public void AddResult(PlayerId playerId, SubroomInfoRaceResult result)
		{
			roomResults[playerId] = result;
		}

		public bool ContainsResult(NetworkPlayer player)
		{
			return ContainsResult(player.PlayerId);
		}

		public bool ContainsResult(PlayerId playerId)
		{
			return roomResults.ContainsKey(playerId);
		}

		public bool TryGetResult(PlayerId playerId, out SubroomInfoRaceResult result)
		{
			return roomResults.TryGetValue(playerId, out result);
		}

		public void ClearResults()
		{
			roomResults.Clear();
		}

		public void ResultsForEach(Action<PlayerId, SubroomInfoRaceResult> action)
		{
			foreach (KeyValuePair<PlayerId, SubroomInfoRaceResult> roomResult in roomResults)
			{
				action(roomResult.Key, roomResult.Value);
			}
		}

		public void PlayersForEach(Action<PlayerId, string> action)
		{
			foreach (KeyValuePair<PlayerId, string> player in players)
			{
				action(player.Key, player.Value);
			}
		}

		public string GetFilteredNickName(PlayerId playerId)
		{
			string value = string.Empty;
			if (players.TryGetValue(playerId, out value) && string.IsNullOrEmpty(value))
			{
				NetworkPlayer runtimePlayer = GetRuntimePlayer(playerId);
				if (runtimePlayer != null)
				{
					value = runtimePlayer.FilteredNickNameWithPlatformIcon;
					players[runtimePlayer.PlayerId] = value;
				}
			}
			return value;
		}

		public float GetLiveTimeBound()
		{
			return 60f;
		}

		public float GetLiveTime()
		{
			float num = 0f;
			if (launchTime > 0f)
			{
				num = launchTime - Time.unscaledTime;
				if (num < 0f)
				{
					num = launchTime + raceTime - Time.unscaledTime;
				}
			}
			else
			{
				num = createTime + 60f - Time.unscaledTime;
			}
			if (!(num > 0f))
			{
				return 0f;
			}
			return num;
		}

		public List<ValidatedRoomResult> GetAllPlayersValidatedResult()
		{
			List<PlayerId> list = new List<PlayerId>(playersIds);
			list.Sort((PlayerId x, PlayerId y) => x.accountId.CompareTo(y.accountId));
			List<ValidatedRoomResult> list2 = new List<ValidatedRoomResult>(validatedResults);
			foreach (PlayerId playerId in list)
			{
				if (list2.Find((ValidatedRoomResult x) => x.playerId == playerId) == null)
				{
					list2.Add(new ValidatedRoomResult(playerId, new SubroomInfoRaceResult(0, DisqualifyReason.TimedOut)));
				}
			}
			return list2;
		}
	}
}
