using UnityEngine;

public class ImmersiveModeInitializer : MonoBehaviour
{
	private void Awake()
	{
		ImmersiveMode.AddCurrentActivity();
	}
}
