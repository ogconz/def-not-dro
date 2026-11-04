namespace DB.Meta
{
	public class PlayerCarProfileLock : IDBMeta
	{
		public int id { get; set; }

		public int profileId { get; set; }

		public bool locked { get; set; }
	}
}
