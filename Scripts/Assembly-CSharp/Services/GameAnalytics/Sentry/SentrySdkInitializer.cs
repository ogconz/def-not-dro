using Sentry.Unity;
using Services.GameAnalytics.Sentry.Integrations;

namespace Services.GameAnalytics.Sentry
{
	public class SentrySdkInitializer
	{
		public void Initialize(SentryConfigHandler config)
		{
			if (config.isServiceEnabled)
			{
				SentryUnityOptions sentryUnityOptions = new SentryUnityOptions();
				ApplyWebConfig(sentryUnityOptions, config);
				SetupGlobalOptions(sentryUnityOptions);
				SetupMaxAttachmentSize(sentryUnityOptions);
				RemoveExternalLogging(sentryUnityOptions);
				SetupEventsRate(sentryUnityOptions);
				SetupDebug(sentryUnityOptions);
				sentryUnityOptions.AddIntegration(new ClientInfoIntegration());
				sentryUnityOptions.AddIntegration(new PlatformInfoIntegration());
				SentrySdk.Init(sentryUnityOptions);
			}
		}

		private void ApplyWebConfig(SentryUnityOptions options, SentryConfigHandler config)
		{
			options.Enabled = config.isServiceEnabled;
			options.Dsn = config.dsn;
		}

		private void SetupGlobalOptions(SentryUnityOptions options)
		{
			options.IsGlobalModeEnabled = true;
			options.CreateHttpMessageHandler = () => new HttpMessageRedirectHandler(options);
		}

		private void SetupMaxAttachmentSize(SentryUnityOptions options)
		{
			options.MaxAttachmentSize = 8388608L;
		}

		private void RemoveExternalLogging(SentryUnityOptions options)
		{
			options.EnableLogs = false;
			options.CaptureLogErrorEvents = false;
			options.AutoSceneLoadTraces = false;
			options.AutoStartupTraces = false;
			options.AddBreadcrumbsWithStructuredLogs = false;
			options.DisableUnityLoggingIntegration();
			options.DisableUnhandledExceptionCapture();
			options.DisableAnrIntegration();
			options.DisableUnobservedTaskExceptionCapture();
			options.DisableAppDomainUnhandledExceptionCapture();
		}

		private void SetupEventsRate(SentryUnityOptions options)
		{
			options.SampleRate = 1f;
			options.ProfilesSampleRate = 1.0;
			options.TracesSampleRate = 1.0;
		}

		private void SetupDebug(SentryUnityOptions options)
		{
		}
	}
}
