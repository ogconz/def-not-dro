using UnityEngine;

namespace Utlis
{
	public static class SystemLanguageConverter
	{
		public static SystemLanguage Iso639LanguageCodeToSystemLanguage(string iso639Language)
		{
			return iso639Language switch
			{
				"af" => SystemLanguage.Afrikaans, 
				"ar" => SystemLanguage.Arabic, 
				"eu" => SystemLanguage.Basque, 
				"be" => SystemLanguage.Belarusian, 
				"bg" => SystemLanguage.Bulgarian, 
				"ca" => SystemLanguage.Catalan, 
				"zh" => SystemLanguage.Chinese, 
				"cs" => SystemLanguage.Czech, 
				"da" => SystemLanguage.Danish, 
				"nl" => SystemLanguage.Dutch, 
				"en" => SystemLanguage.English, 
				"et" => SystemLanguage.Estonian, 
				"fo" => SystemLanguage.Faroese, 
				"fi" => SystemLanguage.Finnish, 
				"fr" => SystemLanguage.French, 
				"de" => SystemLanguage.German, 
				"el" => SystemLanguage.Greek, 
				"he" => SystemLanguage.Hebrew, 
				"hu" => SystemLanguage.Hungarian, 
				"is" => SystemLanguage.Icelandic, 
				"id" => SystemLanguage.Indonesian, 
				"it" => SystemLanguage.Italian, 
				"ja" => SystemLanguage.Japanese, 
				"ko" => SystemLanguage.Korean, 
				"lv" => SystemLanguage.Latvian, 
				"lt" => SystemLanguage.Lithuanian, 
				"no" => SystemLanguage.Norwegian, 
				"nb" => SystemLanguage.Norwegian, 
				"nn" => SystemLanguage.Norwegian, 
				"pl" => SystemLanguage.Polish, 
				"pt" => SystemLanguage.Portuguese, 
				"ro" => SystemLanguage.Romanian, 
				"ru" => SystemLanguage.Russian, 
				"hr" => SystemLanguage.SerboCroatian, 
				"sk" => SystemLanguage.Slovak, 
				"sl" => SystemLanguage.Slovenian, 
				"es" => SystemLanguage.Spanish, 
				"sv" => SystemLanguage.Swedish, 
				"th" => SystemLanguage.Thai, 
				"tr" => SystemLanguage.Turkish, 
				"uk" => SystemLanguage.Ukrainian, 
				"vi" => SystemLanguage.Vietnamese, 
				"hi" => SystemLanguage.Hindi, 
				_ => SystemLanguage.English, 
			};
		}

		public static string SystemLanguageToIso639LanguageCode(SystemLanguage systemLanguage)
		{
			return systemLanguage switch
			{
				SystemLanguage.Afrikaans => "af", 
				SystemLanguage.Arabic => "ar", 
				SystemLanguage.Basque => "eu", 
				SystemLanguage.Belarusian => "be", 
				SystemLanguage.Bulgarian => "bg", 
				SystemLanguage.Catalan => "ca", 
				SystemLanguage.Chinese => "zh", 
				SystemLanguage.French => "fr", 
				SystemLanguage.German => "de", 
				SystemLanguage.English => "en", 
				SystemLanguage.Greek => "el", 
				SystemLanguage.Hungarian => "hu", 
				SystemLanguage.Italian => "it", 
				SystemLanguage.Japanese => "ja", 
				SystemLanguage.Korean => "ko", 
				SystemLanguage.Polish => "pl", 
				SystemLanguage.Portuguese => "pt", 
				SystemLanguage.Russian => "ru", 
				SystemLanguage.Spanish => "es", 
				SystemLanguage.Swedish => "sv", 
				SystemLanguage.Thai => "th", 
				SystemLanguage.Turkish => "tr", 
				SystemLanguage.Vietnamese => "vi", 
				SystemLanguage.Hindi => "hi", 
				SystemLanguage.SerboCroatian => "hr", 
				SystemLanguage.Czech => "cs", 
				SystemLanguage.Danish => "da", 
				SystemLanguage.ChineseSimplified => "zh-CN", 
				SystemLanguage.ChineseTraditional => "zh-TW", 
				SystemLanguage.Dutch => "nl", 
				SystemLanguage.Estonian => "et", 
				SystemLanguage.Faroese => "fo", 
				SystemLanguage.Finnish => "fi", 
				SystemLanguage.Latvian => "lv", 
				SystemLanguage.Lithuanian => "lt", 
				SystemLanguage.Norwegian => "no", 
				SystemLanguage.Slovak => "sk", 
				SystemLanguage.Slovenian => "sl", 
				_ => "en", 
			};
		}
	}
}
