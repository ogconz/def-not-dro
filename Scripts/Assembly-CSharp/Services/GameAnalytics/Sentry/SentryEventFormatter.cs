using System.Collections.Generic;

namespace Services.GameAnalytics.Sentry
{
	public class SentryEventFormatter
	{
		private Dictionary<string, string> m_cache = new Dictionary<string, string>
		{
			["SfsPingTcp"] = "ping/tcp",
			["SfsPingUdp"] = "ping/udp"
		};

		public string Format(string eventName)
		{
			return m_cache.GetValueOrDefault(eventName, eventName);
		}
	}
}
