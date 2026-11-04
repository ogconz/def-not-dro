using GameInput;

namespace HUD
{
	public class UIRaceFreerideModeContext : UIRaceModeContext
	{
		private DriftRaceModeState m_state;

		private InputManager m_inputManager;

		private UIRadialMenuContext m_radialMenuContext;

		private RadialMenuFactory m_radialMenuFactory;

		protected override void OnActivate()
		{
			base.OnActivate();
			m_state = States.current as DriftRaceModeState;
			m_inputManager = InputManager.instance;
			InitializeRadialMenu();
		}

		private void InitializeRadialMenu()
		{
			UIRadialMenuContext radialMenuTemplate = base.owner.Get<UIRadialMenuContext>();
			if (m_radialMenuFactory == null)
			{
				m_radialMenuFactory = new RadialMenuFactory(radialMenuTemplate);
			}
			if ((object)m_radialMenuContext == null)
			{
				m_radialMenuContext = m_radialMenuFactory.CreateRadialMenuContextWithActions(new RadialActionData(base.MoveToWaypoint, ActionType.TeleportToWaypoint, LocalizationKeys.input_action_teleporttowaypoint, 0), new RadialActionData(base.GotoDynostand, ActionType.GoToDynostand, LocalizationKeys.input_action_gotodynostand, 4), new RadialActionData(base.CreateWaypoint, ActionType.CreateWaypoint, LocalizationKeys.input_action_createwaypoint, 5));
			}
		}

		protected override void Update()
		{
			base.Update();
			if (m_state == null)
			{
				m_state = States.current as DriftRaceModeState;
			}
		}

		private void LateUpdate()
		{
			if (m_state != null)
			{
				SetTimer(m_state.currentLapTime);
				SetDriftPoints(m_state.currentDriftPoints);
				RefreshDriftPointAction(m_state.currentDriftPoints);
			}
		}

		protected override void FixedUpdate()
		{
			base.FixedUpdate();
			HandleRadialMenuVisibility();
			HandleShortCuts();
		}

		private void HandleShortCuts()
		{
			if (CanProcessRadialMenu() && !base.owner.IsActive<UIRadialMenuContext>())
			{
				if (m_inputManager.IsActionPressed(ActionType.GoToDynostand))
				{
					GotoDynostand();
				}
				if (m_inputManager.IsActionPressed(ActionType.CreateWaypoint))
				{
					CreateWaypoint();
				}
				if (m_inputManager.IsActionPressed(ActionType.TeleportToWaypoint))
				{
					MoveToWaypoint();
				}
			}
		}
	}
}
