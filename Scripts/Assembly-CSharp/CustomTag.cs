using UnityEngine;

public class CustomTag : MonoBehaviour
{
	public string customTag;

	public void OnEnable()
	{
		CustomTagManager.Register(customTag, base.gameObject);
	}

	public void OnDisable()
	{
		CustomTagManager.Unregister(base.gameObject);
	}
}
