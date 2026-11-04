using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace Services.GameAnalytics.Sentry
{
	public class SentryRawRequestWorker
	{
		private SentryConfigHandler m_config;

		public SentryRawRequestWorker(SentryConfigHandler config)
		{
			m_config = config;
		}

		public void Send(string json, string requestType)
		{
			string json2 = BuildPayload(json, requestType);
			UnityWebRequest request = CreateRequest(json2);
			PerformRequest(request);
		}

		private string BuildPayload(string json, string requestType)
		{
			string text = "{\"dsn\": \"" + m_config.dsn + "\"}";
			string text2 = "{\"type\": \"" + requestType + "\"}";
			return text + "\n" + text2 + "\n" + json;
		}

		private UnityWebRequest CreateRequest(string json)
		{
			UnityWebRequest unityWebRequest = new UnityWebRequest(m_config.url, "POST");
			byte[] bytes = Encoding.UTF8.GetBytes(json);
			unityWebRequest.uploadHandler = new UploadHandlerRaw(bytes);
			unityWebRequest.downloadHandler = new DownloadHandlerBuffer();
			return unityWebRequest;
		}

		private void PerformRequest(UnityWebRequest request)
		{
			request.SendWebRequest().ToUniTask().ContinueWith(delegate(UnityWebRequest unityWebRequest)
			{
				if (unityWebRequest.result != UnityWebRequest.Result.Success)
				{
					GameLog.LogError("Sentry API request failed " + unityWebRequest.error);
				}
				unityWebRequest.Dispose();
			})
				.Forget();
		}
	}
}
