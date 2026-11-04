using System;
using System.Collections.Generic;
using UnityEngine;
using anzu;

namespace Services.Anzu
{
	[Serializable]
	public class AnzuPlacement
	{
		[SerializeField]
		private AnzuAd m_nativeAd;

		[SerializeField]
		private AnzuStats m_debugStats;

		[SerializeField]
		private AnzuMaterialBlinker m_materialBlinker;

		public bool debugStatsActivity
		{
			get
			{
				if (m_debugStats != null)
				{
					return m_debugStats.enabled;
				}
				return false;
			}
		}

		public double angle => m_nativeAd.Angle;

		public AnzuAd nativeAd => m_nativeAd;

		public AnzuChannelSO channel => nativeAd.AnzuChannel;

		public static Dictionary<AnzuChannelSO, bool> channelsPerImperssionGained { get; private set; }

		public void Initialize()
		{
			SetAdEventsSubscription(subscribe: true);
			CommitImpressionStatus(gained: false);
		}

		private void SetAdEventsSubscription(bool subscribe)
		{
			if (subscribe)
			{
				m_nativeAd.OnChannelImpression += OnImpressionReceived;
			}
			else
			{
				m_nativeAd.OnChannelImpression -= OnImpressionReceived;
			}
		}

		private void OnImpressionReceived()
		{
			if (debugStatsActivity)
			{
				BlinkMaterialOnSuccessImpression();
			}
			CommitImpressionStatus(gained: true);
		}

		public void Dispose()
		{
			if (channel != null)
			{
				channelsPerImperssionGained.Remove(channel);
			}
			SetAdEventsSubscription(subscribe: false);
			m_materialBlinker.Dispose();
			m_nativeAd = null;
			m_debugStats = null;
		}

		private void CommitImpressionStatus(bool gained)
		{
			if (channelsPerImperssionGained == null)
			{
				channelsPerImperssionGained = new Dictionary<AnzuChannelSO, bool>();
			}
			if (channel != null)
			{
				channelsPerImperssionGained[channel] = gained;
			}
		}

		public void SetActiveAdStats(bool activity)
		{
			if (!(m_debugStats == null) && debugStatsActivity != activity)
			{
				m_debugStats.enabled = activity;
			}
		}

		private void BlinkMaterialOnSuccessImpression()
		{
			m_materialBlinker.BlinkMaterial();
		}
	}
}
