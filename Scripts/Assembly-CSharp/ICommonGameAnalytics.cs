using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public interface ICommonGameAnalytics
{
	UniTask InitAsync(string currentUserId);

	void SendEvent(string eventName);

	void SendEvent(string eventName, Dictionary<string, object> parameters);
}
