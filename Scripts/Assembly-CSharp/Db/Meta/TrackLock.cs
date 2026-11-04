namespace DB.Meta
{
	public class TrackLock : IDBMeta
	{
		public int id { get; set; }

		public int trackId { get; set; }

		public bool locked { get; set; }
	}
}
