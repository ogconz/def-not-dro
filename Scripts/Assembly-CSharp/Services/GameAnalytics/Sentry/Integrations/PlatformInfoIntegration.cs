using GameOverlay;
using Sentry;
using Sentry.Integrations;

namespace Services.GameAnalytics.Sentry.Integrations
{
	public class PlatformInfoIntegration : ISdkIntegration
	{
		public void Register(IHub hub, SentryOptions options)
		{
			hub.ConfigureScope(delegate(Scope scope)
			{
				UserPlatform.Id platform = Overlay.GetPlatform();
				scope.SetTag("platform.name", platform.ToString());
				int num = (int)platform;
				scope.SetTag("platform.id", num.ToString());
			});
		}
	}
}
