using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Tools/Mesh Combine")]
public class MeshCombine : MonoBehaviour
{
	public GameObject[] combinedGameOjects;

	public GameObject combined;

	public bool canGenerateLightmapUV;

	public int vCount;

	public void EnableRenderers(bool e)
	{
		for (int i = 0; i < combinedGameOjects.Length; i++)
		{
			combinedGameOjects[i].GetComponent<Renderer>().enabled = e;
		}
	}

	public void CombineMeshes()
	{
		MeshFilter[] componentsInChildren = GetComponentsInChildren<MeshFilter>(includeInactive: false);
		if (componentsInChildren.Length == 0)
		{
			return;
		}
		GameObject gameObject = new GameObject();
		gameObject.name = "_Combined Mesh [" + base.name + "]";
		gameObject.AddComponent<MeshFilter>();
		gameObject.AddComponent<MeshRenderer>();
		combinedGameOjects = new GameObject[componentsInChildren.Length];
		List<Material> list = new List<Material>();
		List<List<CombineInstance>> list2 = new List<List<CombineInstance>>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			MeshFilter meshFilter = componentsInChildren[i];
			MeshRenderer component = meshFilter.GetComponent<MeshRenderer>();
			component.enabled = false;
			combinedGameOjects[i] = meshFilter.gameObject;
			for (int j = 0; j < meshFilter.sharedMesh.subMeshCount; j++)
			{
				if (j < component.sharedMaterials.Length && j < meshFilter.sharedMesh.subMeshCount)
				{
					int num = list.IndexOf(component.sharedMaterials[j]);
					if (num == -1)
					{
						list.Add(component.sharedMaterials[j]);
						num = list.Count - 1;
						list2.Add(new List<CombineInstance>());
					}
					CombineInstance item = new CombineInstance
					{
						transform = component.transform.localToWorldMatrix,
						subMeshIndex = j,
						mesh = meshFilter.sharedMesh
					};
					list2[num].Add(item);
				}
				else
				{
					Debug.LogWarning("Simple Mesh Combine: GameObject [ " + component.gameObject.name + " ] is missing a material (Mesh or sub-mesh ignored from combine)");
				}
			}
		}
		Mesh[] array = new Mesh[list.Count];
		CombineInstance[] array2 = new CombineInstance[list.Count];
		for (int k = 0; k < list.Count; k++)
		{
			array[k] = new Mesh();
			array[k].CombineMeshes(list2[k].ToArray(), mergeSubMeshes: true, useMatrices: true);
			array2[k] = default(CombineInstance);
			array2[k].mesh = array[k];
			array2[k].subMeshIndex = 0;
		}
		gameObject.GetComponent<MeshFilter>().sharedMesh = new Mesh();
		gameObject.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(array2, mergeSubMeshes: false, useMatrices: false);
		Mesh[] array3 = array;
		foreach (Mesh obj in array3)
		{
			obj.Clear();
			Object.DestroyImmediate(obj);
		}
		gameObject.GetComponent<MeshRenderer>().sharedMaterials = list.ToArray();
		combined = gameObject.gameObject;
		EnableRenderers(e: false);
		gameObject.transform.parent = base.transform;
		gameObject.GetComponent<MeshFilter>().sharedMesh.uv2 = null;
		vCount = gameObject.GetComponent<MeshFilter>().sharedMesh.vertexCount;
		if (vCount > 65536)
		{
			Debug.LogWarning("Vertex Count: " + vCount + "- Vertex Count too high, please divide mesh combine into more groups. Max 65536 for each mesh");
			canGenerateLightmapUV = false;
		}
		else
		{
			canGenerateLightmapUV = true;
		}
	}

	public void GenerateLightmapUV()
	{
	}
}
