namespace Services.UGC.Interfaces
{
	public interface IUGCReportable
	{
		bool isReported { get; }

		void Report();
	}
}
