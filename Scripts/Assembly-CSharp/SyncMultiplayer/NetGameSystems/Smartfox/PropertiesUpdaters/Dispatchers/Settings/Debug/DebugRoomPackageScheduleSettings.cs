using System;

namespace SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Dispatchers.Settings.Debug
{
	public class DebugRoomPackageScheduleSettings : RoomPackageThreadScheduleSettings
	{
		public Action<PacketId> OnCommandApplied;

		public DebugRoomPackageScheduleSettings()
		{
			InitCommands();
		}

		private void InitCommands()
		{
			GameConsole.SetCmd("setdelay", TryApplyDelay);
		}

		private void TryApplyDelay()
		{
			if (GameConsole.cmdArgs.Length >= 3 && Enum.TryParse<PacketId>(GameConsole.cmdArgs[1], ignoreCase: true, out var result) && int.TryParse(GameConsole.cmdArgs[2], out var result2))
			{
				m_delayPerPacket[result] = result2;
				OnCommandApplied?.SafeCall(result);
				GUICommonGodVoice.ShowText($"Applied delay on {result} within {result2}ms");
			}
		}
	}
}
