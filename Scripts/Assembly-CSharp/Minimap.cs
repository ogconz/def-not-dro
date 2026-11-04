using System;
using UnityEngine;

[AddComponentMenu("Scripts/Minimap/Minimap")]
public class Minimap : MonoBehaviour
{
	[Serializable]
	public class IconType
	{
		public string name = "";

		public Sprite icon;

		public string sprite;

		public int size = 32;

		public int depth;

		public Color color = Color.white;
	}

	[Serializable]
	public class TexturePair
	{
		public Texture mainTexture;

		public Texture alphaTexture;
	}

	[SerializeField]
	private LayerMask m_boundsLayers;

	[SerializeField]
	private Vector2 m_boundsCenter;

	[SerializeField]
	private Vector2 m_boundsSize;

	[SerializeField]
	private float m_baseZoom = 1f;

	[SerializeField]
	private TexturePair[] m_textures;

	private MinimapIconSettings settings;

	public static Minimap instance { get; private set; }

	public Vector2 boundsCenter => m_boundsCenter;

	public Vector2 boundsSize => m_boundsSize;

	public float baseZoom => m_baseZoom;

	public TexturePair[] textures => m_textures;

	public Color GetIconColor(int typeIndex)
	{
		return GetIconType(typeIndex).color;
	}

	public int GetIconSize(int typeIndex)
	{
		return GetIconType(typeIndex).size;
	}

	public int GetIconDepth(int typeIndex)
	{
		return GetIconType(typeIndex).depth;
	}

	public IconType GetIconType(int typeIndex)
	{
		if (typeIndex < settings.iconTypes.Length)
		{
			return settings.iconTypes[typeIndex];
		}
		return null;
	}

	[ContextMenu("Update bounds")]
	public void UpdateBounds()
	{
		Bounds? bounds = null;
		Renderer[] array = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
		foreach (Renderer renderer in array)
		{
			if ((m_boundsLayers.value & (1 << renderer.gameObject.layer)) > 0)
			{
				if (!bounds.HasValue)
				{
					bounds = renderer.bounds;
				}
				else
				{
					bounds.Value.Encapsulate(renderer.bounds);
				}
			}
		}
		if (!bounds.HasValue)
		{
			bounds = new Bounds(base.gameObject.transform.position, new Vector3(1f, 1f, 1f));
		}
		Bounds value = bounds.Value;
		m_boundsCenter = new Vector2(value.min.x + (value.max.x - value.min.x) * 0.5f, value.min.z + (value.max.z - value.min.z) * 0.5f);
		m_boundsSize = new Vector2(value.size.x, value.size.z);
	}

	private void Awake()
	{
		instance = this;
		settings = ScriptableSingleton<MinimapIconSettings>.instance;
	}

	private void OnDestroy()
	{
		if (instance == this)
		{
			instance = null;
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawWireCube(new Vector3(m_boundsCenter.x, 0f, m_boundsCenter.y), new Vector3(m_boundsSize.x, 0f, m_boundsSize.y));
	}
}
