using GameOverlay;
using Sentry;
using Sentry.Integrations;

namespace Services.GameAnalytics.Sentry.Integrations
{
	public class ClientInfoIntegration : ISdkIntegration
	{
		public void Register(IHub hub, SentryOptions options)
		{
			options.Release = GameVersion.versionAsString;
			hub.ConfigureScope(delegate(Scope scope)
			{
				scope.User.Id = Overlay.playerId.accountId.ToString();
			});
		}
	}
}
