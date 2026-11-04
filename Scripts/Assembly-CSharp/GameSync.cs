using System;
using System.Collections;
using System.Collections.Generic;
using DI;
using GameOverlay;
using SimpleJSON;
using UnityEngine;

public static class GameSync
{
	private enum AuthorizationState
	{
		Unauthorized = 0,
		Authorized = 1,
		TokenDepleted = 2
	}

	private enum SyncKeyStatus
	{
		Unknown = 0,
		Valid = 1,
		Invalid = 2
	}

	[Flags]
	private enum ErrorFlags
	{
		NotAutorized = 1,
		AutorizeInProcess = 2,
		InvalidAge = 4,
		NoInternet = 8,
		WebConfigStatus = 0x10,
		NotReadyToSync = 0x20
	}

	private class LoginResponse
	{
		public string token { get; set; }

		public ServerState serverState { get; set; }

		public int cssPM { get; set; }
	}

	private struct ServerState
	{
		public string id { get; set; }

		public int ratingPoints { get; set; }

		public string steamId { get; set; }

		public int rpCurrency { get; set; }
	}

	private class CheckSyncKeyResponse
	{
		public bool hashOk { get; set; }
	}

	private class IncomeStateResponse
	{
		public string state { get; set; }

		public string stateHash { get; set; }
	}

	private class OutgoingStateResponse
	{
		public string newStateHash { get; set; }
	}

	private const float DEFAULT_TIMEOUT = 15f;

	private const float SYNC_PERIOD = 900f;

	private const float AUTHORIZE_TRY_PERIOD = 30f;

	private const float WEB_CONFIG_INIT_PERIOD = 5f;

	public static Action onTokenAcquired;

	private static AuthorizationState m_authorizationState;

	private static SyncKeyStatus m_syncKeyStatus;

	private static bool m_needSync;

	private static float m_nextTimeToAutorize;

	private static float m_nextTimeToSync;

	private static bool m_isSyncProcess;

	private static bool m_invalidTicket;

	private static GamePrefs m_prefs;

	private static ApiClient m_apiClient;

	public static bool isAuthorized => m_authorizationState == AuthorizationState.Authorized;

	public static bool isAuthorizationInProgress { get; private set; }

	public static string token { get; private set; }

	public static ApiClient apiClient => m_apiClient;

	public static bool invalidTicket => m_invalidTicket;

	private static bool needSync
	{
		get
		{
			return m_needSync;
		}
		set
		{
			m_needSync = value;
		}
	}

	public static event Action onSuccessAuthorization;

	static GameSync()
	{
		SetNeedSync();
		AuthorizeReset();
		m_prefs = DependencyInjector.Resolve<GamePrefs>();
	}

	public static void AuthorizeReset()
	{
		token = string.Empty;
		m_authorizationState = AuthorizationState.Unauthorized;
		m_syncKeyStatus = SyncKeyStatus.Unknown;
		m_nextTimeToSync = 0f;
		m_nextTimeToAutorize = 0f;
	}

	public static void RaiseTokenDepletedState()
	{
		m_authorizationState = AuthorizationState.TokenDepleted;
	}

	public static void ForceAuthorizeTime()
	{
		m_nextTimeToAutorize = 0f;
	}

	public static void SetNeedSync()
	{
		needSync = true;
	}

	public static int GetConnectionErrorFlags()
	{
		int num = 0;
		if (!isAuthorized)
		{
			num++;
		}
		if (isAuthorizationInProgress)
		{
			num += 2;
		}
		if (!Overlay.instance.ConnectedToInternet())
		{
			num += 8;
		}
		if (WebConfigManager.status != WebConfigManager.EStatus.Ok)
		{
			num += 16;
		}
		if (!Overlay.instance.IsValidAge)
		{
			num += 4;
		}
		if (!Overlay.instance.IsReadyToSync())
		{
			num += 32;
		}
		return num;
	}

	public static void InitApiClientIfNeed()
	{
		if (m_apiClient != null || WebConfigManager.status != WebConfigManager.EStatus.Ok)
		{
			return;
		}
		WebConfigManager.UpdateWebConfigMask(UserPlatform.ToMask(Overlay.GetPlatform()));
		try
		{
			m_apiClient = new ApiClient(WebConfigManager.BaseServerUrl, delegate(Error error)
			{
				Error.EType type = error.type;
				if ((uint)(type - 16) <= 1u)
				{
					RaiseTokenDepletedState();
				}
			});
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.FatalError, "System", "GameSync: " + ex.Message);
			m_apiClient = null;
		}
	}

	public static IEnumerator Clear()
	{
		InitApiClientIfNeed();
		SyncPlayerProfileModel.ClearGamePrefs();
		yield return Sync();
	}

	public static IEnumerator KeepAuthorizedAndSyncCoroutine()
	{
		m_nextTimeToSync = Time.unscaledTime + 900f;
		if (m_invalidTicket)
		{
			yield break;
		}
		while (WebConfigManager.status != WebConfigManager.EStatus.Ok)
		{
			if (States.current is GarageGUIState)
			{
				yield return WebConfigManager.TryInit();
			}
			if (WebConfigManager.status != WebConfigManager.EStatus.Ok)
			{
				yield return new WaitForSecondsRealtime(5f);
			}
		}
		while (true)
		{
			if (!isAuthorized && Time.unscaledTime > m_nextTimeToAutorize)
			{
				yield return Authorize();
				m_nextTimeToAutorize = Time.unscaledTime + 30f;
				if (m_invalidTicket)
				{
					break;
				}
			}
			if (isAuthorized)
			{
				float curTime = Time.unscaledTime;
				if (curTime > m_nextTimeToSync && needSync)
				{
					bool flag = true;
					if (States.current is BaseRaceState && flag)
					{
						m_nextTimeToSync = curTime + 30f;
					}
					else
					{
						yield return Sync();
						m_nextTimeToSync = curTime + 900f;
					}
				}
			}
			yield return null;
		}
	}

	public static IEnumerator Sync()
	{
		InitApiClientIfNeed();
		if (m_apiClient == null || !Overlay.instance.IsReadyToSync())
		{
			yield break;
		}
		if (m_isSyncProcess)
		{
			GameLog.Print(AlertLevel.Warning, "GameSync", "Sync in progress... please wait!");
			yield break;
		}
		if (!needSync)
		{
			GameLog.Print(AlertLevel.Notify, "GameSync", "No sync needed!");
			yield break;
		}
		GameLog.Print(AlertLevel.Notify, "GameSync", "Begin sync");
		m_isSyncProcess = true;
		yield return InternalSync();
		if (m_authorizationState == AuthorizationState.TokenDepleted)
		{
			yield return InternalSync();
		}
		m_isSyncProcess = false;
		needSync = false;
		GameLog.Print(AlertLevel.Notify, "GameSync", "End sync");
	}

	private static IEnumerator InternalSync()
	{
		if (m_invalidTicket)
		{
			yield break;
		}
		if (!isAuthorized)
		{
			yield return Authorize();
			if (!isAuthorized)
			{
				yield break;
			}
		}
		if (string.IsNullOrEmpty(m_prefs.globals.syncKey))
		{
			m_syncKeyStatus = SyncKeyStatus.Invalid;
			yield return RecvNewData();
			if (isAuthorized && string.IsNullOrEmpty(m_prefs.globals.syncKey))
			{
				m_syncKeyStatus = SyncKeyStatus.Valid;
				yield return SendData();
			}
			yield break;
		}
		yield return UpdateSyncKeyStatus();
		if (isAuthorized)
		{
			switch (m_syncKeyStatus)
			{
			case SyncKeyStatus.Invalid:
				yield return RecvNewData();
				break;
			case SyncKeyStatus.Valid:
				yield return SendData();
				break;
			}
		}
	}

	public static IEnumerator Authorize(float timeout = 20f)
	{
		if (isAuthorizationInProgress || isAuthorized || !Overlay.IsReady() || !Overlay.instance.IsReadyToSync())
		{
			yield break;
		}
		InitApiClientIfNeed();
		if (m_apiClient == null)
		{
			yield break;
		}
		isAuthorizationInProgress = true;
		if (m_apiClient == null)
		{
			token = string.Empty;
			isAuthorizationInProgress = false;
			yield break;
		}
		IGameOverlay overlay = Overlay.instance;
		if (!overlay.IsSessionTicketValid() || m_authorizationState == AuthorizationState.TokenDepleted)
		{
			yield return overlay.UpdateAuthSessionTicket();
		}
		if (!overlay.IsSessionTicketValid())
		{
			GameLog.Print(AlertLevel.Warning, "GameSync", "Overlay failed to obtain a ticket for: " + overlay.player.id.ToString());
			isAuthorizationInProgress = false;
			yield break;
		}
		yield return LoginToServerAndGetToken(timeout, OnTokenAcquired);
		isAuthorizationInProgress = false;
		if (m_authorizationState == AuthorizationState.Authorized)
		{
			GameSync.onSuccessAuthorization.SafeCall();
		}
	}

	private static void OnTokenAcquired(string token)
	{
		GameSync.token = token;
		onTokenAcquired.SafeCall();
	}

	private static IEnumerator UpdateSyncKeyStatus()
	{
		m_syncKeyStatus = SyncKeyStatus.Unknown;
		if (string.IsNullOrEmpty(token))
		{
			AuthorizeReset();
			yield break;
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Clear();
		dictionary.Add("token", token);
		dictionary.Add("prevStateHash", m_prefs.globals.syncKey);
		bool? keyValid = null;
		CheckSyncKeyResponse checkSyncKeyResponse = null;
		Error error = null;
		try
		{
			m_apiClient.CallPost("checkHash", delegate(CheckSyncKeyResponse s, Error hr)
			{
				keyValid = false;
				error = hr;
				checkSyncKeyResponse = s;
			}, null, dictionary);
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "GameSync", ex.Message);
			yield break;
		}
		float time = Time.unscaledTime;
		while (!keyValid.HasValue)
		{
			if (Time.unscaledTime > time + 15f)
			{
				keyValid = false;
				yield break;
			}
			yield return null;
		}
		if (checkSyncKeyResponse != null && error != null)
		{
			if (error.type == Error.EType.None)
			{
				keyValid = checkSyncKeyResponse.hashOk;
			}
			else
			{
				m_authorizationState = AuthorizationState.Unauthorized;
			}
		}
		m_syncKeyStatus = (keyValid.Value ? SyncKeyStatus.Valid : SyncKeyStatus.Invalid);
	}

	private static IEnumerator ShowSyncDialog(Action serverSync)
	{
		bool isServerSync = false;
		UIMessageBox.ShowQuestion("Sync server?", delegate
		{
			serverSync.Call();
			isServerSync = true;
		}, LocalizationKeys.No.Localized(), delegate
		{
			isServerSync = true;
		}, LocalizationKeys.Yes.Localized());
		while (!isServerSync)
		{
			yield return null;
		}
	}

	private static IEnumerator RecvNewData()
	{
		if (m_syncKeyStatus != SyncKeyStatus.Invalid || !isAuthorized)
		{
			yield break;
		}
		if (string.IsNullOrEmpty(token))
		{
			AuthorizeReset();
			yield break;
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Clear();
		dictionary.Add("token", token);
		bool done = false;
		IncomeStateResponse incomeStateResponse = null;
		try
		{
			m_apiClient.CallPost("getState", delegate(IncomeStateResponse s, Error hr)
			{
				done = true;
				if (hr.type == Error.EType.None)
				{
					incomeStateResponse = s;
				}
			}, null, dictionary);
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "GameSync", ex.Message);
			yield break;
		}
		float time = Time.unscaledTime;
		while (!done)
		{
			if (Time.unscaledTime > time + 15f)
			{
				GameLog.Print(AlertLevel.Error, "GameSync", "RecvNewData fail! time out");
				yield break;
			}
			yield return null;
		}
		if (incomeStateResponse != null)
		{
			SyncPlayerProfileModel result = null;
			if (JSON.TryParse<SyncPlayerProfileModel>(incomeStateResponse.state, out result, logError: true))
			{
				result.MergeToGamePrefs();
				m_prefs.globals.syncKey = incomeStateResponse.stateHash;
				GameLog.Print(AlertLevel.Notify, "GameSync", "RecvNewData done!");
			}
			else
			{
				GameLog.Print(AlertLevel.Error, "GameSync", "RecvNewData - state parse fail!");
			}
		}
	}

	private static IEnumerator SendData()
	{
		if (m_syncKeyStatus != SyncKeyStatus.Valid || !isAuthorized)
		{
			yield break;
		}
		if (string.IsNullOrEmpty(token))
		{
			AuthorizeReset();
			yield break;
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Clear();
		dictionary.Add("token", token);
		try
		{
			string jSONString = SyncPlayerProfileModel.GetJSONString();
			m_prefs.carSettings.SaveAndWriteToRemote();
			if (string.IsNullOrEmpty(jSONString))
			{
				GameLog.Print(AlertLevel.Error, "GameSync", "SyncPlayerProfileModel.GetJSONString == null");
				yield break;
			}
			dictionary.Add("state", jSONString);
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "GameSync", ex.Message);
			yield break;
		}
		if (!string.IsNullOrEmpty(m_prefs.globals.syncKey))
		{
			dictionary.Add("prevStateHash", m_prefs.globals.syncKey);
		}
		bool done = false;
		OutgoingStateResponse outgoingStateResponse = null;
		Error error = null;
		try
		{
			m_apiClient.CallPost("setState", delegate(OutgoingStateResponse s, Error hr)
			{
				done = true;
				outgoingStateResponse = s;
				error = hr;
			}, null, dictionary);
		}
		catch (Exception ex2)
		{
			GameLog.Print(AlertLevel.Error, "GameSync", ex2.Message);
			yield break;
		}
		float time = Time.unscaledTime;
		while (!done)
		{
			if (Time.unscaledTime > time + 15f)
			{
				GameLog.Print(AlertLevel.Error, "GameSync", "Send data fail! time out");
				yield break;
			}
			yield return null;
		}
		if (outgoingStateResponse != null && error != null)
		{
			switch (error.type)
			{
			case Error.EType.None:
				m_syncKeyStatus = SyncKeyStatus.Valid;
				m_prefs.globals.syncKey = outgoingStateResponse.newStateHash;
				GameLog.Print(AlertLevel.Notify, "GameSync", "Send data done!");
				break;
			case Error.EType.hash_changed:
				m_syncKeyStatus = SyncKeyStatus.Invalid;
				RaiseTokenDepletedState();
				GameLog.Print(AlertLevel.Warning, "GameSync", "Send data - hash_changed!");
				break;
			default:
				m_syncKeyStatus = SyncKeyStatus.Unknown;
				GameLog.Print(AlertLevel.Error, "GameSync", $"Send data - error:{error.type}!");
				break;
			}
		}
	}

	private static IEnumerator LoginToServerAndGetToken(float timeout, Action<string> result)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		string loginCmd = Overlay.instance.GetLoginCmd(dictionary);
		if (loginCmd == null)
		{
			result.Call("");
			yield break;
		}
		Error error = null;
		string token = string.Empty;
		try
		{
			m_apiClient.CallPost(loginCmd, delegate(LoginResponse response, Error hr)
			{
				error = hr;
				if (response != null)
				{
					token = response.token;
					WebConfigManager.UpdateWebConfigMask(response.cssPM);
				}
			}, null, dictionary);
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "GameSync", ex.Message);
			result.Call("");
			yield break;
		}
		float time = Time.unscaledTime;
		while (error == null)
		{
			if (Time.unscaledTime > time + timeout)
			{
				result.Call("");
				yield break;
			}
			yield return null;
		}
		Overlay.instance.OnLoginError(error);
		switch (error.type)
		{
		case Error.EType.None:
			m_authorizationState = AuthorizationState.Authorized;
			break;
		case Error.EType.invalid_ticket:
		case Error.EType.steam_id_is_compromised:
			m_invalidTicket = true;
			token = string.Empty;
			break;
		default:
			m_authorizationState = AuthorizationState.Unauthorized;
			token = string.Empty;
			break;
		}
		result.Call(token);
	}

	public static void TryEnterToMPZone(Action approvedAction, Action<string> disabledAction)
	{
		Overlay.instance.TryEnterToMPZone(isAuthorized, approvedAction, disabledAction);
	}

	public static string GetCurrentSyncStateMessage()
	{
		return Overlay.instance.GetStateMessage(isAuthorized);
	}
}
