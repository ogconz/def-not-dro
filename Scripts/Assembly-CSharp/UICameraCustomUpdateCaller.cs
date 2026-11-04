using UnityEngine;

public class UICameraCustomUpdateCaller : MonoBehaviour
{
	public void OnEnable()
	{
		UICustomUpdater.Clear();
	}

	public void OnDisable()
	{
		UICustomUpdater.Clear();
	}

	public void LateUpdate()
	{
		UICustomUpdater.Update();
	}
}
