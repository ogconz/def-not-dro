using System.Collections;
using Custom.SdrConfigs;
using Cysharp.Threading.Tasks;
using DB;
using DI;
using FMODUnity;
using GameInput;
using GameOverlay;
using Services.UGC.Utils;
using SimpleJSON;
using SyncMultiplayer;
using TrackLayout.Data;
using UnityEngine;

[AddComponentMenu("Scripts/States/Start")]
public class StartState : BaseGameState
{
	private const float WAIT_INVATION_TIMEOUT = 30f;

	private bool m_isInitialized;

	public static bool m_saveEnded = true;

	public GameObject prefabScreen;

	public override void OnInit()
	{
		base.OnInit();
		this.MakeDependenciesInjected();
	}

	public override void OnEnter()
	{
		base.OnEnter();
		CarXDboxManager.instance?.Initialize();
		TimeController.StartNow(ShowStartScreenAndStartInitAsync());
	}

	private IEnumerator ShowStartScreenAndStartInitAsync()
	{
		yield return UIStartScreen.ShowStartScreenAsync(prefabScreen, !m_isInitialized);
		UIStartScreen.SetStatusText(LocalizationKeys.Loading.Localized());
		if (m_isInitialized && Overlay.IsReady())
		{
			yield return OnPostStart();
			yield break;
		}
		yield return Initialization();
		UGCServiceProvider.instance.Initialize();
		yield return OnPostStart();
		yield return ShowEulaOnInputReadyAsync();
		ProceedOtherServices();
		m_isInitialized = true;
	}

	private IEnumerator ShowEulaOnInputReadyAsync()
	{
		yield return new WaitUntil(() => !UIStartScreen.IsShown);
		if (!UIPrivacyPolicyEULAContext.CheckIsEulaAccess())
		{
			UIPrivacyPolicyEULAContext.Show();
		}
	}

	private void ProceedOtherServices()
	{
		AnalyticsService.LogLaunchGame();
		UIGeneralSettingsContext.ResetVSyncIfNeed();
		InputManager.CheckIsNeedDisableVSync();
	}

	private IEnumerator Initialization()
	{
		WebConfigManager.UpdateWebConfigMask(UserPlatform.ToMask(Overlay.GetPlatform()));
		yield return null;
		yield return JSON.PrewarmCache(8000, 1050, 36200);
		yield return null;
		float startFmodTime = Time.realtimeSinceStartup;
		yield return InitFMOD();
		float num = Time.realtimeSinceStartup - startFmodTime;
		GameLog.Print(AlertLevel.Notify, "System", $"FMOD init time: {num} s");
		DependencyInjector.Resolve<XMLDictionariesManager>().Initialize();
		yield return null;
		yield return TrackMetaDB.Initialize();
		yield return Overlay.PreparePlayer();
		if (!Overlay.IsReady())
		{
			UIStartScreen.SetStatusText("Fatal error: failed to init Overlay!");
			yield break;
		}
		IGameOverlay instance = Overlay.instance;
		GameLog.Print(AlertLevel.Notify, "System", $"Started: {instance.GetType().Name}: {instance.player.nickName} id: {instance.player.id}");
		Screen.sleepTimeout = -1;
		DependencyInjector.Resolve<BaseModel>().ApplyWebStore();
		UIStartScreen.SetStatusText(LocalizationKeys.loading_steam.Localized());
		yield return WebConfigManager.TryInit();
		StatsManager.Init();
		if (WebConfigManager.status == WebConfigManager.EStatus.Ok)
		{
			float time = Time.unscaledTime;
			UIStartScreen.SetStatusText(LocalizationKeys.loading_sync.Localized());
			yield return GameSync.Sync();
			yield return ViewConfigManager.InitConfig().ToCoroutine();
			while (LocalizationService.languageMetaRequested && !LocalizationService.languageMetaResolved && !(Time.unscaledTime > time + 5f))
			{
				yield return null;
			}
		}
		UIStartScreen.SetStatusText(LocalizationKeys.Loading.Localized());
		Overlay.InitAnalyticsService();
		GameEvents.GameEngineStart();
		ModelValidator.ValidateModel();
	}

	private IEnumerator InitFMOD()
	{
		_ = RuntimeManager.StudioSystem;
		RuntimeManager.MuteAllEvents(!FocusState.IsActive);
		yield return null;
	}

	private IEnumerator OnPostStart()
	{
		float timeOut = Time.realtimeSinceStartup + 30f;
		while (!Overlay.instance.DoneProcessingInvitation)
		{
			yield return null;
			if (Time.realtimeSinceStartup > timeOut)
			{
				break;
			}
		}
		yield return null;
		if (NetworkController.NeedAutoJoin())
		{
			NetworkController.TryEnterToMultiplayer(base.contextOwner, needLock: false);
			yield break;
		}
		yield return null;
		States.Change<GarageGUIState>();
	}
}
