using System.Collections.Generic;
using UnityEngine;

namespace SyncMultiplayer.Objects
{
	public class NetworkObject
	{
		public int id;

		internal IReadOnlyList<BaseObjectSyncModel> syncModels = new List<BaseObjectSyncModel>();

		internal readonly GameObject gameObject;

		public NetworkObject(GameObject gameObject)
		{
			this.gameObject = gameObject;
		}

		public void Update()
		{
			foreach (BaseObjectSyncModel syncModel in syncModels)
			{
				syncModel.Update();
			}
		}

		public bool NeedSync()
		{
			foreach (BaseObjectSyncModel syncModel in syncModels)
			{
				if (!syncModel.Synced())
				{
					return true;
				}
			}
			return false;
		}

		public void ProcessPackage(byte[] bytes, SyncPacketType type)
		{
			int offset = 0;
			foreach (BaseObjectSyncModel syncModel in syncModels)
			{
				syncModel.ReadPackage(bytes, ref offset, type);
			}
		}
	}
}
