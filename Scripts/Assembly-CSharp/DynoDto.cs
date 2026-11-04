using DB.Meta;

public struct DynoDto
{
	public int carId { get; set; }

	public int profileId { get; set; }

	public ProfilesInfo.Profile preview { get; set; }

	public ProfilesInfo.Profile cached { get; set; }

	public PlayerCarProfile currentDbData { get; set; }

	public PlayerCarProfile defaultDbData { get; set; }
}
