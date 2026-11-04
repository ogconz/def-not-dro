using Cysharp.Threading.Tasks;
using Sfs2X.Entities;
using SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Interfaces;

namespace SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Obtainers
{
	public class RoomPackageApplier : IPackageApplier<SFSUser, SmartfoxDataPackage>
	{
		private MasterPacketsHandlerSystem m_packetsHandlerSystem;

		public void Init(MasterPacketsHandlerSystem packetsHandlerSystem)
		{
			m_packetsHandlerSystem = packetsHandlerSystem;
		}

		public void Apply(SFSUser user, SmartfoxDataPackage changes)
		{
			NetworkPlayer netPlayer = NetworkController.InstanceGame.FindPlayerByNetworkId(user.Id);
			if (netPlayer != null)
			{
				UniTask.Post(delegate
				{
					m_packetsHandlerSystem?.Handle(netPlayer, changes);
				});
			}
		}
	}
}
