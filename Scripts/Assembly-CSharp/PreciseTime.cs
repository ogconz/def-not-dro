using System;
using UnityEngine;

public static class PreciseTime
{
	public enum PlayerLoopPhase
	{
		EarlyUpdate = 0,
		FixedUpdate = 1,
		Update = 2
	}

	private static readonly PrecisionTimer m_internalTimer;

	public static float unscaledDeltaTime => m_internalTimer.unscaledDeltaTime;

	public static long unscaledDeltaTimeMs => m_internalTimer.unscaledDeltaTimeMs;

	public static float fixedUnscaledDeltaTime => m_internalTimer.fixedUnscaledDeltaTime;

	public static long fixedUnscaledDeltaTimeMs => m_internalTimer.fixedUnscaledDeltaTimeMs;

	public static long unscaledTimeMs => m_internalTimer.unscaledTimeMs;

	public static long fixedUnscaledTimeMs => m_internalTimer.fixedUnscaledTimeMs;

	public static long fixedTimeMs => (long)((float)m_internalTimer.fixedUnscaledTimeMs * Time.timeScale);

	public static long realTimeMs => m_internalTimer.realTimeMs;

	public static PlayerLoopPhase loopPhase { get; private set; }

	static PreciseTime()
	{
		m_internalTimer = new PrecisionTimer();
		loopPhase = PlayerLoopPhase.EarlyUpdate;
		Reset();
		TickManager.RegisterEarlyUpdate(EarlyUpdate, -1, "PreciseTime");
		TickManager.RegisterUpdate(Update, -1, "PreciseTime");
		TickManager.RegisterFixedUpdate(FixedUpdate, -1, "PreciseTime");
		TimeController.onGameEngineStop = (Action)Delegate.Combine(TimeController.onGameEngineStop, new Action(OnGameEngineStopHandler));
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Reset()
	{
		m_internalTimer.Reset();
	}

	private static void EarlyUpdate()
	{
		loopPhase = PlayerLoopPhase.EarlyUpdate;
	}

	private static void Update()
	{
		m_internalTimer.Update();
		loopPhase = PlayerLoopPhase.Update;
	}

	private static void FixedUpdate()
	{
		m_internalTimer.FixedUpdate();
		loopPhase = PlayerLoopPhase.FixedUpdate;
	}

	private static void OnGameEngineStopHandler()
	{
		TimeController.onGameEngineStop = (Action)Delegate.Remove(TimeController.onGameEngineStop, new Action(OnGameEngineStopHandler));
		TickManager.UnregisterEarlyUpdate(EarlyUpdate);
		TickManager.UnregisterFixedUpdate(FixedUpdate);
		TickManager.UnregisterUpdate(Update);
	}
}
