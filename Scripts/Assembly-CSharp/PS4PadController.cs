using System;
using GameInput;
using Rewired;
using Rewired.ControllerExtensions;
using UnityEngine;

public class PS4PadController : MonoBehaviour
{
	[Header("Light")]
	public float restoreStateColorSpeed = 1.5f;

	public Color menuColor = Color.blue;

	public Color singleRaceColor = Color.green;

	public Color multiPlayerRoomColor = Color.green;

	public Color multiPlayerRaceColor = Color.yellow;

	public Color driftColor = Color.red;

	public float driftEnergy = 1f;

	public float driftSpeed = 1f;

	public float maxDriftToUpdate = 0.5f;

	public Color countDownColor = Color.red;

	public Color countDownEndColor = Color.green;

	public float countDownEnergy = 1f;

	public float countDownSpeed = 1f;

	private bool m_interpolateOnlyAlpha;

	[Header("Touch")]
	public float minXSwipeLen = 0.5f;

	public float maxYSwipeSpread = 0.2f;

	public float maxTimeToSwipe = 0.4f;

	public bool logSwipeResult;

	public Sprite touchIcon;

	private IDualShock4Extension m_gamePad;

	private DriftController m_driftController;

	private Color m_currentStateColor;

	private Color m_flashColor;

	private float m_flashEnergy;

	private float m_initFlashEnergy;

	private float m_flashDrainSpeed;

	private bool m_touchState;

	private float m_touchTime;

	private float m_minTouchY;

	private float m_maxTouchY;

	private float m_startTouchX;

	private float m_endTouchX;

	private void Awake()
	{
		base.gameObject.SetActive(value: false);
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private void Start()
	{
		UpdateController(GameInput.InputManager.instance.lastController);
		GameLog.Print(AlertLevel.Notify, "System", "Starting Pad Light Manager");
		GameInput.InputManager instance = GameInput.InputManager.instance;
		instance.onChangeActiveDevice = (Action<Controller>)Delegate.Combine(instance.onChangeActiveDevice, new Action<Controller>(UpdateController));
		States.onStateActivate += OnStateActivate;
		m_currentStateColor = menuColor;
		GameEvents.OnRaceCountDown.Subscribe(GameEventFlag.Global, OnRaceCountDown);
		GameEvents.OnRaceEnd.Subscribe(GameEventFlag.Global, OnRaceEnd);
	}

	private void OnRaceEnd(GameEvents.RaceEndInfo e)
	{
		m_driftController = null;
	}

	private void OnRaceCountDown(GameEvents.RaceCountDown e)
	{
		StartFlash(interpolateAlpha: true, (e.countDown == 0) ? countDownEndColor : countDownColor, countDownEnergy, countDownSpeed);
	}

	private void OnStateActivate(BaseState state)
	{
		m_currentStateColor = GetStateColor(state);
		SetColor(m_currentStateColor);
		BaseRaceState baseRaceState = state as BaseRaceState;
		if (baseRaceState != null && (bool)baseRaceState.userCar)
		{
			m_driftController = baseRaceState.userCar.gameObject.GetComponent(typeof(DriftController)) as DriftController;
		}
	}

	private Color GetStateColor(BaseState state)
	{
		if (IsMPRaceState(state))
		{
			return multiPlayerRaceColor;
		}
		if (IsMPRoomState(state))
		{
			return multiPlayerRoomColor;
		}
		if (IsSingleRaceState(state))
		{
			return singleRaceColor;
		}
		return menuColor;
	}

	private bool IsSingleRaceState(BaseState state)
	{
		BaseRaceModeState baseRaceModeState = state as BaseRaceModeState;
		if (baseRaceModeState != null)
		{
			return !baseRaceModeState.raceMode.IsNetworkRace();
		}
		return false;
	}

	private bool IsMPRoomState(BaseState state)
	{
		return state is MultiplayerPCState;
	}

	private bool IsMPRaceState(BaseState state)
	{
		BaseRaceModeState baseRaceModeState = state as BaseRaceModeState;
		if (baseRaceModeState != null)
		{
			return baseRaceModeState.raceMode == RaceMode.NetworkFreeride;
		}
		return false;
	}

	private void UpdateController(Controller controller)
	{
		if (controller != null && controller.type == ControllerType.Joystick)
		{
			IDualShock4Extension extension = controller.GetExtension<IDualShock4Extension>();
			if (extension != m_gamePad)
			{
				m_gamePad = extension;
				m_touchState = false;
			}
		}
	}

	private void SetColor(Color color)
	{
		if (m_gamePad != null)
		{
			m_gamePad.SetLightColor(color);
		}
	}

	private void StartFlash(bool interpolateAlpha, Color color, float energy = 1f, float drainSpeed = 1f)
	{
		m_interpolateOnlyAlpha = interpolateAlpha;
		m_flashColor = color;
		m_flashEnergy = energy;
		m_initFlashEnergy = energy;
		m_flashDrainSpeed = drainSpeed;
	}

	private void Update()
	{
		if (m_gamePad == null)
		{
			return;
		}
		UpdateDriftState();
		UpdateSwipeDetection();
		if (!(m_flashEnergy > 0f))
		{
			return;
		}
		m_flashEnergy = Mathf.MoveTowards(m_flashEnergy, 0f, m_flashDrainSpeed * Time.unscaledDeltaTime);
		float t = Mathf.Clamp01(m_flashEnergy - (m_initFlashEnergy - 1f));
		Color color;
		if (m_interpolateOnlyAlpha)
		{
			if (m_flashEnergy > 0f)
			{
				color = m_flashColor;
				color.a = Mathf.Lerp(0f, 1f, t);
			}
			else
			{
				color = m_currentStateColor;
				color.a = 0f;
				StartFlash(interpolateAlpha: false, color, 1f, restoreStateColorSpeed);
			}
		}
		else
		{
			color = Color.Lerp(m_currentStateColor, m_flashColor, t);
		}
		SetColor(color);
	}

	private void UpdateSwipeDetection()
	{
		Vector2 position;
		bool touchPosition = m_gamePad.GetTouchPosition(0, out position);
		if (!m_touchState)
		{
			if (touchPosition)
			{
				m_touchState = true;
				m_touchTime = 0f;
				m_startTouchX = position.x;
				m_endTouchX = position.x;
				m_minTouchY = position.y;
				m_maxTouchY = position.y;
			}
			return;
		}
		if (touchPosition)
		{
			m_touchTime += Time.unscaledDeltaTime;
			m_endTouchX = position.x;
			m_minTouchY = Mathf.Min(m_minTouchY, position.y);
			m_maxTouchY = Mathf.Max(m_maxTouchY, position.y);
			return;
		}
		m_touchState = false;
		float num = m_endTouchX - m_startTouchX;
		float num2 = m_maxTouchY - m_minTouchY;
		if (!(m_touchTime < maxTimeToSwipe) || !(num > minXSwipeLen) || !(num2 < maxYSwipeSpread))
		{
			return;
		}
		bool flag = false;
		bool flag2 = false;
		if (States.current is BaseGameState { contextOwner: { } contextOwner })
		{
			UIFreeDroneCameraContext uIFreeDroneCameraContext = contextOwner.Get<UIFreeDroneCameraContext>();
			flag = uIFreeDroneCameraContext != null && uIFreeDroneCameraContext.isActive;
			UIViewingReplayContext uIViewingReplayContext = contextOwner.Get<UIViewingReplayContext>();
			flag2 = uIViewingReplayContext != null && uIViewingReplayContext.isActive;
		}
		if (!flag)
		{
			BaseState current = States.current;
			XDSRaceModeState xDSRaceModeState = current as XDSRaceModeState;
			if (((xDSRaceModeState != null && (bool)xDSRaceModeState.userCar && !xDSRaceModeState.userCar.lockState) || (xDSRaceModeState == null && current is BaseRaceState { isRaceEnded: false } baseRaceState && baseRaceState.startPlayPressed) || flag2) && KeepAliveManager.ToggleUIInRaceMode())
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.swipe_right_to_show_ui.Localized(), 6f, touchIcon);
			}
		}
	}

	private void UpdateDriftState()
	{
		if (m_driftController != null && m_driftController.isDrift && m_flashEnergy < maxDriftToUpdate)
		{
			StartFlash(interpolateAlpha: true, driftColor, driftEnergy, driftSpeed);
		}
	}
}
