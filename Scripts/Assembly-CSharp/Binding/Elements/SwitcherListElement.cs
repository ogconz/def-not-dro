using GameInput;
using UnityEngine.UI;

namespace Binding.Elements
{
	public class SwitcherListElement : BaseListElement
	{
		private ISwitcherListElementData m_data;

		public ActionType actionNavNext = ActionType.NavRight;

		public ActionType actionNavPrev = ActionType.NavLeft;

		public Image nextImage;

		public Image prevImage;

		protected override void OnInit()
		{
			m_data = base.data as ISwitcherListElementData;
		}

		public void OnClickNext()
		{
			InputManager.instance.PlayClickEffect(nextImage, m_data.Next, skipPauseToAction: false);
		}

		public void OnClickPrev()
		{
			InputManager.instance.PlayClickEffect(prevImage, m_data.Prev, skipPauseToAction: false);
		}

		public void OnHover()
		{
			m_data.selected = true;
		}

		private void FixedUpdate()
		{
			if (m_data.selected)
			{
				if (InputManager.instance.IsActionPressed(actionNavNext))
				{
					OnClickNext();
				}
				else if (InputManager.instance.IsActionPressed(actionNavPrev))
				{
					OnClickPrev();
				}
			}
		}
	}
}
