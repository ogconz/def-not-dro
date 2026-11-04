using System.Collections.Generic;
using SyncMultiplayer.Objects;
using UnityEngine;

namespace SyncMultiplayer
{
	public class NetGameSyncObjectSystem : BaseNetGameSystem
	{
		public const string NETWORK_OBJECT_TAG = "NetworkObject";

		private const string PACKET_TYPE = "p";

		private const string OBJ_DATA = "d";

		private const string OBJ_ID = "i";

		private NetworkGame m_game;

		private Dictionary<int, NetworkObject> m_objects = new Dictionary<int, NetworkObject>();

		private Queue<SmartfoxDataPackage> m_updateQueue = new Queue<SmartfoxDataPackage>();

		private ulong m_lastTrackID;

		public override void Attach(NetGameSystemsManager owner)
		{
			base.Attach(owner);
			m_game = owner.host;
			m_game.packetHandler.Subscribe(PacketId.Object, OnObjectDataReceived);
		}

		public override void OnRaceLoaded()
		{
			base.OnRaceLoaded();
			FetchObjects();
		}

		public override void OnReconnect()
		{
			base.OnReconnect();
			if (!IsSameTrack())
			{
				FetchObjects();
			}
		}

		public override void DoPlayerEnter(NetworkPlayer player)
		{
			base.DoPlayerEnter(player);
			SendOriginalObjectsAsync(player);
		}

		public override void Update()
		{
			UpdateQueueProcess();
			UpdateLocalObjects();
		}

		private void UpdateObjectData(SmartfoxDataPackage data)
		{
			ReadPackage(data, out var type, out var payload, out var objId);
			if (m_objects.TryGetValue(objId, out var value))
			{
				value.ProcessPackage(payload, type);
			}
		}

		private void UpdateLocalObjects()
		{
			foreach (NetworkObject value in m_objects.Values)
			{
				value.Update();
				if (value.NeedSync())
				{
					SendObject(value, SyncPacketType.State);
				}
			}
		}

		private bool IsSameTrack()
		{
			if (m_game.CurrentRoomOptions == null)
			{
				return false;
			}
			ulong num = m_game.CurrentRoomOptions.ExternalTrackId;
			if (num == 0L)
			{
				num = (ulong)m_game.CurrentRoomOptions.TrackID;
			}
			bool result = m_lastTrackID == num;
			m_lastTrackID = num;
			return result;
		}

		private void FetchObjects()
		{
			m_objects.Clear();
			GameObject[] array = GameObject.FindGameObjectsWithTag("NetworkObject");
			if (array != null)
			{
				GameObject[] array2 = array;
				foreach (GameObject obj in array2)
				{
					CreateNetworkObject(obj);
				}
			}
		}

		private void CreateNetworkObject(GameObject obj)
		{
			NetworkObject networkObject = new NetworkObjectBuilder(obj, base.owner).AddSync<ObjectTransformSyncModel>().AddSync<ObjectAnimatorSyncModel>().AddSync<ObjectCountdownSyncModel>()
				.Build();
			m_objects[networkObject.id] = networkObject;
		}

		private void SendOriginalObjectsAsync(NetworkPlayer player)
		{
			if (m_game.LocalPlayer.IsMasterClient)
			{
				SendObjects(SyncPacketType.Init, player.NetworkID);
			}
		}

		private void OnObjectDataReceived(NetworkPlayer sender, SmartfoxDataPackage data)
		{
			if (m_objects.Count != 0 || sender.IsMasterClient)
			{
				m_updateQueue.Enqueue(data);
			}
		}

		private void UpdateQueueProcess()
		{
			if (!(m_game.LocalPlayer.userCar == null))
			{
				while (m_updateQueue.Count > 0)
				{
					UpdateObjectData(m_updateQueue.Dequeue());
				}
			}
		}

		private void SendObjects(SyncPacketType type, params int[] receivers)
		{
			foreach (NetworkObject value in m_objects.Values)
			{
				SendObject(value, type, receivers);
			}
		}

		private void SendObject(NetworkObject obj, SyncPacketType type, params int[] receivers)
		{
			if (!(obj.gameObject == null))
			{
				bool reliable = IsReliable(type);
				SmartfoxDataPackage data = MakePackage(obj, type);
				if (receivers == null || receivers.Length < 1)
				{
					base.owner.host.SendOthers(data, reliable);
				}
				else
				{
					base.owner.host.SendOthers(data, reliable, receivers);
				}
			}
		}

		private bool IsReliable(SyncPacketType type)
		{
			if (!type.IsId())
			{
				return type.IsInit();
			}
			return true;
		}

		private SmartfoxDataPackage MakePackage(NetworkObject obj, SyncPacketType type)
		{
			SmartfoxDataPackage result = new SmartfoxDataPackage(PacketId.Object);
			result.Add("p", (byte)type);
			result.Add("i", obj.id);
			result.Add("d", obj.SerializeNetworkObject(type));
			return result;
		}

		private void ReadPackage(SmartfoxDataPackage data, out SyncPacketType type, out byte[] payload, out int objId)
		{
			type = (SyncPacketType)data.Data.GetByte("p");
			payload = data.Data.GetByteArray("d").Bytes;
			objId = data.Data.GetInt("i");
		}
	}
}
