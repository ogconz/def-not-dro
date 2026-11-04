using System;
using System.Collections.Generic;
using ArrayExtension;
using DI;
using PrefsModels;
using UnityEngine;

public class GhostRecorder
{
	private class Recorder
	{
		public GhostData parent;

		public bool isSuspended;

		public GhostData data = new GhostData();

		private RaceCar m_raceCar;

		private NetworkRaceState m_networkRaceState;

		public Recorder(RaceCar raceCar, NetworkRaceState networkRaceState = null)
		{
			m_raceCar = raceCar;
			m_networkRaceState = networkRaceState;
			data.carId = m_raceCar.metaInfo.id;
			data.profileId = m_raceCar.profileMetaInfo.id;
			data.swapId = m_raceCar.swap.engineId;
			GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
			CustomProfile installedCustomProfile = gamePrefs.carSettings.GetInstalledCustomProfile(data.carId);
			data.profile = gamePrefs.carSettings.GetProfileInfo(data.profileId, data.carId, installedCustomProfile?.id);
			data.visual = gamePrefs.carSettings.GetVisualForCar(data.carId);
		}

		public void Record(float time)
		{
			if (!isSuspended)
			{
				if (m_networkRaceState != null)
				{
					data.RecordDriftPoints(time, m_networkRaceState.currentDriftPoints);
				}
				if (!BaseCar.IsNull(m_raceCar))
				{
					data.RecordRaceCar(time, m_raceCar);
				}
			}
		}

		public GhostData GetData()
		{
			FlushParent();
			return data;
		}

		private void FlushParent()
		{
			if (parent != null)
			{
				data.GetTimeInterval(out var begTime, out var endTime);
				if (begTime == endTime && begTime == 0f)
				{
					begTime = float.MaxValue;
				}
				data.WriteBefore(parent, begTime);
				parent = null;
			}
		}
	}

	private const int MaxRecorders = 5;

	private float m_currentTime;

	private Recorder[] m_recorders = new Recorder[0];

	private bool m_isUpdateSubscribed;

	private bool m_isRecordSourceLocked;

	public bool isRecording { get; private set; }

	public void SetRecordSource(RaceCar raceCar, NetworkRaceState networkRaceState = null)
	{
		m_recorders = new Recorder[1]
		{
			new Recorder(raceCar, networkRaceState)
		};
	}

	public void SetRecordSource(RaceCar[] raceCar)
	{
		m_recorders = new Recorder[raceCar.Length];
		for (int i = 0; i < raceCar.Length; i++)
		{
			m_recorders[i] = new Recorder(raceCar[i]);
		}
	}

	public void AddRecordSource(RaceCar raceCar, GhostData parent = null)
	{
		Recorder[] recorders = m_recorders;
		if ((recorders == null || recorders.Length < 5) && !m_isRecordSourceLocked)
		{
			int num = parent?.tempSlotId ?? raceCar.prefs.carSettings.GetActiveSlotId(raceCar.carId);
			int activeSlotId = raceCar.prefs.carSettings.GetActiveSlotId(raceCar.carId);
			raceCar.prefs.carSettings.SetActiveSlotId(raceCar.carId, num);
			Recorder recorder = new Recorder(raceCar);
			recorder.parent = parent;
			recorder.isSuspended = parent != null;
			recorder.data.tempSlotId = num;
			raceCar.prefs.carSettings.SetActiveSlotId(raceCar.carId, activeSlotId);
			m_recorders = m_recorders.Add(recorder);
		}
	}

	public void ClearRecorderSource()
	{
		m_recorders = null;
	}

	public GhostData GetRecordedData(int index = 0)
	{
		if (!m_recorders.IsEmpty() && index < m_recorders.Length)
		{
			return m_recorders[index].GetData();
		}
		return null;
	}

	public List<GhostData> GetRecorderDataList()
	{
		if (m_recorders == null)
		{
			return null;
		}
		List<GhostData> list = new List<GhostData>();
		Recorder[] recorders = m_recorders;
		foreach (Recorder recorder in recorders)
		{
			list.Add(recorder.GetData());
		}
		return list;
	}

	public int Count()
	{
		return m_recorders.Length;
	}

	public void RiseSuspended(bool isSuspend)
	{
		Recorder[] recorders = m_recorders;
		for (int i = 0; i < recorders.Length; i++)
		{
			recorders[i].isSuspended = isSuspend;
		}
	}

	public void RiseSuspended(int index, bool isSuspend)
	{
		if (index >= 0 && index < m_recorders.Length)
		{
			m_recorders[index].isSuspended = isSuspend;
		}
	}

	public void SetLockRecordSource(bool isLocked)
	{
		m_isRecordSourceLocked = isLocked;
	}

	public void Record(float zeroTime = 0f)
	{
		if (!isRecording)
		{
			m_currentTime = zeroTime;
			isRecording = true;
			SubscribeUpdate();
		}
	}

	public void RaceStart()
	{
		Recorder[] recorders = m_recorders;
		for (int i = 0; i < recorders.Length; i++)
		{
			recorders[i].data.greenLightTime = m_currentTime;
		}
	}

	public void Stop()
	{
		UnsubscribeUpdate();
		m_currentTime = 0f;
		isRecording = false;
	}

	public void SetPauseRecord(bool paused)
	{
		isRecording = !paused;
	}

	private void OnUpdate()
	{
		if (!isRecording)
		{
			return;
		}
		float deltaTime = Time.deltaTime;
		if (deltaTime != 0f)
		{
			Recorder[] recorders = m_recorders;
			for (int i = 0; i < recorders.Length; i++)
			{
				recorders[i].Record(m_currentTime);
			}
			m_currentTime += deltaTime;
		}
	}

	private void SubscribeUpdate()
	{
		if (!m_isUpdateSubscribed)
		{
			TimeController.onAfterFixedUpdate = (Action)Delegate.Combine(TimeController.onAfterFixedUpdate, new Action(OnUpdate));
			m_isUpdateSubscribed = true;
		}
	}

	private void UnsubscribeUpdate()
	{
		if (m_isUpdateSubscribed)
		{
			TimeController.onAfterFixedUpdate = (Action)Delegate.Remove(TimeController.onAfterFixedUpdate, new Action(OnUpdate));
			m_isUpdateSubscribed = false;
		}
	}
}
