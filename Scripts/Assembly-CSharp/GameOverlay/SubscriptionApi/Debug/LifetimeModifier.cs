using System;

namespace GameOverlay.SubscriptionApi.Debug
{
	public class LifetimeModifier : IDebugCommand
	{
		public void Execute(string[] args)
		{
			if (args.Length != 2)
			{
				GameLog.Print(AlertLevel.Error, "SubscriptionOwnership", "Incorrect args count for execute!");
				return;
			}
			try
			{
				if (float.TryParse(args[1], out var result))
				{
					if (2f > result)
					{
						result = 3f;
						GameLog.Print(AlertLevel.Error, "SubscriptionOwnership", $"Delay less then cooldown! Force applied delay within {result} sec");
					}
					SubscriptionOwnershipService.configManager.updateScheduler.lifetimeHandler.SetLifetime(result);
				}
			}
			catch (Exception ex)
			{
				GameLog.Print(AlertLevel.Error, "SubscriptionOwnership", "Exception while execute command -> " + ex.Message);
			}
		}
	}
}
