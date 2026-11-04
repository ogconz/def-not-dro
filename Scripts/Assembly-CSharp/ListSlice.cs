using System;
using System.Collections;
using System.Collections.Generic;

public class ListSlice<T> : IReadOnlyList<T>, IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>
{
	private class ListSliceEnumerator<TEnum> : IEnumerator<TEnum>, IEnumerator, IDisposable
	{
		private ListSlice<TEnum> m_slice;

		private int m_position = -1;

		public TEnum Current => m_slice[m_position];

		object IEnumerator.Current => Current;

		public ListSliceEnumerator(ListSlice<TEnum> buffer)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException();
			}
			m_slice = buffer;
		}

		public void Dispose()
		{
		}

		public bool MoveNext()
		{
			m_position++;
			return m_position < m_slice.Count;
		}

		public void Reset()
		{
			m_position = -1;
		}
	}

	private readonly IReadOnlyList<T> m_innerContainer;

	private readonly int m_startIndex;

	public int Count { get; }

	public T this[int index] => m_innerContainer[index + m_startIndex];

	public ListSlice(IReadOnlyList<T> container, int startIndex, int count)
	{
		if (startIndex < 0)
		{
			GameLog.Print(AlertLevel.FatalError, "System", "Given starting index should be non-negative value");
		}
		if (count < 0)
		{
			GameLog.Print(AlertLevel.FatalError, "System", "Given Count should be greater then 0");
		}
		if (startIndex + count > container.Count)
		{
			GameLog.Print(AlertLevel.FatalError, "System", "Desired list slice exceeds given container");
		}
		if (container == null)
		{
			GameLog.Print(AlertLevel.FatalError, "System", "Inner container cannot be NULL");
		}
		m_innerContainer = container;
		m_startIndex = startIndex;
		Count = count;
	}

	public IEnumerator<T> GetEnumerator()
	{
		return new ListSliceEnumerator<T>(this);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
