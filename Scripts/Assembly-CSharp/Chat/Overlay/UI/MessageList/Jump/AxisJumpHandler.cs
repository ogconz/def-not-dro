using Chat.Overlay.UI.MessageList.Scroll;
using UnityEngine;

namespace Chat.Overlay.UI.MessageList.Jump
{
	public class AxisJumpHandler
	{
		private const float SwipeJumpThreshold = 0.85f;

		private const float BlackZoneThreshold = 0.4f;

		private const float DoubleSwipeTime = 0.35f;

		private ScrollProcessor m_scrollProcessor;

		private float m_lastSwipeTime;

		private bool m_isSwiped;

		public void Setup(ScrollProcessor scrollProcessor)
		{
			m_scrollProcessor = scrollProcessor;
			m_scrollProcessor.scrollMoveReader.onScrollEvent += OnScrollMoveHandler;
		}

		public void Release()
		{
			m_scrollProcessor.scrollMoveReader.onScrollEvent -= OnScrollMoveHandler;
			m_scrollProcessor = null;
		}

		private void OnScrollMoveHandler(float delta)
		{
			if (IsBlackZone(delta))
			{
				ReleaseSwipe();
			}
			else if (IsUpDirection(delta))
			{
				ReleaseSwipe();
				m_lastSwipeTime = 0f;
			}
			else if (IsJumpZone(delta))
			{
				if (IsDoubleSwipe())
				{
					m_lastSwipeTime = 0f;
					m_scrollProcessor.FocusToBottom();
				}
				RegisterSwipe();
			}
		}

		private void RegisterSwipe()
		{
			m_isSwiped = true;
		}

		private void ReleaseSwipe()
		{
			if (m_isSwiped)
			{
				m_lastSwipeTime = Time.unscaledTime;
			}
			m_isSwiped = false;
		}

		private bool IsBlackZone(float delta)
		{
			return Mathf.Abs(delta) < 0.4f;
		}

		private bool IsUpDirection(float delta)
		{
			return delta > 0f;
		}

		private bool IsJumpZone(float delta)
		{
			return Mathf.Abs(delta) > 0.85f;
		}

		private bool IsDoubleSwipe()
		{
			if (!m_isSwiped)
			{
				return m_lastSwipeTime + 0.35f > Time.unscaledTime;
			}
			return false;
		}
	}
}
