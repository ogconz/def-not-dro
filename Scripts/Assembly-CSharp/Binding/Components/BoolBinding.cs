using Binding.Base;

namespace Binding.Components
{
	public class BoolBinding : BaseBinding
	{
		public enum CheckBindingType
		{
			BOOLEAN = 0,
			EQUAL_TO_REFERENCE = 1,
			GREATER_THAN_REFERENCE = 2,
			LESS_THAN_REFERENCE = 3,
			EMPTY = 4,
			IS_SELECTED = 5,
			IS_HOVERED = 6
		}

		public CheckBindingType checkType;

		public double reference;

		public bool defaultValue;

		public bool invert;

		private bool m_oldValue;

		private bool m_currentValue;

		private BaseBindingBehaviourTarget m_targetContext;

		public override bool currentValue
		{
			get
			{
				if (!invert)
				{
					return m_currentValue;
				}
				return !m_currentValue;
			}
			protected set
			{
				m_currentValue = value;
			}
		}

		protected override void Start()
		{
			base.Start();
			m_oldValue = defaultValue;
			m_currentValue = defaultValue;
			m_targetContext = GetComponentInParent<BaseBindingBehaviourTarget>();
			if (checkType == CheckBindingType.IS_SELECTED && m_targetContext != null)
			{
				m_targetContext.OnChangeSelected += OnChangeSelected;
				OnChangeSelected();
			}
			else if (checkType == CheckBindingType.IS_HOVERED && m_targetContext != null)
			{
				m_targetContext.OnChangeHovered += OnChangeHovered;
				OnChangeHovered();
			}
			else
			{
				OnValueChanged();
			}
			OnUpdateValue();
		}

		protected void OnChangeSelected()
		{
			m_currentValue = m_targetContext.selectedItem != null && m_targetContext.selectedItem.name.Equals(base.propertyName);
			if (m_oldValue != m_currentValue)
			{
				m_oldValue = m_currentValue;
				OnUpdateValue();
			}
			m_oldValue = m_currentValue;
		}

		protected void OnChangeHovered()
		{
			m_currentValue = m_targetContext.hoveredItem != null && m_targetContext.hoveredItem.name.Equals(base.propertyName);
			if (m_oldValue != m_currentValue)
			{
				m_oldValue = m_currentValue;
				OnUpdateValue();
			}
		}

		protected override void OnValueChanged()
		{
			m_currentValue = defaultValue;
			if (base.property is BoolProperty boolProperty)
			{
				m_currentValue = boolProperty.value;
			}
			else if (base.property is StringProperty stringProperty && checkType == CheckBindingType.EMPTY)
			{
				m_currentValue = string.IsNullOrEmpty(stringProperty.value);
			}
			else if (checkType == CheckBindingType.EMPTY)
			{
				if (base.property != null)
				{
					object objectValue = base.property.GetObjectValue();
					m_currentValue = objectValue == null;
				}
			}
			else
			{
				double num = 0.0;
				if (base.property is IntProperty intProperty)
				{
					num = intProperty.value;
				}
				else if (base.property is FloatProperty floatProperty)
				{
					num = floatProperty.value;
				}
				switch (checkType)
				{
				case CheckBindingType.EQUAL_TO_REFERENCE:
					currentValue = num == reference;
					break;
				case CheckBindingType.GREATER_THAN_REFERENCE:
					currentValue = num > reference;
					break;
				case CheckBindingType.LESS_THAN_REFERENCE:
					currentValue = num < reference;
					break;
				}
			}
			if (m_oldValue != m_currentValue)
			{
				m_oldValue = m_currentValue;
				UpdateValue();
			}
		}
	}
}
