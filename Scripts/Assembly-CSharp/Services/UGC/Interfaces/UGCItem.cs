namespace Services.UGC.Interfaces
{
	public abstract class UGCItem
	{
		public abstract ulong id { get; }

		public abstract string title { get; }

		public abstract IUGCManager.ServiceProvider serviceId { get; }

		public abstract IUGCManager.ContentType contentType { get; }

		public abstract IUGCManager manager { get; }
	}
}
