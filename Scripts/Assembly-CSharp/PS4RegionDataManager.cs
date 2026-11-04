using System;
using GameOverlay.Regions;

public class PS4RegionDataManager : IRegionDetection<RegionData>
{
	private RegionData m_region;

	public RegionData GetRegionData()
	{
		if (m_region != null)
		{
			return m_region;
		}
		int num = 0;
		if (Enum.IsDefined(typeof(Region), num))
		{
			m_region = new RegionData(Country.Unknown, (Region)num);
		}
		else
		{
			m_region = new RegionData(Country.Unknown, Region.Europe);
		}
		return m_region;
	}

	public void SetRegionByCountry(string countryCode)
	{
		Country? country = CountryCodeMappings.GetCountry(countryCode);
		Region? region = CountryCodeMappings.GetRegion(countryCode);
		if (country.HasValue && region.HasValue)
		{
			m_region = new RegionData(country.Value, region.Value);
		}
	}
}
