using Binding;
using DB.Meta;
using UnityEngine;

namespace HUD
{
	public class UIRaceDriftModeContext : UIRaceModeContext
	{
		[SerializeField]
		public UICapsResultContext m_capsResult;

		private DriftRaceModeState m_raceModeState;

		private StringProperty m_record = new StringProperty();

		private IRaceNorm m_norms;

		protected override void OnActivate()
		{
			base.OnActivate();
			m_raceModeState = States.current as DriftRaceModeState;
			int trackId = m_gamePrefs.globals.lastTrackId;
			m_norms = m_model.Get((TrackDriftRaceNormMeta x) => x.trackId == trackId);
			m_record.value = UIUtils.MakeViewBestDriftPoints(trackId);
			if ((bool)m_capsResult && m_norms != null)
			{
				m_capsResult.SetInfo(m_norms, 0);
			}
			base.raceDpAction.IsAllowWrongWay = true;
		}

		protected override void Update()
		{
			base.Update();
			UpdateWrongWay();
			SetTimer(m_raceModeState.timeLeft);
			SetDriftPoints(m_raceModeState.currentDriftPoints);
			RefreshDriftPointAction(m_raceModeState.currentDriftPoints);
			if ((bool)m_capsResult && m_norms != null)
			{
				int cupByValue = (int)m_norms.GetCupByValue(m_raceModeState.currentDriftPoints);
				m_capsResult.cupsLevel.value = cupByValue;
			}
		}
	}
}
