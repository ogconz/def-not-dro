using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public class PlayFabAnalyticsService : ICommonGameAnalytics
{
	private const string PLAYFAB_LOGIN_ENDPOINT = "https://playfabapi.com/";

	private bool m_loggedIn;

	public UniTask InitAsync(string currentUserId)
	{
		try
		{
			PlayfabLoginGeneric(currentUserId);
		}
		catch (Exception arg)
		{
			Debug.LogError($"[Playfab] : Error on playfab login :: {arg}");
			Debug.LogError("[Playfab] : Check PlayFabSharedSettings.asset settings");
		}
		return UniTask.CompletedTask;
	}

	private void PlayfabLoginGeneric(string userId)
	{
		PlayFabSettings.RequestType = WebRequestType.UnityWebRequest;
		PlayFabClientAPI.LoginWithCustomID(new LoginWithCustomIDRequest
		{
			CustomId = userId,
			CreateAccount = true
		}, OnLoginSuccess, OnLoginFail);
	}

	private void OnLoginSuccess(LoginResult result)
	{
		m_loggedIn = true;
	}

	private void OnLoginFail(PlayFabError pfe)
	{
		m_loggedIn = false;
		string text = $"{pfe.Error} | {pfe.ErrorMessage} | {pfe.HttpCode} | " + pfe.HttpStatus + " | " + pfe.ApiEndpoint + " | " + pfe.GenerateErrorReport();
		Debug.LogError("[Playfab] : " + text);
	}

	public void SendEvent(string eventName)
	{
		SendEvent(eventName, null);
	}

	public void SendEvent(string eventName, Dictionary<string, object> parameters)
	{
		if (m_loggedIn)
		{
			if (parameters == null)
			{
				PlayFabClientAPI.WritePlayerEvent(new WriteClientPlayerEventRequest
				{
					EventName = eventName
				}, null, null);
			}
			else
			{
				PlayFabClientAPI.WritePlayerEvent(new WriteClientPlayerEventRequest
				{
					Body = parameters,
					EventName = eventName
				}, null, null);
			}
		}
	}
}
