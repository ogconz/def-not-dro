using System;
using Binding;
using Binding.Base;
using UnityEngine;

public class UIEliminationCountdownContext : BaseBindingBehaviourTarget
{
	private IntProperty m_count = new IntProperty(-1);

	private float m_timer;

	public event Action onFinishedEvent;

	public void Play()
	{
		m_count.value = 3;
		m_timer = m_count.value + 1;
	}

	public void Stop()
	{
		m_count.value = -1;
		m_timer = 0f;
	}

	public void OnFinished()
	{
		Stop();
		this.onFinishedEvent.Call();
		Hide();
	}

	protected override bool OnEscape()
	{
		OnFinished();
		return true;
	}

	private void Update()
	{
		if (m_timer >= 0f)
		{
			m_timer -= Time.deltaTime;
			m_count.value = Mathf.FloorToInt(m_timer);
			if (m_timer <= 0f)
			{
				OnFinished();
			}
		}
	}
}
