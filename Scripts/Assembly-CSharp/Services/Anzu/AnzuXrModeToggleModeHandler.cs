using System;
using UnityEngine;
using anzu;

namespace Services.Anzu
{
	public class AnzuXrModeToggleModeHandler
	{
		private AnzuUnityWrapper m_anzuWrapper;

		public AnzuXrModeToggleModeHandler(AnzuUnityWrapper anzuUnityWrapper)
		{
			m_anzuWrapper = anzuUnityWrapper;
		}

		public void Init()
		{
			SetSubscriptionOnXrModeChanged(subscribe: true);
		}

		public void Dispose()
		{
			SetSubscriptionOnXrModeChanged(subscribe: false);
		}

		private void SetSubscriptionOnXrModeChanged(bool subscribe)
		{
			if (subscribe)
			{
				XRModeManager instance = XRModeManager.instance;
				instance.OnXRSubsystemActivated = (Action)Delegate.Combine(instance.OnXRSubsystemActivated, new Action(OnXrSubsystemActivatedHandler));
				XRModeManager instance2 = XRModeManager.instance;
				instance2.OnXRSubsystemDeactivated = (Action)Delegate.Combine(instance2.OnXRSubsystemDeactivated, new Action(OnXrSubsystemDeactivatedHandler));
			}
			else
			{
				XRModeManager instance3 = XRModeManager.instance;
				instance3.OnXRSubsystemActivated = (Action)Delegate.Remove(instance3.OnXRSubsystemActivated, new Action(OnXrSubsystemActivatedHandler));
				XRModeManager instance4 = XRModeManager.instance;
				instance4.OnXRSubsystemDeactivated = (Action)Delegate.Remove(instance4.OnXRSubsystemDeactivated, new Action(OnXrSubsystemDeactivatedHandler));
			}
		}

		private void OnXrSubsystemActivatedHandler()
		{
			RestartByCurrentVrSettings(enableVrCalculation: true);
		}

		private void OnXrSubsystemDeactivatedHandler()
		{
			RestartByCurrentVrSettings(enableVrCalculation: false);
		}

		private void RestartByCurrentVrSettings(bool enableVrCalculation)
		{
			if (m_anzuWrapper.isStarted)
			{
				AnzuCore.Uninitialize();
				AppSettings currPlatformSettings = m_anzuWrapper.GetCurrPlatformSettings();
				if (currPlatformSettings == null)
				{
					Debug.LogError("Empty anzu platform settings for curr platform!");
					return;
				}
				currPlatformSettings.UseVRCalculations = enableVrCalculation;
				AppSettingsSORuntimeManager.InitSDKWithPlatformSettings();
			}
		}
	}
}
