using System;
using System.Collections.Generic;
using UnityEngine;

namespace SyncMultiplayer
{
	public class MasterPacketsHandlerSystem
	{
		private readonly Dictionary<PacketId, List<PacketHandleDelegate>> m_Handlers;

		private static MasterPacketsHandlerSystem m_Instance;

		public static MasterPacketsHandlerSystem GetInstance()
		{
			if (m_Instance != null)
			{
				return m_Instance;
			}
			return m_Instance = new MasterPacketsHandlerSystem();
		}

		private MasterPacketsHandlerSystem()
		{
			m_Handlers = new Dictionary<PacketId, List<PacketHandleDelegate>>();
			foreach (Type interfacedType in DynamicTypesManager.GetInterfacedTypes<ISingleMasterPacketAutoHandler>())
			{
				object obj = Activator.CreateInstance(interfacedType);
				if (obj != null)
				{
					ISingleMasterPacketAutoHandler singleMasterPacketAutoHandler = obj as ISingleMasterPacketAutoHandler;
					Subscribe(singleMasterPacketAutoHandler.OpCode, singleMasterPacketAutoHandler.Handle);
				}
				else
				{
					Debug.LogError("NetworkHandlers: No handler for " + interfacedType.FullName);
				}
			}
		}

		public void Handle(NetworkPlayer sender, SmartfoxDataPackage data)
		{
			if (data.Data.Size() == 0)
			{
				return;
			}
			List<PacketHandleDelegate> value = null;
			if (!m_Handlers.TryGetValue(data.PacketIdValue, out value))
			{
				return;
			}
			foreach (PacketHandleDelegate item in value)
			{
				item(sender, data);
			}
		}

		public void Subscribe(PacketId id, PacketHandleDelegate handler)
		{
			if (handler != null)
			{
				if (!m_Handlers.TryGetValue(id, out var value))
				{
					value = new List<PacketHandleDelegate>();
					m_Handlers.Add(id, value);
				}
				value.Add(handler);
			}
		}
	}
}
