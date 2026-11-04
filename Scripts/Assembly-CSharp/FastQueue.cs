using System;
using UnityEngine;

public class FastQueue<T> where T : class
{
	public struct Enumerator
	{
		private int m_index;

		private readonly FastQueue<T> m_queue;

		public T Current => m_queue[m_index];

		public Enumerator(FastQueue<T> queue)
		{
			m_queue = queue;
			m_index = -1;
		}

		public void Reset()
		{
			m_index = -1;
		}

		public bool MoveNext()
		{
			if (m_index + 1 == m_queue.count)
			{
				return false;
			}
			m_index++;
			return true;
		}
	}

	private const int minCapacity = 4;

	private int m_head;

	private int m_capacity = 4;

	private int m_count;

	private T[] m_data;

	public int count => m_count;

	public int capacity
	{
		get
		{
			return m_capacity;
		}
		set
		{
			int num = Mathf.Max(value, Mathf.Max(m_count, 4));
			T[] array = new T[num];
			int num2 = Mathf.Min(m_capacity - m_head, m_count);
			Array.Copy(m_data, m_head, array, 0, num2);
			Array.Copy(m_data, 0, array, num2, m_count - num2);
			m_data = array;
			m_head = 0;
			m_capacity = num;
		}
	}

	public T this[int index]
	{
		get
		{
			return m_data[(m_head + index) % m_capacity];
		}
		set
		{
			m_data[(m_head + index) % m_capacity] = value;
		}
	}

	private int tail => (m_head + m_count - 1) % m_capacity;

	public FastQueue()
	{
		m_data = new T[m_capacity];
	}

	public FastQueue(int capacity)
	{
		m_capacity = capacity;
		m_data = new T[m_capacity];
	}

	public void PushTail(T element)
	{
		if (m_count + 1 > m_capacity)
		{
			capacity *= 2;
		}
		m_count++;
		m_data[tail] = element;
	}

	public T PopTail()
	{
		T result = m_data[tail];
		m_data[tail] = null;
		m_count--;
		return result;
	}

	public void PushHead(T element)
	{
		m_count++;
		if (m_count > m_capacity)
		{
			capacity *= 2;
		}
		if (m_count > 1)
		{
			m_head = Decrement(m_head);
		}
		m_data[m_head] = element;
	}

	public T PopHead()
	{
		m_count--;
		T result = m_data[m_head];
		m_data[m_head] = null;
		m_head = Increment(m_head);
		return result;
	}

	public T PeekHead()
	{
		return m_data[m_head];
	}

	public T PeekTail()
	{
		return m_data[tail];
	}

	public void Clear()
	{
		m_head = 0;
		m_count = 0;
	}

	public Enumerator GetEnumerator()
	{
		return new Enumerator(this);
	}

	private int Increment(int index)
	{
		return (index + 1) % m_capacity;
	}

	private int Decrement(int index)
	{
		return (index + m_capacity - 1) % m_capacity;
	}
}
