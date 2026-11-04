using System;
using System.Diagnostics;
using BestHTTP;
using SimpleJSON;
using UnityEngine;

public class NetworkRequestsUtils
{
	private class D
	{
		public int totalRooms { get; set; }

		public int totalUsers { get; set; }
	}

	private class RootObject
	{
		public D d { get; set; }
	}

	public static void CheckPing(string url, Action<float> callback)
	{
		Stopwatch timer = new Stopwatch();
		timer.Start();
		HTTPRequest hTTPRequest = new HTTPRequest(new Uri(url + "/ping"), HTTPMethods.Post, isKeepAlive: false, disableCache: false, delegate(HTTPRequest req, HTTPResponse resp)
		{
			timer.Stop();
			callback.SafeCall((resp != null && resp.IsSuccess) ? ((float)timer.ElapsedMilliseconds) : 0f);
		});
		hTTPRequest.Timeout = new TimeSpan(0, 0, Mathf.RoundToInt(5f));
		hTTPRequest.ConnectTimeout = new TimeSpan(0, 0, Mathf.RoundToInt(15f));
		hTTPRequest.Send();
	}

	public static void GetRoomsUsersCount(string url, Action<int, int> callback)
	{
		HTTPRequest hTTPRequest = new HTTPRequest(new Uri(url + "/info"), HTTPMethods.Post, isKeepAlive: false, disableCache: true, delegate(HTTPRequest req, HTTPResponse resp)
		{
			if (resp == null)
			{
				callback.SafeCall(0, 0);
			}
			else
			{
				RootObject rootObject = JSON.ParseAndSkipErrors<RootObject>(resp.DataAsText);
				if (rootObject == null)
				{
					callback.SafeCall(0, 0);
				}
				else
				{
					callback.SafeCall(rootObject.d.totalRooms, rootObject.d.totalUsers);
				}
			}
		});
		hTTPRequest.Timeout = new TimeSpan(0, 0, Mathf.RoundToInt(5f));
		hTTPRequest.ConnectTimeout = new TimeSpan(0, 0, Mathf.RoundToInt(15f));
		hTTPRequest.Send();
	}
}
