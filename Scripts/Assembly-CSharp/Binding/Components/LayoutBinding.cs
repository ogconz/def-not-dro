using System;
using Binding.Base;
using UnityEngine;
using UnityEngine.UI;

namespace Binding.Components
{
	public class LayoutBinding : BaseBinding<FloatProperty>
	{
		public enum ParameterType
		{
			None = -1,
			Spacing = 0,
			MinWidth = 1,
			MinHeight = 2,
			PreferedWidth = 3,
			PreferedHeight = 4,
			FlexibleWidth = 5,
			FlexibleHeight = 6
		}

		[Flags]
		public enum Paddings
		{
			PaddingLeft = 1,
			PaddingTop = 2,
			PaddingRight = 4,
			PaddingBottom = 8
		}

		[SerializeField]
		private ParameterType m_parameterType;

		[SerializeField]
		private Paddings m_paddings;

		private HorizontalOrVerticalLayoutGroup m_group;

		private LayoutElement m_element;

		private void Awake()
		{
			m_group = GetComponent<HorizontalOrVerticalLayoutGroup>();
			m_element = GetComponent<LayoutElement>();
		}

		protected override void OnUpdateValue()
		{
			SetLayoutGroupPaddings();
			SetParameterType();
		}

		private void SetLayoutGroupPaddings()
		{
			if (!(m_group == null))
			{
				int num = (int)base.property.value;
				if (m_paddings.HasFlag(Paddings.PaddingLeft))
				{
					m_group.padding.left = num;
				}
				if (m_paddings.HasFlag(Paddings.PaddingTop))
				{
					m_group.padding.top = num;
				}
				if (m_paddings.HasFlag(Paddings.PaddingRight))
				{
					m_group.padding.right = num;
				}
				if (m_paddings.HasFlag(Paddings.PaddingBottom))
				{
					m_group.padding.bottom = num;
				}
			}
		}

		private void SetParameterType()
		{
			switch (m_parameterType)
			{
			case ParameterType.Spacing:
				if (m_group != null)
				{
					m_group.spacing = base.property.value;
				}
				break;
			case ParameterType.MinWidth:
				if (m_element != null)
				{
					m_element.minWidth = base.property.value;
				}
				break;
			case ParameterType.MinHeight:
				if (m_element != null)
				{
					m_element.minHeight = base.property.value;
				}
				break;
			case ParameterType.PreferedWidth:
				if (m_element != null)
				{
					m_element.preferredWidth = base.property.value;
				}
				break;
			case ParameterType.PreferedHeight:
				if (m_element != null)
				{
					m_element.preferredHeight = base.property.value;
				}
				break;
			case ParameterType.FlexibleWidth:
				if (m_element != null)
				{
					m_element.flexibleWidth = base.property.value;
				}
				break;
			case ParameterType.FlexibleHeight:
				if (m_element != null)
				{
					m_element.flexibleHeight = base.property.value;
				}
				break;
			}
		}
	}
}
