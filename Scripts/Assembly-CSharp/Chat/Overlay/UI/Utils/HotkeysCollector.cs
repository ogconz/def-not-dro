using System;
using System.Collections.Generic;
using GameInput;

namespace Chat.Overlay.UI.Utils
{
	public class HotkeysCollector
	{
		private delegate HotkeyManager.HotKeyAction CollectHotkeyDelegate();

		private List<HotkeyManager.HotKeyAction> m_buffer = new List<HotkeyManager.HotKeyAction>(8);

		private int m_delegatesCount;

		private bool m_isDirty;

		private event CollectHotkeyDelegate onCollectHotkeysEvent;

		public void Add(ActionType actionType, System.Action action, bool repeat)
		{
			onCollectHotkeysEvent += ConstructCollectHotkey(actionType, action, repeat);
			m_delegatesCount++;
			m_isDirty = true;
		}

		public void Remove(ActionType actionType, System.Action action)
		{
			CollectHotkeyDelegate collectHotkeyDelegate = FindExistingCollectDelegate(actionType, action);
			if (collectHotkeyDelegate != null)
			{
				onCollectHotkeysEvent -= collectHotkeyDelegate;
				m_delegatesCount--;
				m_isDirty = true;
			}
		}

		public int GetRawCount()
		{
			return m_delegatesCount;
		}

		public int GetCollectedCount()
		{
			return CollectHotkeys().Count;
		}

		public List<HotkeyManager.HotKeyAction> CollectHotkeys()
		{
			if (!m_isDirty)
			{
				return m_buffer;
			}
			m_buffer.Clear();
			Delegate[] invocationList = this.onCollectHotkeysEvent.GetInvocationList();
			foreach (Delegate rawDelegate in invocationList)
			{
				if (TryGetHotkeyDataFromDelegate(rawDelegate, out var hotkey))
				{
					m_buffer.Add(hotkey);
				}
			}
			m_isDirty = false;
			return m_buffer;
		}

		private CollectHotkeyDelegate ConstructCollectHotkey(ActionType actionType, System.Action action, bool repeat)
		{
			HotkeyManager.HotKeyAction hotkey = new HotkeyManager.HotKeyAction(action, actionType, string.Empty, skipInputFX: false, repeat);
			return GetHotkeyAction;
			HotkeyManager.HotKeyAction GetHotkeyAction()
			{
				return hotkey;
			}
		}

		private bool TryGetHotkeyDataFromDelegate(Delegate rawDelegate, out HotkeyManager.HotKeyAction hotkey)
		{
			try
			{
				CollectHotkeyDelegate collectHotkeyDelegate = (CollectHotkeyDelegate)rawDelegate;
				hotkey = collectHotkeyDelegate();
				return hotkey != null;
			}
			catch (Exception)
			{
				hotkey = null;
				return false;
			}
		}

		private CollectHotkeyDelegate FindExistingCollectDelegate(ActionType actionType, System.Action action)
		{
			if (this.onCollectHotkeysEvent == null)
			{
				return null;
			}
			Delegate[] invocationList = this.onCollectHotkeysEvent.GetInvocationList();
			foreach (Delegate obj in invocationList)
			{
				try
				{
					CollectHotkeyDelegate collectHotkeyDelegate = (CollectHotkeyDelegate)obj;
					HotkeyManager.HotKeyAction hotKeyAction = collectHotkeyDelegate();
					if (actionType == hotKeyAction.type && action == hotKeyAction.actionEvent)
					{
						return collectHotkeyDelegate;
					}
				}
				catch (Exception)
				{
				}
			}
			return null;
		}
	}
}
