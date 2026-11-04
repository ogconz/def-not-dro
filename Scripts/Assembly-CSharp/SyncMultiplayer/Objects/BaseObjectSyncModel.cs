using System.IO;

namespace SyncMultiplayer.Objects
{
	public abstract class BaseObjectSyncModel
	{
		public abstract bool Init(NetworkObject netObject);

		public virtual void Attach(NetGameSystemsManager systems)
		{
		}

		public virtual void Update()
		{
		}

		public virtual bool Synced()
		{
			return true;
		}

		public abstract void ReadPackage(byte[] bytes, ref int offset, SyncPacketType type);

		public abstract void WritePackage(MemoryStream stream, SyncPacketType type);
	}
}
