using System;
using System.Collections;
using System.Collections.Generic;
using DI;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils.SceneManagement;

public class States : MonoBehaviour
{
	[SerializeField]
	private bool m_debugLog;

	[SerializeField]
	private bool m_callOnUpdate = true;

	private Transform m_popupRoot;

	private RootState m_root;

	private BaseState m_current;

	private Dictionary<Type, BaseState> m_states = new Dictionary<Type, BaseState>();

	private BaseState[] m_popupStates;

	private bool m_inited;

	private static Type m_autoStartState;

	public const string CURRENT_TAGNAME = " <Current>";

	private bool state_changing;

	public static States instance { get; private set; }

	public static BaseState current => instance.m_current;

	public static BaseState mainCurrent
	{
		get
		{
			if (instance == null)
			{
				return null;
			}
			BaseState parent = instance.m_current;
			if (parent == null)
			{
				return null;
			}
			while (parent.isPopupState)
			{
				parent = parent.parent;
				if (parent == null)
				{
					return null;
				}
			}
			return parent;
		}
	}

	public IEnumerable<BaseState> allStates => m_states.Values;

	public static event Action onAboutToLoadLevel;

	public static event Action<BaseState> onStateActivate;

	public static BaseState State(Type type)
	{
		return instance.GetStateForType(type);
	}

	public static T State<T>() where T : BaseState
	{
		Type typeFromHandle = typeof(T);
		T val = instance.GetStateForType(typeFromHandle) as T;
		if ((bool)val)
		{
			return val;
		}
		return instance.FindPopupState(typeFromHandle) as T;
	}

	public static bool isInited()
	{
		return instance != null;
	}

	public static void PrepareAutoStartState(Type startStateType)
	{
		m_autoStartState = startStateType;
	}

	public static void UpdateCurrentState()
	{
		if ((bool)instance.m_current)
		{
			instance.m_current.OnUpdate();
		}
	}

	public static bool isLogEnabled()
	{
		if ((bool)instance)
		{
			return instance.m_debugLog;
		}
		return false;
	}

	private void Awake()
	{
		Init();
	}

	private static void Init()
	{
		if (instance != null)
		{
			throw new Exception("States allready inited!");
		}
		instance = UnityEngine.Object.FindObjectOfType(typeof(States)) as States;
		if (instance == null)
		{
			throw new Exception("States not found in scene!");
		}
		instance.InitStates();
	}

	public static void Change(Type stateType)
	{
		instance.ChangeState(stateType);
	}

	public static void Change<TState>() where TState : BaseState
	{
		instance.ChangeState(typeof(TState));
	}

	public static void Change(BaseState state)
	{
		instance.ChangeState(state.GetType());
	}

	public static bool TryRestart<TState>() where TState : BaseState
	{
		return instance.TryRestartState(typeof(TState));
	}

	public static void Push<TState>() where TState : BaseState
	{
		instance.PushState(typeof(TState));
	}

	public static void Pop()
	{
		instance.PopState();
	}

	private static bool LoadStateScene(BaseState stateToLoad)
	{
		if (string.IsNullOrEmpty(stateToLoad.stateScene) || SceneManager.GetActiveScene().name == stateToLoad.stateScene)
		{
			return false;
		}
		State<LoadSceneState>().LoadSceneAndSetState(stateToLoad);
		return true;
	}

	public static void Spawn(States prefab)
	{
		if (!isInited())
		{
			States states = UnityEngine.Object.Instantiate(prefab);
			states.name = states.name.Substring(0, states.name.IndexOf('('));
		}
	}

	private void ChangeState(string stateType)
	{
		if (!string.IsNullOrEmpty(stateType))
		{
			Type type = Type.GetType(stateType, throwOnError: false);
			if (type == null)
			{
				GameLog.Print(AlertLevel.Error, "States", $"[States] Type for state \"{stateType}\" not found!");
			}
			else
			{
				ChangeState(type);
			}
		}
	}

	private void OnEnable()
	{
		instance = this;
	}

	private void InitStates()
	{
		if (m_inited)
		{
			return;
		}
		m_inited = true;
		FindRoot();
		BaseState[] componentsInChildren = m_root.GetComponentsInChildren<BaseState>(includeInactive: true);
		foreach (BaseState baseState in componentsInChildren)
		{
			Type type = baseState.GetType();
			if (m_states.ContainsKey(type))
			{
				throw new Exception("Dublicates states with Type: " + type?.ToString() + ". Use State with parameters or Popup state.");
			}
			m_states.Add(type, baseState);
			SetStateActive(baseState, activeFlag: false, enableState: false);
			baseState.OnInit();
		}
		m_popupRoot = base.transform.Find("PopupStates");
		if (m_popupRoot == null)
		{
			throw new Exception("PopupStates child not found");
		}
		m_popupStates = m_popupRoot.GetComponentsInChildren<BaseState>(includeInactive: true);
		m_popupRoot.gameObject.SetActive(value: false);
		for (int j = 0; j < m_popupStates.Length; j++)
		{
			BaseState obj = m_popupStates[j];
			obj.isPopupState = true;
			SetStateActive(obj, activeFlag: false, enableState: false);
			obj.OnInit();
		}
		SetCurrent(m_root);
		EnterToState(m_current);
	}

	private static void EnterToState(BaseState stateToEnter)
	{
		stateToEnter.OnEnter();
		States.onStateActivate.SafeCall(stateToEnter);
	}

	private void Start()
	{
		SetStartState();
	}

	private void SetStartState()
	{
		if (m_autoStartState == null)
		{
			StatesStarter obj = UnityEngine.Object.FindObjectOfType(typeof(StatesStarter)) as StatesStarter;
			if (obj == null)
			{
				throw new Exception("Use StatesStarter for set Start state");
			}
			m_autoStartState = obj.startStateType;
		}
		if (m_autoStartState != null)
		{
			AutoStartState();
			m_autoStartState = null;
		}
	}

	private void AutoStartState()
	{
		if (m_current is RootState && m_autoStartState != null)
		{
			if (isLogEnabled())
			{
				GameLog.Print(AlertLevel.Notify, "States", $"[States] Start state: {m_autoStartState}");
			}
			ChangeState(m_autoStartState);
		}
	}

	private void SetCurrent(BaseState state)
	{
		m_current = state;
		SetStateActive(m_current, activeFlag: true, enableState: true);
		SetCurrentTagName(m_current, flag: true);
	}

	private void FindRoot()
	{
		m_root = GetComponentInChildren<RootState>();
		if (m_root == null)
		{
			throw new Exception("Root not found in " + base.name);
		}
	}

	protected void ChangeState(Type stateType)
	{
		DisableInputTemporally();
		BaseState baseState = FindState(stateType);
		if (LoadStateScene(baseState))
		{
			return;
		}
		VerifyNestedCall();
		try
		{
			if (baseState == m_current)
			{
				return;
			}
			if (baseState.isPopupState)
			{
				throw new Exception(baseState?.ToString() + " is popup");
			}
			if (ChangeStackToState(baseState))
			{
				return;
			}
		}
		finally
		{
			state_changing = false;
		}
		throw new Exception("Cannot Change to " + baseState);
	}

	private void DisableInputTemporally()
	{
		HotkeyManager.Sleep(0.2f);
	}

	private bool TryRestartState(Type stateType)
	{
		BaseState baseState = FindState(stateType);
		if (baseState == null || current != baseState)
		{
			return false;
		}
		baseState.OnExit();
		EnterToState(baseState);
		return true;
	}

	protected void PushState(Type stateType)
	{
		VerifyNestedCall();
		try
		{
			BaseState baseState = FindPopupState(stateType);
			if (!(baseState == m_current))
			{
				if (!baseState.isPopupState)
				{
					throw new Exception(baseState?.ToString() + " is not popup");
				}
				_ = baseState.transform.parent != m_popupRoot;
				Push(baseState, pause_current: true);
			}
		}
		finally
		{
			state_changing = false;
		}
	}

	protected BaseState FindPopupState(Type stateType)
	{
		for (int i = 0; i < m_popupStates.Length; i++)
		{
			BaseState baseState = m_popupStates[i];
			if (baseState.GetType() == stateType)
			{
				return baseState;
			}
		}
		throw new Exception("Popup State < " + stateType?.ToString() + " > not found");
	}

	private void Push(BaseState state, bool pause_current)
	{
		BaseState baseState = m_current;
		if (m_current != null && pause_current)
		{
			m_current.OnPaused();
			SetCurrentTagName(m_current, flag: false);
			SetStateActive(m_current, activeFlag: true, enableState: false);
		}
		if (state.isPopupState)
		{
			state.transform.parent = baseState.transform;
		}
		SetCurrent(state);
		EnterToState(m_current);
	}

	protected void PopState()
	{
		VerifyNestedCall();
		try
		{
			FlushTopState();
			SetCurrent(m_current);
			m_current.OnResume();
			States.onStateActivate.SafeCall(m_current);
		}
		finally
		{
			state_changing = false;
		}
	}

	private void FlushTopState()
	{
		m_current.OnExit();
		SetCurrentTagName(m_current, flag: false);
		SetStateActive(m_current, activeFlag: false, enableState: false);
		BaseState parent = m_current.parent;
		if (m_current.isPopupState)
		{
			m_current.transform.parent = m_popupRoot;
		}
		m_current = parent;
	}

	private bool ChangeStackToState(BaseState state)
	{
		BaseState baseState = m_current.FindParentStateInStack(state);
		bool pause_current = baseState == m_current;
		if (baseState != null)
		{
			while (m_current != baseState)
			{
				FlushTopState();
			}
			if (m_current != state)
			{
				Push(state, pause_current);
			}
			else
			{
				SetCurrent(m_current);
				m_current.OnResume();
			}
			return true;
		}
		return false;
	}

	private BaseState GetStateForType(Type stateType)
	{
		BaseState value = null;
		m_states.TryGetValue(stateType, out value);
		return value;
	}

	private BaseState FindState(Type stateType)
	{
		BaseState stateForType = GetStateForType(stateType);
		if (stateForType == null)
		{
			state_changing = false;
			throw new Exception("State < " + stateType?.ToString() + " > not found");
		}
		return stateForType;
	}

	private static void SetCurrentTagName(BaseState state, bool flag)
	{
		int num = state.name.IndexOf(" <Current>");
		if (flag)
		{
			if (num == -1)
			{
				state.name += " <Current>";
			}
		}
		else if (num > 0)
		{
			state.name = state.name.Substring(0, num);
		}
	}

	private static void SetStateActive(BaseState state, bool activeFlag, bool enableState)
	{
		state.gameObject.SetActive(activeFlag);
		state.enabled = enableState;
	}

	private void VerifyNestedCall()
	{
		if (state_changing)
		{
			throw new Exception("Nested call in state changing!");
		}
		state_changing = true;
	}

	private void Update()
	{
		if (m_callOnUpdate && (bool)m_current)
		{
			m_current.OnUpdate();
		}
	}

	private void FixedUpdate()
	{
		if (m_callOnUpdate && (bool)m_current)
		{
			m_current.OnFixedUpdate();
		}
	}

	private void LateUpdate()
	{
		if (m_callOnUpdate && (bool)m_current)
		{
			m_current.OnLateUpdate();
		}
	}

	public static void InitiateLevelLoading(string sceneName)
	{
		States.onAboutToLoadLevel.SafeCall();
		SceneLoader.LoadScene(sceneName);
	}

	public static IEnumerator InitiateLevelLoadingAsync(string sceneName)
	{
		States.onAboutToLoadLevel.SafeCall();
		return SceneLoader.LoadSceneAsync(sceneName);
	}

	public static Coroutine ClearMemory()
	{
		return TimeController.StartNow(ClearMemoryCoroutine());
	}

	private static IEnumerator ClearMemoryCoroutine()
	{
		DependencyInjector.ClearTransient();
		yield return ResourcesWrapper.UnloadUnusedAssets();
		GC.Collect(2, GCCollectionMode.Forced);
		GC.WaitForPendingFinalizers();
		yield return null;
	}
}
