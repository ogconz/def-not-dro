using UnityEngine.UI;

namespace Binding.Components
{
	public class GraphicEnableBinding : BoolBinding
	{
		private Graphic[] m_graphics;

		private int[] m_interactions;

		private void Awake()
		{
			m_graphics = GetComponentsInChildren<Graphic>(includeInactive: true);
			m_interactions = new int[m_graphics.Length];
			for (int i = 0; i < m_graphics.Length; i++)
			{
				m_interactions[i] = (m_graphics[i].raycastTarget ? 1 : 0);
			}
		}

		protected override void OnUpdateValue()
		{
			if (m_graphics == null)
			{
				return;
			}
			for (int i = 0; i < m_graphics.Length; i++)
			{
				bool raycastTarget = currentValue;
				m_graphics[i].enabled = raycastTarget;
				if (m_interactions[i] == 1)
				{
					m_graphics[i].raycastTarget = raycastTarget;
				}
			}
			Graphic[] graphics = m_graphics;
			for (int j = 0; j < graphics.Length; j++)
			{
				graphics[j].enabled = currentValue;
			}
		}
	}
}
