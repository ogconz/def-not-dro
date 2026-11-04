using System.Collections.Generic;
using UnityEngine;

public class CameraTrackingPointsData : MonoBehaviour
{
	public List<Transform> GetPoints()
	{
		List<Transform> list = new List<Transform>(base.transform.childCount);
		for (int i = 0; i < base.transform.childCount; i++)
		{
			list.Add(base.transform.GetChild(i));
		}
		return list;
	}
}
