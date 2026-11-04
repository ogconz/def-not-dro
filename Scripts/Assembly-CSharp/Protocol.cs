using System;
using System.Collections.Generic;
using System.Text;
using SimpleJSON;
using UnityEngine;

public class Protocol
{
	public class ApiException : Exception
	{
		public int ErrorCode { get; set; }

		public ApiException()
		{
		}

		public ApiException(int errorCode, string message)
			: base(message)
		{
			ErrorCode = errorCode;
		}
	}

	public class Success
	{
	}

	private readonly Dictionary<string, string> formParams = new Dictionary<string, string>();

	private readonly Dictionary<string, string> patchParams = new Dictionary<string, string>();

	private readonly Action<Error> m_onConnectionLost;

	public ApiClient ApiClient { get; private set; }

	public Protocol(string basePath, Action<Error> onConnectionLost)
	{
		ApiClient = new ApiClient(basePath, delegate(Error error)
		{
			m_onConnectionLost.SafeCall(error);
		});
		m_onConnectionLost = onConnectionLost;
	}

	private void RequireParam(string name, object param)
	{
		if (!(param is string text) || text == "")
		{
			throw new ApiException(400, "Missing required parameter " + name);
		}
	}

	public void Ping(Action<Success, Error> callback)
	{
		string path = "ping";
		ApiClient.CallGet(path, callback);
	}

	public void UnauthorizedGetRating(int userId, Action<UserElo, Error> callback)
	{
		string path = $"users/{ApiClient.ParameterToString(userId)}";
		ApiClient.CallGet(path, callback);
	}

	public void AuthenticateGoogle(string token, Action<Session, Error> callback)
	{
		RequireParam("token", token);
		string path = "authenticate/google";
		formParams.Clear();
		if (token != null)
		{
			formParams.Add("token", ApiClient.ParameterToString(token));
		}
		formParams.Add("full", "true");
		formParams.Add("clientVersion", GameVersion.versionAsString);
		ApiClient.CallPost(path, callback, null, formParams);
	}

	public void AuthenticateIos(string gameCenterPlayerId, string alias, string publicKeyUrl, string signature, string salt, string timestamp, string pushArn, Action<Session, Error> callback)
	{
		RequireParam("gameCenterPlayerId", gameCenterPlayerId);
		RequireParam("name", alias);
		RequireParam("publicKeyUrl", publicKeyUrl);
		RequireParam("signature", signature);
		RequireParam("salt", salt);
		RequireParam("timestamp", timestamp);
		string path = "authenticate/ios";
		formParams.Clear();
		if (gameCenterPlayerId != null)
		{
			formParams.Add("gameCenterPlayerId", ApiClient.ParameterToString(gameCenterPlayerId));
		}
		if (alias != null)
		{
			formParams.Add("name", ApiClient.ParameterToString(alias));
		}
		if (publicKeyUrl != null)
		{
			formParams.Add("publicKeyUrl", ApiClient.ParameterToString(publicKeyUrl));
		}
		if (signature != null)
		{
			formParams.Add("signature", ApiClient.ParameterToString(signature));
		}
		if (salt != null)
		{
			formParams.Add("salt", ApiClient.ParameterToString(salt));
		}
		if (timestamp != null)
		{
			formParams.Add("timestamp", ApiClient.ParameterToString(timestamp));
		}
		if (!string.IsNullOrEmpty(pushArn))
		{
			formParams.Add("pushArn", pushArn);
		}
		formParams.Add("full", "true");
		formParams.Add("clientVersion", GameVersion.versionAsString);
		ApiClient.CallPost(path, callback, null, formParams);
	}

	private ApiClient.Header MakeAutorizationHeader(string authorization)
	{
		ApiClient.Header header = null;
		if (authorization != null)
		{
			header = new ApiClient.Header();
			header.name = "Authorization";
			header.value = ApiClient.ParameterToString(authorization);
		}
		return header;
	}

	public void GetRating(string authorization, Action<List<Opponent>, Error> callback)
	{
		RequireParam("authorization", authorization);
		string path = "rating";
		ApiClient.Header header = MakeAutorizationHeader(authorization);
		ApiClient.CallGet(path, callback, header);
	}

	public void GetUser(int? id, string authorization, Action<User, Error> callback)
	{
		RequireParam("id", id);
		RequireParam("authorization", authorization);
		string path = $"users/{ApiClient.ParameterToString(id)}";
		ApiClient.Header header = MakeAutorizationHeader(authorization);
		ApiClient.CallGet(path, callback, header);
	}

	public void DownloadUserGhost(string url, Action<byte[]> callback)
	{
		if (string.IsNullOrEmpty(url))
		{
			Debug.LogError("Could not download ghost using empty url");
			callback.SafeCall(null);
		}
		else
		{
			ApiClient.SendGetRequestForLiveryData(url, callback);
		}
	}

	public void CreateRace(bool isQuali, int userId, int maxMultiplier, string authorization, Action<Race, Error> callback)
	{
		RequireParam("authorization", authorization);
		string value = (isQuali ? "quali" : "battle");
		string path = "races";
		ApiClient.Header header = MakeAutorizationHeader(authorization);
		formParams.Clear();
		formParams.Add("type", value);
		formParams.Add("opponentId", userId.ToString());
		formParams.Add("maxMultiplier", maxMultiplier.ToString());
		ApiClient.CallPost(path, callback, header, formParams);
	}

	public void SaveQuali(Race qualiRace, string authorization, Action<Race, Error> callback)
	{
		RequireParam("authorization", authorization);
		RequireParam("qualiRace", qualiRace);
		RequireParam("opponentId", qualiRace.opponentId);
		RequireParam("driftPoints", qualiRace.driftPoints);
		RequireParam("ghostObject", qualiRace.userGhost);
		RequireParam("ghost", qualiRace.userGhost.rawData);
		RequireParam("clientData", qualiRace.userGhost.settings);
		string path = "races/" + qualiRace.id;
		ApiClient.Header header = MakeAutorizationHeader(authorization);
		formParams.Clear();
		formParams.Add("type", qualiRace.type);
		formParams.Add("opponentId", qualiRace.opponentId.Value.ToString());
		formParams.Add("driftPoints", qualiRace.driftPoints.Value.ToString());
		formParams.Add("ghostDriftPoints", qualiRace.driftPoints.Value.ToString());
		formParams.Add("ghostSettings", JSON.Serialize(qualiRace.userGhost.settings));
		Dictionary<string, byte[]> dictionary = new Dictionary<string, byte[]>();
		if (qualiRace.userGhost != null && qualiRace.userGhost.rawData != null)
		{
			dictionary.Add("ghostData", qualiRace.userGhost.rawData);
		}
		ApiClient.CallPost(path, callback, header, formParams, dictionary);
	}

	public void SaveBattleResult(Race battleRace, string authorization, Action<Race, Error> callback)
	{
		RequireParam("authorization", authorization);
		RequireParam("battleRace", battleRace);
		RequireParam("opponentId", battleRace.opponentId);
		RequireParam("driftPoints", battleRace.driftPoints);
		string path = "races/" + battleRace.id;
		ApiClient.Header header = MakeAutorizationHeader(authorization);
		formParams.Clear();
		formParams.Add("type", battleRace.type);
		formParams.Add("result", battleRace.result.ToString());
		formParams.Add("opponentId", battleRace.opponentId.Value.ToString());
		formParams.Add("driftPoints", battleRace.driftPoints.Value.ToString());
		ApiClient.CallPost(path, callback, header, formParams);
	}

	public void SaveBattleGhost(Race battleRace, string authorization, Action<Race, Error> callback)
	{
		RequireParam("authorization", authorization);
		RequireParam("battleRace", battleRace);
		RequireParam("ghostData", battleRace.userGhost);
		RequireParam("ghost", battleRace.userGhost.rawData);
		string path = "races/" + battleRace.id;
		ApiClient.Header header = MakeAutorizationHeader(authorization);
		formParams.Clear();
		formParams.Add("ghostDriftPoints", battleRace.driftPoints.Value.ToString());
		formParams.Add("ghostSettings", JSON.Serialize(battleRace.userGhost.settings));
		Dictionary<string, byte[]> dictionary = new Dictionary<string, byte[]>();
		if (battleRace.userGhost != null && battleRace.userGhost.rawData != null)
		{
			dictionary.Add("ghostData", battleRace.userGhost.rawData);
		}
		ApiClient.CallPost(path, callback, header, formParams, dictionary);
	}

	public void GetRaceLog(string authorization, Action<List<Race>, Error> callback)
	{
		RequireParam("authorization", authorization);
		string path = "users/me/races";
		ApiClient.Header header = MakeAutorizationHeader(authorization);
		ApiClient.CallGet(path, callback, header);
	}

	public void GetLeagues(string authorization, Action<List<League>, Error> callback)
	{
		RequireParam("authorization", authorization);
		string path = "leagues";
		ApiClient.Header header = MakeAutorizationHeader(authorization);
		ApiClient.CallGet(path, callback, header);
	}

	public void RestoreFuelForUser(int fuel, string authorization, User user, Action<User, Error> callback)
	{
		patchParams.Clear();
		patchParams.Add("fuel", fuel.ToString());
		PatchUser(authorization, patchParams, callback);
	}

	public void IncreaseMaxFuelForUser(int increasedMaxFuel, bool byCash, string authorization, User user, Action<User, Error> callback)
	{
		patchParams.Clear();
		if (user.fuel < increasedMaxFuel)
		{
			patchParams.Add("fuel", increasedMaxFuel.ToString());
		}
		patchParams.Add("maxFuel", increasedMaxFuel.ToString());
		if (byCash)
		{
			patchParams.Add("purchasedMaxFuel4Cash", "True");
		}
		PatchUser(authorization, patchParams, callback);
	}

	public void AddQualiAttempt(string authorization, User user, Action<User, Error> callback)
	{
		patchParams.Clear();
		patchParams.Add("qualiAttemptsLeft", (user.qualiAttemptsLeft + 1).ToString());
		PatchUser(authorization, patchParams, callback);
	}

	private static string ValuesToJson(Dictionary<string, string> fieldValues)
	{
		StringBuilder stringBuilder = new StringBuilder("{");
		string text = "";
		foreach (KeyValuePair<string, string> fieldValue in fieldValues)
		{
			stringBuilder.Append(text).Append("\"").Append(fieldValue.Key)
				.Append("\": ");
			stringBuilder.Append("\"").Append(fieldValue.Value).Append("\"");
			if (text == "")
			{
				text = ", ";
			}
		}
		stringBuilder.Append("}");
		return stringBuilder.ToString();
	}

	private void PatchUser(string authorization, Dictionary<string, string> fieldValues, Action<User, Error> callback)
	{
		RequireParam("authorization", authorization);
		string path = "users/me";
		ApiClient.Header header = MakeAutorizationHeader(authorization);
		ApiClient.CallPost(path, callback, header, ValuesToJson(fieldValues));
	}

	public void GetTopReplays(string authorization, int leagueId, int? nextPageAccessor, Action<Top, Error> callback)
	{
		RequireParam("authorization", authorization);
		string path = "tops/list";
		ApiClient.Header header = MakeAutorizationHeader(authorization);
		formParams.Clear();
		formParams.Add("clientVersion", GameVersion.versionAsString);
		formParams.Add("league", leagueId.ToString());
		if (nextPageAccessor.HasValue)
		{
			formParams.Add("nextPageAccessor", nextPageAccessor.ToString());
		}
		ApiClient.CallPost(path, callback, header, formParams);
	}

	public void SendTopReplayLike(string authorization, string raceId, Action<Error> callback)
	{
		RequireParam("authorization", authorization);
		string path = "tops/like";
		ApiClient.Header header = MakeAutorizationHeader(authorization);
		formParams.Clear();
		formParams.Add("clientVersion", GameVersion.versionAsString);
		formParams.Add("raceId", raceId);
		Action<Success, Error> callback2 = delegate(Success b, Error e)
		{
			callback.Call(e);
		};
		ApiClient.CallPost(path, callback2, header, formParams);
	}

	public void SendTopReplayView(string authorization, string raceId, Action<TopView, Error> callback)
	{
		RequireParam("authorization", authorization);
		string path = "/tops/view";
		ApiClient.Header header = MakeAutorizationHeader(authorization);
		formParams.Clear();
		formParams.Add("clientVersion", GameVersion.versionAsString);
		formParams.Add("raceId", raceId);
		ApiClient.CallPost(path, callback, header, formParams);
	}
}
