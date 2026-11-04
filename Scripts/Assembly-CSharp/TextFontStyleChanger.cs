using UnityEngine;
using UnityEngine.UI;

public class TextFontStyleChanger : MonoBehaviour
{
	private Text m_label;

	private void Awake()
	{
		m_label = GetComponent<Text>();
	}

	public void SetFontStyle(int style)
	{
		if (m_label != null)
		{
			m_label.fontStyle = (FontStyle)style;
		}
	}
}
