using Binding;
using DB.Meta;
using UnityEngine;

namespace HUD
{
	public class UIRaceTimeModeContext : UIRaceModeContext
	{
		[SerializeField]
		public UICapsResultContext m_capsResult;

		private TimeAttackRaceModeState m_raceModeState;

		private StringProperty m_record = new StringProperty();

		private StringProperty m_time = new StringProperty();

		private StringProperty m_penalty = new StringProperty();

		private BoolProperty m_isPenaltyGrowing = new BoolProperty();

		private IRaceNorm m_norms;

		protected override void OnActivate()
		{
			base.OnActivate();
			m_raceModeState = States.current as TimeAttackRaceModeState;
			int carId = m_gamePrefs.globals.lastCarId;
			int trackId = m_gamePrefs.globals.lastTrackId;
			m_norms = m_model.Get((TrackTimeAttackRaceNormMeta x) => x.trackId == trackId && x.carId == carId);
			m_record.value = UIUtils.MakeViewBestTimeAttackPoints(trackId);
			if ((bool)m_capsResult && m_norms != null)
			{
				m_capsResult.SetInfo(m_norms, 0);
				m_capsResult.cupsLevel.value = 0;
			}
		}

		protected override void Update()
		{
			base.Update();
			UpdateWrongWay();
			SetTimer(m_raceModeState.timeLeft);
			m_time.value = UIUtils.TimeToMSMString(m_raceModeState.currentLapTime);
			if ((bool)m_capsResult && m_norms != null && m_raceModeState.currentLapTime.HasValue)
			{
				float value = m_raceModeState.currentLapTime.Value;
				int value2 = 0;
				if (m_norms.GetNormByCup(RaceCupType.Gold) < value)
				{
					value2 = 1;
				}
				if (m_norms.GetNormByCup(RaceCupType.Silver) < value)
				{
					value2 = 2;
				}
				if (m_norms.GetNormByCup(RaceCupType.Bronze) < value)
				{
					value2 = 3;
				}
				m_capsResult.cupsLevel.value = value2;
			}
			m_isPenaltyGrowing.value = m_raceModeState.isPenaltyGrowing;
			m_penalty.value = UIUtils.TimeToMSMString(m_raceModeState.currentPenaltyTime);
		}
	}
}
