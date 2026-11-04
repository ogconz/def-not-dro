using System;
using GameInput;
using Rewired;
using UnityEngine;

public class WheelForceFeedbackSupport : MonoBehaviour
{
	public static bool disableWheelFX = true;

	private bool m_cachedInnerStateFFB;

	private IFFBManager m_ffbManager;

	private GameInput.InputManager m_inputManager;

	public bool IsInitiated
	{
		get
		{
			if (m_ffbManager?.feedback != null)
			{
				return m_ffbManager.feedback.isInitiated;
			}
			return false;
		}
	}

	public IForceFeedbackSystem currentFeedbackSystem => m_ffbManager?.feedback;

	public bool FeedbackActive { get; set; }

	private bool isActive
	{
		get
		{
			if (m_cachedInnerStateFFB)
			{
				return FeedbackActive;
			}
			return false;
		}
	}

	public IFFBManager ffbManager => m_ffbManager;

	public void Init(GameInput.InputManager manager)
	{
		FocusState.onChanged = (Action<bool>)Delegate.Combine(FocusState.onChanged, new Action<bool>(OnAppFocusChanged));
		m_inputManager = manager;
		m_ffbManager = InitFFBManager();
		m_ffbManager.Initialize();
		TickManager.RegisterUpdate(UpdateFFB, 40, "ffbManager");
	}

	private IFFBManager InitFFBManager()
	{
		return new PCFFBManager();
	}

	public void SetConstantForce(float k)
	{
		if (isActive)
		{
			m_ffbManager?.feedback?.SetConstantForce(k);
		}
	}

	public void PlayImpactEffect(float k)
	{
		if (isActive && !disableWheelFX)
		{
			m_ffbManager?.feedback?.PlayImpactEffect(k);
		}
		m_inputManager.PlayImpactVibro(k);
	}

	public void PlayGrassSurfaceEffect(float k)
	{
		if (isActive && !disableWheelFX)
		{
			m_ffbManager?.feedback?.PlayGrassSurfaceEffect(k);
		}
		m_inputManager.PlayVibro(k, k);
	}

	public void PlayGravelSurfaceEffect(float k)
	{
		if (isActive && !disableWheelFX)
		{
			m_ffbManager?.feedback?.PlayGravelSurfaceEffect(k);
		}
		m_inputManager.PlayVibro(k, k);
	}

	public void StopFFBEffects(bool instant = true)
	{
		m_ffbManager?.feedback?.StopEffects(instant);
	}

	public bool IsCurrentrlySupportFFB(Controller controller)
	{
		if (controller != null && m_ffbManager != null && m_ffbManager.feedback != null)
		{
			return m_ffbManager.feedback.IsThisDeviceController(controller);
		}
		return false;
	}

	public void TryInitWheelInterfaceByController(Controller addedController, Controller removedController)
	{
		if (addedController != null && m_ffbManager != null)
		{
			OnControllerListChanged(addedController, removedController);
		}
	}

	public void OnUpdateDInputFeedbackSettings()
	{
		m_ffbManager.OnUpdateDInputSettings();
	}

	private void Update()
	{
		if (Time.timeScale == 0f)
		{
			UpdateFFB();
		}
	}

	public void UpdateFFB()
	{
		m_ffbManager?.Update();
	}

	public void SetEnabledFFB(bool flag)
	{
		if (flag)
		{
			m_ffbManager.OnEnable();
		}
		else
		{
			m_ffbManager.OnDisable();
		}
		m_cachedInnerStateFFB = flag;
	}

	private void OnAppFocusChanged(bool focusState)
	{
		m_ffbManager.OnAppFocusChanged(focusState);
	}

	public void OnControllerListChanged(Controller addedController, Controller removedController)
	{
		m_ffbManager.OnControllerListChanged(addedController, removedController);
	}

	private void OnDestroy()
	{
		FocusState.onChanged = (Action<bool>)Delegate.Remove(FocusState.onChanged, new Action<bool>(OnAppFocusChanged));
		TickManager.UnregisterFixedUpdate(UpdateFFB);
		m_ffbManager.Release();
	}
}
