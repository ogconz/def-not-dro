public static class StringExtension
{
	public static string RepeatSpacePaste(this string text, string space, int countSkip)
	{
		if (string.IsNullOrEmpty(text) || text.Length < countSkip)
		{
			return text;
		}
		int num = 0;
		int length = text.Length;
		for (int i = countSkip; i < length; i += countSkip)
		{
			text = text.Insert(i + num, space);
			num += space.Length;
		}
		return text;
	}
}
