using UnityEngine;

public class CarUsageStatListItem : BaseListItem
{
	private Sprite m_sprite;

	public string spriteName;

	public string name;

	public float percent;

	public string hours;

	public float kilometers;

	private Texture2D m_handle;

	public Sprite GetIcon()
	{
		if (m_sprite == null && !string.IsNullOrEmpty(spriteName))
		{
			m_handle = ResourcesWrapper.Load<Texture2D>("ui/catalog/" + spriteName);
			m_sprite = Sprite.Create(m_handle, new Rect(0f, 0f, m_handle.width, m_handle.height), Vector2.one / 2f, 100f);
		}
		return m_sprite;
	}

	public void Release()
	{
		ResourcesWrapper.UnloadAsset(m_handle);
		m_sprite = null;
		spriteName = string.Empty;
	}
}
