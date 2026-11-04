using UnityEngine;

[AddComponentMenu("Scripts/Custom/Misc/MemoryWarningReceiver")]
public class MemoryWarningReceiver : MonoBehaviour
{
	private void DidReceiveMemoryWarningDelegate(string message)
	{
		States.ClearMemory();
	}
}
