using System;
using System.Collections.Generic;
using UnityEngine;

namespace Binding.Base
{
	public abstract class BaseBinding : MonoBehaviour
	{
		[SerializeField]
		private string m_path = "";

		[SerializeField]
		private bool m_globalBind;

		private BaseProperty m_property;

		private IBindingTarget m_target;

		private string m_propertyName;

		private Action m_onValueChangedAction;

		private Action m_onForceUpdateAction;

		protected BaseProperty property => m_property;

		protected IBindingTarget target => m_target;

		protected string propertyName => m_propertyName;

		public virtual bool currentValue { get; protected set; }

		public string path => m_path;

		public bool initialized { get; private set; }

		protected virtual void Start()
		{
			m_onValueChangedAction = OnValueChanged;
			m_onForceUpdateAction = OnForceUpdate;
			Init();
			Bind(total: true);
			UpdateValue();
		}

		private void OnDestroy()
		{
			Unbind(total: true);
			m_onValueChangedAction = null;
			m_onForceUpdateAction = null;
		}

		protected void Init()
		{
			m_propertyName = m_path;
			if (!string.IsNullOrEmpty(m_propertyName))
			{
				FindTarget();
			}
			initialized = true;
		}

		protected virtual void OnValueChanged()
		{
			UpdateValue();
		}

		protected virtual void OnForceUpdate()
		{
			Unbind();
			UpdateProperty();
			Bind();
			UpdateValue();
		}

		protected void Bind(bool total = false)
		{
			if (m_property != null)
			{
				m_property.OnValueChanged += m_onValueChangedAction;
				if (total)
				{
					m_target.OnForceUpdateProperties += m_onForceUpdateAction;
				}
			}
		}

		protected void Unbind(bool total = false)
		{
			if (m_property != null)
			{
				m_property.OnValueChanged -= m_onValueChangedAction;
				if (total)
				{
					m_target.OnForceUpdateProperties -= m_onForceUpdateAction;
				}
			}
		}

		protected void UpdateProperty()
		{
			if (m_target != null)
			{
				int deep = 0;
				m_property = m_target.FindProperty(m_propertyName, deep);
			}
		}

		protected void UpdateValue()
		{
			if (m_property != null)
			{
				OnUpdateValue();
			}
		}

		protected virtual void OnUpdateValue()
		{
		}

		private void FindTarget()
		{
			List<IBindingTarget> list = new List<IBindingTarget>(4);
			List<IBindingTarget> ignoreBuffer = new List<IBindingTarget>();
			Transform transform = base.transform;
			while (transform != null)
			{
				GetTargets(transform, list);
				ClearDuplicates(list, ignoreBuffer);
				int deep = 0;
				foreach (IBindingTarget item in list)
				{
					if (item != null)
					{
						m_property = item.FindProperty(m_propertyName, deep);
						if (m_property != null)
						{
							m_target = item;
							return;
						}
					}
				}
				transform = (m_globalBind ? transform.parent : null);
			}
		}

		private void GetTargets(Transform root, List<IBindingTarget> targetsBuffer)
		{
			targetsBuffer.Clear();
			root.GetComponentsInParent(includeInactive: true, targetsBuffer);
			if (targetsBuffer.Count == 0)
			{
				for (int i = 0; i < root.childCount; i++)
				{
					targetsBuffer.AddRange(root.GetChild(i).GetComponents<IBindingTarget>());
				}
			}
		}

		private void ClearDuplicates(List<IBindingTarget> targetsBuffer, List<IBindingTarget> ignoreBuffer)
		{
			List<IBindingTarget> list = new List<IBindingTarget>();
			for (int i = 0; i < targetsBuffer.Count; i++)
			{
				if (ignoreBuffer.Contains(targetsBuffer[i]))
				{
					list.Add(targetsBuffer[i]);
				}
				else
				{
					ignoreBuffer.Add(targetsBuffer[i]);
				}
			}
			for (int j = 0; j < list.Count; j++)
			{
				targetsBuffer.Remove(list[j]);
			}
		}

		protected virtual bool IsPropertyValid(BaseProperty property)
		{
			return property != null;
		}
	}
	public abstract class BaseBinding<T> : BaseBinding where T : BaseProperty
	{
		protected new T property => base.property as T;

		protected override bool IsPropertyValid(BaseProperty property)
		{
			if (property != null)
			{
				return property is T;
			}
			return false;
		}
	}
}
