using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CarX;
using Cysharp.Threading.Tasks;
using DI;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

public static class TickManager
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct CustomEarlyUpdateSystem
	{
	}

	private class PriorityAction : IComparable<PriorityAction>
	{
		public int priority;

		public Action action;

		public string name;

		public int CompareTo(PriorityAction priorityAction)
		{
			if (priorityAction == null)
			{
				return 1;
			}
			return priority.CompareTo(priorityAction.priority);
		}
	}

	private static List<PriorityAction> m_earlyUpdateActions;

	private static bool m_earlyUpdateNeedSort;

	private static List<PriorityAction> m_fixedUpdateActions;

	private static bool m_fixedUpdateNeedSort;

	private static List<PriorityAction> m_updateActions;

	private static bool m_updateNeedSort;

	public static bool isInterpolate
	{
		get
		{
			if (m_updateActions.Any((PriorityAction item) => item.name == "ApplyFFB"))
			{
				return m_updateActions.Any((PriorityAction item) => item.name == "ffbManager");
			}
			return false;
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void CreateCustomPlayerLoopSubsystems()
	{
		PlayerLoopSystem root = PlayerLoop.GetDefaultPlayerLoop();
		CreateCustomSystem(ref root, typeof(EarlyUpdate), typeof(CustomEarlyUpdateSystem), CustomEarlyUpdate);
		PlayerLoop.SetPlayerLoop(root);
		PlayerLoopHelper.Initialize(ref root);
	}

	private static void CreateCustomSystem(ref PlayerLoopSystem root, Type parentSysType, Type subSysType, PlayerLoopSystem.UpdateFunction updDelegate)
	{
		PlayerLoopSystem playerLoopSystem = FindSubSystem(root, parentSysType);
		List<PlayerLoopSystem> list = new List<PlayerLoopSystem>(playerLoopSystem.subSystemList.Length + 1);
		list.AddRange(playerLoopSystem.subSystemList);
		list.Add(new PlayerLoopSystem
		{
			updateDelegate = updDelegate,
			type = subSysType
		});
		playerLoopSystem.subSystemList = list.ToArray();
		for (int i = 0; i < root.subSystemList.Length; i++)
		{
			if (root.subSystemList[i].type == parentSysType)
			{
				root.subSystemList[i] = playerLoopSystem;
				break;
			}
		}
		static PlayerLoopSystem FindSubSystem(PlayerLoopSystem result, Type requiredType)
		{
			if (result.type == requiredType)
			{
				return result;
			}
			if (result.subSystemList != null)
			{
				PlayerLoopSystem[] subSystemList = result.subSystemList;
				for (int j = 0; j < subSystemList.Length; j++)
				{
					PlayerLoopSystem result2 = FindSubSystem(subSystemList[j], requiredType);
					if (result2.type == requiredType)
					{
						return result2;
					}
				}
			}
			return default(PlayerLoopSystem);
		}
	}

	static TickManager()
	{
		m_earlyUpdateActions = new List<PriorityAction>(4);
		m_earlyUpdateNeedSort = false;
		m_fixedUpdateActions = new List<PriorityAction>(8);
		m_fixedUpdateNeedSort = false;
		m_updateActions = new List<PriorityAction>(8);
		m_updateNeedSort = false;
		Simulator.SetSelfFixedUpdate(value: false);
		Simulator.SetSelfUpdate(value: false);
		RegisterFixedUpdate(Simulator.UpdateSimulator, 20, "simulator");
		RegisterUpdate(Simulator.UpdateInterpolator, 21, "carxInterpolator");
		TimeController.onFixedUpdate = (Action)Delegate.Combine(TimeController.onFixedUpdate, new Action(FixedUpdate));
		TimeController.onUpdate = (Action)Delegate.Combine(TimeController.onUpdate, new Action(Update));
	}

	private static void UnregisterUpds()
	{
		WheelForceFeedbackSupport wheelForceFeedbackSupport = DependencyInjector.Resolve<WheelForceFeedbackSupport>();
		if (wheelForceFeedbackSupport != null)
		{
			UnregisterFixedUpdate(wheelForceFeedbackSupport.UpdateFFB);
			UnregisterUpdate(wheelForceFeedbackSupport.UpdateFFB);
		}
		if (PlayerCarControl.instance != null)
		{
			UnregisterFixedUpdate(PlayerCarControl.instance.ApplyFFB);
			UnregisterUpdate(PlayerCarControl.instance.ApplyFFB);
		}
	}

	public static void ActivateWithoutInterpolateMode()
	{
		UnregisterUpds();
		WheelForceFeedbackSupport wheelForceFeedbackSupport = DependencyInjector.Resolve<WheelForceFeedbackSupport>();
		RegisterFixedUpdate(PlayerCarControl.instance.ApplyFFB, 30, "ApplyFFB");
		RegisterFixedUpdate(wheelForceFeedbackSupport.UpdateFFB, 40, "ffbManager");
	}

	public static void ActivateInterpolateMode()
	{
		UnregisterUpds();
		WheelForceFeedbackSupport wheelForceFeedbackSupport = DependencyInjector.Resolve<WheelForceFeedbackSupport>();
		if (PlayerCarControl.instance != null)
		{
			RegisterUpdate(PlayerCarControl.instance.ApplyFFB, 30, "ApplyFFB");
		}
		if (wheelForceFeedbackSupport != null)
		{
			RegisterUpdate(wheelForceFeedbackSupport.UpdateFFB, 40, "ffbManager");
		}
	}

	public static void RegisterEarlyUpdate(Action action, int priority, string name)
	{
		if (m_earlyUpdateActions.FindIndex((PriorityAction x) => x.priority == priority) >= 0)
		{
			throw new Exception($"action with priority ({priority}) already exists ");
		}
		m_earlyUpdateActions.Add(new PriorityAction
		{
			action = action,
			priority = priority,
			name = name
		});
		m_earlyUpdateNeedSort = true;
	}

	public static void UnregisterEarlyUpdate(Action action)
	{
		int num = m_earlyUpdateActions.FindIndex((PriorityAction x) => x.action == action);
		if (num >= 0)
		{
			m_earlyUpdateActions.RemoveAt(num);
		}
	}

	private static void CustomEarlyUpdate()
	{
		if (m_earlyUpdateNeedSort)
		{
			m_earlyUpdateActions.Sort();
			m_earlyUpdateNeedSort = false;
		}
		for (int i = 0; i < m_earlyUpdateActions.Count; i++)
		{
			m_earlyUpdateActions[i].action.SafeCall();
		}
	}

	public static void RegisterFixedUpdate(Action action, int priority, string name)
	{
		if (m_fixedUpdateActions.FindIndex((PriorityAction x) => x.priority == priority) >= 0)
		{
			throw new Exception($"action with priority ({priority}) already exists ");
		}
		m_fixedUpdateActions.Add(new PriorityAction
		{
			action = action,
			priority = priority,
			name = name
		});
		m_fixedUpdateNeedSort = true;
	}

	public static void UnregisterFixedUpdate(Action action)
	{
		int num = m_fixedUpdateActions.FindIndex((PriorityAction x) => x.action == action);
		if (num >= 0)
		{
			m_fixedUpdateActions.RemoveAt(num);
		}
	}

	private static void FixedUpdate()
	{
		if (m_fixedUpdateNeedSort)
		{
			m_fixedUpdateActions.Sort();
			m_fixedUpdateNeedSort = false;
		}
		for (int i = 0; i < m_fixedUpdateActions.Count; i++)
		{
			m_fixedUpdateActions[i].action.Call();
		}
	}

	public static void RegisterUpdate(Action action, int priority, string name)
	{
		if (m_updateActions.FindIndex((PriorityAction x) => x.priority == priority) >= 0)
		{
			throw new Exception($"action with priority ({priority}) already exists ");
		}
		m_updateActions.Add(new PriorityAction
		{
			action = action,
			priority = priority,
			name = name
		});
		m_updateNeedSort = true;
	}

	public static void UnregisterUpdate(Action action)
	{
		int num = m_updateActions.FindIndex((PriorityAction x) => x.action == action);
		if (num >= 0)
		{
			m_updateActions.RemoveAt(num);
		}
	}

	private static void Update()
	{
		if (m_updateNeedSort)
		{
			m_updateActions.Sort();
			m_updateNeedSort = false;
		}
		for (int i = 0; i < m_updateActions.Count; i++)
		{
			m_updateActions[i].action.Call();
		}
	}
}
