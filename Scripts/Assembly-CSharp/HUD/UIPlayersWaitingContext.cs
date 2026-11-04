using Binding.Base;
using GameInput;
using SyncMultiplayer;

namespace HUD
{
	[RaycastBlocker(true)]
	public class UIPlayersWaitingContext : BaseBindingBehaviourTarget
	{
		private UIRaceHintsContext m_hintsPanel;

		private JoinRoomItem m_roomItem = new JoinRoomItem();

		private UIRaceNetworkFreerideModeContext m_raceContext;

		private void Awake()
		{
			m_hintsPanel = GetComponentInChildren<UIRaceHintsContext>();
		}

		private void Start()
		{
			m_hintsPanel.AddElement(LocalizationKeys.Exit.Localized(), ActionType.NavCancelBtnB, NavCancelBtnB);
		}

		protected override void OnActivate()
		{
			base.OnActivate();
			if (m_raceContext == null)
			{
				m_raceContext = base.owner.Get<UIRaceNetworkFreerideModeContext>();
			}
			m_raceContext.driftAngles.enabled = false;
		}

		public void Init(SubroomInfo subroomInfo)
		{
			m_roomItem.Init(subroomInfo, m_raceContext.subroomModeIcons);
		}

		protected override void OnDeactivate()
		{
			m_raceContext.driftAngles.enabled = true;
			base.OnDeactivate();
		}

		protected override bool OnEscape()
		{
			return true;
		}

		private void NavCancelBtnB()
		{
			if (m_roomItem == null)
			{
				Hide();
			}
			else if ((int)m_roomItem.roomInfo.state < 1)
			{
				m_raceContext.roomsController.SelfPlayerLeaveRoom(m_roomItem.roomInfo);
				m_raceContext.RefreshNotifyCount();
				Hide();
			}
		}

		private void Update()
		{
			if (m_roomItem != null)
			{
				m_roomItem.Refresh();
				SubroomInfo roomInfo = m_roomItem.roomInfo;
				if (roomInfo.GetLiveTime() == 0f || (int)roomInfo.state >= 2 || !m_raceContext.roomsController.ContainsRoom(roomInfo))
				{
					Hide();
				}
			}
		}

		public static void Show(ContextOwner owner, SubroomInfo subroomInfo)
		{
			owner.Push<UIPlayersWaitingContext>().Init(subroomInfo);
		}
	}
}
