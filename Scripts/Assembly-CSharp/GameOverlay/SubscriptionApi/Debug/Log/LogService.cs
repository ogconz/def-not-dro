using System;
using System.Collections.Generic;
using GameOverlay.SubscriptionApi.Debug.Notifier;

namespace GameOverlay.SubscriptionApi.Debug.Log
{
	public class LogService : IDebugCommand
	{
		private List<Logger> m_loggers = new List<Logger>();

		public LogService()
		{
			m_loggers.Add(new LifetimeLogger(new GodvoiceNotifier()));
		}

		public void Execute(string[] args)
		{
			if (args.Length != 2)
			{
				GameLog.Print(AlertLevel.Error, "SubscriptionOwnership", "Incorrect args count for execute!");
				return;
			}
			try
			{
				bool active = Convert.ToBoolean(int.Parse(args[1]));
				foreach (Logger logger in m_loggers)
				{
					logger.SetActive(active);
				}
			}
			catch (Exception ex)
			{
				GameLog.Print(AlertLevel.Error, "SubscriptionOwnership", "Exception while execute command -> " + ex.Message);
			}
		}
	}
}
