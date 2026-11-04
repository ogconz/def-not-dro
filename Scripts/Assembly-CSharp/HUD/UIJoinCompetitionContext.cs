using Binding;
using Binding.Base;
using Cysharp.Threading.Tasks;
using GameInput;
using SyncMultiplayer;

namespace HUD
{
	[RaycastBlocker(true)]
	public class UIJoinCompetitionContext : BaseBindingBehaviourTarget
	{
		private UIRaceNetworkFreerideModeContext m_raceContext;

		private ListProperty m_roomsList = new ListProperty();

		private void Awake()
		{
			m_roomsList.OnClick += OnCkickRoomItem;
		}

		protected override void OnActivate()
		{
			base.OnActivate();
			if (m_raceContext == null)
			{
				m_raceContext = base.owner.Get<UIRaceNetworkFreerideModeContext>();
			}
			UIRaceHintsContext.ShowHints(base.owner, new ActionHintData[2]
			{
				new ActionHintData
				{
					hint = LocalizationKeys.nav_hint_goto_back.Localized(),
					type = ActionType.NavCancelBtnB,
					action = NavCancelBtnB
				},
				new ActionHintData
				{
					hint = LocalizationKeys.nav_hint_select.Localized(),
					type = ActionType.NavSelectBtnA,
					action = NavSelectBtnA
				}
			});
			FillRoomList();
			m_raceContext.SetBlockPlayerController(block: true);
			m_raceContext.roomsController.onSubroomOperation += onSubroomOperation;
			XRModeManager.TryApplyCockpitCamera();
		}

		private void onSubroomOperation(SubroomInfo roomInfo, SubroomOperation operation)
		{
			if (operation == SubroomOperation.Spawn)
			{
				m_roomsList.Add(new JoinRoomItem(roomInfo, m_raceContext.subroomModeIcons));
				if (m_roomsList.count == 1)
				{
					m_roomsList.Selection(0, selected: true);
				}
			}
		}

		protected override void OnDeactivate()
		{
			m_raceContext.roomsController.onSubroomOperation -= onSubroomOperation;
			m_raceContext.SetBlockPlayerController(block: false);
			base.owner.Hide<UIRaceHintsContext>();
			m_roomsList.Clear();
			base.OnDeactivate();
		}

		protected override bool OnEscape()
		{
			return true;
		}

		private void FillRoomList()
		{
			m_roomsList.Clear();
			foreach (SubroomInfo roomInfo in m_raceContext.roomsController.roomInfos)
			{
				if (roomInfo.state == SubroomState.WaitingPlayers)
				{
					m_roomsList.Add(new JoinRoomItem(roomInfo, m_raceContext.subroomModeIcons));
				}
			}
			m_roomsList.Selection(0, selected: true);
		}

		private async UniTaskVoid JoinRoom(SubroomInfo info)
		{
			if (!info.AllPlayersLoaded())
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.network_room_state_waitingplayers.Localized(), 5f);
				await UniTask.WaitUntil(info.AllPlayersLoaded);
			}
			m_raceContext.roomsController.JoinRoom(info);
		}

		private void NavSelectBtnA()
		{
			if (m_roomsList.GetSelected() is JoinRoomItem joinRoomItem)
			{
				if (UIRaceNetworkModeContext.VeryBadConnectionCheck(joinRoomItem.roomInfo.subroomMode))
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.xds_cant_join_badconnection.Localized(), 3f);
					Hide();
					return;
				}
				JoinRoom(joinRoomItem.roomInfo).Forget();
			}
			Hide();
		}

		private void NavCancelBtnB()
		{
			Hide();
		}

		[GameHotkey(ActionType.NavLeft, false, false)]
		private void NavLeft()
		{
			m_roomsList.Selection(m_roomsList.GetSelectedIndex() - 2, selected: true);
		}

		[GameHotkey(ActionType.NavRight, false, false)]
		private void NavRight()
		{
			m_roomsList.Selection(m_roomsList.GetSelectedIndex() + 2, selected: true);
		}

		[GameHotkey(ActionType.NavUp, false, false)]
		private void NavUp()
		{
			m_roomsList.Selection(m_roomsList.GetSelectedIndex() - 1, selected: true);
		}

		[GameHotkey(ActionType.NavDown, false, false)]
		private void NavDown()
		{
			m_roomsList.Selection(m_roomsList.GetSelectedIndex() + 1, selected: true);
		}

		public void RemoveRoomById(string id)
		{
			if (base.isActive)
			{
				int num = m_roomsList.IndexOf((JoinRoomItem x) => x.roomInfo.id == id);
				if (num >= 0)
				{
					m_roomsList.RemoveAt(num);
				}
			}
		}

		private void OnCkickRoomItem(BaseListElementData obj)
		{
			if (!obj.selected)
			{
				m_roomsList.Selection(obj, selected: true);
			}
			else
			{
				NavSelectBtnA();
			}
		}

		private void Update()
		{
			for (int i = 0; i < m_roomsList.count; i++)
			{
				JoinRoomItem joinRoomItem = m_roomsList[i] as JoinRoomItem;
				if ((int)joinRoomItem.roomInfo.state > 0 || joinRoomItem.roomInfo.GetLiveTime() == 0f || joinRoomItem.roomInfo.runtimePlayers.Count == 0)
				{
					m_roomsList.RemoveAt(i--);
				}
				else
				{
					joinRoomItem.Refresh();
				}
			}
		}
	}
}
