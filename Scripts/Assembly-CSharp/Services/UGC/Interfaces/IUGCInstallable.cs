namespace Services.UGC.Interfaces
{
	public interface IUGCInstallable
	{
		bool isInstalled { get; }

		string directory { get; }

		bool installFromScratch { get; }
	}
}
