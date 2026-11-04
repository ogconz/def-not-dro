using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Services.UGC.Interfaces;

namespace Services.UGC.Platforms
{
	public abstract class UGCService<T> : IUGCService where T : IUGCManager
	{
		protected List<T> m_managers = new List<T>();

		protected bool m_isInitialized;

		protected IUGCManagersFactoty<T> m_managersFactory;

		public abstract IUGCAuthentication authentication { get; }

		public abstract IUGCStorageManager storage { get; }

		public UGCService(IUGCManagersFactoty<T> managersFactory)
		{
			m_managersFactory = managersFactory;
			m_managers.AddRange(m_managersFactory.CreateManagers());
		}

		public abstract UniTask<bool> Initialize();

		public T GetServiceByType(IUGCManager.ContentType contentType)
		{
			return m_managers.Find((T m) => m.contentType == contentType);
		}

		public bool IsInstalled(ulong id)
		{
			foreach (T manager in m_managers)
			{
				if (manager.IsPackageInstalled(id))
				{
					return true;
				}
			}
			return false;
		}

		public ulong GetRequestedQueueItemsTotalSizeBytes()
		{
			ulong num = 0uL;
			foreach (T manager in m_managers)
			{
				IUGCManager iUGCManager = manager;
				num += iUGCManager.GetRequestedQueueItemsTotalSizeBytes();
			}
			return num;
		}

		public abstract void Update();
	}
}
