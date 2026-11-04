using System;
using System.Collections.Generic;
using System.Text;
using AhoCorasick;
using TMPro;

public static class NGUISymbolsHolder
{
	private static Dictionary<string, string> m_symbolMapper;

	private static Trie m_trie;

	private static Dictionary<char, string> m_flagLetters;

	private static UTF32Encoding s_encoding;

	private static StringBuilder s_hex;

	private static List<string> s_list;

	static NGUISymbolsHolder()
	{
		m_symbolMapper = new Dictionary<string, string>();
		m_trie = new Trie();
		m_flagLetters = new Dictionary<char, string>
		{
			{ 'a', "e6" },
			{ 'b', "e7" },
			{ 'c', "e8" },
			{ 'd', "e9" },
			{ 'e', "ea" },
			{ 'f', "eb" },
			{ 'g', "ec" },
			{ 'h', "ed" },
			{ 'i', "ee" },
			{ 'j', "ef" },
			{ 'k', "f0" },
			{ 'l', "f1" },
			{ 'm', "f2" },
			{ 'n', "f3" },
			{ 'o', "f4" },
			{ 'p', "f5" },
			{ 'q', "f6" },
			{ 'r', "f7" },
			{ 's', "f8" },
			{ 't', "f9" },
			{ 'u', "fa" },
			{ 'v', "fb" },
			{ 'w', "fc" },
			{ 'x', "fd" },
			{ 'y', "fe" },
			{ 'z', "ff" }
		};
		s_encoding = new UTF32Encoding(bigEndian: true, byteOrderMark: false);
		s_hex = new StringBuilder();
		s_list = new List<string>(4);
		m_symbolMapper.Add(":)", GetConvertedString("1f600"));
		m_symbolMapper.Add("(rolf)", GetConvertedString("1f623"));
		m_symbolMapper.Add(">_<", GetConvertedString("1f606"));
		m_symbolMapper.Add(";)", GetConvertedString("1f609"));
		m_symbolMapper.Add("(cool)", GetConvertedString("1f60e"));
		m_symbolMapper.Add("(love)", GetConvertedString("1f60d"));
		m_symbolMapper.Add("(kiss)", GetConvertedString("1f618"));
		m_symbolMapper.Add("(thinking)", GetConvertedString("1f914"));
		m_symbolMapper.Add(":|", GetConvertedString("1f610"));
		m_symbolMapper.Add("(rolling)", GetConvertedString("1f644"));
		m_symbolMapper.Add(":(", GetConvertedString("1f615"));
		m_symbolMapper.Add("(zzz)", GetConvertedString("1f634"));
		m_symbolMapper.Add(":O", GetConvertedString("1f632"));
		m_symbolMapper.Add("($)", GetConvertedString("1f911"));
		m_symbolMapper.Add("(cry)", GetConvertedString("1f62d"));
		m_symbolMapper.Add("(fear)", GetConvertedString("1f631"));
		m_symbolMapper.Add("(angry)", GetConvertedString("1f621"));
		m_symbolMapper.Add("(angel)", GetConvertedString("1f607"));
		m_symbolMapper.Add("(demon)", GetConvertedString("1f47f"));
		m_symbolMapper.Add("(devil)", GetConvertedString("1f479"));
		m_symbolMapper.Add("(skull)", GetConvertedString("1f480"));
		m_symbolMapper.Add("(ghost)", GetConvertedString("1f47b"));
		m_symbolMapper.Add("(baby)", GetConvertedString("1f476"));
		m_symbolMapper.Add("(sleep)", GetConvertedString("1f6cc"));
		m_symbolMapper.Add("(selfie)", GetConvertedString("1f933"));
		m_symbolMapper.Add("(mfinger)", GetConvertedString("1f595"));
		m_symbolMapper.Add("(v)", GetConvertedString("270C"));
		m_symbolMapper.Add("(spock)", GetConvertedString("1f596"));
		m_symbolMapper.Add("(rock)", GetConvertedString("1f918"));
		m_symbolMapper.Add("(hi)", GetConvertedString("1f44b"));
		m_symbolMapper.Add("(ok)", GetConvertedString("1f44c"));
		m_symbolMapper.Add("(+1)", GetConvertedString("1f44d"));
		m_symbolMapper.Add("(-1)", GetConvertedString("1f44e"));
		m_symbolMapper.Add("(monkey)", GetConvertedString("1f412"));
		m_symbolMapper.Add("(dog)", GetConvertedString("1f415"));
		m_symbolMapper.Add("(pig)", GetConvertedString("1f437"));
		m_symbolMapper.Add("(chicken)", GetConvertedString("1f413"));
		m_symbolMapper.Add("(crab)", GetConvertedString("1f980"));
		m_symbolMapper.Add("(snail)", GetConvertedString("1f40c"));
		m_symbolMapper.Add("(drink)", GetConvertedString("1f37b"));
		m_symbolMapper.Add("(tractor)", GetConvertedString("1f69c"));
		m_symbolMapper.Add("(scooter)", GetConvertedString("1f6f4"));
		m_symbolMapper.Add("(moon)", GetConvertedString("1f31a"));
		m_symbolMapper.Add("(rain)", GetConvertedString("1f327"));
		m_symbolMapper.Add("(#1)", GetConvertedString("1f947"));
		m_symbolMapper.Add("(#2)", GetConvertedString("1f948"));
		m_symbolMapper.Add("(#3)", GetConvertedString("1f949"));
		m_symbolMapper.Add("(top)", GetConvertedString("1f51d"));
		m_symbolMapper.Add("(racing)", GetConvertedString("1f3c1"));
		foreach (string item in new List<string>
		{
			"ad", "ae", "af", "ag", "ai", "al", "am", "ao", "aq", "ar",
			"as", "at", "au", "aw", "ax", "az", "ba", "bb", "bd", "be",
			"bf", "bg", "bh", "bi", "bj", "bl", "bm", "bn", "bo", "bq",
			"br", "bs", "bt", "bv", "bw", "by", "bz", "ca", "cc", "cd",
			"cf", "cg", "ch", "ci", "ck", "cl", "cm", "cn", "co", "cr",
			"cu", "cv", "cw", "cx", "cy", "cz", "de", "dj", "dk", "dm",
			"do", "dz", "ec", "ee", "eg", "eh", "er", "es", "et", "eu",
			"fi", "fj", "fk", "fm", "fo", "fr", "ga", "gb", "ge", "gf",
			"gg", "gh", "gi", "gl", "gm", "gn", "gp", "gq", "gr", "gs",
			"gt", "gu", "gw", "gy", "hk", "hm", "hn", "hr", "ht", "hu",
			"id", "ie", "il", "im", "in", "io", "iq", "ir", "is", "it",
			"je", "jm", "jo", "jp", "ke", "kg", "kh", "ki", "km", "kn",
			"kp", "kr", "kw", "ky", "kz", "la", "lb", "lc", "li", "lk",
			"lr", "ls", "lt", "lu", "lv", "ly", "ma", "mc", "md", "me",
			"mf", "mg", "mh", "mk", "ml", "mm", "mn", "mo", "mp", "mq",
			"mr", "ms", "mt", "mu", "mv", "mw", "mx", "my", "mz", "na",
			"nc", "ne", "nf", "ng", "ni", "nl", "no", "np", "nr", "nu",
			"nz", "om", "pa", "pe", "pf", "pg", "ph", "pk", "pl", "pm",
			"pn", "pr", "ps", "pt", "pw", "py", "qa", "re", "ro", "rs",
			"ru", "rw", "sa", "sb", "sc", "sd", "se", "sg", "sh", "si",
			"sj", "sk", "sl", "sm", "sn", "so", "sr", "ss", "st", "su",
			"sv", "sx", "sy", "sz", "tc", "td", "tf", "tg", "th", "tj",
			"tk", "tl", "tm", "tn", "to", "tr", "tt", "tv", "tw", "tz",
			"ua", "ug", "um", "us", "uy", "uz", "va", "vc", "ve", "vg",
			"vi", "vn", "vu", "wf", "ws", "ye", "yt", "za", "zm", "zw"
		})
		{
			m_symbolMapper.Add("(:" + item + ")", GetFlagSmile(item));
		}
		m_trie.Add(m_symbolMapper.Keys);
		m_trie.Build();
	}

	public static string GetFlagSmile(string contryCode)
	{
		if (string.IsNullOrEmpty(contryCode))
		{
			return "";
		}
		string text = "1f1" + m_flagLetters[contryCode[0]];
		for (int i = 1; i < contryCode.Length; i++)
		{
			text = text + "-1f1" + m_flagLetters[contryCode[i]];
		}
		return GetConvertedString(text);
	}

	public static string PreparseString(string input)
	{
		foreach (string item in m_trie.Find(input))
		{
			input = input.Replace(item, m_symbolMapper[item]);
		}
		return input;
	}

	public static string GetConvertedString(string inputString)
	{
		string[] array = inputString.Split('-');
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = char.ConvertFromUtf32(Convert.ToInt32(array[i], 16));
		}
		return string.Join(string.Empty, array);
	}

	public static string TMP_PreparseString(string input)
	{
		foreach (string item in m_trie.Find(input))
		{
			string text = m_symbolMapper[item];
			if (text.Length > 2)
			{
				string name = UnicodeToString(text);
				int spriteIndexFromName = TMP_Settings.defaultSpriteAsset.GetSpriteIndexFromName(name);
				if (spriteIndexFromName >= 0)
				{
					input = input.Replace(item, $"<sprite={spriteIndexFromName}/>");
					continue;
				}
			}
			input = input.Replace(item, text);
		}
		return input;
	}

	private static string UnicodeToString(string inputString)
	{
		byte[] bytes = s_encoding.GetBytes(inputString);
		s_hex.Clear();
		s_list.Clear();
		for (int i = 0; i < bytes.Length; i++)
		{
			string value = bytes[i].ToString("x");
			s_hex.Append(value);
			if ((i + 1) % 4 == 0)
			{
				s_list.Add(s_hex.ToString());
				s_hex.Clear();
			}
		}
		for (int num = s_list.Count - 1; num >= 0; num--)
		{
			string text = s_list[num];
			if (string.IsNullOrEmpty(text) || text == "0000")
			{
				s_list.RemoveAt(num);
			}
			else if (text[0] == '0')
			{
				s_list[num] = text.Remove(0, 1);
			}
		}
		return string.Join("-", s_list.ToArray());
	}
}
