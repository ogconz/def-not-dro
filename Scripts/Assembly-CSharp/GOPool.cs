using System;
using System.Collections.Generic;
using UnityEngine;

public static class GOPool
{
	public static void Allocate<T>(T template, ref List<T> pool, int allocateCount, Action<T> Constructor, Action<T> Deactivate) where T : MonoBehaviour
	{
		while (allocateCount > pool.Count)
		{
			T val = UnityEngine.Object.Instantiate(template, template.transform.parent, worldPositionStays: false);
			pool.Add(val);
			Constructor.Call(val);
		}
		if (Deactivate != null)
		{
			for (int i = allocateCount; i < pool.Count; i++)
			{
				Deactivate(pool[i]);
			}
		}
	}
}
