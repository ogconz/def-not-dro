using Cysharp.Threading.Tasks;
using Services.UGC.Interfaces;

namespace Services.UGC.Platforms
{
	public interface IUGCService
	{
		IUGCAuthentication authentication { get; }

		IUGCStorageManager storage { get; }

		UniTask<bool> Initialize();

		void Update();

		bool IsInstalled(ulong id);
	}
}
