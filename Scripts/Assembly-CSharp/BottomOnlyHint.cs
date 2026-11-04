using TMPro;
using UnityEngine;

public class BottomOnlyHint : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI m_hintLabel;

	private void Awake()
	{
		if (m_hintLabel == null)
		{
			Transform transform = base.transform.FindChildDeph("Hint");
			if (transform != null)
			{
				m_hintLabel = transform.GetComponent<TextMeshProUGUI>();
			}
		}
	}

	public void SetHintText(string hint)
	{
		if (m_hintLabel != null)
		{
			m_hintLabel.text = hint;
		}
	}
}
