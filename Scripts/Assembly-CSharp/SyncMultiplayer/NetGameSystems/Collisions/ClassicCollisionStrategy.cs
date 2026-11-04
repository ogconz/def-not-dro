using System.Collections.Generic;
using CarModelSystem;
using SyncMultiplayer.Utils;
using UnityEngine;

namespace SyncMultiplayer.NetGameSystems.Collisions
{
	public class ClassicCollisionStrategy : ICollisionHandlerStrategy
	{
		private ClassicCollisionMapHolder m_collisionMapHolder;

		private NetworkGame m_cachedGameInstance;

		private NetGameIgnoreSystem m_ignoreSystem;

		private NetGamePlayerStateSystem m_playerState;

		private NetworkPlayer[] m_slots = new NetworkPlayer[16];

		private Dictionary<string, List<NetworkPlayer>> m_collideGroups = new Dictionary<string, List<NetworkPlayer>>();

		private List<string> m_groupsToRemoveBuffer = new List<string>();

		private Stack<List<NetworkPlayer>> m_listPool = new Stack<List<NetworkPlayer>>();

		private bool m_enabled;

		public void Initialize(NetGameSystemsManager owner)
		{
			m_cachedGameInstance = owner.host;
			m_ignoreSystem = owner.Get<NetGameIgnoreSystem>();
			m_playerState = owner.Get<NetGamePlayerStateSystem>();
			m_collisionMapHolder = new ClassicCollisionMapHolder(16, delegate(int leftSlotID, int rightSlotID, bool state)
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
		}

		public void LateUpdate()
		{
			if (m_enabled)
			{
				m_collisionMapHolder.UpdateSuspendedStates(Time.unscaledDeltaTime);
			}
		}

		public void OnReconnect()
		{
			int layer = LayerMask.NameToLayer("Car");
			int layer2 = LayerMask.NameToLayer("LocalCar");
			m_enabled = !Physics.GetIgnoreLayerCollision(layer, layer2);
			for (int i = 0; i < m_slots.Length; i++)
			{
				m_slots[i] = null;
			}
			int num = 0;
			foreach (NetworkPlayer player in m_cachedGameInstance.Players)
			{
				m_slots[num] = player;
				player.SlotID = (byte)num;
				MovePlayerToColliderGroup("", player);
				num++;
			}
		}

		public void PlayerSpawned(NetworkPlayer player)
		{
			FlushCollideMap(player);
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
					if (m_collisionMapHolder.GetCollideState(i, player.SlotID, out var suspensionTime) == NetGameCollisionManager.CollideState.Off)
					{
						break;
					}
					m_collisionMapHolder.ClearState(i, player.SlotID);
					m_collisionMapHolder.SetCollisionState(i, player.SlotID, newState: true, suspensionTime);
				}
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
				value = ((m_listPool.Count > 0) ? m_listPool.Pop() : new List<NetworkPlayer>());
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
				foreach (NetworkPlayer item in collideGroup.Value)
				{
					bool state = collideGroup.Key == playerToMove.collideGroup;
					float timer = (string.IsNullOrEmpty(playerToMove.collideGroup) ? 15f : 5f);
					SetCollideState(playerToMove, item, state, timer);
				}
			}
			foreach (string item2 in m_groupsToRemoveBuffer)
			{
				List<NetworkPlayer> list = m_collideGroups[item2];
				list.Clear();
				m_listPool.Push(list);
				m_collideGroups.Remove(item2);
			}
			m_groupsToRemoveBuffer.Clear();
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
				m_listPool.Push(list);
				m_collideGroups.Remove(item);
			}
			m_groupsToRemoveBuffer.Clear();
			if (m_slots[player.SlotID] == player)
			{
				m_slots[player.SlotID] = null;
			}
		}

		public void FixedUpdate()
		{
		}

		public void Update()
		{
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
			byte slotID = player.SlotID;
			for (int i = 0; i < m_slots.Length; i++)
			{
				NetworkPlayer networkPlayer = m_slots[i];
				if (slotID != i && networkPlayer != null)
				{
					bool newState = player.collideGroup == networkPlayer.collideGroup;
					float suspendAfterChangeTime = (string.IsNullOrEmpty(player.collideGroup) ? 15f : 5f);
					m_collisionMapHolder.SetCollisionState(slotID, i, newState, suspendAfterChangeTime);
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

		public NetGameCollisionManager.CollideState GetColliderState(NetworkPlayer player, out float time)
		{
			if (!m_enabled || player == null)
			{
				time = 0f;
				return NetGameCollisionManager.CollideState.On;
			}
			byte slotID = m_cachedGameInstance.LocalPlayer.SlotID;
			byte slotID2 = player.SlotID;
			return m_collisionMapHolder.GetCollideState(slotID, slotID2, out time);
		}
	}
}
