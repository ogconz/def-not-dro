using System.Collections.ObjectModel;
using CarX;

public class GroundDetector
{
	private BaseCar m_baseCar;

	private bool m_onGround = true;

	private bool m_offRoad;

	private int m_wheelsCountOnTrack;

	private int m_wheelsCountOnGround;

	private readonly int m_flippedWheelsCount = 2;

	private readonly float m_flippedPeriod = 1f;

	private readonly float m_flippedSpeed = 0.5f;

	private PeriodTimer m_flippedPeriodTimer;

	private readonly float m_flyBeforeFlippedPeriod = 4f;

	private bool m_flipCandidate;

	private bool m_flipped;

	private ReadOnlyCollection<CARXSurfaceTemplate> m_surfaceTempates;

	public bool onGround => m_onGround;

	public bool offRoad => m_offRoad;

	public int WheelsCountOnTrack => m_wheelsCountOnTrack;

	public int wheelsCountOnGround => m_wheelsCountOnGround;

	public bool flipped => m_flipped;

	public GroundDetector(BaseCar baseCar)
	{
		if (baseCar == null)
		{
			GameLog.Print(AlertLevel.Error, "Debug", "GroundDetector::GroundDetector(): baseCar is null.");
		}
		m_surfaceTempates = CARXSurfaceManager.GetAllTemplates();
		m_baseCar = baseCar;
		m_flippedPeriodTimer = new PeriodTimer(m_flippedPeriod, FlippedCallback);
	}

	public void FixedUpdate(float fixedDeltaTime)
	{
		m_onGround = false;
		m_offRoad = true;
		m_wheelsCountOnTrack = 0;
		m_wheelsCountOnGround = 0;
		int num = 0;
		if (BaseCar.IsNull(m_baseCar))
		{
			return;
		}
		WheelIndex[] activeWheels = m_baseCar.activeWheels;
		for (int i = 0; i < activeWheels.Length; i++)
		{
			Wheel wheel = m_baseCar.carX.GetWheel(activeWheels[i]);
			if (wheel.HasPatch())
			{
				m_onGround = true;
				m_wheelsCountOnGround++;
				if (wheel.GetUserData(out var userData) && userData >= 0)
				{
					CARXSurfaceTemplate surfaceTemplate = GetSurfaceTemplate(userData);
					if (surfaceTemplate != null && surfaceTemplate.isTrack)
					{
						m_wheelsCountOnTrack++;
					}
				}
			}
			else
			{
				num++;
			}
		}
		if (m_wheelsCountOnTrack != 0 || !m_onGround)
		{
			m_offRoad = false;
		}
		CheckFlip(fixedDeltaTime, num);
	}

	private CARXSurfaceTemplate GetSurfaceTemplate(int index)
	{
		if (index >= 0 && index < m_surfaceTempates.Count)
		{
			return m_surfaceTempates[index];
		}
		return null;
	}

	private void CheckFlip(float deltaTime, int wheelsWithoutPatch)
	{
		if (wheelsWithoutPatch < m_flippedWheelsCount)
		{
			m_flipped = false;
			m_flipCandidate = false;
		}
		else
		{
			if (m_flipped)
			{
				return;
			}
			if (m_flipCandidate)
			{
				if (!m_onGround)
				{
					m_flippedPeriodTimer.Update(deltaTime);
				}
				else if (m_baseCar.carX.speedMPS > m_flippedSpeed)
				{
					m_flipCandidate = false;
				}
				else
				{
					m_flippedPeriodTimer.Update(deltaTime);
				}
			}
			else if (!m_onGround)
			{
				m_flipCandidate = true;
				m_flippedPeriodTimer.Reset(m_flyBeforeFlippedPeriod);
			}
			else if (m_baseCar.carX.speedMPS <= m_flippedSpeed)
			{
				m_flipCandidate = true;
				m_flippedPeriodTimer.Reset();
			}
		}
	}

	private void FlippedCallback()
	{
		m_flipped = true;
	}
}
