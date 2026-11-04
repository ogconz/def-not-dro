using UnityEngine;
using UnityEngine.UI;

namespace Binding.Components
{
	[RequireComponent(typeof(Image))]
	public class SpriteFlipToggleBinding : BoolBinding
	{
		public enum FlipSpriteOrientation
		{
			None = 0,
			Horizontal = 1,
			Vertical = 2,
			Both = 3
		}

		public FlipSpriteOrientation trueOrientation;

		public FlipSpriteOrientation falseOrientation;

		private Image m_image;

		protected override void Start()
		{
			m_image = GetComponent<Image>();
			base.Start();
		}

		protected override void OnUpdateValue()
		{
			base.OnUpdateValue();
			if (m_image != null)
			{
				Vector3 localScale = m_image.rectTransform.localScale;
				switch (currentValue ? trueOrientation : falseOrientation)
				{
				case FlipSpriteOrientation.Horizontal:
					localScale.x = 0f - localScale.x;
					break;
				case FlipSpriteOrientation.Vertical:
					localScale.y = 0f - localScale.y;
					break;
				case FlipSpriteOrientation.Both:
					localScale.x = 0f - localScale.x;
					localScale.y = 0f - localScale.y;
					break;
				}
				m_image.rectTransform.localScale = localScale;
			}
		}
	}
}
