using System;
using System.Collections.Generic;
using System.Reflection;
using Binding.Components;
using GameInput;
using UnityEngine;

public class HotkeyManager
{
	public class HotKeyAction
	{
		public System.Action actionEvent;

		public ActionType type;

		public string methodName;

		public bool skipInputFX;

		public bool repeat;

		public HotKeyAction(System.Action action, ActionType type, string methodName, bool skipInputFX, bool repeat)
		{
			actionEvent = action;
			this.type = type;
			this.methodName = methodName;
			this.skipInputFX = skipInputFX;
			this.repeat = repeat;
		}
	}

	private class ActiveHotkey
	{
		public class Node
		{
			public BaseContext owner;

			public HotKeyAction hotKey;

			public ActionElementBinding elementOwner;

			public Node(BaseContext context, HotKeyAction hotKey, ActionElementBinding element = null)
			{
				owner = context;
				this.hotKey = hotKey;
				elementOwner = element;
			}

			public bool IsGenericHotkey()
			{
				return elementOwner == null;
			}

			public bool IsActionElementHotkey()
			{
				return elementOwner != null;
			}
		}

		public ActionType type;

		public List<Node> nodes = new List<Node>();

		public float m_nextActionTime;

		public float m_delay;

		public ActiveHotkey(ActionType type)
		{
			this.type = type;
		}

		public bool CallActionHold(int generation)
		{
			if (Time.unscaledTime > m_nextActionTime)
			{
				m_lastActionCollectionGeneration = m_collectionGeneration;
				m_nextActionTime = Time.unscaledTime + 0.08f;
				BaseContext baseContext = m_ownerStack.Last();
				bool flag = baseContext != null && baseContext.IsHotkeyBlocker();
				bool flag2 = baseContext != null && baseContext.IsRaycastBlocker();
				Node node = null;
				for (int num = nodes.Count - 1; num >= 0; num--)
				{
					Node node2 = nodes[num];
					if (node2.hotKey.repeat)
					{
						if (node2.IsActionElementHotkey())
						{
							if ((!flag2 || IsActionElementEnabled(baseContext, node2.owner)) && node2.elementOwner.isActiveAndEnabled)
							{
								node2.elementOwner.OnClickNoEffect();
								break;
							}
						}
						else
						{
							if (node == null && (!flag || !(node2.owner != baseContext)) && node2.owner != null && node2.owner.IsHotkeysEnabled())
							{
								node = node2;
							}
							if (generation != m_collectionGeneration)
							{
								break;
							}
						}
					}
				}
				if (node != null)
				{
					node.hotKey.actionEvent.SafeCall();
					return true;
				}
			}
			return false;
		}

		public bool CallAction(bool buttonPressed, int generation)
		{
			if (m_lastActionCollectionGeneration != m_collectionGeneration && !buttonPressed)
			{
				return false;
			}
			bool flag = false;
			Node node = null;
			if (buttonPressed || Time.unscaledTime > m_nextActionTime)
			{
				m_lastActionCollectionGeneration = m_collectionGeneration;
				if (buttonPressed)
				{
					m_delay = 0.3f;
				}
				else
				{
					m_delay *= 0.75f;
					if (m_delay < 0.08f)
					{
						m_delay = 0.08f;
					}
				}
				m_nextActionTime = Time.unscaledTime + m_delay;
				BaseContext baseContext = m_ownerStack.Last();
				bool flag2 = baseContext != null && baseContext.IsHotkeyBlocker();
				bool flag3 = baseContext != null && baseContext.IsRaycastBlocker();
				for (int num = nodes.Count - 1; num >= 0; num--)
				{
					Node node2 = nodes[num];
					if (node2.IsActionElementHotkey() && (!flag3 || IsActionElementEnabled(baseContext, node2.owner)) && node2.elementOwner.isActiveAndEnabled)
					{
						node2.elementOwner.OnClick();
						flag = true;
						break;
					}
					if (node == null && node2.IsGenericHotkey() && (!flag2 || !(node2.owner != baseContext)) && node2.owner != null && node2.owner.IsHotkeysEnabled())
					{
						node = node2;
					}
					if (generation != m_collectionGeneration)
					{
						break;
					}
				}
				if (!flag && node != null)
				{
					if (!node.hotKey.skipInputFX)
					{
						InputManager.instance.PlayHotkeyPressedEffect(type, node.hotKey.actionEvent);
					}
					else
					{
						node.hotKey.actionEvent.SafeCall();
					}
					flag = true;
				}
			}
			return flag;
		}

		public static bool IsActionElementEnabled(BaseContext top, BaseContext context)
		{
			if (top != null)
			{
				if (top == context)
				{
					return true;
				}
				return top.IsTopOrBottomContext(context);
			}
			return false;
		}
	}

	public const float ACTION_DELAY = 0.3f;

	public const float REPEAT_FACTOR = 0.75f;

	public const float MIN_DELAY = 0.08f;

	private static List<ActiveHotkey> m_activeHotkeys = new List<ActiveHotkey>();

	private static List<BaseContext> m_ownerStack = new List<BaseContext>();

	private static WeakReference lastRemoved = null;

	private static int m_collectionGeneration = 0;

	private static int m_lastActionCollectionGeneration = -1;

	private static List<ActiveHotkey> m_deadList = new List<ActiveHotkey>();

	private static InputManager m_inputManager;

	private static float m_switchContextDelay = 0f;

	private static float m_wakeupTime = 0f;

	public static event System.Action onOwnerStackChangedEvent;

	public static void Sleep(float time = 0.1f)
	{
		m_wakeupTime = Time.unscaledTime + time;
	}

	public static bool IsSleeping()
	{
		return Time.unscaledTime < m_wakeupTime;
	}

	public static bool CanClick(ActionElementBinding element)
	{
		if (element == null)
		{
			return false;
		}
		BaseContext parentBaseContext = element.GetParentBaseContext();
		BaseContext baseContext = m_ownerStack.Last();
		bool flag = baseContext != null && baseContext.IsRaycastBlocker();
		if (!(parentBaseContext == baseContext) && flag)
		{
			return ActiveHotkey.IsActionElementEnabled(baseContext, parentBaseContext);
		}
		return true;
	}

	public static bool IsHotkeyBlocked(BaseContext owner)
	{
		if (!m_ownerStack.Contains(owner))
		{
			return false;
		}
		BaseContext baseContext = m_ownerStack.Last();
		if (owner != baseContext && baseContext != null)
		{
			return baseContext.IsHotkeyBlocker();
		}
		return false;
	}

	public static void FixedUpdate()
	{
		if (Time.unscaledTime < m_wakeupTime)
		{
			return;
		}
		if (m_inputManager == null)
		{
			m_inputManager = InputManager.instance;
		}
		if (m_inputManager == null || InputManager.IsInputLocked())
		{
			return;
		}
		m_deadList.Clear();
		List<ActiveHotkey> activeHotkeys = m_activeHotkeys;
		int collectionGeneration = m_collectionGeneration;
		for (int num = activeHotkeys.Count - 1; num >= 0; num--)
		{
			ActiveHotkey activeHotkey = activeHotkeys[num];
			if (activeHotkey != null)
			{
				bool flag = m_inputManager.IsActionPressed(activeHotkey.type);
				if ((flag && activeHotkey.CallAction(flag, collectionGeneration)) || (m_switchContextDelay > 0.75f && m_inputManager.IsActionHold(activeHotkey.type) && activeHotkey.CallActionHold(collectionGeneration)))
				{
					break;
				}
				m_switchContextDelay += Time.fixedDeltaTime;
				if (collectionGeneration != m_collectionGeneration)
				{
					break;
				}
			}
		}
	}

	public static void CollectHotkeys(BaseContext context, List<HotKeyAction> hotKeys, ref bool isHotkeyBlocker, ref bool isRaycastBlocker)
	{
		Type type = context.GetType();
		hotKeys.Clear();
		MethodInfo[] cachedMethods = type.GetCachedMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (MethodInfo methodInfo in cachedMethods)
		{
			if (methodInfo.GetParameters().Length == 0)
			{
				GameHotkeyAttribute[] attributes = GameHotkeyAttribute.GetAttributes(methodInfo);
				foreach (GameHotkeyAttribute gameHotkeyAttribute in attributes)
				{
					HotKeyAction item = new HotKeyAction((System.Action)Delegate.CreateDelegate(typeof(System.Action), context, methodInfo), gameHotkeyAttribute.actionType, methodInfo.Name, !gameHotkeyAttribute.skipInputFX, gameHotkeyAttribute.repeat);
					hotKeys.Add(item);
				}
			}
		}
		if (type.IsDefined(typeof(GameHotkeyBlockerAttribute), inherit: false))
		{
			GameHotkeyBlockerAttribute[] array = type.GetCustomAttributes(typeof(GameHotkeyBlockerAttribute), inherit: false) as GameHotkeyBlockerAttribute[];
			if (array.Length != 0)
			{
				isHotkeyBlocker = array[0].isBlocker;
			}
		}
		if (type.IsDefined(typeof(RaycastBlockerAttribute), inherit: false) && type.GetCustomAttributes(typeof(RaycastBlockerAttribute), inherit: false) is RaycastBlockerAttribute[] array2 && array2.Length != 0)
		{
			isRaycastBlocker = array2[0].isBlocker;
		}
	}

	public static void RegisterHotkeys(BaseContext owner, List<HotKeyAction> hotkeys)
	{
		if (hotkeys.Count == 0 && !owner.IsHotkeyBlocker() && !owner.IsRaycastBlocker())
		{
			return;
		}
		UnregisterHotkeys(owner);
		lastRemoved = null;
		m_ownerStack.Add(owner);
		HotkeyManager.onOwnerStackChangedEvent.SafeCall();
		foreach (HotKeyAction hotkey in hotkeys)
		{
			GetOrCreateActiveHotkey(hotkey.type).nodes.Add(new ActiveHotkey.Node(owner, hotkey));
		}
		m_collectionGeneration++;
	}

	public static void UnregisterHotkeys(BaseContext owner)
	{
		if (lastRemoved != null && lastRemoved.Target == owner)
		{
			return;
		}
		lastRemoved = new WeakReference(owner);
		m_ownerStack.Remove(owner);
		HotkeyManager.onOwnerStackChangedEvent.SafeCall();
		foreach (ActiveHotkey activeHotkey in m_activeHotkeys)
		{
			activeHotkey.nodes.RemoveAll((ActiveHotkey.Node n) => n.IsGenericHotkey() && n.owner == owner);
		}
		RemoveEmptyHotkeys();
		m_collectionGeneration++;
	}

	public static void UpdateHotkeys(BaseContext owner, List<HotKeyAction> hotkeys)
	{
		if (hotkeys.Count == 0 || !m_ownerStack.Contains(owner))
		{
			return;
		}
		foreach (ActiveHotkey activeHotkey in m_activeHotkeys)
		{
			activeHotkey.nodes.RemoveAll((ActiveHotkey.Node n) => n.IsGenericHotkey() && n.owner == owner);
		}
		RemoveEmptyHotkeys();
		foreach (HotKeyAction hotkey in hotkeys)
		{
			GetOrCreateActiveHotkey(hotkey.type).nodes.Add(new ActiveHotkey.Node(owner, hotkey));
		}
		m_collectionGeneration++;
	}

	private static void RemoveEmptyHotkeys()
	{
		m_activeHotkeys.RemoveAll(delegate(ActiveHotkey a)
		{
			if (a.nodes.Count == 0)
			{
				m_deadList.Add(a);
				return true;
			}
			return false;
		});
		m_switchContextDelay = 0f;
	}

	public static void RegisterHotkey(ActionElementBinding element, bool ignoreRepeat = false)
	{
		BaseContext parentBaseContext = element.GetParentBaseContext();
		ActiveHotkey orCreateActiveHotkey = GetOrCreateActiveHotkey(element.actionType);
		HotKeyAction hotkey = element.GetHotkey();
		ActiveHotkey.Node item = new ActiveHotkey.Node(parentBaseContext, hotkey, element);
		orCreateActiveHotkey.nodes.Add(item);
		m_collectionGeneration++;
	}

	public static void UnregisterHotkey(ActionElementBinding element)
	{
		foreach (ActiveHotkey activeHotkey in m_activeHotkeys)
		{
			activeHotkey.nodes.RemoveAll((ActiveHotkey.Node n) => n.elementOwner == element);
		}
		RemoveEmptyHotkeys();
		m_collectionGeneration++;
	}

	public static bool IsHokeyRegistered(ActionType type)
	{
		if (m_deadList.Find((ActiveHotkey a) => a.type == type) != null)
		{
			return true;
		}
		return m_activeHotkeys.Find((ActiveHotkey a) => a.type == type) != null;
	}

	private static ActiveHotkey GetOrCreateActiveHotkey(ActionType type)
	{
		ActiveHotkey activeHotkey = m_activeHotkeys.Find((ActiveHotkey a) => a.type == type);
		if (activeHotkey == null)
		{
			activeHotkey = new ActiveHotkey(type);
			m_activeHotkeys.Add(activeHotkey);
		}
		return activeHotkey;
	}
}
