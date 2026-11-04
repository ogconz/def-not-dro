using Binding.Base;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Binding.Components
{
	[RequireComponent(typeof(Image))]
	public class AtlasSpriteBinding : BaseBinding<StringProperty>
	{
		public SpriteAtlas[] m_atlases;

		private Image m_component;

		private void Awake()
		{
			m_component = GetComponent<Image>();
		}

		protected override void OnUpdateValue()
		{
			Sprite sprite = null;
			for (int i = 0; i < m_atlases.Length; i++)
			{
				SpriteAtlas spriteAtlas = m_atlases[i];
				if (spriteAtlas != null)
				{
					sprite = spriteAtlas.GetSprite(base.property.value);
					if (sprite != null)
					{
						break;
					}
				}
			}
			m_component.sprite = sprite;
		}
	}
}
