using System;
using System.IO;

namespace SyncMultiplayer.Objects
{
	public class ObjectCountdownSyncModel : BaseObjectSyncModel
	{
		public CountdownLights countdown;

		private NetGameSyncTimeSystem m_timeSync;

		private NetworkGame m_game;

		private float m_oldEndTime;

		private float m_oldCooldown;

		private bool m_isMaster => m_game.LocalPlayer.IsMasterClient;

		public override void Attach(NetGameSystemsManager systems)
		{
			m_game = systems.host;
			m_timeSync = systems.Get<NetGameSyncTimeSystem>();
		}

		public override bool Init(NetworkObject netObject)
		{
			if (!netObject.gameObject.TryGetComponent<CountdownLights>(out countdown))
			{
				return false;
			}
			m_oldCooldown = countdown.cooldown;
			countdown.SetTime(0f, 0f);
			return true;
		}

		public override void Update()
		{
			countdown.cooldown = (m_isMaster ? m_oldCooldown : 0f);
			countdown.time = (m_timeSync.initialSyncDone ? m_timeSync.GetModelTime() : 0f);
		}

		public override bool Synced()
		{
			if (m_isMaster)
			{
				return EqualEndTime();
			}
			return true;
		}

		public override void ReadPackage(byte[] bytes, ref int offset, SyncPacketType type)
		{
			if (type.IsInit() || type.IsState())
			{
				countdown.endTime = BitConverter.ToSingle(bytes, offset);
				offset += 4;
				countdown.readyDelay = BitConverter.ToSingle(bytes, offset);
				offset += 4;
			}
		}

		public override void WritePackage(MemoryStream stream, SyncPacketType type)
		{
			if (type.IsInit() || type.IsState())
			{
				stream.Write(BitConverter.GetBytes(countdown.endTime), 0, 4);
				stream.Write(BitConverter.GetBytes(countdown.readyDelay), 0, 4);
			}
		}

		private bool EqualEndTime()
		{
			bool num = Math.Abs(countdown.endTime - m_oldEndTime) < 0.001f;
			if (!num)
			{
				m_oldEndTime = countdown.endTime;
			}
			return num;
		}
	}
}
