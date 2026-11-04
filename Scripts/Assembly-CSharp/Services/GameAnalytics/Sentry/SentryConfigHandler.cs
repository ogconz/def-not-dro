using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameAnalytics;

namespace Services.GameAnalytics.Sentry
{
	public class SentryConfigHandler
	{
		private const string AnalyticsConfigEndpoint = "analytics/analyticsConfig";

		public string url { get; private set; }

		public string dsn { get; private set; }

		public bool isInitialized { get; private set; }

		public bool isServiceEnabled { get; private set; }

		public async UniTask InitConfigIfNeedAsync()
		{
			await WaitTokenAcquireAsync();
			await TryAcquireConfigAsync();
		}

		private async UniTask WaitTokenAcquireAsync()
		{
			UniTaskCompletionSource completionSource = new UniTaskCompletionSource();
			if (string.IsNullOrEmpty(GameSync.token))
			{
				GameSync.onTokenAcquired = (Action)Delegate.Combine(GameSync.onTokenAcquired, new Action(OnTokenAcquiredHandler));
			}
			else
			{
				completionSource.TrySetResult();
			}
			await completionSource.Task;
			void OnTokenAcquiredHandler()
			{
				completionSource.TrySetResult();
			}
		}

		private async UniTask TryAcquireConfigAsync()
		{
			UniTaskCompletionSource completionSource;
			if (!isInitialized)
			{
				completionSource = new UniTaskCompletionSource();
				Dictionary<string, string> formParams = new Dictionary<string, string> { 
				{
					"token",
					GameSync.token
				} };
				GameSync.apiClient.CallPost<SdrRestSentryAnalyticsConfig>("analytics/analyticsConfig", InvokeCallbackAndCompleteTask, null, formParams);
				await completionSource.Task;
			}
			void InvokeCallbackAndCompleteTask(SdrRestSentryAnalyticsConfig config, Error error)
			{
				OnConfigAcquiredHandler(config, error);
				completionSource.TrySetResult();
			}
		}

		private void OnConfigAcquiredHandler(SdrRestSentryAnalyticsConfig config, Error error)
		{
			if (error.type != Error.EType.None)
			{
				return;
			}
			if (string.IsNullOrWhiteSpace(config.analyticsSentyUrl))
			{
				GameLog.LogError("Sentry: DSN cannot be null or empty");
				return;
			}
			string[] array = config.analyticsSentyUrl.Split(new char[2] { '@', '/' }, StringSplitOptions.RemoveEmptyEntries);
			if (array.Length < 2)
			{
				GameLog.LogError("Sentry: Invalid DSN format");
				return;
			}
			dsn = config.analyticsSentyUrl;
			url = "https://" + array[2] + "/api/" + array[^1] + "/envelope/";
			isServiceEnabled = config.analyticsSentryEnable;
			isInitialized = true;
		}
	}
}
