using UnityEngine;
using UnityEngine.UI;

namespace HUD
{
	public class UIMinimapItem : MonoBehaviour
	{
		[SerializeField]
		private Image m_image;

		private RectTransform m_rectTransform;

		public Sprite image
		{
			get
			{
				if (!m_image)
				{
					return null;
				}
				return m_image.sprite;
			}
			set
			{
				if ((bool)m_image)
				{
					m_image.sprite = value;
				}
			}
		}

		public Color color
		{
			get
			{
				if (!m_image)
				{
					return Color.white;
				}
				return m_image.color;
			}
			set
			{
				if ((bool)m_image)
				{
					m_image.color = value;
				}
			}
		}

		public Vector2 position
		{
			get
			{
				return rectTransform.anchoredPosition;
			}
			set
			{
				rectTransform.anchoredPosition = value;
			}
		}

		public Vector2 size
		{
			get
			{
				return rectTransform.sizeDelta;
			}
			set
			{
				rectTransform.sizeDelta = value;
			}
		}

		public Vector2 scale
		{
			get
			{
				return rectTransform.localScale;
			}
			set
			{
				rectTransform.localScale = value;
			}
		}

		public float angle
		{
			get
			{
				return rectTransform.localEulerAngles.z;
			}
			set
			{
				rectTransform.localEulerAngles = Vector3.forward * value;
			}
		}

		public MinimapItem item { get; private set; }

		public RectTransform rectTransform
		{
			get
			{
				if (m_rectTransform == null)
				{
					m_rectTransform = base.transform as RectTransform;
				}
				return m_rectTransform;
			}
		}

		private void Awake()
		{
			if (m_image == null)
			{
				m_image = GetComponent<Image>();
			}
		}

		private void OnValidate()
		{
			if (m_image == null)
			{
				Debug.LogError(base.name + " - m_image == null");
			}
		}

		public void Init(MinimapItem item)
		{
			if (!(this.item == item) && (bool)item.cachedTransform)
			{
				this.item = item;
				angle = 0f - item.cachedTransform.localEulerAngles.y;
				Minimap.IconType iconType = Minimap.instance.GetIconType(item.typeIndex);
				image = iconType.icon;
			}
		}
	}
}
