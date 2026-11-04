using Services.UGC.Interfaces;

namespace Services.UGC.Storage
{
	public class VoidUGCStorageManager : IUGCStorageManager
	{
		public bool IsReady()
		{
			return false;
		}

		public ulong GetAvailableSizeBytes()
		{
			return ulong.MaxValue;
		}

		public ulong GetSizeBytes()
		{
			return ulong.MaxValue;
		}

		public bool IsEnoughSpaceToInstall(IUGCMeta meta)
		{
			return true;
		}
	}
}
