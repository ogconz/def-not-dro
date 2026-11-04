using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CarProfile;
using CarX;
using DB;
using DB.Meta;
using DI;
using Db.Meta.CarProfile;
using PrefsModels;
using SyncMultiplayer;
using Tayx.Graphy;
using Tayx.Graphy.Utils;
using TrackLayout.Builders;
using TrackLayout.Data;
using UnityEngine;

public static class GameConsole
{
	[Flags]
	public enum Flags : byte
	{
		None = 0,
		Debug = 1,
		ReadOnly = 2,
		Hidden = 4,
		VoidDefault = 8,
		ContextVar = 0x10
	}

	public class Var
	{
		public string value;

		public string defaultValue;

		public Flags flags;
	}

	public class Cmd
	{
		public Action command;

		public Flags flags;
	}

	public class StringVarGetter
	{
		public string this[string key]
		{
			get
			{
				Var var = GetVar(key);
				if (var != null)
				{
					return var.value;
				}
				return "";
			}
			set
			{
				SetVar(key, value, Flags.VoidDefault);
			}
		}
	}

	public class FloatVarGetter
	{
		public float this[string key]
		{
			get
			{
				Var var = GetVar(key);
				if (var == null)
				{
					return 0f;
				}
				if (!float.TryParse(var.value, out var result))
				{
					return 0f;
				}
				return result;
			}
			set
			{
				SetVar(key, value.ToString(), Flags.VoidDefault);
			}
		}
	}

	public class IntVarGetter
	{
		public int this[string key]
		{
			get
			{
				Var var = GetVar(key);
				if (var == null)
				{
					return 0;
				}
				if (!int.TryParse(var.value, out var result))
				{
					return 0;
				}
				return result;
			}
			set
			{
				SetVar(key, value.ToString(), Flags.VoidDefault);
			}
		}
	}

	public class BoolVarGetter
	{
		public bool this[string key]
		{
			get
			{
				Var var = GetVar(key);
				if (var == null)
				{
					return false;
				}
				if (int.TryParse(var.value, out var result))
				{
					return result > 0;
				}
				return false;
			}
			set
			{
				SetVar(key, value ? "1" : "0", Flags.VoidDefault);
			}
		}
	}

	public const int DISPLAY_LIMIT = 80;

	private static int m_displayGeneration;

	private static int m_historyPointer;

	private static List<string> m_history;

	private static List<string> m_display;

	private static List<int> m_displayLineCounter;

	public static string[] cmdArgs;

	public static bool echo;

	private static Dictionary<string, Var> m_vars;

	private static Dictionary<string, Cmd> m_cmds;

	private static string m_fileName;

	private static string m_autoName;

	private static string m_dataPath;

	private static bool m_IsEditor;

	private static bool m_ignoreCommands;

	private static bool m_ignoreVars;

	public static IntVarGetter Int;

	public static FloatVarGetter Float;

	public static BoolVarGetter Bool;

	public static StringVarGetter String;

	private static bool m_clearPrefsBtnEnabled;

	public static int displayGeneration => m_displayGeneration;

	public static List<string> display => m_display;

	public static List<int> displayLineCounter => m_displayLineCounter;

	static GameConsole()
	{
		m_displayGeneration = 0;
		m_historyPointer = 0;
		m_history = new List<string>();
		m_display = new List<string>();
		m_displayLineCounter = new List<int>();
		cmdArgs = new string[0];
		echo = true;
		m_vars = new Dictionary<string, Var>();
		m_cmds = new Dictionary<string, Cmd>();
		m_fileName = "game.cfg";
		m_autoName = "gauto.cfg";
		m_dataPath = "";
		m_IsEditor = false;
		m_ignoreCommands = false;
		m_ignoreVars = false;
		Int = new IntVarGetter();
		Float = new FloatVarGetter();
		Bool = new BoolVarGetter();
		String = new StringVarGetter();
		m_clearPrefsBtnEnabled = true;
		m_IsEditor = Application.isEditor;
		if (m_IsEditor)
		{
			m_dataPath = Application.dataPath + "/../";
		}
		else
		{
			m_dataPath = Application.dataPath + "/";
		}
		InitCommandsAndVars();
		GameEvents.OnGameEngineStart.Subscribe(GameEventFlag.OneCall, delegate
		{
			m_ignoreVars = true;
			try
			{
				LaunchFrom(m_fileName);
			}
			finally
			{
				m_ignoreVars = false;
			}
			LaunchFrom(m_autoName);
		});
	}

	public static void PreLoad()
	{
		m_ignoreCommands = true;
		try
		{
			LaunchFrom(m_fileName);
		}
		finally
		{
			m_ignoreCommands = false;
		}
	}

	private static bool IsDebugBuild()
	{
		return m_IsEditor;
	}

	public static void Save()
	{
		try
		{
			List<string> list = new List<string>();
			foreach (KeyValuePair<string, Var> var in m_vars)
			{
				if (var.Value.value != var.Value.defaultValue)
				{
					if ((int)(var.Value.flags & Flags.ContextVar) > 0)
					{
						list.Add("!" + var.Key + " " + var.Value.value);
					}
					else
					{
						list.Add(var.Key + " " + var.Value.value);
					}
				}
			}
			WriteLines(m_fileName, list);
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "System", "Console save failed: " + ex.Message);
		}
	}

	private static void LaunchFrom(string fileName)
	{
		try
		{
			string[] array = ReadLines(fileName);
			for (int i = 0; i < array.Length; i++)
			{
				UserCommand(array[i], useVoidFlag: true);
			}
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "System", "Console load failed: " + ex.Message);
		}
	}

	private static void WriteLines(string fileName, List<string> lines)
	{
		WriteLines_File(m_fileName, lines);
	}

	private static void WriteLines_File(string fileName, List<string> lines)
	{
		File.WriteAllLines(m_dataPath + m_fileName, lines);
	}

	private static void WriteLines_PlayerPrefs(string fileName, List<string> lines)
	{
		string value = string.Join("\n", lines);
		PlayerPrefs.SetString(GetPrefsKey(fileName), value);
	}

	private static string GetPrefsKey(string fileName)
	{
		return "GameConsole_" + fileName;
	}

	private static string[] ReadLines(string fileName)
	{
		return ReadLines_FromFile(fileName);
	}

	private static string[] ReadLines_FromFile(string fileName)
	{
		if (File.Exists(m_dataPath + fileName))
		{
			File.ReadAllLines(m_dataPath + fileName);
		}
		return new string[0];
	}

	private static string[] ReadLines_FromPlayerPrefs(string fileName)
	{
		string prefsKey = GetPrefsKey(fileName);
		if (PlayerPrefs.HasKey(prefsKey))
		{
			string text = PlayerPrefs.GetString(prefsKey);
			if (!string.IsNullOrEmpty(text))
			{
				return text.Split('\n');
			}
		}
		return new string[0];
	}

	private static void CmdList()
	{
		Print("--- Cmd List ---");
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, Var> var in m_vars)
		{
			if ((IsDebugBuild() || (int)(var.Value.flags & Flags.Debug) <= 0) && (int)(var.Value.flags & Flags.Hidden) <= 0)
			{
				list.Add(var.Key);
			}
		}
		foreach (KeyValuePair<string, Cmd> cmd in m_cmds)
		{
			if ((!IsDebugBuild() && (int)(cmd.Value.flags & Flags.Debug) > 0) || (int)(cmd.Value.flags & Flags.Hidden) > 0)
			{
				continue;
			}
			bool flag = true;
			foreach (KeyValuePair<string, Var> var2 in m_vars)
			{
				if (string.Compare(cmd.Key, var2.Key, ignoreCase: true) == 0)
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				list.Add(cmd.Key);
			}
		}
		list.Sort();
		foreach (string item in list)
		{
			Print(item);
		}
		Print("--- end of Cmd List ---");
	}

	public static string UpPressed()
	{
		m_historyPointer--;
		if (m_historyPointer < 0)
		{
			m_historyPointer = 0;
		}
		if (m_historyPointer >= m_history.Count)
		{
			return "";
		}
		return m_history[m_historyPointer];
	}

	public static string DownPressed()
	{
		m_historyPointer++;
		if (m_historyPointer > m_history.Count)
		{
			m_historyPointer = m_history.Count;
		}
		if (m_historyPointer >= m_history.Count)
		{
			return "";
		}
		return m_history[m_historyPointer];
	}

	public static string TabPressed(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return input;
		}
		List<string> list = new List<string>();
		int length = input.Length;
		int num = int.MaxValue;
		foreach (KeyValuePair<string, Var> var in m_vars)
		{
			string key = var.Key;
			Var value = var.Value;
			if ((IsDebugBuild() || (int)(value.flags & Flags.Debug) <= 0) && (int)(value.flags & Flags.Hidden) <= 0 && key.Length >= length && string.Compare(key.Substring(0, length), input, ignoreCase: true) == 0)
			{
				list.Add(key);
				if (key.Length < num)
				{
					num = key.Length;
				}
			}
		}
		foreach (KeyValuePair<string, Cmd> cmd in m_cmds)
		{
			string key2 = cmd.Key;
			if ((int)(cmd.Value.flags & Flags.Hidden) > 0 || key2.Length < length || string.Compare(key2.Substring(0, length), input, ignoreCase: true) != 0)
			{
				continue;
			}
			bool flag = true;
			foreach (string key3 in m_vars.Keys)
			{
				if (string.Compare(key3, key2, ignoreCase: true) == 0)
				{
					flag = false;
				}
			}
			if (flag)
			{
				list.Add(key2);
				if (key2.Length < num)
				{
					num = key2.Length;
				}
			}
		}
		if (list.Count > 0)
		{
			if (list.Count == 1)
			{
				return list[0] + " ";
			}
			Print("List:");
			foreach (string item in list)
			{
				Print(item);
			}
			int num2 = 0;
			char c = list[0][num2];
			while (true)
			{
				for (int i = 0; i < list.Count; i++)
				{
					if (c != list[i][num2])
					{
						return list[i].Substring(0, num2);
					}
				}
				num2++;
				if (num2 >= num)
				{
					break;
				}
				c = list[0][num2];
			}
			return list[0].Substring(0, num2);
		}
		return input;
	}

	public static void Print(string text)
	{
		if (!echo || string.IsNullOrEmpty(text))
		{
			return;
		}
		m_displayGeneration++;
		if (m_display.Count == 0 || !m_display.Last().Equals(text))
		{
			m_display.Add(text);
			m_displayLineCounter.Add(0);
		}
		else
		{
			int num = m_displayLineCounter.Count - 1;
			if (num >= 0)
			{
				List<int> list = m_displayLineCounter;
				int index = num;
				int value = list[index] + 1;
				list[index] = value;
			}
		}
		if (m_display.Count > 80)
		{
			m_display.RemoveRange(0, m_display.Count - 80);
			m_displayLineCounter.RemoveRange(0, m_displayLineCounter.Count - 80);
		}
	}

	public static Var SetContextVar(string name, string value, Flags flags = Flags.None, Action attachedCmd = null)
	{
		if (attachedCmd != null)
		{
			SetCmd(name, attachedCmd);
		}
		name = name.ToLower();
		if (m_vars.TryGetValue(name, out var value2))
		{
			if (IsDebugBuild() || (value2.flags & Flags.Debug) == 0)
			{
				value2.defaultValue = value;
				value2.flags = flags | Flags.ContextVar;
			}
		}
		else
		{
			value2 = new Var();
			value2.flags = flags | Flags.ContextVar;
			value2.defaultValue = value;
			value2.value = value;
			m_vars.Add(name, value2);
		}
		return value2;
	}

	public static Var SetVar(string name, string value, Flags flags = Flags.None)
	{
		name = name.ToLower();
		if (m_vars.TryGetValue(name, out var value2))
		{
			if (IsDebugBuild() || (value2.flags & Flags.Debug) == 0)
			{
				value2.value = value;
			}
		}
		else
		{
			value2 = new Var();
			if ((flags & Flags.VoidDefault) == 0)
			{
				value2.flags = flags;
				value2.defaultValue = value;
			}
			else
			{
				value2.flags = flags | Flags.Hidden;
				value2.defaultValue = "";
			}
			value2.value = value;
			m_vars.Add(name, value2);
		}
		return value2;
	}

	public static Var GetVar(string name)
	{
		name = name.ToLower();
		Var value = null;
		m_vars.TryGetValue(name, out value);
		return value;
	}

	public static void RegisterCarXParamFloat(ParamFloat param, string description)
	{
		string cmd = "crxf_" + param;
		SetCmd(cmd, delegate
		{
			SteeringMode steerMode = SteeringMode.Buttons;
			float param2 = float.MaxValue;
			if (!TryParse(ref steerMode, ref param2))
			{
				Print(description);
				Print("use: " + cmd + " " + GetSteerModes() + " param");
			}
			else
			{
				float buildInParamFloat = ScriptableSingleton<ControlsSetup>.instance.GetBuildInParamFloat(steerMode, param);
				if (param2 == float.MaxValue)
				{
					Print(description);
					float currentFloat = ScriptableSingleton<ControlsSetup>.instance.GetCurrentFloat(steerMode, param);
					Print($"{cmd}[{steerMode}] is {currentFloat} (default {buildInParamFloat})");
				}
				else
				{
					ScriptableSingleton<ControlsSetup>.instance.OverrideParamFloat(steerMode, param, param2);
					float currentFloat = ScriptableSingleton<ControlsSetup>.instance.GetCurrentFloat(steerMode, param);
					Print($"{cmd}[{steerMode}] set {currentFloat} (default {buildInParamFloat})");
				}
			}
		});
	}

	private static bool TryParse(ref SteeringMode steerMode, ref float param)
	{
		if (cmdArgs.Length >= 2 && Enum.TryParse<SteeringMode>(cmdArgs[1], ignoreCase: true, out steerMode))
		{
			if (cmdArgs.Length == 3 && float.TryParse(cmdArgs[2], out var result))
			{
				param = result;
			}
			return true;
		}
		return false;
	}

	private static string GetSteerModes()
	{
		return string.Join("|", Enum.GetNames(typeof(SteeringMode))) ?? "";
	}

	public static void RegisterCarXParamInt(ParamInt param, string description)
	{
		string cmd = "crxi_" + param;
		SetCmd(cmd, delegate
		{
			SteeringMode steerMode = SteeringMode.Buttons;
			float param2 = float.MaxValue;
			if (!TryParse(ref steerMode, ref param2))
			{
				Print(description);
				Print("use: " + cmd + " " + GetSteerModes() + " param");
			}
			else
			{
				int builtInParamInt = ScriptableSingleton<ControlsSetup>.instance.GetBuiltInParamInt(steerMode, param);
				if (param2 == float.MaxValue)
				{
					Print(description);
					int currentInt = ScriptableSingleton<ControlsSetup>.instance.GetCurrentInt(steerMode, param);
					Print($"{cmd}[{steerMode}] is {currentInt} (default {builtInParamInt})");
				}
				else
				{
					ScriptableSingleton<ControlsSetup>.instance.OverrideParamInt(steerMode, param, (int)param2);
					int currentInt = ScriptableSingleton<ControlsSetup>.instance.GetCurrentInt(steerMode, param);
					Print($"{cmd}[{steerMode}] set {currentInt} (default {builtInParamInt})");
				}
			}
		});
	}

	public static void SetCmd(string name, Action callback, Flags flags = Flags.None)
	{
		if (IsDebugBuild() || (flags & Flags.Debug) == 0)
		{
			name = name.ToLower();
			if (!m_cmds.TryGetValue(name, out var value))
			{
				value = new Cmd();
				m_cmds.Add(name, value);
			}
			value.command = callback;
			value.flags = flags;
		}
	}

	public static void RemoveCmd(string name)
	{
		name = name.ToLower();
		m_cmds.Remove(name);
	}

	private static void AddToHistory(string cmd)
	{
		if (!m_ignoreCommands && (IsDebugBuild() || cmd[0] != '#'))
		{
			cmd = cmd.Trim();
			if (m_history.Count > 10)
			{
				m_history.RemoveRange(0, m_history.Count - 10);
			}
			m_history.RemoveAll((string e) => string.Compare(e, cmd) == 0);
			m_history.Add(cmd);
			m_historyPointer = m_history.Count;
		}
	}

	public static void ToggleBool(string var)
	{
		echo = false;
		try
		{
			bool flag = Bool[var];
			UserCommand(var + (flag ? " 0" : " 1"));
		}
		catch
		{
		}
		echo = true;
	}

	public static void UserCommand(string cmd, bool useVoidFlag = false)
	{
		cmdArgs = cmd.Split(' ');
		if (cmdArgs.Length == 0 || string.IsNullOrEmpty(cmdArgs[0]))
		{
			return;
		}
		bool flag = false;
		if (useVoidFlag && cmdArgs[0].StartsWith("!"))
		{
			flag = true;
			cmdArgs[0] = cmdArgs[0].Substring(1);
		}
		if (string.IsNullOrEmpty(cmdArgs[0]))
		{
			return;
		}
		useVoidFlag = useVoidFlag && (flag || cmdArgs[0].StartsWith("#"));
		Var var = GetVar(cmdArgs[0]);
		if (!m_ignoreVars)
		{
			if (var != null)
			{
				if ((var.flags & Flags.Hidden) == 0)
				{
					AddToHistory(cmd);
				}
				if (cmdArgs.Length == 1)
				{
					Print(cmdArgs[0] + " is " + var.value);
				}
				else if ((int)(var.flags & Flags.ReadOnly) > 0)
				{
					Print(cmdArgs[0] + " is write protected!");
				}
				else
				{
					string value = var.value;
					SetVar(cmdArgs[0], cmdArgs[1]);
					Print(cmdArgs[0] + " is set to " + var.value + " (old: " + value + ")");
				}
			}
			else if (useVoidFlag && cmdArgs.Length > 1)
			{
				Flags flags = Flags.VoidDefault;
				if (flag)
				{
					flags |= Flags.ContextVar;
				}
				SetVar(cmdArgs[0], cmdArgs[1], flags);
				Print(cmdArgs[0] + " is set to " + cmdArgs[1]);
			}
		}
		if (m_ignoreCommands)
		{
			return;
		}
		if (m_cmds.TryGetValue(cmdArgs[0].ToLower(), out var value2))
		{
			if (var == null && (value2.flags & Flags.Hidden) == 0)
			{
				AddToHistory(cmd);
			}
			value2.command();
		}
		else if (var == null)
		{
			AddToHistory(cmd);
		}
	}

	private static void InitCommandsAndVars()
	{
		SetVar("cl_allowselfchallenge", "0", Flags.Debug);
		SetVar("cl_debug", "0", Flags.Debug);
		SetVar("sv_maxplayers", "16");
		SetVar("cl_fastAssistance", "0");
		SetCmd("cl_fastAssistance", delegate
		{
			if ((bool)PlayerCarControl.instance)
			{
				PlayerCarControl.instance.fastSteering = Bool["cl_fastAssistance"];
			}
		});
		SetVar("r_fovMirrorBase", "60");
		SetVar("r_fov", "60");
		SetCmd("r_fov", UpdateFOV);
		SetVar("r_fov_cockpit", "60");
		SetCmd("r_fov_cockpit", UpdateFOV);
		SetCmd("vinyls_count", ShowVinylsCount);
		SetCmd("cockpit_mode", SetCockpitCameraMode);
		SetCmd("cockpit_max_speed", SetCockpitCameraSpeed);
		SetCmd("cockpit_smooth", SetCockpitCameraSmooth);
		SetCmd("cockpit_max_angle", SetCockpitCameraMaxAngle);
		SetCmd("cockpit_vector_scale", SetCockpitCameraVector);
		SetCmd("cockpit_use_vel", SetCockpitCameraUseVel);
		SetCmd("cockpit_use_accel", SetCockpitCameraUseAccel);
		SetCmd("cockpit_accel_smooth", SetCockpitCameraAccelSmooth);
		SetCmd("cockpit_cam_deadzone", SetCockpitCamDeadzone);
		SetVar("dinput", "0");
		SetCmd("dinput", UpdateDInputFeedbackSettings);
		SetVar("ffb_force", "0");
		SetCmd("ffb_force", UpdateDInputFeedbackSettings);
		SetVar("r_points", "1");
		SetCmd("r_points", UpdatePoints);
		SetVar("r_input", "0");
		SetCmd("r_hideDriver", SwitchDriverShowing);
		SetCmd("r_hideWheel", SwitchWheelShowing);
		SetVar("ChampionshipMode", "0");
		SetCmd("ResetHalloween", delegate
		{
			DependencyInjector.Resolve<GamePrefs>()?.eventsData?.Reset();
			StatsManager.SetStat(StatsManager.EStats.HalloweenWin, 0);
		}, Flags.Hidden);
		SetCmd("WinHalloween", delegate
		{
			EventSettings eventSettings = ResourcesWrapper.Load<EventSettings>("HalloweenEventSettings");
			GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
			gamePrefs.eventsData.pumpkinsCount = eventSettings.maxCountPumpkins;
			gamePrefs.eventsData.halloweenComplete = true;
			foreach (DB.Meta.Track item in DependencyInjector.Resolve<BaseModel>().QueryAll<DB.Meta.Track>())
			{
				if (!(item.configuration != "Default") && item.enabled && !item.hidden && TrackMetaDB.HaveConfig(item.scene, RaceMode.HalloweenEvent))
				{
					if (gamePrefs.eventsData.halloweenLocksTracks.ContainsKey(item.id))
					{
						gamePrefs.eventsData.halloweenLocksTracks[item.id] = 1;
					}
					else
					{
						gamePrefs.eventsData.halloweenLocksTracks.Add(item.id, 1);
					}
				}
			}
		}, Flags.Hidden);
		SetVar("#network_lobby_sorter", "", Flags.Hidden);
		SetVar("#network_lobby_lastname", "", Flags.Hidden);
		SetVar("#network_lobby_lastlocked", "0", Flags.Hidden);
		SetVar("#network_lobby_lastsettings", "-1", Flags.Hidden);
		SetVar("#network_lobby_lasttrack", "-1", Flags.Hidden);
		SetVar("#network_lobby_lastnightmode", "0", Flags.Hidden);
		SetVar("#mainmenu_category", "0", Flags.Hidden);
		SetVar("#mainmenu_singleplayer_ticket", "0", Flags.Hidden);
		SetVar("#mainmenu_multiplayer_ticket", "0", Flags.Hidden);
		SetVar("#restdata", "0", Flags.Debug);
		SetCmd("additionalWheelFX", delegate
		{
			WheelForceFeedbackSupport.disableWheelFX = !WheelForceFeedbackSupport.disableWheelFX;
			Print($"additionalWheelFX now : {!WheelForceFeedbackSupport.disableWheelFX}");
		});
		SetVar("sv_crazyNetworkTracks", "0", Flags.Debug);
		SetVar("sv_crazyNetworkCars", "0", Flags.Debug);
		SetVar("sv_crazyNetworkCarVisual", "0", Flags.Debug);
		SetVar("sv_crazyNetworkChallenges", "0", Flags.Debug);
		SetVar("sv_crazyNetworkCarVinyls", "0", Flags.Debug);
		SetVar("sv_crazyNetworkNoVinyls", "0", Flags.Debug);
		SetVar("#fake_steam", "0", Flags.Debug);
		SetCmd("invite", delegate
		{
			NetworkController.SaveInvitation(cmdArgs[1]);
		}, Flags.Debug);
		SetCmd("GiveAll", GiveAll, Flags.Debug);
		SetCmd("GiveLevel", GiveLevel, Flags.Debug);
		SetCmd("LostLevel", LostLevel, Flags.Debug);
		SetCmd("GiveMoney", GiveMoney, Flags.Debug);
		SetCmd("GivePartSwapById", GivePartSwapById, Flags.Debug);
		SetCmd("TakeAll", TakeAll, Flags.Debug);
		SetCmd("out_tires", OutTires);
		SetCmd("out_engine", OutEngine);
		SetCmd("Clear", Clear);
		SetCmd("ClearVar", ClearVar);
		SetCmd("ClearCars", ClearCars);
		SetCmd("CmdList", CmdList);
		SetCmd("Exec", Execute);
		SetCmd("Quit", Application.Quit);
		SetCmd("ConnectionStats", delegate
		{
			GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
			foreach (string key in gamePrefs.stats.serversStats.Keys)
			{
				GameLog.Print(AlertLevel.Notify, "Debug", key + ": " + gamePrefs.stats.serversStats[key]);
			}
		});
		SetVar("r_maxfps", "0");
		SetCmd("r_maxfps", delegate
		{
			if (cmdArgs.Length == 2)
			{
				int result = 0;
				if (int.TryParse(cmdArgs[1], out result))
				{
					Application.targetFrameRate = result;
				}
			}
		});
		SetCmd("deltaV", delegate
		{
			if (cmdArgs.Length == 2 && int.TryParse(cmdArgs[1], out var result))
			{
				if (result != 0 && result != 1)
				{
					Debug.Log("input error! please set 1 or 0");
				}
				else
				{
					DriftControllerXDS.enableShowdeltaV = result == 1;
				}
			}
		});
		SetCmd("slowzone", delegate
		{
			TrackBuilder.debugSlowZones = !TrackBuilder.debugSlowZones;
			Debug.Log("slowzone : " + TrackBuilder.debugSlowZones);
		});
		ScriptableSingleton<ControlsSetup>.instance.ResetOverrides();
		RegisterCarXParamFloat(ParamFloat.SteerStab_w0, "множитель для предсказания инпута, множитель угловой скорости при фиксированном угле кузова, рулит проще если в районе 1");
		RegisterCarXParamFloat(ParamFloat.SteerStab_w1, "множитель для предсказания инпута, желаемая угловая скорость кузова при максимальном повороте руля");
		RegisterCarXParamFloat(ParamFloat.SteerStabK_range, "угол в градусах, на который можно довернуть колеса дополнительно к границам угла колес, которые ограничивает помощник");
		RegisterCarXParamInt(ParamInt.SteerVersion, "0 - старая версия, 1 - новая на старой, 2 - новая с параметрами");
		RegisterCarXParamInt(ParamInt.SteerSharp, "0 - сглаженное управление, 1 - резкое");
		SetCmd("crx_clear", delegate
		{
			ScriptableSingleton<ControlsSetup>.instance.ResetOverrides();
			Print("All crx* overrides cleared");
		});
		SetCmd("resetmap", delegate
		{
			BaseRaceState baseRaceState = States.current as BaseRaceState;
			if (baseRaceState != null)
			{
				baseRaceState.ResetMap();
			}
		});
		SetVar("cl_changeAssist_wheel", "0", Flags.Debug);
		SetCmd("cl_changeAssist_wheel", delegate
		{
			GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
			GlobalModel.SteeringAssist steerAssistFromArg = GetSteerAssistFromArg("cl_changeAssist_wheel");
			gamePrefs.globals.steeringAssistentWheel = steerAssistFromArg;
			gamePrefs.globals.Save();
			GameLog.Print(AlertLevel.Notify, "System", $"Wheel assist changed to {gamePrefs.globals.steeringAssistentWheel}");
		}, Flags.Debug);
		SetVar("cl_changeAssist_gamepad", "2", Flags.Debug);
		SetCmd("cl_changeAssist_gamepad", delegate
		{
			GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
			GlobalModel.SteeringAssist steerAssistFromArg = GetSteerAssistFromArg("cl_changeAssist_gamepad");
			gamePrefs.globals.steeringAssistentGamepad = steerAssistFromArg;
			gamePrefs.globals.Save();
			GameLog.Print(AlertLevel.Notify, "System", $"Gamepad assist changed to {gamePrefs.globals.steeringAssistentGamepad}");
		}, Flags.Debug);
		SetCmd("ResetInfoVersion", delegate
		{
			DependencyInjector.Resolve<GamePrefs>().globals.newsInfoVersion = 0;
		}, Flags.Debug);
		GraphyManager.maxPresetsLimit = 1;
		SetCmd("Graphy", ToggleGraphy);
		SetVar("GraphyMode", "0");
		SetCmd("GraphyMode", delegate
		{
			G_Singleton<GraphyManager>.Instance.GraphyMode = (GraphyManager.Mode)Int["GraphyMode"];
		});
		SetCmd("sv_splineRespawn", delegate
		{
			GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
			if (cmdArgs.Length > 1)
			{
				int result = 0;
				string text = cmdArgs[1];
				if (!int.TryParse(text, out result))
				{
					Print("Invalid argument: " + text);
					return;
				}
				gamePrefs.globals.respawnCarOnlyOnSpline = result > 0;
				gamePrefs.globals.Save();
			}
			Print($"sv_splineRespawn is set to {gamePrefs.globals.respawnCarOnlyOnSpline}");
		});
		SetVar("SetInterpolate", "1", Flags.Hidden);
		SetCmd("SetInterpolate", delegate
		{
			PlayerCarControl.instance.car.carX.interpolation = Bool["SetInterpolate"];
			if (Bool["SetInterpolate"])
			{
				TickManager.ActivateInterpolateMode();
			}
			else
			{
				TickManager.ActivateWithoutInterpolateMode();
			}
			GUICommonGodVoice.ShowText((PlayerCarControl.instance.car.carX.interpolation && TickManager.isInterpolate) ? "Включён интерполятор" : "Интерполятор выключен", 3f);
		}, Flags.Hidden);
		SetCmd("carDebugInfo", delegate
		{
			RaceCar.carDebugInfo = !RaceCar.carDebugInfo;
			Print($"carDebugInfo enabled: [{RaceCar.carDebugInfo}]");
		});
		SetCmd("CheckModdableBranchPlayers", CheckModdableBranchPlayers);
		GameEvents.OnGarageEnter += (GameEventHandler<GameEvents.EventInfo>)delegate
		{
			SetCollisions(value: false);
		};
	}

	private static void CheckModdableBranchPlayers()
	{
		if (cmdArgs.Length > 1)
		{
			Print("Do not use parameters");
			return;
		}
		NetworkGame instanceGame = NetworkController.InstanceGame;
		if (instanceGame == null || instanceGame.CurrentRoom == null)
		{
			Print("You are not in the network room");
			return;
		}
		List<NetworkPlayer> players = instanceGame.Players;
		if (players == null || players.Count == 0)
		{
			Print("No players found");
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < players.Count; i++)
		{
			SmartfoxPlayerProperties smartfoxPlayerProperties = players[i]?.PlayerProperties;
			if (smartfoxPlayerProperties != null && !smartfoxPlayerProperties.isDefaultBranchUser)
			{
				stringBuilder.Append(players[i].OriginaNickName + ", ");
			}
		}
		string text = stringBuilder.ToString();
		if (string.IsNullOrEmpty(text))
		{
			Print("No users found");
			return;
		}
		text = text.Trim();
		Print(text.Remove(text.Length - 1));
	}

	private static void ToggleGraphy()
	{
		int num = 0;
		if (cmdArgs.Length > 1)
		{
			if (!int.TryParse(cmdArgs[1], out var result))
			{
				Print("Error > wrong argumnent, need integer");
				return;
			}
			num = Mathf.Clamp(result, 0, 8);
		}
		DependencyInjector.Resolve<GamePrefs>().globals.showFPSSetting = num > 0;
		GraphyManager instance = G_Singleton<GraphyManager>.Instance;
		if ((bool)instance)
		{
			instance.GraphModulePosition = GraphyManager.ModulePosition.TOP_RIGHT;
			instance.SetSize(GraphyManager.ModuleSize.NORMAL);
			instance.SetPreset(num);
			Print((num == 0) ? "Graphy disabled, to enable it use an int argument from 1 to 8" : $"Graphy enabled with state {num}");
		}
	}

	private static GlobalModel.SteeringAssist GetSteerAssistFromArg(string command)
	{
		int value = Int[command];
		value = Mathf.Clamp(value, 0, 2);
		switch (value)
		{
		case 1:
			value = 2;
			break;
		case 2:
			value = 1;
			break;
		}
		return (GlobalModel.SteeringAssist)value;
	}

	public static void UpdateDInputFeedbackSettings()
	{
		GameEvents.OnGameEngineStart.Subscribe(GameEventFlag.OneCall, delegate
		{
			DependencyInjector.Resolve<WheelForceFeedbackSupport>().OnUpdateDInputFeedbackSettings();
		});
	}

	public static void Clear()
	{
		m_displayGeneration++;
		m_display.Clear();
		m_displayLineCounter.Clear();
	}

	private static void FinishRace()
	{
		BaseRaceState baseRaceState = States.current as BaseRaceState;
		if (baseRaceState != null)
		{
			baseRaceState.ForceFinishRace();
		}
	}

	private static void Execute()
	{
		if (cmdArgs.Length == 2)
		{
			LaunchFrom(cmdArgs[1]);
		}
	}

	public static void UpdateFOV()
	{
		DependencyInjector.Resolve<GamePrefs>().globals.Apply();
	}

	public static void SetCockpitCameraMode()
	{
		GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
		int result;
		if (cmdArgs.Length < 2)
		{
			int cockpitCamMode = (int)gamePrefs.globals.dynamicCamParams.cockpitCamMode;
			Print($"{cmdArgs[0]} is set to {cockpitCamMode}");
		}
		else if (int.TryParse(cmdArgs[1], out result) && result >= 0 && result <= 1)
		{
			gamePrefs.globals.dynamicCamParams.cockpitCamMode = (GlobalModel.DynamicCockpitCamMode)result;
		}
		else
		{
			Print("Invalid argument. Should be 0 - default, 1 - new mode");
		}
	}

	public static void SetCockpitCameraSpeed()
	{
		GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
		int result;
		if (cmdArgs.Length < 2)
		{
			float maxRotSpeed = gamePrefs.globals.dynamicCamParams.maxRotSpeed;
			Print($"{cmdArgs[0]} is set to {maxRotSpeed}");
		}
		else if (int.TryParse(cmdArgs[1], out result) && result >= 0 && result <= 360)
		{
			gamePrefs.globals.dynamicCamParams.maxRotSpeed = result;
		}
		else
		{
			Print("Invalid argument. Should be in range from 0 to 360 (deg/sec)");
		}
	}

	public static void SetCockpitCameraSmooth()
	{
		GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
		float result;
		if (cmdArgs.Length < 2)
		{
			float cameraSmooth = gamePrefs.globals.dynamicCamParams.cameraSmooth;
			Print($"{cmdArgs[0]} is set to {cameraSmooth}");
		}
		else if (float.TryParse(cmdArgs[1], out result) && result >= 0f)
		{
			gamePrefs.globals.dynamicCamParams.cameraSmooth = result;
		}
		else
		{
			Print("Invalid argument. Should be greater or equal to 0");
		}
	}

	public static void SetCockpitCameraMaxAngle()
	{
		GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
		float result;
		if (cmdArgs.Length < 2)
		{
			float maxRotAngle = gamePrefs.globals.dynamicCamParams.maxRotAngle;
			Print($"{cmdArgs[0]} is set to {maxRotAngle}");
		}
		else if (float.TryParse(cmdArgs[1], out result) && result >= 0f && result <= 90f)
		{
			gamePrefs.globals.dynamicCamParams.maxRotAngle = result;
		}
		else
		{
			Print("Invalid argument. Should be in range from 0 to 90 (degrees)");
		}
	}

	public static void SetCockpitCameraVector()
	{
		GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
		float result;
		if (cmdArgs.Length < 2)
		{
			float velocityScale = gamePrefs.globals.dynamicCamParams.velocityScale;
			Print($"{cmdArgs[0]} is set to {velocityScale}");
		}
		else if (float.TryParse(cmdArgs[1], out result) && result >= 0f && result <= 1f)
		{
			gamePrefs.globals.dynamicCamParams.velocityScale = result;
		}
		else
		{
			Print("Invalid argument. Should be in range 0-1");
		}
	}

	public static void SetCockpitCameraUseVel()
	{
		GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
		int result;
		if (cmdArgs.Length < 2)
		{
			int num = ((gamePrefs.globals.dynamicCamParams.camAimMode == DynamicCamParameters.AimMode.Velocity) ? 1 : 0);
			Print($"{cmdArgs[0]} is set to {num}");
		}
		else if (int.TryParse(cmdArgs[1], out result) && (result == 1 || result == 0))
		{
			gamePrefs.globals.dynamicCamParams.camAimMode = (DynamicCamParameters.AimMode)result;
		}
		else
		{
			Print("Invalid argument. Should be 1 or 0");
		}
	}

	public static void SetCockpitCameraUseAccel()
	{
		GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
		int result;
		if (cmdArgs.Length < 2)
		{
			int num = (gamePrefs.globals.dynamicCamParams.moveWithAccel ? 1 : 0);
			Print($"{cmdArgs[0]} is set to {num}");
		}
		else if (int.TryParse(cmdArgs[1], out result) && (result == 1 || result == 0))
		{
			gamePrefs.globals.dynamicCamParams.moveWithAccel = result == 1;
		}
		else
		{
			Print("Invalid argument. Should be 1 or 0");
		}
	}

	public static void SetCockpitCameraAccelSmooth()
	{
		GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
		float result;
		if (cmdArgs.Length < 2)
		{
			float accelSmooth = gamePrefs.globals.dynamicCamParams.accelSmooth;
			Print($"{cmdArgs[0]} is set to {accelSmooth}");
		}
		else if (float.TryParse(cmdArgs[1], out result) && result >= 0f)
		{
			gamePrefs.globals.dynamicCamParams.accelSmooth = result;
		}
		else
		{
			Print("Invalid argument. Should be greater or equal to 0");
		}
	}

	public static void SetCockpitCamDeadzone()
	{
		GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
		float result;
		if (cmdArgs.Length < 2)
		{
			float dynamicCamDeadZone = gamePrefs.globals.dynamicCamParams.dynamicCamDeadZone;
			Print($"{cmdArgs[0]} is set to {dynamicCamDeadZone}");
		}
		else if (float.TryParse(cmdArgs[1], out result) && result >= 0f && result <= 1f)
		{
			gamePrefs.globals.dynamicCamParams.dynamicCamDeadZone = result;
		}
		else
		{
			Print("Invalid argument. Should be in range [0; 1]");
		}
	}

	public static void UpdatePoints()
	{
		UIDriftEventsContext.silenceByPointsOptions = !Bool["r_points"];
	}

	public static void UpdateDriverVisibility()
	{
		if ((bool)PlayerCarControl.instance)
		{
			RaceCar car = PlayerCarControl.instance.car;
			if ((bool)car)
			{
				car.UpdateDriverVisibility();
			}
		}
	}

	public static void SwitchWheelShowing()
	{
		GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
		gamePrefs.globals.hideWheel = !gamePrefs.globals.hideWheel;
		gamePrefs.globals.Apply();
		UpdateDriverVisibility();
	}

	public static void SwitchDriverShowing()
	{
		GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
		gamePrefs.globals.hideDriver = !gamePrefs.globals.hideDriver;
		gamePrefs.globals.Apply();
		UpdateDriverVisibility();
	}

	public static void SetCollisions(bool value)
	{
		int num = LayerMask.NameToLayer("Car");
		int num2 = LayerMask.NameToLayer("LocalCar");
		bool ignore = !value;
		Physics.IgnoreLayerCollision(num, num, ignore);
		Physics.IgnoreLayerCollision(num, num2, ignore);
		Physics.IgnoreLayerCollision(num2, num2, ignore);
	}

	public static void GiveAll()
	{
		GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
		gamePrefs.playerProfile.exp = 999999;
		gamePrefs.money.coins = 99999999;
		gamePrefs.locks.UnlockAll();
	}

	public static void GivePartSwapById()
	{
		GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
		List<EngineInfo> list = SwapsAsset.LoadAndInitDB().swaps as List<EngineInfo>;
		if (cmdArgs.Length > 1 && int.TryParse(cmdArgs[1], out var result))
		{
			string partItem = list[result].partItem;
			int num = 1;
			if (cmdArgs.Length > 2 && int.TryParse(cmdArgs[2], out var result2))
			{
				num = result2;
			}
			gamePrefs.inventory.AddItem(partItem, num, permanentItem: true);
			int count = gamePrefs.inventory.GetItem(partItem).count;
			Debug.Log($"Add {num} {partItem}. New count {count}");
		}
	}

	public static void GiveMoney()
	{
		int coins = 99999999;
		GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
		if (cmdArgs.Length == 2 && int.TryParse(cmdArgs[1], out var result))
		{
			coins = result;
		}
		if (gamePrefs.money.coins != 99999999)
		{
			gamePrefs.money.coins = coins;
		}
		else
		{
			gamePrefs.money.coins = 0;
		}
	}

	public static void GiveLevel()
	{
		GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
		gamePrefs.playerProfile.exp = gamePrefs.playerProfile.nextLevelExp;
		if (!(States.mainCurrent is BaseRaceModeState))
		{
			gamePrefs.playerProfile.UnlockNewLevelFeatures();
		}
	}

	public static void LostLevel()
	{
		GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
		gamePrefs.playerProfile.exp = gamePrefs.playerProfile.prevLevelExp - 1;
		if (!(States.mainCurrent is BaseRaceModeState))
		{
			gamePrefs.playerProfile.UnlockNewLevelFeatures();
		}
	}

	public static void OutEngine()
	{
		NetworkGame instanceGame = NetworkController.InstanceGame;
		if (instanceGame == null)
		{
			Print("Network not initalized!");
			return;
		}
		if (cmdArgs.Length == 1)
		{
			string text = "";
			foreach (NetworkPlayer player in instanceGame.Players)
			{
				text = text + player.FilteredNickName + "(" + player.NetworkID + ") ";
			}
			Print(text);
		}
		if (cmdArgs.Length != 2)
		{
			return;
		}
		BaseModel baseModel = DependencyInjector.Resolve<BaseModel>();
		if (!int.TryParse(cmdArgs[1], out var result))
		{
			result = -1;
		}
		foreach (NetworkPlayer player2 in instanceGame.Players)
		{
			if (player2.NetworkID != result)
			{
				continue;
			}
			int carid = player2.PlayerProperties.CarId;
			ProfilesInfo.Profile profile = player2.PlayerProperties.Profile;
			int profileID = player2.PlayerProperties.Profile.id;
			PlayerCarProfile playerCarProfile = baseModel.QueryRecordsByColumnValue("carId", carid, (PlayerCarProfile x) => x.carId == carid).Find((PlayerCarProfile x) => x.id == profileID);
			Print(player2.FilteredNickName + " => " + playerCarProfile.name);
			foreach (CarProfileController controller in playerCarProfile.dynoInfo.controllers)
			{
				bool flag = true;
				if (!profile.controllers.TryGetValue(controller.name, out var value))
				{
					if (controller.changers.Count == 1)
					{
						CarProfileChanger carProfileChanger = controller.changers[0];
						value = carProfileChanger.EvaluatePhase(carProfileChanger.defaultValue);
					}
					else
					{
						flag = false;
					}
				}
				if (flag && controller.changers.Count != 0)
				{
					CarProfileChanger carProfileChanger2 = controller.changers[0];
					CarProfileParameterType type = baseModel.GetById<CarProfileParameterMeta>(carProfileChanger2.parameterId).type;
					if ((uint)(type - 28) <= 8u || (uint)(type - 57) <= 3u)
					{
						float num = carProfileChanger2.EvaluateOriginalValue(value);
						Print(controller.name + " : " + num.ToString("0.00"));
					}
				}
			}
			Print("-----------------------------------");
		}
	}

	public static void OutTires()
	{
		NetworkGame instanceGame = NetworkController.InstanceGame;
		if (instanceGame == null)
		{
			Print("Network not initalized!");
			return;
		}
		if (cmdArgs.Length == 1)
		{
			string text = "";
			foreach (NetworkPlayer player in instanceGame.Players)
			{
				text = text + player.FilteredNickName + "(" + player.NetworkID + ") ";
			}
			Print(text);
		}
		if (cmdArgs.Length != 2)
		{
			return;
		}
		BaseModel baseModel = DependencyInjector.Resolve<BaseModel>();
		if (!int.TryParse(cmdArgs[1], out var result))
		{
			result = -1;
		}
		foreach (NetworkPlayer player2 in instanceGame.Players)
		{
			if (player2.NetworkID != result)
			{
				continue;
			}
			Print(player2.FilteredNickName + " =>");
			int carid = player2.PlayerProperties.CarId;
			ProfilesInfo.Profile profile = player2.PlayerProperties.Profile;
			int profileID = player2.PlayerProperties.Profile.id;
			foreach (CarProfileController controller in baseModel.QueryRecordsByColumnValue("carId", carid, (PlayerCarProfile x) => x.carId == carid).Find((PlayerCarProfile x) => x.id == profileID).dynoInfo.controllers)
			{
				bool flag = true;
				if (!profile.controllers.TryGetValue(controller.name, out var value))
				{
					if (controller.changers.Count == 1)
					{
						CarProfileChanger carProfileChanger = controller.changers[0];
						value = carProfileChanger.EvaluatePhase(carProfileChanger.defaultValue);
					}
					else
					{
						flag = false;
					}
				}
				if (flag && controller.changers.Count != 0)
				{
					CarProfileChanger carProfileChanger2 = controller.changers[0];
					switch (baseModel.GetById<CarProfileParameterMeta>(carProfileChanger2.parameterId).type)
					{
					case CarProfileParameterType.FrontDiscDiam:
					case CarProfileParameterType.FrontTyreWidth:
					case CarProfileParameterType.RearDiscDiam:
					case CarProfileParameterType.RearTyreWidth:
					case CarProfileParameterType.FrontTireType:
					case CarProfileParameterType.RearTireType:
					case CarProfileParameterType.FrontTyreProfile:
					case CarProfileParameterType.RearTyreProfile:
					{
						float num = carProfileChanger2.EvaluateOriginalValue(value);
						Print(controller.name + " : " + num.ToString("0.00"));
						break;
					}
					}
				}
			}
			Print("-----------------------------------");
		}
	}

	public static void TakeAll()
	{
		if (!m_clearPrefsBtnEnabled)
		{
			Print("Clear in progress... please wait!");
		}
		else
		{
			KeepAliveManager.instance.StartCoroutine(ClearPrefs());
		}
	}

	private static IEnumerator ClearPrefs()
	{
		m_clearPrefsBtnEnabled = false;
		yield return GameSync.Clear();
		m_clearPrefsBtnEnabled = true;
		Print("Clear complete!");
	}

	public static void ClearVar()
	{
		if (cmdArgs.Length == 2)
		{
			Var var = GetVar(cmdArgs[1]);
			if (var != null)
			{
				SetVar(cmdArgs[1], var.defaultValue);
				Print("Var " + cmdArgs[1] + " is set to " + var.defaultValue);
			}
		}
	}

	public static void ClearCars()
	{
		CarPartExternalLoader.CleanUp();
	}

	private static void ShowVinylsCount()
	{
		if (!IsActiveContextOnCurrState<UIVinylsDecalSelectionContext, GarageGUIState>(out var context))
		{
			Print("This command can be applied only in decal selection context!");
		}
		else
		{
			TimeController.StartNow(context.ShowCategorysVinylsCount());
		}
	}

	private static bool IsActiveContextOnCurrState<T, K>(out T context) where T : BaseContext where K : BaseGameState
	{
		context = null;
		Type typeFromHandle = typeof(K);
		BaseState current = States.current;
		if (typeFromHandle != current.GetType() && typeFromHandle != States.mainCurrent.GetType())
		{
			return false;
		}
		if (current is BaseGameState { contextOwner: { } contextOwner })
		{
			context = contextOwner.Get<T>();
			if (context != null)
			{
				return context.isActive;
			}
			return false;
		}
		return false;
	}
}
