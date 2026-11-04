using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Unity;

namespace Services.GameAnalytics.Sentry.Integrations
{
	public class HttpMessageRedirectHandler : HttpClientHandler
	{
		private SentryUnityOptions m_options;

		public HttpMessageRedirectHandler(SentryUnityOptions options)
		{
			m_options = options;
		}

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			AddIdentityParameterIfNeed(request);
			m_options.DiagnosticLogger?.LogInfo($"HTTP Request: {request.Method} {request.RequestUri}");
			HttpResponseMessage httpResponseMessage = await base.SendAsync(request, cancellationToken);
			m_options.DiagnosticLogger?.LogInfo($"HTTP Response: {(int)httpResponseMessage.StatusCode} {request.Method} {request.RequestUri}");
			return httpResponseMessage;
		}

		private void AddIdentityParameterIfNeed(HttpRequestMessage request)
		{
			string text = request.RequestUri.ToString();
			request.RequestUri = new Uri(text + "?tr_type=sdk_event");
		}
	}
}
