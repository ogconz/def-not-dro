using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonDoubleClick : MonoBehaviour
{
	[SerializeField]
	[Range(0.2f, 1f)]
	private float m_doubleClickTime = 0.5f;

	[SerializeField]
	public UnityEvent m_doubleClick;

	private Button m_button;

	private bool m_isClicked;

	private float m_clickedAt;

	private float m_prevClickedAt;

	private void Start()
	{
		m_button = GetComponent<Button>();
		m_button.onClick.AddListener(OnDoubleClickCheck);
	}

	private void OnDoubleClickCheck()
	{
		m_prevClickedAt = m_clickedAt;
		m_clickedAt = Time.unscaledTime;
		if (Time.unscaledTime - m_prevClickedAt <= m_doubleClickTime && m_isClicked)
		{
			m_isClicked = false;
			m_doubleClick?.Invoke();
		}
		else
		{
			m_isClicked = true;
		}
	}
}
