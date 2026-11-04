using System;
using System.Collections.Generic;

namespace GameOverlay.Regions
{
	public static class CountryCodeMappings
	{
		public static readonly Dictionary<string, (Country country, Region region)> CountryRegionMapping = new Dictionary<string, (Country, Region)>(StringComparer.OrdinalIgnoreCase)
		{
			["RU"] = (Country.Russia, Region.Eastern_Europe),
			["KZ"] = (Country.Kazakhstan, Region.Eastern_Europe),
			["PL"] = (Country.Poland, Region.Europe),
			["CZ"] = (Country.CzechRepublic, Region.Europe),
			["HU"] = (Country.Hungary, Region.Europe),
			["RO"] = (Country.Romania, Region.Europe),
			["BG"] = (Country.Bulgaria, Region.Europe),
			["RS"] = (Country.Serbia, Region.Europe),
			["HR"] = (Country.Croatia, Region.Europe),
			["SK"] = (Country.Slovakia, Region.Europe),
			["SI"] = (Country.Slovenia, Region.Europe),
			["LT"] = (Country.Lithuania, Region.Europe),
			["LV"] = (Country.Latvia, Region.Europe),
			["EE"] = (Country.Estonia, Region.Europe),
			["MD"] = (Country.Moldova, Region.Europe),
			["UA"] = (Country.Ukraine, Region.Europe),
			["BY"] = (Country.Belarus, Region.Europe),
			["DE"] = (Country.Germany, Region.Europe),
			["FR"] = (Country.France, Region.Europe),
			["GB"] = (Country.UnitedKingdom, Region.Europe),
			["IT"] = (Country.Italy, Region.Europe),
			["ES"] = (Country.Spain, Region.Europe),
			["NL"] = (Country.Netherlands, Region.Europe),
			["BE"] = (Country.Belgium, Region.Europe),
			["SE"] = (Country.Sweden, Region.Europe),
			["NO"] = (Country.Norway, Region.Europe),
			["FI"] = (Country.Finland, Region.Europe),
			["DK"] = (Country.Denmark, Region.Europe),
			["CH"] = (Country.Switzerland, Region.Europe),
			["AT"] = (Country.Austria, Region.Europe),
			["IE"] = (Country.Ireland, Region.Europe),
			["PT"] = (Country.Portugal, Region.Europe),
			["GR"] = (Country.Greece, Region.Europe),
			["IS"] = (Country.Iceland, Region.Europe),
			["LU"] = (Country.Luxembourg, Region.Europe),
			["MT"] = (Country.Malta, Region.Europe),
			["CY"] = (Country.Cyprus, Region.Europe),
			["US"] = (Country.USA, Region.America),
			["CA"] = (Country.Canada, Region.America),
			["MX"] = (Country.Mexico, Region.America),
			["BR"] = (Country.Brazil, Region.America),
			["AR"] = (Country.Argentina, Region.America),
			["CL"] = (Country.Chile, Region.America),
			["CO"] = (Country.Colombia, Region.America),
			["PE"] = (Country.Peru, Region.America),
			["VE"] = (Country.Venezuela, Region.America),
			["EC"] = (Country.Ecuador, Region.America),
			["UY"] = (Country.Uruguay, Region.America),
			["PY"] = (Country.Paraguay, Region.America),
			["BO"] = (Country.Bolivia, Region.America),
			["CR"] = (Country.CostaRica, Region.America),
			["PA"] = (Country.Panama, Region.America),
			["DO"] = (Country.DominicanRepublic, Region.America),
			["GT"] = (Country.Guatemala, Region.America),
			["CU"] = (Country.Cuba, Region.America),
			["JP"] = (Country.Japan, Region.Asia),
			["CN"] = (Country.China, Region.Asia),
			["KR"] = (Country.SouthKorea, Region.Asia),
			["IN"] = (Country.India, Region.Asia),
			["ID"] = (Country.Indonesia, Region.Asia),
			["PH"] = (Country.Philippines, Region.Asia),
			["VN"] = (Country.Vietnam, Region.Asia),
			["TH"] = (Country.Thailand, Region.Asia),
			["MY"] = (Country.Malaysia, Region.Asia),
			["SG"] = (Country.Singapore, Region.Asia),
			["TW"] = (Country.Taiwan, Region.Asia),
			["HK"] = (Country.HongKong, Region.Asia),
			["PK"] = (Country.Pakistan, Region.Asia),
			["BD"] = (Country.Bangladesh, Region.Asia),
			["LK"] = (Country.SriLanka, Region.Asia),
			["NP"] = (Country.Nepal, Region.Asia),
			["MM"] = (Country.Myanmar, Region.Asia),
			["KH"] = (Country.Cambodia, Region.Asia),
			["LA"] = (Country.Laos, Region.Asia),
			["MN"] = (Country.Mongolia, Region.Asia),
			["TR"] = (Country.Turkey, Region.Europe),
			["EG"] = (Country.Egypt, Region.Europe),
			["IL"] = (Country.Israel, Region.Europe),
			["LB"] = (Country.Lebanon, Region.Europe),
			["JO"] = (Country.Jordan, Region.Europe),
			["SY"] = (Country.Syria, Region.Europe),
			["IR"] = (Country.Iran, Region.Europe),
			["IQ"] = (Country.Iraq, Region.Europe),
			["SA"] = (Country.SaudiArabia, Region.Europe),
			["AE"] = (Country.UnitedArabEmirates, Region.Asia),
			["QA"] = (Country.Qatar, Region.Asia),
			["KW"] = (Country.Kuwait, Region.Asia),
			["OM"] = (Country.Oman, Region.Asia),
			["BH"] = (Country.Bahrain, Region.Asia),
			["YE"] = (Country.Yemen, Region.Asia),
			["ZA"] = (Country.SouthAfrica, Region.Europe),
			["NG"] = (Country.Nigeria, Region.Europe),
			["KE"] = (Country.Kenya, Region.Europe),
			["MA"] = (Country.Morocco, Region.Europe),
			["DZ"] = (Country.Algeria, Region.Europe),
			["ET"] = (Country.Ethiopia, Region.Europe),
			["TZ"] = (Country.Tanzania, Region.Europe),
			["UG"] = (Country.Uganda, Region.Europe),
			["GH"] = (Country.Ghana, Region.Europe),
			["CI"] = (Country.IvoryCoast, Region.Europe),
			["AO"] = (Country.Angola, Region.Europe),
			["MZ"] = (Country.Mozambique, Region.Europe),
			["MG"] = (Country.Madagascar, Region.Europe),
			["AU"] = (Country.Australia, Region.Asia),
			["NZ"] = (Country.NewZealand, Region.Asia)
		};

		public static (Country country, Region region)? GetCountryAndRegion(string countryCode)
		{
			if (string.IsNullOrEmpty(countryCode))
			{
				return null;
			}
			(Country, Region) value;
			return CountryRegionMapping.TryGetValue(countryCode.ToUpper(), out value) ? value : (Country.Other, Region.Europe);
		}

		public static Country? GetCountry(string countryCode)
		{
			return GetCountryAndRegion(countryCode)?.country;
		}

		public static Region? GetRegion(string countryCode)
		{
			return GetCountryAndRegion(countryCode)?.region;
		}

		public static bool IsCountryInRegion(string countryCode, Region region)
		{
			(Country, Region)? countryAndRegion = GetCountryAndRegion(countryCode);
			if (!countryAndRegion.HasValue)
			{
				return false;
			}
			return countryAndRegion.GetValueOrDefault().Item2 == region;
		}
	}
}
