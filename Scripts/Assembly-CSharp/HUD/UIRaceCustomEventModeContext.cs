using Binding;
using DB.Meta;
using UnityEngine;

namespace HUD
{
	public class UIRaceCustomEventModeContext : UIRaceModeContext
	{
		[SerializeField]
		private UICapsResultContext m_capsResult;

		private HalloweenEventState m_raceModeState;

		private StringProperty m_record = new StringProperty();

		private IRaceNorm m_norms;

		protected override void OnActivate()
		{
			base.OnActivate();
			m_raceModeState = States.current as HalloweenEventState;
			m_norms = new EmptyRaceNormMeta();
			m_record.value = UIUtils.MakeViewBestDriftPoints(m_gamePrefs.globals.lastTrackId);
			if (m_capsResult != null && m_norms != null)
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
			if (m_capsResult != null && m_norms != null)
			{
				RaceCupType cupByValue = m_norms.GetCupByValue(m_raceModeState.currentDriftPoints);
				m_capsResult.cupsLevel.value = (int)cupByValue;
			}
		}
	}
}
