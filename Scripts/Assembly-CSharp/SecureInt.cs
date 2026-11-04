using UnityEngine;

public class SecureInt
{
	private int m_val;

	private int m_key;

	public int value
	{
		get
		{
			return m_val ^ m_key;
		}
		set
		{
			m_key = Random.Range(1, 9999999);
			m_val = value ^ m_key;
		}
	}

	public SecureInt()
		: this(0)
	{
	}

	public SecureInt(int val)
	{
		value = val;
	}
}
