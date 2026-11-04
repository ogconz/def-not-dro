using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public static class ExtensionMethods
{
	public static bool ContainesAcceptableHitInfo(this RaycastHit hitInfo, string[] ignoredMaterials)
	{
		if (hitInfo.transform != null && hitInfo.rigidbody == null)
		{
			CARXSurface component = hitInfo.transform.gameObject.GetComponent<CARXSurface>();
			if (component == null || !IsIgnoredMaterial(ignoredMaterials, component.templateName))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsIgnoredMaterial(string[] ignoredMaterials, string materialName)
	{
		for (int i = 0; i < ignoredMaterials.Length; i++)
		{
			if (ignoredMaterials[i] == materialName)
			{
				return true;
			}
		}
		return false;
	}

	public static IEnumerator AsIEnumerator(this Task task)
	{
		while (!task.IsCompleted)
		{
			yield return null;
		}
		if (task.IsFaulted && task.Exception != null)
		{
			throw task.Exception;
		}
	}
}
