using System.Collections.Generic;
using SyncMultiplayer.DynamicCollisions.Structs;
using Unity.Collections;

namespace SyncMultiplayer.DynamicCollisions.Stages
{
	public class CarSlotRegedit
	{
		private Queue<int> m_addQueue;

		private Queue<int> m_removeQueue;

		public CarSlotRegedit(DynamicCollisionsConfig config)
		{
			m_addQueue = new Queue<int>(config.maxCars);
			m_removeQueue = new Queue<int>(config.maxCars);
		}

		~CarSlotRegedit()
		{
			m_addQueue = null;
			m_removeQueue = null;
		}

		public void BindPlayer(NetworkPlayer player)
		{
			int slotID = player.SlotID;
			if (!m_addQueue.Contains(slotID))
			{
				m_addQueue.Enqueue(slotID);
			}
		}

		public void ReleasePlayer(NetworkPlayer player)
		{
			int slotID = player.SlotID;
			if (!m_removeQueue.Contains(slotID))
			{
				m_removeQueue.Enqueue(slotID);
			}
		}

		public void ProcessQueue(ref DataState state)
		{
			int result;
			while (m_removeQueue.TryDequeue(out result))
			{
				int num = state.activeSlots.IndexOf(result);
				if (num != -1)
				{
					state.activeSlots.RemoveAt(num);
					state.activeCarsCount--;
				}
			}
			int result2;
			while (m_addQueue.TryDequeue(out result2))
			{
				state.activeSlots.AddNoResize(result2);
				state.activeCarsCount++;
			}
		}
	}
}
