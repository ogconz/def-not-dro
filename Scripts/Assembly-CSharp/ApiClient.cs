using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BestHTTP;
using SimpleJSON;
using UnityEngine;

public class ApiClient
{
	public class Header
	{
		public string name;

		public string value;
	}

	private class TaskParams
	{
		public string response;

		public Action<TaskParams> callback;

		public Type type;

		public object result;
	}

	private class RequestWorker<T> where T : class
	{
		private static bool m_isAboutToQuit;

		private readonly ApiClient m_apiClient;

		private readonly HTTPRequest m_request;

		private readonly Action<T, Error> m_callback;

		private int m_attemptsPerRequest;

		private float m_startTimePerRequest;

		public RequestWorker(ApiClient apiClient, HTTPRequest request, Action<T, Error> mCallback)
		{
			m_apiClient = apiClient;
			m_request = request;
			m_callback = mCallback;
		}

		private void LogResponse(HTTPRequest req, HTTPResponse resp)
		{
			float num = Time.realtimeSinceStartup - m_startTimePerRequest;
			string text = "NULL";
			float num2 = -1f;
			try
			{
				List<string> headerValues = resp.GetHeaderValues("X-RequestId");
				if (headerValues.Count > 0)
				{
					text = headerValues[0];
				}
				headerValues = resp.GetHeaderValues("X-Benchmark");
				if (headerValues.Count > 0)
				{
					num2 = float.Parse(headerValues[0]);
				}
			}
			catch
			{
			}
			string text2 = $"Got response in {num}s ({num2 / 1000f} on server) for {req.MethodType} to {req.Uri} with code {resp.StatusCode}. Request id = {text}";
			if (GameConsole.Bool["#restdata"])
			{
				text2 = text2 + "\ndata:\n" + resp.DataAsText;
			}
			GameLog.Print(AlertLevel.Notify, "REST", text2);
		}

		private void ParsingThreadFunc(object param)
		{
			if (!(param is TaskParams taskParams))
			{
				return;
			}
			object obj = null;
			if (taskParams.type == typeof(string))
			{
				obj = JSON.TryParseNodeValue(taskParams.response, "d", logError: true);
				if (obj == null)
				{
					obj = taskParams.response;
				}
			}
			else
			{
				obj = JSON.TryParseNodeObject(taskParams.response, "d", taskParams.type, logError: true);
			}
			taskParams.result = obj;
			if (!m_isAboutToQuit)
			{
				TimeController.CallFromMainThread(taskParams.callback, taskParams);
			}
		}

		private void OnTaskStart(Task task)
		{
			TimeController.onGameEngineStop = (Action)Delegate.Combine(TimeController.onGameEngineStop, new Action(OnGameEngineStop));
		}

		private void OnTaskEnd()
		{
			TimeController.onGameEngineStop = (Action)Delegate.Remove(TimeController.onGameEngineStop, new Action(OnGameEngineStop));
		}

		private string ExtractReqName(string reqUri)
		{
			int num = reqUri.IndexOf('?');
			if (num == -1)
			{
				return reqUri;
			}
			int num2 = num;
			while (num2 > 0 && reqUri[num2] != '/')
			{
				num2--;
			}
			num2++;
			if (num - num2 <= 0)
			{
				return reqUri;
			}
			return reqUri.Substring(num2, num - num2);
		}

		private static Error ParseError(string respDataText)
		{
			object obj = JSON.TryParseNodeObject(respDataText, "e", typeof(Error), logError: true);
			if (obj == null)
			{
				return null;
			}
			return obj as Error;
		}

		private void OnApiCallFinished<K>(HTTPRequest req, HTTPResponse resp, Action<K, Error> callback) where K : class
		{
			string reqInfo = ExtractReqName(req.Uri.ToString()) + "\\" + typeof(K).Name + ">";
			K result = null;
			Action<Error.EType, string, string> fail = delegate(Error.EType errorType, string message, string guiMessage)
			{
				GameLog.Print(AlertLevel.Error, "REST", "request failed: " + message);
				Error error2 = new Error
				{
					type = errorType,
					debugMessage = reqInfo + message,
					guiMessage = guiMessage
				};
				if (!error2.IsConnectionError() || m_attemptsPerRequest >= 3)
				{
					callback.SafeCall(result, error2);
					m_apiClient.m_onError.SafeCall(error2);
				}
				else
				{
					Send(2.5f);
				}
			};
			if (resp == null)
			{
				fail(Error.EType.NoResponse, $"Null response for the request to {req.Uri}", LocalizationKeys.no_server_connection.Localized());
				return;
			}
			LogResponse(req, resp);
			int num = resp.StatusCode / 100;
			if (num != 2)
			{
				Error error = ParseError(resp.DataAsText);
				if (resp.StatusCode >= 502 && resp.StatusCode <= 504)
				{
					error = error ?? new Error
					{
						type = Error.EType.Unknown
					};
					error.type = ((error.type != Error.EType.None) ? error.type : Error.EType.Unknown);
					if (!string.IsNullOrEmpty(error.guiMessage))
					{
						error.guiMessage = LocalizationService.Localize(error.guiMessage);
					}
					error.debugMessage = reqInfo + error.debugMessage;
					GameLog.Print(AlertLevel.Error, "REST", "request returned an error: " + error.debugMessage);
					GameLog.Print(AlertLevel.Error, "REST", resp.DataAsText);
					fail(Error.EType.ServerOverloaded, $"Code {resp.StatusCode} received", LocalizationKeys.error_server_olerloaded_try_again_later.Localized());
				}
				else if (num == 4 || num == 5)
				{
					error = error ?? new Error
					{
						type = Error.EType.Unknown
					};
					error.type = ((error.type != Error.EType.None) ? error.type : Error.EType.Unknown);
					error.guiMessage = LocalizationService.Localize(error.guiMessage);
					error.debugMessage = reqInfo + error.debugMessage;
					GameLog.Print(AlertLevel.Error, "REST", "request returned an error: " + error.debugMessage);
					GameLog.Print(AlertLevel.Error, "REST", resp.DataAsText);
					callback.SafeCall(null, error);
					m_apiClient.m_onError.SafeCall(error);
				}
				else
				{
					fail(Error.EType.RequestFailed, $"Code {resp.StatusCode} returned for the request to {req.Uri}", LocalizationKeys.error_server_error.Localized());
				}
				return;
			}
			if (resp.StatusCode != 204 && string.IsNullOrEmpty(resp.DataAsText))
			{
				fail(Error.EType.EmptyResponse, $"Empty response returned for the request to {req.Uri}", LocalizationKeys.error_server_error.Localized());
				return;
			}
			if (typeof(K) == ProtocolSuccessType)
			{
				callback.SafeCall(new Protocol.Success() as K, new Error());
				return;
			}
			TaskParams taskParam = new TaskParams
			{
				type = typeof(K),
				response = resp.DataAsText,
				callback = delegate(TaskParams p)
				{
					if (!(p.result is K param))
					{
						Error.EType arg = ParseError(resp.DataAsText)?.type ?? Error.EType.CorruptedResponse;
						fail(arg, $"Could not parse response '{p.response}' to the type {typeof(K).Name} for the request to {req.Uri}", LocalizationKeys.error_corrupted_server_response.Localized());
					}
					else
					{
						callback.SafeCall(param, new Error());
					}
					OnTaskEnd();
				}
			};
			Task task = Task.Factory.StartNew(delegate
			{
				ParsingThreadFunc(taskParam);
			});
			OnTaskStart(task);
		}

		public void Send(float delay)
		{
			m_attemptsPerRequest++;
			m_startTimePerRequest = Time.realtimeSinceStartup + delay;
			if (m_request.Callback == null)
			{
				m_request.Callback = delegate(HTTPRequest originalRequest, HTTPResponse response)
				{
					OnApiCallFinished(originalRequest, response, m_callback);
				};
			}
			TimeController.AddCallback(delay, delegate
			{
				m_request.Send();
			});
		}

		private void OnGameEngineStop()
		{
			m_isAboutToQuit = true;
		}
	}

	private const int AttemptsBeforeTotalFailure = 3;

	private static readonly Type ProtocolSuccessType = typeof(Protocol.Success);

	private readonly Action<Error> m_onError;

	private readonly Dictionary<string, string> m_defaultHeaderMap = new Dictionary<string, string>();

	public Action<Error> needReloginEvent;

	public string BasePath { get; set; }

	public Dictionary<string, string> DefaultHeader => m_defaultHeaderMap;

	public ApiClient(string basePath, Action<Error> onRequestTotalFailure)
	{
		BasePath = basePath;
		m_onError = onRequestTotalFailure;
	}

	private string MakeFullUri(string path, Dictionary<string, string> queryParams = null)
	{
		StringBuilder stringBuilder = new StringBuilder(BasePath);
		stringBuilder.Append("/").Append(path);
		if (queryParams != null && queryParams.Count > 0)
		{
			stringBuilder.Append("?");
			foreach (KeyValuePair<string, string> queryParam in queryParams)
			{
				stringBuilder.Append(queryParam.Key).Append("=").Append(queryParam.Value)
					.Append("&");
			}
		}
		return stringBuilder.ToString();
	}

	private static void SetRequestHeader(HTTPRequest request, Header header)
	{
		if (header != null)
		{
			request.AddHeader(header.name, header.value);
		}
	}

	private static void AddFormParams(HTTPRequest request, Dictionary<string, string> formParams = null, Dictionary<string, byte[]> fileParams = null)
	{
		if (formParams != null)
		{
			foreach (KeyValuePair<string, string> formParam in formParams)
			{
				request.AddField(formParam.Key, formParam.Value);
			}
		}
		if (fileParams == null)
		{
			return;
		}
		foreach (KeyValuePair<string, byte[]> fileParam in fileParams)
		{
			if (fileParam.Value == null)
			{
				GameLog.Print(AlertLevel.Error, "REST", "Null data for file param " + fileParam.Key);
				continue;
			}
			request.AddBinaryData(fileParam.Key, fileParam.Value);
			GameLog.Print(AlertLevel.Notify, "REST", $"Sending binary data '{fileParam.Key}' {fileParam.Value.Length} bytes length");
		}
	}

	private void Send<T>(HTTPRequest request, Action<T, Error> callback) where T : class
	{
		request?.AddField("clientVersion", Application.platform.ToString() + "." + GameVersion.versionAsString);
		new RequestWorker<T>(this, request, callback).Send(0f);
	}

	public void CallGet<T>(string path, Action<T, Error> callback, Header header = null, Dictionary<string, string> queryParams = null) where T : class
	{
		HTTPRequest request = new HTTPRequest(new Uri(MakeFullUri(path, queryParams)), HTTPMethods.Get, isKeepAlive: true, disableCache: true, null);
		SetRequestHeader(request, header);
		Send(request, callback);
	}

	public void CallPost<T>(string path, Action<T, Error> callback, Header header, Dictionary<string, string> formParams = null, Dictionary<string, byte[]> fileParams = null) where T : class
	{
		HTTPRequest request = new HTTPRequest(new Uri(MakeFullUri(path)), HTTPMethods.Post, isKeepAlive: true, disableCache: true, null);
		SetRequestHeader(request, header);
		AddFormParams(request, formParams, fileParams);
		Send(request, callback);
	}

	public void CallPost<T>(string path, Action<T, Error> callback, Header header, string postBody = null) where T : class
	{
		HTTPRequest hTTPRequest = new HTTPRequest(new Uri(MakeFullUri(path)), HTTPMethods.Post, isKeepAlive: true, disableCache: true, null);
		SetRequestHeader(hTTPRequest, header);
		if (postBody != null)
		{
			hTTPRequest.AddHeader("content-type", "application/json");
			hTTPRequest.RawData = Encoding.UTF8.GetBytes(postBody);
		}
		Send(hTTPRequest, callback);
	}

	public void SendGetRequestForLiveryData(string uri, Action<byte[]> callback)
	{
		new HTTPRequest(callback: delegate(HTTPRequest req, HTTPResponse resp)
		{
			Action<string> action = delegate(string message)
			{
				GameLog.Print(AlertLevel.Error, "REST", "REST request failed: " + message);
				callback.SafeCall(null);
			};
			if (resp == null)
			{
				action("Null response for the request to " + uri);
			}
			else
			{
				byte[] data = resp.Data;
				if (resp.StatusCode / 100 != 2)
				{
					action($"Code {resp.StatusCode} returned for the request to {uri}");
				}
				else
				{
					callback.SafeCall(data);
				}
			}
		}, uri: new Uri(uri), methodType: HTTPMethods.Get, isKeepAlive: false, disableCache: true).Send();
	}

	public void AddDefaultHeader(string key, string value)
	{
		m_defaultHeaderMap.Add(key, value);
	}

	public string ParameterToString(object obj)
	{
		if (obj is DateTime dateTime)
		{
			return dateTime.ToString("u");
		}
		if (obj is List<string>)
		{
			return string.Join(",", (obj as List<string>).ToArray());
		}
		return Convert.ToString(obj);
	}
}
