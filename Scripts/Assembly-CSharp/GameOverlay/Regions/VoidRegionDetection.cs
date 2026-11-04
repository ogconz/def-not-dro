namespace GameOverlay.Regions
{
	public class VoidRegionDetection : IRegionDetection<RegionData>
	{
		public RegionData GetRegionData()
		{
			return new RegionData(Country.Unknown, Region.Europe);
		}
	}
}
