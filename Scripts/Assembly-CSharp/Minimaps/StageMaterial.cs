using UnityEngine;
using UnityEngine.UI;

namespace Minimaps
{
	public class StageMaterial
	{
		public int zoneId;

		private RawImage m_uiTexture;

		public bool IsApplied
		{
			get
			{
				if (m_uiTexture != null)
				{
					return m_uiTexture.texture;
				}
				return false;
			}
		}

		public StageMaterial(RawImage texture)
		{
			m_uiTexture = texture;
		}

		private static Minimap.TexturePair GetMinimapTexture(byte id)
		{
			if (Minimap.instance.textures == null || id >= Minimap.instance.textures.Length)
			{
				return null;
			}
			return Minimap.instance.textures[id];
		}

		public void Restore()
		{
			if (m_uiTexture != null && m_uiTexture.texture == null)
			{
				SetAlpha(0f);
			}
		}

		public void ApplyStage()
		{
			Restore();
			Minimap.TexturePair minimapTexture = GetMinimapTexture((byte)zoneId);
			if (minimapTexture != null && !(minimapTexture.mainTexture == null))
			{
				if ((bool)m_uiTexture)
				{
					m_uiTexture.texture = minimapTexture.mainTexture;
				}
				if (m_uiTexture != null && m_uiTexture.texture != null)
				{
					SetAlpha(1f);
				}
			}
		}

		public void SetCustomTextureDeltaSize()
		{
			Minimap.TexturePair minimapTexture = GetMinimapTexture((byte)zoneId);
			if (minimapTexture != null && !(minimapTexture.mainTexture == null))
			{
				m_uiTexture.rectTransform.sizeDelta = new Vector2(minimapTexture.mainTexture.width, minimapTexture.mainTexture.height);
			}
		}

		public void SetAlpha(float value)
		{
			if ((bool)m_uiTexture)
			{
				Color color = m_uiTexture.color;
				color.a = Mathf.Lerp(0f, 0.5f, value);
				m_uiTexture.color = color;
			}
		}
	}
}
