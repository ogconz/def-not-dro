namespace Services.UGC.Interfaces
{
	public interface IUGCDownloadable
	{
		bool isDownloading { get; }

		bool isDownloaded { get; }
	}
}
