using Binding;
using UnityEngine;

namespace Chat.Overlay.UI.MessageList.Scroll
{
	public class ScrollMoveHandler
	{
		private ScrollMoveReader m_scrollMoveReader;

		private ScrollRectProperty m_scrollRect;

		private ListProperty m_listProperty;

		private TrackedScrollRect m_trackedScrollRect;

		private float m_targetScrollPosition;

		private int m_lastManualSelectedIndex;

		public void Setup(ScrollMoveReader scrollMoveReader, ScrollRectProperty scrollRect, ListProperty listProperty)
		{
			m_scrollMoveReader = scrollMoveReader;
			m_scrollRect = scrollRect;
			m_listProperty = listProperty;
			m_trackedScrollRect = (TrackedScrollRect)m_scrollRect.value;
			m_scrollMoveReader.onMoveEvent += OnMoveHandler;
			m_scrollMoveReader.onScrollEvent += OnScrollHandler;
			m_trackedScrollRect.onScrollEvent += RecordScrollPosition;
		}

		public void Release()
		{
			m_scrollMoveReader.onMoveEvent -= OnMoveHandler;
			m_scrollMoveReader.onScrollEvent -= OnScrollHandler;
			m_trackedScrollRect.onScrollEvent -= RecordScrollPosition;
			m_scrollMoveReader = null;
			m_scrollRect = null;
			m_listProperty = null;
			m_trackedScrollRect = null;
		}

		private void OnMoveHandler(float delta)
		{
			int num = ((delta < 0f) ? 1 : (-1));
			int num2 = m_listProperty.GetSelectedIndex();
			if (num2 < 0 || num2 >= m_listProperty.count)
			{
				if (m_lastManualSelectedIndex != -1)
				{
					num2 = m_lastManualSelectedIndex;
				}
				else
				{
					SyncScrollPosition();
					num2 = GetElementIndexByTargetPosition();
				}
				num = 0;
			}
			num2 += num;
			num2 = Mathf.Clamp(num2, 0, m_listProperty.count - 1);
			SetTargetScrollPosition(GetScrollPositionFromIndex(num2));
			m_listProperty.ScrollTo(num2);
			SetSelection(num2);
		}

		private void OnScrollHandler(float rawDelta)
		{
			if (m_listProperty.GetSelectedIndex() == -1)
			{
				SyncScrollPosition();
			}
			float normalizedScrollDelta = GetNormalizedScrollDelta(rawDelta * 0.1f);
			SetTargetScrollPosition(m_targetScrollPosition + normalizedScrollDelta);
			m_listProperty.ScrollTo(m_targetScrollPosition);
			SetSelection(GetElementIndexByTargetPosition());
		}

		private void RecordScrollPosition(TrackedScrollRect.ScrollType scrollType, Vector2 position)
		{
			if (scrollType != TrackedScrollRect.ScrollType.Other && m_scrollMoveReader.IsInputUnlocked() && SyncScrollPositionAndGetDiffDelta() > 1E-05f)
			{
				ResetSelection();
			}
		}

		private void SetSelection(int index)
		{
			m_listProperty.Selection(index, selected: true);
			m_lastManualSelectedIndex = index;
		}

		public void ResetSelection()
		{
			m_listProperty.SelectionAll(selected: false);
			m_lastManualSelectedIndex = -1;
			m_targetScrollPosition = 0f;
		}

		public void LastSelection()
		{
			int num = m_listProperty.count - 1;
			m_listProperty.Selection(num, selected: true);
			m_lastManualSelectedIndex = num;
			m_targetScrollPosition = 0f;
		}

		private void SyncScrollPosition()
		{
			SetTargetScrollPosition(m_trackedScrollRect.verticalNormalizedPosition);
		}

		private float SyncScrollPositionAndGetDiffDelta()
		{
			float targetScrollPosition = m_targetScrollPosition;
			SyncScrollPosition();
			return Mathf.Abs(m_targetScrollPosition - targetScrollPosition);
		}

		private void SetTargetScrollPosition(float position)
		{
			m_targetScrollPosition = Mathf.Clamp01(position);
		}

		private int GetElementIndexByTargetPosition()
		{
			int num = m_listProperty.count - 1;
			int num2 = Mathf.RoundToInt(m_targetScrollPosition * (float)num);
			return num - num2;
		}

		private float GetNormalizedScrollDelta(float delta)
		{
			int num = Mathf.Max(1, m_listProperty.count);
			int num2 = Mathf.Max(1, num - 1);
			float num3 = 1f / (float)num2;
			return delta * num3;
		}

		private float GetScrollPositionFromIndex(int index)
		{
			int num = m_listProperty.count - 1;
			return (float)(num - index) / (float)num;
		}
	}
}
