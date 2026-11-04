using System.Collections.Generic;
using Binding.Base;
using UnityEngine;
using UnityEngine.UI;

namespace Binding.Components
{
	public class InteractableIntBinding : BaseBinding<IntProperty>
	{
		public enum CheckType
		{
			Equal = 0,
			Greater = 1,
			Less = 2,
			InRange = 3
		}

		[SerializeField]
		private CheckType m_checkType;

		[Tooltip("Examples:\n\nEqual - \"1, 2, 3\"\nGreater - \"1\"\nLess - \"1\"\nInRange - \"1, 3\"")]
		[SerializeField]
		private string m_value = "0";

		[SerializeField]
		private bool m_invert;

		private List<int> m_values = new List<int>();

		private Selectable m_component;

		private void Awake()
		{
			m_component = GetComponent<Selectable>();
			ParseValues();
		}

		protected override void OnUpdateValue()
		{
			bool flag = false;
			switch (m_checkType)
			{
			case CheckType.Equal:
				foreach (int value in m_values)
				{
					if (base.property.value == value)
					{
						flag = true;
						break;
					}
				}
				break;
			case CheckType.Greater:
			{
				int num3 = m_values[0];
				flag = base.property.value > num3;
				break;
			}
			case CheckType.Less:
			{
				int num4 = m_values[0];
				flag = base.property.value < num4;
				break;
			}
			case CheckType.InRange:
			{
				int num = m_values[0];
				int num2 = m_values[1];
				flag = base.property.value >= num && base.property.value <= num2;
				break;
			}
			}
			m_component.interactable = (m_invert ? (!flag) : flag);
		}

		private void ParseValues()
		{
			m_values.Clear();
			switch (m_checkType)
			{
			case CheckType.Equal:
			{
				string[] array2 = m_value.Trim().Split(',');
				foreach (string stringValue2 in array2)
				{
					AddValue(stringValue2);
				}
				break;
			}
			case CheckType.Greater:
			case CheckType.Less:
				AddValue(m_value);
				break;
			case CheckType.InRange:
			{
				string[] array = m_value.Trim().Split(',');
				if (array.Length >= 2)
				{
					for (int i = 0; i < 2; i++)
					{
						string stringValue = array[i];
						AddValue(stringValue);
					}
				}
				break;
			}
			}
		}

		private void AddValue(string stringValue)
		{
			int result = 0;
			if (int.TryParse(stringValue, out result))
			{
				m_values.Add(result);
			}
		}
	}
}
