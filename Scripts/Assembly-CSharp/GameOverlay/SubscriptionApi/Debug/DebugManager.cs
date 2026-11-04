using System.Collections.Generic;
using GameOverlay.SubscriptionApi.Debug.Log;

namespace GameOverlay.SubscriptionApi.Debug
{
	public class DebugManager
	{
		public enum CommandType
		{
			Log = 0,
			LifetimeModifier = 1
		}

		private Dictionary<CommandType, IDebugCommand> m_commands = new Dictionary<CommandType, IDebugCommand>();

		public void Execute(CommandType type, string[] args)
		{
			if (!m_commands.TryGetValue(type, out var value))
			{
				value = CreateCommand(type);
				m_commands[type] = value;
			}
			value.Execute(args);
		}

		public IDebugCommand CreateCommand(CommandType type)
		{
			return type switch
			{
				CommandType.Log => new LogService(), 
				CommandType.LifetimeModifier => new LifetimeModifier(), 
				_ => null, 
			};
		}
	}
}
