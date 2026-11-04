using System.Collections.Generic;
using DB.Meta;
using UnityEngine;

public class FreerideRaceModeState : DriftRaceModeState
{
	private List<GhostData> m_recordedGhostDataList = new List<GhostData>();

	public override RaceMode raceMode => RaceMode.Freeride;

	protected override bool isGhostEnabled => false;

	protected override bool isStoppingOnFinish => false;

	protected override bool isCountdownEnabled => false;

	protected override bool isStartingStopped => true;

	protected override bool allowRepeatDrift => true;

	public override void OnEnter()
	{
		base.OnEnter();
		base.checkpointManager.laps = 2;
		base.checkpointManager.OnLapCompleted += IncreaseLapsCount;
		base.contextOwner.Preload<UIDynostandContext>();
		base.contextOwner.Preload<UIRadialMenuContext>();
	}

	public override void OnExit()
	{
		base.OnExit();
		base.checkpointManager.OnLapCompleted -= IncreaseLapsCount;
	}

	protected override void Init()
	{
		base.Init();
		m_totalTimeLimitSeconds = 0;
		DB.Meta.Track byId = m_model.GetById<DB.Meta.Track>(base.userCar.prefs.globals.lastTrackId);
		if (byId != null && byId.external)
		{
			base.checkpointManager.RegisterStartRaceMode(base.userCar, Time.time);
		}
	}

	protected override void OnRaceCompletedHandle()
	{
		m_recordedGhostDataList = m_ghostRecorder.GetRecorderDataList();
		base.OnRaceCompletedHandle();
	}

	protected override void SetupRaceReplay()
	{
		if (m_recordedGhostDataList == null)
		{
			return;
		}
		m_ghostPlayer.ClearPlayers();
		foreach (GhostData recordedGhostData in m_recordedGhostDataList)
		{
			m_ghostPlayer.AddPlayer(new GhostPlayer.StartArgs
			{
				data = recordedGhostData,
				visibleOnlyInProgress = true
			});
		}
		DestroyUserCar();
		m_ghostPlayer.Play();
	}

	private void IncreaseLapsCount(RaceCar car, int lap, float lapTime, bool isPersonalBestLap, bool isAbsoluteBestLap)
	{
		base.checkpointManager.laps++;
	}

	protected override void CalculateAward(ref DetailedRaceAward award)
	{
	}
}
