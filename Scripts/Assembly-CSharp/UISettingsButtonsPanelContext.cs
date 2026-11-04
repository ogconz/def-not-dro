using System;
using Binding;
using Binding.Base;
using DG.Tweening;
using DI;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using GameOverlay;
using UnityEngine;

public class UISettingsButtonsPanelContext : BaseBindingBehaviourTarget
{
	public enum SettingsButtonType
	{
		Game = 0,
		Graphics = 1,
		Input = 2,
		Sounds = 3,
		DLC = 4,
		Discord = 5,
		EpicServices = 6,
		Credits = 7,
		Exit = 8
	}

	private const string EpicGamesSite = "https://www.epicgames.com/";

	[NonSerialized]
	private BaseBindingBehaviourTarget m_parentContext;

	public readonly BoolProperty enableDLC = new BoolProperty(startValue: true);

	public readonly BoolProperty enableEpic = new BoolProperty(startValue: true);

	public readonly BoolProperty standaloneBuild = new BoolProperty();

	private readonly ColorProperty m_epicConnected = new ColorProperty();

	private readonly StringProperty m_epicConnectedText = new StringProperty();

	private Result? m_result;

	private Action<SettingsButtonType> m_onPressSettingsButtonAction;

	private BaseBindingBehaviourTarget parentContext
	{
		get
		{
			if (m_parentContext == null)
			{
				m_parentContext = GetComponentInParent<BaseBindingBehaviourTarget>();
			}
			return m_parentContext;
		}
	}

	public void SubscribePressSettingsButton(Action<SettingsButtonType> action)
	{
		m_onPressSettingsButtonAction = (Action<SettingsButtonType>)Delegate.Combine(m_onPressSettingsButtonAction, action);
	}

	public void UnsubscribePressSettingsButton(Action<SettingsButtonType> action)
	{
		m_onPressSettingsButtonAction = (Action<SettingsButtonType>)Delegate.Remove(m_onPressSettingsButtonAction, action);
	}

	private void Awake()
	{
		SetPlatformView();
	}

	public void SetOwner(BaseBindingBehaviourTarget parentCtxParam)
	{
		m_parentContext = parentCtxParam;
	}

	private void SetPlatformView()
	{
		standaloneBuild.value = true;
	}

	public void RegisterPanel()
	{
		UnregisterPanel();
		NavigationInputModule.instance.RegisterContext(this);
	}

	public void UnregisterPanel()
	{
		NavigationInputModule.instance.UnregisterContext(this);
	}

	private void Start()
	{
		RegisterPanel();
	}

	private void ValidStatusEpic()
	{
		if (!(Overlay.instance is SteamOverlay steamOverlay))
		{
			return;
		}
		Result? epicResultCode = steamOverlay.GetEpicResultCode();
		if (m_result == epicResultCode)
		{
			return;
		}
		m_result = epicResultCode;
		Result? result = epicResultCode;
		if (result.HasValue)
		{
			if (result.GetValueOrDefault() == Result.Success && steamOverlay.IsEpicPlatformConnected())
			{
				DOTween.To(() => m_epicConnected.value, delegate(Color newValue)
				{
					m_epicConnected.value = newValue;
				}, Color.green, 1f).Play();
				m_epicConnectedText.value = "Linked";
			}
			else
			{
				DOTween.To(() => m_epicConnected.value, delegate(Color newValue)
				{
					m_epicConnected.value = newValue;
				}, new Color(1f, 0.5f, 0f, 1f), 1f).Play();
				m_epicConnectedText.value = "Error";
			}
		}
		else
		{
			DOTween.To(() => m_epicConnected.value, delegate(Color newValue)
			{
				m_epicConnected.value = newValue;
			}, Color.clear, 1f).Play();
			m_epicConnectedText.value = string.Empty;
		}
	}

	protected override void OnDestroy()
	{
		UnregisterPanel();
		base.OnDestroy();
	}

	public void GoToGameSettings()
	{
		if (!(m_parentContext == null))
		{
			UIGeneralSettingsContext.ShowSettings(parentContext.owner, ETypeSettings.Game);
			m_onPressSettingsButtonAction.SafeCall(SettingsButtonType.Game);
		}
	}

	public void GoToSoundsSettings()
	{
		if (!(m_parentContext == null))
		{
			UIGeneralSettingsContext.ShowSettings(parentContext.owner, ETypeSettings.Sounds);
			m_onPressSettingsButtonAction.SafeCall(SettingsButtonType.Sounds);
		}
	}

	public void GoToGraphicsSettings()
	{
		if (!(m_parentContext == null))
		{
			UIGeneralSettingsContext.ShowSettings(parentContext.owner, ETypeSettings.Graphics);
			m_onPressSettingsButtonAction.SafeCall(SettingsButtonType.Graphics);
		}
	}

	public void GoToInputSettings()
	{
		if (!(m_parentContext == null))
		{
			parentContext.owner.HideStackAndPush<UIInputSettingsContext>();
			m_onPressSettingsButtonAction.SafeCall(SettingsButtonType.Input);
		}
	}

	public void GoToDLC()
	{
		if (Overlay.instance is SteamOverlay steamOverlay)
		{
			steamOverlay.ShowDLCList();
			m_onPressSettingsButtonAction.SafeCall(SettingsButtonType.DLC);
		}
	}

	public void GoToDiscord()
	{
		UIMessageBox.ShowQuestion(LocalizationKeys.prevention_discord_message.Localized(), delegate
		{
			m_onPressSettingsButtonAction.SafeCall(SettingsButtonType.Discord);
		}, LocalizationKeys.Cancel.Localized(), delegate
		{
			Application.OpenURL("https://discord.gg/bXpFeuU");
		}, LocalizationKeys.Yes.Localized());
	}

	public void GoToEpicServices()
	{
		if (DependencyInjector.Resolve<GamePrefs>() == null)
		{
			return;
		}
		IGameOverlay instance = Overlay.instance;
		SteamOverlay steam = instance as SteamOverlay;
		if (steam == null || steam.IsEpicPlatformConnected())
		{
			return;
		}
		steam.AuthEpicViaSteamIntegratedPlatform(delegate(LoginCallbackInfo? token, Result result)
		{
			if (result != Result.Success && token.HasValue)
			{
				UIMessageBox.ShowQuestion(LocalizationKeys.prevention_epic_games_message.Localized(), delegate
				{
					m_onPressSettingsButtonAction.SafeCall(SettingsButtonType.EpicServices);
				}, LocalizationKeys.Cancel.Localized(), delegate
				{
					steam.CreateAndLinkAccount(token.Value);
				}, LocalizationKeys.Yes.Localized());
			}
		});
	}

	private void Update()
	{
		ValidStatusEpic();
	}

	public void GoToCredits()
	{
		if (!(parentContext == null))
		{
			parentContext.owner.HideStackAndPush<UICreditsListContext>();
			m_onPressSettingsButtonAction.SafeCall(SettingsButtonType.Credits);
		}
	}

	public void GoToExit()
	{
		if (UIMainMenuContext.IsExitDisabled())
		{
			m_onPressSettingsButtonAction.SafeCall(SettingsButtonType.Exit);
		}
		else
		{
			UIMessageBox.ShowQuestion(LocalizationKeys.exit_question.Localized(), delegate
			{
			}, LocalizationKeys.Cancel.Localized(), KeepAliveManager.Quit, LocalizationKeys.Exit.Localized());
		}
	}

	protected override void OnFailedMoveSelection(NavigateDirection direction)
	{
		UIMainMenuContext uIMainMenuContext = m_parentContext as UIMainMenuContext;
		if (uIMainMenuContext != null)
		{
			uIMainMenuContext.OnFailedMoveByNavigation(direction);
		}
	}
}
