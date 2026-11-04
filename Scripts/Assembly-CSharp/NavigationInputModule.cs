using System.Collections.Generic;
using Binding.Base;
using GameInput;
using Rewired;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NavigationInputModule : MonoBehaviour
{
	public const float REUSE_SUBMIT_TIME = 0.3f;

	private static NavigationInputModule s_instance;

	private EventSystem m_eventSystem;

	private GameInput.InputManager m_inputManager;

	public GraphicRaycaster raycaster;

	private PointerEventData m_pointerEventData;

	private readonly List<RaycastResult> m_raycastResults = new List<RaycastResult>(8);

	private ElementNavigate m_lastUnderMouseObject;

	private bool m_isLastUnderMouseObjectInvalidated;

	private BaseBindingBehaviourTarget m_lastUnderMouseHost;

	private ElementNavigate m_lastLockedObject;

	private BaseBindingBehaviourTarget m_lastLockedHost;

	private float m_ignoreSubmitsTime;

	private Dictionary<NavigateDirection, ActionType> m_navigateActionsMap = new Dictionary<NavigateDirection, ActionType>
	{
		[NavigateDirection.Up] = ActionType.NavUp,
		[NavigateDirection.Down] = ActionType.NavDown,
		[NavigateDirection.Left] = ActionType.NavLeft,
		[NavigateDirection.Right] = ActionType.NavRight
	};

	private readonly List<BaseBindingBehaviourTarget> m_activeContexts = new List<BaseBindingBehaviourTarget>(8);

	private Vector3 m_lastMouseCoords;

	private float m_epsCoordsEquals = 0.01f;

	private int m_skipFrameCounter = 1;

	public static NavigationInputModule instance => s_instance ?? (s_instance = Object.FindObjectOfType<NavigationInputModule>());

	private void Awake()
	{
		s_instance = this;
		m_eventSystem = GetComponent<EventSystem>();
		m_pointerEventData = new PointerEventData(m_eventSystem);
	}

	private void Update()
	{
		m_ignoreSubmitsTime -= ((Time.timeScale < 1f) ? Time.unscaledDeltaTime : Time.deltaTime);
		if (m_ignoreSubmitsTime < 0f)
		{
			m_ignoreSubmitsTime = 0f;
		}
		if (m_activeContexts.Count != 0 && Time.timeScale == 0f)
		{
			UpdateInput();
		}
	}

	private void FixedUpdate()
	{
		UpdateInput();
	}

	private void UpdateInput()
	{
		if (GameInput.InputManager.IsInputLocked() || KeepAliveManager.isUIHidden)
		{
			return;
		}
		if (m_inputManager == null)
		{
			m_inputManager = GameInput.InputManager.instance;
		}
		if (m_skipFrameCounter < 1)
		{
			m_skipFrameCounter++;
		}
		else if (m_activeContexts.Count != 0 && m_inputManager != null)
		{
			UpdateNavigation();
			if (CanRaycast())
			{
				UpdateMouse();
			}
		}
	}

	private bool CanRaycast()
	{
		for (int num = m_activeContexts.Count - 1; num >= 0; num--)
		{
			if (m_activeContexts[num].IsRaycastBlocker())
			{
				return false;
			}
		}
		return true;
	}

	private void UpdateNavigation()
	{
		for (int num = m_activeContexts.Count - 1; num >= 0; num--)
		{
			if (!HotkeyManager.IsHotkeyBlocked(m_activeContexts[num]))
			{
				UpdateNavigateActions(m_activeContexts[num]);
				if (m_activeContexts[num].IsHotkeyBlocker())
				{
					break;
				}
			}
		}
	}

	private void UpdateMouse()
	{
		if (m_pointerEventData == null)
		{
			return;
		}
		m_pointerEventData.position = ReInput.controllers.Mouse.screenPosition;
		if (m_isLastUnderMouseObjectInvalidated || Vector3.Distance(m_pointerEventData.position, m_lastMouseCoords) > m_epsCoordsEquals)
		{
			m_isLastUnderMouseObjectInvalidated = false;
			UpdateLastUnderMouseObject();
		}
		if (m_inputManager.IsActionPressed(ActionType.NavMouseSelect))
		{
			UpdateLastUnderMouseObject();
			m_lastLockedObject = m_lastUnderMouseObject;
			m_lastLockedHost = m_lastUnderMouseHost;
		}
		if (m_inputManager.IsActionReleased(ActionType.NavMouseSelect))
		{
			if (m_lastLockedObject == m_lastUnderMouseObject && m_ignoreSubmitsTime == 0f)
			{
				MouseSubmit();
				m_ignoreSubmitsTime = 0.3f;
			}
			if (m_lastLockedObject == null)
			{
				m_ignoreSubmitsTime = 0.3f;
			}
		}
	}

	private void UpdateNavigateActions(BaseBindingBehaviourTarget context)
	{
		if (!context.IsAllowNavigation())
		{
			return;
		}
		foreach (KeyValuePair<NavigateDirection, ActionType> item in m_navigateActionsMap)
		{
			var (direction, actionType2) = (KeyValuePair<NavigateDirection, ActionType>)(ref item);
			if (m_inputManager.IsActionPressed(actionType2))
			{
				m_inputManager.PlayNavigateEffect(null, actionType2, InvokeSelectionMove);
				m_ignoreSubmitsTime = 0f;
			}
			void InvokeSelectionMove()
			{
				context.SelectionMove(direction);
				m_lastLockedHost = context;
				m_lastLockedObject = context.selectedNavigate;
			}
		}
	}

	private void UpdateLastUnderMouseObject()
	{
		m_lastUnderMouseObject = null;
		m_lastUnderMouseHost = null;
		if (m_activeContexts.Count == 0)
		{
			return;
		}
		m_pointerEventData.position = ReInput.controllers.Mouse.screenPosition;
		m_lastMouseCoords = ReInput.controllers.Mouse.screenPosition;
		m_raycastResults.Clear();
		raycaster.Raycast(m_pointerEventData, m_raycastResults);
		if (m_raycastResults.Count == 0)
		{
			foreach (BaseBindingBehaviourTarget activeContext in m_activeContexts)
			{
				GraphicRaycaster graphicRaycaster = activeContext.raycaster;
				if (graphicRaycaster != null)
				{
					graphicRaycaster.Raycast(m_pointerEventData, m_raycastResults);
					if (m_raycastResults.Count > 0)
					{
						break;
					}
				}
			}
		}
		ContextOwner ownerContext = GetOwnerContext();
		if (ownerContext != null && ownerContext.topContext != null && ownerContext.topContext.IsRaycastBlocker())
		{
			BaseBindingBehaviourTarget baseBindingBehaviourTarget = ownerContext.topContext as BaseBindingBehaviourTarget;
			if (!(baseBindingBehaviourTarget == null))
			{
				ProcessRaycast(baseBindingBehaviourTarget);
			}
			return;
		}
		foreach (BaseBindingBehaviourTarget activeContext2 in m_activeContexts)
		{
			ProcessRaycast(activeContext2);
		}
	}

	private void ProcessRaycast(BaseBindingBehaviourTarget context)
	{
		context.CheckRaycast(m_raycastResults);
		bool flag = false;
		if (context.lastHoveredNavElement != null)
		{
			foreach (RaycastResult raycastResult in m_raycastResults)
			{
				if (raycastResult.gameObject.GetInstanceID() == context.lastHoveredNavElement.gameObject.GetInstanceID())
				{
					flag = true;
					break;
				}
			}
		}
		if (flag)
		{
			m_lastUnderMouseObject = context.lastHoveredNavElement;
			m_lastUnderMouseHost = context;
		}
	}

	public void ForceUpdateMousePosition()
	{
		m_isLastUnderMouseObjectInvalidated = true;
	}

	public void Submit()
	{
		m_isLastUnderMouseObjectInvalidated = true;
		UpdateLockedObjects();
		if (m_lastLockedHost != null)
		{
			m_lastLockedHost.SubmitCurrentSelected();
		}
	}

	public void MouseSubmit()
	{
		if (m_pointerEventData != null)
		{
			m_isLastUnderMouseObjectInvalidated = true;
			if (!(m_lastLockedHost == null) && !(m_lastLockedObject == null))
			{
				m_pointerEventData.position = ReInput.controllers.Mouse.screenPosition;
				m_lastMouseCoords = ReInput.controllers.Mouse.screenPosition;
				GameInput.InputManager.instance.PlayNavigateEffect(m_lastLockedObject, ActionType.NavMouseSelect, m_lastLockedObject.Submit);
			}
		}
	}

	private ContextOwner GetOwnerContext()
	{
		BaseGameState baseGameState = States.current as BaseGameState;
		if (!(baseGameState != null))
		{
			return null;
		}
		return baseGameState.contextOwner;
	}

	private void UpdateLockedObjects()
	{
		m_lastLockedHost = null;
		m_lastLockedObject = null;
		if (m_activeContexts.Count == 0)
		{
			return;
		}
		for (int num = m_activeContexts.Count - 1; num >= 0; num--)
		{
			BaseBindingBehaviourTarget baseBindingBehaviourTarget = m_activeContexts[num];
			if (baseBindingBehaviourTarget.IsAllowNavigation() && baseBindingBehaviourTarget.selectedNavigate != null)
			{
				m_lastLockedHost = baseBindingBehaviourTarget;
				m_lastLockedObject = baseBindingBehaviourTarget.selectedNavigate;
				break;
			}
		}
	}

	public void UnregisterContext(BaseBindingBehaviourTarget context)
	{
		if (!(context == null))
		{
			m_activeContexts.Remove(context);
		}
	}

	public void RegisterContext(BaseBindingBehaviourTarget context)
	{
		if (!(context == null))
		{
			if (!m_activeContexts.Contains(context))
			{
				m_activeContexts.Add(context);
			}
			else if (m_activeContexts.IndexOf(context) != m_activeContexts.Count - 1)
			{
				m_activeContexts.Remove(context);
				m_activeContexts.Add(context);
			}
			UpdateLockedObjects();
			m_skipFrameCounter = 0;
		}
	}
}
