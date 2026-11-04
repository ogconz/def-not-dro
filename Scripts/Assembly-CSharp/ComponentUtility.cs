using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public static class ComponentUtility
{
	public static T[] FindAllComponent<T>(this Transform parent) where T : Component
	{
		List<T> list = new List<T>(parent.childCount);
		for (int i = 0; i < parent.childCount; i++)
		{
			Transform child = parent.GetChild(i);
			T[] array = child.FindAllComponent<T>();
			if (array != null && array.Length != 0)
			{
				list.AddRange(array);
			}
			T component = child.GetComponent<T>();
			if (component != null)
			{
				list.Add(component);
			}
		}
		return list.ToArray();
	}

	public static T[] FindAllComponent<T>(this Transform parent, Func<T, bool> exp) where T : Component
	{
		List<T> list = new List<T>(parent.childCount);
		for (int i = 0; i < parent.childCount; i++)
		{
			Transform child = parent.GetChild(i);
			T[] array = child.FindAllComponent(exp);
			if (array != null && array.Length != 0)
			{
				list.AddRange(array);
			}
			T component = child.GetComponent<T>();
			if (component != null && exp(component))
			{
				list.Add(component);
			}
		}
		return list.ToArray();
	}

	public static void SetLightLayer(this Transform transform, RenderingLayerMask layerEnum)
	{
		MeshRenderer[] array = transform.FindAllComponent<MeshRenderer>();
		SkinnedMeshRenderer[] array2 = transform.FindAllComponent<SkinnedMeshRenderer>();
		MeshRenderer[] array3 = array;
		for (int i = 0; i < array3.Length; i++)
		{
			array3[i].renderingLayerMask = (uint)layerEnum;
		}
		SkinnedMeshRenderer[] array4 = array2;
		for (int i = 0; i < array4.Length; i++)
		{
			array4[i].renderingLayerMask = (uint)layerEnum;
		}
	}
}
