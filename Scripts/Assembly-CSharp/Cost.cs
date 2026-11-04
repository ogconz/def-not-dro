using CodeStage.AntiCheat.ObscuredTypes;

public struct Cost
{
	private ObscuredInt m_coins;

	private ObscuredInt m_premium;

	public int coins
	{
		get
		{
			return m_coins;
		}
		set
		{
			m_coins = value;
		}
	}

	public int cash
	{
		get
		{
			return m_premium;
		}
		set
		{
			m_premium = value;
		}
	}

	public Cost(int coins, int premium)
	{
		m_coins = coins;
		m_premium = premium;
	}
}
