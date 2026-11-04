namespace CarX.Gameplay.ClippingZone
{
	public class BaseClippingPoint : BaseClipping
	{
		protected ClippingPointInfo m_info;

		protected bool m_reacted;

		public void SetTouch(bool touch)
		{
			if (!m_reacted)
			{
				if (touch && m_info == null)
				{
					m_info = new ClippingPointInfo();
				}
				if (m_driftController != null && touch && !m_info.touched)
				{
					OnTriggerEnter(null);
					OnTriggerStay(null);
					m_info.touched = true;
					m_reacted = true;
					m_driftController.ClippingPointReaction(m_info);
				}
			}
		}
	}
}
