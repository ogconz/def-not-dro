using System.Collections.Generic;
using Service.UGC.ModIO;
using Services.UGC.Interfaces;

namespace Services.UGC.Builders
{
	public class ModIOManagersFactory : IUGCManagersFactoty<ModIOManager>
	{
		public List<ModIOManager> CreateManagers()
		{
			return new List<ModIOManager>
			{
				new ModIOMapManager(),
				new ModIOCarManager()
			};
		}
	}
}
