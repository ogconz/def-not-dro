using System.Threading;
using Binding;
using Binding.Base;
using Cysharp.Threading.Tasks;

namespace Chat.Overlay.UI.MessageList
{
	public class SyncStatusHandler : BaseBindingTarget
	{
		private const int SyncAnimationDelayMs = 500;

		private readonly BoolProperty m_synced = new BoolProperty();

		private Message m_message;

		private ActionHolder m_callback;

		private CancellationTokenSource m_delayedSetStatusCts;

		public void Setup(Message message)
		{
			m_message = message;
			m_callback = TimeController.AddCallback(0f, SetupCallback);
		}

		public void Release()
		{
			ReleaseCallback();
			ReleaseSyncHandler();
			m_message = null;
		}

		private void SetupCallback()
		{
			m_callback = null;
			UpdateSyncStatus();
			m_message.skipSyncAnimation = true;
			if (!m_message.synced)
			{
				BindSyncHandler();
			}
		}

		private void ReleaseCallback()
		{
			if (m_callback != null)
			{
				TimeController.RemoveCallback(m_callback);
				m_callback = null;
			}
		}

		private void BindSyncHandler()
		{
			m_message.onSyncedEvent += OnSyncMessageHandler;
			RunDelayedSetStatus();
		}

		private void ReleaseSyncHandler()
		{
			m_message.onSyncedEvent -= OnSyncMessageHandler;
			ReleaseDelayedSetStatus();
		}

		private void OnSyncMessageHandler()
		{
			ReleaseSyncHandler();
			UpdateSyncStatus();
		}

		private void RunDelayedSetStatus()
		{
			m_delayedSetStatusCts = new CancellationTokenSource();
			DelayedSetStatus(m_delayedSetStatusCts.Token).Forget();
		}

		private void ReleaseDelayedSetStatus()
		{
			if (m_delayedSetStatusCts != null)
			{
				m_delayedSetStatusCts.Cancel();
				m_delayedSetStatusCts.Dispose();
				m_delayedSetStatusCts = null;
			}
		}

		private async UniTask DelayedSetStatus(CancellationToken token)
		{
			await UniTask.Delay(500, ignoreTimeScale: false, PlayerLoopTiming.Update, token);
			if (!token.IsCancellationRequested)
			{
				UpdateSyncStatus();
				ReleaseDelayedSetStatus();
			}
		}

		private void UpdateSyncStatus()
		{
			m_synced.value = !m_message.skipSyncAnimation || m_message.synced;
			m_synced.ForceValueChanged();
		}
	}
}
