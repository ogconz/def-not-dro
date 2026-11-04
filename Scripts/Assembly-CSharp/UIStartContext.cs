using Binding;
using Binding.Base;

public class UIStartContext : BaseBindingBehaviourTarget
{
	private IntProperty m_count = new IntProperty(-1);

	protected override void OnActivate()
	{
		base.OnActivate();
		m_count.value = -1;
	}

	public void Play(int seconds)
	{
		if (m_count.value != seconds && seconds <= 3)
		{
			m_count.value = seconds;
			SoundController.Play2DUISound((seconds == 0) ? SoundSample.MPSubraceGO : SoundSample.MPTimerTick, 0f);
			GameEvents.RaceStartCountDown(seconds);
		}
	}
}
