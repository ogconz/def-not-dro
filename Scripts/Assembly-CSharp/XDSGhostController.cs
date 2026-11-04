using System;
using System.Collections;
using System.Collections.Generic;
using DI;
using SyncMultiplayer;
using UnityEngine;

public class XDSGhostController
{
	public enum GhostMode
	{
		FirstRace = 0,
		SecondRace = 1,
		Replay = 2,
		RecordReplay = 3
	}

	public GhostRecorder recorder;

	public GhostPlayer player;

	public GhostData[] RecordGhosts;

	private GhostMode m_lastActivatedMode;

	private Dictionary<string, GhostData> m_savedRecordData = new Dictionary<string, GhostData>();

	public void PrepareForMode(GhostMode mode)
	{
		switch (mode)
		{
		case GhostMode.FirstRace:
			StopPlay();
			StopRecord();
			break;
		case GhostMode.SecondRace:
			StopPlay();
			StopRecord();
			LoadGhost(1, 1);
			break;
		case GhostMode.Replay:
			StopPlay();
			StopRecord();
			LoadGhost(2, 1);
			LoadGhost(2, 2);
			break;
		case GhostMode.RecordReplay:
		{
			StopPlay();
			StopRecord();
			GhostData[] recordGhosts = RecordGhosts;
			foreach (GhostData ghostData in recordGhosts)
			{
				LoadGhost(ghostData);
			}
			break;
		}
		}
		if (player != null)
		{
			player.CurTimeOverride(1f);
			foreach (GhostPlayer.Player player in player.players)
			{
				player.Play(1f, networkMode: false);
			}
		}
		TimeController.StartNow(HackResetGhostPlayer(resetRpmAndAccel: true));
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
	}

	private IEnumerator HackResetGhostPlayer(bool resetRpmAndAccel)
	{
		yield return new WaitForFixedUpdate();
		if (player == null)
		{
			yield break;
		}
		foreach (GhostPlayer.Player player in player.players)
		{
			if (!player.isPlay && player.ghostCar != null)
			{
				player.Play(1f, networkMode: false);
				Rigidbody getRigidbody = player.ghostCar.getRigidbody;
				player.ghostCar.getTransform.SetPositionAndRotation(getRigidbody.position, getRigidbody.rotation);
				if (resetRpmAndAccel)
				{
					player.ghostCar.carX.accelerate = 0f;
					player.ghostCar.carX.rpm = player.ghostCar.carX.engineCutRPM * 1.1f;
				}
			}
		}
	}

	public void ActivateMode(GhostMode mode, RaceCar playerCar, float greenlightTime)
	{
		m_lastActivatedMode = mode;
		switch (mode)
		{
		case GhostMode.FirstRace:
			StopPlay();
			CaptureGhost(playerCar);
			Record();
			break;
		case GhostMode.SecondRace:
			CaptureGhost(player.ghostCar, player.ghostData);
			CaptureGhost(playerCar);
			Play(greenlightTime);
			Record();
			break;
		case GhostMode.Replay:
			StopRecord();
			Play(greenlightTime);
			break;
		}
		if (player == null)
		{
			return;
		}
		Track instance = Track.instance;
		foreach (GhostPlayer.Player player in player.players)
		{
			player.ghostCar.SetCurrentTrack(instance);
		}
	}

	private void LoadGhost(int raceIndex, int playerIndex)
	{
		if (player == null)
		{
			player = new GhostPlayer();
		}
		GhostData value = new GhostData();
		string ghostRecordName = GetGhostRecordName(raceIndex, playerIndex);
		if (!m_savedRecordData.TryGetValue(ghostRecordName, out value))
		{
			value = null;
		}
		LoadGhost(value);
	}

	private void LoadGhost(GhostData ghostData)
	{
		if (player == null)
		{
			player = new GhostPlayer();
		}
		if (ghostData != null)
		{
			player.AddPlayer(new GhostPlayer.StartArgs
			{
				data = ghostData,
				needCollisions = true
			});
		}
		player.Pause();
	}

	public void RaceStart()
	{
		if (recorder != null)
		{
			recorder.RaceStart();
		}
	}

	private void CaptureGhost(RaceCar car, GhostData parent = null)
	{
		if (recorder == null)
		{
			recorder = new GhostRecorder();
		}
		recorder.AddRecordSource(car, parent);
	}

	private void Play(float greenlightTime)
	{
		if (player != null)
		{
			player.CurTimeOverride(0f);
			player.Play();
		}
	}

	private void Record()
	{
		if (recorder != null)
		{
			recorder.Record();
		}
	}

	public void PlayBackgroundGhosts()
	{
		ActivateMode(GhostMode.Replay, null, 0f);
		if (player != null && player.players.Count > 1)
		{
			player.IgnoreCollisions();
			player.CurTimeOverride(2f);
			player.Play();
			CameraSwitch.instance.SetCar(player.players[1].ghostCar);
			NetworkPlayer localPlayer = NetworkController.InstanceGame.LocalPlayer;
			localPlayer.userCar = player.players[1].ghostCar;
			NetworkPlayer networkPlayer = new NetworkPlayer(NetworkController.InstanceGame, isLocal: false, localPlayer.PlayerProperties);
			networkPlayer.userCar = player.players[0].ghostCar;
			player.ghostCar.networkPlayer = networkPlayer;
			SubroomInfo attachedRoom = (localPlayer.attachedRoom = new SubroomInfo());
			networkPlayer.attachedRoom = attachedRoom;
			CameraSwitch.instance.ActivateReplayCamera();
		}
	}

	private void StopPlay()
	{
		if (player != null)
		{
			player.ClearPlayers();
		}
		player = null;
	}

	private void StopRecord()
	{
		if (recorder != null)
		{
			recorder.Stop();
			recorder.ClearRecorderSource();
			recorder = null;
		}
	}

	public void StopCars()
	{
		if (player != null)
		{
			if (recorder != null && recorder.isRecording)
			{
				recorder.RiseSuspended(isSuspend: false);
			}
			player.StopCars();
		}
	}

	private string GetGhostRecordName(int raceIndex, int playerIndex)
	{
		string name = DependencyInjector.Resolve<GamePrefs>().globals.lastMetaConfig.name;
		return string.Concat(RaceMode.XDS.ToString() + "_" + name, "_", raceIndex.ToString(), playerIndex.ToString());
	}

	public void SaveGhosts()
	{
		if (recorder == null)
		{
			return;
		}
		if (recorder.isRecording)
		{
			recorder.Stop();
		}
		int raceIndex = ((States.mainCurrent as XDSRaceModeState).firstRace ? 1 : 2);
		for (int i = 0; i < recorder.Count(); i++)
		{
			GhostData recordedData = recorder.GetRecordedData(i);
			string ghostRecordName = GetGhostRecordName(raceIndex, i + 1);
			if (recordedData != null)
			{
				m_savedRecordData[ghostRecordName] = recordedData;
			}
		}
	}

	public void Release()
	{
		Stop();
		m_savedRecordData.Clear();
		m_lastActivatedMode = GhostMode.FirstRace;
	}

	public void Stop()
	{
		StopRecord();
		StopPlay();
	}

	public static implicit operator bool(XDSGhostController x)
	{
		return x != null;
	}
}
