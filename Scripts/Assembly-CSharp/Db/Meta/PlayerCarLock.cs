namespace DB.Meta
{
	public class PlayerCarLock : IDBMeta
	{
		public int id { get; set; }

		public int carId { get; set; }

		public bool locked { get; set; }
	}
}
