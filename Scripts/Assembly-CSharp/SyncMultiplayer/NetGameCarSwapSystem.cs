using System;
using System.Collections.Generic;
using System.Threading;
using CarProfile;
using CarX;
using Cysharp.Threading.Tasks;
using DB;
using DB.Meta;
using DI;
using GameOverlay;
using HUD;
using SyncMultiplayer.NetGameSystems;
using SyncMultiplayer.NetGameSystems.Collisions;
using UnityEngine;

namespace SyncMultiplayer
{
	public class NetGameCarSwapSystem : BaseNetGameSystem
	{
		private const string LiveryDownloadReadyKey = "l";

		private const string CarSwapStartKey = "cs";

		private const string CarSlotChangeKey = "slc";

		private const float BlockSubroomJoinBeforeSpawn = 15f;

		private const float BlockSubroomJoinAfterSpawn = 2f;

		public Action<NetworkPlayer> onCarSlotChanged;

		private NetGameNotifySystem m_notify;

		private NetGameSyncPosSystem m_syncPos;

		private NetGameTextureTransferSystem m_textureTransfer;

		private NetGameCollisionManager m_collision;

		private NetGameSubroomsSystem m_subrooms;

		private BaseModel m_baseModel;

		private GamePrefs m_prefs;

		private CarVisualManager m_carVisualManager;

		private Dictionary<NetworkPlayer, CancellationTokenSource> m_tokenSources = new Dictionary<NetworkPlayer, CancellationTokenSource>();

		private HashSet<NetworkPlayer> m_liveryUploads = new HashSet<NetworkPlayer>();

		private HashSet<NetworkPlayer> m_changedCars = new HashSet<NetworkPlayer>();

		private HashSet<NetworkPlayer> m_changedProfiles = new HashSet<NetworkPlayer>();

		private PlayerId m_targetWatchPlayerPid;

		public override void Attach(NetGameSystemsManager owner)
		{
			base.Attach(owner);
			m_notify = owner.Get<NetGameNotifySystem>();
			m_syncPos = owner.Get<NetGameSyncPosSystem>();
			m_textureTransfer = owner.Get<NetGameTextureTransferSystem>();
			m_collision = owner.Get<NetGameCollisionManager>();
			m_subrooms = owner.Get<NetGameSubroomsSystem>();
			m_baseModel = DependencyInjector.Resolve<BaseModel>();
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
			m_carVisualManager = DependencyInjector.Resolve<CarVisualManager>();
			m_notify.Subscribe("l", RECV_LiveryReady);
			m_notify.Subscribe("cs", RECV_CarSwapStart);
			m_notify.Subscribe("2", RECV_CarChange);
			m_notify.Subscribe("pr", RECV_ProfileChange);
			m_notify.Subscribe("slc", RECV_CarSlotChange);
		}

		public override void OnReconnect()
		{
			base.OnReconnect();
			DisposeTokenSources();
			ResetSync();
			m_liveryUploads.Clear();
			m_changedCars.Clear();
			m_changedProfiles.Clear();
		}

		public override void OnDisconnect()
		{
			base.OnDisconnect();
			CameraSwitch instance = CameraSwitch.instance;
			if ((bool)instance)
			{
				instance.onSwitchCar -= OnCameraSwapCar;
			}
			DisposeTokenSources();
			ResetSync();
		}

		public override void OnRaceLoaded()
		{
			base.OnRaceLoaded();
			CameraSwitch instance = CameraSwitch.instance;
			if ((bool)instance)
			{
				instance.onSwitchCar -= OnCameraSwapCar;
				instance.onSwitchCar += OnCameraSwapCar;
			}
		}

		public override void DoPlayerLeave(NetworkPlayer player)
		{
			base.DoPlayerLeave(player);
			DisposeTokenSource(player);
		}

		private void DisposeTokenSource(NetworkPlayer player)
		{
			if (m_tokenSources.TryGetValue(player, out var value))
			{
				value?.Cancel();
				value?.Dispose();
			}
			m_tokenSources.Remove(player);
		}

		private void DisposeTokenSources()
		{
			foreach (CancellationTokenSource value in m_tokenSources.Values)
			{
				value?.Cancel();
				value?.Dispose();
			}
			m_tokenSources.Clear();
		}

		private void EnableSyncForPlayer(NetworkPlayer player, bool enable)
		{
			if (m_syncPos.TryGetRAWInfo(player.NetworkID, out var syncInfo))
			{
				syncInfo.update = enable;
			}
		}

		private void ResetSync()
		{
			m_syncPos.SetLocalCarSync(enable: true);
			foreach (NetworkPlayer player in base.owner.host.Players)
			{
				EnableSyncForPlayer(player, enable: true);
			}
		}

		public void SEND_LiveryReady()
		{
			m_notify.Send("l");
		}

		private void RECV_LiveryReady(NetworkPlayer sender)
		{
			m_liveryUploads.Add(sender);
		}

		private void RECV_CarChange(NetworkPlayer sender)
		{
			m_changedCars.Add(sender);
		}

		private void RECV_ProfileChange(NetworkPlayer sender)
		{
			m_changedProfiles.Add(sender);
		}

		public void SEND_CarSwapStart()
		{
			m_subrooms.SetCooldownForSendRequests(15f);
			m_syncPos.SetLocalCarSync(enable: false);
			m_notify.Send("cs");
		}

		private void RECV_CarSwapStart(NetworkPlayer player)
		{
			ProcessCarSwapAsync(player).Forget();
		}

		private async UniTask ProcessCarSwapAsync(NetworkPlayer player)
		{
			m_textureTransfer.DisposeTokenSource(player);
			DisposeTokenSource(player);
			m_tokenSources[player] = new CancellationTokenSource();
			CancellationToken cancelToken = m_tokenSources[player].Token;
			m_liveryUploads.Remove(player);
			try
			{
				while (m_subrooms.MustHidePlayer(player) || player.isNetworkCarLoading || !player.userCar || !m_changedCars.Contains(player))
				{
					await UniTask.Yield(cancelToken);
				}
				m_changedCars.Remove(player);
				if (player.userCar == null || player.userCar.gameObject == null)
				{
					throw new OperationCanceledException("Object is null");
				}
				if (!m_syncPos.TryGetRAWInfo(player.NetworkID, out var posInfo))
				{
					throw new OperationCanceledException("Player is disconnected");
				}
				bool stayDroneCamera = IsNeedKeepLookAfterCar(player);
				player.userCar.gameObject.ReplaceLayerRecursively(29, 17);
				await UniTask.DelayFrame(10, PlayerLoopTiming.Update, cancelToken);
				Transform getTransform = player.userCar.getTransform;
				player.NetworkSpawn(getTransform.position, getTransform.rotation, respawn: false);
				await UniTask.WaitUntil(() => !player.isNetworkCarLoading && (bool)player.userCar, PlayerLoopTiming.Update, cancelToken);
				if (base.owner.host.PlayerHasUGCCar(player))
				{
					player.ugcCarReadyForConstruction = false;
					player.GrabUGCCar(player.userCar);
				}
				posInfo.update = true;
				m_collision.PlayerSpawned(player);
				m_collision.UpdateCollideState(player);
				await UniTask.NextFrame(cancelToken);
				stayDroneCamera |= IsNeedKeepLookAfterCar(player);
				player.userCar.OnCollisionEnterEvent -= posInfo.HandleCollision;
				player.userCar.OnCollisionEnterEvent += posInfo.HandleCollision;
				if (m_targetWatchPlayerPid.Equals(player.PlayerId))
				{
					WatchPlayer(player);
				}
				if (stayDroneCamera)
				{
					AttachCarToDroneCamera(player);
				}
				if (m_textureTransfer.PlayerHasLivery(player))
				{
					while (!m_liveryUploads.Contains(player))
					{
						await UniTask.Yield();
					}
					m_liveryUploads.Remove(player);
					if (player != null && !player.IsLocal)
					{
						await m_textureTransfer.DownloadUserLiveryAsync(player);
					}
				}
			}
			catch (Exception ex)
			{
				GameLog.Print(AlertLevel.Error, "NetGameCarSwap", ex.ToString());
			}
			Cleanup(player);
		}

		public async UniTask CarSwapReady()
		{
			m_subrooms.SetCooldownForSendRequests(2f);
			m_syncPos.SetLocalCarSync(enable: true);
			await base.owner.host.CaptureAndUploadCarLivery();
		}

		public void SEND_CarSlotChange()
		{
			m_subrooms.SetCooldownForSendRequests(15f);
			m_syncPos.SetLocalCarSync(enable: false);
			m_notify.Send("slc");
		}

		private void RECV_CarSlotChange(NetworkPlayer player)
		{
			ProcessCarSlotChangeAsync(player).Forget();
		}

		private async UniTask ProcessCarSlotChangeAsync(NetworkPlayer player)
		{
			_ = 5;
			try
			{
				m_textureTransfer.DisposeTokenSource(player);
				DisposeTokenSource(player);
				m_tokenSources[player] = new CancellationTokenSource(60000);
				CancellationToken cancelToken = m_tokenSources[player].Token;
				m_liveryUploads.Remove(player);
				m_changedProfiles.Remove(player);
				RaceCar raceCar = player.userCar;
				while (m_subrooms.MustHidePlayer(player) || player.isNetworkCarLoading || !raceCar || !m_changedProfiles.Contains(player))
				{
					await UniTask.Yield(cancelToken);
				}
				m_changedProfiles.Remove(player);
				if (raceCar == null || raceCar.gameObject == null)
				{
					throw new OperationCanceledException("Object is null");
				}
				if (!m_syncPos.TryGetRAWInfo(player.NetworkID, out var posInfo))
				{
					throw new OperationCanceledException("Player is disconnected");
				}
				posInfo.update = false;
				ProfilesInfo.Profile profile = player.PlayerProperties.Profile;
				PlayerCarProfile byId = m_baseModel.GetById<PlayerCarProfile>(profile.id);
				if (byId != null)
				{
					raceCar.profileMetaInfo = byId;
				}
				await m_carVisualManager.ApplyVisualAsync(CarVisualManager.CustomizationFlags.All, raceCar, player.PlayerProperties.VisualSettings, profile, isLocalPlayerCar: false);
				posInfo.update = true;
				UpdateCarPhysics(raceCar);
				int swapId = player.PlayerProperties.SwapId;
				if (!raceCar.swap.carAttached)
				{
					await raceCar.swap.Initialize(raceCar, m_prefs, m_baseModel, swapId);
				}
				else
				{
					await raceCar.swap.Refresh(swapId, force: true);
				}
				raceCar.ApplyAdvancedProfile(m_baseModel, profile, m_prefs.carSettings, reset: false, skipSwapControllers: true);
				m_collision.PlayerSpawned(player);
				m_collision.UpdateCollideState(player);
				if (m_textureTransfer.PlayerHasLivery(player))
				{
					while (!m_liveryUploads.Contains(player))
					{
						await UniTask.Yield(cancelToken);
					}
					m_liveryUploads.Remove(player);
					await m_textureTransfer.DownloadUserLiveryAsync(player);
				}
			}
			catch (Exception ex)
			{
				GameLog.Print(AlertLevel.Error, "NetGameCarSwap", ex.ToString());
			}
			Cleanup(player);
			onCarSlotChanged?.SafeCall(player);
		}

		private void UpdateCarPhysics(RaceCar raceCar)
		{
			raceCar.ResetVelocity();
			Singletone<Simulator>.instance.OnPostSimulationEvent += UpdateWheelAfterSimulation;
			Singletone<Simulator>.instance.OnUpdateWheelsEvent += UpdateCarModelAfterInterpolation;
			void UpdateCarModelAfterInterpolation()
			{
				Singletone<Simulator>.instance.OnUpdateWheelsEvent -= UpdateCarModelAfterInterpolation;
				raceCar.AlignCarPositionAtSpawn();
			}
			void UpdateWheelAfterSimulation()
			{
				Singletone<Simulator>.instance.OnPostSimulationEvent -= UpdateWheelAfterSimulation;
				raceCar.ResetVelocity();
			}
		}

		private void Cleanup(NetworkPlayer player)
		{
			EnableSyncForPlayer(player, enable: true);
			m_changedCars.Remove(player);
			m_liveryUploads.Remove(player);
			m_changedProfiles.Remove(player);
		}

		private void OnCameraSwapCar(RaceCar car)
		{
			if (BaseCar.IsNull(car))
			{
				return;
			}
			foreach (NetworkPlayer player in base.owner.host.Players)
			{
				if (player != null && (bool)player.userCar && player.userCar.Equals(car))
				{
					m_targetWatchPlayerPid = player.PlayerId;
					break;
				}
			}
		}

		private void WatchPlayer(NetworkPlayer player)
		{
			if (States.mainCurrent is BaseRaceState baseRaceState)
			{
				UIWatchPlayerContext uIWatchPlayerContext = baseRaceState.contextOwner.Get<UIWatchPlayerContext>();
				if (uIWatchPlayerContext != null && uIWatchPlayerContext.isActiveAndEnabled)
				{
					uIWatchPlayerContext.WatchPlayer(player);
				}
			}
		}

		private bool IsNeedKeepLookAfterCar(NetworkPlayer player)
		{
			if (!(States.current is FreeDroneState freeDroneState))
			{
				return false;
			}
			UIFreeDroneCameraContext uIFreeDroneCameraContext = freeDroneState.contextOwner.Get<UIFreeDroneCameraContext>();
			if (uIFreeDroneCameraContext == null)
			{
				return false;
			}
			return player.userCar.transform.Equals(uIFreeDroneCameraContext.GetTargetTransform());
		}

		private void AttachCarToDroneCamera(NetworkPlayer player)
		{
			if (States.current is FreeDroneState freeDroneState)
			{
				UIFreeDroneCameraContext uIFreeDroneCameraContext = freeDroneState.contextOwner.Get<UIFreeDroneCameraContext>();
				if (uIFreeDroneCameraContext != null)
				{
					uIFreeDroneCameraContext.SetCameraToCar(player.userCar, forceEnableOptions: false);
				}
			}
		}
	}
}
