using System;
using BestHTTP;
using UnityEngine;

public static class ConnectionUtils
{
	private const string PING_ENDPOINT = "/ping";

	private const string PROTOCOL = "https";

	public static void IsInternetAccessAvailable(Action<bool> onDone, float timeout = 5f, float connectionTimeout = 15f)
	{
		string text = WebConfigManager.BaseServerUrl;
		if (!text.Contains("https"))
		{
			text = "https://config.carx-online.com/gcs";
		}
		text += "/ping";
		HTTPRequest hTTPRequest = new HTTPRequest(new Uri(text), HTTPMethods.Get, isKeepAlive: false, disableCache: true, delegate(HTTPRequest requst, HTTPResponse response)
		{
			onDone.SafeCall(response?.IsSuccess ?? false);
		});
		hTTPRequest.Timeout = new TimeSpan(0, 0, Mathf.RoundToInt(timeout));
		hTTPRequest.ConnectTimeout = new TimeSpan(0, 0, Mathf.RoundToInt(connectionTimeout));
		hTTPRequest.Send();
	}
}
