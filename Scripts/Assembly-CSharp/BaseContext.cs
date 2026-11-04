using System;
using System.Collections;
using System.Collections.Generic;
using Binding.Base;
using DI;
using HUD;
using UnityEngine;

public class BaseContext : ObjectPool
{
	public class ContextAction
	{
		public GameObject template;

		public string buttonText;

		public int buttonWidth;

		public bool isAction;

		public string ico;

		public float icoScaleX = 1f;

		public string scenarioMarker;

		public virtual void Perform(ContextOwner owner)
		{
		}
	}

	public class CallBackAction : ContextAction
	{
		public Action ActionEvent;

		public override void Perform(ContextOwner owner)
		{
			ActionEvent.Call();
		}
	}

	public delegate bool HandleEscapeDelegate();

	private int m_state;

	private IEnumerator m_uiElementsCreatedCoroutine;

	protected UITopPanelContext m_topPanelContext;

	protected UIBottomPanelContext m_bottomPanelContext;

	protected UISidePanelContext m_sidePanelContext;

	public bool dontDestroyByStateChange;

	public bool dontDestroyAlways;

	private List<HotkeyManager.HotKeyAction> m_hotkeys = new List<HotkeyManager.HotKeyAction>();

	private bool m_isHotkeyBlocker;

	private bool m_isRaycastBlocker;

	[Dependency]
	protected GameManager m_gameManager;

	public Action<int> StateChangedEvent;

	public Action<BaseContext> onActiveChanged;

	public HandleEscapeDelegate onHandleEscapeEvent;

	public ContextOwner owner { get; private set; }

	public int state
	{
		get
		{
			return m_state;
		}
		set
		{
			if (m_state != value)
			{
				Type stateEnumType = GetStateEnumType();
				if (stateEnumType == null)
				{
					Debug.LogError("SetState for the stateless context type: " + GetType().Name + " (override GetStateEnumType to make it stateful)");
				}
				else if (!Enum.IsDefined(stateEnumType, value))
				{
					Debug.LogError(value + " is not defined in the " + stateEnumType.Name + " type! Context: " + GetType().Name);
				}
				else
				{
					int num = m_state;
					m_state = value;
					OnStateChanged(num);
					StateChangedEvent.Call(num);
				}
			}
		}
	}

	public bool isActive { get; private set; }

	public bool uiBuilded { get; private set; }

	public void Initialize(ContextOwner contextOwner)
	{
		owner = contextOwner;
		if (m_gameManager == null)
		{
			m_gameManager = DependencyInjector.Resolve<GameManager>();
		}
		HotkeyManager.CollectHotkeys(this, m_hotkeys, ref m_isHotkeyBlocker, ref m_isRaycastBlocker);
	}

	public virtual bool IsHotkeysEnabled()
	{
		if (base.gameObject.activeInHierarchy)
		{
			return isActive;
		}
		return false;
	}

	protected virtual Type GetStateEnumType()
	{
		return null;
	}

	public virtual List<ContextAction> GetAvailableActions()
	{
		return null;
	}

	public void Hide()
	{
		if (isActive)
		{
			owner.Hide(GetType());
		}
	}

	protected virtual void OnStateChanged(int oldState)
	{
	}

	public void BringForwardByDepth()
	{
		RectTransform rectTransform = base.transform as RectTransform;
		if (rectTransform != null)
		{
			rectTransform.SetAsLastSibling();
		}
		ReleaseHotkeys();
		ApplyHotkeys();
	}

	protected void NotifyUiChanged()
	{
		owner.ContextUiChanged(this);
	}

	private IEnumerator UiElementsCreatedCoroutine()
	{
		yield return null;
		yield return new WaitForSeconds(0.01f);
		uiBuilded = true;
		NotifyUiChanged();
	}

	private void RemoveElementsCreatedCoroutine()
	{
		if (m_uiElementsCreatedCoroutine != null)
		{
			TimeController.RemoveCoroutine(m_uiElementsCreatedCoroutine);
			m_uiElementsCreatedCoroutine = null;
		}
	}

	protected void RunUiElementsCreatedCoroutine()
	{
		RemoveElementsCreatedCoroutine();
		uiBuilded = false;
		m_uiElementsCreatedCoroutine = UiElementsCreatedCoroutine();
		TimeController.RunCoroutine(m_uiElementsCreatedCoroutine);
	}

	protected virtual void ShowTopPanel(ETopPanelState state, string title = null, HeaderActionData[] headerActions = null)
	{
		m_topPanelContext = UITopPanelContext.Show(owner);
		if ((bool)m_topPanelContext)
		{
			m_topPanelContext.ShowPanel(state, title, headerActions);
		}
	}

	protected virtual void HideTopPanel()
	{
		if ((bool)m_topPanelContext)
		{
			m_topPanelContext.Hide();
		}
	}

	public virtual bool IsTopOrBottomContext(BaseContext context)
	{
		if (!(context == m_topPanelContext) && !(context == m_bottomPanelContext))
		{
			return context == UIRaceHintsContext.activeInstance;
		}
		return true;
	}

	protected virtual void ShowBottomPanel(IReadOnlyList<ActionHintData> actions, IReadOnlyList<HintOnlyData> hintOnlyDatas = null, bool ignoreRepeat = false)
	{
		m_bottomPanelContext = UIBottomPanelContext.Show(owner, actions, hintOnlyDatas, ignoreRepeat);
	}

	protected virtual void HideBottomPanel()
	{
		if ((bool)m_bottomPanelContext)
		{
			m_bottomPanelContext.Clear();
			m_bottomPanelContext.Hide();
		}
	}

	protected virtual void ShowSidePanel(IReadOnlyList<MultiActionsData> actions, bool ignoreRepeat = false, bool transparent = false)
	{
		m_sidePanelContext = UISidePanelContext.Show(owner, actions, ignoreRepeat, transparent);
	}

	protected virtual void HideSidePanel()
	{
		if ((bool)m_sidePanelContext)
		{
			m_sidePanelContext.Clear();
			m_sidePanelContext.Hide();
		}
	}

	internal void Activate()
	{
		isActive = true;
		OnActivate();
		UIUtils.LoadResources(this);
		RunUiElementsCreatedCoroutine();
		RegisterNavigation();
		onActiveChanged.Call(this);
	}

	internal void Deactivate()
	{
		UnregisterNavigation();
		RemoveElementsCreatedCoroutine();
		UIUtils.UnloadResources(this);
		OnDeactivate();
		isActive = false;
		onActiveChanged.Call(this);
	}

	protected virtual void OnActivate()
	{
		ApplyHotkeys();
	}

	protected virtual void OnDeactivate()
	{
		ReleaseHotkeys();
	}

	protected virtual void OnDestroy()
	{
		UnregisterNavigation();
		ReleaseHotkeys();
	}

	protected void ApplyHotkeys()
	{
		HotkeyManager.RegisterHotkeys(this, m_hotkeys);
	}

	protected void ReleaseHotkeys()
	{
		HotkeyManager.UnregisterHotkeys(this);
	}

	private void RegisterNavigation()
	{
		if ((bool)NavigationInputModule.instance)
		{
			NavigationInputModule.instance.RegisterContext(this as BaseBindingBehaviourTarget);
		}
	}

	private void UnregisterNavigation()
	{
		if ((bool)NavigationInputModule.instance)
		{
			NavigationInputModule.instance.UnregisterContext(this as BaseBindingBehaviourTarget);
		}
	}

	public bool IsHotkeyBlocker()
	{
		return m_isHotkeyBlocker;
	}

	public void SetHotkeyBlocker(bool value)
	{
		m_isHotkeyBlocker = value;
	}

	public bool IsRaycastBlocker()
	{
		return m_isRaycastBlocker;
	}

	public void SetRaycastBlocker(bool value)
	{
		m_isRaycastBlocker = value;
	}

	public void HandleAudioFX()
	{
		if (GetType().GetCustomAttributes(typeof(AudioFXBlockerAttribute), inherit: false) is AudioFXBlockerAttribute[] array && array.Length != 0)
		{
			SoundController.instance.fxAudioPause = array[0].isBlocker;
		}
	}

	public virtual void OnPause()
	{
	}

	public virtual void OnResume()
	{
	}

	protected virtual bool OnEscape()
	{
		return true;
	}

	public bool HandleEscape()
	{
		if (onHandleEscapeEvent != null && !onHandleEscapeEvent())
		{
			return false;
		}
		return OnEscape();
	}
}
