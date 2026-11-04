namespace GameOverlay.Regions
{
	public interface IRegionDetection<out T> where T : RegionData
	{
		T GetRegionData();
	}
}
