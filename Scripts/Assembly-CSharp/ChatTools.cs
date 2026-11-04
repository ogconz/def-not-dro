using System;
using GameOverlay;
using UnityEngine;

public static class ChatTools
{
	public static string WrapColor(string value, string hexColor)
	{
		return $"<color=#{hexColor}>{value}</color>";
	}

	public static string WrapLink(string value, PlayerId link)
	{
		return $"<link={link.ToString()}>{value}</link>";
	}

	public static string WrapColor(string value, Color color)
	{
		string text = ColorChannelToHex(color.r);
		string text2 = ColorChannelToHex(color.g);
		string text3 = ColorChannelToHex(color.b);
		return WrapColor(value, text + text2 + text3);
	}

	public static string ColorToBBCode(Color color)
	{
		string text = ColorChannelToHex(color.r);
		string text2 = ColorChannelToHex(color.g);
		string text3 = ColorChannelToHex(color.b);
		return "[" + text + text2 + text3 + "]";
	}

	public static string ColorChannelToHex(float value)
	{
		return Convert.ToInt32(value * 255f).ToString("X2");
	}

	public static bool IsInURLTagZone(string s, int characterIndex)
	{
		if (characterIndex != -1 && characterIndex < s.Length - 6)
		{
			int num = ((s[characterIndex] != '[' || s[characterIndex + 1] != 'u' || s[characterIndex + 2] != 'r' || s[characterIndex + 3] != 'l' || s[characterIndex + 4] != '=') ? s.LastIndexOf("[url=", characterIndex) : characterIndex);
			if (num == -1)
			{
				return false;
			}
			num += 5;
			int num2 = s.IndexOf("]", num);
			if (num2 == -1)
			{
				return false;
			}
			int num3 = s.IndexOf("[/url]", num2);
			if (num3 == -1 || characterIndex <= num3)
			{
				return true;
			}
		}
		return false;
	}
}
