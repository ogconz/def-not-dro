using System.Diagnostics;

public class PrecisionTimer
{
	private readonly Stopwatch m_stopwatch = new Stopwatch();

	public long realTimeMs => GetCurrentTimeMs();

	public long unscaledTimeMs { get; private set; }

	public long fixedUnscaledTimeMs { get; private set; }

	public float unscaledDeltaTime { get; private set; }

	public long unscaledDeltaTimeMs { get; private set; }

	public float fixedUnscaledDeltaTime { get; private set; }

	public long fixedUnscaledDeltaTimeMs { get; private set; }

	public PrecisionTimer()
	{
		Reset();
	}

	public void Reset()
	{
		m_stopwatch.Reset();
		m_stopwatch.Start();
		unscaledTimeMs = GetCurrentTimeMs();
		fixedUnscaledTimeMs = GetCurrentTimeMs();
		unscaledDeltaTimeMs = 0L;
		unscaledDeltaTime = 0f;
		fixedUnscaledDeltaTimeMs = 0L;
		fixedUnscaledDeltaTime = 0f;
	}

	public void Update()
	{
		long currentTimeMs = GetCurrentTimeMs();
		unscaledDeltaTimeMs = currentTimeMs - unscaledTimeMs;
		unscaledDeltaTime = (float)unscaledDeltaTimeMs / 1000f;
		unscaledTimeMs = currentTimeMs;
	}

	public void FixedUpdate()
	{
		long currentTimeMs = GetCurrentTimeMs();
		fixedUnscaledDeltaTimeMs = currentTimeMs - fixedUnscaledTimeMs;
		fixedUnscaledDeltaTime = (float)fixedUnscaledDeltaTimeMs / 1000f;
		fixedUnscaledTimeMs = currentTimeMs;
	}

	private long GetCurrentTimeMs()
	{
		return m_stopwatch.ElapsedMilliseconds;
	}
}
