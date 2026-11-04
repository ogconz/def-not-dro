using System.Collections.Generic;
using GameOverlay;
using Services.UGC.Interfaces;
using Services.UGC.Platforms;

namespace Services.UGC.Utils
{
	public class UGCServiceProvider
	{
		private static UGCServiceProvider m_instance;

		private List<IUGCService> m_services = new List<IUGCService>();

		public static UGCServiceProvider instance => m_instance ?? (m_instance = new UGCServiceProvider());

		private bool isReady => m_instance != null;

		private UGCServiceProvider()
		{
			m_instance = this;
		}

		private void RegisterUpdate()
		{
			TickManager.RegisterUpdate(Update, 50, "UGCServiceProvider");
		}

		public void Initialize()
		{
			Build();
			foreach (IUGCService service in m_services)
			{
				service.Initialize();
			}
			RegisterUpdate();
		}

		private void Build()
		{
			m_services.AddRange(new List<IUGCService>
			{
				new ModIOService(),
				new SteamWorkshopService()
			});
		}

		public bool IsContentInstalled(ulong contentId)
		{
			foreach (IUGCService service in m_services)
			{
				if (service.IsInstalled(contentId))
				{
					return true;
				}
			}
			return false;
		}

		public IUGCManager GetManagerBy(IUGCManager.ContentType contentType, IUGCManager.ServiceProvider serviceProvider)
		{
			return serviceProvider switch
			{
				IUGCManager.ServiceProvider.Steamworkshop => GetServiceBy<SteamWorkshopService>().GetServiceByType(contentType), 
				IUGCManager.ServiceProvider.ModIO => GetServiceBy<ModIOService>().GetServiceByType(contentType), 
				_ => new VoidUGCManager(), 
			};
		}

		public IUGCService GetServiceBy(IUGCManager.ServiceProvider serviceProvider)
		{
			return serviceProvider switch
			{
				IUGCManager.ServiceProvider.Steamworkshop => GetServiceBy<SteamWorkshopService>(), 
				IUGCManager.ServiceProvider.ModIO => GetServiceBy<ModIOService>(), 
				_ => null, 
			};
		}

		public T GetServiceBy<T>() where T : IUGCService
		{
			return (T)m_services.Find((IUGCService service) => service is T);
		}

		private void Update()
		{
			if (!isReady)
			{
				return;
			}
			foreach (IUGCService service in m_services)
			{
				service.Update();
			}
		}
	}
}
