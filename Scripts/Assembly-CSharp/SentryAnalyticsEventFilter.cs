using System.Collections.Generic;

public class SentryAnalyticsEventFilter : IAnalyticsEventFilter
{
	private HashSet<string> m_whiteList = new HashSet<string> { "SfsPingTcp", "SfsPingUdp", "LoadPrefsError" };

	public bool IsAllowed(string eventName)
	{
		return m_whiteList.Contains(eventName);
	}
}
