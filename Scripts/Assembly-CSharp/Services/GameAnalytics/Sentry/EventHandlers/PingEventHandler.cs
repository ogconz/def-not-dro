using System;
using System.Collections.Generic;
using GameOverlay;
using Newtonsoft.Json;

namespace Services.GameAnalytics.Sentry.EventHandlers
{
	public class PingEventHandler : IEventHandler
	{
		private const string RequestType = "transaction";

		private const string IpSentrySpecInstruction = "{{auto}}";

		private const int MSecInSec = 1000;

		private SentryRawRequestWorker m_requestWorker;

		private string m_userId;

		public PingEventHandler(SentryRawRequestWorker requestWorker, string userId)
		{
			m_requestWorker = requestWorker;
			m_userId = userId;
		}

		public void Process(string eventName, Dictionary<string, object> parameters)
		{
			string json = BuildJson(eventName, parameters);
			m_requestWorker.Send(json, "transaction");
		}

		private string BuildJson(string eventName, Dictionary<string, object> parameters)
		{
			float num = (long)DateTime.UtcNow.Millisecond / 1000L;
			float start_timestamp = (num - (float)(int)parameters["ping"]) / 1000f;
			return JsonConvert.SerializeObject(new
			{
				transaction = eventName,
				timestamp = num,
				start_timestamp = start_timestamp,
				contexts = new
				{
					trace = new
					{
						trace_id = Guid.NewGuid().ToString("N"),
						span_id = Guid.NewGuid().ToString("N").Substring(0, 16),
						op = "ping",
						status = "ok"
					}
				},
				tags = new
				{
					game_platform = Overlay.instance.GetPlatform().ToString().ToLower(),
					room_region = parameters["room_region"]
				},
				user = new
				{
					id = m_userId,
					ip_address = "{{auto}}"
				},
				measurements = new
				{
					ping_ms = new
					{
						value = (int)parameters["ping"],
						unit = "ms"
					}
				}
			});
		}
	}
}
