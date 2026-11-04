using System.Collections.Generic;
using GameOverlay;
using SyncMultiplayer;

public class NetRoomPlayersListSystem : BaseNetGameSystem
{
	private const string NICKNAME_END_POINT = "/getPlayerNameByPlatformId";

	private const string TOKEN_ARG = "token";

	private const string USER_ARG = "platformUserId";

	private NetworkGame m_networkGame;

	private bool m_isMaster;

	private ApiClient m_apiClient;

	private Dictionary<string, string> m_requestArgs = new Dictionary<string, string>(4);

	private ApiClient MasterClient()
	{
		if (m_apiClient == null)
		{
			m_apiClient = new ApiClient(WebConfigManager.BaseServerUrl, delegate(Error error)
			{
				GameLog.Print(AlertLevel.Error, "ServerSharingService", "Server sharing: " + error.debugMessage);
			});
		}
		return m_apiClient;
	}

	public override void Attach(NetGameSystemsManager owner)
	{
		base.Attach(owner);
		m_networkGame = owner?.host;
	}

	public override void OnGameConnect()
	{
		if (m_networkGame != null && m_isMaster)
		{
			UpdateRoomPlayerListProp(m_networkGame.Players, null);
		}
	}

	public override void OnReconnect()
	{
		base.OnReconnect();
		FetchCreatorNickname();
	}

	private void FetchCreatorNickname()
	{
		if (!string.IsNullOrEmpty(m_networkGame?.playerRoomCreator?.FilteredNickName) || m_networkGame == null)
		{
			return;
		}
		SmartfoxRoomOptions currentRoomOptions = m_networkGame.CurrentRoomOptions;
		if (currentRoomOptions == null)
		{
			return;
		}
		string roomCreatorId = currentRoomOptions.RoomCreatorId;
		if (!PlayerId.TryParse(roomCreatorId, out var _))
		{
			return;
		}
		m_requestArgs.Clear();
		m_requestArgs.Add("token", GameSync.token);
		m_requestArgs.Add("platformUserId", roomCreatorId);
		MasterClient().CallPost("/getPlayerNameByPlatformId", delegate(SdrNickname result, Error error)
		{
			if (error.type == Error.EType.None && result != null)
			{
				m_networkGame.SetRoomCreatorName(result.name);
			}
		}, null, m_requestArgs);
	}

	public override void ChangeMasterState(bool isMaster)
	{
		m_isMaster = isMaster;
		if (m_networkGame != null && m_isMaster)
		{
			UpdateRoomPlayerListProp(m_networkGame.Players, null);
		}
	}

	public override void DoPlayerEnter(NetworkPlayer player)
	{
		if (m_isMaster && m_networkGame != null)
		{
			UpdateRoomPlayerListProp(m_networkGame.Players, null);
		}
	}

	public override void DoPlayerLeave(NetworkPlayer player)
	{
		if (m_isMaster && m_networkGame != null)
		{
			UpdateRoomPlayerListProp(m_networkGame.Players, player);
		}
	}

	private void UpdateRoomPlayerListProp(List<NetworkPlayer> roomPlayers, NetworkPlayer playerToSkip)
	{
		if (m_networkGame == null || m_networkGame.CurrentRoomOptions == null)
		{
			return;
		}
		SmartfoxRoomOptions smartfoxRoomOptions = m_networkGame.CurrentRoomOptions.Clone();
		string text = string.Empty;
		for (int i = 0; i < roomPlayers.Count; i++)
		{
			if (roomPlayers[i] != playerToSkip)
			{
				text = text + roomPlayers[i].PlayerId.ToString() + "; ";
			}
		}
		smartfoxRoomOptions.RoomPlayersList = text;
		m_networkGame.Client.SendChangedRoomOptions(smartfoxRoomOptions);
	}
}
