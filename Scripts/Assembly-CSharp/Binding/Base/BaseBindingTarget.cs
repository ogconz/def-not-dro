using System;
using UnityEngine;

namespace Binding.Base
{
	public abstract class BaseBindingTarget : IBindingTarget
	{
		private BindingProps<BaseBindingTarget> m_bindingProps;

		public string Name => GetType().ToString();

		public GameObject selectedItem { get; set; }

		public GameObject hoveredItem { get; set; }

		public event Action OnChangeSelected;

		public event Action OnChangeHovered;

		public event Action OnForceUpdateProperties;

		public BaseBindingTarget()
		{
		}

		public BaseProperty FindProperty(string propertyName, int deep)
		{
			if (m_bindingProps == null)
			{
				m_bindingProps = new BindingProps<BaseBindingTarget>(this);
			}
			return m_bindingProps.FindProperty(propertyName, deep);
		}

		protected void ForceUpdateProperties()
		{
			this.OnForceUpdateProperties.Call();
		}
	}
}
