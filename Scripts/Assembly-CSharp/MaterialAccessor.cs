using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class MaterialAccessor : MonoBehaviour
{
	private Material[] m_materials;

	private Material[] m_sourceRendererMaterials;

	public int materialsCount
	{
		get
		{
			InitIfNeeded();
			return m_materials.Length;
		}
	}

	public IEnumerator<Material> GetEnumerator()
	{
		InitIfNeeded();
		int i = 0;
		int iMax = m_materials.Length;
		while (i < iMax)
		{
			yield return GetMaterial(i);
			int num = i + 1;
			i = num;
		}
	}

	public Material GetMaterial(int index)
	{
		InitIfNeeded();
		Material material = m_materials[index];
		if (!material)
		{
			material = m_sourceRendererMaterials[index];
			m_materials[index] = material;
		}
		return material;
	}

	private void InitIfNeeded()
	{
		if (m_materials == null)
		{
			Renderer component = GetComponent<Renderer>();
			m_sourceRendererMaterials = component.materials;
			m_materials = new Material[m_sourceRendererMaterials.Length];
		}
	}

	private void OnDestroy()
	{
		if (m_materials != null)
		{
			Material[] materials = m_materials;
			foreach (Material material in materials)
			{
				if ((bool)material)
				{
					Object.DestroyImmediate(material);
				}
			}
		}
		m_materials = null;
		m_sourceRendererMaterials = null;
	}
}
