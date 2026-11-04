using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public static class GameLog
{
	private class FileInfo
	{
		public long date;

		public string name;
	}

	public static bool instantFlush;

	private static List<string> m_logGroupIgnoreList;

	private static bool m_Initiated;

	private static StreamWriter m_file;

	private static string m_fileName;

	private static Dictionary<string, LogGroupSettings> m_groupList;

	private static bool m_skipSelfCallback;

	static GameLog()
	{
		instantFlush = true;
		m_logGroupIgnoreList = new List<string>();
		m_Initiated = false;
		m_groupList = new Dictionary<string, LogGroupSettings>();
		m_skipSelfCallback = false;
		SetupGroup("System", AlertLevel.Notify, CheckLevel.Any);
		SetupGroup("Unity", AlertLevel.Notify, CheckLevel.Any);
		if (Application.isEditor)
		{
			m_fileName = Application.dataPath + "/../log.txt";
			SetupGroup("Debug", AlertLevel.Warning, CheckLevel.Fail);
			SetupGroup("Multiplayer", AlertLevel.Warning, CheckLevel.Fail);
			SetupGroup("GameSync", AlertLevel.Notify, CheckLevel.Any);
			SetupGroup("Top32", AlertLevel.Notify, CheckLevel.Any);
			SetupGroup("VirtualReality", AlertLevel.Notify, CheckLevel.Any);
			SetupGroup("Video", AlertLevel.Notify, CheckLevel.Any);
			SetupGroup("ServerQuests", AlertLevel.Notify, CheckLevel.Any);
			SetupGroup("LiveryTransferSystem", AlertLevel.Notify, CheckLevel.Any);
			SetupGroup("ModMaps", AlertLevel.Notify, CheckLevel.Fail);
			SetupGroup("IconStorage", AlertLevel.Warning, CheckLevel.Any);
			SetupGroup("NetGameCarSwap", AlertLevel.Warning, CheckLevel.Any);
			SetupGroup("Chat", AlertLevel.Warning, CheckLevel.Any);
			SetupGroup("AssetBundleLoader", AlertLevel.Warning, CheckLevel.Any);
		}
		else
		{
			m_fileName = Application.dataPath + "/log.txt";
			SetupGroup("Debug", AlertLevel.Error, CheckLevel.Fail);
			SetupGroup("IconStorage", AlertLevel.Error, CheckLevel.Fail);
			SetupGroup("Chat", AlertLevel.Error, CheckLevel.Any);
			SetupGroup("AssetBundleLoader", AlertLevel.Error, CheckLevel.Fail);
		}
		GameConsole.SetCmd("debug", DebugToggle);
		GameConsole.SetCmd("ignore_group", delegate
		{
			SetGroupIgnoreListOccurrence(include: true);
		});
		GameConsole.SetCmd("unignore_group", delegate
		{
			SetGroupIgnoreListOccurrence(include: false);
		});
		if (!Application.isPlaying)
		{
			return;
		}
		try
		{
			try
			{
				if (File.Exists(m_fileName))
				{
					string directoryName = Path.GetDirectoryName(m_fileName);
					char altDirectorySeparatorChar = Path.AltDirectorySeparatorChar;
					string text = directoryName + altDirectorySeparatorChar;
					string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(m_fileName);
					string extension = Path.GetExtension(m_fileName);
					string text2 = text + fileNameWithoutExtension + "_old" + extension;
					if (File.Exists(text2))
					{
						File.Delete(text2);
					}
					if (!CheckForCrash(m_fileName))
					{
						File.Move(m_fileName, text2);
					}
					File.Delete(m_fileName);
				}
			}
			catch
			{
			}
			m_file = new StreamWriter(m_fileName);
			m_Initiated = true;
			Print(AlertLevel.Notify, "System", $"Log started: {DateTime.Now} Build: {Application.buildGUID}");
			Print(AlertLevel.Notify, "System", "Launch params: " + Environment.CommandLine);
			AddTextDelimiter();
			Application.logMessageReceived -= LogCallback;
			Application.logMessageReceived += LogCallback;
			GameEvents.OnGameEngineStop -= new GameEventHandler<GameEvents.EventInfo>(Close);
			GameEvents.OnGameEngineStop += new GameEventHandler<GameEvents.EventInfo>(Close);
		}
		catch
		{
			m_Initiated = false;
		}
	}

	private static void DebugToggle()
	{
		if (GameConsole.cmdArgs.Length < 2)
		{
			GameConsole.Print("Debug group was not specified! Use name of group or '*' for all groups");
			ShowGroupStates();
			return;
		}
		foreach (LogGroupSettings value in m_groupList.Values)
		{
			if ((int)value.alertLevel < 2)
			{
				value.alertLevel = AlertLevel.Error;
			}
			if ((int)value.checkLevel < 2)
			{
				value.checkLevel = CheckLevel.Fail;
			}
		}
		SetupGroup("System", AlertLevel.Notify, CheckLevel.Any);
		SetupGroup("Unity", AlertLevel.Notify, CheckLevel.Any);
		string[] array = GameConsole.cmdArgs[1].Split(' ');
		if (array.Length == 1 && array[0] == "*")
		{
			foreach (string key in m_groupList.Keys)
			{
				SetupGroup(key, AlertLevel.Notify, CheckLevel.Any);
				GameConsole.Print("Trace for '" + key + "' AlertLevel.Notify CheckLevel.Any");
			}
			return;
		}
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].ToLower();
			if (m_groupList.ContainsKey(text))
			{
				SetupGroup(text, AlertLevel.Notify, CheckLevel.Any);
				GameConsole.Print("Trace for '" + text + "' AlertLevel.Notify CheckLevel.Any");
			}
			else
			{
				GameConsole.Print("Group '" + text + "' not found");
			}
		}
	}

	private static void SetGroupIgnoreListOccurrence(bool include)
	{
		if (GameConsole.cmdArgs.Length != 2)
		{
			GameConsole.Print("Command takes one group name as an argument");
			return;
		}
		string text = GameConsole.cmdArgs[1].ToLower();
		if (!m_groupList.ContainsKey(text))
		{
			GameConsole.Print(text + " is not a correct group name");
			return;
		}
		bool flag = m_logGroupIgnoreList.Contains(text);
		if (include)
		{
			if (!flag)
			{
				m_logGroupIgnoreList.Add(text);
				GameConsole.Print("Added " + text + " to debug log ignore list");
			}
			else
			{
				GameConsole.Print(text + " already in debug log ignore list");
			}
		}
		else if (flag)
		{
			m_logGroupIgnoreList.Remove(text);
			GameConsole.Print("Removed " + text + " from debug log ignore list");
		}
		else
		{
			GameConsole.Print(text + " not in debug log ignore list");
		}
	}

	private static void ShowGroupStates()
	{
		GameConsole.Print("Group states:");
		foreach (KeyValuePair<string, LogGroupSettings> group in m_groupList)
		{
			GameConsole.Print($"Group:'{group.Key,20}'       Alert:{group.Value.alertLevel,10}       Check:{group.Value.checkLevel,10}");
		}
	}

	[Conditional("DEBUG")]
	[Conditional("DEV_BUILD")]
	[Conditional("TEST_BUILD")]
	public static void Log(string message, UnityEngine.Object obj = null)
	{
		UnityEngine.Debug.Log(message, obj);
	}

	public static void LogWarning(string message, UnityEngine.Object obj = null)
	{
		UnityEngine.Debug.LogWarning(message, obj);
	}

	public static void LogError(string message, UnityEngine.Object obj = null)
	{
		UnityEngine.Debug.LogError(message, obj);
	}

	public static void LogException(Exception e)
	{
		UnityEngine.Debug.LogError(e.Message);
	}

	private static void LogCallback(string condition, string stackTrace, LogType type)
	{
		if (m_skipSelfCallback)
		{
			m_skipSelfCallback = false;
			return;
		}
		Print(type.ToAlertLevel(), "Unity", condition);
		m_skipSelfCallback = false;
	}

	private static void Close(GameEvents.EventInfo e)
	{
		if (m_Initiated)
		{
			AddTextDelimiter();
			Print(AlertLevel.Notify, "System", "Log ended: " + DateTime.Now);
			m_Initiated = false;
			m_file.Write('\t');
			m_file.Flush();
			m_file.Close();
			m_file = null;
		}
	}

	public static void AddTextDelimiter()
	{
		Add("------------------------------------------------------------", timeStampNeeded: false);
	}

	public static AlertLevel ToAlertLevel(this LogType logType)
	{
		return logType switch
		{
			LogType.Log => AlertLevel.Notify, 
			LogType.Warning => AlertLevel.Warning, 
			LogType.Error => AlertLevel.Error, 
			LogType.Exception => AlertLevel.FatalError, 
			LogType.Assert => AlertLevel.Error, 
			_ => AlertLevel.Silence, 
		};
	}

	public static string GetName(this AlertLevel level)
	{
		return level switch
		{
			AlertLevel.Notify => "[Notify]    : ", 
			AlertLevel.Warning => "[Warning]   : ", 
			AlertLevel.Error => "[Error]     : ", 
			AlertLevel.FatalError => "[FatalError]: ", 
			_ => "[Silence]   : ", 
		};
	}

	public static string GetName(this CheckLevel level)
	{
		return level switch
		{
			CheckLevel.Any => "<Any>", 
			CheckLevel.Fail => "<Fail>", 
			CheckLevel.Success => "<Success>", 
			_ => "<Silence>", 
		};
	}

	public static AlertLevel GetAlertByName(string name)
	{
		return name switch
		{
			"notify" => AlertLevel.Notify, 
			"warning" => AlertLevel.Warning, 
			"error" => AlertLevel.Error, 
			"fatalerror" => AlertLevel.FatalError, 
			_ => AlertLevel.Silence, 
		};
	}

	public static CheckLevel GetCheckByName(string name)
	{
		return name switch
		{
			"any" => CheckLevel.Any, 
			"fail" => CheckLevel.Fail, 
			"success" => CheckLevel.Success, 
			_ => CheckLevel.Silence, 
		};
	}

	public static LogGroupSettings Group(string name)
	{
		name = name.ToLower();
		if (!m_groupList.TryGetValue(name, out var value))
		{
			return RegisterGroup(name, silent: true);
		}
		return value;
	}

	public static void SetupGroup(string name, AlertLevel alertLevel, CheckLevel checkLevel)
	{
		LogGroupSettings logGroupSettings = Group(name);
		logGroupSettings.alertLevel = alertLevel;
		logGroupSettings.checkLevel = checkLevel;
	}

	public static void Print(AlertLevel alertLevel, string groupName, string text, UnityEngine.Object obj = null)
	{
		if (!m_logGroupIgnoreList.Contains(groupName.ToLower()))
		{
			LogGroupSettings logGroupSettings = Group(groupName);
			if (logGroupSettings.alertLevel != AlertLevel.Silence && (int)logGroupSettings.alertLevel <= (int)alertLevel)
			{
				string text2 = groupName + "> " + text;
				Add(alertLevel.GetName() + text2);
			}
		}
	}

	public static void Check(AlertLevel alertLevel, string groupName, string text, bool val, UnityEngine.Object obj = null)
	{
		if (m_logGroupIgnoreList.Contains(groupName.ToLower()))
		{
			return;
		}
		LogGroupSettings logGroupSettings = Group(groupName);
		if (logGroupSettings.alertLevel != AlertLevel.Silence && logGroupSettings.checkLevel != CheckLevel.Silence)
		{
			bool flag = false;
			if ((int)logGroupSettings.alertLevel <= (int)alertLevel)
			{
				flag = logGroupSettings.checkLevel == CheckLevel.Any || (val && logGroupSettings.checkLevel == CheckLevel.Success) || (!val && logGroupSettings.checkLevel == CheckLevel.Fail);
			}
			if (flag)
			{
				string text2 = groupName + "> " + text + (val ? "...OK" : "...FAIL");
				Add(alertLevel.GetName() + text2);
			}
		}
	}

	[Conditional("DEBUG")]
	[Conditional("DEV_BUILD")]
	[Conditional("TEST_BUILD")]
	private static void SendMessageToUnityLog(AlertLevel alertLevel, string message, UnityEngine.Object obj)
	{
		if (!m_skipSelfCallback)
		{
			m_skipSelfCallback = true;
			switch (alertLevel)
			{
			case AlertLevel.Notify:
				UnityEngine.Debug.Log(message, obj);
				break;
			case AlertLevel.Warning:
				UnityEngine.Debug.LogWarning(message, obj);
				break;
			case AlertLevel.Error:
				UnityEngine.Debug.LogError(message, obj);
				break;
			case AlertLevel.FatalError:
				UnityEngine.Debug.LogError(message, obj);
				break;
			default:
				UnityEngine.Debug.Log(message, obj);
				break;
			}
			m_skipSelfCallback = false;
		}
	}

	[Conditional("UNITY_PS4")]
	private static void EchoLogToStdConsole(string message, AlertLevel alertLevel)
	{
		switch (alertLevel)
		{
		case AlertLevel.Silence:
			break;
		case AlertLevel.Warning:
		case AlertLevel.Error:
			Console.Error.WriteLine(message);
			break;
		default:
			Console.WriteLine(message);
			break;
		}
	}

	private static void Add(string text, bool timeStampNeeded = true)
	{
		GameConsole.Print(text);
		if (m_Initiated)
		{
			if (timeStampNeeded)
			{
				text = DateTime.Now.ToString("HH:mm:ss") + ":" + text;
			}
			m_file.WriteLine(text);
			if (instantFlush)
			{
				m_file.Flush();
			}
		}
	}

	private static LogGroupSettings RegisterGroup(string name, bool silent = false)
	{
		LogGroupSettings logGroupSettings = new LogGroupSettings();
		logGroupSettings.alertLevel = AlertLevel.Error;
		logGroupSettings.checkLevel = CheckLevel.Fail;
		m_groupList.Add(name, logGroupSettings);
		if (!silent)
		{
			Print(AlertLevel.Warning, "System", "New group '" + name + "' registered.");
		}
		return logGroupSettings;
	}

	private static bool CheckForCrash(string fileName)
	{
		if (File.Exists(fileName))
		{
			string directoryName = Path.GetDirectoryName(fileName);
			char altDirectorySeparatorChar = Path.AltDirectorySeparatorChar;
			string text = directoryName + altDirectorySeparatorChar;
			RemoveOldCrashes(text + "logs\\");
			StreamReader streamReader = new StreamReader(fileName);
			streamReader.BaseStream.Seek(-1L, SeekOrigin.End);
			int num = streamReader.Read();
			streamReader.Close();
			if (num != 9)
			{
				DateTime now = DateTime.Now;
				string text2 = now.ToString("dd_MM_yy") + "_crash_" + now.ToString("HH_mm_ss");
				File.Move(fileName, text + "logs\\" + text2 + ".txt");
				return true;
			}
		}
		return false;
	}

	private static void RemoveOldCrashes(string path)
	{
		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
		}
		List<FileInfo> list = Directory.GetFiles(path).Map((string n) => new FileInfo
		{
			name = n,
			date = File.GetCreationTime(n).Ticks
		});
		if (list.Count <= 9)
		{
			return;
		}
		list.Sort(delegate(FileInfo lhs, FileInfo rhs)
		{
			if (lhs.date > rhs.date)
			{
				return -1;
			}
			return (lhs.date < rhs.date) ? 1 : 0;
		});
		for (int num = 9; num < list.Count - 1; num++)
		{
			File.Delete(list[num].name);
		}
	}
}
