using System;

namespace SyncMultiplayer.NetGameSystems.Smartfox.PropertiesUpdaters.Dispatchers.Settings.Debug
{
	public class DebugUserPropsScheduleSettings : UserPropsScheduleSettings
	{
		public Action OnCommandApplied;

		public DebugUserPropsScheduleSettings()
		{
			InitCommands();
		}

		private void InitCommands()
		{
			GameConsole.SetCmd("setdelayprops", ApplyDelay);
		}

		private void ApplyDelay()
		{
			if (GameConsole.cmdArgs.Length >= 2 && int.TryParse(GameConsole.cmdArgs[1], out var result))
			{
				base.delay = result;
				OnCommandApplied?.SafeCall();
				GUICommonGodVoice.ShowText($"Applied delay user properties update within {result}ms");
			}
		}
	}
}
