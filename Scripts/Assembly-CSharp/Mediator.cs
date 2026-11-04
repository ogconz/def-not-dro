public class Mediator
{
	private float[] m_data;

	private int[] m_pos;

	private int[] m_allocatedHeap;

	private int m_size;

	private int m_heapBeg;

	private int m_idx;

	private int m_minCt;

	private int m_maxCt;

	public Mediator(int size)
	{
		m_data = new float[size];
		m_pos = new int[size];
		m_allocatedHeap = new int[size];
		m_size = size;
		m_heapBeg = size / 2;
		m_minCt = (m_maxCt = (m_idx = 0));
		while (size-- != 0)
		{
			m_pos[size] = (size + 1) / 2 * (((size & 1) == 0) ? 1 : (-1));
			m_allocatedHeap[m_heapBeg + m_pos[size]] = size;
		}
	}

	public void Insert(float v)
	{
		int num = m_pos[m_idx];
		float num2 = m_data[m_idx];
		m_data[m_idx] = v;
		m_idx = (m_idx + 1) % m_size;
		if (num > 0)
		{
			if (m_minCt < (m_size - 1) / 2)
			{
				m_minCt++;
			}
			else if (v > num2)
			{
				MinSortDown(num);
				return;
			}
			if (MinSortUp(num) && CmpExch(0, -1))
			{
				MaxSortDown(-1);
			}
		}
		else if (num < 0)
		{
			if (m_maxCt < m_size / 2)
			{
				m_maxCt++;
			}
			else if (v < num2)
			{
				MaxSortDown(num);
				return;
			}
			if (MaxSortUp(num) && m_minCt != 0 && CmpExch(1, 0))
			{
				MinSortDown(1);
			}
		}
		else
		{
			if (m_maxCt != 0 && MaxSortUp(-1))
			{
				MaxSortDown(-1);
			}
			if (m_minCt != 0 && MinSortUp(1))
			{
				MinSortDown(1);
			}
		}
	}

	public float GetMedian()
	{
		float num = m_data[m_allocatedHeap[m_heapBeg]];
		if (m_minCt < m_maxCt)
		{
			num = (num + m_data[m_allocatedHeap[m_heapBeg - 1]]) * 0.5f;
		}
		return num;
	}

	private bool Exchange(int i, int j)
	{
		int num = m_allocatedHeap[m_heapBeg + i];
		m_allocatedHeap[m_heapBeg + i] = m_allocatedHeap[m_heapBeg + j];
		m_allocatedHeap[m_heapBeg + j] = num;
		m_pos[m_allocatedHeap[m_heapBeg + i]] = i;
		m_pos[m_allocatedHeap[m_heapBeg + j]] = j;
		return true;
	}

	private void MinSortDown(int i)
	{
		i *= 2;
		while (i <= m_minCt)
		{
			if (i < m_minCt && Less(i + 1, i))
			{
				i++;
			}
			if (CmpExch(i, i / 2))
			{
				i *= 2;
				continue;
			}
			break;
		}
	}

	private void MaxSortDown(int i)
	{
		i *= 2;
		while (i >= -m_maxCt)
		{
			if (i > -m_maxCt && Less(i, i - 1))
			{
				i--;
			}
			if (CmpExch(i / 2, i))
			{
				i *= 2;
				continue;
			}
			break;
		}
	}

	private bool Less(int i, int j)
	{
		return m_data[m_allocatedHeap[m_heapBeg + i]] < m_data[m_allocatedHeap[m_heapBeg + j]];
	}

	private bool CmpExch(int i, int j)
	{
		if (Less(i, j))
		{
			return Exchange(i, j);
		}
		return false;
	}

	private bool MinSortUp(int i)
	{
		while (i > 0 && CmpExch(i, i / 2))
		{
			i /= 2;
		}
		return i == 0;
	}

	private bool MaxSortUp(int i)
	{
		while (i < 0 && CmpExch(i / 2, i))
		{
			i /= 2;
		}
		return i == 0;
	}
}
