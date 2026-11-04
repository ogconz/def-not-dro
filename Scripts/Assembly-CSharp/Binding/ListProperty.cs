using System;
using System.Collections.Generic;
using Binding.Base;
using UnityEngine;

namespace Binding
{
	public class ListProperty : Property<IEnumerable<BaseListElementData>>
	{
		protected List<BaseListElementData> m_elementsData = new List<BaseListElementData>();

		public BaseListElementData this[int index]
		{
			get
			{
				return GetByIndex(index);
			}
			set
			{
				SetByIndex(index, value);
			}
		}

		public int count => m_elementsData.Count;

		public bool multiSelection { get; set; }

		public event Action<BaseListElementData> OnElementAdded;

		public event Action<int, BaseListElementData> OnElementInserted;

		public event Action<int, BaseListElementData> OnElementRemoved;

		public event Action<int> OnScrollToElement;

		public event Action<float> OnScrollToPosition;

		public event Action<int, BaseListElementData> OnElementChanged;

		public event Action<BaseListElementData> OnElementDataEdited;

		public event Action<int, int> OnElementMoved;

		public event Action OnFullReorder;

		public event Action<BaseListElementData> OnClick;

		public event Action<BaseListElementData> OnDoubleClick;

		public event Action<BaseListElementData> OnSelectionChanged;

		public void Add(BaseListElementData data)
		{
			AddElement(data);
		}

		public void Add<T>(Action<T> action) where T : BaseListElementData, new()
		{
			T val = new T();
			action(val);
			AddElement(val);
		}

		public void Insert(int index, BaseListElementData data)
		{
			Insertlement(index, data);
		}

		public void Insert<T>(int index, Action<T> action) where T : BaseListElementData, new()
		{
			T val = new T();
			action(val);
			Insertlement(index, val);
		}

		protected bool CheckIndexRange(int index)
		{
			if (index >= 0)
			{
				return index < m_elementsData.Count;
			}
			return false;
		}

		public void Remove(BaseListElementData data)
		{
			int num = m_elementsData.IndexOf(data);
			if (num != -1)
			{
				RemoveElementAt(num);
			}
		}

		public void RemoveAt(int index)
		{
			if (CheckIndexRange(index))
			{
				RemoveElementAt(index);
			}
		}

		public void RemoveRange(int index, int count)
		{
			int num = Mathf.Min(index + count, m_elementsData.Count);
			for (int i = index; i < num; i++)
			{
				RemoveElementAt(index);
			}
		}

		public void RemoveAll(Predicate<BaseListElementData> predicate)
		{
			List<int> list = new List<int>();
			for (int i = 0; i < m_elementsData.Count; i++)
			{
				BaseListElementData obj = m_elementsData[i];
				if (predicate(obj))
				{
					list.Add(i);
				}
			}
			for (int j = 0; j < list.Count; j++)
			{
				int index = list[j];
				RemoveElementAt(index);
			}
		}

		public void RemoveAll<T>(Predicate<T> predicate) where T : BaseListElementData
		{
			List<int> list = new List<int>();
			for (int i = 0; i < m_elementsData.Count; i++)
			{
				BaseListElementData baseListElementData = m_elementsData[i];
				if (predicate(baseListElementData as T))
				{
					list.Add(i);
				}
			}
			for (int j = 0; j < list.Count; j++)
			{
				int index = list[j];
				RemoveElementAt(index);
			}
		}

		public void Fill(int count, Func<BaseListElementData> coustructor)
		{
			ClearElements();
			for (int i = 0; i < count; i++)
			{
				BaseListElementData baseListElementData = coustructor();
				baseListElementData.OnSelectionChanged += SelectionChangeHandler;
				baseListElementData.OnClick += ClickHandler;
				baseListElementData.OnDoubleClick += DoubleClickHandler;
				m_elementsData.Add(baseListElementData);
			}
			ValueChanged();
		}

		public void Fill(int count, Func<int, BaseListElementData> coustructor)
		{
			ClearElements();
			for (int i = 0; i < count; i++)
			{
				BaseListElementData baseListElementData = coustructor(i);
				baseListElementData.OnSelectionChanged += SelectionChangeHandler;
				baseListElementData.OnClick += ClickHandler;
				baseListElementData.OnDoubleClick += DoubleClickHandler;
				m_elementsData.Add(baseListElementData);
			}
			ValueChanged();
		}

		public void Fill<T>(int count, Action<T> action) where T : BaseListElementData, new()
		{
			ClearElements();
			for (int i = 0; i < count; i++)
			{
				T val = new T();
				action(val);
				val.OnSelectionChanged += SelectionChangeHandler;
				val.OnClick += ClickHandler;
				val.OnDoubleClick += DoubleClickHandler;
				m_elementsData.Add(val);
			}
			ValueChanged();
		}

		public void Fill<T>(int count, Action<T, int> action) where T : BaseListElementData, new()
		{
			ClearElements();
			for (int i = 0; i < count; i++)
			{
				T val = new T();
				action(val, i);
				val.OnSelectionChanged += SelectionChangeHandler;
				val.OnClick += ClickHandler;
				val.OnDoubleClick += DoubleClickHandler;
				m_elementsData.Add(val);
			}
			ValueChanged();
		}

		public void Move(int index, int position)
		{
			if (index >= 0 && index < m_elementsData.Count && position >= 0 && position < m_elementsData.Count)
			{
				MoveElement(index, position);
			}
		}

		public void Move(BaseListElementData data, int position)
		{
			int num = m_elementsData.IndexOf(data);
			if (num != -1)
			{
				Move(num, position);
			}
		}

		public void ScrollTo(int index)
		{
			if (CheckIndexRange(index))
			{
				this.OnScrollToElement.Call(index);
			}
		}

		public void ScrollTo(BaseListElementData data)
		{
			int num = m_elementsData.IndexOf(data);
			if (num != -1)
			{
				this.OnScrollToElement.Call(num);
			}
		}

		public void ScrollTo(float position)
		{
			this.OnScrollToPosition.Call(position);
		}

		public void Selection(int index, bool selected)
		{
			if (CheckIndexRange(index))
			{
				m_elementsData[index].selected = selected;
			}
		}

		public void Selection(BaseListElementData data, bool selected)
		{
			if (m_elementsData.Contains(data))
			{
				data.selected = selected;
			}
		}

		public void Selection(Predicate<BaseListElementData> predicate, bool selected)
		{
			BaseListElementData baseListElementData = m_elementsData.Find(predicate);
			if (baseListElementData != null)
			{
				baseListElementData.selected = selected;
			}
		}

		public void Selection<T>(Predicate<T> predicate, bool selected) where T : BaseListElementData
		{
			T val = Find(predicate);
			if (val != null)
			{
				val.selected = selected;
			}
		}

		public void SelectionAll(bool selected)
		{
			for (int i = 0; i < m_elementsData.Count; i++)
			{
				m_elementsData[i].selected = selected;
			}
		}

		public void ForEach(Action<BaseListElementData> action)
		{
			m_elementsData.ForEach(delegate(BaseListElementData element)
			{
				action(element);
			});
		}

		public void ForEach(Action<BaseListElementData, int> action)
		{
			for (int i = 0; i < m_elementsData.Count; i++)
			{
				action(m_elementsData[i], i);
			}
		}

		public void ForEach<T>(Action<T> action) where T : BaseListElementData
		{
			m_elementsData.ForEach(delegate(BaseListElementData element)
			{
				action(element as T);
			});
		}

		public void ForEach<T>(Action<T, int> action) where T : BaseListElementData
		{
			for (int i = 0; i < m_elementsData.Count; i++)
			{
				action(m_elementsData[i] as T, i);
			}
		}

		public BaseListElementData Find(Predicate<BaseListElementData> predicate)
		{
			return m_elementsData.Find(predicate);
		}

		public T Find<T>(Predicate<T> predicate) where T : BaseListElementData
		{
			foreach (BaseListElementData elementsDatum in m_elementsData)
			{
				T val = elementsDatum as T;
				if (predicate(val))
				{
					return val;
				}
			}
			return null;
		}

		public List<BaseListElementData> FindAll(Predicate<BaseListElementData> predicate)
		{
			return m_elementsData.FindAll(predicate);
		}

		public List<T> FindAll<T>(Predicate<T> predicate) where T : BaseListElementData
		{
			List<T> list = new List<T>();
			foreach (BaseListElementData elementsDatum in m_elementsData)
			{
				T val = elementsDatum as T;
				if (predicate(val))
				{
					list.Add(val);
				}
			}
			return list;
		}

		public bool Exists(Predicate<BaseListElementData> predicate)
		{
			return m_elementsData.Exists(predicate);
		}

		public bool Exists<T>(Predicate<T> predicate) where T : BaseListElementData
		{
			return Find(predicate) != null;
		}

		public void Sort()
		{
			m_elementsData.Sort();
			this.OnFullReorder.Call();
		}

		public void Sort(Comparison<BaseListElementData> comparison)
		{
			m_elementsData.Sort(comparison);
			this.OnFullReorder.Call();
		}

		public BaseListElementData GetSelected()
		{
			return m_elementsData.Find((BaseListElementData data) => data.selected);
		}

		public List<BaseListElementData> GetSelectedAll()
		{
			return m_elementsData.FindAll((BaseListElementData data) => data.selected);
		}

		public int GetSelectedIndex()
		{
			return m_elementsData.FindIndex((BaseListElementData data) => data.selected);
		}

		public int IndexOf(BaseListElementData data)
		{
			return m_elementsData.IndexOf(data);
		}

		public int IndexOf(Predicate<BaseListElementData> predicate)
		{
			return m_elementsData.FindIndex(predicate);
		}

		public int IndexOf<T>(Predicate<T> predicate) where T : BaseListElementData
		{
			for (int i = 0; i < m_elementsData.Count; i++)
			{
				T obj = m_elementsData[i] as T;
				if (predicate(obj))
				{
					return i;
				}
			}
			return -1;
		}

		public void Clear()
		{
			if (m_elementsData.Count > 0)
			{
				ClearElements();
				ValueChanged();
			}
		}

		protected override IEnumerable<BaseListElementData> GetValue()
		{
			return m_elementsData;
		}

		protected override void SetValue(IEnumerable<BaseListElementData> value)
		{
			List<BaseListElementData> list = new List<BaseListElementData>(value);
			if (m_elementsData.Count == 0 || list.Count == 0)
			{
				ClearElements();
				m_elementsData = list;
				m_elementsData.ForEach(delegate(BaseListElementData baseListElementData)
				{
					baseListElementData.OnSelectionChanged += SelectionChangeHandler;
					baseListElementData.OnClick += ClickHandler;
					baseListElementData.OnDoubleClick += DoubleClickHandler;
				});
				ValueChanged();
			}
			else if (list.Count > m_elementsData.Count)
			{
				for (int num = 0; num < list.Count; num++)
				{
					if (num < m_elementsData.Count)
					{
						BaseListElementData data = list[num];
						ChangeElement(num, data);
					}
					else
					{
						BaseListElementData data2 = list[num];
						AddElement(data2);
					}
				}
			}
			else
			{
				for (int num2 = 0; num2 < list.Count; num2++)
				{
					BaseListElementData data3 = list[num2];
					ChangeElement(num2, data3);
				}
				int num3 = list.Count;
				while (num3 < m_elementsData.Count)
				{
					RemoveElementAt(m_elementsData.Count - 1);
				}
			}
		}

		public BaseListElementData GetByIndex(int index)
		{
			if (index < 0 || index >= m_elementsData.Count)
			{
				return null;
			}
			return m_elementsData[index];
		}

		public T GetByIndex<T>(int index) where T : BaseListElementData
		{
			return ((index >= 0 && index < m_elementsData.Count) ? m_elementsData[index] : null) as T;
		}

		private void SetByIndex(int index, BaseListElementData data)
		{
			if (CheckIndexRange(index))
			{
				ChangeElement(index, data);
			}
		}

		private void MoveElement(int index, int position)
		{
			BaseListElementData item = m_elementsData[index];
			m_elementsData.RemoveAt(index);
			m_elementsData.Insert(position, item);
			this.OnElementMoved.Call(index, position);
		}

		private void AddElement(BaseListElementData data)
		{
			m_elementsData.Add(data);
			data.OnSelectionChanged += SelectionChangeHandler;
			data.OnClick += ClickHandler;
			data.OnDoubleClick += DoubleClickHandler;
			data.OnDataUpdated += DataEditedHandler;
			this.OnElementInserted.Call(m_elementsData.Count - 1, data);
		}

		private void Insertlement(int index, BaseListElementData data)
		{
			m_elementsData.Insert(index, data);
			data.OnSelectionChanged += SelectionChangeHandler;
			data.OnDataUpdated += DataEditedHandler;
			data.OnClick += ClickHandler;
			data.OnDoubleClick += DoubleClickHandler;
			this.OnElementInserted.Call(index, data);
		}

		private void RemoveElementAt(int index)
		{
			BaseListElementData baseListElementData = m_elementsData[index];
			FinalizeElement(baseListElementData);
			m_elementsData.RemoveAt(index);
			this.OnElementRemoved.Call(index, baseListElementData);
		}

		private void ClearElements()
		{
			for (int i = 0; i < m_elementsData.Count; i++)
			{
				FinalizeElement(m_elementsData[i]);
			}
			m_elementsData.Clear();
		}

		private void ChangeElement(int index, BaseListElementData data)
		{
			BaseListElementData baseListElementData = m_elementsData[index];
			if (baseListElementData != data)
			{
				FinalizeElement(baseListElementData);
				m_elementsData[index] = data;
				data.OnSelectionChanged += SelectionChangeHandler;
				data.OnDataUpdated += DataEditedHandler;
				data.OnClick += ClickHandler;
				data.OnDoubleClick += DoubleClickHandler;
				this.OnElementChanged.Call(index, data);
			}
		}

		private void FinalizeElement(BaseListElementData data)
		{
			data.OnSelectionChanged -= SelectionChangeHandler;
			data.OnDataUpdated -= DataEditedHandler;
			data.OnClick -= ClickHandler;
			data.OnDoubleClick -= DoubleClickHandler;
		}

		private void ClickHandler(BaseListElementData data)
		{
			this.OnClick.SafeCall(data);
		}

		private void DoubleClickHandler(BaseListElementData data)
		{
			this.OnDoubleClick.SafeCall(data);
		}

		private void DataEditedHandler(BaseListElementData data)
		{
			this.OnElementDataEdited.SafeCall(data);
		}

		private void SelectionChangeHandler(BaseListElementData data)
		{
			if (!multiSelection && data.selected)
			{
				for (int i = 0; i < m_elementsData.Count; i++)
				{
					BaseListElementData baseListElementData = m_elementsData[i];
					if (baseListElementData != data)
					{
						baseListElementData.selected = false;
					}
				}
			}
			this.OnSelectionChanged.Call(data);
		}

		public ListProperty()
			: base((IEnumerable<BaseListElementData>)null)
		{
		}
	}
}
