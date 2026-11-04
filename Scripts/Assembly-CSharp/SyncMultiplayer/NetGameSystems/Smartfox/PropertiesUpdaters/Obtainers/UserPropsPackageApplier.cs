using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sfs2X.Entities;
using SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Interfaces;

namespace SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Obtainers
{
	public class UserPropsPackageApplier : IPackageApplier<SFSUser, HashSet<string>>
	{
		private NetGameNotifySystem m_notifySystem;

		public UserPropsPackageApplier(NetGameNotifySystem notifySystem)
		{
			m_notifySystem = notifySystem;
		}

		public void Apply(SFSUser user, HashSet<string> changes)
		{
			NetworkPlayer netPlayer = NetworkController.InstanceGame.FindPlayerByNetworkId(user.Id);
			HashSet<string> changedValue;
			if (netPlayer != null)
			{
				changedValue = changes;
				netPlayer.PlayerProperties.SetFromUser(user, changedValue, OnPropsChanged);
			}
			void OnPropsChanged()
			{
				UniTask.Post(delegate
				{
					m_notifySystem?.HandleProps(netPlayer, changedValue);
				});
			}
		}
	}
}
