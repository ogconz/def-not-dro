using GameOverlay.Regions;

public class XboxRegionDetection : IRegionDetection<RegionData>
{
	private RegionData m_regionData;

	public RegionData GetRegionData()
	{
		if (m_regionData != null)
		{
			return m_regionData;
		}
		m_regionData = new RegionData(Country.Unknown, Region.Europe);
		return m_regionData;
	}

	private Region ConvertToRegion(string marketPlaceId)
	{
		switch (marketPlaceId.ToLower())
		{
		case "ja-jp":
			return Region.Japan;
		case "zh-tw":
		case "en-sg":
		case "zh-sg":
		case "en-hk":
		case "zh-hk":
		case "ko-kr":
		case "en-in":
		case "zh-cn":
		case "en-au":
			return Region.Asia;
		case "en-us":
		case "en-ca":
		case "en-fr":
		case "es-mx":
		case "pt-br":
		case "es-ar":
		case "es-cl":
		case "es-co":
			return Region.America;
		default:
			return Region.Europe;
		}
	}
}
