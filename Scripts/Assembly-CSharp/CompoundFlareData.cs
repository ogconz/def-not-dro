using UnityEngine;

public class CompoundFlareData : FlareData
{
	[SerializeField]
	private FlareData[] m_flareData;

	[SerializeField]
	private Mesh m_mesh;

	[SerializeField]
	private Material m_material;

	public FlareData[] FlareData => m_flareData;

	public override Material Material => m_material;

	public override Mesh Mesh => m_mesh;
}
