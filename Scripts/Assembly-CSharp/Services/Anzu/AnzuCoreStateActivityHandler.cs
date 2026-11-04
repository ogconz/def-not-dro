using System;
using System.Collections.Generic;
using anzu;

namespace Services.Anzu
{
	public class AnzuCoreStateActivityHandler
	{
		private List<Type> m_pauseListStates = new List<Type> { typeof(InGameMenuState) };

		public void Initialize()
		{
			SetSubscriptionOnStateChanged(subscribe: true);
			ValidateSdkActivityOnState(States.current);
		}

		public void Dispose()
		{
			SetSubscriptionOnStateChanged(subscribe: false);
		}

		private void SetSubscriptionOnStateChanged(bool subscribe)
		{
			if (subscribe)
			{
				States.onStateActivate += ValidateSdkActivityOnState;
			}
			else
			{
				States.onStateActivate -= ValidateSdkActivityOnState;
			}
		}

		private void ValidateSdkActivityOnState(BaseState baseState)
		{
			Type type = baseState.GetType();
			bool coreActivity = true;
			foreach (Type pauseListState in m_pauseListStates)
			{
				if (pauseListState.IsAssignableFrom(type))
				{
					coreActivity = false;
					break;
				}
			}
			SetCoreActivity(coreActivity);
		}

		public void SetCoreActivity(bool activity)
		{
			AnzuCore.SetPauseState(!activity);
		}
	}
}
