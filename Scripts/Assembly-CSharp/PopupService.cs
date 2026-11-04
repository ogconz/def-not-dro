using System;
using System.Collections.Generic;
using DI;
using UnityEngine;

public class PopupService : MonoBehaviour
{
	private abstract class BasePopupHandler
	{
		protected readonly GamePrefs m_gamePrefs;

		protected DateTime lastShown;

		protected BasePopupHandler()
		{
			m_gamePrefs = DependencyInjector.Resolve<GamePrefs>();
			lastShown = m_gamePrefs.popupLastShown.GetPopupLastShownDate(GetPopupType());
		}

		private string GetPopupType()
		{
			return GetType().Name;
		}

		protected abstract bool OnRaceEndInternal(ContextOwner contextOwner, int raceNumberSession, int raceNumberTotal);

		private bool UpdateLastShowIfNeeded(bool isShown)
		{
			if (isShown)
			{
				lastShown = m_gamePrefs.popupLastShown.UpdatePopupLastShownDate(GetPopupType());
			}
			return isShown;
		}

		public bool OnRaceEnd(ContextOwner contextOwner)
		{
			int racesPlayed = m_gamePrefs.stats.racesPlayed;
			int racesPlayedCountSinceSessionStart = m_gamePrefs.stats.GetRacesPlayedCountSinceSessionStart();
			bool isShown = OnRaceEndInternal(contextOwner, racesPlayedCountSinceSessionStart, racesPlayed);
			return UpdateLastShowIfNeeded(isShown);
		}
	}

	private List<BasePopupHandler> m_allHandlers;

	private BaseRaceModeState m_raceModeState;

	private bool m_isEnabled;

	public bool isEnabled
	{
		get
		{
			return m_isEnabled;
		}
		set
		{
			if (m_isEnabled != value)
			{
				OnEnableChanged(value);
			}
		}
	}

	private void OnEnableChanged(bool value)
	{
		m_isEnabled = value;
	}

	private void Awake()
	{
		this.MakeDependenciesInjected();
		m_allHandlers = new List<BasePopupHandler>();
		States.onStateActivate += OnEnterToNewState;
		OnEnableChanged(value: true);
	}

	private void OnDestroy()
	{
		States.onStateActivate -= OnEnterToNewState;
	}

	private void UnsubscribeFinished()
	{
		if (m_raceModeState != null)
		{
			BaseRaceModeState raceModeState = m_raceModeState;
			raceModeState.onFinishedEvent = (Action<BaseRaceModeState, BaseRaceModeState.DetailedRaceAward>)Delegate.Remove(raceModeState.onFinishedEvent, new Action<BaseRaceModeState, BaseRaceModeState.DetailedRaceAward>(OnFinishedEvent));
			m_raceModeState = null;
		}
	}

	private void SubscribeFinished(BaseRaceModeState modeState)
	{
		m_raceModeState = modeState;
		BaseRaceModeState raceModeState = m_raceModeState;
		raceModeState.onFinishedEvent = (Action<BaseRaceModeState, BaseRaceModeState.DetailedRaceAward>)Delegate.Combine(raceModeState.onFinishedEvent, new Action<BaseRaceModeState, BaseRaceModeState.DetailedRaceAward>(OnFinishedEvent));
	}

	private void OnRaceEnd(ContextOwner contextOwner)
	{
		for (int i = 0; i < m_allHandlers.Count && !m_allHandlers[i].OnRaceEnd(contextOwner); i++)
		{
		}
	}

	private void OnEnterToNewState(BaseState baseState)
	{
		UnsubscribeFinished();
		if (baseState is BaseRaceModeState)
		{
			SubscribeFinished((BaseRaceModeState)baseState);
		}
	}

	private void OnFinishedEvent(BaseRaceModeState state, BaseRaceModeState.DetailedRaceAward award)
	{
		if (isEnabled)
		{
			OnRaceEnd(m_raceModeState.contextOwner);
		}
	}
}
