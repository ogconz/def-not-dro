using System;
using CarProfile;
using Cysharp.Threading.Tasks;
using DB;
using DI;

namespace SyncMultiplayer
{
	public class NetGameDynoSystem : BaseNetGameSystem
	{
		private const string CAR_SETTINGS_CHANGED_KEY = "csc";

		private const string CAR_SYNC_ENABLE_KEY = "csynce";

		private const string CAR_SYNC_DISABLE_KEY = "csyncd";

		public Action<NetworkPlayer> onNetworkPlayerProfileApplied;

		private NetGameNotifySystem m_notify;

		private NetGameSubroomsSystem m_subrooms;

		private NetGameSyncPosSystem m_syncPos;

		private NetGamePlayerStateSystem m_playerState;

		private SafetyAwaiterSet<NetworkPlayer> m_changedSettings = new SafetyAwaiterSet<NetworkPlayer>();

		private BaseModel m_baseModel;

		private GamePrefs m_prefs;

		public override void Attach(NetGameSystemsManager owner)
		{
			base.Attach(owner);
			m_notify = owner.Get<NetGameNotifySystem>();
			m_subrooms = owner.Get<NetGameSubroomsSystem>();
			m_syncPos = owner.Get<NetGameSyncPosSystem>();
			m_playerState = owner.Get<NetGamePlayerStateSystem>();
			m_baseModel = DependencyInjector.Resolve<BaseModel>();
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
			m_notify.Subscribe("csc", RECV_CarSettingsChanged);
			m_notify.Subscribe("csynce", RECV_CarSyncEnable);
			m_notify.Subscribe("csyncd", RECV_CarSyncDisable);
			m_notify.Subscribe("pr", m_changedSettings.SetReady);
		}

		public override void OnReconnect()
		{
			base.OnReconnect();
			bool flag = States.current is DynoMenuState;
			UpdatePlayerDynoState(flag);
			UpdateCarSyncState(!flag);
		}

		public void UpdatePlayerDynoState(bool enter)
		{
			if (enter)
			{
				m_playerState.AddStateForLocalPlayer(PlayerCarState.Dyno);
				m_subrooms.SpawnLocalRoom();
			}
			else
			{
				m_subrooms.ReleaseLocalRoom();
				m_playerState.RemoveStateForLocalPlayer(PlayerCarState.Dyno);
			}
		}

		public void UpdateCarSyncState(bool enable)
		{
			m_notify.Send(enable ? "csynce" : "csyncd");
		}

		private void RECV_CarSyncEnable(NetworkPlayer player)
		{
			if (m_syncPos.TryGetRAWInfo(player.NetworkID, out var syncInfo))
			{
				syncInfo.interpolator.UnmarkLastWithFlags(DataFrameFlags.Finished);
				syncInfo.update = true;
				if (player.userCar != null)
				{
					player.userCar.getRigidbody.isKinematic = false;
				}
			}
		}

		private void RECV_CarSyncDisable(NetworkPlayer player)
		{
			if (m_syncPos.TryGetRAWInfo(player.NetworkID, out var syncInfo))
			{
				syncInfo.interpolator.MarkLastWithFlags(DataFrameFlags.Finished);
				syncInfo.update = false;
				if (player.userCar != null)
				{
					player.userCar.getRigidbody.isKinematic = true;
					player.userCar.AlignCarPositionAtSpawn();
				}
			}
		}

		public void SEND_CarSettingsChanged()
		{
			m_notify.Send("csc");
		}

		private void RECV_CarSettingsChanged(NetworkPlayer player)
		{
			m_changedSettings.Reset(player);
			AwaitAndApplyProfileChanges().Forget();
			async UniTask AwaitAndApplyProfileChanges()
			{
				if (await m_changedSettings.Wait(player) && !BaseCar.IsNull(player.userCar))
				{
					player.userCar.ApplyAdvancedProfile(m_baseModel, player.PlayerProperties.Profile, m_prefs.carSettings, reset: false, skipSwapControllers: true);
					player.userCar.AlignCarPositionAtSpawn();
					onNetworkPlayerProfileApplied.SafeCall(player);
				}
			}
		}
	}
}
