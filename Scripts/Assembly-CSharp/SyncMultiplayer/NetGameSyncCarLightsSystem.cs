using System.Collections;
using System.Collections.Generic;
using DI;
using GameOverlay;

namespace SyncMultiplayer
{
	public class NetGameSyncCarLightsSystem : BaseNetGameSystem
	{
		private class LightsSyncCar
		{
			public readonly NetworkPlayer player;

			public bool lightsSynced;

			public LightsSyncCar(NetworkPlayer networkPlayer)
			{
				player = networkPlayer;
			}
		}

		private const string LIGHTS_ENABLED_KEY = "l";

		private const string SEND_BACK_KEY = "sb";

		private NetworkGame m_game;

		private GamePrefs m_prefs;

		private bool m_forceSendLightPacket;

		private bool m_isSyncedHeadlights;

		private readonly Dictionary<int, LightsSyncCar> m_lightSyncPlayersIds = new Dictionary<int, LightsSyncCar>(16);

		private readonly List<int> m_playersIdsToSync = new List<int>(16);

		public override void Attach(NetGameSystemsManager systemOwner)
		{
			base.Attach(systemOwner);
			m_game = systemOwner.host;
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
			m_game.packetHandler.Subscribe(PacketId.Headlights, OnLightsDataReceived);
			States.onStateActivate += OnStateActivateHandler;
		}

		private void OnStateActivateHandler(BaseState state)
		{
			if (state is SyncNetFreerideRaceModeState)
			{
				bool flag = IsSyncEnabled();
				m_forceSendLightPacket = m_isSyncedHeadlights != flag && flag;
				m_isSyncedHeadlights = flag;
			}
		}

		public override void OnReconnect()
		{
			base.OnReconnect();
			m_lightSyncPlayersIds.Clear();
			foreach (NetworkPlayer player in m_game.Players)
			{
				if (player != null)
				{
					m_lightSyncPlayersIds.Add(player.NetworkID, new LightsSyncCar(player));
				}
			}
		}

		public override void OnDisconnect()
		{
			base.OnDisconnect();
			m_lightSyncPlayersIds.Clear();
		}

		public override void DoPlayerEnter(NetworkPlayer player)
		{
			base.DoPlayerEnter(player);
			if (player != null)
			{
				m_lightSyncPlayersIds[player.NetworkID] = new LightsSyncCar(player);
			}
		}

		public override void DoPlayerLeave(NetworkPlayer player)
		{
			if (player != null && m_lightSyncPlayersIds.ContainsKey(player.NetworkID))
			{
				m_lightSyncPlayersIds.Remove(player.NetworkID);
			}
		}

		public override void Update()
		{
			NetworkPlayer localPlayer = m_game.LocalPlayer;
			if (localPlayer == null || localPlayer.userCar == null)
			{
				return;
			}
			m_playersIdsToSync.Clear();
			foreach (KeyValuePair<int, LightsSyncCar> lightSyncPlayersId in m_lightSyncPlayersIds)
			{
				if (lightSyncPlayersId.Value != null && lightSyncPlayersId.Value.player != null && !(lightSyncPlayersId.Value.player.userCar == null) && !lightSyncPlayersId.Value.player.userCar.isLocalPlayer && !lightSyncPlayersId.Value.lightsSynced)
				{
					m_playersIdsToSync.Add(lightSyncPlayersId.Value.player.NetworkID);
					lightSyncPlayersId.Value.lightsSynced = true;
				}
			}
			if (IsSyncEnabled() && m_playersIdsToSync.Count > 0)
			{
				base.owner.host.Client?.Send(MakePackage(localPlayer.userCar.headlightsEnabled), reliable: true, m_playersIdsToSync.ToArray());
			}
			if (m_forceSendLightPacket)
			{
				m_forceSendLightPacket = false;
				base.owner.host.Client?.Send(MakePackage(localPlayer.userCar.headlightsEnabled, needResend: true), reliable: true);
			}
		}

		public void SendToAll(bool headlightsEnabled)
		{
			if (IsSyncEnabled())
			{
				base.owner.host.Client?.Send(MakePackage(headlightsEnabled), reliable: true);
			}
		}

		private void OnLightsDataReceived(NetworkPlayer sender, SmartfoxDataPackage data)
		{
			if (IsSyncEnabled())
			{
				TimeController.StartNow(SetHeadlightEnabledCoroutine(sender, data));
			}
		}

		private IEnumerator SetHeadlightEnabledCoroutine(NetworkPlayer sender, SmartfoxDataPackage data)
		{
			if (sender == null)
			{
				yield break;
			}
			while (sender.userCar == null || sender.userCar.IsLoadingVisualInProgress)
			{
				yield return null;
			}
			bool lightsEnabled = data.Data.GetBool("l");
			sender.userCar.SetHeadlightsEnabled(lightsEnabled);
			if (data.Data.GetBool("sb"))
			{
				RaceCar raceCar = m_game?.LocalPlayer.userCar;
				if ((bool)raceCar)
				{
					SendToAll(raceCar.headlightsEnabled);
				}
			}
		}

		private bool IsSyncEnabled()
		{
			if (m_prefs.globals.syncHeadlights)
			{
				return !Overlay.instance.IsUserGeneratedContentRestricted;
			}
			return false;
		}

		private static SmartfoxDataPackage MakePackage(bool headlightsEnabled, bool needResend = false)
		{
			SmartfoxDataPackage result = new SmartfoxDataPackage(PacketId.Headlights);
			result.Data.PutBool("l", headlightsEnabled);
			if (needResend)
			{
				result.Data.PutBool("sb", needResend);
			}
			return result;
		}
	}
}
