using UnityEngine;

namespace SyncMultiplayer
{
	public class NetGameSyncSupport : BaseNetGameSystem
	{
		public const float UNSYNC_TIME_TO_RECONNECT = 3f;

		private NetworkGame m_game;

		private NetGameSyncTimeSystem m_timeSync;

		private float m_unsyncTime;

		public override void Attach(NetGameSystemsManager owner)
		{
			base.Attach(owner);
			m_game = owner.host;
			m_timeSync = owner.Get<NetGameSyncTimeSystem>();
		}

		public override void Update()
		{
			if (m_game.State == ClientState.Joined && !m_timeSync.initialSyncDone && States.current is SyncNetFreerideRaceModeState)
			{
				m_unsyncTime += Mathf.Min(Time.unscaledDeltaTime, 0.33f);
				if (m_unsyncTime > 3f)
				{
					ResetUnsyncTime();
					NetworkController.TryReconnect();
				}
			}
		}

		public override void OnReconnect()
		{
			ResetUnsyncTime();
		}

		public override void OnRaceLoaded()
		{
			ResetUnsyncTime();
		}

		private void ResetUnsyncTime()
		{
			m_unsyncTime = 0f;
		}
	}
}
