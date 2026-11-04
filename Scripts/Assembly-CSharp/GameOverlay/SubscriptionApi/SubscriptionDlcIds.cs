using System.Collections.Generic;

namespace GameOverlay.SubscriptionApi
{
	public static class SubscriptionDlcIds
	{
		public static readonly Dictionary<DLCCode, string> dlcIdPerCode = new Dictionary<DLCCode, string> { 
		{
			DLCCode.SeasonPass,
			"e36bf3d4-6435-4aa2-89f1-6900434c2a0e"
		} };
	}
}
