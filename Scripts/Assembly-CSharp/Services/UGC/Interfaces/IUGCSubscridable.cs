using Cysharp.Threading.Tasks;

namespace Services.UGC.Interfaces
{
	public interface IUGCSubscridable
	{
		bool isSubscribed { get; }

		ulong subscriptionsCount { get; }

		UniTask<bool> SetSubscribed(bool value);
	}
}
