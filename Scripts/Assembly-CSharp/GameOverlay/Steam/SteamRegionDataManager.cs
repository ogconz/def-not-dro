using System;
using GameOverlay.Regions;
using Steamworks;

namespace GameOverlay.Steam
{
	public class SteamRegionDataManager : IRegionDetection<RegionData>, IDisposable
	{
		private RegionData m_regionData;

		public SteamRegionDataManager()
		{
			SteamUtils.OnIpCountryChanged += OnCountryChangedHandler;
		}

		public RegionData GetRegionData()
		{
			return m_regionData ?? (m_regionData = CreateInternalRegionData());
		}

		private void OnCountryChangedHandler()
		{
			m_regionData = CreateInternalRegionData();
		}

		private RegionData CreateInternalRegionData()
		{
			var (country, region) = CountryCodeMappings.GetCountryAndRegion(SteamUtils.IpCountry.ToUpper()) ?? (Country.Unknown, Region.Europe);
			return new RegionData(country, region);
		}

		public void Dispose()
		{
			SteamUtils.OnIpCountryChanged -= OnCountryChangedHandler;
		}
	}
}
