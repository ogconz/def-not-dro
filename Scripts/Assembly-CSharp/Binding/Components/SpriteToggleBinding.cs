using UnityEngine;
using UnityEngine.UI;

namespace Binding.Components
{
	[RequireComponent(typeof(Image))]
	public class SpriteToggleBinding : BoolBinding
	{
		public Sprite trueSprite;

		public Sprite falseSprite;

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
				m_image.sprite = (currentValue ? trueSprite : falseSprite);
			}
		}
	}
}
