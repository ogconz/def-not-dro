using UnityEngine;

namespace Chat.Overlay.UI.Layout
{
	public class ActionPanelLayoutElement : BaseLayoutElement
	{
		public override void Apply()
		{
			RectTransform rectTransform = base.transform as RectTransform;
			m_target.SetParent(base.transform.parent, worldPositionStays: false);
			m_target.pivot = rectTransform.pivot;
			m_target.anchorMin = rectTransform.anchorMin;
			m_target.anchorMax = rectTransform.anchorMax;
			m_target.anchoredPosition = rectTransform.anchoredPosition;
		}
	}
}
