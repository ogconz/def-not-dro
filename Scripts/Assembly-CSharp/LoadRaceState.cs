using System;
using System.Collections;
using System.Threading.Tasks;
using AssetBundleParser;
using CarModelSystem.Data;
using Customization;
using DB;
using DB.Meta;
using DI;
using PrefsModels;
using Services.UGC.Utils;
using TrackLayout.Data;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using Utils.SceneManagement;
using VinylSystem;

public class LoadRaceState : BaseGameState
{
	public class StartArgs
	{
		public enum LoadingType
		{
			Normal = 0,
			PleaseWait = 1,
			NormalEmulation = 2
		}

		public LoadingType loadingType;

		public string trackName;

		public string trackConfiguration;

		public TrackLayoutData trackData;

		public bool isExternalMap;

		public ulong externalMapID;

		public byte ugcServiceId;

		public RaceMode raceMode = RaceMode.Drift;

		public Type nextStateType;

		public Action<BaseState> onNextStateActivated;

		public StartArgs(RaceMode mode, string track, string config, Type nextState, LoadingType loading, Action<BaseState> onNextStateActivatedCallback)
		{
			raceMode = mode;
			trackName = track;
			trackConfiguration = config;
			nextStateType = nextState;
			onNextStateActivated = onNextStateActivatedCallback;
			loadingType = loading;
		}

		public StartArgs()
		{
			loadingType = LoadingType.Normal;
		}
	}

	[Dependency]
	private BaseModel m_model;

	[Dependency]
	private GamePrefs m_prefs;

	private UILoadingScreenContext m_loadingContext;

	private StartArgs m_startArgs;

	private bool m_reloadScene = true;

	private bool m_onLoadedRized;

	private BaseRaceState m_nextRaceState;

	private ModMapCommand m_mapCommand = new ModMapCommand();

	public Action onRaceLoadedEvent;

	public Action<BaseState> onRaceStateActivatedEvent;

	public RaceMode currentRaceMode
	{
		get
		{
			if (m_startArgs == null)
			{
				return RaceMode.Drift;
			}
			return m_startArgs.raceMode;
		}
	}

	public TrackLayoutData currentTrackLayout
	{
		get
		{
			if (m_startArgs == null)
			{
				return null;
			}
			return m_startArgs.trackData;
		}
	}

	private Type GetLocalRaceStateType()
	{
		BaseRaceModeState state = ((m_prefs.globals.lastTrackId == -1) ? RaceMode.Drift : m_prefs.stats.GetLastRaceModeForTrack(m_prefs.globals.lastTrackId)).GetState();
		if (!(state != null))
		{
			return null;
		}
		return state.GetType();
	}

	public void SetRaceState()
	{
		States.Change(GetLocalRaceStateType());
	}

	private StartArgs MakeStartArgsByRaceInfo(StartArgs.LoadingType loadingType)
	{
		DB.Meta.Track track = ((m_prefs.globals.lastTrackId != -1) ? m_model.GetById<DB.Meta.Track>(m_prefs.globals.lastTrackId) : null);
		RaceMode raceMode = RaceMode.Drift;
		string trackName;
		string trackConfiguration;
		if (track == null)
		{
			trackName = "Fiorano";
			trackConfiguration = "Default";
			Debug.LogError("Could not find track with id " + m_prefs.globals.lastTrackId);
		}
		else
		{
			trackName = track.scene;
			trackConfiguration = track.configuration;
			raceMode = m_prefs.stats.GetLastRaceModeForTrack(track.id);
		}
		return new StartArgs
		{
			trackName = trackName,
			trackConfiguration = trackConfiguration,
			raceMode = raceMode,
			nextStateType = GetLocalRaceStateType(),
			loadingType = loadingType,
			externalMapID = track.externalId,
			ugcServiceId = track.ugcServiceId,
			isExternalMap = track.external
		};
	}

	public void LoadExternalRace(ulong externalMapID, StartArgs.LoadingType loadingType = StartArgs.LoadingType.Normal, bool reloadScene = true)
	{
		StartArgs startArgs = MakeStartArgsByRaceInfo(loadingType);
		startArgs.trackName = "CustomMap";
		startArgs.externalMapID = externalMapID;
		startArgs.raceMode = RaceMode.NetworkFreeride;
		startArgs.trackData = MapModsDatabase.GetDefaultLayout(externalMapID);
		startArgs.nextStateType = startArgs.raceMode.GetState().GetType();
		LoadRaceCommon(startArgs, reloadScene);
	}

	public async void LoadLocalRace(StartArgs.LoadingType loadingType = StartArgs.LoadingType.Normal, bool reloadScene = true)
	{
		StartArgs startArgs = MakeStartArgsByRaceInfo(loadingType);
		await CheckLoadedSceneAssetBundles(startArgs);
		LoadRaceCommon(startArgs, reloadScene);
	}

	private async Task CheckLoadedSceneAssetBundles(StartArgs startArgs)
	{
		if (startArgs.externalMapID != 0L)
		{
			await TryLoadSceneBundle(startArgs);
		}
		else
		{
			UGCSceneBundleManager.ReleaseLoadedAssetBundle();
		}
		async Task TryLoadSceneBundle(StartArgs startArgs2)
		{
			UILockInputContext.DisableLock();
			TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
			float time = Time.unscaledTime;
			UIMessageBox.ShowProgressBar(LocalizationKeys.unpacking_downloaded_external_resources.Localized(), async delegate(Action<float> progressFill, Action closeProgressBar)
			{
				await UGCSceneBundleManager.TryLoadSceneBundleAsyncBy(startArgs2.externalMapID, startArgs2.ugcServiceId, progressFill);
				UpdateMetaSceneNameByLoadedBundleWithArgs(startArgs2);
				for (float num = Time.unscaledTime - time; num <= 0.7f; num = Time.unscaledTime - time)
				{
					progressFill(num / 0.7f);
					await Task.Yield();
				}
				closeProgressBar.SafeCall();
				taskCompletionSource.SetResult(result: true);
			}, null, null, showBG: true);
			await taskCompletionSource.Task;
		}
	}

	private void UpdateMetaSceneNameByLoadedBundleWithArgs(StartArgs startArgs)
	{
		AssetBundle item = UGCSceneBundleManager.loadedSceneBundle.Item2;
		if (item == null)
		{
			return;
		}
		string text = TrackAssetBundleParser.instance.TryGetSceneNameFromAssetBundle(item);
		if (!string.IsNullOrEmpty(text))
		{
			DB.Meta.Track track = UGCSceneBundleManager.TryGetMetaByExternalId(startArgs.externalMapID);
			if (track != null)
			{
				track.scene = text;
				startArgs.trackName = text;
			}
		}
	}

	public void LoadRaceScene(RaceMode raceMode, string sceneName, string trackConfig, bool reloadScene = true)
	{
		StartArgs args = new StartArgs
		{
			trackName = sceneName,
			trackConfiguration = trackConfig,
			raceMode = raceMode,
			nextStateType = raceMode.GetState().GetType(),
			loadingType = StartArgs.LoadingType.Normal
		};
		LoadRaceCommon(args);
	}

	public async void LoadLocalRace(TrackLayoutData trackData, StartArgs.LoadingType loadingType = StartArgs.LoadingType.Normal, bool reloadScene = true)
	{
		StartArgs startArgs = MakeStartArgsByRaceInfo(loadingType);
		startArgs.trackData = trackData;
		await CheckLoadedSceneAssetBundles(startArgs);
		LoadRaceCommon(startArgs, reloadScene);
	}

	public void LoadRaceCommon(StartArgs args, bool reloadScene = true)
	{
		if (!SceneLoader.HasScene(args.trackName))
		{
			if (!args.isExternalMap)
			{
				Debug.LogError("Track Scene '" + args.trackName + "' not in build list.");
				return;
			}
			SceneLoader.AddScene(args.trackName, delegate
			{
				CacheData cacheData;
				if (args.isExternalMap)
				{
					cacheData = UnityEngine.Object.FindObjectOfType<CacheData>();
					if (!(cacheData == null))
					{
						SceneLoader.LoadScene("ModMapAdditional", LoadSceneMode.Additive);
						ModMapTestValidator.Subscribe();
						ModMapTestTool.Play(args.trackName)?.WithList(ModMapTestTool.Target.data).ValidComponents();
						ModMapTestTool.InitTests(args.trackName);
						ModMapTestTool.RunTest(args.trackName);
						ModMapTestValidator.UnSubscribe();
						m_mapCommand.CommandNoRecursive(cacheData.gameMarkers);
						GraphicsQuality graphicsQuality = DependencyInjector.Resolve<GraphicsQuality>();
						if ((bool)graphicsQuality)
						{
							GraphicsQuality.nightSettings.volumetric = graphicsQuality.volumetricFog != 0;
						}
						WaitCreateLightVariantController();
					}
				}
				async void WaitCreateLightVariantController()
				{
					while (LightVariantController.instance == null)
					{
						await Task.Delay(300);
					}
					if (LightVariantController.instance != null)
					{
						if (cacheData.reflectionProbe != null)
						{
							Transform transform = cacheData.reflectionProbe.gameObject.transform;
							transform.DetachChildren();
							GameObject gameObject = UnityEngine.Object.Instantiate(transform.gameObject, transform.position, transform.rotation, LightVariantController.instance.transform);
							Component[] components = gameObject.GetComponents(typeof(Component));
							for (int i = 0; i < components.Length; i++)
							{
								Type type = components[i].GetType();
								if (type.Name != "HDAdditionalReflectionData" && type.Name != "ReflectionProbe" && type.Name != "Transform" && type.Name != "RectTransform")
								{
									UnityEngine.Object.Destroy(components[i]);
								}
							}
							DestroyComponent<HDAdditionalReflectionData>(transform.gameObject);
							DestroyComponent<ReflectionProbe>(transform.gameObject);
							HDProbe component = gameObject.GetComponent<HDProbe>();
							component.settingsRaw.mode = ProbeSettings.Mode.Custom;
							int num = 537002370;
							num = ~num;
							cacheData.reflectionProbe.cullingMask = num;
							component.settingsRaw.cameraSettings.culling.cullingMask = num;
							component.RequestRenderNextUpdate();
							LightVariantController.instance.reflectionProbe = component;
						}
						else
						{
							Debug.LogError("Not find reflectionProbe");
						}
					}
					else
					{
						Debug.LogError("LightVariantController is null");
					}
				}
			});
		}
		m_startArgs = args;
		m_onLoadedRized = false;
		m_reloadScene = reloadScene;
		States.Change<LoadRaceState>();
		static void DestroyComponent<T>(GameObject gameObject) where T : Component
		{
			T component = gameObject.GetComponent<T>();
			if (component != null)
			{
				UnityEngine.Object.Destroy(component);
			}
		}
	}

	public StartArgs GetCurrentStartArgs()
	{
		return m_startArgs;
	}

	private static IEnumerator LoadRaceSceneCoroutine(string sceneName)
	{
		yield return SceneLoader.LoadSceneAsync("Empty");
		yield return States.InitiateLevelLoadingAsync(sceneName);
		yield return SceneLoader.LoadSceneAsync("Race", LoadSceneMode.Additive);
	}

	private void AfterSceneLoading(bool clearNotActiveConfigurations)
	{
		Application.backgroundLoadingPriority = ThreadPriority.Low;
		GameEvents.RaceLoaded();
		ConfigurationManager instance = ConfigurationManager.instance;
		if (instance != null)
		{
			if (m_startArgs.trackData != null && m_startArgs.externalMapID == 0L)
			{
				instance.SetCustomTrack(m_startArgs.trackData);
				if (m_startArgs.raceMode != RaceMode.XDS)
				{
					instance.InstantiateCustomConfiguration("default");
				}
			}
			else
			{
				instance.InitializeConfiguration(m_startArgs.raceMode, m_startArgs.trackConfiguration);
			}
		}
		CarModelConfig.forcedSyncLoading = false;
		VinylSystemConfig.forcedSyncLoading = false;
		switch (m_startArgs.loadingType)
		{
		case StartArgs.LoadingType.Normal:
			OnEndLoading();
			break;
		case StartArgs.LoadingType.PleaseWait:
			UIWaitDialogContext.Close(base.contextOwner);
			ChangeToLoadedState();
			break;
		}
	}

	private IEnumerator LoadRaceCoroutine()
	{
		yield return null;
		BasePrefsModel.LockSaves(flag: false);
		yield return BasePrefsModel.SaveAllQueries(inOneFrame: false);
		if (m_reloadScene)
		{
			yield return LoadRaceSceneCoroutine(m_startArgs.trackName);
			yield return States.ClearMemory();
		}
		else if (!SceneManager.GetSceneByName(m_startArgs.trackName).isLoaded)
		{
			yield return LoadRaceSceneCoroutine(m_startArgs.trackName);
		}
		if (m_startArgs.externalMapID != 0L)
		{
			yield return MapModsDatabase.Instantiate(m_startArgs.externalMapID);
		}
		AfterSceneLoading(m_reloadScene && !m_startArgs.raceMode.IsNetworkRace());
		onRaceLoadedEvent.Call();
	}

	private void StartAsyncLoading()
	{
		StartCoroutine(LoadRaceCoroutine());
	}

	public override void OnEnter()
	{
		base.OnEnter();
		SoundController.instance.fxAudioPause = true;
		if (m_startArgs == null)
		{
			Debug.LogError("Start args is null");
			return;
		}
		GameConsole.Save();
		m_nextRaceState = States.State(m_startArgs.nextStateType) as BaseRaceState;
		switch (m_startArgs.loadingType)
		{
		case StartArgs.LoadingType.Normal:
			InitLoadingContext();
			StartAsyncLoading();
			break;
		case StartArgs.LoadingType.PleaseWait:
			UIWaitDialogContext.Show(base.contextOwner, LocalizationKeys.please_wait.Localized());
			StartAsyncLoading();
			break;
		case StartArgs.LoadingType.NormalEmulation:
			InitLoadingContext();
			OnEndLoading();
			break;
		}
	}

	private void InitLoadingContext()
	{
		m_loadingContext = base.contextOwner.Push<UILoadingScreenContext>();
		if (m_nextRaceState != null)
		{
			m_nextRaceState.TryTakeControlOfLoadingContext(m_loadingContext);
		}
	}

	private void OnEndLoading()
	{
		if (m_nextRaceState == null)
		{
			UILoadingScreenContext loadingContext = m_loadingContext;
			loadingContext.ContinueClickEvent = (Action)Delegate.Combine(loadingContext.ContinueClickEvent, new Action(ChangeToLoadedState));
			m_loadingContext.SetLoadedAndShowContinuePlay(() => true);
		}
		else
		{
			ChangeToLoadedState();
		}
	}

	public override void OnExit()
	{
		m_clearAllContextOnExit = m_nextRaceState == null;
		SoundController.instance.fxAudioPause = false;
		m_loadingContext = null;
		base.OnExit();
	}

	private void ChangeToLoadedState()
	{
		if (!m_onLoadedRized)
		{
			m_onLoadedRized = true;
			if (m_startArgs.nextStateType != null)
			{
				States.Change(m_startArgs.nextStateType);
				m_startArgs.onNextStateActivated.Call(States.current);
				onRaceStateActivatedEvent.Call(States.current);
			}
			CarModelConfig.forcedSyncLoading = false;
			VinylSystemConfig.forcedSyncLoading = false;
		}
	}
}
