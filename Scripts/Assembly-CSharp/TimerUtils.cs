using System;
using System.Text;

public class TimerUtils
{
	private const char Dot = '.';

	private static readonly double m_startInSeconds;

	public static double StartTime => m_startInSeconds;

	public static double CurrentTime => DateTimeToSeconds(DateTime.Now);

	static TimerUtils()
	{
		m_startInSeconds = DateTimeToSeconds(DateTime.Now);
	}

	public static double DateTimeToSeconds(DateTime dateTime)
	{
		return TimeSpan.FromTicks(dateTime.Ticks).TotalSeconds;
	}

	public static DateTime SecondsToDateTime(double seconds)
	{
		return default(DateTime).Add(TimeSpan.FromSeconds(seconds));
	}

	public static DateTime GetDayBegin()
	{
		return DateTime.Today.AddHours(5.0);
	}

	private static string ConvertTimeSpan(string source, char from, char to)
	{
		if (source != null)
		{
			StringBuilder stringBuilder = new StringBuilder();
			bool flag = false;
			foreach (char c in source)
			{
				if (!flag)
				{
					flag = c == '.';
					stringBuilder.Append(c);
				}
				else if (c == from)
				{
					stringBuilder.Append(to);
				}
				else
				{
					stringBuilder.Append(c);
				}
			}
			return stringBuilder.ToString();
		}
		return null;
	}

	public static string TimeSpanToJson(TimeSpan timeSpan)
	{
		return ConvertTimeSpan(timeSpan.ToString(), ':', '.');
	}

	public static TimeSpan TimeSpanFromJson(string text)
	{
		text = ConvertTimeSpan(text, '.', ':');
		return TimeSpan.Parse(text);
	}
}
