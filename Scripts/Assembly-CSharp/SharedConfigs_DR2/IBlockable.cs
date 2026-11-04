namespace SharedConfigs_DR2
{
	public interface IBlockable
	{
		int id { get; set; }

		Blocker blocker { get; set; }
	}
}
