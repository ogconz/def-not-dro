using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Binding.Components
{
	public class ColorToggleBinding : BoolBinding
	{
		public Color trueColor;

		public Color falseColor;

		private bool m_needSkipBinding;

		private List<Graphic> m_graphicsList = new List<Graphic>();

		protected override void Start()
		{
			GetComponents(m_graphicsList);
			base.Start();
		}

		private void OnDestroy()
		{
			if (m_graphicsList == null)
			{
				return;
			}
			for (int i = 0; i < m_graphicsList.Count; i++)
			{
				if ((bool)m_graphicsList[i])
				{
					Object.Destroy(m_graphicsList[i]);
					m_graphicsList[i] = null;
				}
			}
			m_graphicsList.Clear();
		}

		protected override void OnUpdateValue()
		{
			base.OnUpdateValue();
			if (!m_needSkipBinding)
			{
				RefreshImageColors();
			}
		}

		public void IgnoreColorBinding(bool needIgnore)
		{
			m_needSkipBinding = needIgnore;
		}

		public void RefreshImageColors()
		{
			foreach (Graphic graphics in m_graphicsList)
			{
				graphics.color = (currentValue ? trueColor : falseColor);
			}
		}

		public void ClearImageColorBinding()
		{
			foreach (Graphic graphics in m_graphicsList)
			{
				graphics.color = Color.white;
			}
		}
	}
}
