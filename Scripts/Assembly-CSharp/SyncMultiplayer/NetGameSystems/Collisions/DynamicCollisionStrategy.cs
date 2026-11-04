using System.Collections.Generic;
using CarModelSystem;
using SyncMultiplayer.DynamicCollisions;
using SyncMultiplayer.Utils;
using UnityEngine;

namespace SyncMultiplayer.NetGameSystems.Collisions
{
	public class DynamicCollisionStrategy : ICollisionHandlerStrategy
	{
		private DynamicCollisionMapHolder m_collisionMapHolder;

		private DynamicCollisionWorker m_dynamicCollisionWorker;

		private NetworkGame m_cachedGameInstance;

		private NetGameIgnoreSystem m_ignoreSystem;

		private NetGamePlayerStateSystem m_playerState;

		private NetworkPlayer[] m_slots = new NetworkPlayer[16];

		private Dictionary<string, List<NetworkPlayer>> m_collideGroups = new Dictionary<string, List<NetworkPlayer>>();

		private List<string> m_groupsToRemoveBuffer = new List<string>();

		private Stack<List<NetworkPlayer>> m_listsPool = new Stack<List<NetworkPlayer>>();

		private HashSet<string> m_groupsWithoutCollision = new HashSet<string>();

		private bool m_enabled;

		public void Initialize(NetGameSystemsManager owner)
		{
			m_cachedGameInstance = owner.host;
			m_ignoreSystem = owner.Get<NetGameIgnoreSystem>();
			m_playerState = owner.Get<NetGamePlayerStateSystem>();
			m_collisionMapHolder = new DynamicCollisionMapHolder(16, delegate(int leftSlotID, int rightSlotID, bool state)
			{
				RaceCar raceCar = m_slots[leftSlotID]?.userCar;
				RaceCar raceCar2 = m_slots[rightSlotID]?.userCar;
				if (raceCar == null || raceCar2 == null)
				{
					return false;
				}
				ApplyColliderState(raceCar.carModel, raceCar2.carModel, state);
				return true;
			});
			m_dynamicCollisionWorker = new DynamicCollisionWorker(owner, m_collisionMapHolder);
		}

		public void DoPlayerEnter(NetworkPlayer player)
		{
			for (int i = 0; i < m_slots.Length; i++)
			{
				if (m_slots[i] == null)
				{
					m_slots[i] = player;
					player.SlotID = (byte)i;
					MovePlayerToColliderGroup("", player);
					m_dynamicCollisionWorker.AddPlayer(player);
					break;
				}
			}
		}

		public void DoPlayerLeave(NetworkPlayer player)
		{
			m_groupsToRemoveBuffer.Clear();
			foreach (KeyValuePair<string, List<NetworkPlayer>> collideGroup in m_collideGroups)
			{
				for (int i = 0; i < collideGroup.Value.Count; i++)
				{
					if (player.PlayerId.Equals(collideGroup.Value[i].PlayerId))
					{
						collideGroup.Value.RemoveAtSwapBack(i);
						i--;
					}
				}
				if (collideGroup.Value.Count == 0)
				{
					m_groupsToRemoveBuffer.Add(collideGroup.Key);
				}
			}
			for (int j = 0; j < m_slots.Length; j++)
			{
				if (j != player.SlotID && m_slots[j] != null)
				{
					m_collisionMapHolder.ClearState(j, player.SlotID);
				}
			}
			foreach (string item in m_groupsToRemoveBuffer)
			{
				List<NetworkPlayer> list = m_collideGroups[item];
				list.Clear();
				m_listsPool.Push(list);
				m_collideGroups.Remove(item);
			}
			m_groupsToRemoveBuffer.Clear();
			if (m_slots[player.SlotID] == player)
			{
				m_slots[player.SlotID] = null;
			}
			m_dynamicCollisionWorker.RemovePlayer(player);
		}

		public void FixedUpdate()
		{
			if (m_enabled)
			{
				m_collisionMapHolder.ProcessApplyRequests();
			}
		}

		public NetGameCollisionManager.CollideState GetColliderState(NetworkPlayer player, out float time)
		{
			if (!m_enabled || player == null)
			{
				time = 0f;
				return NetGameCollisionManager.CollideState.On;
			}
			byte slotID = m_dynamicCollisionWorker.userRegedit.GetLocalUser().SlotID;
			byte slotID2 = player.SlotID;
			return m_collisionMapHolder.GetCollideState(slotID, slotID2, out time);
		}

		public void SetCollidingForGroup(string groupName, bool state)
		{
			if (state)
			{
				m_groupsWithoutCollision.Remove(groupName);
			}
			else
			{
				m_groupsWithoutCollision.Add(groupName);
			}
		}

		public void LateUpdate()
		{
			if (m_enabled)
			{
				m_collisionMapHolder.UpdateSuspendedStates(Time.unscaledDeltaTime);
			}
		}

		public void MovePlayerToColliderGroup(string groupName, NetworkPlayer playerToMove)
		{
			m_ignoreSystem.UpdateVisibilityPlayers();
			if (!m_enabled || playerToMove == null || playerToMove.NetworkID == -1)
			{
				return;
			}
			playerToMove.collideGroup = groupName;
			if (m_ignoreSystem.IgnorePlayerIfNeed(playerToMove) || m_playerState.ProcessPlayerIfNeed(playerToMove))
			{
				return;
			}
			foreach (List<NetworkPlayer> value2 in m_collideGroups.Values)
			{
				for (int i = 0; i < value2.Count; i++)
				{
					if (playerToMove.Equals(value2[i]))
					{
						value2.RemoveAtSwapBack(i);
						i--;
					}
				}
			}
			if (!m_collideGroups.TryGetValue(groupName, out var value))
			{
				value = ((m_listsPool.Count > 0) ? m_listsPool.Pop() : new List<NetworkPlayer>());
				m_collideGroups.Add(groupName, value);
			}
			value.Add(playerToMove);
			foreach (KeyValuePair<string, List<NetworkPlayer>> collideGroup in m_collideGroups)
			{
				if (collideGroup.Value.Count == 0)
				{
					m_groupsToRemoveBuffer.Add(collideGroup.Key);
					continue;
				}
				bool flag = !m_groupsWithoutCollision.Contains(collideGroup.Key);
				foreach (NetworkPlayer item in collideGroup.Value)
				{
					bool state = flag && collideGroup.Key == playerToMove.collideGroup;
					float timer = (string.IsNullOrEmpty(playerToMove.collideGroup) ? 15f : 5f);
					SetCollideState(playerToMove, item, state, timer);
				}
			}
			foreach (string item2 in m_groupsToRemoveBuffer)
			{
				List<NetworkPlayer> list = m_collideGroups[item2];
				list.Clear();
				m_listsPool.Push(list);
				m_collideGroups.Remove(item2);
			}
			m_groupsToRemoveBuffer.Clear();
		}

		public void OnReconnect()
		{
			int layer = LayerMask.NameToLayer("Car");
			int layer2 = LayerMask.NameToLayer("LocalCar");
			m_enabled = !Physics.GetIgnoreLayerCollision(layer, layer2);
			for (int i = 0; i < m_slots.Length; i++)
			{
				m_dynamicCollisionWorker.RemovePlayer(m_slots[i]);
				m_slots[i] = null;
			}
			int num = 0;
			foreach (NetworkPlayer player in m_cachedGameInstance.Players)
			{
				m_slots[num] = player;
				player.SlotID = (byte)num;
				MovePlayerToColliderGroup("", player);
				m_dynamicCollisionWorker.AddPlayer(player);
				num++;
			}
		}

		public void PlayerSpawned(NetworkPlayer player)
		{
			FlushCollideMap(player);
		}

		public void Update()
		{
			if (m_enabled)
			{
				m_dynamicCollisionWorker.Update();
			}
		}

		public void UpdateCollideState(NetworkPlayer player)
		{
			if (player == null)
			{
				return;
			}
			for (int i = 0; i < m_slots.Length; i++)
			{
				if (i != player.SlotID && m_slots[i] != null)
				{
					m_collisionMapHolder.ForceUpdateColliderState(i, player.SlotID);
				}
			}
		}

		private void ApplyColliderState(CarModel left, CarModel right, bool value)
		{
			int i = 0;
			for (int num = left.colliders.Length; i < num; i++)
			{
				int j = 0;
				for (int num2 = right.colliders.Length; j < num2; j++)
				{
					Physics.IgnoreCollision(left.colliders[i], right.colliders[j], !value);
				}
			}
		}

		private void SetCollideState(NetworkPlayer lhs, NetworkPlayer rhs, bool state, float timer)
		{
			if (lhs != null && rhs != null)
			{
				m_collisionMapHolder.SetCollisionState(lhs.SlotID, rhs.SlotID, state, timer);
			}
		}

		private void FlushCollideMap(NetworkPlayer player)
		{
			if (player == null)
			{
				return;
			}
			bool flag = !m_groupsWithoutCollision.Contains(player.collideGroup);
			byte slotID = player.SlotID;
			for (int i = 0; i < m_slots.Length; i++)
			{
				NetworkPlayer networkPlayer = m_slots[i];
				if (slotID != i && networkPlayer != null)
				{
					bool newState = flag && player.collideGroup == networkPlayer.collideGroup;
					float suspendAfterChangeTime = (string.IsNullOrEmpty(player.collideGroup) ? 15f : 5f);
					m_collisionMapHolder.SetCollisionState(slotID, i, newState, suspendAfterChangeTime);
				}
			}
		}
	}
}
