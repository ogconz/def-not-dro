using System;
using ArrayExtension;
using DI;
using FMOD.Studio;
using FMODUnity;
using GameOverlay;
using PrefsModels;
using UnityEngine;

namespace JukeBox
{
	[Serializable]
	public class Controller
	{
		public Node[] Nodes;

		public const int MAX_CHANNELS = 2;

		public const float MUSIC_SUPRESSED_VOLUME = 0.25f;

		public const float MAX_MUSIC_SLOWDOWN = 0.6f;

		public const float MAX_MUSIC_SLOWDOWN_FACTOR = 2.5000002f;

		public const float MAX_MUSIC_SLOWDOWN_VOLUME = 0.1f;

		public const float FADE_TIME = 1f;

		public const float FADE_SPEED = 1f;

		private static ChannelNode m_ActiveNode = null;

		private static ChannelNode m_FadeInNode = null;

		private static ChannelNode m_FadeOutNode = null;

		private static FadeMode m_FadeMode = FadeMode.Inplace;

		public static GlobalModel m_settings;

		private static float m_SlowdownVolume = 1f;

		private static float m_SubmasterVolume = 1f;

		private static float m_SubmasterTarget = 1f;

		private static float m_SupressorEnd = 0f;

		private static float m_MasterPitch = 1f;

		private static EventInstance[] m_Channels;

		private static bool[] m_ChannelsLock;

		private static bool m_manualUpdate;

		private static MusicSignal m_ActiveSignal;

		public static Controller Instance { get; private set; }

		public static MusicSignal ActiveSignal
		{
			get
			{
				return m_ActiveSignal;
			}
			set
			{
				if (value != m_ActiveSignal)
				{
					m_FadeMode = FadeMode.Outplace;
					m_ActiveSignal = value;
					m_FadeInNode = GetNode(m_ActiveSignal, m_FadeOutNode);
					if (m_FadeInNode == null)
					{
						m_ActiveSignal = MusicSignal.Silence;
					}
				}
			}
		}

		public static ChannelNode GetActiveNode()
		{
			return m_ActiveNode;
		}

		public void Initiate(Transform root)
		{
			Instance = this;
			new GameObject("@MusicChannels").transform.SetParent(root);
			Overlay.OnReady = (Action)Delegate.Combine(Overlay.OnReady, new Action(OnOverlayReady));
			m_Channels = new EventInstance[2];
			m_ChannelsLock = new bool[m_Channels.GetLength()];
			m_ChannelsLock.MapMut((bool x) => false);
		}

		private void OnOverlayReady()
		{
			m_settings = DependencyInjector.Resolve<GamePrefs>().globals;
		}

		public static void ManualUpdate(bool enable)
		{
			m_manualUpdate = enable;
		}

		public static void Supress(float time, float targetVolume = 0.25f)
		{
			if (!(time < 0f))
			{
				m_SubmasterTarget = Mathf.Min(m_SubmasterTarget, targetVolume);
				m_SupressorEnd = Mathf.Max(m_SupressorEnd, Time.unscaledTime + time);
			}
		}

		public static void Update()
		{
			if (!m_manualUpdate && m_settings != null && !(Time.unscaledDeltaTime > 0.2f))
			{
				if (m_SupressorEnd != 0f && Time.unscaledTime > m_SupressorEnd)
				{
					m_SupressorEnd = 0f;
					m_SubmasterTarget = 1f;
				}
				float maxDelta = ((m_SubmasterTarget > m_SubmasterVolume) ? Time.unscaledDeltaTime : (5f * Time.unscaledDeltaTime));
				m_SubmasterVolume = Mathf.MoveTowards(m_SubmasterVolume, m_SubmasterTarget, maxDelta);
				m_MasterPitch = Mathf.MoveTowards(m_MasterPitch, Mathf.Lerp(0.6f, 1f, Time.timeScale), 2f * Time.unscaledDeltaTime);
				m_SlowdownVolume = Mathf.Lerp(0.1f, 1f, (m_MasterPitch - 0.6f) * 2.5000002f);
				UpdateChannels();
				UpdateActive();
				UpdateVolume();
				UpdateChannelsPitch();
			}
		}

		private static void UpdateChannelsPitch()
		{
			for (int i = 0; i < m_Channels.Length; i++)
			{
				if (m_ChannelsLock[i])
				{
					m_Channels[i].setPitch(m_MasterPitch);
				}
			}
		}

		private static void UpdateChannels()
		{
			if (m_FadeOutNode != null)
			{
				if (m_FadeOutNode.Volume > 0f)
				{
					return;
				}
				if (m_FadeOutNode.Channel != -1 && m_FadeOutNode.Info.Looped)
				{
					m_Channels[m_FadeOutNode.Channel].getTimelinePosition(out m_FadeOutNode.Info.LastPos);
				}
				m_FadeOutNode.ReleaseChannel();
				m_FadeOutNode = null;
			}
			if (m_FadeInNode == null)
			{
				return;
			}
			m_FadeOutNode = m_ActiveNode;
			m_ActiveNode = m_FadeInNode;
			m_ActiveNode.AcquireChannel();
			if (m_ActiveNode.Channel != -1)
			{
				if (m_ActiveNode.Info.Looped)
				{
					m_Channels[m_ActiveNode.Channel].setTimelinePosition(m_ActiveNode.Info.LastPos);
				}
				UpdateChannelsPitch();
				m_Channels[m_ActiveNode.Channel].start();
				m_Channels[m_ActiveNode.Channel].getDescription(out var description);
				description.getLength(out var length);
				m_ActiveNode.length = length;
			}
			m_FadeInNode = null;
		}

		private static void UpdateActive()
		{
			if (m_ActiveNode == null || m_ActiveNode.Info.Looped || m_ActiveNode.Channel == -1 || m_FadeInNode != null || m_FadeOutNode != null)
			{
				return;
			}
			if (m_ActiveSignal == MusicSignal.Silence)
			{
				m_FadeOutNode = m_ActiveNode;
				m_ActiveNode = null;
				return;
			}
			EventInstance eventInstance = m_Channels[m_ActiveNode.Channel];
			eventInstance.getTimelinePosition(out var position);
			if ((float)position >= (float)m_ActiveNode.length - 1000f)
			{
				m_FadeMode = FadeMode.Inplace;
				m_FadeInNode = GetNode(m_ActiveSignal, m_ActiveNode);
			}
		}

		private static void UpdateVolume()
		{
			float num = m_settings.musicVolume * Mathf.Min(m_SlowdownVolume, m_SubmasterVolume);
			bool flag = m_FadeMode == FadeMode.Inplace;
			if (m_FadeOutNode.IsValid())
			{
				if (m_FadeOutNode.Volume > 0f)
				{
					m_FadeOutNode.Volume -= Time.unscaledDeltaTime * 1f;
					if (m_FadeOutNode.Volume < 0f)
					{
						m_FadeOutNode.Volume = 0f;
						flag = true;
					}
					if (m_ActiveSignal == MusicSignal.Silence)
					{
						m_FadeOutNode.Volume = 0f;
					}
				}
				m_FadeOutNode.ChannelVolume = m_FadeOutNode.Volume * num;
			}
			else
			{
				flag = true;
			}
			if (!m_ActiveNode.IsValid())
			{
				return;
			}
			if (flag && m_ActiveNode.Volume < 1f)
			{
				m_ActiveNode.Volume += Time.unscaledDeltaTime * 1f;
				if (m_ActiveNode.Volume > 1f)
				{
					m_ActiveNode.Volume = 1f;
				}
			}
			if (m_ActiveSignal == MusicSignal.Silence)
			{
				m_ActiveNode.Volume = 0f;
			}
			m_ActiveNode.ChannelVolume = m_ActiveNode.Volume * num;
		}

		public static int GetChannel(string eventRef, bool looped)
		{
			int num = m_ChannelsLock.FindFirst(value: false);
			if (num != -1)
			{
				m_ChannelsLock[num] = true;
				EventInstance eventInstance = RuntimeManager.CreateInstance(RuntimeManager.PathToGUID(eventRef));
				eventInstance.set3DAttributes(default(Vector3).To3DAttributes());
				eventInstance.setVolume(0f);
				m_Channels[num] = eventInstance;
			}
			return num;
		}

		public static float GetChannelVolume(int channel)
		{
			if (channel == -1 || !m_Channels[channel].hasHandle())
			{
				return 0f;
			}
			m_Channels[channel].getVolume(out var _, out var finalvolume);
			return finalvolume;
		}

		public static void SetChannelVolume(int channel, float value)
		{
			if (channel != -1 && m_Channels[channel].hasHandle())
			{
				m_Channels[channel].setVolume(value);
			}
		}

		public static void ReleaseChannel(int channel)
		{
			if (channel != -1 && m_Channels[channel].hasHandle())
			{
				m_ChannelsLock[channel] = false;
				EventInstance eventInstance = m_Channels[channel];
				eventInstance.stop(STOP_MODE.IMMEDIATE);
				eventInstance.release();
			}
		}

		private static ChannelNode GetNode(MusicSignal signal, ChannelNode excludeNode = null)
		{
			Node excluder = excludeNode?.Info;
			Node node = Instance.Nodes.SelectRandom((Node node2) => node2 != excluder && node2.Signal == signal);
			if (node == null)
			{
				node = Instance.Nodes.Find((Node node2) => node2.Signal == signal);
			}
			if (node == null)
			{
				return null;
			}
			return new ChannelNode(node);
		}
	}
}
