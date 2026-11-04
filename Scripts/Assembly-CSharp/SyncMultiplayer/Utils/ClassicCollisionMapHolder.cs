using System;
using System.Collections;
using System.Collections.Generic;
using SyncMultiplayer.NetGameSystems.Collisions;

namespace SyncMultiplayer.Utils
{
	public class ClassicCollisionMapHolder
	{
		private BitArray m_desiredCollisionMap;

		private BitArray m_commitedCollisionMap;

		private Dictionary<int, float> m_stopList;

		private Func<int, int, bool, bool> applyStateCallback { get; }

		private int maxPlayersCount { get; }

		public ClassicCollisionMapHolder(byte maxPlayersCount, Func<int, int, bool, bool> applyStateCallback)
		{
			if (applyStateCallback == null)
			{
				throw new ArgumentNullException("Error, applyStateCallback parameter cannot be NULL!");
			}
			int num = maxPlayersCount * (maxPlayersCount - 1) / 2;
			m_desiredCollisionMap = new BitArray(num, defaultValue: true);
			m_commitedCollisionMap = new BitArray(num, defaultValue: true);
			m_stopList = new Dictionary<int, float>(num);
			this.applyStateCallback = applyStateCallback;
			this.maxPlayersCount = maxPlayersCount;
		}

		public void UpdateSuspendedStates(float dt)
		{
			int index = 0;
			float value = 0f;
			for (int i = 1; i < maxPlayersCount; i++)
			{
				for (int j = 0; j < i; j++)
				{
					if (!TryGetIndex(j, i, out index))
					{
						continue;
					}
					if (m_stopList.TryGetValue(index, out value) && CheckBlockTime(ref value, dt))
					{
						m_stopList[index] = value;
						if (m_commitedCollisionMap[index])
						{
							ApplyColliderState(j, i, index, state: false);
						}
					}
					else if (m_desiredCollisionMap[index] != m_commitedCollisionMap[index])
					{
						m_stopList.Remove(index);
						ApplyColliderState(j, i, index, m_desiredCollisionMap[index]);
					}
				}
			}
			static bool CheckBlockTime(ref float blockTime, float num)
			{
				blockTime -= num;
				return blockTime > 0f;
			}
		}

		public void SetCollisionState(int leftSlotID, int rightSlotID, bool newState, float suspendAfterChangeTime = 0f)
		{
			if (TryGetIndex(leftSlotID, rightSlotID, out var index))
			{
				m_desiredCollisionMap[index] = newState;
				if (suspendAfterChangeTime > 0f && newState)
				{
					InternalSuspendCollisions(leftSlotID, rightSlotID, index, suspendAfterChangeTime);
					return;
				}
				m_stopList.Remove(index);
				ApplyColliderState(leftSlotID, rightSlotID, index, newState);
			}
		}

		public NetGameCollisionManager.CollideState GetCollideState(int leftSlotID, int rightSlotID, out float suspensionTime)
		{
			if (!TryGetIndex(leftSlotID, rightSlotID, out var index))
			{
				suspensionTime = -1f;
				return NetGameCollisionManager.CollideState.On;
			}
			if (m_stopList.TryGetValue(index, out suspensionTime))
			{
				return NetGameCollisionManager.CollideState.Suspended;
			}
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
				m_stopList.Remove(index);
			}
		}

		private void InternalSuspendCollisions(int leftSlotID, int rightSlotID, int index, float blockTime)
		{
			if (m_desiredCollisionMap[index])
			{
				m_stopList[index] = blockTime;
				if (m_commitedCollisionMap[index])
				{
					ApplyColliderState(leftSlotID, rightSlotID, index, state: false);
				}
			}
		}

		private bool ApplyColliderState(int leftSlotID, int rightSlotID, int index, bool state)
		{
			if (applyStateCallback(leftSlotID, rightSlotID, state))
			{
				m_commitedCollisionMap[index] = state;
				return true;
			}
			return false;
		}

		private bool TryGetIndex(int leftSlotID, int rightSlotID, out int index)
		{
			if (leftSlotID > rightSlotID)
			{
				int num = leftSlotID;
				leftSlotID = rightSlotID;
				rightSlotID = num;
			}
			if (rightSlotID != leftSlotID)
			{
				index = rightSlotID * (rightSlotID - 1) / 2 + leftSlotID;
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
