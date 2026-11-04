using System;
using Binding.Base;

namespace Binding
{
	public abstract class BaseListElementData : BaseBindingTarget, IComparable<BaseListElementData>, IDisposable
	{
		private readonly BoolProperty m_selected = new BoolProperty();

		public bool selected
		{
			get
			{
				return m_selected.value;
			}
			set
			{
				SetSelection(value);
			}
		}

		public virtual int sort { get; set; }

		public event Action<BaseListElementData> OnClick;

		public event Action<BaseListElementData> OnDoubleClick;

		public event Action<BaseListElementData> OnSelectionChanged;

		public event Action<BaseListElementData> OnDataUpdated;

		public void Click()
		{
			this.OnClick.Call(this);
			SetSelection(value: true);
		}

		public void DoubleClick()
		{
			this.OnDoubleClick.Call(this);
			SetSelection(value: true);
		}

		public int CompareTo(BaseListElementData other)
		{
			return sort.CompareTo(other.sort);
		}

		protected virtual void OnSetSelection()
		{
		}

		private void SetSelection(bool value)
		{
			if (m_selected.value != value)
			{
				m_selected.value = value;
				OnSetSelection();
				this.OnSelectionChanged.Call(this);
			}
		}

		public virtual void UpdateData()
		{
			this.OnDataUpdated.Call(this);
		}

		public virtual void OnDestroy()
		{
		}

		public virtual void Dispose()
		{
		}
	}
}
