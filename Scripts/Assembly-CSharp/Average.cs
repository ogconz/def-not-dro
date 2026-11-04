using UnityEngine;

public class Average
{
	public float last { get; private set; }

	public float max { get; private set; }

	public float min { get; private set; }

	public int count { get; private set; }

	public float average { get; private set; }

	public Average()
	{
		Reset();
	}

	public void Push(float value)
	{
		last = value;
		average = (average * (float)count + value) / (float)(++count);
		max = Mathf.Max(max, value);
		min = Mathf.Min(min, value);
	}

	public float GetValue(bool average)
	{
		if (!average)
		{
			return last;
		}
		return this.average;
	}

	public void Reset()
	{
		int num = (count = 0);
		float num3 = (min = num);
		float num5 = (max = num3);
		float num7 = (average = num5);
		last = num7;
	}
}
