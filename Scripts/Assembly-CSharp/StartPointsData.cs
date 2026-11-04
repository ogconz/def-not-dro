using UnityEngine;

public class StartPointsData : MonoBehaviour
{
	public StartPosition[] GetPoints()
	{
		return GetComponentsInChildren<StartPosition>();
	}
}
