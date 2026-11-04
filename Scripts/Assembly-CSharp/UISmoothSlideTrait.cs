using System;
using GameInput;
using UnityEngine;

public class UISmoothSlideTrait
{
	private const float TIME_DELAY_TO_APPLY_SMOOTH = 0.5f;

	public static UISmoothSlideTrait activeInstance;

	public bool multiInputMode;

	private Func<float> m_get;

	private Action<float> m_set;

	private float m_accelerate;

	private bool m_locked;

	public ActionType actionLeft = ActionType.NavLeft;

	public ActionType actionRight = ActionType.NavRight;

	public float minClamp;

	public float maxClamp = 1f;

	public const float defaultStep = 0.008f;

	public float step = 0.008f;

	private bool m_nextReleased;

	private bool m_prevReleased;

	public float multiplier = 20f;

	public bool useSmooth = true;

	private InputManager m_input;

	private ContextOwner m_contextOwner;

	private float m_lastTimeLeftActionPressed;

	private float m_lastTimeRightActionPressed;

	public UISmoothSlideTrait(Func<float> getter, Action<float> setter)
	{
		m_get = getter;
		m_set = setter;
		m_input = InputManager.instance;
		m_contextOwner = (States.current as BaseGameState)?.contextOwner;
	}

	private void ClampAndSet(float v)
	{
		m_set(Mathf.Clamp(v, minClamp, maxClamp));
	}

	private bool HasRaycastBlocker()
	{
		if (!(m_contextOwner?.topContext != null))
		{
			return false;
		}
		if (m_contextOwner.topContext.IsRaycastBlocker())
		{
			return m_contextOwner.topContext is UIBaseDialogContext;
		}
		return false;
	}

	public void FixedUpdate()
	{
		if ((!multiInputMode && activeInstance != this) || HasRaycastBlocker())
		{
			return;
		}
		bool flag = m_input.IsActionPressed(actionRight);
		bool flag2 = m_input.IsActionPressed(actionLeft);
		if (flag2)
		{
			m_lastTimeLeftActionPressed = Time.unscaledTime;
			m_locked = false;
		}
		if (flag)
		{
			m_lastTimeRightActionPressed = Time.unscaledTime;
			m_locked = false;
		}
		int num = 0;
		if (m_input.IsActionReleased(actionRight) && m_nextReleased)
		{
			m_nextReleased = false;
			num = 1;
		}
		if (m_input.IsActionReleased(actionLeft) && m_prevReleased)
		{
			m_prevReleased = false;
			num = -1;
		}
		if (num != 0)
		{
			if (!m_locked)
			{
				ClampAndSet(m_get() + (float)num * step);
			}
			return;
		}
		float num2 = 0f;
		if (useSmooth)
		{
			if (m_input.IsActionHold(actionRight) && Time.unscaledTime - m_lastTimeRightActionPressed > 0.5f)
			{
				num2 = 1f;
			}
			else if (m_input.IsActionHold(actionLeft) && Time.unscaledTime - m_lastTimeLeftActionPressed > 0.5f)
			{
				num2 = -1f;
			}
		}
		else if (flag)
		{
			num2 = 1f;
		}
		else if (flag2)
		{
			num2 = -1f;
		}
		if (num2 != 0f)
		{
			float unscaledDeltaTime = Time.unscaledDeltaTime;
			m_accelerate = Mathf.Clamp01(m_accelerate + unscaledDeltaTime);
			m_locked = m_accelerate > 0.2f;
			float val = m_accelerate * m_accelerate;
			val = Math.Max(val, 0.1f * unscaledDeltaTime);
			ClampAndSet((maxClamp - 1f > 0.01f) ? (m_get() + num2 * unscaledDeltaTime * (step * multiplier + val)) : (m_get() + num2 * unscaledDeltaTime * val));
		}
		else
		{
			m_accelerate = 0f;
		}
	}

	public void IncValue()
	{
		ClampAndSet(m_get() + step);
	}

	public void DecValue()
	{
		ClampAndSet(m_get() - step);
	}

	public void NextValueChange()
	{
		m_nextReleased = true;
	}

	public void PrevValueChange()
	{
		m_prevReleased = true;
	}
}
