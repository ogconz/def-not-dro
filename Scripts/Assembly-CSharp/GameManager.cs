using System;
using System.Collections.Generic;
using DI;
using FMODUnity;
using GameOverlay;
using UnityEngine;

[AddComponentMenu("Scripts/Custom/GameManager")]
public class GameManager : MonoBehaviour
{
	public enum EVideoMoneySource
	{
		FreeCashShop = 0,
		RaceResult = 1
	}

	private const string ENVIRONMENT_VARIABLE_SESSION_TYPE = "XDG_SESSION_TYPE";

	private static readonly List<string> m_environmentXdgSessionTypeValues = new List<string> { "wayland", "x11" };

	private float m_storedTimeScale;

	private bool m_gamePaused;

	public static Action<Texture2D> onMainPlayerAvatarChanged;

	public static float raceDisabledByPhysExplo_Time = 0f;

	public Texture2D steamIcon;

	public Texture2D ps4Icon;

	public Texture2D xboxIcon;

	public Texture2D switchIcon;

	public Texture2D defaultAvatar;

	private static Texture2D m_mainPlayerAvatar = null;

	public static GameManager instance { get; private set; }

	public bool gamePaused => m_gamePaused;

	public static bool isInGarage => States.current is GarageGUIState;

	public bool isUnityUIActive { get; set; }

	public static bool NightModeInstalled => Overlay.instance.IsDLCInstalled(DLCCode.NightMode);

	public static bool SwapsDLCInstalled => Overlay.instance.IsDLCInstalled(DLCCode.Swaps);

	public static bool SlotDLCInstalled => Overlay.instance.IsDLCInstalled(DLCCode.CarPresetsUnlock);

	public static Texture2D mainPlayerAvatar
	{
		get
		{
			return m_mainPlayerAvatar;
		}
		set
		{
			if (m_mainPlayerAvatar != value)
			{
				m_mainPlayerAvatar = value;
				onMainPlayerAvatarChanged.Call(m_mainPlayerAvatar);
			}
		}
	}

	public static Texture2D GetMainPlayerAvatarNotNull()
	{
		if (!UIPrivacyPolicyEULAContext.CheckIsEulaAccess())
		{
			return instance.defaultAvatar;
		}
		if (!(m_mainPlayerAvatar != null))
		{
			return instance.defaultAvatar;
		}
		return m_mainPlayerAvatar;
	}

	public void SetGamePause(bool pause)
	{
		if (m_gamePaused != pause)
		{
			m_gamePaused = pause;
			if (m_gamePaused)
			{
				m_storedTimeScale = Time.timeScale;
				Time.timeScale = 0f;
			}
			else
			{
				Time.timeScale = m_storedTimeScale;
			}
		}
	}

	private void Awake()
	{
		instance = this;
		Overlay.OnPlayerAvatarDecoded.Subscribe(GameEventFlag.Global, delegate(Overlay.AvatarInfo e)
		{
			if (Overlay.instance.player.id == e.userId)
			{
				mainPlayerAvatar = e.avatar;
			}
		});
		FocusState.onChanged = (Action<bool>)Delegate.Combine(FocusState.onChanged, (Action<bool>)delegate(bool focus)
		{
			RuntimeManager.MuteAllEvents(!focus);
		});
	}

	private void Update()
	{
		if (Time.timeScale == 0f)
		{
			HotkeyManager.FixedUpdate();
		}
	}

	private void FixedUpdate()
	{
		HotkeyManager.FixedUpdate();
	}

	public static void GameRestart()
	{
		GameLog.Print(AlertLevel.Notify, "System", "Restart game...");
		DependencyInjector.Resolve<GraphicsQuality>().ApplySettings();
		LocalizationService.ClearData();
		States.Change<StartState>();
	}

	public static void ClearAllGameData()
	{
		CryptoPlayerPrefs.DeleteAll();
	}

	public Texture2D GetPlatformIcon(UserPlatform.Id platformId)
	{
		return platformId switch
		{
			UserPlatform.Id.PS4 => ps4Icon, 
			UserPlatform.Id.Xbox => xboxIcon, 
			UserPlatform.Id.Switch => switchIcon, 
			_ => steamIcon, 
		};
	}

	public static bool IsLinuxOsPlatform()
	{
		string item = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE")?.ToLower();
		return m_environmentXdgSessionTypeValues.Contains(item);
	}
}
