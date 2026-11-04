using System;
using Cysharp.Threading.Tasks;
using Services.UGC.Builders;
using Services.UGC.Interfaces;
using Services.UGC.Steamworks;
using Services.UGC.Storage;

namespace Services.UGC.Platforms
{
	public class SteamWorkshopService : UGCService<SteamWorkshopManager>
	{
		private IUGCAuthentication m_authentication = new VoidUGCManagerAuthentication();

		private IUGCStorageManager m_storage = new VoidUGCStorageManager();

		public override IUGCAuthentication authentication => m_authentication;

		public override IUGCStorageManager storage => m_storage;

		public SteamWorkshopService()
			: base((IUGCManagersFactoty<SteamWorkshopManager>)new SteamWorkshopManagersFactory())
		{
		}

		public override async UniTask<bool> Initialize()
		{
			foreach (SteamWorkshopManager manager in m_managers)
			{
				manager.Initialize(this);
				await manager.QueryFavoriteItems();
			}
			await DownloadAllSubscribedPackages();
			return true;
		}

		public override void Update()
		{
			foreach (SteamWorkshopManager manager in m_managers)
			{
				manager.Update();
			}
		}

		public async UniTask DownloadAllSubscribedPackages(Action<float, string> progress = null, Action onFinished = null)
		{
			foreach (SteamWorkshopManager manager in m_managers)
			{
				manager.DownloadAllSubscribedPackages();
			}
			onFinished.SafeCall();
		}
	}
}
