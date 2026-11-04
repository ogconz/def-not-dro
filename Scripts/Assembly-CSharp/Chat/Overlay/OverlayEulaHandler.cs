using System;
using DI;

namespace Chat.Overlay
{
	public class OverlayEulaHandler
	{
		public void Setup()
		{
			UIPrivacyPolicyEULAContext.onEulaClosedResult = (Action<bool>)Delegate.Combine(UIPrivacyPolicyEULAContext.onEulaClosedResult, new Action<bool>(OnEulaResultHandler));
		}

		public void Release()
		{
			UIPrivacyPolicyEULAContext.onEulaClosedResult = (Action<bool>)Delegate.Remove(UIPrivacyPolicyEULAContext.onEulaClosedResult, new Action<bool>(OnEulaResultHandler));
		}

		private void OnEulaResultHandler(bool isApproved)
		{
			if (isApproved)
			{
				DependencyInjector.Resolve<GamePrefs>().globals.chatSingleplayer = true;
				Release();
			}
		}
	}
}
