using System.Collections.Generic;
using UnityEngine;

public static class ScreenDebug
{
	private class ScreenConsole : MonoBehaviour
	{
		private static string m_log = "";

		private static GUIStyle m_upperCenterStyle;

		private static GUIStyle m_lowerLeftStyle;

		private const int _MAX_LINES = 4;

		private const int _BUTTON_AND_LABEL_COUNTER = 5;

		private static readonly float m_fontSize = 7f / 180f * (float)Screen.height;

		private static readonly float m_heightFactor = 0.3f;

		private static readonly float m_widthFactor = 0.5f;

		private static readonly float m_bottomPanelHeight = (float)Screen.height * 0.08f;

		private static readonly float m_buttonWidth = (float)Screen.width * m_widthFactor / 5f;

		private static readonly float m_logWPos = ((float)Screen.width - (float)Screen.width * m_widthFactor) / 2f;

		private static readonly float m_logHPos = (float)Screen.height - (float)Screen.height * m_heightFactor;

		private static bool m_isScroll = true;

		private static int m_index;

		private static int m_prevSize;

		private static bool m_init;

		private const string _WARNING_NOT_TEST_BUILD = "Log displayed only in the editor. Set TEST Build for displayed log on mobile devices.";

		private void Awake()
		{
			Object.DontDestroyOnLoad(this);
		}

		public void HandleChangeLogList(int countLines)
		{
			if (m_prevSize == 1000)
			{
				if (!m_isScroll)
				{
					m_index -= countLines;
				}
			}
			else if (m_isScroll)
			{
				m_index += countLines;
			}
			m_prevSize = m_logList.Count;
		}

		private void OnGUI()
		{
			if (!m_init)
			{
				GUIStyle style = GUI.skin.GetStyle("Label");
				m_upperCenterStyle = new GUIStyle(style)
				{
					alignment = TextAnchor.UpperCenter
				};
				m_lowerLeftStyle = new GUIStyle(style)
				{
					alignment = TextAnchor.LowerLeft
				};
				m_init = true;
			}
			int max = Mathf.Max(m_logList.Count - 4, 0);
			m_index = Mathf.Clamp(m_index, 0, max);
			int max2 = Mathf.Max(m_logList.Count - 1, 0);
			int num = Mathf.Clamp(m_index + 4 - 1, 0, max2);
			m_log = string.Empty;
			try
			{
				for (int i = m_index; i <= num; i++)
				{
					string text = m_logList[i];
					m_log += text;
				}
			}
			catch
			{
			}
			Rect position = new Rect(m_logWPos, m_logHPos, (float)Screen.width * m_widthFactor, (float)Screen.height * m_heightFactor - m_bottomPanelHeight);
			string[] obj2 = new string[5] { "<color=blue><size=", null, null, null, null };
			float fontSize = m_fontSize;
			obj2[1] = fontSize.ToString();
			obj2[2] = ">";
			obj2[3] = m_log;
			obj2[4] = "</size></color>";
			GUI.Label(position, string.Concat(obj2), m_lowerLeftStyle);
			GUI.VerticalScrollbar(new Rect(m_logWPos - (float)Screen.width * 0.02f, (float)Screen.height - m_bottomPanelHeight, (float)Screen.width * 0.02f, m_bottomPanelHeight), m_index + 2, Mathf.Min(4, m_logList.Count), 0f, m_logList.Count);
			if (GUI.RepeatButton(new Rect(m_logWPos, (float)Screen.height - m_bottomPanelHeight, m_buttonWidth, m_bottomPanelHeight), "up"))
			{
				m_index--;
			}
			if (GUI.RepeatButton(new Rect(m_logWPos + m_buttonWidth, (float)Screen.height - m_bottomPanelHeight, m_buttonWidth, m_bottomPanelHeight), "down"))
			{
				m_index++;
			}
			if (GUI.Button(new Rect(m_logWPos + m_buttonWidth * 2f, (float)Screen.height - m_bottomPanelHeight, m_buttonWidth, m_bottomPanelHeight), m_isScroll ? "scrl" : "stop"))
			{
				m_isScroll = !m_isScroll;
			}
			if (GUI.Button(new Rect(m_logWPos + m_buttonWidth * 3f, (float)Screen.height - m_bottomPanelHeight, m_buttonWidth, m_bottomPanelHeight), "end"))
			{
				m_isScroll = true;
				m_index = m_logList.Count - 4;
			}
			if (GUI.Button(new Rect(m_logWPos + m_buttonWidth * 4f, (float)Screen.height - m_bottomPanelHeight, m_buttonWidth, m_bottomPanelHeight), "clr"))
			{
				Clear();
			}
			WarnNotTest();
		}

		private void WarnNotTest()
		{
			GUI.Label(new Rect(0f, 0f, Screen.width, Screen.height), "<color=white><size=" + m_fontSize * 0.6f + ">WARNING_NOT_TEST_BUILD</size></color>", m_upperCenterStyle);
		}
	}

	private static int m_counter;

	private static readonly List<string> m_logList = new List<string>();

	private static GameObject m_screenConsoleGameObject;

	private const int _CASHE_RECORD_SIZE = 1000;

	public static void Log(object logString)
	{
	}

	public static void LogEnumerable<T>(IEnumerable<T> objects)
	{
	}

	private static void Init()
	{
		if (m_screenConsoleGameObject == null)
		{
			m_screenConsoleGameObject = new GameObject("ScreenDebug");
		}
	}

	public static void Clear()
	{
	}
}
