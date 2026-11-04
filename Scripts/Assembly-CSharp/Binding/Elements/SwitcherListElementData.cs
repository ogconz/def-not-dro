using System;
using System.Collections.Generic;
using UnityEngine;

namespace Binding.Elements
{
	public class SwitcherListElementData<T> : BaseListElementData, ISwitcherListElementData
	{
		private StringProperty m_valueStr = new StringProperty();

		private List<T> m_list = new List<T>();

		private Func<T, string> m_formatFunc;

		private int m_index = -1;

		public bool isEmpty => m_list.Count == 0;

		public bool isValidIndex
		{
			get
			{
				if (m_index >= 0)
				{
					return m_index < m_list.Count;
				}
				return false;
			}
		}

		public int index
		{
			get
			{
				return m_index;
			}
			set
			{
				if (m_index != value && value < m_list.Count && value >= 0)
				{
					m_index = value;
					Refresh();
					this.onChanged.Call();
				}
			}
		}

		public T item
		{
			get
			{
				if (isValidIndex)
				{
					return m_list[index];
				}
				return default(T);
			}
		}

		public event Action onChanged;

		public SwitcherListElementData()
		{
			m_formatFunc = (T x) => x.ToString();
		}

		public SwitcherListElementData(Func<T, string> formatFunc)
		{
			m_formatFunc = formatFunc;
		}

		public void Init(IEnumerable<T> list)
		{
			Clear();
			m_list.AddRange(list);
			Validate();
		}

		public void Clear()
		{
			m_list.Clear();
		}

		public void Add(T item)
		{
			m_list.Add(item);
		}

		public void Validate()
		{
			m_index = Mathf.Clamp(m_index, 0, m_list.Count - 1);
			if (isValidIndex)
			{
				Refresh();
				this.onChanged.Call();
			}
		}

		public void Refresh()
		{
			if (isValidIndex)
			{
				m_valueStr.value = m_formatFunc(item);
			}
		}

		public void Next()
		{
			int num = index + 1;
			if (num >= m_list.Count)
			{
				num = 0;
			}
			index = num;
		}

		public void Prev()
		{
			int num = index - 1;
			if (num < 0)
			{
				num = m_list.Count - 1;
			}
			index = num;
		}
	}
}
