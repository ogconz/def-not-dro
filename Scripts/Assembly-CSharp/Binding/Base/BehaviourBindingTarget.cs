using System;
using UnityEngine;

namespace Binding.Base
{
	public class BehaviourBindingTarget : MonoBehaviour, IBindingTarget
	{
		private BindingProps<BehaviourBindingTarget> m_bindingProps;

		public string Name => base.name;

		public GameObject selectedItem { get; set; }

		public GameObject hoveredItem { get; set; }

		public event Action OnChangeSelected;

		public event Action OnChangeHovered;

		public event Action OnForceUpdateProperties;

		protected BehaviourBindingTarget()
		{
		}

		public BaseProperty FindProperty(string propertyName, int deep)
		{
			if (m_bindingProps == null)
			{
				m_bindingProps = new BindingProps<BehaviourBindingTarget>(this);
			}
			return m_bindingProps.FindProperty(propertyName, deep);
		}

		protected void ForceUpdateProperties()
		{
			this.OnForceUpdateProperties.Call();
		}
	}
}
