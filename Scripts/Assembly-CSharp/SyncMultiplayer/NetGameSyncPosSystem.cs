using System;
using System.Collections.Generic;
using CarX;
using DI;
using GameOverlay;
using Sfs2X.Entities.Data;
using Sfs2X.Util;
using SyncMultiplayer.NetGameSystems.Collisions;
using UnityEngine;

namespace SyncMultiplayer
{
	public class NetGameSyncPosSystem : BaseNetGameSystem
	{
		public class PosSyncInfo
		{
			public NetGameCircularInterpolator interpolator;

			public long lastTimeUpdated;

			public long lockedByCollision;

			public bool isSyncDirty;

			public float syncSteer;

			public Quaternion syncRotError = Quaternion.identity;

			public Vector3 fwdPosError;

			public Vector3 sidePosError;

			public int syncGeneration;

			public bool update = true;

			private Vector3 m_debugSyncPosition;

			private Vector3 m_debugLastPos;

			public NetworkPlayer host;

			private RaceCarFromCollderCache m_raceCarCollderCache = new RaceCarFromCollderCache();

			public PosSyncInfo(NetworkPlayer host)
			{
				this.host = host;
				isSyncDirty = false;
				lockedByCollision = -1L;
				lastTimeUpdated = -1L;
				interpolator = new NetGameCircularInterpolator(host, 120);
			}

			public void HandleCollision(Collision collInfo)
			{
				if (collInfo != null && !(CalcImpact(collInfo) < 2.5f) && lockedByCollision <= 0 && !(m_raceCarCollderCache.GetCar(collInfo) == null))
				{
					lockedByCollision = PreciseTime.unscaledTimeMs + (m_syncTime.GetLastPingMs() + host.Ping);
					fwdPosError = Vector3.zero;
					sidePosError = Vector3.zero;
					syncRotError = Quaternion.identity;
				}
				static float CalcImpact(Collision col)
				{
					Vector3 zero = Vector3.zero;
					for (int i = 0; i < col.contactCount; i++)
					{
						zero += col.GetContact(i).normal;
					}
					if (col.contactCount > 0)
					{
						zero /= (float)col.contactCount;
					}
					return Mathf.Abs(Vector3.Dot(zero, col.relativeVelocity));
				}
			}

			public void Update()
			{
				if (!host.IsLocal && !(host.userCar == null))
				{
					Car carX = host.userCar.carX;
					Rigidbody getRigidbody = carX.getRigidbody;
					float steer = carX.GetSteer();
					carX.SetSteer(Mathf.Lerp(steer, syncSteer, Time.deltaTime * 10f), useAssistent: false, Time.deltaTime);
					float magnitude = sidePosError.magnitude;
					float magnitude2 = fwdPosError.magnitude;
					Vector3 vector = Vector3.MoveTowards(Vector3.zero, sidePosError, 2f * Time.deltaTime * magnitude);
					Vector3 vector2 = Vector3.MoveTowards(Vector3.zero, fwdPosError, 1f * Time.deltaTime * magnitude2);
					Vector3 vector3 = vector + vector2;
					Quaternion quaternion = Quaternion.Lerp(Quaternion.identity, syncRotError, 6f * Time.deltaTime);
					float num = magnitude + magnitude2;
					if (num > 6f || vector3.sqrMagnitude > num * num)
					{
						vector3 = fwdPosError + sidePosError;
						quaternion = syncRotError;
						fwdPosError = Vector3.zero;
						sidePosError = Vector3.zero;
						syncRotError = Quaternion.identity;
					}
					else
					{
						fwdPosError -= vector2;
						sidePosError -= vector;
						syncRotError *= Quaternion.Inverse(quaternion);
					}
					m_debugLastPos = getRigidbody.position + vector3;
					getRigidbody.MovePosition(getRigidbody.position + vector3);
					if (!(getRigidbody.rotation * quaternion).IsRotationHaveNANs())
					{
						getRigidbody.MoveRotation(getRigidbody.rotation * quaternion);
					}
				}
			}

			public void OnDrawGizmos()
			{
				if (!host.IsLocal && !(host.userCar == null))
				{
					Gizmos.color = Color.red;
					Gizmos.DrawLine(m_debugSyncPosition, m_debugLastPos);
					Gizmos.color = Color.cyan;
					Gizmos.DrawSphere(m_debugSyncPosition, 0.25f);
				}
			}

			public void UpdateSync(RaceCar car)
			{
				if (!(car == null) && !(car.carX == null))
				{
					Sync(car, m_syncTime.GetModelFixedTimeMs());
				}
			}

			private void Sync(RaceCar raceCar, long modelFixedTimeMs)
			{
				Car carX = raceCar.carX;
				PosDataFrame dataFrame = interpolator.GetDataFrame(modelFixedTimeMs);
				if (!dataFrame.IsValid())
				{
					return;
				}
				m_debugSyncPosition = interpolator.GetDataFrame(modelFixedTimeMs + (long)(Time.fixedDeltaTime * 1000f)).position;
				if (dataFrame.IsFlaged(DataFrameFlags.Inactive))
				{
					if (host.userCar != null && host.userCar.gameObject.activeSelf)
					{
						host.userCar.gameObject.SetActive(value: false);
					}
					return;
				}
				if (dataFrame.IsFlaged(DataFrameFlags.Finished))
				{
					if (!(host.userCar == null))
					{
						carX.handbrake = 1f;
						carX.accelerate = 0f;
						carX.brake = 0f;
						fwdPosError = Vector3.zero;
						sidePosError = Vector3.zero;
						syncRotError = Quaternion.identity;
						isSyncDirty = false;
					}
					return;
				}
				Rigidbody getRigidbody = raceCar.getRigidbody;
				if (!host.userCar.gameObject.activeSelf || syncGeneration != dataFrame.posGeneration)
				{
					syncGeneration = dataFrame.posGeneration;
					InstantRepositionCar(host.userCar, this);
					getRigidbody.position = dataFrame.position;
					getRigidbody.rotation = dataFrame.rotation;
					m_ignoreSystem.UpdateVisibilityPlayers();
					if (host.userCar.gameObject.activeSelf)
					{
						m_collisions.PlayerSpawned(host);
					}
				}
				else
				{
					if (interpolator.lastExtrapolated && (!interpolator.lastExtrapolated || !isSyncDirty))
					{
						return;
					}
					syncSteer = dataFrame.steer;
					carX.accelerate = dataFrame.accelerate;
					carX.gear = (sbyte)dataFrame.gear;
					carX.brake = dataFrame.brake;
					carX.handbrake = dataFrame.handbrake;
					if (dataFrame.processEngineStart)
					{
						if (!carX.autoClutch && dataFrame.autoClutch && !dataFrame.clutchKick)
						{
							carX.autoClutch = true;
						}
						else if ((carX.autoClutch && !dataFrame.autoClutch) || dataFrame.clutchKick)
						{
							carX.autoClutch = false;
						}
						if (!carX.autoClutch || dataFrame.clutchKick)
						{
							carX.clutch = (dataFrame.clutchKick ? 1f : dataFrame.clutch);
						}
						raceCar.engineController.SetNetworkIgnition(dataFrame.ignitionEnabled);
						raceCar.engineController.StarterNetwork(dataFrame.starterEnabled, dataFrame.engineStartTime);
						raceCar.mileageMoreZero = dataFrame.mileage;
						if (dataFrame.rpm > carX.engineCutRPM && raceCar.engineController.isIgnitionOn && !raceCar.engineController.engineWorking && !raceCar.engineController.isStarterOn)
						{
							raceCar.engineController.ForceStartEngine(bumpRpm: true);
						}
						else if (dataFrame.rpm < carX.engineCutRPM && !raceCar.engineController.isStarterOn)
						{
							raceCar.engineController.ShutdownEngineNetwork();
						}
					}
					else
					{
						carX.autoClutch = true;
					}
					if ((bool)host.userCar.flares)
					{
						host.userCar.flares.flares.SetActive(dataFrame.flares);
					}
					if (PreciseTime.fixedUnscaledTimeMs >= lockedByCollision)
					{
						if (lockedByCollision > 0)
						{
							lockedByCollision = -1L;
						}
						if (isSyncDirty)
						{
							getRigidbody.velocity = dataFrame.velocity;
							getRigidbody.angularVelocity = dataFrame.angularVelocity;
						}
						isSyncDirty = false;
						Vector3 vector = dataFrame.position - getRigidbody.position;
						fwdPosError = Vector3.Project(vector, dataFrame.velocity.normalized);
						sidePosError = vector - fwdPosError;
						syncRotError = Quaternion.Inverse(getRigidbody.rotation) * dataFrame.rotation;
					}
				}
			}
		}

		private class PH_Controls
		{
			public class Code
			{
				public static readonly string Key = "k";

				public static readonly byte PosGeneration = 0;

				public static readonly byte SentTime = 1;

				public static readonly byte Position = 2;

				public static readonly byte SentTimeMs = 3;

				public static readonly string PosData = "d";

				public static readonly string PointsData = "p";

				public static readonly string PingData = "s";

				public const int KEY = 98728240;
			}

			private enum DebugPingCode
			{
				Zero = 0,
				FirstPing = -1,
				LocalNotSync = -2,
				PlayerOldPacket = -3,
				UserCarXDisable = -4,
				PositionNaN = -5,
				MustHidePlayer = -6,
				UserCarLoading = -7,
				FinishedPlayer = -8,
				LoadUGCCar = -9,
				NetworkSpawnCar = -10,
				PlayerCarSpawned = -11,
				SyncDataCorrupted = -12
			}

			public const PacketId ID = PacketId.Controls;

			private NetGameSyncPosSystem m_syncPos;

			public PH_Controls(NetGameSyncPosSystem syncPos)
			{
				m_syncPos = syncPos;
			}

			public static SmartfoxDataPackage ConstructData(NetGameSyncPosSystem syncPos, NetworkPlayer player, RaceCar car)
			{
				SmartfoxDataPackage result = new SmartfoxDataPackage(PacketId.Controls);
				result.Add(Code.Key, 98728240);
				SFSArray sFSArray = new SFSArray();
				sFSArray.AddByte(player.spawnGeneration);
				sFSArray.AddFloat(m_syncTime.GetModelFixedTime());
				if (car == null)
				{
					sFSArray.AddByteArray(new ByteArray(CustomTypes.SerializeCarxInputControls(null, 0f)));
				}
				else
				{
					m_prefs.stats.carMileage.TryGetValue(player.userCar.carId, out var value);
					if (syncPos.m_syncLocalCar)
					{
						if (syncPos.m_lastSyncedRecordData != null)
						{
							syncPos.m_lastSyncedRecordData = null;
						}
						sFSArray.AddByteArray(new ByteArray(CustomTypes.SerializeCarxInputControls(car, value)));
					}
					else
					{
						if (syncPos.m_lastSyncedRecordData == null)
						{
							syncPos.m_lastSyncedRecordData = new ByteArray(CustomTypes.SerializeCarxDefaultData(car, value));
						}
						sFSArray.AddByteArray(syncPos.m_lastSyncedRecordData);
					}
				}
				sFSArray.AddLong(m_syncTime.GetModelFixedTimeMs());
				result.AddSFSArray(Code.PosData, sFSArray);
				return result;
			}

			public async void HandleAsync(NetworkPlayer player, SmartfoxDataPackage data)
			{
				if (!m_syncPos.TryGetRAWInfo(player.NetworkID, out var posInfo))
				{
					return;
				}
				ISFSArray sFSArray = data.Data.GetSFSArray(Code.PosData);
				if (player.Ping == 0)
				{
					DebugPingLogExit(player, data, DebugPingCode.FirstPing, !m_syncTime.initialSyncDone);
				}
				if (!data.Data.ContainsKey(Code.Key) || data.Data.GetInt(Code.Key) != 98728240)
				{
					return;
				}
				player.spawnGeneration = sFSArray.GetByte(Code.PosGeneration);
				long num = ((sFSArray.Count > Code.SentTimeMs) ? sFSArray.GetLong(Code.SentTimeMs) : ((long)(sFSArray.GetFloat(Code.SentTime) * 1000f)));
				if (!m_syncTime.initialSyncDone || posInfo.lastTimeUpdated > num)
				{
					DebugPingLogExit(player, data, CodeSyncProblem(), !m_syncTime.initialSyncDone);
					DebugPingLogExit(player, data, DebugPingCode.PlayerOldPacket, posInfo.lastTimeUpdated > num);
					return;
				}
				if (data.Data.ContainsKey(Code.PointsData))
				{
					m_points.ApplyPackedData(player, data.Data.GetSFSArray(Code.PointsData));
				}
				if (data.Data.ContainsKey(Code.PingData))
				{
					player.Ping = data.Data.GetInt(Code.PingData);
				}
				if (!posInfo.update && posInfo.lastTimeUpdated > 0)
				{
					return;
				}
				if (player.userCar != null && !player.userCar.carX.enabled)
				{
					player.userCar.gameObject.SetActive(value: false);
					DebugPingLogExit(player, data, DebugPingCode.UserCarXDisable, condition: true);
					return;
				}
				PosDataFrame dataFrame = CustomTypes.DeserializeCarxInputControls(sFSArray.GetByteArray(Code.Position).Bytes);
				if (!dataFrame.IsValid())
				{
					DebugPingLogExit(player, data, DebugPingCode.SyncDataCorrupted, condition: true);
					return;
				}
				dataFrame.modelTime = num;
				dataFrame.posGeneration = player.spawnGeneration;
				posInfo.interpolator.AddData(ref dataFrame);
				posInfo.lastTimeUpdated = num;
				player.lastRecivedNetworkPosition = dataFrame.position;
				player.lastRecivedNetworkRotation = dataFrame.rotation;
				if (m_subrooms.MustHidePlayer(player))
				{
					posInfo.interpolator.MarkLastWithFlags(DataFrameFlags.Inactive);
					DebugPingLogExit(player, data, DebugPingCode.MustHidePlayer, condition: true);
					return;
				}
				posInfo.interpolator.UnmarkLastWithFlags(DataFrameFlags.Inactive);
				if (player.ugcCarReadyForConstruction)
				{
					player.ConstructUGCCar();
					DebugPingLogExit(player, data, DebugPingCode.LoadUGCCar, condition: true);
				}
				if (player.isNetworkCarLoading)
				{
					return;
				}
				if (player.userCar == null)
				{
					if (player.isNetworkCarLoading)
					{
						DebugPingLogExit(player, data, DebugPingCode.UserCarLoading, condition: true);
						return;
					}
					DebugPingLogExit(player, data, DebugPingCode.NetworkSpawnCar, condition: true);
					NetworkPlayer networkPlayer = player;
					networkPlayer.OnCarAttached = (Action<NetworkPlayer>)Delegate.Combine(networkPlayer.OnCarAttached, new Action<NetworkPlayer>(OnCarAttachedCallback));
					await player.NetworkSpawnAsync(respawn: true);
				}
				if (m_subrooms.IsFinishedPlayerInMyRoom(player))
				{
					DebugPingLogExit(player, data, DebugPingCode.FinishedPlayer, condition: true);
					posInfo.interpolator.MarkLastWithFlags(DataFrameFlags.Finished);
				}
				else
				{
					posInfo.isSyncDirty = true;
				}
				void OnCarAttachedCallback(NetworkPlayer netPlayer)
				{
					if (netPlayer != null)
					{
						netPlayer.OnCarAttached = (Action<NetworkPlayer>)Delegate.Remove(netPlayer.OnCarAttached, new Action<NetworkPlayer>(OnCarAttachedCallback));
						DebugPingLogExit(netPlayer, data, DebugPingCode.PlayerCarSpawned, condition: true);
						m_collisions.PlayerSpawned(netPlayer);
						posInfo.fwdPosError = Vector3.zero;
						posInfo.sidePosError = Vector3.zero;
						posInfo.syncRotError = Quaternion.identity;
						m_collisions.UpdateCollideState(player);
						netPlayer.userCar.OnCollisionEnterEvent -= posInfo.HandleCollision;
						netPlayer.userCar.OnCollisionEnterEvent += posInfo.HandleCollision;
						netPlayer.userCar.transform.position = posInfo.interpolator.GetLast().position;
						netPlayer.userCar.transform.rotation = posInfo.interpolator.GetLast().rotation;
						if (States.mainCurrent is SyncNetFreerideRaceModeState { IsLoadingScreenHided: not false })
						{
							Overlay.NotifyMPFeature(NetworkController.InstanceGame.Players.Count > 1);
						}
					}
				}
			}

			private DebugPingCode CodeSyncProblem()
			{
				return (DebugPingCode)(-(200 + 10 * m_syncTime.frameToSync + ((m_syncTime.lastGenDiff >= 0) ? m_syncTime.lastGenDiff : (10 + m_syncTime.lastGenDiff))));
			}

			private void DebugPingLogExit(NetworkPlayer player, SmartfoxDataPackage data, DebugPingCode code, bool condition)
			{
				if (condition)
				{
					player.Ping = (int)code;
				}
			}
		}

		private const int SYNC_HZ = 30;

		private const int SYNC_INFO_HZ = 10;

		private const int CIRCULAR_BUFFER_SIZE_SEC = 4;

		private const float CLUTCH_SYNC_SCALE = 0.7f;

		private ApproxTimeKeeper m_syncPlayer = new ApproxTimeKeeper(33L);

		private ApproxTimeKeeper m_syncPlayerInfo = new ApproxTimeKeeper(100L);

		private NetworkGame m_game;

		private static NetGameCollisionManager m_collisions;

		private static NetGameSyncPointsSystem m_points;

		private static NetGameSubroomsSystem m_subrooms;

		private static NetGameSyncTimeSystem m_syncTime;

		private static NetGameSyncSpawnSystem m_spawnSystem;

		private static NetGameIgnoreSystem m_ignoreSystem;

		private Dictionary<int, PosSyncInfo> m_SyncInfos = new Dictionary<int, PosSyncInfo>();

		private bool m_syncLocalCar = true;

		private ByteArray m_lastSyncedRecordData;

		private static long timeBiassMs;

		private static GamePrefs m_prefs;

		public override void Attach(NetGameSystemsManager owner)
		{
			base.Attach(owner);
			m_game = owner.host;
			m_points = owner.Get<NetGameSyncPointsSystem>();
			m_subrooms = owner.Get<NetGameSubroomsSystem>();
			m_collisions = owner.Get<NetGameCollisionManager>();
			m_syncTime = owner.Get<NetGameSyncTimeSystem>();
			m_ignoreSystem = owner.Get<NetGameIgnoreSystem>();
			m_game.packetHandler.Subscribe(PacketId.Controls, new PH_Controls(this).HandleAsync);
			GameConsole.SetCmd("cl_timebiass", delegate
			{
				if (GameConsole.cmdArgs.Length >= 2)
				{
					long result = 0L;
					if (long.TryParse(GameConsole.cmdArgs[1], out result))
					{
						timeBiassMs = result;
					}
				}
			});
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
		}

		public override void OnGameConnect()
		{
			base.OnGameConnect();
			SubscriptionOnPreSimulation();
		}

		public override void OnReconnect()
		{
			base.OnReconnect();
			SubscriptionOnPreSimulation();
			m_SyncInfos.Clear();
			foreach (NetworkPlayer player in m_game.Players)
			{
				m_SyncInfos.Add(player.NetworkID, new PosSyncInfo(player));
			}
		}

		public override void OnDisconnect()
		{
			base.OnDisconnect();
			TickManager.UnregisterEarlyUpdate(PreSimulationCars);
		}

		public void SetPlayerActive(NetworkPlayer player, bool active)
		{
			if (m_SyncInfos.TryGetValue(player.NetworkID, out var value))
			{
				if (active)
				{
					value.interpolator.UnmarkLastWithFlags(DataFrameFlags.Inactive);
				}
				else
				{
					value.interpolator.MarkLastWithFlags(DataFrameFlags.Inactive);
				}
			}
		}

		private void SubscriptionOnPreSimulation()
		{
			TickManager.UnregisterEarlyUpdate(PreSimulationCars);
			TickManager.RegisterEarlyUpdate(PreSimulationCars, 11, "MP_SYNC");
		}

		private void PreSimulationCars()
		{
			foreach (KeyValuePair<int, PosSyncInfo> syncInfo in m_SyncInfos)
			{
				RaceCar userCar = syncInfo.Value.host.userCar;
				if ((bool)userCar && userCar.isNetworkCar)
				{
					syncInfo.Value.UpdateSync(userCar);
				}
			}
		}

		public override void DoPlayerEnter(NetworkPlayer player)
		{
			if (player != null)
			{
				m_SyncInfos[player.NetworkID] = new PosSyncInfo(player);
			}
		}

		public override void DoPlayerLeave(NetworkPlayer player)
		{
			m_SyncInfos.Remove(player.NetworkID);
		}

		public override void Update()
		{
			if (m_syncPlayer.ShouldExecute(PreciseTime.unscaledDeltaTimeMs / 2) && m_syncTime.initialSyncDone)
			{
				NetworkPlayer localPlayer = m_game.LocalPlayer;
				SmartfoxDataPackage data = PH_Controls.ConstructData(this, localPlayer, localPlayer.userCar);
				if (m_syncPlayerInfo.ShouldExecute(PreciseTime.unscaledDeltaTimeMs / 2))
				{
					localPlayer.Ping = SmartfoxClient.SmartfoxRTT / 2;
					data.AddSFSArray(PH_Controls.Code.PointsData, m_points.GetPacketData());
					data.Add(PH_Controls.Code.PingData, localPlayer.Ping);
					m_syncPlayerInfo.Reset();
				}
				m_game.SendRealtimeData(data);
				m_syncPlayer.Reset();
			}
			foreach (KeyValuePair<int, PosSyncInfo> syncInfo in m_SyncInfos)
			{
				syncInfo.Value.Update();
			}
		}

		public override void OnDrawGizmos()
		{
			if (m_SyncInfos == null)
			{
				return;
			}
			foreach (PosSyncInfo value in m_SyncInfos.Values)
			{
				value.OnDrawGizmos();
			}
		}

		public PosSyncInfo GetRAWInfo(int actorID)
		{
			return m_SyncInfos[actorID];
		}

		public bool TryGetRAWInfo(int actorID, out PosSyncInfo syncInfo)
		{
			return m_SyncInfos.TryGetValue(actorID, out syncInfo);
		}

		public void SetLocalCarSync(bool enable)
		{
			m_syncLocalCar = enable;
		}

		private static void InstantRepositionCar(RaceCar car, PosSyncInfo posInfo)
		{
			if (!(car == null))
			{
				posInfo.fwdPosError = Vector3.zero;
				posInfo.sidePosError = Vector3.zero;
				posInfo.syncRotError = Quaternion.identity;
			}
		}
	}
}
