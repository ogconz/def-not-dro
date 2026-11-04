using GameInput;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonMissClick : MonoBehaviour
{
	public UnityEvent missClick;

	private Button m_btnSource;

	private MissController m_controller;

	private ActionType m_actionLeft = ActionType.NavLeft;

	private ActionType m_actionRight = ActionType.NavRight;

	private void OnDisable()
	{
		if ((bool)m_controller)
		{
			m_controller.Reset(this);
		}
	}

	private void Start()
	{
		m_controller = GetComponentInParent<MissController>();
		m_btnSource = GetComponent<Button>();
		SetDefaultButton();
		m_btnSource.onClick.AddListener(OnMissClickCheck);
	}

	private void Update()
	{
		InputManager instance = InputManager.instance;
		if (!(m_controller == null) && (instance.IsActionPressed(m_actionLeft) || instance.IsActionHold(m_actionLeft) || instance.IsActionPressed(m_actionRight) || instance.IsActionHold(m_actionRight)))
		{
			m_controller.CheckMissClick(this);
		}
	}

	private void OnDestroy()
	{
		if ((bool)m_btnSource)
		{
			m_btnSource.onClick.RemoveAllListeners();
		}
	}

	private void OnEnable()
	{
		SetDefaultButton();
	}

	private void SetDefaultButton()
	{
		if ((bool)m_controller)
		{
			m_controller.SetDefaultButton(this);
		}
	}

	private void OnMissClickCheck()
	{
		if (!(m_controller == null) && m_controller.CheckMissClick(this))
		{
			missClick?.Invoke();
		}
	}
}
