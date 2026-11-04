using System.Diagnostics;

public class ApproxTimeKeeper
{
	private readonly Stopwatch m_timer = new Stopwatch();

	private long m_lastExecutionTime;

	public long intervalMs { get; set; }

	public bool isEnabled { get; set; }

	public bool ShouldExecute(long epsilonMs)
	{
		if (isEnabled)
		{
			return m_timer.ElapsedMilliseconds - m_lastExecutionTime > intervalMs - epsilonMs;
		}
		return false;
	}

	public ApproxTimeKeeper(long intervalMs)
	{
		isEnabled = true;
		this.intervalMs = intervalMs;
		m_timer.Start();
	}

	public void Reset()
	{
		m_lastExecutionTime = m_timer.ElapsedMilliseconds;
	}

	public void ResetToNow()
	{
		Reset();
		m_lastExecutionTime = m_timer.ElapsedMilliseconds - intervalMs;
	}
}
