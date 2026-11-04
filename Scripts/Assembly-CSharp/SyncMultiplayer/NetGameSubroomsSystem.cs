using System;
using System.Collections.Generic;
using GameOverlay;
using JukeBox;
using Sfs2X.Entities.Data;
using Sfs2X.Util;
using SimpleJSON;
using SyncMultiplayer.NetGameSystems.Collisions;
using TrackLayout.Behaviours;
using UnityEngine;

namespace SyncMultiplayer
{
	public class NetGameSubroomsSystem : BaseNetGameSystem
	{
		public const float REQUEST_DELAY = 10f;

		private NetworkGame m_game;

		private NetGameSyncTimeSystem m_timeSync;

		private NetGameSyncPosSystem m_posSync;

		private NetGameSyncPointsSystem m_points;

		private NetGameIgnoreSystem m_ignoreSystem;

		private DataSyncManager m_dataSync;

		private HashSet<int> m_notInformedPlayers_ChangeRoomID = new HashSet<int>();

		private HashSet<int> m_notInformedPlayers_RoomSpawn = new HashSet<int>();

		private HashSet<int> m_notInformedPlayers_RaceResult = new HashSet<int>();

		private Dictionary<string, SubroomInfo> m_roomInfos = new Dictionary<string, SubroomInfo>();

		private Dictionary<string, string> m_roomPointsInfos = new Dictionary<string, string>();

		private float m_lastActiveRequest;

		private SubroomInfo m_myLastResultRoom;

		private SubroomInfoRaceResult m_myLastResult = new SubroomInfoRaceResult();

		private float m_lastResultClearTime;

		private bool m_isLocalRoomCreated;

		private NetGameCollisionManager m_collisions;

		private SubroomInfo m_prevRoom;

		private SubroomInfo m_myRoom;

		private string m_lastRequestedSubroomIdToJoin;

		private float m_lastDiscardSingleRaceCallTime;

		private bool m_isUpdateRoomCtrls;

		private DelayedPacketManager m_delayedPackets = new DelayedPacketManager();

		private string m_lastRequestedID = string.Empty;

		private SubroomController m_myRoomCtrl;

		private Dictionary<string, SubroomController> m_roomCtrls = new Dictionary<string, SubroomController>();

		private List<string> m_destroyRoomCtrls = new List<string>();

		public IReadOnlyCollection<SubroomInfo> roomInfos => m_roomInfos.Values;

		public SubroomInfoRaceResult myLastResult => m_myLastResult;

		public SubroomInfo myRoom => m_myRoom;

		public bool iPlayedInRoom => m_myRoom != null;

		public bool isChampionshipMode => GameConsole.Bool["ChampionshipMode"];

		public event Action<SubroomInfo, SubroomOperation> onSubroomOperation;

		public override void Attach(NetGameSystemsManager owner)
		{
			base.Attach(owner);
			m_game = owner.host;
			m_timeSync = owner.Get<NetGameSyncTimeSystem>();
			m_points = owner.Get<NetGameSyncPointsSystem>();
			m_collisions = owner.Get<NetGameCollisionManager>();
			m_posSync = owner.Get<NetGameSyncPosSystem>();
			m_ignoreSystem = owner.Get<NetGameIgnoreSystem>();
			m_dataSync = owner.Get<DataSyncManager>();
			m_game.packetHandler.Subscribe(PacketId.Subroom, RECV_SubroomPacket);
			m_game.SubscribeOnServerCommand("ku", HandlePointUpdate);
			m_game.SubscribeOnServerCommand("kr", HandlePointRemove);
		}

		public override void Update()
		{
			UpdateNotInformedPlayers_ChangeRoomID();
			if (myRoom != m_prevRoom)
			{
				m_prevRoom = myRoom;
				SEND_ChangeRoomID(null, myRoom);
				ActivatePlayersExceptSelf(myRoom == null || ForceActivateCarIfNeed());
			}
			UpdateRoomCtrls();
			UpdateNotInformedPlayers_RaceResult();
			if (m_timeSync.initialSyncDone && m_delayedPackets.count > 0)
			{
				m_delayedPackets.Handle(DispatchSubroomPacket);
			}
		}

		private bool ForceActivateCarIfNeed()
		{
			return false;
		}

		public bool IsPlayerInMyRoom(NetworkPlayer player)
		{
			if (myRoom != null && player != null)
			{
				return player.attachedRoom == myRoom;
			}
			return false;
		}

		public bool IsFinishedPlayerInMyRoom(NetworkPlayer player)
		{
			if (IsPlayerInMyRoom(player))
			{
				return myRoom.ContainsResult(player);
			}
			return false;
		}

		public bool MustHidePlayer(NetworkPlayer player)
		{
			if (ForceActivateCarIfNeed())
			{
				return false;
			}
			if (myRoom == null)
			{
				return false;
			}
			if (!IsPlayerInMyRoom(player))
			{
				return true;
			}
			if (!myRoom.subroomFlags.HasFlag(SubroomFlags.XDSCollide) && myRoom.subroomMode.IsXDS() && myRoom.tandemLeadId == m_game.LocalPlayer.PlayerId)
			{
				return myRoom.state != SubroomState.WaitingSecondRace;
			}
			return false;
		}

		public void ResendAllSpawnRoomOperations()
		{
			foreach (SubroomInfo roomInfo in roomInfos)
			{
				if ((int)roomInfo.state <= 0)
				{
					InvokeSubroomOperation(roomInfo, SubroomOperation.Spawn);
				}
			}
		}

		public override void OnDisconnect()
		{
			if ((bool)(States.mainCurrent as SyncNetFreerideRaceModeState))
			{
				UIXDSInfoContext.instance.DiscardRace();
			}
			if (m_roomPointsInfos.Count > 0)
			{
				m_roomPointsInfos.Clear();
			}
			m_lastRequestedID = "";
			m_delayedPackets.GameLost();
			m_prevRoom = null;
			if (m_isLocalRoomCreated)
			{
				ReleaseLocalRoom();
			}
			NetworkPlayer localPlayer = m_game.LocalPlayer;
			RemovePlayerFromRooms(localPlayer);
			if (myRoom != null)
			{
				myRoom.state = SubroomState.Finished;
				InternalDestroyRoom(myRoom);
			}
			ClearRooms();
		}

		public override void OnReconnect()
		{
			ClearRooms();
			m_notInformedPlayers_RoomSpawn.Clear();
			bool flag = true;
			if (myRoom != null)
			{
				bool flag2 = myRoom.state != SubroomState.Race;
				if (!flag2)
				{
					if (myRoom.tandemState == TandemState.None)
					{
						if (Time.unscaledTime < myRoom.launchTime)
						{
							flag = false;
						}
						m_roomInfos.Add(myRoom.id, myRoom);
						m_roomCtrls.Add(myRoom.id, m_myRoomCtrl);
					}
					else
					{
						flag2 = true;
					}
				}
				if (flag2)
				{
					InvokeSubroomOperation(myRoom, SubroomOperation.Destroy);
					ReleaseMyRoom();
				}
			}
			if (States.mainCurrent is SyncNetFreerideRaceModeState syncNetFreerideRaceModeState)
			{
				syncNetFreerideRaceModeState.SetupTimeLimit(0);
			}
			NetworkPlayer localPlayer = m_game.LocalPlayer;
			if (m_game.CurrentRoom == null || !m_game.CurrentRoomOptions.NewRoomSettings.HasFlag(RoomData.RoomSettingFlags.CHAMPIONSHIP_MODE))
			{
				localPlayer.FlushMapToDefault();
			}
			RaceCar userCar = localPlayer.userCar;
			if ((bool)userCar)
			{
				userCar.useTC = false;
				if (userCar.lockState)
				{
					userCar.lockState = !flag;
				}
			}
		}

		public override void OnGameConnect()
		{
			m_roomPointsInfos.Clear();
			m_lastRequestedID = "";
			ReleaseMyRoom();
			ClearRooms();
			m_notInformedPlayers_RaceResult.Clear();
			m_lastActiveRequest = 0f;
			RaceCar userCar = m_game.LocalPlayer.userCar;
			if ((bool)userCar)
			{
				userCar.useTC = false;
				userCar.lockState = false;
			}
		}

		public void ShowMap(NetworkPlayer player)
		{
			if (m_roomPointsInfos.TryGetValue(player.collideGroup, out var value))
			{
				player.ShowMap(value);
				return;
			}
			player.ShowMap();
			m_lastRequestedID = ((player.attachedRoom == null) ? player.collideGroup : "");
		}

		public void SetCooldownForSendRequests(float time)
		{
			m_lastActiveRequest = Time.unscaledTime - 10f + time;
		}

		public void RestartSingleRace()
		{
			if (Time.unscaledTime < m_lastDiscardSingleRaceCallTime + 1f)
			{
				return;
			}
			m_lastDiscardSingleRaceCallTime = Time.unscaledTime;
			if (myRoom != null && myRoom.settingsPlayersCount == 1)
			{
				if ((bool)UIXDSInfoContext.instance)
				{
					UIXDSInfoContext.instance.DiscardRace();
				}
				SubroomSettings settings = myRoom.settings;
				m_lastRequestedID = "";
				m_prevRoom = null;
				NetworkPlayer localPlayer = m_game.LocalPlayer;
				RemovePlayerFromRooms(localPlayer);
				myRoom.state = SubroomState.Finished;
				InternalDestroyRoom(myRoom);
				SpawnRoom(settings);
			}
		}

		public void ForceRaceEnd()
		{
			m_myRoomCtrl?.ForceRaceEnd();
		}

		public void LocalFinished(SubroomInfo room, SubroomInfoRaceResult result)
		{
			m_points.UpdatePoints();
			m_lastResultClearTime = room.launchTime + room.raceTime + 5f;
			m_myLastResultRoom = room;
			m_myLastResult = result.Clone();
			InvokeSubroomOperation(room, SubroomOperation.LocalFinished);
			SEND_OtherFinished(m_game.LocalPlayer, room.id);
			m_notInformedPlayers_RaceResult.Clear();
			foreach (NetworkPlayer player in m_game.Players)
			{
				if (!player.IsLocal)
				{
					m_notInformedPlayers_RaceResult.Add(player.NetworkID);
					GameLog.Print(AlertLevel.Notify, "Multiplayer", "RaceEnd> player " + player.FilteredNickName + " in send list");
				}
			}
			if (m_game.CurrentRoom == null && m_game.CurrentRoomOptions != null && m_game.CurrentRoomOptions.NewRoomSettings.HasFlag(RoomData.RoomSettingFlags.CHAMPIONSHIP_MODE))
			{
				Singleton<TrackLayoutController>.instance.ResetPlacementOnAdditionalScene(forceDelayToReset: true);
			}
			else
			{
				m_game.LocalPlayer.FlushMapToDefault();
			}
		}

		private void UpdateRoomCtrls()
		{
			m_isUpdateRoomCtrls = true;
			foreach (SubroomController value in m_roomCtrls.Values)
			{
				value.Update();
			}
			m_isUpdateRoomCtrls = false;
			if (m_destroyRoomCtrls.Count <= 0)
			{
				return;
			}
			foreach (string destroyRoomCtrl in m_destroyRoomCtrls)
			{
				m_roomCtrls.Remove(destroyRoomCtrl);
			}
			m_destroyRoomCtrls.Clear();
		}

		public void DisqualifyYourself(DisqualifyReason reason)
		{
			m_myRoomCtrl?.DisqualifyYourself(reason);
			if (reason == DisqualifyReason.BadConnection)
			{
				DestroyRoomByDisqualify();
			}
		}

		private void DestroyRoomByDisqualify()
		{
			if (myRoom != null)
			{
				InvokeSubroomOperation(myRoom, SubroomOperation.Destroy);
				m_game.LocalPlayer.attachedRoom = null;
				m_myRoom = null;
				m_myRoomCtrl.iPlayInThisRoom = false;
				m_myRoomCtrl = null;
			}
		}

		private void UpdateNotInformedPlayers_ChangeRoomID()
		{
			if (m_notInformedPlayers_ChangeRoomID.Count <= 0)
			{
				return;
			}
			NetworkPlayer networkPlayer = null;
			foreach (int item in m_notInformedPlayers_ChangeRoomID)
			{
				networkPlayer = m_game.FindPlayerByNetworkId(item);
				if (networkPlayer != null)
				{
					break;
				}
			}
			if (networkPlayer != null)
			{
				m_notInformedPlayers_ChangeRoomID.Remove(networkPlayer.NetworkID);
				SEND_ChangeRoomID(networkPlayer, myRoom);
			}
		}

		private void UpdateNotInformedPlayers_RaceResult()
		{
			if (m_notInformedPlayers_RaceResult.Count <= 0)
			{
				return;
			}
			NetworkPlayer networkPlayer = null;
			foreach (int item in m_notInformedPlayers_RaceResult)
			{
				networkPlayer = m_game.FindPlayerByNetworkId(item);
				if (networkPlayer != null)
				{
					break;
				}
			}
			if (networkPlayer != null)
			{
				m_notInformedPlayers_RaceResult.Remove(networkPlayer.NetworkID);
				SEND_SubroomResult(networkPlayer);
				if (Time.unscaledTime > m_lastResultClearTime)
				{
					m_notInformedPlayers_RaceResult.Clear();
				}
			}
		}

		public void UpdateNotInformedPlayers_RoomSpawn()
		{
			if (m_notInformedPlayers_RoomSpawn.Count <= 0)
			{
				return;
			}
			int num = 0;
			using (HashSet<int>.Enumerator enumerator = m_notInformedPlayers_RoomSpawn.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					num = enumerator.Current;
				}
			}
			NetworkPlayer networkPlayer = m_game.FindPlayerByNetworkId(num);
			if (networkPlayer != null)
			{
				m_notInformedPlayers_RoomSpawn.Remove(num);
				SEND_SubroomSpawn(myRoom, networkPlayer.NetworkID);
			}
		}

		public void SpawnRoom(SubroomSettings settings)
		{
			if (myRoom != null)
			{
				GameLog.Print(AlertLevel.Warning, "Multiplayer", "SpawnRoom> already have attached room!");
				return;
			}
			if (settings.subroomMode == SubroomMode.ShowTrack)
			{
				m_game.LocalPlayer.ShowMap(settings.pointName);
				return;
			}
			AnalyticsService.MultiplayerRoomSpawned(settings.subroomMode.ToString(), settings.pointName);
			SubroomInfo subroomInfo = new SubroomInfo(settings)
			{
				version = GetLocalRoomModeVersion(settings.subroomMode)
			};
			AddRoom(subroomInfo);
			InitMyRoom(subroomInfo);
			if (settings.playersCount > 1)
			{
				InformPlayers();
			}
			InvokeSubroomOperation(subroomInfo, SubroomOperation.Spawn);
			CameraSwitch.instance.SetCar(PlayerCarControl.instance.car);
		}

		public void SpawnLocalRoom()
		{
			if (IsLocalRoomExist())
			{
				m_game.LocalPlayer.attachedRoom = myRoom;
				return;
			}
			SubroomInfo subroomInfo = new SubroomInfo(new SubroomSettings
			{
				subroomMode = SubroomMode.Challenge,
				playersCount = 2,
				pointName = string.Empty,
				xdsFlags = SubroomFlags.None
			});
			m_myRoom = subroomInfo;
			m_game.LocalPlayer.attachedRoom = myRoom;
			m_roomInfos.Add(myRoom.id, myRoom);
			m_isLocalRoomCreated = true;
		}

		public void ReleaseLocalRoom()
		{
			if (IsLocalRoomExist())
			{
				m_roomInfos.Remove(myRoom.id);
			}
			m_myRoom = null;
			m_game.LocalPlayer.attachedRoom = null;
			m_isLocalRoomCreated = false;
		}

		private bool IsLocalRoomExist()
		{
			if (m_isLocalRoomCreated)
			{
				return ContainsRoom(myRoom);
			}
			return false;
		}

		private void InformPlayers()
		{
			foreach (NetworkPlayer player in m_game.Players)
			{
				if (!player.IsLocal)
				{
					m_notInformedPlayers_RoomSpawn.Add(player.NetworkID);
				}
			}
		}

		public void JoinRoom(SubroomInfo info)
		{
			if (Time.unscaledTime < m_lastActiveRequest + 10f)
			{
				GameLog.Print(AlertLevel.Warning, "Multiplayer", "JoinRoom> wait!");
			}
			else if (myRoom != null)
			{
				GameLog.Print(AlertLevel.Warning, "Multiplayer", "JoinRoom> already have room!");
			}
			else if ((int)info.state >= 2)
			{
				GameLog.Print(AlertLevel.Warning, "Multiplayer", "JoinRoom> room started!");
			}
			else
			{
				SEND_RequestPlayerJoin(info);
			}
		}

		public void SelfPlayerLeaveRoom(SubroomInfo info)
		{
			if (myRoom == info)
			{
				SEND_PlayerLeave(m_game.LocalPlayer, info.id);
				RemovePlayerFromRooms(m_game.LocalPlayer);
				ReleaseMyRoom();
			}
		}

		public void QuitMyRoom()
		{
			m_myRoomCtrl?.DisqualifyYourself(DisqualifyReason.PlayerOut);
			DestroyRoomByDisqualify();
			SoundController.PlayMusic(MusicSignal.MPIdle);
		}

		public void DestroyRoom(SubroomInfo info)
		{
			SEND_RoomDestroy(info.id);
			InternalDestroyRoom(info);
		}

		private void ActivatePlayersExceptSelf(bool active)
		{
			NetworkPlayer localPlayer = m_game.LocalPlayer;
			foreach (NetworkPlayer player in m_game.Players)
			{
				if (player != localPlayer && player.userCar != null && !IsPlayerInMyRoom(player))
				{
					m_posSync.SetPlayerActive(player, active);
				}
			}
		}

		public void InternalDestroyRoom(SubroomInfo info)
		{
			if (info == null || ((int)info.state >= 2 && info.state != SubroomState.Finished && info.state != SubroomState.WaitingSecondRace))
			{
				return;
			}
			if (info == myRoom)
			{
				if (States.mainCurrent is SyncNetFreerideRaceModeState)
				{
					m_points.UpdatePoints();
					UIXDSInfoContext.instance.DiscardRace();
				}
				SoundController.PlayMusic(MusicSignal.MPIdle);
				m_lastActiveRequest = 0f;
				RaceCar userCar = m_game.LocalPlayer.userCar;
				if ((bool)userCar)
				{
					userCar.lockState = false;
					userCar.useTC = false;
				}
				ReleaseMyRoom();
			}
			RemoveRoom(info);
			InvokeSubroomOperation(info, SubroomOperation.Destroy);
		}

		public override void DoPlayerEnter(NetworkPlayer player)
		{
			m_notInformedPlayers_RoomSpawn.Add(player.NetworkID);
			m_notInformedPlayers_ChangeRoomID.Add(player.NetworkID);
			if (myRoom != null && (int)myRoom.state >= 3)
			{
				m_notInformedPlayers_RaceResult.Add(player.NetworkID);
			}
		}

		public override void DoPlayerLeave(NetworkPlayer player)
		{
			m_delayedPackets.PlayerLost(player);
			SubroomInfo attachedRoom = player.attachedRoom;
			if (attachedRoom != null && !attachedRoom.ContainsResult(player))
			{
				SubroomInfoRaceResult result = new SubroomInfoRaceResult(0, DisqualifyReason.PlayerOut);
				if (TryGetRoomCtrl(attachedRoom.id, out var ctrl))
				{
					ctrl.ReceiveResult(player, result);
				}
				else
				{
					attachedRoom.AddResult(player, result);
				}
			}
			m_notInformedPlayers_RoomSpawn.Remove(player.NetworkID);
			m_notInformedPlayers_RaceResult.Remove(player.NetworkID);
			RemovePlayerFromRooms(player);
			if ((bool)CameraSwitch.instance && CameraSwitch.instance.GetCar() == player.userCar)
			{
				RaceCar car = PlayerCarControl.instance.car;
				CameraSwitch.instance.SetCar(car);
				if (!BaseCar.IsNull(car))
				{
					car.SetWindowsVisability((bool)CameraSwitch.instance && !CameraSwitch.instance.CheckForOutsideView(car.carX));
				}
			}
		}

		private void MovePlayerToRoom(NetworkPlayer player, SubroomInfo room)
		{
			if (room != null)
			{
				RemovePlayerFromRooms(player);
				room.AddPlayer(player);
				if (m_game.LocalPlayer.NetworkID == player.NetworkID)
				{
					InvokeSubroomOperation(room, SubroomOperation.Join);
					InitMyRoom(room);
				}
			}
		}

		private void RemovePlayerFromRooms(NetworkPlayer player)
		{
			player.attachedRoom = null;
			foreach (SubroomInfo roomInfo in roomInfos)
			{
				if (!roomInfo.ContainsPlayer(player))
				{
					continue;
				}
				if ((int)roomInfo.state <= 1)
				{
					roomInfo.RemovePlayer(player);
				}
				if (roomInfo.creatorId == player.PlayerId && roomInfo.GetNextCreatorPlayer(out roomInfo.creatorId, player.PlayerId))
				{
					roomInfo.AddPlayerIsReady(roomInfo.creatorId);
					if (m_game.LocalPlayer.PlayerId == roomInfo.creatorId && myRoom == roomInfo)
					{
						m_notInformedPlayers_RoomSpawn.Clear();
					}
				}
				if (roomInfo.playersCount == 0)
				{
					InternalDestroyRoom(roomInfo);
				}
				break;
			}
		}

		private void SEND_OtherFinished(NetworkPlayer sender, string subroomId)
		{
			SmartfoxDataPackage data = new SmartfoxDataPackage(PacketId.Subroom);
			data.Add("1", (byte)15);
			data.Add("2", subroomId);
			data.Add("3", sender.PlayerId.ToString());
			m_game.SendOthers(data, reliable: true);
		}

		private void RECV_OtherFinished(NetworkPlayer sender, SmartfoxDataPackage data)
		{
			string text = data.Data.GetText("2");
			PlayerId playerId = PlayerId.Parse(data.Data.GetUtfString("3"));
			SubroomInfo room = GetRoom(text);
			if (room != null)
			{
				NetworkPlayer networkPlayer = m_game.FindPlayer(playerId);
				if (networkPlayer != null && !networkPlayer.IsLocal)
				{
					InvokeSubroomOperation(room, SubroomOperation.OtherFinished);
				}
			}
		}

		private void SEND_RequestPlayerJoin(SubroomInfo info)
		{
			NetworkPlayer networkPlayer = m_game.FindPlayer(info.creatorId);
			if (networkPlayer == null)
			{
				GameLog.Print(AlertLevel.Warning, "Multiplayer", "SEND_RequestPlayerJoin> creator lost");
				return;
			}
			m_lastRequestedSubroomIdToJoin = info.id;
			SmartfoxDataPackage data = new SmartfoxDataPackage(PacketId.Subroom);
			data.Add("1", (byte)5);
			data.Add("2", info.id);
			m_game.SendOthers(data, reliable: true, new int[1] { networkPlayer.NetworkID });
			m_lastActiveRequest = Time.unscaledTime;
		}

		private void RECV_RequestPlayerJoin(NetworkPlayer sender, SmartfoxDataPackage data)
		{
			NetworkPlayer localPlayer = m_game.LocalPlayer;
			string text = data.Data.GetText("2");
			if (myRoom == null || myRoom.creatorId != localPlayer.PlayerId || (int)myRoom.state >= 1 || myRoom.id != text || myRoom.playersCount >= myRoom.settingsPlayersCount)
			{
				SEND_PlayerJoinNegative(sender);
				return;
			}
			MovePlayerToRoom(sender, myRoom);
			SEND_PlayerJoin(sender, text);
		}

		private void SEND_ConfirmReadyToStart(NetworkPlayer player, string subroomId)
		{
			SmartfoxDataPackage data = new SmartfoxDataPackage(PacketId.Subroom);
			data.Add("1", (byte)14);
			data.Add("2", subroomId);
			data.Add("3", player.PlayerId.ToString());
			m_game.SendOthers(data, reliable: true);
		}

		private void RECV_ConfirmReadyToStart(NetworkPlayer sender, SmartfoxDataPackage data)
		{
			string text = data.Data.GetText("2");
			PlayerId playerId = PlayerId.Parse(data.Data.GetUtfString("3"));
			SubroomInfo room = GetRoom(text);
			if (room == null)
			{
				GameLog.Print(AlertLevel.Error, "Multiplayer", "RECV_ConfirmReadyToStart> Unknown room!");
			}
			else if (m_game.FindPlayer(playerId) == null)
			{
				GameLog.Print(AlertLevel.Error, "Multiplayer", "RECV_ConfirmReadyToStart> Unknown player!");
			}
			else
			{
				room.AddPlayerIsReady(playerId);
			}
		}

		private void SEND_PlayerJoin(NetworkPlayer player, string subroomId)
		{
			SmartfoxDataPackage data = new SmartfoxDataPackage(PacketId.Subroom);
			data.Add("1", (byte)2);
			data.Add("2", subroomId);
			data.Add("3", player.PlayerId.ToString());
			m_game.SendOthers(data, reliable: true);
		}

		private void RECV_PlayerJoin(NetworkPlayer sender, SmartfoxDataPackage data)
		{
			string text = data.Data.GetText("2");
			PlayerId playerId = PlayerId.Parse(data.Data.GetUtfString("3"));
			if (!TryGetRoom(text, out var info))
			{
				GameLog.Print(AlertLevel.Error, "Multiplayer", "RECV_PlayerJoin> Unknown room!");
				return;
			}
			NetworkPlayer networkPlayer = m_game.FindPlayer(playerId);
			if (networkPlayer == null)
			{
				GameLog.Print(AlertLevel.Error, "Multiplayer", "RECV_PlayerJoin> Unknown player!");
				return;
			}
			bool flag = networkPlayer.NetworkID == m_game.LocalPlayer.NetworkID;
			if (flag)
			{
				if (m_lastRequestedSubroomIdToJoin != text)
				{
					GameLog.Print(AlertLevel.Error, "Multiplayer", $"RECV_PlayerJoin> {sender.PlayerId} - {sender.OriginaNickName} forced invite you to room!");
					return;
				}
				m_lastRequestedSubroomIdToJoin = string.Empty;
				m_lastActiveRequest = 0f;
				CameraSwitch.instance.SetCar(PlayerCarControl.instance.car);
			}
			MovePlayerToRoom(networkPlayer, info);
			if (flag)
			{
				SEND_ConfirmReadyToStart(networkPlayer, info.id);
			}
		}

		private void SEND_PlayerJoinNegative(NetworkPlayer player)
		{
			SmartfoxDataPackage data = new SmartfoxDataPackage(PacketId.Subroom);
			data.Add("1", (byte)6);
			m_game.SendOthers(data, reliable: true, new int[1] { player.NetworkID });
		}

		private void RECV_PlayerJoinNegative(NetworkPlayer sender, SmartfoxDataPackage data)
		{
			m_lastActiveRequest = 0f;
		}

		private void SEND_PlayerLeave(NetworkPlayer player, string subroomId)
		{
			SmartfoxDataPackage data = new SmartfoxDataPackage(PacketId.Subroom);
			data.Add("1", (byte)3);
			data.Add("2", subroomId);
			if (player.IsLocal)
			{
				m_lastRequestedSubroomIdToJoin = string.Empty;
			}
			m_game.SendOthers(data, reliable: true);
		}

		private void RECV_PlayerLeave(NetworkPlayer sender, SmartfoxDataPackage data)
		{
			RemovePlayerFromRooms(sender);
		}

		private void SEND_SubroomSpawn(SubroomInfo info, int target = -1)
		{
			if (info == null)
			{
				GameLog.Print(AlertLevel.Warning, "Multiplayer", "SEND_SubroomSpawn: null subroom");
				return;
			}
			GameLog.Print(AlertLevel.Notify, "Multiplayer", "SEND_SubroomSpawn: target=" + target);
			SmartfoxDataPackage data = new SmartfoxDataPackage(PacketId.Subroom);
			data.Add("1", (byte)1);
			data.Add("2", CustomTypes.SerializeSubroom(m_timeSync, info));
			if (target == -1)
			{
				m_game.SendOthers(data, reliable: true);
				return;
			}
			m_game.SendOthers(data, reliable: true, new int[1] { target });
		}

		private void RECV_SubroomSpawn(NetworkPlayer sender, SmartfoxDataPackage data)
		{
			SubroomInfo subroomInfo = CustomTypes.DeserializeSubroom(m_timeSync, data.Data.GetByteArray("2").Bytes);
			if (subroomInfo == null)
			{
				return;
			}
			if (sender.PlayerId != subroomInfo.creatorId)
			{
				GameLog.Print(AlertLevel.Error, "Multiplayer", "Subroom spawn from non-creator, ignored");
			}
			else
			{
				if (!CheckRoomVersion(subroomInfo) || m_ignoreSystem.IsPlayerIgnoringMe(sender))
				{
					return;
				}
				if (ContainsRoom(subroomInfo.id))
				{
					RemoveRoom(subroomInfo.id);
					AddRoom(subroomInfo);
					if (myRoom?.id == subroomInfo.id)
					{
						InitMyRoom(subroomInfo);
					}
					InvokeSubroomOperation(subroomInfo, SubroomOperation.Destroy);
				}
				else
				{
					AddRoom(subroomInfo);
				}
				foreach (PlayerId playersId in subroomInfo.playersIds)
				{
					NetworkPlayer networkPlayer = m_game.FindPlayer(playersId);
					if (networkPlayer != null && !networkPlayer.IsLocal)
					{
						networkPlayer.attachedRoom = subroomInfo;
					}
				}
				subroomInfo.UpdateRuntimePlayersNickName();
				InvokeSubroomOperation(subroomInfo, SubroomOperation.Spawn);
			}
		}

		private void SEND_RoomDestroy(string subroomId)
		{
			SmartfoxDataPackage data = new SmartfoxDataPackage(PacketId.Subroom);
			data.Add("1", (byte)4);
			data.Add("2", subroomId);
			if (m_lastRequestedSubroomIdToJoin == subroomId)
			{
				m_lastRequestedSubroomIdToJoin = string.Empty;
			}
			m_game.SendOthers(data, reliable: true);
		}

		private void RECV_RoomDestroy(NetworkPlayer sender, SmartfoxDataPackage data)
		{
			string text = data.Data.GetText("2");
			if (m_lastRequestedSubroomIdToJoin == text)
			{
				m_lastRequestedSubroomIdToJoin = string.Empty;
			}
			if (TryGetRoom(text, out var info))
			{
				GameLog.Print(AlertLevel.Notify, "Multiplayer", "Room destroyed by network command");
				InternalDestroyRoom(info);
			}
		}

		public void SEND_RoomStartRace(SubroomInfo room)
		{
			SmartfoxDataPackage data = new SmartfoxDataPackage(PacketId.Subroom);
			data.Add("1", (byte)7);
			data.Add("2", room.id);
			data.Add("3", m_timeSync.ConvertToModel(room.launchTime));
			data.Add("4", CustomTypes.SerializePlayerIdList(room.positions));
			m_game.SendIncludeSelf(data, reliable: true);
			SFSObject sFSObject = new SFSObject();
			sFSObject.PutUtfString("p", room.pointName);
			m_dataSync.AddData(room.id, 60, sFSObject);
		}

		private void RECV_RoomStartRace(NetworkPlayer sender, SmartfoxDataPackage data)
		{
			string text = data.Data.GetText("2");
			SubroomInfo room = GetRoom(text);
			if (room != null)
			{
				SubroomController ctrl;
				if (!m_timeSync.initialSyncDone)
				{
					InternalDestroyRoom(room);
					GameLog.Print(AlertLevel.Error, "Multiplayer", "RECV_RoomStartRace> have no time sync. Room destroyed. Race canceled!");
				}
				else if (TryGetRoomCtrl(text, out ctrl))
				{
					float startTime = m_timeSync.ConvertToLocal(data.Data.GetFloat("3"));
					List<PlayerId> positions = CustomTypes.DeserializePlayerIdList(data.Data.GetByteArray("4").Bytes);
					ctrl.StartRace(startTime, positions);
				}
			}
		}

		private void SEND_SubroomResult(NetworkPlayer receiver)
		{
			if (m_myLastResultRoom == null || m_myLastResult == null)
			{
				return;
			}
			SmartfoxDataPackage data = new SmartfoxDataPackage(PacketId.Subroom);
			data.Add("1", (byte)8);
			data.Add("2", m_myLastResultRoom.id);
			Dictionary<string, string> detailedResult = m_myLastResult.detailedResult;
			if (!m_myLastResultRoom.ContainsPlayer(receiver))
			{
				m_myLastResult.detailedResult = null;
				try
				{
					data.Add("3", JSON.Serialize(m_myLastResult));
				}
				finally
				{
					m_myLastResult.detailedResult = detailedResult;
				}
			}
			else
			{
				data.Add("3", JSON.Serialize(m_myLastResult));
			}
			m_game.SendOthers(data, reliable: true, new int[1] { receiver.NetworkID });
			GameLog.Print(AlertLevel.Notify, "Multiplayer", "Send race result to " + receiver.FilteredNickName + " " + m_myLastResult.asString + " " + m_myLastResult.disqualifyReason);
		}

		private void RECV_SubroomResult(NetworkPlayer sender, SmartfoxDataPackage data)
		{
			string text = data.Data.GetText("2");
			SubroomInfoRaceResult subroomInfoRaceResult = JSON.Parse<SubroomInfoRaceResult>(data.Data.GetText("3"));
			if (TryGetRoom(text, out var info))
			{
				SubroomController ctrl;
				if (!info.ContainsPlayer(sender))
				{
					GameLog.Print(AlertLevel.Error, "Multiplayer", $"RECV_SubroomResult> {sender.PlayerId} - {sender.OriginaNickName} trying to send subroom result from outside");
				}
				else if (TryGetRoomCtrl(text, out ctrl))
				{
					ctrl.ReceiveResult(sender, subroomInfoRaceResult);
					GameLog.Print(AlertLevel.Notify, "Multiplayer", $"Received race result from {sender.FilteredNickName} {subroomInfoRaceResult.asString} {subroomInfoRaceResult.disqualifyReason}");
				}
			}
		}

		private void SEND_ChangeRoomID(NetworkPlayer receiver, SubroomInfo info)
		{
			SmartfoxDataPackage data = new SmartfoxDataPackage(PacketId.Subroom);
			data.Add("1", (byte)13);
			string value = info?.id;
			data.Add("2", string.IsNullOrEmpty(value) ? new byte[0] : CustomTypes.SerializeSubroom(m_timeSync, info));
			if (receiver == null)
			{
				m_notInformedPlayers_ChangeRoomID.Clear();
				m_game.SendIncludeSelf(data, reliable: true);
			}
			else
			{
				m_notInformedPlayers_ChangeRoomID.Remove(receiver.NetworkID);
				m_game.SendOthers(data, reliable: true, new int[1] { receiver.NetworkID });
			}
		}

		private void RECV_ChangeRoomID(NetworkPlayer sender, SmartfoxDataPackage data)
		{
			ByteArray byteArray = data.Data.GetByteArray("2");
			SubroomInfo subroomInfo = ((byteArray == null || byteArray.Length == 0) ? null : CustomTypes.DeserializeSubroom(m_timeSync, byteArray.Bytes));
			string text = subroomInfo?.id ?? string.Empty;
			m_collisions.MovePlayerToColliderGroup(text, sender);
			if (TryGetRoom(text, out var info))
			{
				sender.attachedRoom = info;
			}
			else
			{
				if (subroomInfo != null && !ContainsRoom(subroomInfo))
				{
					AddRoom(subroomInfo);
				}
				sender.attachedRoom = subroomInfo;
			}
			CameraUpdateNetworkTrack(subroomInfo, sender);
		}

		public void CameraUpdateNetworkTrack(SubroomInfo info, NetworkPlayer sender)
		{
			if ((info == null || (int)info.state >= 1) && CameraSwitch.instance != null && CameraSwitch.instance.GetCar() == sender.userCar)
			{
				CameraSwitch.instance.UpdateNetworkTrack();
			}
		}

		private void RECV_SubroomPacket(NetworkPlayer sender, SmartfoxDataPackage data)
		{
			if (!m_timeSync.initialSyncDone)
			{
				m_delayedPackets.RegisterPacket(sender, data);
				return;
			}
			if (m_delayedPackets.count > 0)
			{
				m_delayedPackets.Handle(DispatchSubroomPacket);
			}
			DispatchSubroomPacket(sender, data);
		}

		private void DispatchSubroomPacket(NetworkPlayer sender, SmartfoxDataPackage data)
		{
			SubroomOperation subroomOperation = (SubroomOperation)data.Data.GetByte("1");
			switch (subroomOperation)
			{
			case SubroomOperation.Spawn:
				RECV_SubroomSpawn(sender, data);
				break;
			case SubroomOperation.Destroy:
				RECV_RoomDestroy(sender, data);
				break;
			case SubroomOperation.Join:
				RECV_PlayerJoin(sender, data);
				break;
			case SubroomOperation.Leave:
				RECV_PlayerLeave(sender, data);
				break;
			case SubroomOperation.RequestJoin:
				RECV_RequestPlayerJoin(sender, data);
				break;
			case SubroomOperation.RequestJoinNegative:
				RECV_PlayerJoinNegative(sender, data);
				break;
			case SubroomOperation.StartRace:
				RECV_RoomStartRace(sender, data);
				break;
			case SubroomOperation.RaceResult:
				RECV_SubroomResult(sender, data);
				break;
			case SubroomOperation.ChangeRoomID:
				RECV_ChangeRoomID(sender, data);
				break;
			case SubroomOperation.ConfirmReadyToStart:
				RECV_ConfirmReadyToStart(sender, data);
				break;
			case SubroomOperation.OtherFinished:
				RECV_OtherFinished(sender, data);
				break;
			default:
				GameLog.Print(AlertLevel.Warning, "Multiplayer", $"RECV_SubroomPacket> Unknown packet[{subroomOperation}] from: {sender?.FilteredNickName}|{sender?.PlayerId.platform}");
				break;
			}
		}

		private void HandlePointUpdate(ISFSObject data)
		{
			string text = data.GetText("ki");
			if (myRoom != null && myRoom.id == text)
			{
				m_dataSync.Subscribe(text);
			}
			string text2 = data.GetSFSObject("kd").GetText("p");
			m_roomPointsInfos[text] = text2;
			if (m_lastRequestedID == text)
			{
				m_lastRequestedID = string.Empty;
				if (myRoom == null)
				{
					m_game.LocalPlayer.ShowMap(text2);
				}
			}
		}

		private void HandlePointRemove(ISFSObject data)
		{
			string text = data.GetText("ki");
			m_roomPointsInfos.Remove(text);
		}

		public void InvokeSubroomOperation(SubroomInfo info, SubroomOperation operation)
		{
			this.onSubroomOperation.SafeCall(info, operation);
		}

		private SubroomController CreateController(SubroomInfo info)
		{
			return info.subroomMode switch
			{
				SubroomMode.Challenge => new ChallengeSubroomController(info, this, m_game), 
				SubroomMode.TimeAttack => new TimeAttackSubroomController(info, this, m_game), 
				SubroomMode.ClippingDuel => new ClippingDuelSubroomController(info, this, m_game), 
				SubroomMode.XDS => new XDSSubroomController(info, this, m_game), 
				SubroomMode.Tandem => new TandemSubroomController(info, this, m_game), 
				SubroomMode.TandemXDS => new TandemXDSSubroomController(info, this, m_game), 
				_ => null, 
			};
		}

		private void InitMyRoom(SubroomInfo info)
		{
			m_myRoom = info;
			TryGetRoomCtrl(info.id, out m_myRoomCtrl);
			m_myRoomCtrl.iPlayInThisRoom = true;
		}

		private void ReleaseMyRoom()
		{
			m_myRoom = null;
			if (m_myRoomCtrl != null)
			{
				m_myRoomCtrl.iPlayInThisRoom = false;
				m_myRoomCtrl.Release(releaseCar: true);
				m_myRoomCtrl = null;
			}
			m_notInformedPlayers_RoomSpawn.Clear();
		}

		private void ClearRooms()
		{
			m_roomInfos.Clear();
			m_roomCtrls.Clear();
		}

		public bool ContainsRoom(SubroomInfo roomInfo)
		{
			if (roomInfo == null)
			{
				return false;
			}
			return ContainsRoom(roomInfo.id);
		}

		public bool ContainsRoom(string roomInfoId)
		{
			return m_roomInfos.ContainsKey(roomInfoId);
		}

		private bool TryGetRoom(string roomInfoId, out SubroomInfo info)
		{
			return m_roomInfos.TryGetValue(roomInfoId, out info);
		}

		public bool TryGetRoomCtrl(string roomInfoId, out SubroomController ctrl)
		{
			return m_roomCtrls.TryGetValue(roomInfoId, out ctrl);
		}

		private SubroomInfo GetRoom(string roomInfoId)
		{
			TryGetRoom(roomInfoId, out var info);
			return info;
		}

		public SubroomController GetRoomCtrl(string roomInfoId)
		{
			TryGetRoomCtrl(roomInfoId, out var ctrl);
			return ctrl;
		}

		private void AddRoom(SubroomInfo roomInfo)
		{
			m_roomInfos.Add(roomInfo.id, roomInfo);
			m_roomCtrls.Add(roomInfo.id, CreateController(roomInfo));
		}

		private void RemoveRoom(SubroomInfo roomInfo)
		{
			RemoveRoom(roomInfo.id);
		}

		private void RemoveRoom(string roomInfoId)
		{
			m_roomInfos.Remove(roomInfoId);
			if (m_roomCtrls.TryGetValue(roomInfoId, out var value))
			{
				value.Release(value.iPlayInThisRoom);
				if (m_isUpdateRoomCtrls)
				{
					m_destroyRoomCtrls.Add(roomInfoId);
				}
				else
				{
					m_roomCtrls.Remove(roomInfoId);
				}
			}
		}

		private bool CheckRoomVersion(SubroomInfo remoteRoomInfo)
		{
			string localRoomModeVersion = GetLocalRoomModeVersion(remoteRoomInfo.subroomMode);
			string version = remoteRoomInfo.version;
			_ = remoteRoomInfo.subroomMode;
			return localRoomModeVersion == version;
		}

		private string GetLocalRoomModeVersion(SubroomMode subroomMode)
		{
			return "1";
		}
	}
}
