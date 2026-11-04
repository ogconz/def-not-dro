using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FastList<T>
{
	private class Comparer : IComparer
	{
		private Comparison<T> m_compare;

		public Comparer(Comparison<T> comparer)
		{
			m_compare = comparer;
		}

		public int Compare(object x, object y)
		{
			return m_compare((T)x, (T)y);
		}
	}

	public T[] buffer;

	public int size;

	public T this[int i]
	{
		get
		{
			return buffer[i];
		}
		set
		{
			buffer[i] = value;
		}
	}

	public IEnumerator<T> GetEnumerator()
	{
		if (buffer != null)
		{
			int i = 0;
			while (i < size)
			{
				yield return buffer[i];
				int num = i + 1;
				i = num;
			}
		}
	}

	private void AllocateMore()
	{
		T[] array = ((buffer != null) ? new T[Mathf.Max(buffer.Length << 1, 32)] : new T[32]);
		if (buffer != null && size > 0)
		{
			buffer.CopyTo(array, 0);
		}
		buffer = array;
	}

	private void Trim()
	{
		if (size > 0)
		{
			if (size < buffer.Length)
			{
				T[] array = new T[size];
				for (int i = 0; i < size; i++)
				{
					array[i] = buffer[i];
				}
				buffer = array;
			}
		}
		else
		{
			buffer = null;
		}
	}

	public void Clear()
	{
		size = 0;
	}

	public void Release()
	{
		size = 0;
		buffer = null;
	}

	public void CopyFrom(FastList<T> source)
	{
		if (source == null)
		{
			Clear();
			return;
		}
		buffer = new T[source.buffer.Length];
		size = source.size;
		Array.Copy(source.buffer, buffer, size);
	}

	public void Add(T item)
	{
		if (buffer == null || size == buffer.Length)
		{
			AllocateMore();
		}
		buffer[size++] = item;
	}

	public void Insert(int index, T item)
	{
		if (buffer == null || size == buffer.Length)
		{
			AllocateMore();
		}
		if (index < size)
		{
			for (int num = size; num > index; num--)
			{
				buffer[num] = buffer[num - 1];
			}
			buffer[index] = item;
			size++;
		}
		else
		{
			Add(item);
		}
	}

	public bool Contains(T item)
	{
		if (buffer == null)
		{
			return false;
		}
		for (int i = 0; i < size; i++)
		{
			if (buffer[i].Equals(item))
			{
				return true;
			}
		}
		return false;
	}

	public bool Remove(T item)
	{
		if (buffer != null)
		{
			EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
			for (int i = 0; i < size; i++)
			{
				if (equalityComparer.Equals(buffer[i], item))
				{
					size--;
					buffer[i] = default(T);
					for (int j = i; j < size; j++)
					{
						buffer[j] = buffer[j + 1];
					}
					buffer[size] = default(T);
					return true;
				}
			}
		}
		return false;
	}

	public void RemoveAt(int index)
	{
		if (buffer != null && index < size)
		{
			size--;
			buffer[index] = default(T);
			for (int i = index; i < size; i++)
			{
				buffer[i] = buffer[i + 1];
			}
			buffer[size] = default(T);
		}
	}

	public T Pop()
	{
		if (buffer != null && size != 0)
		{
			T result = buffer[--size];
			buffer[size] = default(T);
			return result;
		}
		return default(T);
	}

	public T[] ToArray()
	{
		Trim();
		return buffer;
	}

	public void Sort(Comparison<T> comparer)
	{
		if (size > 0)
		{
			Array.Sort(buffer, 0, size, new Comparer(comparer));
		}
	}
}
