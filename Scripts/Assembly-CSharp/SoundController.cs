using System;
using System.Collections.Generic;
using System.Reflection;
using DI;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using GameOverlay;
using JukeBox;
using UnityEngine;

[Serializable]
public class SoundController : MonoBehaviour
{
	private enum FMOD_PS4_PORT_TYPES
	{
		FMOD_PS4_PORT_TYPE_MUSIC = 0,
		FMOD_PS4_PORT_TYPE_VOICE = 1,
		FMOD_PS4_PORT_TYPE_PERSONAL = 2,
		FMOD_PS4_PORT_TYPE_CONTROLLER = 3,
		FMOD_PS4_PORT_TYPE_COPYRIGHT_MUSIC = 4,
		FMOD_PS4_PORT_TYPE_SOCIAL = 5,
		FMOD_PS4_PORT_TYPE_MAX = 6
	}

	protected const string voidSoundEvent = "event:/GameEvents/Void";

	public static SoundController instance;

	public Controller musicController;

	public string ps4PadMixerGUID;

	[EventRef]
	public string ColorCustomize;

	[EventRef]
	public string DiskCustomize;

	[EventRef]
	public string BodyKitCustomize;

	[EventRef]
	public string DefaultBtn;

	[EventRef]
	public string HoverClip;

	[EventRef]
	public string SwitchPages;

	[EventRef]
	public string LevelUp;

	[EventRef]
	public string MPSubraceGO;

	[EventRef]
	public string MPSubraceReady;

	[EventRef]
	public string MPSubroomShow;

	[EventRef]
	public string MPTimerTick;

	[EventRef]
	public string ProSetup;

	[EventRef]
	public string VinylSelect;

	[EventRef]
	public string VinylCreate;

	[EventRef]
	public string VinylCancel;

	[EventRef]
	public string Intro;

	[EventRef]
	public string Promotion;

	public float dublicateVolumeFactor = 0.6f;

	public LoopedTripleSound ProSetupSlide;

	public LoopedTripleSound RaceStartSound;

	private static BetterList<LoopedTripleSound> m_ActiveFXList = new BetterList<LoopedTripleSound>();

	private static Dictionary<FXSample, LoopedTripleSound> m_fxHolder;

	private static Dictionary<SoundSample, string> m_ClipHolder;

	private static Dictionary<SoundSample, string> m_padEventCache;

	private const string FX_BUS = "bus:/FX";

	private const string UI_BUS = "bus:/UI";

	private const string AMBIENCE_BUS = "bus:/FX/Ambience";

	private bool m_lastPauseCached;

	protected static GamePrefs m_prefs;

	public static bool isPadSoundSupported { get; private set; }

	public bool fxAudioPause
	{
		get
		{
			RuntimeManager.GetBus("bus:/FX").getPaused(out var paused);
			return paused;
		}
		set
		{
			Bus bus = RuntimeManager.GetBus("bus:/FX");
			bus.getPaused(out var paused);
			if (paused == value && m_lastPauseCached == value)
			{
				return;
			}
			bus.setPaused(value);
			m_lastPauseCached = value;
			if (value)
			{
				return;
			}
			bus.getChannelGroup(out var group);
			group.getNumDSPs(out var numdsps);
			for (int i = 0; i < numdsps; i++)
			{
				if (group.getDSP(i, out var dsp) == RESULT.OK)
				{
					dsp.reset();
				}
			}
		}
	}

	public void Awake()
	{
		instance = this;
		InitiateSampleDB();
		InitiateFXDB();
		musicController.Initiate(base.transform);
		Overlay.OnReady = (Action)Delegate.Combine(Overlay.OnReady, new Action(OnOverlayReady));
	}

	private void OnOverlayReady()
	{
		InitPadPort();
	}

	private void InitPadPort()
	{
	}

	private void InitiateSampleDB()
	{
		m_ClipHolder = new Dictionary<SoundSample, string>();
		m_padEventCache = new Dictionary<SoundSample, string>();
		Dictionary<string, SoundSample> dictionary = new Dictionary<string, SoundSample>();
		foreach (SoundSample value in Enum.GetValues(typeof(SoundSample)))
		{
			dictionary.Add(value.ToString(), value);
		}
		FieldInfo[] cachedFields = GetType().GetCachedFields(BindingFlags.Instance | BindingFlags.Public);
		foreach (FieldInfo fieldInfo in cachedFields)
		{
			string key = fieldInfo.Name;
			if (dictionary.ContainsKey(key))
			{
				m_ClipHolder.Add(dictionary[key], fieldInfo.GetValue(instance) as string);
			}
		}
	}

	private void InitiateFXDB()
	{
		m_fxHolder = new Dictionary<FXSample, LoopedTripleSound>();
		Dictionary<string, FXSample> dictionary = new Dictionary<string, FXSample>();
		foreach (FXSample value in Enum.GetValues(typeof(FXSample)))
		{
			dictionary.Add(value.ToString(), value);
		}
		FieldInfo[] cachedFields = GetType().GetCachedFields(BindingFlags.Instance | BindingFlags.Public);
		foreach (FieldInfo fieldInfo in cachedFields)
		{
			if (dictionary.ContainsKey(fieldInfo.Name))
			{
				m_fxHolder.Add(dictionary[fieldInfo.Name], fieldInfo.GetValue(instance) as LoopedTripleSound);
			}
		}
	}

	public void CleanUp()
	{
		foreach (LoopedTripleSound activeFX in m_ActiveFXList)
		{
			activeFX.CleanUp();
		}
		m_ActiveFXList.Clear();
	}

	public void Update()
	{
		for (int num = m_ActiveFXList.size - 1; num >= 0; num--)
		{
			LoopedTripleSound loopedTripleSound = m_ActiveFXList[num];
			if (!loopedTripleSound.IsActive())
			{
				loopedTripleSound.CleanUp();
				m_ActiveFXList.RemoveAt(num);
			}
		}
		Controller.Update();
	}

	public static string ResolveEventLink(string eventLink, string defaultLink = "event:/GameEvents/Void")
	{
		string text = eventLink;
		FMOD.Studio.System studioSystem = RuntimeManager.StudioSystem;
		if (studioSystem.lookupID(text, out var id) != RESULT.OK)
		{
			text = defaultLink;
			if (studioSystem.lookupID(text, out id) != RESULT.OK)
			{
				text = "event:/GameEvents/Void";
				if (studioSystem.lookupID(text, out id) != RESULT.OK)
				{
					text = "";
				}
			}
		}
		return text;
	}

	private static EventInstance PlayOneShot(string eventLink, Vector3 position = default(Vector3))
	{
		EventInstance result = RuntimeManager.CreateInstance(RuntimeManager.PathToGUID(eventLink));
		result.set3DAttributes(position.To3DAttributes());
		result.start();
		return result;
	}

	public static void PlayFX(FXSample sample)
	{
		m_fxHolder.TryGetValue(sample, out var value);
		if (value != null)
		{
			value.Initiate();
			value.Play();
			m_ActiveFXList.Add(value);
		}
	}

	public static void StopFX(FXSample sample)
	{
		m_fxHolder.TryGetValue(sample, out var value);
		if (m_ActiveFXList.Contains(value))
		{
			value.Stop();
		}
	}

	public static EventInstance Play2DSound(SoundSample sample, float volume = 1f, float pitch = 1f, bool disableRelease = false)
	{
		return Play2DSound(ResolveEventLink(m_ClipHolder[sample]), volume, pitch, disableRelease);
	}

	public static EventInstance Play2DSound(string clip, float volume = 1f, float pitch = 1f, bool disableRelease = false)
	{
		EventInstance result = PlayOneShot(clip);
		if (!disableRelease)
		{
			result.release();
		}
		return result;
	}

	public static void Play2DUISound(SoundSample sample, float pitchRnd = 0.1f, float volume = 1f)
	{
		string eventLink = ResolveEventLink(m_ClipHolder[sample]);
		EventInstance eventInstance = PlayOneShot(eventLink);
		eventInstance.setVolume(volume);
		float pitch = UnityEngine.Random.Range(1f - pitchRnd, 1f + pitchRnd);
		eventInstance.setPitch(pitch);
		eventInstance.release();
		if (isPadSoundSupported)
		{
			TryPlayToPad(sample, eventLink, pitch, volume);
		}
	}

	public static void Play2DUISoundIfFits(SoundSample sample, float timeLimitPoint)
	{
		EventInstance eventInstance = PlayOneShot(ResolveEventLink(m_ClipHolder[sample]));
		eventInstance.getDescription(out var description);
		description.getLength(out var length);
		if (Time.time > timeLimitPoint - (float)length * 0.001f)
		{
			eventInstance.stop(STOP_MODE.IMMEDIATE);
		}
		eventInstance.release();
	}

	private static void TryPlayToPad(SoundSample sample, string eventLink, float pitch, float volume)
	{
		if (m_prefs == null)
		{
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
		}
		if (!(m_prefs == null))
		{
			_ = m_prefs.globals.padSound;
		}
	}

	public static void PlayMusic(MusicSignal signal)
	{
		Controller.ActiveSignal = signal;
	}

	public static void SetSoundFXVolume(float volume)
	{
		RuntimeManager.GetBus("bus:/FX").setVolume(volume);
		RuntimeManager.GetBus("bus:/UI").setVolume(volume);
	}

	public static void GetSoundAmbienceVolume(out float volume)
	{
		RuntimeManager.GetBus("bus:/FX/Ambience").getVolume(out volume);
	}

	public static void SetSoundAmbienceVolume(float volume)
	{
		RuntimeManager.GetBus("bus:/FX/Ambience").setVolume(volume);
	}
}
