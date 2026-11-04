using System;
using System.Collections;
using System.Runtime.InteropServices;
using BestHTTP;
using CarX;
using CarX.Points;
using Chat;
using DG.Tweening;
using DI;
using GameInput;
using GameOverlay;
using MessagePack;
using MessagePack.Resolvers;
using MessagePack.Unity;
using MessagePack.Unity.Extension;
using PrefsModels;
using Quests;
using SyncMultiplayer;
using UnityEngine;

public class KeepAliveManager : MonoBehaviour
{
	[SerializeField]
	private GameObject[] productionOnlyObjects;

	[SerializeField]
	private GameObject[] testOnlyObjects;

	[SerializeField]
	private GameObject engineStartNode;

	[SerializeField]
	private Canvas godVoiceRoot;

	[Header("Only XBOX (or Editor data link)")]
	[SerializeField]
	private XboxDataContainer m_xboxDataContainer;

	private static bool m_serializerRegistered;

	private IGameOverlay m_overlay;

	private bool m_engineStarted;

	private bool m_isSyncWasCalled;

	public const string SKIP_HIDE_CANVAS_TAG = "SkipHideCanvas";

	private Coroutine m_authorizedCoroutine;

	public static KeepAliveManager instance { get; private set; }

	public static bool isUIHidden { get; private set; }

	public static event System.Action onUIVisibleChanged;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Initialize()
	{
		DOTween.SetTweensCapacity(10000, 100);
		if (!m_serializerRegistered)
		{
			StaticCompositeResolver.Instance.Register(BuiltinResolver.Instance, AttributeFormatterResolver.Instance, UnityResolver.Instance, MessagePackResolver.Instance, PrimitiveObjectResolver.Instance, UnityBlitWithPrimitiveArrayResolver.Instance, StandardResolver.Instance);
			MessagePackSerializer.DefaultOptions = MessagePackSerializerOptions.Standard.WithResolver(StaticCompositeResolver.Instance);
			m_serializerRegistered = true;
		}
	}

	private void Awake()
	{
		instance = this;
		_ = productionOnlyObjects;
		_ = testOnlyObjects;
		UnityEngine.Object.DontDestroyOnLoad(this);
		GameObject[] array = null;
		array = testOnlyObjects;
		if (array == null)
		{
			GameLog.Print(AlertLevel.Error, "System", "Unknown build type!");
		}
		else
		{
			GameObject[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				UnityEngine.Object.Destroy(array2[i]);
			}
		}
		HTTPManager.Setup();
		CarPartExternalLoader.Initialize();
		if (Application.platform != RuntimePlatform.XboxOne)
		{
			BasePrefsModel.InitSaveSystem();
		}
		GameConsole.PreLoad();
		m_overlay = Overlay.Create();
		if (m_overlay == null)
		{
			MessageBox(0, (Overlay.instance == null) ? "Init error" : Overlay.instance.GetInitiateAttachErrorText(), "error", 262160u);
			base.enabled = false;
			TerminateApplication();
			return;
		}
		GameEvents.OnGameEngineStart += new GameEventHandler<GameEvents.EventInfo>(OnGameEngineStart);
		States.onStateActivate += delegate
		{
			if (!(States.mainCurrent is BaseRaceState))
			{
				SetUIVisible(isVisible: true);
			}
		};
		ChatOverlay.Initialize();
		QuestController.Initialize();
	}

	public static void Quit()
	{
		TimeController.StartNow(instance.TrySyncAndQuit());
	}

	private void TerminateApplication()
	{
		m_isSyncWasCalled = true;
		Quit();
	}

	private IEnumerator TrySyncAndQuit()
	{
		if (m_authorizedCoroutine != null)
		{
			TimeController.StopNow(m_authorizedCoroutine);
		}
		if (!m_isSyncWasCalled && Overlay.IsReady())
		{
			yield return DelayedSyncAndQuit();
		}
		else
		{
			Finalization();
			_ = m_isSyncWasCalled;
		}
		Application.Quit();
	}

	private bool WantsToQuit()
	{
		Quit();
		return false;
	}

	private IEnumerator DelayedSyncAndQuit()
	{
		try
		{
			m_isSyncWasCalled = true;
			BaseGameState baseGameState = States.current as BaseGameState;
			if (baseGameState != null)
			{
				UIWaitDialogContext.Show(baseGameState.contextOwner);
				Time.timeScale = 1f;
			}
			yield return GameSync.Sync();
			Finalization();
		}
		finally
		{
			Application.wantsToQuit -= WantsToQuit;
			Application.Quit();
		}
	}

	private void Finalization()
	{
		try
		{
			GameEvents.GameEngineStop();
			GameConsole.Save();
			BasePrefsModel.SaveAllQueries(inOneFrame: true);
		}
		catch
		{
		}
		finally
		{
			if (Overlay.IsReady())
			{
				GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
				gamePrefs.SaveAll();
				gamePrefs.MakeBackup();
				Overlay.instance.Shutdown();
			}
		}
	}

	private void Update()
	{
		if (Time.timeScale == 0f)
		{
			FixedUpdate();
		}
		m_overlay?.Update();
		if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.BackQuote))
		{
			BaseGameState baseGameState = States.current as BaseGameState;
			if (baseGameState != null && !(baseGameState is LoadRaceState))
			{
				UIConsoleUGUIContext.Toggle(baseGameState.contextOwner);
			}
		}
		BasePrefsModel.UpdateDefferedSaves();
		QuestController.Update();
	}

	private void FixedUpdate()
	{
		if (!m_engineStarted)
		{
			return;
		}
		InputManager inputManager = InputManager.instance;
		if (inputManager == null || inputManager.pollingMode)
		{
			return;
		}
		BaseState current = States.current;
		XDSRaceModeState xDSRaceModeState = current as XDSRaceModeState;
		if (!(current is FreeDroneState) && !InputManager.IsInputLocked() && inputManager.IsActionPressed(ActionType.HideUI) && ((xDSRaceModeState != null && (bool)xDSRaceModeState.userCar && !xDSRaceModeState.userCar.lockState) || (xDSRaceModeState == null && current is BaseRaceState { isRaceEnded: false, startPlayPressed: not false })))
		{
			inputManager.PlayClickEffect(null, delegate
			{
				ToggleUIInRaceMode();
			}, skipPauseToAction: true, skipUIHiddenTest: true);
		}
	}

	private void OnGUI()
	{
	}

	public static bool SetUIVisible(bool isVisible)
	{
		isUIHidden = !isVisible;
		KeepAliveManager.onUIVisibleChanged.SafeCall();
		Canvas[] array = UnityEngine.Object.FindObjectsOfType<Canvas>(includeInactive: true);
		if (array == null)
		{
			return true;
		}
		Canvas[] array2 = array;
		foreach (Canvas canvas in array2)
		{
			if ((bool)canvas && !canvas.CompareTag("SkipHideCanvas") && canvas != instance.godVoiceRoot)
			{
				canvas.enabled = !isUIHidden;
			}
		}
		if (InputManager.instance != null)
		{
			InputManager.instance.SetCursorVisibility(isVisible);
		}
		return isUIHidden;
	}

	public static bool ToggleUIInRaceMode()
	{
		if (!isUIHidden)
		{
			if (States.current is BaseRaceState)
			{
				BindElement actionBindOfLastInput = InputManager.instance.GetActionBindOfLastInput(ActionType.HideUI, allowFallbackToKeyboard: true);
				if (actionBindOfLastInput != null)
				{
					GUICommonGodVoice.ShowText(LocalizationKeys.hint_show_ui.LocalizedFormat(actionBindOfLastInput.elementName), 3f);
				}
				else
				{
					string text = LocalizationKeys.input_action_.AddLocalized(ActionType.HideUI.ToString().ToLower());
					GUICommonGodVoice.ShowText(LocalizationKeys.hint_show_ui.LocalizedFormat("\"" + text + "\""), 3f);
				}
				return SetUIVisible(isVisible: false);
			}
			return isUIHidden;
		}
		return SetUIVisible(isVisible: true);
	}

	private void OnGameEngineStart(GameEvents.EventInfo e)
	{
		GameLog.Print(AlertLevel.Notify, "System", $"Game ver: {GameVersion.versionAsString} ({GameVersion.version}) CarX Engine ver: {Simulator.version} Net ver: {GameVersion.networkVersion} XDS ver: {NativeAPI.GetXDSVersion()}");
		m_engineStarted = true;
		m_authorizedCoroutine = TimeController.StartNow(GameSync.KeepAuthorizedAndSyncCoroutine());
		engineStartNode.SetActive(value: true);
		Application.wantsToQuit += WantsToQuit;
	}

	private void OnApplicationFocus(bool focus)
	{
		FocusState.OnApplicationFocusCallback(focus);
		if (focus)
		{
			GameSync.ForceAuthorizeTime();
		}
	}

	private void OnApplicationQuit()
	{
		ChatOverlay.Release();
		CarXDboxManager.instance?.Terminate();
		NetworkController.InstanceGame?.Disconnect();
	}

	[DllImport("user32.dll")]
	public static extern int MessageBox(int hWnd, string text, string caption, uint type);
}
