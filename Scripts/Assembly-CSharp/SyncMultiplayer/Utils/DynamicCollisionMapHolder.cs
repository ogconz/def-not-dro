using System;
using SyncMultiplayer.DynamicCollisions.Tools;
using SyncMultiplayer.NetGameSystems.Collisions;

namespace SyncMultiplayer.Utils
{
	public class DynamicCollisionMapHolder
	{
		private bool[] m_desiredCollisionMap;

		private bool[] m_commitedCollisionMap;

		private bool[] m_suspendedCollisionMap;

		private bool[] m_lockCollisionMap;

		private bool[] m_submitCollisionMap;

		private bool[] m_realCollisionMap;

		private bool[] m_applyRequestMap;

		private bool[] m_forceApplyMap;

		private float[] m_stopList;

		public bool[] rawDesiredCollideStates => m_desiredCollisionMap;

		public bool[] rawCommitedCollideStates => m_commitedCollisionMap;

		public bool[] rawLockCollideStates => m_lockCollisionMap;

		public bool[] rawSubmitCollideStates => m_submitCollisionMap;

		private Func<int, int, bool, bool> applyStateCallback { get; }

		private int maxPlayersCount { get; }

		public DynamicCollisionMapHolder(byte maxPlayersCount, Func<int, int, bool, bool> applyStateCallback)
		{
			if (applyStateCallback == null)
			{
				throw new ArgumentNullException("Error, applyStateCallback parameter cannot be NULL!");
			}
			int num = maxPlayersCount * (maxPlayersCount - 1) / 2;
			m_desiredCollisionMap = new bool[num];
			m_commitedCollisionMap = new bool[num];
			m_suspendedCollisionMap = new bool[num];
			m_lockCollisionMap = new bool[num];
			m_submitCollisionMap = new bool[num];
			m_realCollisionMap = new bool[num];
			m_applyRequestMap = new bool[num];
			m_forceApplyMap = new bool[num];
			m_stopList = new float[num];
			Array.Fill(m_desiredCollisionMap, value: true);
			Array.Fill(m_commitedCollisionMap, value: true);
			Array.Fill(m_submitCollisionMap, value: true);
			Array.Fill(m_realCollisionMap, value: true);
			this.applyStateCallback = applyStateCallback;
			this.maxPlayersCount = maxPlayersCount;
		}

		public void UpdateSuspendedStates(float dt)
		{
			int index = 0;
			for (int i = 0; i < maxPlayersCount; i++)
			{
				for (int j = i + 1; j < maxPlayersCount; j++)
				{
					if (!TryGetIndex(i, j, out index))
					{
						continue;
					}
					float blockTime = m_stopList[index];
					if (CheckBlockTime(ref blockTime, dt))
					{
						m_stopList[index] = blockTime;
						if (m_commitedCollisionMap[index] && m_suspendedCollisionMap[index])
						{
							ApplyColliderState(index, state: false);
						}
					}
					else if (m_desiredCollisionMap[index] != m_commitedCollisionMap[index])
					{
						m_stopList[index] = 0f;
						if (ApplyColliderState(index, m_desiredCollisionMap[index]))
						{
							m_suspendedCollisionMap[index] = false;
						}
					}
				}
			}
			static bool CheckBlockTime(ref float reference, float num)
			{
				reference -= num;
				return reference > 0f;
			}
		}

		public void ProcessApplyRequests()
		{
			for (int i = 0; i < maxPlayersCount; i++)
			{
				for (int j = i + 1; j < maxPlayersCount; j++)
				{
					if (TryGetIndex(i, j, out var index) && (m_applyRequestMap[index] || m_forceApplyMap[index]))
					{
						bool flag = m_commitedCollisionMap[index] && m_submitCollisionMap[index];
						if ((m_realCollisionMap[index] != flag || m_forceApplyMap[index]) && applyStateCallback(i, j, flag))
						{
							m_applyRequestMap[index] = false;
							m_forceApplyMap[index] = false;
							m_realCollisionMap[index] = flag;
						}
					}
				}
			}
		}

		public void SetCollisionState(int leftSlotID, int rightSlotID, bool newState, float suspendAfterChangeTime = 0f, bool resetSuspendTime = true)
		{
			if (TryGetIndex(leftSlotID, rightSlotID, out var index))
			{
				SetCollisionStateByPairIndex(index, newState, suspendAfterChangeTime, resetSuspendTime);
			}
		}

		public void SetCollisionStateByPairIndex(int index, bool newState, float suspendAfterChangeTime = 0f, bool resetSuspendTime = true)
		{
			m_desiredCollisionMap[index] = newState;
			if (!resetSuspendTime)
			{
				suspendAfterChangeTime = MathF.Max(m_stopList[index], suspendAfterChangeTime);
			}
			if (suspendAfterChangeTime > 0f && newState)
			{
				InternalSuspendCollisions(index, suspendAfterChangeTime);
			}
			else if (ApplyColliderState(index, newState))
			{
				m_suspendedCollisionMap[index] = false;
			}
		}

		public NetGameCollisionManager.CollideState GetCollideState(int leftSlotID, int rightSlotID, out float suspensionTime)
		{
			if (!TryGetIndex(leftSlotID, rightSlotID, out var index))
			{
				suspensionTime = -1f;
				return NetGameCollisionManager.CollideState.On;
			}
			if (!m_lockCollisionMap[index] && m_suspendedCollisionMap[index])
			{
				suspensionTime = m_stopList[index];
				return NetGameCollisionManager.CollideState.Suspended;
			}
			suspensionTime = 0f;
			if (!m_commitedCollisionMap[index])
			{
				return NetGameCollisionManager.CollideState.Off;
			}
			return NetGameCollisionManager.CollideState.On;
		}

		public void ClearState(int leftSlotID, int rightSlotID)
		{
			if (TryGetIndex(leftSlotID, rightSlotID, out var index))
			{
				m_commitedCollisionMap[index] = true;
				m_desiredCollisionMap[index] = true;
				m_suspendedCollisionMap[index] = false;
				m_lockCollisionMap[index] = false;
				m_submitCollisionMap[index] = true;
				m_realCollisionMap[index] = true;
				m_applyRequestMap[index] = false;
				m_forceApplyMap[index] = false;
				m_stopList[index] = 0f;
			}
		}

		public void ForceUpdateColliderState(int leftSlotID, int rightSlotID)
		{
			if (TryGetIndex(leftSlotID, rightSlotID, out var index))
			{
				m_forceApplyMap[index] = true;
			}
		}

		private void InternalSuspendCollisions(int index, float blockTime)
		{
			if (m_desiredCollisionMap[index])
			{
				m_suspendedCollisionMap[index] = true;
				m_stopList[index] = blockTime;
				if (m_commitedCollisionMap[index])
				{
					ApplyColliderState(index, state: false);
				}
			}
		}

		private bool ApplyColliderState(int index, bool state)
		{
			if (m_lockCollisionMap[index])
			{
				return false;
			}
			if (m_commitedCollisionMap[index] != state)
			{
				m_commitedCollisionMap[index] = state;
				m_applyRequestMap[index] = true;
			}
			return true;
		}

		private bool TryGetIndex(int leftSlotID, int rightSlotID, out int index)
		{
			if (rightSlotID != leftSlotID)
			{
				index = PairMath.GetPairIndex(leftSlotID, rightSlotID, maxPlayersCount);
			}
			else
			{
				index = -1;
			}
			if (index >= 0)
			{
				return index < m_commitedCollisionMap.Length;
			}
			return false;
		}
	}
}
