using System;
using System.Collections.Generic;
using Binding;
using Binding.Base;
using DI;
using GameInput;
using UnityEngine;

[GameHotkeyBlocker(true)]
[RaycastBlocker(true)]
public class UIChangeServerRegionContext : BaseBindingBehaviourTarget
{
	public class ServerRegionItem : BaseListElementData
	{
		private StringProperty m_serverName = new StringProperty();

		private BoolProperty m_current = new BoolProperty();

		private BoolProperty m_isSelected = new BoolProperty();

		private BoolProperty m_inLobbyRooms = new BoolProperty();

		private IntProperty m_roomsCount = new IntProperty();

		private IntProperty m_usersCount = new IntProperty();

		private FloatProperty m_ping = new FloatProperty();

		private string m_url;

		private string m_serverNameNotLocalized;

		public string serverUrl => m_url;

		public string serverName => m_serverName.value;

		public bool isCurrent => m_current.value;

		public string serverNameNotLocalized => m_serverNameNotLocalized;

		public ServerRegionItem(string serverCaption, string url, string lastRegion)
		{
			m_url = url;
			m_serverNameNotLocalized = serverCaption;
			string localizedServer = WebConfigManager.GetLocalizedServer(serverCaption);
			m_serverName.value = localizedServer;
			m_current.value = serverCaption.Equals(lastRegion);
			m_isSelected.value = m_current.value;
			m_inLobbyRooms.value = States.mainCurrent is MultiplayerPCState;
		}

		public void UpdatePing(float ping)
		{
			m_ping.value = ping;
		}

		public void UpdateRoomUserCount(int roomsCount, int usersCount)
		{
			m_roomsCount.value = roomsCount;
			m_usersCount.value = usersCount;
		}

		public void SetSelected(bool selected)
		{
			m_isSelected.value = selected;
		}
	}

	private static bool m_forceLobbyReconnect;

	private bool ENABLE_PING;

	private float m_currTime;

	private float m_pingUpdatePeriod = 2f;

	[Dependency]
	private GamePrefs m_prefs;

	private ListProperty m_serversList = new ListProperty();

	private BoolProperty m_inLobbyRooms = new BoolProperty();

	private Dictionary<ServerRegionItem, string> m_serverNames = new Dictionary<ServerRegionItem, string>();

	public static void Show(ContextOwner owner, bool lobbyReconnect = true)
	{
		if (owner == null)
		{
			UIUtils.LogNullReference("UIChangeServerRegionContext.Show", "owner");
			return;
		}
		m_forceLobbyReconnect = lobbyReconnect;
		owner.Push<UIChangeServerRegionContext>();
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		InitList();
		m_serversList.OnSelectionChanged += OnListSelectionChangedHandler;
		m_serversList.OnClick += OnClickHandler;
		m_inLobbyRooms.value = States.mainCurrent is MultiplayerPCState;
		if (ENABLE_PING)
		{
			TimeController.onUpdate = (System.Action)Delegate.Combine(TimeController.onUpdate, new System.Action(OnUpdateHandler));
			m_currTime = Time.unscaledTime;
		}
	}

	protected override void OnDeactivate()
	{
		m_serversList.OnSelectionChanged -= OnListSelectionChangedHandler;
		m_serversList.OnClick -= OnClickHandler;
		if (ENABLE_PING)
		{
			TimeController.onUpdate = (System.Action)Delegate.Remove(TimeController.onUpdate, new System.Action(OnUpdateHandler));
		}
		m_serverNames.Clear();
		m_serversList.Clear();
		base.OnDeactivate();
	}

	private void OnUpdateHandler()
	{
		if (m_currTime + m_pingUpdatePeriod < Time.unscaledTime)
		{
			m_currTime = Time.unscaledTime;
			for (int i = 0; i < m_serversList.count; i++)
			{
				UpdateServerRegionInfo(m_serversList[i] as ServerRegionItem, updatePing: true, updateRoomsUsersCount: false);
			}
		}
	}

	private void InitList()
	{
		foreach (KeyValuePair<string, string> availableServer in WebConfigManager.availableServers)
		{
			ServerRegionItem serverRegionItem = new ServerRegionItem(availableServer.Key, availableServer.Value, m_prefs.globals.currentRegion);
			UpdateServerRegionInfo(serverRegionItem, updatePing: false);
			m_serverNames.Add(serverRegionItem, availableServer.Key);
			m_serversList.Add(serverRegionItem);
		}
		m_serversList.Selection(GetCurrentServerRegion(), selected: true);
	}

	private void UpdateServerRegionInfo(ServerRegionItem item, bool updatePing = true, bool updateRoomsUsersCount = true)
	{
		if (item == null)
		{
			return;
		}
		if (updatePing)
		{
			NetworkRequestsUtils.CheckPing(item.serverUrl, delegate(float ping)
			{
				item.UpdatePing(ping);
			});
		}
		if (updateRoomsUsersCount)
		{
			NetworkRequestsUtils.GetRoomsUsersCount(item.serverUrl, delegate(int roomsCount, int usersCount)
			{
				item.UpdateRoomUserCount(roomsCount, usersCount);
			});
		}
	}

	private ServerRegionItem GetCurrentServerRegion()
	{
		return m_serversList.Find((BaseListElementData item) => (item as ServerRegionItem).isCurrent) as ServerRegionItem;
	}

	private void OnListSelectionChangedHandler(BaseListElementData baseListElement)
	{
		for (int i = 0; i < m_serversList.count; i++)
		{
			if (m_serversList[i] is ServerRegionItem serverRegionItem)
			{
				serverRegionItem.SetSelected(m_serversList[i] == baseListElement);
			}
		}
	}

	private void ChangeRegionAndQuit(ServerRegionItem item)
	{
		if (item != null)
		{
			string currentRegion = m_prefs.globals.currentRegion;
			string text = (string.IsNullOrEmpty(m_serverNames[item]) ? item.serverName : m_serverNames[item]);
			if (!currentRegion.Equals(text))
			{
				WebConfigManager.SetMultiplayerMasterServerUrl(text, m_forceLobbyReconnect, showGodvoice: true);
			}
		}
		Hide();
	}

	private void OnClickHandler(BaseListElementData baseListElement)
	{
		ChangeRegionAndQuit(baseListElement as ServerRegionItem);
	}

	[GameHotkey(ActionType.NavSelectBtnA, false, false)]
	public void NavigationSubmit()
	{
		ChangeRegionAndQuit(m_serversList.GetSelected() as ServerRegionItem);
	}

	[GameHotkey(ActionType.NavCancelBtnB, false, false)]
	public void NavigationClose()
	{
		Hide();
	}

	[GameHotkey(ActionType.NavDown, false, true)]
	public void NavDown()
	{
		m_serversList.Selection(m_serversList.GetSelectedIndex() + 1, selected: true);
	}

	[GameHotkey(ActionType.NavUp, false, true)]
	public void NavUp()
	{
		m_serversList.Selection(m_serversList.GetSelectedIndex() - 1, selected: true);
	}
}
