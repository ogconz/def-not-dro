using UnityEngine;

public class SingleFlareData : FlareData
{
	[SerializeField]
	private bool m_isHqFlare;

	[SerializeField]
	private Sprite m_sprite;

	[SerializeField]
	private Material m_material;

	[SerializeField]
	private float m_boundsSize = 3f;

	[SerializeField]
	private float m_zOffset = -0.1f;

	[SerializeField]
	private float m_distance = 200f;

	[SerializeField]
	private float m_nearBrightness = 1f;

	[SerializeField]
	private float m_farBrightness;

	[SerializeField]
	private float m_nearSize = 0.1f;

	[SerializeField]
	private float m_farSize = 5f;

	[SerializeField]
	private Vector2 m_offset = Vector2.zero;

	[SerializeField]
	private float m_angle;

	[SerializeField]
	private bool m_directional;

	[SerializeField]
	private float m_directionalFadeOffset;

	[SerializeField]
	private bool m_anamorphic;

	[SerializeField]
	private bool m_xAxisSymmetry;

	[SerializeField]
	private Mesh m_mesh;

	public bool isHqFlare => m_isHqFlare;

	public float BoundsSize => m_boundsSize;

	public float ZOffset => m_zOffset;

	public float Distance => m_distance;

	public float NearBrightness => m_nearBrightness;

	public float FarBrightness => m_farBrightness;

	public Sprite Sprite => m_sprite;

	public float NearSize => m_nearSize;

	public float FarSize => m_farSize;

	public Vector2 Offset => m_offset;

	public float Angle => m_angle;

	public bool Directional => m_directional;

	public float DirectionalFadeOffset => m_directionalFadeOffset;

	public bool Anamorphic => m_anamorphic;

	public bool XAxisSymmetry => m_xAxisSymmetry;

	public override Material Material => m_material;

	public override Mesh Mesh => m_mesh;
}
