namespace DB.Meta
{
	public interface IRaceNorm
	{
		RaceCupType GetCupByValue(float value);

		float GetNormByCup(RaceCupType cupType);

		string GetNormAsString(RaceCupType cupType);
	}
}
