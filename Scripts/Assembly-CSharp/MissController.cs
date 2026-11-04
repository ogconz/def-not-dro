using UnityEngine;

public class MissController : MonoBehaviour
{
	private ButtonMissClick m_lastClickBtn;

	public void Reset(ButtonMissClick btn)
	{
		if (m_lastClickBtn == btn)
		{
			m_lastClickBtn = null;
		}
	}

	public void SetDefaultButton(ButtonMissClick btn)
	{
		if (m_lastClickBtn == null)
		{
			m_lastClickBtn = btn;
		}
	}

	public void SetLastButton(ButtonMissClick btn)
	{
		m_lastClickBtn = btn;
	}

	public bool CheckMissClick(ButtonMissClick btn)
	{
		if (m_lastClickBtn == btn)
		{
			return true;
		}
		m_lastClickBtn = btn;
		return false;
	}
}
