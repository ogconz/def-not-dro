using System;

namespace Chat.Overlay
{
	public class GameContextObserver
	{
		private BaseState m_currentState;

		private ContextOwner m_currentContextOwner;

		public event Action onStateChanged;

		public event Action onActiveContextChanged;

		public BaseState GetCurrentState()
		{
			return m_currentState;
		}

		public ContextOwner GetCurrentContextOwner()
		{
			return m_currentContextOwner;
		}

		public void Setup()
		{
			BindStateChangeHandler();
		}

		public void Release()
		{
			ReleaseStateChangeHandler();
			ReleaseContextChangeHandler();
		}

		private void BindStateChangeHandler()
		{
			States.onStateActivate += OnStateActivateHandler;
		}

		private void ReleaseStateChangeHandler()
		{
			States.onStateActivate -= OnStateActivateHandler;
		}

		private void OnStateActivateHandler(BaseState baseState)
		{
			m_currentState = baseState;
			if (m_currentState is BaseGameState baseGameState)
			{
				ReleaseContextChangeHandler();
				BindContextChangeHandler(baseGameState.contextOwner);
			}
			this.onStateChanged.SafeCall();
		}

		private void BindContextChangeHandler(ContextOwner contextOwner)
		{
			if (m_currentContextOwner == null)
			{
				m_currentContextOwner = contextOwner;
				ContextOwner currentContextOwner = m_currentContextOwner;
				currentContextOwner.onContextActiveChanged = (Action<BaseContext>)Delegate.Combine(currentContextOwner.onContextActiveChanged, new Action<BaseContext>(OnContextActiveChangedHandler));
			}
		}

		private void ReleaseContextChangeHandler()
		{
			if (m_currentContextOwner != null)
			{
				ContextOwner currentContextOwner = m_currentContextOwner;
				currentContextOwner.onContextActiveChanged = (Action<BaseContext>)Delegate.Remove(currentContextOwner.onContextActiveChanged, new Action<BaseContext>(OnContextActiveChangedHandler));
				m_currentContextOwner = null;
			}
		}

		private void OnContextActiveChangedHandler(BaseContext context)
		{
			this.onActiveContextChanged.SafeCall();
		}
	}
}
