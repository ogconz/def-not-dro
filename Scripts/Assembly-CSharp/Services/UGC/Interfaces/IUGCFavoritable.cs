using Cysharp.Threading.Tasks;

namespace Services.UGC.Interfaces
{
	public interface IUGCFavoritable
	{
		bool isFavorite { get; }

		UniTask<bool> SetFavorite(bool value);
	}
}
