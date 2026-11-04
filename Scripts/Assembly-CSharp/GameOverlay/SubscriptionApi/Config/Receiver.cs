using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameOverlay.SubscriptionApi.Config
{
	public class Receiver
	{
		public delegate void ConfigReceivedDelegate(SdrRestServerPlayerCarxSubscriptionOwnership config, float lastTimeReceived);

		private const string Endpoint = "internal/getCarxSubscriptionOwnership";

		private const string TokenKey = "token";

		private UpdateScheduler m_updateScheduler;

		private bool m_waitForResponse;

		public event ConfigReceivedDelegate onReceived;

		public event Action onReceiveFailed;

		public void Init(UpdateScheduler updateScheduler)
		{
			m_updateScheduler = updateScheduler;
			UpdateScheduler updateScheduler2 = m_updateScheduler;
			updateScheduler2.onNeedUpdate = (Action)Delegate.Combine(updateScheduler2.onNeedUpdate, new Action(OnNeedUpdateHandler));
		}

		public void Dispose()
		{
			UpdateScheduler updateScheduler = m_updateScheduler;
			updateScheduler.onNeedUpdate = (Action)Delegate.Remove(updateScheduler.onNeedUpdate, new Action(OnNeedUpdateHandler));
			m_updateScheduler = null;
		}

		public void OnNeedUpdateHandler()
		{
			ReceiveAsync().Forget();
		}

		public async UniTask<bool> ReceiveAsync()
		{
			bool num = await ReceiveInternalAsync();
			if (!num)
			{
				this.onReceiveFailed?.SafeCall();
			}
			return num;
		}

		private async UniTask<bool> ReceiveInternalAsync()
		{
			if (m_waitForResponse)
			{
				return false;
			}
			if (!(await CheckInternetConnectionAsync()))
			{
				return false;
			}
			if (GameSync.apiClient == null || string.IsNullOrEmpty(GameSync.token))
			{
				return false;
			}
			Dictionary<string, string> formParams = new Dictionary<string, string> { 
			{
				"token",
				GameSync.token
			} };
			AutoResetUniTaskCompletionSource<bool> tcs = AutoResetUniTaskCompletionSource<bool>.Create();
			m_waitForResponse = true;
			GameSync.apiClient.CallPost("internal/getCarxSubscriptionOwnership", delegate(SdrRestServerPlayerCarxSubscriptionOwnership config, Error error)
			{
				OnConfigReceived(config, error, tcs);
			}, null, formParams);
			return await tcs.Task;
		}

		private async UniTask<bool> CheckInternetConnectionAsync()
		{
			AutoResetUniTaskCompletionSource<bool> tcs = AutoResetUniTaskCompletionSource<bool>.Create();
			ConnectionUtils.IsInternetAccessAvailable(delegate(bool isAccessible)
			{
				tcs.TrySetResult(isAccessible);
			});
			return await tcs.Task;
		}

		private void OnConfigReceived(SdrRestServerPlayerCarxSubscriptionOwnership config, Error error, AutoResetUniTaskCompletionSource<bool> taskCompletionSource)
		{
			m_waitForResponse = false;
			if (error.type != Error.EType.None)
			{
				GameLog.Print(AlertLevel.Error, "SubscriptionOwnership", $"Unable to check subscription ownership >> {error.type}");
				taskCompletionSource.TrySetResult(result: false);
			}
			else
			{
				this.onReceived?.Invoke(config, Time.time);
				taskCompletionSource.TrySetResult(result: true);
			}
		}
	}
}
