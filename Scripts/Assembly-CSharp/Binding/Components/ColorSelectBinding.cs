using System;
using System.Collections.Generic;
using Binding.Base;
using UnityEngine;
using UnityEngine.UI;

namespace Binding.Components
{
	public class ColorSelectBinding : BaseBinding<IntProperty>
	{
		[Serializable]
		private class Reference
		{
			public int value;

			public Color color = Color.white;
		}

		private List<Graphic> m_graphicsList = new List<Graphic>();

		[SerializeField]
		private Reference[] m_reference;

		private void Awake()
		{
			GetComponents(m_graphicsList);
		}

		protected override void OnUpdateValue()
		{
			base.OnUpdateValue();
			Refresh(base.property.value);
		}

		private void Refresh(int value)
		{
			Reference[] reference = m_reference;
			foreach (Reference reference2 in reference)
			{
				if (reference2.value != value)
				{
					continue;
				}
				{
					foreach (Graphic graphics in m_graphicsList)
					{
						graphics.color = reference2.color;
					}
					break;
				}
			}
		}
	}
}
