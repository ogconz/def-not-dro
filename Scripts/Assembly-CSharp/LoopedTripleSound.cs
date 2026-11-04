using System;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

[Serializable]
public class LoopedTripleSound
{
	[EventRef]
	public string EventLink;

	private EventInstance m_eventInstance;

	private bool m_Initiated;

	public bool IsActive()
	{
		if (!m_eventInstance.hasHandle())
		{
			return false;
		}
		m_eventInstance.getPlaybackState(out var state);
		if (state != PLAYBACK_STATE.STARTING)
		{
			return state == PLAYBACK_STATE.PLAYING;
		}
		return true;
	}

	public void Initiate()
	{
		if (!m_Initiated)
		{
			string path = SoundController.ResolveEventLink(EventLink);
			m_eventInstance = RuntimeManager.CreateInstance(RuntimeManager.PathToGUID(path));
			m_eventInstance.set3DAttributes(default(Vector3).To3DAttributes());
			m_Initiated = true;
		}
	}

	public void CleanUp()
	{
		m_Initiated = false;
		if (m_eventInstance.hasHandle())
		{
			m_eventInstance.stop(STOP_MODE.IMMEDIATE);
			m_eventInstance.release();
		}
	}

	public void Play()
	{
		if (m_Initiated)
		{
			m_eventInstance.start();
		}
	}

	public void Stop()
	{
		if (m_Initiated)
		{
			m_eventInstance.stop(STOP_MODE.ALLOWFADEOUT);
		}
	}
}
