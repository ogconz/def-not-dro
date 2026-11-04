using System;
using UnityEngine;

public class PhaseTimeController
{
	public bool IsReversed;

	public PhaseControllerWorkMode workMode;

	public PhaseControllerTimerMode timerMode;

	private float m_Phase;

	private float m_LastRequested;

	private float m_StartTime;

	private float m_TimePassed;

	private float m_Length;

	private bool m_Paused = true;

	private bool m_Active;

	private bool m_Finished;

	public float Length
	{
		get
		{
			return m_Length;
		}
		set
		{
			m_Length = Mathf.Max(0.01f, value);
		}
	}

	public bool IsPaused
	{
		get
		{
			return m_Paused;
		}
		set
		{
			if (value)
			{
				m_TimePassed = GetTime() - m_StartTime;
				m_Paused = true;
				m_Active = false;
			}
			else
			{
				m_Paused = false;
				m_StartTime = GetTime() - m_TimePassed;
			}
		}
	}

	public bool IsActive => m_Active;

	public bool IsFinished
	{
		get
		{
			return m_Finished;
		}
		set
		{
			if (value)
			{
				m_Paused = false;
				m_Active = false;
				m_Finished = true;
				m_Phase = 1f;
			}
		}
	}

	public float GetTime()
	{
		return timerMode switch
		{
			PhaseControllerTimerMode.RealTime => Time.realtimeSinceStartup, 
			PhaseControllerTimerMode.UnscaledTime => Time.unscaledTime, 
			_ => Time.time, 
		};
	}

	public PhaseTimeController(PhaseControllerTimerMode timerMode, PhaseControllerWorkMode workMode, float length, bool suspended)
	{
		this.workMode = workMode;
		this.timerMode = timerMode;
		m_Paused = suspended;
		m_Active = !suspended;
		m_Length = Mathf.Max(0.01f, length);
		if (m_Active)
		{
			m_StartTime = GetTime();
		}
	}

	public void Play()
	{
		m_Active = true;
		m_Finished = false;
		if (m_Paused)
		{
			m_StartTime = GetTime() - m_TimePassed;
		}
		else
		{
			m_StartTime = GetTime();
			m_TimePassed = 0f;
		}
		m_Paused = false;
	}

	public void Reset()
	{
		m_Finished = false;
		m_Active = false;
		m_Paused = false;
		m_Phase = 0f;
		m_LastRequested = 0f;
	}

	public float GetPhase()
	{
		float time = GetTime();
		if (m_Active && time != m_LastRequested)
		{
			m_Phase = (time - m_StartTime) / m_Length;
			if (m_Phase > 1f)
			{
				switch (workMode)
				{
				case PhaseControllerWorkMode.Once:
					m_Active = false;
					m_Finished = true;
					m_Phase = 1f;
					break;
				case PhaseControllerWorkMode.OnceBack:
					if (m_Phase > 2f)
					{
						m_Active = false;
						m_Finished = true;
						m_Phase = 0f;
					}
					else
					{
						m_Phase = 2f - m_Phase;
					}
					break;
				case PhaseControllerWorkMode.Looped:
					m_Phase -= (int)m_Phase;
					break;
				case PhaseControllerWorkMode.LoopedBack:
					if (m_Phase > 2f)
					{
						m_Phase -= (int)m_Phase;
						m_StartTime = time - m_Phase * m_Length;
					}
					else
					{
						m_Phase = 2f - m_Phase;
					}
					break;
				}
			}
		}
		m_LastRequested = time;
		if (IsReversed)
		{
			return 1f - m_Phase;
		}
		return m_Phase;
	}

	public int getIValueFromRange(int left, int right)
	{
		return (int)(GetPhase() * (float)(right - left) + (float)left);
	}

	public float getFValueFromRange(float left, float right)
	{
		return GetPhase() * (right - left) + left;
	}

	public float getSinValue(float factor)
	{
		return factor * Mathf.Sin(GetPhase() * MathF.PI / 2f);
	}

	public int getLaxitySlideIValue(int factor)
	{
		if (IsReversed)
		{
			float num = 1f - GetPhase();
			return (int)((float)factor - (float)factor * num * num * (3f - 2f * num));
		}
		float phase = GetPhase();
		return (int)((float)factor * phase * phase * (3f - 2f * phase));
	}

	public float getLaxitySlideFValue(float factor)
	{
		if (IsReversed)
		{
			float num = 1f - GetPhase();
			return factor - factor * num * num * (3f - 2f * num);
		}
		float phase = GetPhase();
		return factor * phase * phase * (3f - 2f * phase);
	}

	public int getVerySmoothSlideIValue(float factor)
	{
		if (IsReversed)
		{
			float num = 1f - GetPhase();
			return (int)(factor - factor * num * num * num * (num * (6f * num - 15f) + 10f));
		}
		float phase = GetPhase();
		return (int)(factor * phase * phase * phase * (phase * (6f * phase - 15f) + 10f));
	}

	public float getVerySmoothSlideFValue(float factor)
	{
		if (IsReversed)
		{
			float num = 1f - GetPhase();
			return factor - factor * num * num * num * (num * (6f * num - 15f) + 10f);
		}
		float phase = GetPhase();
		return factor * phase * phase * phase * (phase * (6f * phase - 15f) + 10f);
	}
}
