using System.Collections.Generic;

namespace Services.UGC.Interfaces
{
	public interface IUGCManagersFactoty<T> where T : IUGCManager
	{
		List<T> CreateManagers();
	}
}
