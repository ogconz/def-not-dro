using System.Text;
using Binding.Base;
using GameInput;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[GameHotkeyBlocker(true)]
public class UIConsoleUGUIContext : BaseBindingBehaviourTarget
{
	[SerializeField]
	private TMP_InputField m_commandLine;

	[SerializeField]
	private Text m_textHistory;

	[SerializeField]
	private ScrollRect m_scrollRect;

	private StringBuilder m_histroyStringBuilder = new StringBuilder();

	private int m_lastHistoryGeneration = -1;

	private float m_curScrollSpeed;

	private float m_lastScrollTime;

	private string m_activeFilter = string.Empty;

	private static bool m_canShowDebugConsole = true;

	private void Awake()
	{
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		BringForwardByDepth();
		if (!m_canShowDebugConsole)
		{
			base.gameObject.SetActive(m_canShowDebugConsole);
		}
	}

	public static void Toggle(ContextOwner owner)
	{
		if (owner == null)
		{
			GameLog.Print(AlertLevel.Error, "UIConsoleContext", "Toggle(owner == null)");
		}
		else if (m_canShowDebugConsole)
		{
			UIConsoleUGUIContext uIConsoleUGUIContext = owner.Get<UIConsoleUGUIContext>();
			if ((bool)uIConsoleUGUIContext && uIConsoleUGUIContext.isActive)
			{
				uIConsoleUGUIContext.Hide();
				return;
			}
			UIConsoleUGUIContext uIConsoleUGUIContext2 = owner.Push<UIConsoleUGUIContext>();
			uIConsoleUGUIContext2.m_commandLine.text = string.Empty;
			uIConsoleUGUIContext2.m_scrollRect.verticalNormalizedPosition = 0f;
			uIConsoleUGUIContext2.SetFocusInput();
		}
	}

	[GameHotkey(ActionType.MenuLayerSeparator, false, false)]
	public void Empty()
	{
	}

	protected override bool OnEscape()
	{
		Hide();
		return true;
	}

	private void ResetCaret()
	{
		m_commandLine.MoveTextEnd(shift: false);
	}

	private void SetFocusInput()
	{
		m_commandLine.ActivateInputField();
		ResetCaret();
	}

	private void OnReceiveInputText(string text)
	{
		m_commandLine.text = text;
		ResetCaret();
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Tab))
		{
			m_commandLine.text = GameConsole.TabPressed(m_commandLine.text);
			ResetCaret();
		}
		if (Input.GetKeyDown(KeyCode.UpArrow))
		{
			m_commandLine.text = GameConsole.UpPressed();
			ResetCaret();
		}
		if (Input.GetKeyDown(KeyCode.DownArrow))
		{
			m_commandLine.text = GameConsole.DownPressed();
			ResetCaret();
		}
		if (Input.GetKey(KeyCode.PageUp))
		{
			ScrollLog(-1);
		}
		if (Input.GetKey(KeyCode.PageDown))
		{
			ScrollLog(1);
		}
		if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && !string.IsNullOrEmpty(m_commandLine.text))
		{
			OnSubmit(m_commandLine.text);
		}
		if (InputManager.GetLockedInputObject() != m_commandLine)
		{
			InputManager.LockInput(m_commandLine);
		}
		if (!m_commandLine.isFocused)
		{
			SetFocusInput();
		}
		if (m_commandLine.text.Length > 0 && m_commandLine.text[0] == '>')
		{
			string text = m_commandLine.text.Substring(1);
			if (text != m_activeFilter)
			{
				m_activeFilter = text;
				m_lastHistoryGeneration = 0;
			}
		}
		else if (m_activeFilter.Length > 0)
		{
			m_activeFilter = string.Empty;
			m_lastHistoryGeneration = 0;
		}
		if (m_lastHistoryGeneration == GameConsole.displayGeneration)
		{
			return;
		}
		m_lastHistoryGeneration = GameConsole.displayGeneration;
		m_histroyStringBuilder.Clear();
		for (int i = 0; i < GameConsole.display.Count; i++)
		{
			string text2 = GameConsole.display[i];
			int num = GameConsole.displayLineCounter[i];
			if (num > 0)
			{
				text2 += $"({num})";
			}
			if (m_activeFilter.Length == 0)
			{
				m_histroyStringBuilder.AppendLine(text2);
			}
			else if (text2.Contains(m_activeFilter))
			{
				m_histroyStringBuilder.AppendLine(text2.Replace(m_activeFilter, "<b>" + m_activeFilter + "</b>"));
			}
		}
		if (m_histroyStringBuilder.Length > 16250)
		{
			m_histroyStringBuilder.Remove(0, m_histroyStringBuilder.Length - 16250);
		}
		if (m_histroyStringBuilder.Length > 0)
		{
			m_histroyStringBuilder.Remove(m_histroyStringBuilder.Length - 1, 1);
		}
		m_textHistory.text = m_histroyStringBuilder.ToString();
		m_scrollRect.verticalNormalizedPosition = 0f;
	}

	private void ScrollLog(int offset)
	{
		int num = GameConsole.display.Count - 10;
		if (num <= 0)
		{
			return;
		}
		float num2 = 0.7f / (float)num;
		float num3 = num2 * 10f;
		float unscaledTime = Time.unscaledTime;
		if (unscaledTime - m_lastScrollTime > 0.5f)
		{
			m_curScrollSpeed = num2;
		}
		else
		{
			m_curScrollSpeed *= 1.1f;
			if (m_curScrollSpeed > num3)
			{
				m_curScrollSpeed = num3;
			}
		}
		m_lastScrollTime = unscaledTime;
		m_scrollRect.verticalNormalizedPosition -= (float)offset * m_curScrollSpeed;
	}

	private void OnSubmit(string command)
	{
		GameConsole.UserCommand(command.Trim());
		m_commandLine.text = string.Empty;
	}

	public void OnDisable()
	{
		if (InputManager.GetLockedInputObject() == m_commandLine)
		{
			InputManager.LockInput(null);
		}
	}
}
