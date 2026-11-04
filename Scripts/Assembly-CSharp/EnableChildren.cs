using UnityEngine;

public class EnableChildren : MonoBehaviour
{
	private void Awake()
	{
		int childCount = base.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			Transform child = base.transform.GetChild(i);
			if (!child.gameObject.activeSelf)
			{
				child.gameObject.SetActive(value: true);
			}
		}
	}
}
