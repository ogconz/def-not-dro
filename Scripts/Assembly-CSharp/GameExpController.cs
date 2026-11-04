using System;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using DB;
using DB.Meta;
using DI;
using GameOverlay;
using SyncMultiplayer;
using UnityEngine;

public class GameExpController : MonoBehaviour
{
	public enum ExpSource
	{
		Drift = 0,
		Race = 1,
		Time = 2,
		Cup = 3,
		XDS = 4
	}

	private const float TIME_FOR_START_MP = 60f;

	private GlobalGameSettings.ExpSettings m_settings;

	private GamePrefs m_prefs;

	private NetworkGameStats m_networkStats;

	private ObscuredInt m_raceExp;

	private bool m_isMultiplayer;

	private bool m_expForStartTaken = true;

	private float m_giveExpForStartTime;

	private Dictionary<ExpSource, int> m_expBySources = new Dictionary<ExpSource, int>();

	public int raceExp => (int)m_raceExp + (m_isMultiplayer ? ((int)(m_prefs.expFactor * (float)m_networkStats.xp)) : 0);

	private void Init()
	{
		m_raceExp.ApplyNewCryptoKey();
		Overlay.OnReady = (Action)Delegate.Combine(Overlay.OnReady, new Action(OnOverlayReady));
	}

	private void OnOverlayReady()
	{
		m_settings = DependencyInjector.Resolve<BaseModel>().GetSingle<GlobalGameSettings>().expSettings;
		m_prefs = DependencyInjector.Resolve<GamePrefs>();
		m_networkStats = NetworkController.InstanceGame.stats;
	}

	public void Reset()
	{
		m_raceExp = 0;
		m_expBySources.Clear();
		m_networkStats.Clear();
	}

	public void Apply()
	{
		int num = (int)((float)(int)m_raceExp * m_prefs.expFactor);
		m_prefs.playerProfile.exp += num;
		Reset();
		m_prefs.playerProfile.UnlockNewLevelFeatures();
		AnalyticsService.EarnExp(num);
	}

	public void Update()
	{
		if (!m_expForStartTaken && m_isMultiplayer && Time.unscaledTime > m_giveExpForStartTime)
		{
			m_expForStartTaken = true;
			m_raceExp = (int)m_raceExp + m_settings.mp_raceStart;
			AddExpToSource(ExpSource.Race, m_settings.mp_raceStart);
			ObscuredInt obscuredInt = m_raceExp;
			GameLog.Print(AlertLevel.Notify, "PlayerProfile", "(exp) exp for race start, exp is " + obscuredInt.ToString());
		}
	}

	public int GetExpBySource(ExpSource source)
	{
		int value = 0;
		m_expBySources.TryGetValue(source, out value);
		return value;
	}

	public void SetExpBySource(ExpSource source, int amount)
	{
		int value = 0;
		if (!m_expBySources.TryGetValue(source, out value))
		{
			m_expBySources.Add(source, amount);
		}
		else
		{
			m_expBySources[source] = amount;
		}
	}

	public void AddExpToSource(ExpSource source, int amount)
	{
		int value = 0;
		amount *= (int)m_prefs.expFactor;
		if (!m_expBySources.TryGetValue(source, out value))
		{
			m_expBySources.Add(source, amount);
		}
		else
		{
			m_expBySources[source] = value + amount;
		}
	}

	public void HandleXDSFinishRace(int exp)
	{
		m_raceExp = (int)m_raceExp + exp;
		AddExpToSource(ExpSource.XDS, exp);
	}

	private void Start()
	{
		Init();
		GameEvents.OnRaceLoaded.Subscribe(GameEventFlag.Global, delegate
		{
			Reset();
			m_expForStartTaken = false;
			if (States.current is LoadRaceState)
			{
				m_isMultiplayer = (States.current as LoadRaceState).currentRaceMode == RaceMode.NetworkFreeride;
			}
			else
			{
				m_isMultiplayer = States.mainCurrent is SyncNetFreerideRaceModeState;
			}
			m_giveExpForStartTime = Time.unscaledTime + 60f;
			if (!m_isMultiplayer)
			{
				m_raceExp = m_settings.raceStart;
				AddExpToSource(ExpSource.Race, m_settings.raceStart);
			}
			ObscuredInt obscuredInt = m_raceExp;
			GameLog.Print(AlertLevel.Notify, "PlayerProfile", "(exp) race start, exp is " + obscuredInt.ToString());
		});
		GameEvents.OnDriftExpEarned.Subscribe(GameEventFlag.Global, delegate(GameEvents.DriftExpInfo e)
		{
			if (e.expPoints > 0f)
			{
				int num = Mathf.RoundToInt(e.expPoints);
				m_raceExp = (int)m_raceExp + num;
				AddExpToSource(ExpSource.Drift, num);
				string text = num.ToString();
				ObscuredInt obscuredInt = m_raceExp;
				GameLog.Print(AlertLevel.Notify, "PlayerProfile", "(exp " + text + ") drift taken, exp is " + obscuredInt.ToString());
			}
		});
		GameEvents.OnRaceCupEarned.Subscribe(GameEventFlag.Global, delegate(GameEvents.CupInfo e)
		{
			int num = 0;
			switch (e.cupIndex)
			{
			case 1:
				num = m_settings.gold;
				break;
			case 2:
				num = m_settings.silver;
				break;
			case 3:
				num = m_settings.bronze;
				break;
			}
			m_raceExp = (int)m_raceExp + num;
			AddExpToSource(ExpSource.Cup, num);
		});
		GameEvents.OnRaceEnd.Subscribe(GameEventFlag.Global, delegate(GameEvents.RaceEndInfo e)
		{
			m_expForStartTaken = true;
			ObscuredInt obscuredInt;
			if (e != null)
			{
				if (!e.isCompleted && !m_isMultiplayer)
				{
					obscuredInt = m_raceExp;
					GameLog.Print(AlertLevel.Notify, "PlayerProfile", "(exp) race canceled, exp is discarded [" + obscuredInt.ToString() + "]");
					m_raceExp = 0;
					return;
				}
				if (m_isMultiplayer)
				{
					m_networkStats.SendUserActiveSignal();
					int num = Mathf.RoundToInt((Time.unscaledTime - m_networkStats.gameStart - m_networkStats.badTime) * m_settings.elapsedPerSecond);
					m_raceExp = (int)m_raceExp + num;
					AddExpToSource(ExpSource.Time, num);
					if (e.award != null)
					{
						GameLog.Print(AlertLevel.Notify, "PlayerProfile", "(exp) race finished, exp for time (" + e.award.raceTimeSpent + ") spent is " + num);
					}
					GameLog.Print(AlertLevel.Notify, "PlayerProfile", "(exp) idle time is " + m_networkStats.badTime);
				}
				else if (e.award != null)
				{
					int num2 = Mathf.RoundToInt(e.award.raceTimeLeft * m_settings.estimatePerSecond);
					m_raceExp = (int)m_raceExp + num2;
					AddExpToSource(ExpSource.Time, num2);
					GameLog.Print(AlertLevel.Notify, "PlayerProfile", "(exp) race finished, exp for time (" + e.award.raceTimeLeft + ") left is " + num2);
				}
			}
			if (m_isMultiplayer)
			{
				if (Time.unscaledTime > m_giveExpForStartTime)
				{
					m_raceExp = (int)m_raceExp + m_settings.mp_raceEnd;
					AddExpToSource(ExpSource.Race, m_settings.mp_raceEnd);
				}
			}
			else
			{
				m_raceExp = (int)m_raceExp + m_settings.raceEnd;
				AddExpToSource(ExpSource.Race, m_settings.raceEnd);
			}
			obscuredInt = m_raceExp;
			GameLog.Print(AlertLevel.Notify, "PlayerProfile", "(exp) race finished, exp is " + obscuredInt.ToString());
		});
	}
}
