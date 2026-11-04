using Chat.Overlay.UI.Layout;
using UnityEngine;

namespace Chat.Overlay
{
	public class ContextLayoutProcessor
	{
		private GameContextObserver m_contextObserver;

		private LayoutData m_layoutData;

		private LayoutType m_currentLayout;

		public void Setup(GameContextObserver contextObserver)
		{
			m_contextObserver = contextObserver;
			m_contextObserver.onStateChanged += UpdateLayout;
		}

		public void Release()
		{
			m_contextObserver.onStateChanged -= UpdateLayout;
			m_contextObserver = null;
			m_layoutData = null;
		}

		public void BindData(LayoutData data)
		{
			m_layoutData = data;
		}

		public void BindContext(BaseContext context, TargetType type)
		{
			BaseLayoutElement[] elements = m_layoutData.GetElements();
			foreach (BaseLayoutElement baseLayoutElement in elements)
			{
				if (baseLayoutElement.targetType == type)
				{
					baseLayoutElement.SetTargetTransform((RectTransform)context.transform);
				}
			}
		}

		public void UpdateLayout()
		{
			BaseState currentState = m_contextObserver.GetCurrentState();
			LayoutType layoutForState = GetLayoutForState(currentState);
			ApplyLayout(layoutForState);
		}

		private void ApplyLayout(LayoutType layout)
		{
			if (m_layoutData == null || m_currentLayout == layout)
			{
				return;
			}
			m_currentLayout = layout;
			BaseLayoutElement[] elements = m_layoutData.GetElements();
			foreach (BaseLayoutElement baseLayoutElement in elements)
			{
				if (baseLayoutElement.layoutType == layout)
				{
					baseLayoutElement.Apply();
				}
			}
		}

		private LayoutType GetLayoutForState(BaseState state)
		{
			if (state is GarageGUIState)
			{
				return LayoutType.Garage;
			}
			return LayoutType.Race;
		}
	}
}
