using System;
using System.Collections.Generic;

public static class MathTools
{
	public class SimpleMovingAvgFilter
	{
		private readonly List<float> m_innerBuffer;

		private int m_lastInd = -1;

		public float value { get; private set; }

		public bool isReady => m_innerBuffer.Capacity == m_innerBuffer.Count;

		public SimpleMovingAvgFilter(int N)
		{
			m_innerBuffer = new List<float>(N);
		}

		public void AddValue(float newValue)
		{
			if (m_innerBuffer.Capacity - 1 > m_innerBuffer.Count)
			{
				m_innerBuffer.Add(newValue);
				value = newValue;
				m_lastInd++;
			}
			else if (m_innerBuffer.Capacity - 1 == m_innerBuffer.Count)
			{
				m_innerBuffer.Add(newValue);
				value = 0f;
				for (int i = 0; i < m_innerBuffer.Count; i++)
				{
					value += m_innerBuffer[i];
				}
				value /= m_innerBuffer.Count;
				m_lastInd++;
			}
			else
			{
				m_lastInd = (m_lastInd + 1) % m_innerBuffer.Capacity;
				float num = m_innerBuffer[m_lastInd];
				m_innerBuffer[m_lastInd] = newValue;
				value += (newValue - num) / (float)m_innerBuffer.Capacity;
			}
		}

		public void Clear()
		{
			m_innerBuffer.Clear();
			value = 0f;
			m_lastInd = -1;
		}
	}

	public enum RoundMode
	{
		HalfEven = 0,
		HalfPos = 1,
		HalfNeg = 2,
		HalfDown = 3,
		HalfUp = 4,
		RndNeg = 5,
		RndPos = 6,
		RndDown = 7,
		RndUp = 8
	}

	public static float Round(float value, RoundMode mode = RoundMode.HalfEven)
	{
		return Round(value, 2.942979335784906E-07, mode);
	}

	public static float Round(float value, double error, RoundMode mode = RoundMode.HalfEven)
	{
		if (float.IsNaN(value))
		{
			return value;
		}
		double num = Math.Abs(error);
		double num2 = 0.0;
		switch (mode)
		{
		case RoundMode.HalfEven:
			num2 = Math.Round((double)Math.Abs(value) - num, MidpointRounding.AwayFromZero);
			if ((int)num2 % 2 != 0)
			{
				num2 = Math.Round((double)Math.Abs(value) + num, MidpointRounding.AwayFromZero);
			}
			break;
		case RoundMode.HalfPos:
			num2 = Math.Round((double)value + num, MidpointRounding.AwayFromZero);
			break;
		case RoundMode.HalfNeg:
			num2 = Math.Round((double)value - num, MidpointRounding.AwayFromZero);
			break;
		case RoundMode.HalfDown:
			num2 = Math.Round((double)Math.Abs(value) - num, MidpointRounding.AwayFromZero);
			break;
		case RoundMode.HalfUp:
			num2 = Math.Round((double)Math.Abs(value) + num, MidpointRounding.AwayFromZero);
			break;
		case RoundMode.RndNeg:
			num2 = Math.Round((double)value + (num - 0.5), MidpointRounding.AwayFromZero);
			break;
		case RoundMode.RndPos:
			num2 = Math.Round((double)value - (num - 0.5), MidpointRounding.AwayFromZero);
			break;
		case RoundMode.RndDown:
			num2 = Math.Round((double)Math.Abs(value) + (num - 0.5), MidpointRounding.AwayFromZero);
			break;
		case RoundMode.RndUp:
			num2 = Math.Round((double)Math.Abs(value) - (num - 0.5), MidpointRounding.AwayFromZero);
			break;
		default:
			num2 = Math.Round(value, MidpointRounding.AwayFromZero);
			break;
		}
		if (!(value < 0f))
		{
			return (float)num2;
		}
		return (float)(0.0 - num2);
	}

	private static double CalcEps()
	{
		double num = 1.0;
		float num2;
		do
		{
			num /= 2.0;
			num2 = 1f + (float)(num / 2.0);
		}
		while (num2 != 1f);
		return num;
	}

	public static float Clamp(float value, float min, float max)
	{
		if (!(value >= min))
		{
			return min;
		}
		if (!(value <= max))
		{
			return max;
		}
		return value;
	}

	public static int Clamp(int value, int min, int max)
	{
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	public static long Clamp(long value, long min, long max)
	{
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	public static float Lerp(float from, float to, float t)
	{
		return from + (to - from) * t;
	}

	public static long Lerp(long from, long to, double t)
	{
		return from + (long)((double)(to - from) * t);
	}
}
