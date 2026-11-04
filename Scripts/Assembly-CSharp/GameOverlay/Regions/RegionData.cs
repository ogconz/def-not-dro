namespace GameOverlay.Regions
{
	public class RegionData
	{
		public Region region { get; private set; }

		public Country country { get; private set; }

		public RegionData(Country country, Region region)
		{
			this.country = country;
			this.region = region;
		}

		public string GetFormattedRegionStr()
		{
			string text = region.ToString();
			if (text.Contains("_"))
			{
				return text.Replace("_", " ");
			}
			return text;
		}
	}
}
