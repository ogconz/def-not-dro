using UnityEngine;

namespace Chat.Overlay.UI.Layout
{
	public class LayoutData
	{
		private BaseLayoutElement[] m_elements;

		public BaseLayoutElement[] GetElements()
		{
			return m_elements;
		}

		public void CollectAllElements(Transform parent)
		{
			m_elements = parent.GetComponentsInChildren<BaseLayoutElement>(includeInactive: true);
		}
	}
}
