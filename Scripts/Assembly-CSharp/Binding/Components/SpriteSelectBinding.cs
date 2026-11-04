using System;
using Binding.Base;
using UnityEngine;
using UnityEngine.UI;

namespace Binding.Components
{
	[RequireComponent(typeof(Image))]
	public class SpriteSelectBinding : BaseBinding<IntProperty>
	{
		[Serializable]
		private class ReferenceSprite
		{
			public int value;

			public Sprite sprite;
		}

		private Image m_image;

		[SerializeField]
		private ReferenceSprite[] m_reference;

		private void Awake()
		{
			m_image = GetComponent<Image>();
		}

		protected override void OnUpdateValue()
		{
			base.OnUpdateValue();
			Refresh(base.property.value);
		}

		private void Refresh(int value)
		{
			bool flag = false;
			ReferenceSprite[] reference = m_reference;
			foreach (ReferenceSprite referenceSprite in reference)
			{
				if (referenceSprite.value == value)
				{
					m_image.sprite = referenceSprite.sprite;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				m_image.sprite = null;
			}
		}
	}
}
