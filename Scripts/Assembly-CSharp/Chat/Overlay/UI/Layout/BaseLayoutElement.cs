using UnityEngine;

namespace Chat.Overlay.UI.Layout
{
	public abstract class BaseLayoutElement : MonoBehaviour
	{
		public LayoutType layoutType;

		public TargetType targetType;

		protected RectTransform m_target;

		public abstract void Apply();

		public void SetTargetTransform(RectTransform rect)
		{
			m_target = rect;
		}
	}
}
