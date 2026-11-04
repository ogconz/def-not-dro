public interface IAnalyticsEventFilter
{
	bool IsAllowed(string eventName);
}
