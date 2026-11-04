using System.Collections.Generic;

namespace Services.GameAnalytics
{
	public interface IEventHandler
	{
		void Process(string eventName, Dictionary<string, object> parameters);
	}
}
