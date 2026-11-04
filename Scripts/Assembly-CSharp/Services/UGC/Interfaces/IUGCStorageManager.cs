namespace Services.UGC.Interfaces
{
	public interface IUGCStorageManager
	{
		bool IsReady();

		bool IsEnoughSpaceToInstall(IUGCMeta meta);

		ulong GetSizeBytes();

		ulong GetAvailableSizeBytes();
	}
}
