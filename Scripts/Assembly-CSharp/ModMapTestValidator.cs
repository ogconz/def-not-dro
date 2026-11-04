using System;
using System.Collections.Generic;

public static class ModMapTestValidator
{
	private static Dictionary<string, string> m_validateExternalTracks = new Dictionary<string, string>();

	public static void Subscribe()
	{
		ModMapTestTool.errorCallback = (Action<string, string>)Delegate.Combine(ModMapTestTool.errorCallback, new Action<string, string>(OnModMapTestToolOnErrorEvent));
		ModMapTestTool.playCallback = (Func<string, bool>)Delegate.Combine(ModMapTestTool.playCallback, new Func<string, bool>(OnModMapTestToolOnPlayEvent));
	}

	public static void UnSubscribe()
	{
		ModMapTestTool.errorCallback = (Action<string, string>)Delegate.Remove(ModMapTestTool.errorCallback, new Action<string, string>(OnModMapTestToolOnErrorEvent));
		ModMapTestTool.playCallback = (Func<string, bool>)Delegate.Remove(ModMapTestTool.playCallback, new Func<string, bool>(OnModMapTestToolOnPlayEvent));
	}

	private static bool OnModMapTestToolOnPlayEvent(string mapName)
	{
		if (m_validateExternalTracks.TryGetValue(mapName, out var value))
		{
			OnModMapTestToolOnErrorEvent(mapName, value);
			return false;
		}
		return true;
	}

	private static void OnModMapTestToolOnErrorEvent(string mapName, string message)
	{
		if (!string.IsNullOrEmpty(mapName))
		{
			m_validateExternalTracks[mapName] = message;
		}
		message = "Map " + mapName + ", Error Validate : " + message;
		LoadRaceState state = States.State<LoadRaceState>();
		LoadRaceState loadRaceState = state;
		loadRaceState.onRaceLoadedEvent = (Action)Delegate.Remove(loadRaceState.onRaceLoadedEvent, new Action(Callback));
		LoadRaceState loadRaceState2 = state;
		loadRaceState2.onRaceLoadedEvent = (Action)Delegate.Combine(loadRaceState2.onRaceLoadedEvent, new Action(Callback));
		void Callback()
		{
			GarageGUIState.ExitToGarage();
			GameLog.Print(AlertLevel.Notify, "ModMaps", message);
			GUICommonGodVoice.ShowText(message);
			LoadRaceState loadRaceState3 = state;
			loadRaceState3.onRaceLoadedEvent = (Action)Delegate.Remove(loadRaceState3.onRaceLoadedEvent, new Action(Callback));
		}
	}
}
