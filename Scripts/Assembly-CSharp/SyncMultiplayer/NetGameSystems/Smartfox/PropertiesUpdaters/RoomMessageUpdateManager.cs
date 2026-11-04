using System;
using System.Collections.Generic;
using Sfs2X.Entities;
using SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Batching;
using SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Dispatchers;
using SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Dispatchers.Settings;
using SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Dispatchers.Settings.Debug;
using SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Obtainers;

namespace SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters
{
	public class RoomMessageUpdateManager : IDisposable
	{
		protected Dictionary<PacketId, PackageUpdateDispatcher<SFSUser, SmartfoxDataPackage>> m_dispatchers = new Dictionary<PacketId, PackageUpdateDispatcher<SFSUser, SmartfoxDataPackage>>();

		private RoomPackageApplier m_packageApplier;

		private RoomPackageThreadScheduleSettings m_scheduleSettings;

		public void Init(MasterPacketsHandlerSystem masterPacketsHandlerSystem)
		{
			m_packageApplier = new RoomPackageApplier();
			m_packageApplier.Init(masterPacketsHandlerSystem);
			InitSettings();
		}

		public void Process(SFSUser user, SmartfoxDataPackage dataPackage)
		{
			if (!m_dispatchers.TryGetValue(dataPackage.PacketIdValue, out var value))
			{
				value = CreateDefaultDispatcher(dataPackage.PacketIdValue);
			}
			value.Process(user, dataPackage);
		}

		public void Dispose()
		{
			foreach (KeyValuePair<PacketId, PackageUpdateDispatcher<SFSUser, SmartfoxDataPackage>> dispatcher in m_dispatchers)
			{
				dispatcher.Value.Dispose();
			}
			m_dispatchers?.Clear();
			m_scheduleSettings = null;
			m_packageApplier = null;
		}

		private PackageUpdateDispatcher<SFSUser, SmartfoxDataPackage> CreateDefaultDispatcher(PacketId packetId)
		{
			PackageUpdateDispatcher<SFSUser, SmartfoxDataPackage> packageUpdateDispatcher = packetId switch
			{
				PacketId.Subroom => Create<SmartfoxPackageHolder>(packetId, usingThreadPool: false), 
				PacketId.Notify => Create<NotifyPackageHolder>(packetId), 
				_ => Create<SmartfoxPackageHolder>(packetId), 
			};
			return m_dispatchers[packetId] = packageUpdateDispatcher;
		}

		private PackageUpdateDispatcher<SFSUser, SmartfoxDataPackage> Create<T>(PacketId packetId, bool usingThreadPool = true) where T : SmartfoxPackageHolder, new()
		{
			PackageUpdateDispatcher<SFSUser, SmartfoxDataPackage> packageUpdateDispatcher;
			if (usingThreadPool)
			{
				MultiThreadPackageBatcherUpdateDispatcher<SFSUser, T, SmartfoxDataPackage> multiThreadPackageBatcherUpdateDispatcher = new MultiThreadPackageBatcherUpdateDispatcher<SFSUser, T, SmartfoxDataPackage>();
				multiThreadPackageBatcherUpdateDispatcher.SetDelay(m_scheduleSettings.GetDelay(packetId));
				packageUpdateDispatcher = multiThreadPackageBatcherUpdateDispatcher;
			}
			else
			{
				packageUpdateDispatcher = new PackageUpdateDispatcher<SFSUser, SmartfoxDataPackage>();
			}
			packageUpdateDispatcher.Register(m_packageApplier);
			packageUpdateDispatcher.Init();
			return packageUpdateDispatcher;
		}

		private void InitSettings()
		{
			m_scheduleSettings = InitDebugSettings();
		}

		private RoomPackageThreadScheduleSettings InitDebugSettings()
		{
			DebugRoomPackageScheduleSettings debugRoomPackageScheduleSettings = new DebugRoomPackageScheduleSettings();
			debugRoomPackageScheduleSettings.OnCommandApplied = (Action<PacketId>)Delegate.Remove(debugRoomPackageScheduleSettings.OnCommandApplied, new Action<PacketId>(OnApplyCommandHandler));
			debugRoomPackageScheduleSettings.OnCommandApplied = (Action<PacketId>)Delegate.Combine(debugRoomPackageScheduleSettings.OnCommandApplied, new Action<PacketId>(OnApplyCommandHandler));
			return debugRoomPackageScheduleSettings;
		}

		private void OnApplyCommandHandler(PacketId packetId)
		{
			CreateDefaultDispatcher(packetId);
		}
	}
}
