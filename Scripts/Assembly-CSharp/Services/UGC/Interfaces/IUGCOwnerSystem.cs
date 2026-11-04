using Cysharp.Threading.Tasks;
using GameOverlay;

namespace Services.UGC.Interfaces
{
	public interface IUGCOwnerSystem
	{
		public enum RequestInfo
		{
			Name = 0
		}

		Overlay.UserInfo owner { get; }

		bool IsValidUserName();

		UniTask<bool> RequestInfoFromServices(RequestInfo info);
	}
}
