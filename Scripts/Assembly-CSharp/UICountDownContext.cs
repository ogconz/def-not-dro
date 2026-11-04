using System;
using System.Collections;
using Binding;
using Binding.Base;
using UnityEngine;
using Utils;

public class UICountDownContext : BaseBindingBehaviourTarget
{
	private const int COUNT_MAX = 4;

	private IntProperty m_count = new IntProperty(-1);

	private float m_delayTime = 1f;

	private float m_crossfadeTime = 0.5f;

	private Task m_task;

	private bool m_started;

	private bool m_playing;

	private float m_timer;

	public event Action onFinishedEvent;

	protected override void OnActivate()
	{
		base.OnActivate();
		if (!m_started)
		{
			m_count.value = 0;
			m_count.value = -1;
			m_timer = 0f;
			m_playing = false;
			m_started = false;
		}
	}

	public float GetCountdownTime()
	{
		return 3f * (m_delayTime + m_crossfadeTime);
	}

	public void DoStart()
	{
		if (m_task != null)
		{
			m_task.Stop();
			m_task = null;
		}
		m_started = true;
		m_playing = true;
		m_timer = 0f;
	}

	public void DoStart(RaceCar car, StartLine startLine)
	{
		if (m_task != null)
		{
			m_task.Stop();
			m_task = null;
		}
		m_started = false;
		m_playing = false;
		m_timer = 0f;
		m_task = new Task(WorkerTaskFunction2(car, startLine));
	}

	public void Pause()
	{
		if (m_task != null && !m_task.paused)
		{
			m_task.Pause();
		}
		m_playing = false;
	}

	public void Unpause()
	{
		if (m_task != null && m_task.paused)
		{
			m_task.Unpause();
		}
		m_playing = true;
	}

	public void Stop()
	{
		if (m_task != null)
		{
			m_task.Stop();
		}
		m_timer = 0f;
		m_playing = false;
		m_started = false;
	}

	private void FixedUpdate()
	{
		if (m_started && m_playing)
		{
			m_timer += Time.fixedDeltaTime;
			float num = m_delayTime + m_crossfadeTime;
			int num2 = Mathf.FloorToInt(m_timer / num);
			if (num2 != m_count.value)
			{
				PlayState(num2);
			}
			if (m_timer >= GetCountdownTime())
			{
				this.onFinishedEvent.Call();
				Stop();
				Invoke("Hide", m_delayTime);
			}
		}
	}

	private void PlayState(int state)
	{
		int num = 3;
		m_count.value = Mathf.Clamp(state, 0, num);
		SoundController.Play2DUISound((state == num) ? SoundSample.MPSubraceGO : SoundSample.MPTimerTick, 0f);
	}

	private IEnumerator WorkerTaskFunction2(RaceCar car, StartLine startLine)
	{
		BaseState mainCurrent = States.mainCurrent;
		if (mainCurrent is BaseRaceState state)
		{
			while (!state.startPlayPressed)
			{
				yield return null;
			}
		}
		int stateMax = 3;
		float delay = m_delayTime + m_crossfadeTime;
		float timeTotal = (float)stateMax * delay;
		m_count.value = -1;
		int i = 0;
		while (i < 4)
		{
			if (!car || !car.positionOnSpline)
			{
				yield break;
			}
			bool isLast = i == stateMax;
			float distanceBetweenPositions = car.positionOnSpline.GetDistanceBetweenPositions(startLine.distance, car.positionOnSpline.distanceAlongCenterLine + car.lengthFront);
			float num = car.carX.speedMPS * timeTotal + car.carX.longAccel * timeTotal * timeTotal * 0.5f;
			while (distanceBetweenPositions > num)
			{
				yield return null;
				if (!car || !car.positionOnSpline)
				{
					yield break;
				}
				distanceBetweenPositions = car.positionOnSpline.GetDistanceBetweenPositions(startLine.distance, car.positionOnSpline.distanceAlongCenterLine + car.lengthFront);
				num = car.carX.speedMPS * timeTotal + car.carX.longAccel * timeTotal * timeTotal * 0.5f;
			}
			timeTotal -= delay;
			PlayState(i);
			GameEvents.RaceStartCountDown(stateMax - i);
			if (isLast)
			{
				this.onFinishedEvent.Call();
			}
			yield return new WaitForSeconds(m_delayTime);
			int num2 = i + 1;
			i = num2;
		}
		Hide();
	}

	protected override bool OnEscape()
	{
		Hide();
		return true;
	}
}
