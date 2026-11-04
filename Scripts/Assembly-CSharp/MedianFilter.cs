public class MedianFilter
{
	private int m_num;

	private float[] m_val;

	private int[] m_big;

	private int m_med;

	private int m_count;

	private float m_median;

	public MedianFilter(int n)
	{
		m_num = n;
		m_val = new float[n];
		m_big = new int[n];
		m_count = 0;
		m_med = 0;
		m_median = 0f;
		for (int i = 0; i < m_num; i++)
		{
			m_val[i] = 0f;
			m_big[i] = ((i > m_num / 2) ? 1 : 0);
		}
	}

	public float Process(float val)
	{
		m_val[m_count] = val;
		if (m_count == m_med)
		{
			if (val <= m_median)
			{
				m_med = -1;
				m_med = FindMax();
			}
			else
			{
				m_big[m_count] = 1;
				m_med = FindMin();
				m_big[m_med] = 0;
			}
		}
		else if (m_big[m_count] == 0)
		{
			if (val > m_median)
			{
				m_big[m_count] = 1;
				m_med = FindMin();
				m_big[m_med] = 0;
			}
		}
		else if (val <= m_median)
		{
			m_big[m_count] = 0;
			m_big[m_med] = 1;
			m_med = FindMax();
		}
		m_count = (m_count + 1) % m_num;
		m_median = m_val[m_med];
		return m_median;
	}

	private int FindMax()
	{
		float num = float.MinValue;
		int result = -1;
		for (int i = 0; i < m_num; i++)
		{
			if (i != m_med && m_big[i] == 0 && m_val[i] > num)
			{
				num = m_val[i];
				result = i;
			}
		}
		return result;
	}

	private int FindMin()
	{
		float num = float.MaxValue;
		int result = -1;
		for (int i = 0; i < m_num; i++)
		{
			if (m_big[i] == 1 && m_val[i] < num)
			{
				num = m_val[i];
				result = i;
			}
		}
		return result;
	}
}
