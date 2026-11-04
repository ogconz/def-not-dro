using System.Collections.Generic;
using Sfs2X.Entities.Data;
using Sfs2X.Util;
using UnityEngine;

namespace SyncMultiplayer
{
	public struct SmartfoxDataPackage
	{
		private const string PACKET_ID_KEY = "0";

		private ISFSObject m_data;

		public PacketId PacketIdValue => (PacketId)(m_data?.GetByte("0")).Value;

		public ISFSObject Data => m_data;

		public SmartfoxDataPackage(PacketId packetId)
		{
			m_data = new SFSObject();
			m_data.PutByte("0", (byte)packetId);
		}

		public SmartfoxDataPackage(ISFSObject data)
		{
			m_data = data;
		}

		public ISFSObject ToDataPackage()
		{
			return m_data;
		}

		public void Add(string key, byte value)
		{
			m_data.PutByte(key, value);
		}

		public Vector3 GetVector3(string key)
		{
			return CustomTypes.DeserializeVector3(m_data.GetByteArray(key).Bytes);
		}

		public Quaternion GetQuaternion(string key)
		{
			return CustomTypes.DeserializeQuaternion(m_data.GetByteArray(key).Bytes);
		}

		public void Add(string key, byte[] value)
		{
			m_data.PutByteArray(key, new ByteArray(value));
		}

		public void Add(string key, int value)
		{
			m_data.PutInt(key, value);
		}

		public void Add(string key, string value)
		{
			m_data.PutText(key, value);
		}

		public void Add(string key, ulong value)
		{
			m_data.PutLong(key, (long)value);
		}

		public void Add(string key, float value)
		{
			m_data.PutFloat(key, value);
		}

		public void Add(string key, bool value)
		{
			m_data.PutBool(key, value);
		}

		public void AddLongArray(string key, List<ulong> positions)
		{
			m_data.PutLongArray(key, positions.Map((ulong val) => (long)val).ToArray());
		}

		public void AddSFSArray(string key, SFSArray array)
		{
			m_data.PutSFSArray(key, array);
		}
	}
}
