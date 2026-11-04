using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class FilteredAnalyticsService : ICommonGameAnalytics
{
	private ICommonGameAnalytics m_commonGameAnalytics;

	private IAnalyticsEventFilter m_analyticsEventFilter;

	public FilteredAnalyticsService(ICommonGameAnalytics analytics, IAnalyticsEventFilter eventFilter)
	{
		m_commonGameAnalytics = analytics;
		m_analyticsEventFilter = eventFilter;
	}

	public UniTask InitAsync(string currentUserId)
	{
		return m_commonGameAnalytics.InitAsync(currentUserId);
	}

	public void SendEvent(string eventName)
	{
		if (m_analyticsEventFilter.IsAllowed(eventName))
		{
			m_commonGameAnalytics.SendEvent(eventName);
		}
	}

	public void SendEvent(string eventName, Dictionary<string, object> parameters)
	{
		if (m_analyticsEventFilter.IsAllowed(eventName))
		{
			m_commonGameAnalytics.SendEvent(eventName, parameters);
		}
	}
}
