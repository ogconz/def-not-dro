using System.Collections.Generic;
using Services.UGC.Interfaces;
using Services.UGC.Steamworks;

namespace Services.UGC.Builders
{
	public class SteamWorkshopManagersFactory : IUGCManagersFactoty<SteamWorkshopManager>
	{
		public List<SteamWorkshopManager> CreateManagers()
		{
			return new List<SteamWorkshopManager>
			{
				new SteamWorkshopMapManager(),
				new SteamWorkshopCarManager()
			};
		}
	}
}
