using System.IO;
using UnityEngine;

namespace SyncMultiplayer.Objects
{
	public class ObjectTransformSyncModel : BaseObjectSyncModel
	{
		public Transform transform;

		public override bool Init(NetworkObject netObject)
		{
			transform = netObject.gameObject.transform;
			return true;
		}

		public override void WritePackage(MemoryStream stream, SyncPacketType type)
		{
			if (type.IsId() || type.IsInit())
			{
				CustomTypes.SerializeVector3(transform.position, stream);
				CustomTypes.SerializeQuaternion(transform.rotation, stream);
				CustomTypes.SerializeVector3(transform.localScale, stream);
			}
		}

		public override void ReadPackage(byte[] bytes, ref int offset, SyncPacketType type)
		{
			if (type.IsId() || type.IsInit())
			{
				transform.position = CustomTypes.DeserializeVector3(bytes, offset);
				offset += 12;
				transform.rotation = CustomTypes.DeserializeQuaternion(bytes, offset);
				offset += 16;
				transform.localScale = CustomTypes.DeserializeVector3(bytes, offset);
				offset += 12;
			}
		}
	}
}
