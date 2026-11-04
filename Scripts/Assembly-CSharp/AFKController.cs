using System;
using GameInput;
using UnityEngine;

public class AFKController : MonoBehaviour
{
	private const float ANALOG_INPUT_TRIGGER = 0.15f;

	private const float AFK_MESSAGE_REFRESH_TIME = 1f;

	[Header("Show kick warning message after (minutes)")]
	[SerializeField]
	private float m_afkTimeWarnMinutes;

	[Header("Show kick message after (minutes)")]
	[SerializeField]
	private float m_afkTimeKickMinutes;

	public System.Action onAfkKick;

	private bool m_active;

	private float m_afkTimer;

	private float m_formatTextTimer;

	private bool m_kickMessageActive;

	private InputManager m_inputManager;

	private float afkTimeWarn => m_afkTimeWarnMinutes * 60f;

	private float afkTimeKick => m_afkTimeKickMinutes * 60f;

	public static AFKController instance { get; private set; }

	private void Awake()
	{
		instance = this;
	}

	public void SetActive(bool active, bool clearCallbacks)
	{
		GameLog.Print(AlertLevel.Notify, "AFKController", $"AFK controller is set to {active}");
		m_active = active;
		if (m_active)
		{
			ResetTimerAndHide();
		}
		if (clearCallbacks)
		{
			onAfkKick = null;
		}
	}

	private void Update()
	{
		if (!m_active && !m_kickMessageActive)
		{
			return;
		}
		if (States.mainCurrent is SyncNetFreerideRaceModeState { contextOwner: not null } syncNetFreerideRaceModeState && syncNetFreerideRaceModeState.contextOwner.topContext is UIPostCombatContext)
		{
			ResetTimerAndHide();
			return;
		}
		CameraSwitch cameraSwitch = CameraSwitch.instance;
		if (cameraSwitch != null && cameraSwitch.targetRaceCar != null && cameraSwitch.targetRaceCar.isNetworkCar)
		{
			return;
		}
		if (m_inputManager == null)
		{
			m_inputManager = InputManager.instance;
		}
		if (Input.anyKey)
		{
			ResetTimerAndHide();
			return;
		}
		GameInput.Action[] actions = m_inputManager.GetActions();
		foreach (GameInput.Action action in actions)
		{
			if (action.pressed || Mathf.Abs(action.value) >= 0.15f)
			{
				ResetTimerAndHide();
				return;
			}
		}
		if (!m_kickMessageActive)
		{
			float deltaTime = Time.deltaTime;
			m_afkTimer += deltaTime;
			m_formatTextTimer += deltaTime;
			if (m_afkTimer >= afkTimeKick)
			{
				onAfkKick.SafeCall();
				m_kickMessageActive = true;
				UIWarningAFK.ShowText(LocalizationKeys.afk_kick_reason.Localized());
			}
			else if (m_afkTimer >= afkTimeWarn && m_formatTextTimer >= 1f)
			{
				m_formatTextTimer = 0f;
				UIWarningAFK.ShowText(GetFormattedText());
			}
		}
	}

	private void ResetTimerAndHide()
	{
		m_afkTimer = 0f;
		m_kickMessageActive = false;
		UIWarningAFK.HideText();
	}

	private string GetFormattedText()
	{
		float timeSeconds = afkTimeKick - m_afkTimer;
		return string.Format(LocalizationKeys.afk_kick_warn.Localized(), UIUtils.TimeToMSString(timeSeconds));
	}
}
