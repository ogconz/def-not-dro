using Cysharp.Threading.Tasks;

namespace Services.UGC.Interfaces
{
	public interface IUGCRatable
	{
		float rate { get; }

		bool supportSetRate { get; }

		UniTask<bool> SetRateAsync(int value);
	}
}
