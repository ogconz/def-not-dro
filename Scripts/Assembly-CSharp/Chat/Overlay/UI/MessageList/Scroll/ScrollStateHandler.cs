using Binding;
using Chat.Overlay.UI.State;
using UnityEngine;

namespace Chat.Overlay.UI.MessageList.Scroll
{
	public class ScrollStateHandler
	{
		private StateContext m_state;

		private ScrollRectProperty m_scrollRect;

		private ListProperty m_listProperty;

		public void Setup(StateContext state, ScrollRectProperty scrollRect, ListProperty listProperty)
		{
			m_state = state;
			m_scrollRect = scrollRect;
			m_listProperty = listProperty;
			m_listProperty.OnSelectionChanged += OnSelectionChangedHandler;
			m_scrollRect.value.onValueChanged.AddListener(OnScrollMoveHandler);
		}

		public void Release()
		{
			m_listProperty.OnSelectionChanged -= OnSelectionChangedHandler;
			m_scrollRect.value.onValueChanged.RemoveListener(OnScrollMoveHandler);
			m_state = null;
			m_scrollRect = null;
			m_listProperty = null;
		}

		private void OnSelectionChangedHandler(BaseListElementData item)
		{
			UpdateAutoScrollState();
		}

		private void OnScrollMoveHandler(Vector2 position)
		{
			UpdateAutoScrollState();
		}

		private void UpdateAutoScrollState()
		{
			float height = m_scrollRect.value.content.GetHeight();
			float height2 = m_scrollRect.value.viewport.GetHeight();
			float num = height / height2;
			bool flag = 0.3f / num > m_scrollRect.value.verticalNormalizedPosition;
			bool flag2 = m_listProperty.GetSelectedIndex() != -1;
			m_state.messageAutoScrollMode.Set(flag && !flag2);
			if (m_state.messageAutoScrollMode.Get())
			{
				m_state.messageScrollVisible.Set(value: false);
			}
			else
			{
				m_state.messageScrollVisible.Set(height > height2 && !flag);
			}
		}
	}
}
