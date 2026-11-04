using System.Collections.Generic;

public class PlayFabAnalyticsEventFilter : IAnalyticsEventFilter
{
	private HashSet<string> m_blackList = new HashSet<string> { "SfsPingTcp", "SfsPingUdp", "LoadPrefsError" };

	public bool IsAllowed(string eventName)
	{
		return !m_blackList.Contains(eventName);
	}
}
