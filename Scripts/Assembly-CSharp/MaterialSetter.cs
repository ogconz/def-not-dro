using System.Collections.Generic;
using UnityEngine;

public class MaterialSetter
{
	private int[] m_propertyIDs;

	private Material[][] m_materials;

	public MaterialSetter(GameObject root, string[] propertyNames, bool includeChildren)
	{
		m_propertyIDs = new int[propertyNames.Length];
		int i = 0;
		for (int num = propertyNames.Length; i < num; i++)
		{
			m_propertyIDs[i] = Shader.PropertyToID(propertyNames[i]);
		}
		m_materials = new Material[m_propertyIDs.Length][];
		MeshRenderer[] array = new MeshRenderer[0];
		if (includeChildren)
		{
			array = root.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
		}
		else if ((bool)root.GetComponent<MeshRenderer>())
		{
			array = new MeshRenderer[1] { root.GetComponent<MeshRenderer>() };
		}
		int j = 0;
		for (int num2 = m_propertyIDs.Length; j < num2; j++)
		{
			int nameID = m_propertyIDs[j];
			List<Material> list = new List<Material>();
			int k = 0;
			for (int num3 = array.Length; k < num3; k++)
			{
				MeshRenderer obj = array[k];
				MaterialAccessor orAddComponent = obj.gameObject.GetOrAddComponent<MaterialAccessor>();
				Material[] sharedMaterials = obj.sharedMaterials;
				if (sharedMaterials == null)
				{
					continue;
				}
				int l = 0;
				for (int num4 = sharedMaterials.Length; l < num4; l++)
				{
					Material material = sharedMaterials[l];
					if (material != null && material.HasProperty(nameID))
					{
						list.Add(orAddComponent.GetMaterial(l));
					}
				}
			}
			m_materials[j] = list.ToArray();
		}
	}

	public void SetColor(int propertyIndex, Color value)
	{
		int nameID = m_propertyIDs[propertyIndex];
		Material[] array = m_materials[propertyIndex];
		int i = 0;
		for (int num = array.Length; i < num; i++)
		{
			array[i].SetColor(nameID, value);
		}
	}

	public void SetFloat(int propertyIndex, float value)
	{
		int nameID = m_propertyIDs[propertyIndex];
		Material[] array = m_materials[propertyIndex];
		int i = 0;
		for (int num = array.Length; i < num; i++)
		{
			array[i].SetFloat(nameID, value);
		}
	}

	public void SetVector(int propertyIndex, Vector4 value)
	{
		int nameID = m_propertyIDs[propertyIndex];
		Material[] array = m_materials[propertyIndex];
		int i = 0;
		for (int num = array.Length; i < num; i++)
		{
			array[i].SetVector(nameID, value);
		}
	}
}
