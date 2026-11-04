using System;
using GameInput;

namespace Chat.Overlay.UI.Utils
{
	public class HotkeyProvider
	{
		private HotkeysCollector m_hotkeysCollector = new HotkeysCollector();

		private BaseContext m_context;

		private int m_registeredHotkeysCount;

		private bool m_oldHotkeyBlockState;

		private bool m_oldRaycastBlockState;

		public void Setup(BaseContext context)
		{
			m_context = context;
			ApplyHotkeys();
		}

		public void Release()
		{
			ReleaseHotkeys();
			m_context = null;
		}

		public void Add(ActionType actionType, System.Action action, bool repeat)
		{
			m_hotkeysCollector.Add(actionType, action, repeat);
		}

		public void Remove(ActionType actionType, System.Action action)
		{
			m_hotkeysCollector.Remove(actionType, action);
		}

		public void UpdateHotkeys()
		{
			if (RequireRegisterHotkeys())
			{
				ApplyHotkeys();
			}
			else
			{
				ReleaseHotkeys();
			}
		}

		private bool RequireRegisterHotkeys()
		{
			int rawCount = m_hotkeysCollector.GetRawCount();
			if (rawCount > 0)
			{
				return rawCount != m_registeredHotkeysCount;
			}
			return false;
		}

		private bool IsHotkeysRegistered()
		{
			return m_registeredHotkeysCount > 0;
		}

		private bool IsContextReady()
		{
			return m_context != null;
		}

		private void ApplyHotkeys()
		{
			if (IsContextReady() && RequireRegisterHotkeys())
			{
				if (IsHotkeysRegistered())
				{
					HotkeyManager.UpdateHotkeys(m_context, m_hotkeysCollector.CollectHotkeys());
				}
				else
				{
					ApplyBlockers();
					HotkeyManager.RegisterHotkeys(m_context, m_hotkeysCollector.CollectHotkeys());
				}
				m_registeredHotkeysCount = m_hotkeysCollector.GetCollectedCount();
			}
		}

		private void ReleaseHotkeys()
		{
			if (IsContextReady() && IsHotkeysRegistered())
			{
				ReleaseBlockers();
				HotkeyManager.UnregisterHotkeys(m_context);
				m_registeredHotkeysCount = 0;
			}
		}

		private void ApplyBlockers()
		{
			m_oldHotkeyBlockState = m_context.IsHotkeyBlocker();
			m_oldRaycastBlockState = m_context.IsRaycastBlocker();
			m_context.SetHotkeyBlocker(value: true);
			m_context.SetRaycastBlocker(value: true);
		}

		private void ReleaseBlockers()
		{
			m_context.SetHotkeyBlocker(m_oldHotkeyBlockState);
			m_context.SetRaycastBlocker(m_oldRaycastBlockState);
		}

		public bool IsHotkeyBlocked()
		{
			return HotkeyManager.IsHotkeyBlocked(m_context);
		}
	}
}
