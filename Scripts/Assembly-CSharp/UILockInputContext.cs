using System.Collections;
using Binding;
using Binding.Base;
using GameInput;
using GameOverlay;
using UnityEngine;

public class UILockInputContext : BaseBindingBehaviourTarget
{
	private const float DELAY_TO_SAFE_CLOSE = 20f;

	private static ContextOwner m_owner;

	private static bool m_showBgAtStart = true;

	private Coroutine m_autoCloseCoroutine;

	private readonly BoolProperty m_showBg = new BoolProperty(startValue: true);

	private readonly StringProperty m_statusText = new StringProperty();

	protected override void OnActivate()
	{
		base.OnActivate();
		m_showBg.value = m_showBgAtStart;
		InputManager.LockInput(this);
		m_autoCloseCoroutine = StartCoroutine(SafeAutoClose());
	}

	public static void SetStatusText(string status)
	{
		if (IsActive())
		{
			(m_owner?.Get<UILockInputContext>()).SetStatus(status);
		}
	}

	public void SetStatus(string status)
	{
		m_statusText.value = status;
	}

	private IEnumerator SafeAutoClose()
	{
		float timeToClose = Time.unscaledTime + 20f;
		while (Time.unscaledTime < timeToClose)
		{
			yield return new WaitForSecondsRealtime(0.3f);
			IGameOverlay instance = Overlay.instance;
			if (false || !instance.DoneProcessingInvitation || !FocusState.IsActive)
			{
				timeToClose = Time.unscaledTime + 20f;
			}
		}
		GUICommonGodVoice.ShowText(LocalizationKeys.unknown_error.Localized(), 3f);
		DisableLock();
	}

	protected override void OnDeactivate()
	{
		m_showBgAtStart = true;
		InputManager.LockInput(null);
		if (m_autoCloseCoroutine != null)
		{
			StopCoroutine(m_autoCloseCoroutine);
			m_autoCloseCoroutine = null;
		}
		base.OnDeactivate();
	}

	private void Update()
	{
		if (!InputManager.IsInputLocked(this))
		{
			InputManager.LockInput(this);
		}
	}

	public static bool IsActive()
	{
		UILockInputContext uILockInputContext = m_owner?.Get<UILockInputContext>();
		if (uILockInputContext != null)
		{
			return uILockInputContext.isActiveAndEnabled;
		}
		return false;
	}

	public static void EnableLock(bool showBgAtStart = true)
	{
		m_showBgAtStart = showBgAtStart;
		if (States.mainCurrent is BaseGameState baseGameState)
		{
			SetLock(baseGameState.contextOwner);
		}
	}

	public static void SetLock(ContextOwner owner)
	{
		m_owner = owner;
		if (m_owner != null && !IsActive())
		{
			m_owner.Push<UILockInputContext>();
		}
	}

	public static void DisableLock()
	{
		if (IsActive())
		{
			SetStatusText(string.Empty);
			m_owner.Hide<UILockInputContext>();
		}
	}

	public static void DisableLockIfContextActive<T>() where T : BaseContext
	{
		if (m_owner != null)
		{
			T val = m_owner.Get<T>();
			if (!(val == null) && val.isActive && Overlay.instance.DoneProcessingInvitation && IsActive())
			{
				m_owner.Hide<UILockInputContext>();
			}
		}
	}
}
