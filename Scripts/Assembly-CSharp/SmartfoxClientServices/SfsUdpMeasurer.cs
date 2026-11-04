using System;
using System.Diagnostics;
using System.Timers;
using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Entities.Data;
using Sfs2X.Requests;

namespace SmartfoxClientServices
{
	public class SfsUdpMeasurer
	{
		public Action<int> onPingResult;

		private const int InvalidPingValue = -1;

		private const string PingCommand = "p";

		private const string UdpPingParam = "u";

		private const int DefaultTimeoutMS = 5000;

		private SmartFox m_sfs;

		private int m_timeoutPingReceiving;

		private Stopwatch m_udpPingStopwatch = new Stopwatch();

		private Timer m_timeoutTimer;

		private bool m_isMeasurementInProgress;

		private bool m_isMeasurementAvaliable = true;

		public SfsUdpMeasurer(SmartFox sfsClient, Action<int> onPingReceivedCallback, int timeoutMs = 5000)
		{
			SetActive(active: true);
			m_sfs = sfsClient;
			m_timeoutPingReceiving = timeoutMs;
			onPingResult = (Action<int>)Delegate.Combine(onPingResult, onPingReceivedCallback);
			m_sfs.AddEventListener(SFSEvent.EXTENSION_RESPONSE, OnExtensionResponse);
		}

		public void SetActive(bool active)
		{
			m_isMeasurementAvaliable = active;
		}

		public void MeasurePing()
		{
			if (m_isMeasurementAvaliable && !m_isMeasurementInProgress)
			{
				if (!m_sfs.IsConnected)
				{
					onPingResult.SafeCall(-1);
					return;
				}
				m_isMeasurementInProgress = true;
				CreatePingRequest();
				SetupTimeoutTimer();
			}
		}

		private void CreatePingRequest()
		{
			ISFSObject iSFSObject = new SFSObject();
			iSFSObject.PutBool("u", val: true);
			m_udpPingStopwatch = Stopwatch.StartNew();
			m_sfs.Send(new ExtensionRequest("p", iSFSObject));
		}

		private void OnExtensionResponse(BaseEvent evt)
		{
			if ((string)evt.Params["cmd"] == "p" && m_isMeasurementInProgress)
			{
				CompleteMeasurement((int)m_udpPingStopwatch.ElapsedMilliseconds);
			}
		}

		private void SetupTimeoutTimer()
		{
			m_timeoutTimer = new Timer(m_timeoutPingReceiving);
			m_timeoutTimer.Elapsed += OnTimeoutElapsed;
			m_timeoutTimer.AutoReset = true;
			m_timeoutTimer.Start();
		}

		private void OnTimeoutElapsed(object sender, ElapsedEventArgs e)
		{
			if (!m_isMeasurementInProgress)
			{
				CompleteMeasurement(-1);
			}
		}

		private void CompleteMeasurement(int pingMs)
		{
			m_isMeasurementInProgress = false;
			m_udpPingStopwatch?.Stop();
			StopTimeoutTimer();
			onPingResult.SafeCall(pingMs);
		}

		private void StopTimeoutTimer()
		{
			m_timeoutTimer?.Stop();
			m_timeoutTimer?.Dispose();
			m_timeoutTimer = null;
		}
	}
}
