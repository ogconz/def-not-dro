using Epic.OnlineServices.UserInfo;
using GameOverlay.Regions;

namespace GameOverlay.EpicGames
{
	public class EOSRegionDataManager : IRegionDetection<RegionData>
	{
		private RegionData m_regionData;

		private UserInfoData m_userInfo;

		public EOSRegionDataManager(UserInfoData userInfo)
		{
			m_userInfo = userInfo;
		}

		public RegionData GetRegionData()
		{
			return m_regionData ?? (m_regionData = CreateInternalRegionData());
		}

		private RegionData CreateInternalRegionData()
		{
			var (country, region) = CountryCodeMappings.GetCountryAndRegion(m_userInfo.Country.ToString().ToUpper()) ?? (Country.Unknown, Region.Europe);
			return new RegionData(country, region);
		}
	}
}
