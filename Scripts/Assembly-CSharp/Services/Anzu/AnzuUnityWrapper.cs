using System;
using System.Collections;
using Custom.SdrConfigs;
using DI;
using GameOverlay;
using GameOverlay.Regions;
using UnityEngine;
using anzu;

namespace Services.Anzu
{
	public class AnzuUnityWrapper : AnzuUnity
	{
		private AnzuXrModeToggleModeHandler m_xrModeToggleModeHandler;

		private static GamePrefs s_prefs;

		public new static AnzuUnityWrapper Instance => AnzuUnity.Instance as AnzuUnityWrapper;

		public bool isStarted { get; private set; }

		protected override void Awake()
		{
			m_xrModeToggleModeHandler = new AnzuXrModeToggleModeHandler(this);
			base.Awake();
		}

		protected override void OnEnable()
		{
			TimeController.StartNow(TryInvokeSdkAsync(OnDoneIntialize));
		}

		private void OnDoneIntialize(bool result)
		{
			if (result)
			{
				OnSuccessStart();
			}
		}

		protected override void OnDisable()
		{
			m_xrModeToggleModeHandler.Dispose();
			base.OnDisable();
		}

		private IEnumerator TryInvokeSdkAsync(Action<bool> onDone)
		{
			if (!IsPlatformValid())
			{
				onDone.SafeCall(param: false);
				yield break;
			}
			isStarted = IsAutoStartPlatform();
			if (isStarted)
			{
				Debug.LogError("Anzu c enabled! Its not allowed due awaiting eula confirmation. Check appConfig");
			}
			yield return WaitPrefsReady();
			if (Overlay.instance.regionDetection.GetRegionData().country == Country.Russia)
			{
				onDone.SafeCall(param: false);
				yield break;
			}
			yield return WaitViewConfig();
			if (!ViewConfigManager.isInited || !ViewConfigManager.sdrRestViewConfig.viewAnzuAdEnable)
			{
				onDone.SafeCall(param: false);
				yield break;
			}
			if (!isStarted)
			{
				yield return WaitEulaAccept();
				StartManual();
			}
			onDone.SafeCall(param: true);
		}

		private void OnSuccessStart()
		{
			m_xrModeToggleModeHandler.Init();
			base.OnEnable();
		}

		public void StartManual()
		{
			if (isStarted)
			{
				Debug.LogError("AnzuUnity already started!");
				return;
			}
			AppSettingsSORuntimeManager.InitSDKWithPlatformSettings();
			isStarted = true;
		}

		public AppSettings GetCurrPlatformSettings()
		{
			return AppSettingsSORuntimeManager.GetAppSettingsByPlatform(AnzuPlatform.Windows);
		}

		public bool IsPlatformValid()
		{
			return true;
		}

		private bool IsAutoStartPlatform()
		{
			return (Resources.Load("AnzuAppSettings") as AppSettingsSO).InitSDKOnAppStart;
		}

		private IEnumerator WaitEulaAccept()
		{
			yield return new WaitUntil(() => s_prefs.globals.eulaAccepted);
			AnzuCore.SetGDPRConsent(hasConsent: true);
		}

		private IEnumerator WaitViewConfig()
		{
			yield return new WaitUntil(() => ViewConfigManager.isReceived);
		}

		private IEnumerator WaitPrefsReady()
		{
			if (s_prefs == null)
			{
				yield return new WaitUntil(() => Overlay.IsReady() && Overlay.instance.regionDetection != null);
				s_prefs = DependencyInjector.Resolve<GamePrefs>();
			}
		}
	}
}
