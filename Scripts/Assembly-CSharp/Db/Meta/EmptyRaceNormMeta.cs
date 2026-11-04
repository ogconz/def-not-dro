namespace DB.Meta
{
	public class EmptyRaceNormMeta : IRaceNorm
	{
		public RaceCupType GetCupByValue(float value)
		{
			return RaceCupType.None;
		}

		public float GetNormByCup(RaceCupType cupType)
		{
			return 0f;
		}

		public string GetNormAsString(RaceCupType cupType)
		{
			return string.Empty;
		}
	}
}
