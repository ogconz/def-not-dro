using System;
using Sfs2X.Entities.Data;
using UnityEngine;

namespace SyncMultiplayer
{
	public class NetGameSyncTimeSystem : BaseNetGameSystem
	{
		private const string SERVER_PING = "p";

		public const byte FRAMES_BEFORE_SYNC = 5;

		public const long SYNC_TIME = 1000L;

		public const long KEEP_SYNC_TIME = 10000L;

		public const long MAX_ERROR = 10L;

		private const long DESYNC_THRESHOLD_MS = 10L;

		private NetworkGame m_game;

		private bool m_initialSyncDone;

		private long m_lastSyncPing;

		private long m_lastSyncTime;

		private long m_lastSent;

		private long m_lastError = long.MaxValue;

		private long m_lastPing;

		private long m_lastTimeLag;

		private long m_serverOffsetMs;

		private long m_serverOffsetFixedMs;

		private long m_targetServerOffsetMs;

		private double m_unityTimeOffset;

		private double m_unityTimeTargetOffset;

		private long m_serverModelTime;

		private long m_pingHandleTime;

		private bool m_desyncDetected;

		private byte m_generation;

		private bool m_forceSend;

		private byte m_framesBeforeSync = 5;

		public int lastGenDiff;

		private SFSObject m_pingDataSFSObject = new SFSObject();

		private static int sfxPing => SmartfoxClient.SmartfoxRTT;

		public bool initialSyncDone => m_initialSyncDone;

		public int frameToSync => m_framesBeforeSync;

		public override void Attach(NetGameSystemsManager owner)
		{
			base.Attach(owner);
			m_game = owner.host;
			m_game.SubscribeOnServerCommand("p", HandlePing);
		}

		private static long LerpToward(long v, long target, long add)
		{
			long num = ((target - v >= 0) ? 1 : (-1));
			long num2 = v + num * add;
			if (num != ((target - num2 >= 0) ? 1 : (-1)))
			{
				num2 = target;
			}
			return num2;
		}

		private static double LerpToward(double v, double target, double add)
		{
			double num = ((target - v >= 0.0) ? 1 : (-1));
			double num2 = v + num * add;
			if (num != (double)((target - num2 >= 0.0) ? 1 : (-1)))
			{
				num2 = target;
			}
			return num2;
		}

		public override void Update()
		{
			long unscaledDeltaTimeMs = PreciseTime.unscaledDeltaTimeMs;
			long unscaledTimeMs = PreciseTime.unscaledTimeMs;
			long fixedUnscaledTimeMs = PreciseTime.fixedUnscaledTimeMs;
			if (m_initialSyncDone)
			{
				long num = Math.Abs(m_targetServerOffsetMs - m_serverOffsetMs);
				if (num > 300)
				{
					long num2 = 3L;
					m_serverOffsetMs = LerpToward(m_serverOffsetMs, m_targetServerOffsetMs, num2 * unscaledDeltaTimeMs);
					m_unityTimeOffset = LerpToward(m_unityTimeOffset, m_unityTimeTargetOffset, (double)num2 * Time.unscaledTimeAsDouble);
				}
				else if (num > 100)
				{
					long num3 = 1L;
					m_serverOffsetMs = LerpToward(m_serverOffsetMs, m_targetServerOffsetMs, num3 * unscaledDeltaTimeMs);
					m_unityTimeOffset = LerpToward(m_unityTimeOffset, m_unityTimeTargetOffset, (double)num3 * Time.unscaledTimeAsDouble);
				}
				else
				{
					m_serverOffsetMs = LerpToward(m_serverOffsetMs, m_targetServerOffsetMs, unscaledDeltaTimeMs >> 1);
					m_unityTimeOffset = LerpToward(m_unityTimeOffset, m_unityTimeTargetOffset, Time.unscaledTimeAsDouble / 2.0);
				}
				m_serverOffsetFixedMs = unscaledTimeMs + m_serverOffsetMs - fixedUnscaledTimeMs;
				if (unscaledTimeMs > m_lastSyncPing + 10000)
				{
					SendPing();
				}
			}
			else if (m_forceSend || unscaledTimeMs > m_lastSyncPing + 1000)
			{
				SendPing();
			}
		}

		public long GetLastErrorMs()
		{
			return m_lastError;
		}

		public long GetLastPingMs()
		{
			return m_lastPing;
		}

		public float GetModelFixedTime()
		{
			return (float)(PreciseTime.fixedTimeMs + m_serverOffsetFixedMs) / 1000f;
		}

		public long GetModelFixedTimeMs()
		{
			return PreciseTime.fixedTimeMs + m_serverOffsetFixedMs;
		}

		public float GetModelTime()
		{
			return (float)(PreciseTime.unscaledTimeMs + m_serverOffsetMs) / 1000f;
		}

		public long GetModelTimeMs()
		{
			return PreciseTime.unscaledTimeMs + m_serverOffsetMs;
		}

		public float ConvertToLocal(float modelTime)
		{
			return modelTime - (float)m_unityTimeOffset;
		}

		public float ConvertToModel(float localTime)
		{
			return localTime + (float)m_unityTimeOffset;
		}

		private void SendPing()
		{
			m_forceSend = false;
			m_generation++;
			m_lastSyncPing = PreciseTime.realTimeMs;
			m_lastSent = PreciseTime.realTimeMs;
			m_pingDataSFSObject.PutByte("g", m_generation);
			if (!m_initialSyncDone)
			{
				m_pingDataSFSObject.PutByte("u", 1);
			}
			m_game.SendServerCommand("p", m_pingDataSFSObject, out var sentTime);
			if (sentTime > 0)
			{
				m_lastSent = sentTime;
				m_lastSyncPing = sentTime;
			}
		}

		public override void OnReconnect()
		{
			m_initialSyncDone = false;
			m_generation = 0;
			m_lastSyncPing = 0L;
			m_lastSyncTime = 0L;
			m_serverOffsetMs = 0L;
			m_lastError = long.MaxValue;
			m_lastPing = 0L;
			m_framesBeforeSync = 5;
		}

		private void HandlePing(ISFSObject data)
		{
			byte b = data.GetByte("g");
			if (b != m_generation)
			{
				lastGenDiff = m_generation - b;
				return;
			}
			lastGenDiff = 0;
			long unscaledTimeMs = PreciseTime.unscaledTimeMs;
			m_lastPing = UpdatePing(unscaledTimeMs, out var roundTripTimeMs);
			m_lastTimeLag = roundTripTimeMs - sfxPing + sfxPing / 2;
			long num = m_lastError - m_lastPing;
			long num2 = data.GetInt("t") + m_lastTimeLag;
			long num3 = m_serverModelTime + (unscaledTimeMs - m_pingHandleTime);
			m_desyncDetected = Mathf.Abs(num2 - num3) > 1000f;
			long num4 = GetModelTimeMs() - num2;
			if (num4 * (long)Mathf.Sign(num4) > 10 || num > 10 || m_desyncDetected)
			{
				m_lastError = m_lastPing;
				m_targetServerOffsetMs = num2 - unscaledTimeMs;
				m_serverOffsetMs = m_targetServerOffsetMs;
				m_unityTimeTargetOffset = (double)num2 / 1000.0 - Time.unscaledTimeAsDouble;
				if (!m_initialSyncDone)
				{
					m_serverOffsetMs = m_targetServerOffsetMs;
					m_serverOffsetFixedMs = num2 - PreciseTime.fixedUnscaledTimeMs;
					m_unityTimeOffset = m_unityTimeTargetOffset;
				}
			}
			m_lastSyncTime = unscaledTimeMs;
			m_serverModelTime = num2;
			m_pingHandleTime = unscaledTimeMs;
			UpdateSyncState();
		}

		private void UpdateSyncState()
		{
			m_initialSyncDone = m_framesBeforeSync == 0;
			if (m_framesBeforeSync > 0)
			{
				m_forceSend = true;
				m_framesBeforeSync--;
			}
		}

		private long UpdatePing(long timeStamp, out long roundTripTimeMs)
		{
			roundTripTimeMs = timeStamp - m_lastSent;
			return roundTripTimeMs / 2;
		}

		private long EstimateServerTime(long serverSentTime)
		{
			return serverSentTime + m_lastPing;
		}
	}
}
