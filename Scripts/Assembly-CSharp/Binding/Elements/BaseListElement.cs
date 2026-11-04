using System;
using Binding.Base;

namespace Binding.Elements
{
	public abstract class BaseListElement : BaseBindingBehaviourTarget, IComparable<BaseListElement>
	{
		private BaseListElementData m_data;

		public BaseListElementData data => m_data;

		public void Init(BaseListElementData data)
		{
			m_data = data;
			OnDataChange(data);
			OnInit();
			ForceUpdateProperties();
		}

		public int CompareTo(BaseListElement other)
		{
			return m_data.sort.CompareTo(other.m_data.sort);
		}

		public virtual void ButtonClickHandler()
		{
			data?.Click();
		}

		public virtual void OnDoubleClickHandler()
		{
			data?.DoubleClick();
		}

		public virtual void OnDataUpdated()
		{
		}

		protected virtual void OnInit()
		{
		}

		protected virtual void OnDataChange(BaseListElementData newData)
		{
		}

		protected override void OnDestroy()
		{
			data?.OnDestroy();
		}
	}
}
