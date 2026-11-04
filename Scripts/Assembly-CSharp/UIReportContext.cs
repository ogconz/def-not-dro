using System;
using System.Collections.Generic;
using Binding;
using Binding.Base;
using GameInput;
using GameOverlay;
using HUD;
using Rewired;
using SimpleJSON;
using SyncMultiplayer;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VinylSystem;

[GameHotkeyBlocker(true)]
[RaycastBlocker(true)]
public class UIReportContext : BaseBindingBehaviourTarget
{
	public class ReasonData
	{
		private int reasonType;
	}

	public class ReportReasonItem : BaseListElementData
	{
		private StringProperty m_reasonName = new StringProperty();

		public ReasonType m_reportReasons;

		private BoolProperty m_isSelected = new BoolProperty(false);

		public ReportReasonItem(string reasonName, ReasonType reportReasons)
		{
			string localizedServer = WebConfigManager.GetLocalizedServer(reasonName);
			m_reasonName.value = localizedServer;
			m_reportReasons = reportReasons;
		}

		public void SetSelected(bool selected)
		{
			m_isSelected.value = selected;
		}
	}

	private class ReportInformation
	{
		public string roomName;

		public string roomId;

		public int roomSize;

		public List<string> roomPlayers;

		public int lobbyRoomsCount;

		public int lobbyPlayersCount;
	}

	[SerializeField]
	private TMP_InputField m_input;

	[SerializeField]
	private UIRaceHintsContext m_hintsPanel;

	[SerializeField]
	private Transform m_background;

	[SerializeField]
	private RectTransform m_reportLayout;

	public SpriteProperty reportedIcon = new SpriteProperty();

	public readonly Property<bool> isReasonPage = new BoolProperty(startValue: true);

	public readonly Property<bool> showInputField = new BoolProperty(startValue: true);

	public readonly Property<bool> needShowAdditionalInfo = new BoolProperty(startValue: true);

	private readonly StringProperty m_charsCount = new StringProperty();

	private readonly StringProperty m_playerNickName = new StringProperty();

	private readonly IntProperty m_sourceTypeView = new IntProperty(-1);

	private readonly BoolProperty m_showBg = new BoolProperty();

	public Action<bool> showUIReportPanelAction;

	public Action<string> onSendSharingReport;

	private const string REQUEST_URL = "report/complaintReport";

	private readonly string[] REASON_KEYS = new string[6]
	{
		string.Empty,
		"incorrect_room_name_reason",
		"using_cheats_reason",
		"griefing_reason",
		"bad_vinyls_reason",
		"report_setup_reason"
	};

	private readonly ReasonType[] m_roomReportReasons = new ReasonType[2]
	{
		ReasonType.NONE,
		ReasonType.ROOM_NAME
	};

	private readonly ReasonType[] m_playerReportReasons = new ReasonType[4]
	{
		ReasonType.NONE,
		ReasonType.CHEATING,
		ReasonType.GRIEFING,
		ReasonType.LIVERY
	};

	private ReasonType[] m_sharingReportReasons;

	private BaseSharingConsole m_vinylsSharing;

	public static List<string> alreadyBannedInThisSessionPlayers = new List<string>();

	private ListProperty m_reportReasons = new ListProperty();

	private List<string> m_roomPlayers = new List<string>();

	private NetworkPlayer m_selectedPlayer;

	private BaseEventData m_baseEventData;

	private GameInput.InputManager m_inputManager;

	private string m_reportedRoomId = string.Empty;

	private string m_reportedPlayerId = string.Empty;

	private ReasonType m_reasonType;

	private SourceType m_sourceType;

	private string m_roomName = string.Empty;

	private int m_roomPlayersCount;

	private int m_lobbyRoomsCount;

	private int m_lobbyPlayersCount;

	private bool m_firstSubmitPress = true;

	private bool m_needUpdateLayout;

	private bool m_reportFromChamp;

	private string m_sharingCode;

	private string m_liveryCode;

	private CarVilylsInfo.VinylPreset m_presetToReport;

	public void Show(ContextOwner owner, Dictionary<string, string> param)
	{
		if (owner == null)
		{
			UIUtils.LogNullReference("UIReportContext.Show", "owner");
			return;
		}
		m_showBg.value = true;
		m_reportFromChamp = false;
		showInputField.value = true;
		SetSourceTypeAndUpdateRoomBool(SourceType.LOBBY);
		try
		{
			m_reportedRoomId = param["roomId"];
			m_reportedPlayerId = param["creatorId"];
			m_roomName = param["roomName"];
			m_roomPlayersCount = Convert.ToInt32(param["roomSize"]);
			m_lobbyRoomsCount = Convert.ToInt32(param["lobbyRoomsCount"]);
			m_lobbyPlayersCount = Convert.ToInt32(param["lobbyPlayersCount"]);
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "System", ex.ToString());
			return;
		}
		owner.Push<UIReportContext>();
	}

	public static void ClearBanPlayersList()
	{
		alreadyBannedInThisSessionPlayers.Clear();
	}

	public void ShowSharingReport(ContextOwner owner, string code, string toPlayerId, ReasonType reasonType, object data = null)
	{
		if (owner == null)
		{
			UIUtils.LogNullReference("UIReportContext.Show", "owner");
			return;
		}
		m_sharingReportReasons = new ReasonType[2]
		{
			ReasonType.NONE,
			reasonType
		};
		m_showBg.value = true;
		m_reportFromChamp = false;
		showInputField.value = true;
		m_presetToReport = data as CarVilylsInfo.VinylPreset;
		SetSourceTypeAndUpdateRoomBool(SourceType.SETUP);
		m_sharingCode = code;
		m_reportedPlayerId = toPlayerId;
		owner.Push<UIReportContext>();
	}

	public void Show(ContextOwner owner, NetworkPlayer player, string roomName, string roomId, List<string> roomPlayers)
	{
		if (owner == null)
		{
			UIUtils.LogNullReference("UIReportContext.Show", "owner");
			return;
		}
		showInputField.value = true;
		m_reportFromChamp = false;
		SetSourceTypeAndUpdateRoomBool(SourceType.ROOM);
		m_selectedPlayer = player;
		m_reportedPlayerId = player.PlayerId.ToString();
		m_roomName = roomName;
		m_reportedRoomId = roomId;
		m_roomPlayersCount = roomPlayers.Count;
		m_roomPlayers = roomPlayers;
		m_playerNickName.value = player.FilteredNickNameWithPlatformIcon;
		Overlay.instance.RequestUserAvatarAndName(player.PlayerId, OnLoadedAvatar);
		owner.Push<UIReportContext>();
	}

	private void SetSourceTypeAndUpdateRoomBool(SourceType currentType)
	{
		m_sourceType = currentType;
		m_sourceTypeView.value = (int)currentType;
	}

	private byte[] GetPlayerVinylForNetwork(NetworkPlayer player)
	{
		if (player == null || player.PlayerProperties == null || player.PlayerProperties.VisualSettings == null)
		{
			return null;
		}
		CarVilylsInfo.VinylPreset vinylPreset = new CarVilylsInfo().CreatePreset();
		vinylPreset.layers = player.PlayerProperties.VisualSettings.vinylLayers;
		return SerializeVinylPreset(vinylPreset);
	}

	private byte[] SerializeVinylPreset(CarVilylsInfo.VinylPreset vinylPreset)
	{
		if (vinylPreset == null)
		{
			return null;
		}
		if (m_vinylsSharing == null)
		{
			m_vinylsSharing = BaseSharingConsole.Create<VinylSharingManager>();
		}
		return m_vinylsSharing.GetSaveMemoryStream(vinylPreset).ToArray();
	}

	private void ShowHints()
	{
		if (m_hintsPanel == null)
		{
			m_hintsPanel = GetComponentInChildren<UIRaceHintsContext>();
		}
		m_hintsPanel.Clear();
		m_hintsPanel.AddElement(LocalizationKeys.nav_hint_goto_back.Localized(), ActionType.NavCancelBtnB, NavigationClose);
	}

	private void OnLoadedAvatar(PlayerId id, string name, Texture2D texture)
	{
		SetSprite(reportedIcon, texture);
	}

	private void SetSprite(SpriteProperty property, Texture2D texture)
	{
		Texture2D texture2D = ((texture != null) ? texture : GameManager.instance.defaultAvatar);
		property.value = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), Vector2.one * 0.5f);
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		isReasonPage.value = true;
		m_firstSubmitPress = true;
		needShowAdditionalInfo.value = true;
		m_inputManager = GameInput.InputManager.instance;
		m_baseEventData = new BaseEventData(EventSystem.current);
		m_input.text = string.Empty;
		InitList();
		m_reportReasons.OnSelectionChanged += OnListSelectionChangedHandler;
		m_reportReasons.OnClick += OnClickHandler;
		if (m_sourceType == SourceType.LOBBY || m_reportFromChamp)
		{
			BringForwardByDepth();
			ShowBottomPanel(new ActionHintData[1]
			{
				new ActionHintData
				{
					hint = LocalizationKeys.nav_hint_goto_back.Localized(),
					type = ActionType.NavCancelBtnB,
					action = NavigationClose
				}
			});
		}
		else
		{
			if (m_sourceType == SourceType.SETUP)
			{
				BringForwardByDepth();
			}
			ShowHints();
			showUIReportPanelAction.SafeCall(param: true);
		}
		if (Overlay.instance is SteamOverlay steamOverlay && GameManager.IsLinuxOsPlatform())
		{
			steamOverlay.onCloseOverlayKeyboard = (Action<string>)Delegate.Combine(steamOverlay.onCloseOverlayKeyboard, new Action<string>(OnReceiveInputText));
		}
		NeedUpdateLayout();
	}

	public void SetFocusInputField()
	{
		m_input.OnSelect(m_baseEventData);
		if (GameInput.InputManager.GetLockedInputObject() != m_input)
		{
			GameInput.InputManager.LockInput(m_input);
		}
	}

	private void DeselectInput()
	{
		if (m_input.isFocused)
		{
			m_input.OnDeselect(m_baseEventData);
		}
		if (GameInput.InputManager.GetLockedInputObject() == m_input)
		{
			GameInput.InputManager.LockInput(null);
		}
	}

	protected override void OnDeactivate()
	{
		m_reportReasons.OnSelectionChanged -= OnListSelectionChangedHandler;
		m_reportReasons.OnClick -= OnClickHandler;
		m_reportReasons.Clear();
		if (m_hintsPanel != null)
		{
			m_hintsPanel.Clear();
			m_hintsPanel = null;
		}
		showUIReportPanelAction.SafeCall(param: false);
		if (Overlay.instance is SteamOverlay steamOverlay && GameManager.IsLinuxOsPlatform())
		{
			steamOverlay.onCloseOverlayKeyboard = (Action<string>)Delegate.Remove(steamOverlay.onCloseOverlayKeyboard, new Action<string>(OnReceiveInputText));
		}
		DeselectInput();
		base.OnDeactivate();
		if (m_reportFromChamp)
		{
			return;
		}
		if (m_sourceType == SourceType.LOBBY)
		{
			UINetworkRoomsContext uINetworkRoomsContext = base.owner.Get<UINetworkRoomsContext>();
			if ((bool)uINetworkRoomsContext && uINetworkRoomsContext.isActive)
			{
				uINetworkRoomsContext.OnReActivate();
			}
		}
		else if (m_sourceType != SourceType.SETUP)
		{
			base.owner.Push<UIRaceStatsContext>();
		}
	}

	private void Update()
	{
		if (!isReasonPage.value)
		{
			m_charsCount.value = m_input.text.Length.ToString();
		}
		if (m_needUpdateLayout)
		{
			LayoutRebuilder.MarkLayoutForRebuild((RectTransform)base.transform);
			NeedUpdateLayout(value: false);
		}
		if (GameInput.InputManager.GetLockedInputObject() != m_input)
		{
			GameInput.InputManager.LockInput(m_input);
		}
		if (m_input.isFocused)
		{
			if (m_inputManager.lastController.type == ControllerType.Joystick)
			{
				GameInput.InputManager.LockInput(null);
				return;
			}
			if (GameInput.InputManager.GetLockedInputObject() != m_input)
			{
				GameInput.InputManager.LockInput(m_input);
			}
			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
			{
				m_input.lineType = TMP_InputField.LineType.MultiLineNewline;
			}
			else
			{
				m_input.lineType = TMP_InputField.LineType.MultiLineSubmit;
			}
		}
		else if (GameInput.InputManager.GetLockedInputObject() == m_input)
		{
			GameInput.InputManager.LockInput(null);
		}
	}

	private void InitList()
	{
		switch (m_sourceType)
		{
		case SourceType.LOBBY:
			AddingItemToList(m_roomReportReasons);
			break;
		case SourceType.ROOM:
			AddingItemToList(m_playerReportReasons);
			break;
		case SourceType.SETUP:
			AddingItemToList(m_sharingReportReasons);
			break;
		}
		m_reportReasons.Selection(0, selected: true);
		if (m_reportReasons[0] is ReportReasonItem reportReasonItem)
		{
			reportReasonItem.SetSelected(selected: true);
		}
		RectTransform rectTransform = m_background as RectTransform;
		if (rectTransform != null)
		{
			rectTransform.SetAsFirstSibling();
		}
	}

	private void AddingItemToList(ReasonType[] reasons)
	{
		foreach (ReasonType reasonType in reasons)
		{
			if (reasonType != ReasonType.NONE)
			{
				ReportReasonItem data = new ReportReasonItem(GetLocalizedReason(REASON_KEYS[(int)reasonType]), reasonType);
				m_reportReasons.Add(data);
			}
		}
	}

	private string GetLocalizedReason(string reason)
	{
		string text = new LocalizationKeys.Key(reason).Localized();
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return reason;
	}

	private void SelectReason(ReportReasonItem item)
	{
		if (item != null)
		{
			m_reasonType = item.m_reportReasons;
		}
		isReasonPage.value = false;
		m_reportReasons.Clear();
		if (showInputField.value)
		{
			ActivateInputField();
		}
		else
		{
			needShowAdditionalInfo.value = false;
		}
	}

	private void ActivateInputField()
	{
		if (Overlay.instance is SteamOverlay steamOverlay && GameManager.IsLinuxOsPlatform())
		{
			steamOverlay.ShowOverlayKeyboard(LocalizationKeys.report_description_info.Localized(), string.Empty, 40);
		}
		else
		{
			SetFocusInputField();
		}
		NeedUpdateLayout();
	}

	private void OnReceiveInputText(string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			m_input.text = text;
			if (m_input.text.Length > 250)
			{
				m_input.text = m_input.text.Substring(0, 250);
			}
		}
	}

	private void SendReport()
	{
		switch (m_sourceType)
		{
		case SourceType.LOBBY:
			SendRoomReport(m_reasonType, m_input.text);
			return;
		case SourceType.SETUP:
			SendSharingReport(m_reasonType);
			return;
		}
		SendPlayerReport(m_reasonType, m_input.text);
		if (m_selectedPlayer != null && m_selectedPlayer.userCar != null)
		{
			HidePlayerCar(m_selectedPlayer.userCar);
			if (m_reasonType == ReasonType.LIVERY)
			{
				HidePlayerVinyls(m_selectedPlayer.userCar);
			}
		}
	}

	private void SendRoomReport(ReasonType reason, string description)
	{
		UINetworkRoomsContext.ignoredRooms.Add(m_reportedRoomId);
		SendPlayerReport(reason, description);
	}

	private void SendSharingReport(ReasonType reason)
	{
		onSendSharingReport.SafeCall(m_sharingCode);
		SendPlayerReport(reason, m_sharingCode);
	}

	private void HidePlayerVinyls(RaceCar reportedCar)
	{
		if (!reportedCar.vinylsNotSupport)
		{
			VinylSystemConfig config = VinylSystemConfig.config;
			reportedCar.carModel.OverrideVinylWrapTexture(config.defaultMaterialTexture, config.defaultTexture);
		}
	}

	private void HidePlayerCar(RaceCar reportedCar)
	{
		NetworkController.InstanceGame.systems.Get<NetGameIgnoreSystem>().IgnorePlayer(reportedCar.networkPlayer);
	}

	private void SendPlayerReport(ReasonType reason, string discription)
	{
		string value = Overlay.playerId.ToString();
		Dictionary<string, string> dictionary = new Dictionary<string, string>
		{
			{ "from", value },
			{ "to", m_reportedPlayerId },
			{
				"sourceType",
				m_sourceType.ToString()
			},
			{
				"reasonType",
				m_reasonType.ToString()
			},
			{ "description", discription }
		};
		Dictionary<string, byte[]> dictionary2 = new Dictionary<string, byte[]>();
		if (m_reasonType == ReasonType.LIVERY)
		{
			byte[] array = null;
			switch (m_sourceType)
			{
			case SourceType.ROOM:
				array = GetPlayerVinylForNetwork(m_selectedPlayer);
				NetworkGame.reportedByVinylsPlayers.Add(m_reportedPlayerId);
				break;
			case SourceType.SETUP:
				array = SerializeVinylPreset(m_presetToReport);
				break;
			}
			if (array != null)
			{
				dictionary2.Add("data", array);
			}
		}
		ReportInformation reportInformation = new ReportInformation
		{
			roomName = m_roomName,
			roomId = m_reportedRoomId,
			roomSize = m_roomPlayersCount
		};
		switch (m_sourceType)
		{
		case SourceType.ROOM:
			reportInformation.roomPlayers = m_roomPlayers;
			break;
		case SourceType.LOBBY:
			reportInformation.lobbyRoomsCount = m_lobbyRoomsCount;
			reportInformation.lobbyPlayersCount = m_lobbyPlayersCount;
			break;
		}
		dictionary.Add("info", JSON.Serialize(reportInformation));
		GameSync.apiClient.CallPost("report/complaintReport", delegate(ReasonData info, Error error)
		{
			NavigationClose();
			GUICommonGodVoice.ShowText(LocalizationKeys.report_sended.Localized(), 3f);
			if (error.type == Error.EType.None)
			{
				alreadyBannedInThisSessionPlayers.Add(m_reportedPlayerId);
			}
			else
			{
				GameLog.Print(AlertLevel.Error, "Multiplayer", "Can't send report: " + error.debugMessage);
			}
		}, null, dictionary, dictionary2);
	}

	private void OnListSelectionChangedHandler(BaseListElementData baseListElement)
	{
		for (int i = 0; i < m_reportReasons.count; i++)
		{
			if (m_reportReasons[i] is ReportReasonItem reportReasonItem)
			{
				reportReasonItem.SetSelected(m_reportReasons[i] == baseListElement);
			}
		}
	}

	private void OnClickHandler(BaseListElementData baseListElement)
	{
		SelectReason(baseListElement as ReportReasonItem);
	}

	[GameHotkey(ActionType.NavSelectBtnA, false, true)]
	public void NavigationSubmit()
	{
		if (isReasonPage.value)
		{
			SelectReason(m_reportReasons.GetSelected() as ReportReasonItem);
		}
		else
		{
			OnReceiveInputText(m_input.text);
			DeselectInput();
			if (m_inputManager.lastActionDevice != GameInput.DeviceType.Keyboard)
			{
				m_firstSubmitPress = false;
			}
			if (!m_input.isFocused && !m_firstSubmitPress)
			{
				NavSendReport();
				return;
			}
			m_firstSubmitPress = false;
		}
		NeedUpdateLayout();
	}

	private void NeedUpdateLayout(bool value = true)
	{
		m_needUpdateLayout = value;
		if (value)
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(m_reportLayout);
		}
	}

	public void NavSendReport()
	{
		UILockInputContext.SetLock(base.owner);
		GameSync.TryEnterToMPZone(SendReport, CantSendReport);
	}

	private void CantSendReport(string msg)
	{
		GUICommonGodVoice.ShowText(msg);
		NavigationClose();
	}

	public void NavigationClose()
	{
		UILockInputContext.DisableLock();
		Hide();
	}

	[GameHotkey(ActionType.NavDown, false, true)]
	public void NavDown()
	{
		m_reportReasons.Selection(m_reportReasons.GetSelectedIndex() + 1, selected: true);
	}

	[GameHotkey(ActionType.NavUp, false, true)]
	public void NavUp()
	{
		m_reportReasons.Selection(m_reportReasons.GetSelectedIndex() - 1, selected: true);
	}
}
